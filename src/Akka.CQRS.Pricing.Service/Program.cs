using System;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.Infrastructure.Ops;
using Akka.CQRS.Pricing.Actors;
using Akka.CQRS.Pricing.Cli;
using Akka.Persistence.MongoDb.Query;
using Akka.Persistence.Query;
using Akka.Util;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using static Akka.CQRS.Infrastructure.MongoDbHoconHelper;

namespace Akka.CQRS.Pricing.Service
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
                .WithFallback(OpsConfig.GetOpsConfig())
                .WithFallback(ClusterSharding.DefaultConfig())
                .WithFallback(DistributedPubSub.DefaultConfig());

            var actorSystem = ActorSystem.Create("AkkaPricing", conf.BootstrapFromDocker());
            var readJournal = actorSystem.ReadJournalFor<MongoDbReadJournal>(MongoDbReadJournal.Identifier);
            var priceViewMaster = actorSystem.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");

            Cluster.Cluster.Get(actorSystem).RegisterOnMemberUp(() =>
            {
            var sharding = ClusterSharding.Get(actorSystem);

            var shardRegion = sharding.Start("priceAggregator",
                s => Props.Create(() => new MatchAggregator(s, readJournal)),
                ClusterShardingSettings.Create(actorSystem),
                new StockShardMsgRouter());

            // used to seed pricing data
            var singleton = ClusterSingletonManager.Props(
                Props.Create(() => new PriceInitiatorActor(readJournal, shardRegion)),
                ClusterSingletonManagerSettings.Create(
                    actorSystem.Settings.Config.GetConfig("akka.cluster.price-singleton")));

                // start the creation of the pricing views
                priceViewMaster.Tell(new PriceViewMaster.BeginTrackPrices(shardRegion));
            });

            // start Petabridge.Cmd (for external monitoring / supervision)
            var pbm = PetabridgeCmd.Get(actorSystem);
            void RegisterPalette(CommandPaletteHandler h)
            {
                if (pbm.RegisterCommandPalette(h))
                {
                    Console.WriteLine("Petabridge.Cmd - Registered {0}", h.Palette.ModuleName);
                }
                else
                {
                    Console.WriteLine("Petabridge.Cmd - DID NOT REGISTER {0}", h.Palette.ModuleName);
                }
            }


            RegisterPalette(ClusterCommands.Instance);
            RegisterPalette(new RemoteCommands());
            RegisterPalette(ClusterShardingCommands.Instance);
            RegisterPalette(new PriceCommands(priceViewMaster));
            pbm.Start();

            actorSystem.WhenTerminated.Wait();
            return 0;
        }
    }
}
