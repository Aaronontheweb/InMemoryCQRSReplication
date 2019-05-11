using System;
using System.IO;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.TradeProcessor.Actors;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using static Akka.CQRS.Infrastructure.MongoDbHoconHelper;

namespace Akka.CQRS.TradeProcessor.Service
{
    class Program
    {
        static int Main(string[] args)
        {
            var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STR")?.Trim();
            if (string.IsNullOrEmpty(mongoConnectionString))
            {
                Console.WriteLine("ERROR! MongoDb connection string not provided. Can't start.");
                return -1;
            }
            else
            {
                Console.WriteLine("Connecting to MongoDb at {0}", mongoConnectionString);
            }

            var config = File.ReadAllText("app.conf");
            var conf = ConfigurationFactory.ParseString(config).WithFallback(GetMongoHocon(mongoConnectionString))
                .WithFallback(ClusterSharding.DefaultConfig());

            var actorSystem = ActorSystem.Create("AkkaTrader", conf.BootstrapFromDocker());

            Cluster.Cluster.Get(actorSystem).RegisterOnMemberUp(() =>
            {
                var sharding = ClusterSharding.Get(actorSystem);

                var shardRegion = sharding.Start("orderBook", s => OrderBookActor.PropsFor(s), ClusterShardingSettings.Create(actorSystem),
                    new StockShardMsgRouter());
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
