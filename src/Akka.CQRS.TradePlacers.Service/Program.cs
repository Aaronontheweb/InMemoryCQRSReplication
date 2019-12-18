using System;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.Infrastructure.Ops;
using Akka.CQRS.TradeProcessor.Actors;
using Akka.Util;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;

namespace Akka.CQRS.TradePlacers.Service
{
    class Program
    {
        static int Main(string[] args)
        {
            var config = File.ReadAllText("app.conf");
            var conf = ConfigurationFactory.ParseString(config)
                .WithFallback(OpsConfig.GetOpsConfig())
                .WithFallback(ClusterSharding.DefaultConfig())
                .WithFallback(DistributedPubSub.DefaultConfig());

            var actorSystem = ActorSystem.Create("AkkaTrader", conf.BootstrapFromDocker());

            Cluster.Cluster.Get(actorSystem).RegisterOnMemberUp(() =>
            {
                var sharding = ClusterSharding.Get(actorSystem);

                var shardRegionProxy = sharding.StartProxy("orderBook", "trade-processor", new StockShardMsgRouter());
                foreach (var stock in AvailableTickerSymbols.Symbols)
                {
                    var max = (decimal)ThreadLocalRandom.Current.Next(20, 45);
                    var min = (decimal) ThreadLocalRandom.Current.Next(10, 15);
                    var range = new PriceRange(min, 0.0m, max);

                    // start bidders
                    foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                    {
                        actorSystem.ActorOf(Props.Create(() => new BidderActor(stock, range, shardRegionProxy)));
                    }

                    // start askers
                    foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                    {
                        actorSystem.ActorOf(Props.Create(() => new AskerActor(stock, range, shardRegionProxy)));
                    }
                }
            });

            // start Petabridge.Cmd (for external monitoring / supervision)
            var pbm = PetabridgeCmd.Get(actorSystem);
            pbm.RegisterCommandPalette(ClusterCommands.Instance);
            pbm.RegisterCommandPalette(ClusterShardingCommands.Instance);
            pbm.RegisterCommandPalette(RemoteCommands.Instance);
            pbm.Start();

            actorSystem.WhenTerminated.Wait();
            return 0;
        }
    }
}
