

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;
using Akka.CQRS.Infrastructure.Ops;
using static Akka.CQRS.Infrastructure.SqlDbHoconHelper;
using Akka.Hosting;
using Akka.Persistence.Sql;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.TradeProcessor.Actors;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Remote;
using Microsoft.Extensions.DependencyInjection;
using Akka.Util;
using Akka.Bootstrap.Docker;
using Microsoft.Extensions.Options;
using Akka.Cluster.Tools.Singleton;
using Akka.CQRS.Pricing.Actors;
using Akka.CQRS.Pricing.Cli;
using Akka.Persistence.Query;
using Akka.Persistence.Sql.Query;
using Testcontainers.MsSql;

namespace Akka.CQRS.Hosting.Tests
{
    public class TradeProcessor : IAsyncLifetime
    {
        private readonly string _sqlConnectionString = "Server=sql,1433;User Id=sa;Password=This!IsOpenSource1;TrustServerCertificate=true";
        private readonly string _sqlProviderName = "SqlServer.2019";

        private MsSqlContainer _dockerContainer = null!;
        private IContainer _lighthouse = null!;

        public TradeProcessor(ITestOutputHelper output)
        {
            Output = output;
        }

        public ITestOutputHelper Output { get; }
        public async Task DisposeAsync()
        {
            await _dockerContainer.StopAsync();
            await _lighthouse.StopAsync();
        }

        public async Task InitializeAsync()
        {
            #region SQL
            _dockerContainer = new MsSqlBuilder()
                .WithName("sql")
                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                .WithPortBinding(1633, 1433)
                .WithPassword("This!IsOpenSource1")
                .WithEnvironment(new Dictionary<string, string>()
                {
                        { "ACCEPT_EULA", "Y" }
                })
                .WithCleanUp(true)
                .Build();

            await _dockerContainer.StartAsync();
            #endregion
            #region lighthouse
            _lighthouse = new ContainerBuilder()
           .WithName("lighthouse")
           .WithImage("petabridge/lighthouse:latest")
           .WithPortBinding(9110, 9110)
           .WithPortBinding(4053, 4053)
           .WithEnvironment(new Dictionary<string, string>()
           {
                { "ACTORSYSTEM", "AkkaTrader" },
                { "CLUSTER_PORT", "4053"},
                { "CLUSTER_IP", "lighthouse"},
                { "CLUSTER_SEEDS", "akka.tcp://AkkaTrader@lighthouse:4053"}

           })
           
           .Build();

            await _lighthouse.StartAsync();
            #endregion
        }
        [Fact]
        public async Task Processor()
        {
            // arrange
            var config = await File.ReadAllTextAsync("C:\\Users\\Ebere\\source\\repos\\InMemoryCQRSReplication\\src\\Akka.CQRS.TradeProcessor.Service\\app.conf");
            using var host0 = await TestHelper.CreateHost(builder =>
            {
                // Add HOCON configuration from Docker
                var conf = ConfigurationFactory.ParseString(config)
                    .WithFallback(GetSqlHocon(_sqlConnectionString, _sqlProviderName))
                    .WithFallback(OpsConfig.GetOpsConfig())
                    .WithFallback(ClusterSharding.DefaultConfig())
                    .WithFallback(DistributedPubSub.DefaultConfig())
                    .WithFallback(SqlPersistence.DefaultConfiguration);
                builder.AddHocon(conf, HoconAddMode.Prepend)
                .WithActors((system, registry) =>
                {
                    Cluster.Cluster.Get(system).RegisterOnMemberUp(() =>
                    {
                        var sharding = ClusterSharding.Get(system);

                        var shardRegion = sharding.Start("orderBook", s => OrderBookActor.PropsFor(s), ClusterShardingSettings.Create(system),
                            new StockShardMsgRouter());
                    });
                })
                .WithActors((system, registry) =>
                {
                    Cluster.Cluster.Get(system).RegisterOnMemberUp(() =>
                    {
                        var sharding = ClusterSharding.Get(system);

                        var shardRegionProxy = sharding.StartProxy("orderBook", "trade-processor", new StockShardMsgRouter());
                        foreach (var stock in AvailableTickerSymbols.Symbols)
                        {
                            var max = (decimal)ThreadLocalRandom.Current.Next(20, 45);
                            var min = (decimal)ThreadLocalRandom.Current.Next(10, 15);
                            var range = new PriceRange(min, 0.0m, max);

                            // start bidders
                            foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                            {
                                system.ActorOf(Props.Create(() => new BidderActor(stock, range, shardRegionProxy)));
                            }

                            // start askers
                            foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                            {
                                system.ActorOf(Props.Create(() => new AskerActor(stock, range, shardRegionProxy)));
                            }
                        }
                    });
                })
                 .AddPetabridgeCmd(cmd =>
                 {
                     Console.WriteLine("   PetabridgeCmd Added");
                     cmd.RegisterCommandPalette(ClusterCommands.Instance);
                     cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                     cmd.RegisterCommandPalette(new RemoteCommands());
                     cmd.Start();
                 });
            }, new ClusterOptions() { /*SeedNodes = new[] { "akka.tcp://AkkaTrader@lighthouse:4053" } */}, Output) ;
            config = await File.ReadAllTextAsync("C:\\Users\\Ebere\\source\\repos\\InMemoryCQRSReplication\\src\\Akka.CQRS.TradePlacers.Service\\app.conf");
            using var host1 = await TestHelper.CreateHost(builder =>
            {
                // Add HOCON configuration from Docker
                var conf = ConfigurationFactory.ParseString(config)
                    .WithFallback(OpsConfig.GetOpsConfig())
                    .WithFallback(ClusterSharding.DefaultConfig())
                    .WithFallback(DistributedPubSub.DefaultConfig());
                builder.AddHocon(conf, HoconAddMode.Prepend)
                
                .WithActors((system, registry) =>
                {
                    Cluster.Cluster.Get(system).RegisterOnMemberUp(() =>
                    {
                        var sharding = ClusterSharding.Get(system);

                        var shardRegionProxy = sharding.StartProxy("orderBook", "trade-processor", new StockShardMsgRouter());
                        foreach (var stock in AvailableTickerSymbols.Symbols)
                        {
                            var max = (decimal)ThreadLocalRandom.Current.Next(20, 45);
                            var min = (decimal)ThreadLocalRandom.Current.Next(10, 15);
                            var range = new PriceRange(min, 0.0m, max);

                            // start bidders
                            foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                            {
                                system.ActorOf(Props.Create(() => new BidderActor(stock, range, shardRegionProxy)));
                            }

                            // start askers
                            foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                            {
                                system.ActorOf(Props.Create(() => new AskerActor(stock, range, shardRegionProxy)));
                            }
                        }
                    });
                })
                 .AddPetabridgeCmd(cmd =>
                 {
                     cmd.RegisterCommandPalette(ClusterCommands.Instance);
                     cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                     cmd.RegisterCommandPalette(new RemoteCommands());
                     cmd.Start();
                 });
            }, new ClusterOptions() { /*SeedNodes = new[] { "akka.tcp://AkkaTrader@lighthouse:4053" } */}, Output);
            config = await File.ReadAllTextAsync("C:\\Users\\Ebere\\source\\repos\\InMemoryCQRSReplication\\src\\Akka.CQRS.Pricing.Service\\app.conf");
            using var host2 = await TestHelper.CreateHost(builder =>
            {
                // Add HOCON configuration from Docker
                var conf = ConfigurationFactory.ParseString(config)
                     .WithFallback(GetSqlHocon(_sqlConnectionString, _sqlProviderName))
                     .WithFallback(OpsConfig.GetOpsConfig())
                     .WithFallback(ClusterSharding.DefaultConfig())
                     .WithFallback(DistributedPubSub.DefaultConfig())
                     .WithFallback(SqlPersistence.DefaultConfiguration);
                builder.AddHocon(conf, HoconAddMode.Prepend)
                .WithActors((system, registry) =>
                {
                    var priceViewMaster = system.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");
                    registry.Register<PriceViewMaster>(priceViewMaster);
                    // used to seed pricing data
                    var readJournal = system.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
                    Cluster.Cluster.Get(system).RegisterOnMemberUp(() =>
                    {
                        var sharding = ClusterSharding.Get(system);

                        var shardRegion = sharding.Start("priceAggregator",
                            s => Props.Create(() => new MatchAggregator(s, readJournal)),
                            ClusterShardingSettings.Create(system),
                            new StockShardMsgRouter());

                        // used to seed pricing data
                        var singleton = ClusterSingletonManager.Props(
                            Props.Create(() => new PriceInitiatorActor(readJournal, shardRegion)),
                            ClusterSingletonManagerSettings.Create(
                                system.Settings.Config.GetConfig("akka.cluster.price-singleton")));

                        // start the creation of the pricing views
                        priceViewMaster.Tell(new PriceViewMaster.BeginTrackPrices(shardRegion));
                    });

                })
                .AddPetabridgeCmd(cmd =>
                {
                    void RegisterPalette(CommandPaletteHandler h)
                    {
                        if (cmd.RegisterCommandPalette(h))
                        {
                            Console.WriteLine("Petabridge.Cmd - Registered {0}", h.Palette.ModuleName);
                        }
                        else
                        {
                            Console.WriteLine("Petabridge.Cmd - DID NOT REGISTER {0}", h.Palette.ModuleName);
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
                });
            }, new ClusterOptions() { /*SeedNodes = new[] { "akka.tcp://AkkaTrader@lighthouse:4053" } */}, Output, "AkkaPricing");
            
            
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
