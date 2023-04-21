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
using Akka.Hosting;
using Akka.Util;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using Microsoft.Extensions.Logging;

namespace Akka.CQRS.TradePlacers.Service
{
    class Program
    {
        static int Main(string[] args)
        {
            var config = File.ReadAllText("app.conf");
            using var host = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {

                services.AddAkka("AkkaTrader", options =>
                {
                    // Add HOCON configuration from Docker
                    var conf = ConfigurationFactory.ParseString(config)
                    .WithFallback(OpsConfig.GetOpsConfig())
                    .WithFallback(ClusterSharding.DefaultConfig())
                    .WithFallback(DistributedPubSub.DefaultConfig());
                    options.AddHocon(conf.BootstrapFromDocker(), HoconAddMode.Prepend)
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
                    .AddPetabridgeCmd(cmd =>
                    {
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
            host.Run();
            Console.ReadLine();
            return 0;
        }
    }
}
