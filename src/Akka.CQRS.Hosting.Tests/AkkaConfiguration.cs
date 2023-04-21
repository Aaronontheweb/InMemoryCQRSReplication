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
namespace Akka.CQRS.Tests.Hosting;

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
                    SeedNodes = new[] { "akka.tcp://test@127.0.0.1:5055" },
                    Roles = new[] { "trade-processor", "trade-events" }
                })
                .WithDistributedPubSub("trade-events")
                .WithShardRegion<OrderBookActor>("orderBook",
                (system, registry, resolver) => s => OrderBookActor.PropsFor(s),
                new StockShardMsgRouter(), new ShardOptions()
                {
                    Role = "trade-processor"
                })
                .WithShardRegion<MatchAggregator>("priceAggregator",
                 (system, registry, resolver) => s => Props.Create(() => new MatchAggregator(s, system.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier))),
                 new StockShardMsgRouter(), new ShardOptions() { Role = "trade-events" })
                .WithSingleton<PriceInitiatorActor>("price-initiator",
                  (_, _, resolver) => resolver.Props<PriceInitiatorActor>(),
                new ClusterSingletonOptions() { Role = "trade-events", LeaseRetryInterval = TimeSpan.FromSeconds(1)})

                .WithSqlServerPersistence(connectionString, journalBuilder: builder =>
                {
                    builder.AddWriteEventAdapter<StockEventTagger>("stock-tagger", new[] { typeof(IWithStockId) });
                })
                .AddHocon(@$"akka.persistence.journal.sql.provider-name = SqlServer.2019", HoconAddMode.Prepend)
                .WithActors((system, registry) =>
                {
                    var priceViewMaster = system.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");
                    var shardRegion = registry.Get<MatchAggregator>();
                    registry.Register<PriceViewMaster>(priceViewMaster);
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
                        // start the creation of the pricing views
                        priceViewMaster.Tell(new PriceViewMaster.BeginTrackPrices(shardRegion));
                    });
                });
        return builder;
    }
}
