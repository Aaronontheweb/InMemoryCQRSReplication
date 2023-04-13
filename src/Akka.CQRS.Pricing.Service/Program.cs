using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.Infrastructure.Ops;
using Akka.CQRS.Pricing.Actors;
using Akka.CQRS.Pricing.Cli;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Query;
using Akka.Persistence.Sql;
using Akka.Persistence.Sql.Query;
using Akka.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            using var host = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {

                services.AddAkka("AkkaPricing", options =>
                {
                    // Add HOCON configuration from Docker
                    var conf = ConfigurationFactory.ParseString(config)                 
                    .WithFallback(GetSqlHocon(sqlConnectionString, sqlProviderName))                
                    .WithFallback(OpsConfig.GetOpsConfig())                 
                    .WithFallback(ClusterSharding.DefaultConfig())                 
                    .WithFallback(DistributedPubSub.DefaultConfig())                 
                    .WithFallback(SqlPersistence.DefaultConfiguration);
                    options.AddHocon(conf.BootstrapFromDocker(), HoconAddMode.Prepend)
                    
                    .WithShardRegion<MatchAggregator>("priceAggregator", (system, registry) =>
                        {
                            var readJournal = system.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
                            return s => Props.Create(() => new MatchAggregator(s, readJournal));
                        },
                        new StockShardMsgRouter(),
                        new ShardOptions() {  }
                    )
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
                    })
                    .WithActors((system, registry) =>
                    {
                        var priceViewMaster = system.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");
                        registry.Register<PriceViewMaster>(priceViewMaster);
                        // used to seed pricing data
                        var readJournal = system.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
                        var actorRegistry = ActorRegistry.For(system);
                        var shardRegion = actorRegistry.Get<MatchAggregator>();
                        var singleton = ClusterSingletonManager.Props(
                            Props.Create(() => new PriceInitiatorActor(readJournal, shardRegion)),
                            ClusterSingletonManagerSettings.Create(
                                system.Settings.Config.GetConfig("akka.cluster.price-singleton")));

                        // start the creation of the pricing views
                        priceViewMaster.Tell(new PriceViewMaster.BeginTrackPrices(shardRegion));

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
            return 0;
        }
    }
}
