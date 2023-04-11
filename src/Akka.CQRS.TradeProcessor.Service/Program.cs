using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.Infrastructure.Ops;
using Akka.CQRS.TradeProcessor.Actors;
using Akka.Persistence.Sql;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using static Akka.CQRS.Infrastructure.SqlDbHoconHelper;

namespace Akka.CQRS.TradeProcessor.Service
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
                .WithFallback(SqlPersistence.DefaultConfiguration);;

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
            pbm.RegisterCommandPalette(new RemoteCommands());
            pbm.Start();

            actorSystem.WhenTerminated.Wait();
            return 0;
        }
    }
}
