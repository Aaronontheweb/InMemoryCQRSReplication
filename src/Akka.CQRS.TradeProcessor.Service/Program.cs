using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Bootstrap.Docker;

using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.Infrastructure.Ops;
using Akka.CQRS.TradeProcessor.Actors;
using Akka.Hosting;
using Akka.Persistence.Sql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            using var host = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                
                services.AddAkka("AkkaTrader", options =>
                {
                    // Add HOCON configuration from Docker
                    var conf = ConfigurationFactory.ParseString(config)
                        .WithFallback(GetSqlHocon(sqlConnectionString, sqlProviderName))
                        .WithFallback(OpsConfig.GetOpsConfig())
                        .WithFallback(ClusterSharding.DefaultConfig())
                        .WithFallback(DistributedPubSub.DefaultConfig())
                        .WithFallback(SqlPersistence.DefaultConfiguration);
                    options.AddHocon(conf.BootstrapFromDocker(), HoconAddMode.Prepend)                    
                    .WithShardRegion<OrderBookActor>("orderBook", s => OrderBookActor.PropsFor(s),
                        new StockShardMsgRouter(),
                        new ShardOptions() {  })
                    .AddPetabridgeCmd(cmd =>
                    {
                        Console.WriteLine("   PetabridgeCmd Added");
                        cmd.RegisterCommandPalette(ClusterCommands.Instance);
                        cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                        cmd.RegisterCommandPalette(new RemoteCommands());
                        cmd.Start();
                    });
                    
                });
            })
            .ConfigureLogging((hostContext, configLogging) =>
            {
                configLogging.AddConsole();
            })
            .UseConsoleLifetime()
            .Build();
            await host.RunAsync();
            Console.ReadLine();
            return 0;
        }
    }
}
