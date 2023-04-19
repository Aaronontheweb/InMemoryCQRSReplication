

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.TradeProcessor.Actors;
using Akka.Util;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;
using FluentAssertions;
using Akka.CQRS.Pricing.Actors;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Remote;
using Petabridge.Cmd.Cluster;
using Akka.CQRS.Pricing.Cli;
using Microsoft.Extensions.Hosting;
using Akka.Cluster.Tools.PublishSubscribe;
using System.Data;

namespace Akka.CQRS.Hosting.Tests
{
    public class TradeProcessor : Akka.Hosting.TestKit.TestKit
    {
        private readonly string _sqlConnectionString = "Server=sql,1633;User Id=sa;Password=This!IsOpenSource1;TrustServerCertificate=true";
       
       
        private readonly ITestOutputHelper _output;
        public TradeProcessor(ITestOutputHelper output)
        {
            _output = output;
        }
       
        protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        {
            //builder
                //.ConfigureTradeProcessor(_sqlConnectionString)
                //.ConfigureTradeProcessorProxy()
                //.ConfigurePrices()
                //.WithActors((system, registry) =>
                //{
               //     var priceViewMaster = system.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");
               //     registry.Register<PriceViewMaster>(priceViewMaster);

                //})
              //  ;
        }

        [Fact]
        public async Task Trade_Processor_Test()
        {
            using var host = await TestHelper.CreateHost(builder =>
            {
                builder
                .ConfigureTradeProcessor(_sqlConnectionString)
                .ConfigureTradeProcessorProxy()
                .ConfigurePrices()
                .AddPetabridgeCmd(cmd =>
                {
                    cmd.RegisterCommandPalette(ClusterCommands.Instance);
                    cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    cmd.RegisterCommandPalette(new RemoteCommands());
                    cmd.Start();
                });

            }, new ClusterOptions() { /*SeedNodes = new[] { "akka.tcp://AkkaTrader@lighthouse:4053" } */}, _output);
            // arrange
            var orderBook = ActorRegistry.Get<OrderBookActor>();
            var shardRegion = ActorRegistry.Get<MatchAggregator>();
            var priceViewMaster = Sys.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");
            //registry.Register<PriceViewMaster>(priceViewMaster);
            //var priceViewMaster = ActorRegistry.Get<PriceViewMaster>();
            // start the creation of the pricing views
            priceViewMaster.Tell(new PriceViewMaster.BeginTrackPrices(shardRegion));
            // act
            //var id = await shardRegion.Ask<string>(new OrderBookActor.GetId("foo"), TimeSpan.FromSeconds(3));
            // sourceRef = 
            // await shardRegion.Ask<IActorRef>(new OrderBookActor.GetSourceRef("foo"), TimeSpan.FromSeconds(3));

            // assert
            //id.Should().Be("foo");
            //sourceRef.Should().Be(actorRegistry.Get<OrderBookActor>());
            // act

            // assert
            orderBook.Should().NotBeNull();
            //id.Should().Be("foo");
            //sourceRef.Should().Be(actorRegistry.Get<OrderBookActor>());
        }

    }
}
