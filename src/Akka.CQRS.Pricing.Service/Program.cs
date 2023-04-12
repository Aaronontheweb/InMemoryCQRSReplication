using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using Akka.Persistence.Query;
using Akka.Persistence.Sql;
using Akka.Persistence.Sql.Query;
using Akka.Util;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using static Akka.CQRS.Infrastructure.SqlDbHoconHelper;

namespace Akka.CQRS.Pricing.Service
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var sqlConnectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STR")?.Trim();
            if (string.IsNullOrEmpty(sqlConnectionString))
            {
                Console.WriteLine("ERROR! SQL connection string not provided. Can't start.");
                return -1;
            }
            Console.WriteLine($"Connecting to SQL server at {sqlConnectionString}");

            var sqlProviderName = Environment.GetEnvironmentVariable("SQL_PROVIDER_NAME")?.Trim();
            if (string.IsNullOrEmpty(sqlProviderName))
            {
                Console.WriteLine("ERROR! SQL provider name not provided. Can't start.");
                return -1;
            }
            Console.WriteLine($"Connecting to SQL provider {sqlProviderName}");

            // Need to wait for the SQL server to spin up
            await Task.Delay(TimeSpan.FromSeconds(15));
            
            var config = await File.ReadAllTextAsync("app.conf");
            var conf = ConfigurationFactory.ParseString(config)
                .WithFallback(GetSqlHocon(sqlConnectionString, sqlProviderName))
                .WithFallback(OpsConfig.GetOpsConfig())
                .WithFallback(ClusterSharding.DefaultConfig())
                .WithFallback(DistributedPubSub.DefaultConfig())
                .WithFallback(SqlPersistence.DefaultConfiguration);

            var actorSystem = ActorSystem.Create("AkkaPricing", conf.BootstrapFromDocker());
            var readJournal = actorSystem.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
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
