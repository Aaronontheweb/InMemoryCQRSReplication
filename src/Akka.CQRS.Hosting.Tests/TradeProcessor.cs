

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
using Akka.Event;
using Akka.TestKit.Xunit2.Internals;

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
            builder
                .ConfigureTradeProcessor(_sqlConnectionString)
                .WithActors((system, registry) =>
                {
                    var extSystem = (ExtendedActorSystem)system;
                    var logger = extSystem.SystemActorOf(Props.Create(() => new TestOutputLogger(_output)));
                    logger.Tell(new InitializeLogger(system.EventStream));
                });
        }

        [Fact]
        public void Trade_Processor_Test()
        {
            
            // arrange
            var orderBook = ActorRegistry.Get<OrderBookActor>();
            var shardRegion = ActorRegistry.Get<MatchAggregator>();
            var priceViewMaster = ActorRegistry.Get<PriceViewMaster>();

            // act

            // assert
            orderBook.Should().NotBeNull();
            shardRegion.Should().NotBeNull();
            priceViewMaster.Should().NotBeNull();
            
        }

    }
}
