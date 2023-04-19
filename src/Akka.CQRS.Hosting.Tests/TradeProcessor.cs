

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.TradeProcessor.Actors;
using Akka.Util;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;
using FluentAssertions;
using Akka.CQRS.Pricing.Actors;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Remote;
using Petabridge.Cmd.Cluster;
using Akka.CQRS.Pricing.Cli;
using Microsoft.Extensions.Hosting;

namespace Akka.CQRS.Hosting.Tests
{
    public class TradeProcessor : Akka.Hosting.TestKit.TestKit
    {
        private readonly string _sqlConnectionString = "Server=sql,1633;User Id=sa;Password=This!IsOpenSource1;TrustServerCertificate=true";
       
       
        private readonly ITestOutputHelper _output;
        public TradeProcessor(ITestOutputHelper output)
        {
            _output = output;
        }
        private async Task<IHost> TraderHost()
        {
            var tcs = new TaskCompletionSource();
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));

            var host = new HostBuilder()
                .ConfigureServices(collection =>
                {
                    collection.AddAkka("AkkaTrader", builder  =>
                    {
                        #region Trade Processors
                        builder
                            .WithRemoting(new RemoteOptions
                            {
                                HostName = "127.0.0.1",
                                PublicHostName = "127.0.0.1",
                                Port = 5055,
                                PublicPort = 5055,
                            })
                            .WithClustering(new ClusterOptions
                            {
                                SeedNodes = new[] { "akka.tcp://AkkaTrader@127.0.0.1:5055" },
                                Roles = new[] { "trade-processor", "trade-events" }
                            })
                            .WithDistributedPubSub("trade-events")
                            .WithShardRegion<OrderBookActor>("orderBook",
                            (system, registry, resolver) => s => OrderBookActor.PropsFor(s),
                            new StockShardMsgRouter(), new ShardOptions()
                            {
                                Role = "trade-processor"
                            })
                            .WithSqlServerPersistence(_sqlConnectionString, journalBuilder: builder =>
                            {
                                builder.AddWriteEventAdapter<StockEventTagger>("stock-tagger", new[] { typeof(IWithStockId) });
                            })
                            .AddHocon(@$"akka.persistence.journal.sql.provider-name = SqlServer.2019", HoconAddMode.Prepend)
                            .AddPetabridgeCmd(cmd =>
                            {
                                _output.WriteLine("   PetabridgeCmd Added");
                                cmd.RegisterCommandPalette(ClusterCommands.Instance);
                                cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                                cmd.RegisterCommandPalette(new RemoteCommands());
                                cmd.Start();
                            });
                        #endregion
                    });
                }).Build();

            await host.StartAsync(cancellationTokenSource.Token);
            //await Task.Delay(TimeSpan.FromSeconds(120));
            await (tcs.Task.WaitAsync(cancellationTokenSource.Token));

            return host;
        }
        private async Task TraderHostProx()
        {
            using var host1 = await TestHelper.CreateHost(builder =>
            {
                #region Trade Processors
                builder
                .WithRemoting(new RemoteOptions
                {
                    HostName = "127.0.0.1",
                    Port = 5054,
                })
                .WithClustering(new ClusterOptions
                {
                    SeedNodes = new[] { "akka.tcp://AkkaTrader@127.0.0.1:5055" },
                    Roles = new[] { "trader", "trade-events" }
                })
                .WithDistributedPubSub("trade-events")
                .WithShardRegionProxy<OrderBookActor>("orderBook", "trade-processor", new StockShardMsgRouter())
                .WithActors((system, registry) =>
                {
                    var order = registry.Get<OrderBookActor>();
                    foreach (var stock in AvailableTickerSymbols.Symbols)
                    {
                        var max = (decimal)ThreadLocalRandom.Current.Next(20, 45);
                        var min = (decimal)ThreadLocalRandom.Current.Next(10, 15);
                        var range = new PriceRange(min, 0.0m, max);

                        // start bidders                       
                        foreach (var i in System.Linq.Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                        {
                            system.ActorOf(Props.Create(() => new BidderActor(stock, range, order)));
                        }
                        // start askers                       
                        foreach (var i in System.Linq.Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                        {
                            system.ActorOf(Props.Create(() => new AskerActor(stock, range, order)));
                        }
                    }
                })
                .AddPetabridgeCmd(cmd =>
                {
                    cmd.RegisterCommandPalette(ClusterCommands.Instance);
                    cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    cmd.RegisterCommandPalette(new RemoteCommands());
                    cmd.Start();
                });
                #endregion
            }, new ClusterOptions() { /*SeedNodes = new[] { "akka.tcp://AkkaTrader@lighthouse:4053" } */}, _output);

        }
        private async Task PriceHost()
        {

            using var host1 = await TestHelper.CreateHost(builder =>
            {
                #region Pricing
                builder
                    .WithRemoting(new RemoteOptions
                    {
                        HostName = "127.0.0.1",
                        PublicHostName = "127.0.0.1",
                        Port = 6055,
                        PublicPort = 6055,
                    })
                    .WithClustering(new ClusterOptions
                    {
                        SeedNodes = new[] { "akka.tcp://AkkaPricing@127.0.0.1:6055" },
                        Roles = new[] { "pricing-engine", "price-events" }
                    })
                    .WithDistributedPubSub("price-events")
                    .WithShardRegion<MatchAggregator>("priceAggregator",
                     (system, registry, resolver) => s => Props.Create(() => new MatchAggregator(s, system.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier))),
                     new StockShardMsgRouter(), new ShardOptions() { Role = "pricing-engine" })
                    .WithSingleton<PriceInitiatorActor>("price-initiator",
                         (_, _, resolver) => resolver.Props<PriceInitiatorActor>(),
                         new ClusterSingletonOptions() { Role = "pricing-engine", LeaseRetryInterval = TimeSpan.FromSeconds(1), BufferSize = 10 })
                    .WithSqlServerPersistence(_sqlConnectionString, journalBuilder: builder =>
                    {
                        builder.AddWriteEventAdapter<StockEventTagger>("stock-tagger", new[] { typeof(IWithStockId) });
                    })
                    .AddHocon(@$"akka.persistence.journal.sql.provider-name = SqlServer.2019", HoconAddMode.Prepend)
                    .AddPetabridgeCmd(cmd =>
                    {
                        void RegisterPalette(CommandPaletteHandler h)
                        {
                            if (cmd.RegisterCommandPalette(h))
                            {
                                _output.WriteLine("Petabridge.Cmd - Registered {0}", h.Palette.ModuleName);
                            }
                            else
                            {
                                _output.WriteLine("Petabridge.Cmd - DID NOT REGISTER {0}", h.Palette.ModuleName);
                            }
                        }

                        var actorSystem = cmd.Sys;
                        var actorRegistry = ActorRegistry.For(actorSystem);
                        var priceViewMaster = actorRegistry.Get<PriceViewMaster>();

                        RegisterPalette(ClusterCommands.Instance);
                        RegisterPalette(new RemoteCommands());
                        RegisterPalette(ClusterShardingCommands.Instance);
                        RegisterPalette(new PriceCommands(priceViewMaster));
                        cmd.Start();
                    })
                    .StartActors((system, registry) =>
                    {
                        var priceViewMaster = system.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");
                        registry.Register<PriceViewMaster>(priceViewMaster);

                    });
                #endregion
            }, new ClusterOptions() { /*SeedNodes = new[] { "akka.tcp://AkkaTrader@lighthouse:4053" } */}, _output, "AkkaPricing");
           
        }
        protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        {
            #region Trade Processors
            builder
                .WithRemoting(new RemoteOptions
                {
                    HostName = "127.0.0.1",
                    PublicHostName = "127.0.0.1",
                    Port = 5055,
                    PublicPort = 5055,
                })
                .WithClustering(new ClusterOptions
                {
                    SeedNodes = new[] { "akka.tcp://AkkaTrader@127.0.0.1:5055" },
                    Roles = new[] { "trade-processor", "trade-events" }
                })
                .WithDistributedPubSub("trade-events")
                .WithShardRegion<OrderBookActor>("orderBook",
                (system, registry, resolver) => s => OrderBookActor.PropsFor(s),
                new StockShardMsgRouter(), new ShardOptions()
                {
                    Role = "trade-processor"
                })
                .WithSqlServerPersistence(_sqlConnectionString, journalBuilder: builder =>
                {
                    builder.AddWriteEventAdapter<StockEventTagger>("stock-tagger", new[] { typeof(IWithStockId) });
                })
                .AddHocon(@$"akka.persistence.journal.sql.provider-name = SqlServer.2019", HoconAddMode.Prepend)
                .AddPetabridgeCmd(cmd =>
                {
                    _output.WriteLine("   PetabridgeCmd Added");
                    cmd.RegisterCommandPalette(ClusterCommands.Instance);
                    cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    cmd.RegisterCommandPalette(new RemoteCommands());
                    cmd.Start();
                });
            #endregion
        }

        [Fact]
        public void Trade_Processor_Test()
        {
            // arrange
            var orderBook = ActorRegistry.Get<OrderBookActor>();
           
            // act

            // assert
            orderBook.Should().NotBeNull();
            //id.Should().Be("foo");
            //sourceRef.Should().Be(actorRegistry.Get<OrderBookActor>());
        }

        [Fact]
        public async Task Trade_Places_Test()
        {
            // arrange
            
            
            
            // act
            //var id = await shardRegion.Ask<string>(new OrderBookActor.GetId("foo"), TimeSpan.FromSeconds(3));
            // sourceRef = 
            // await shardRegion.Ask<IActorRef>(new OrderBookActor.GetSourceRef("foo"), TimeSpan.FromSeconds(3));

            // assert
            //id.Should().Be("foo");
            //sourceRef.Should().Be(actorRegistry.Get<OrderBookActor>());
        }

        [Fact]
        public async Task Trade_Price_Test()
        {
            // arrange
            await TraderHostProx();
            await PriceHost();
            var shardRegion = ActorRegistry.Get<MatchAggregator>();
            var priceViewMaster = Sys.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");
            //registry.Register<PriceViewMaster>(priceViewMaster);
            //var priceViewMaster = ActorRegistry.Get<PriceViewMaster>();
            // start the creation of the pricing views
            priceViewMaster.Tell(new PriceViewMaster.BeginTrackPrices(shardRegion));
            // act
            //var id = await shardRegion.Ask<string>(new OrderBookActor.GetId("foo"), TimeSpan.FromSeconds(3));
            // sourceRef = 
            // await shardRegion.Ask<IActorRef>(new OrderBookActor.GetSourceRef("foo"), TimeSpan.FromSeconds(3));

            // assert
            //id.Should().Be("foo");
            //sourceRef.Should().Be(actorRegistry.Get<OrderBookActor>());
        }
    }
}
