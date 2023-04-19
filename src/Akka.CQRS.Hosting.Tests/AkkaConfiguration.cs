using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.Pricing.Actors;
using Akka.CQRS.TradeProcessor.Actors;
using Akka.Hosting;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;
using Akka.Persistence.Query;
using Akka.Util;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;

namespace Akka.CQRS.Hosting.Tests;

public static class AkkaConfiguration
{
    public static AkkaConfigurationBuilder ConfigureTradeProcessor(this AkkaConfigurationBuilder builder, string connectionString)
    {
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
                    Roles = new[] { "trade-processor", "trader", "trade-events", "pricing-engine", "price-events" }
                })
                .WithActors((system, registry) =>
                {
                    Cluster.Cluster.Get(system).RegisterOnMemberUp(() =>
                    {
                        var sharding = ClusterSharding.Get(system);
                        
                        var shardRegion = sharding.Start("orderBook", s => OrderBookActor.PropsFor(s), 
                            ClusterShardingSettings.Create(system).WithRole("trade-processor"),
                            new StockShardMsgRouter());
                    });
                })
                .WithDistributedPubSub("trade-events")
                /*.WithShardRegion<OrderBookActor>("orderBook",
                (system, registry, resolver) => s => OrderBookActor.PropsFor(s),
                new StockShardMsgRouter(), new ShardOptions()
                {
                    Role = "trade-processor"
                })
                */
                .WithSqlServerPersistence(connectionString, journalBuilder: builder =>
                {
                    builder.AddWriteEventAdapter<StockEventTagger>("stock-tagger", new[] { typeof(IWithStockId) });
                })
                .AddHocon(@$"akka.persistence.journal.sql.provider-name = SqlServer.2019", HoconAddMode.Prepend);
        return builder;
    }
    public static AkkaConfigurationBuilder ConfigurePrices(this AkkaConfigurationBuilder builder)
    {
                builder
                .WithDistributedPubSub("price-events")
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
                            ClusterShardingSettings.Create(system).WithRole("pricing-engine"),
                            new StockShardMsgRouter());

                        // used to seed pricing data
                        var singleton = ClusterSingletonManager.Props(
                            Props.Create(() => new PriceInitiatorActor(readJournal, shardRegion)),
                            ClusterSingletonManagerSettings.Create(
                                system.Settings.Config.GetConfig("akka.cluster.price-singleton"))
                            .WithRole("pricing-engine")
                            .WithSingletonName("pricing-engine")
                            .WithHandOverRetryInterval(TimeSpan.FromSeconds(1)));

                        // start the creation of the pricing views
                        priceViewMaster.Tell(new PriceViewMaster.BeginTrackPrices(shardRegion));
                    });

                })
                                  /*.WithShardRegion<MatchAggregator>("priceAggregator",
                                       (system, registry, resolver) => s => Props.Create(() => new MatchAggregator(s, system.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier))),
                                       new StockShardMsgRouter(), new ShardOptions() { Role = "pricing-engine" })
                                  .WithSingleton<PriceInitiatorActor>("price-initiator",
                                           (_, _, resolver) => resolver.Props<PriceInitiatorActor>(),
                                           new ClusterSingletonOptions() { Role = "pricing-engine", LeaseRetryInterval = TimeSpan.FromSeconds(1), BufferSize = 10 }) */;
        return builder;
    }
    public static AkkaConfigurationBuilder ConfigureTradeProcessorProxy(this AkkaConfigurationBuilder builder)
    {
        builder
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
        /*.WithShardRegionProxy<OrderBookActor>("orderBook", "trade-processor", new StockShardMsgRouter())
                .WithActors((system, registry) =>
                {
                    var order = registry.Get<OrderBookActor>();
                    foreach (var stock in AvailableTickerSymbols.Symbols)
                    {
                        var max = (decimal)ThreadLocalRandom.Current.Next(20, 45);
                        var min = (decimal)ThreadLocalRandom.Current.Next(10, 15);
                        var range = new PriceRange(min, 0.0m, max);

                        // start bidders                       
                        foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                        {
                            system.ActorOf(Props.Create(() => new BidderActor(stock, range, order)));
                        }
                        // start askers                       
                        foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                        {
                            system.ActorOf(Props.Create(() => new AskerActor(stock, range, order)));
                        }
                    }
                })*/;
        return builder;
    }
}
