using System;
using System.IO;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Akka.CQRS.TradeProcessor.Actors;
using static Akka.CQRS.Util.MongoDbHoconHelper;

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

            var conf = ConfigurationFactory.ParseString(File.ReadAllText("app.conf")).BootstrapFromDocker()
                .WithFallback(GetMongoHocon(mongoConnectionString));

            var actorSystem = ActorSystem.Create("AkkaTrader", conf);
            var sharding = ClusterSharding.Get(actorSystem);

            sharding.Start("orderBook", s => OrderBookActor.)
        }
    }
}
