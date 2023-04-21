

using Akka.Actor;
using Akka.Hosting;
using Akka.CQRS.TradeProcessor.Actors;
using FluentAssertions;
using Akka.CQRS.Pricing.Actors;
using Akka.Event;
using Akka.TestKit.Xunit2.Internals;
using DotNet.Testcontainers.Networks;
using Testcontainers.MsSql;

namespace Akka.CQRS.Tests.Hosting
{
    public class TradeProcessor : Akka.Hosting.TestKit.TestKit
    {
        private readonly string _sqlConnectionString = "Server=127.0.0.1,1633;User Id=sa;Password=yourStrong(!)Password;";
        #region Docker
        #endregion
        private (MsSqlContainer ms, INetwork network, IContainer lighthouse) _t;
        private readonly ITestOutputHelper _output;
        public TradeProcessor(ITestOutputHelper output)
        {
            #region Docker
            _t = Test().GetAwaiter().GetResult();
            #endregion
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
        public async Task Trade_Processor_Test()
        {   

            // arrange
            var orderBook = ActorRegistry.Get<OrderBookActor>();
            var shardRegion = ActorRegistry.Get<MatchAggregator>();
            var priceViewMaster = ActorRegistry.Get<PriceViewMaster>();

            // act
            //var wdc = await priceViewMaster.Ask<PriceHistory>(new GetPriceHistory("WDC"), TimeSpan.FromSeconds(30));
            //var w = wdc;

            // assert
            orderBook.Should().NotBeNull();
            shardRegion.Should().NotBeNull();
            priceViewMaster.Should().NotBeNull();
            await Task.Delay(10000);
            await TestDispose(_t.ms, _t.network, _t.lighthouse);
        }

        private async Task<(MsSqlContainer ms, INetwork network, IContainer lighthouse)> Test()
        {
            
            var network = new NetworkBuilder()
            .Build();

            var msSqlContainer = new MsSqlBuilder()
                //.WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithNetwork(network)
                //.WithPassword("This!IsOpenSource1")
                .WithNetworkAliases("sql")
                .WithPortBinding(1633, 1433)
                .WithName("sql")
                //.WithEnvironment("ACCEPT_EULA", "Y")
                //.WithEnvironment("MSSQL_SA_PASSWORD", "This!IsOpenSource1")
                //.WithEnvironment("MSSQL_PID", "Developer")
                //.WithEnvironment("MSSQL_TCP_PORT", "1234")      
                .Build();

            
            var lighthouse = new ContainerBuilder()
           .WithName("lighthouse")
           .WithImage("petabridge/lighthouse:latest")
           .WithPortBinding(9110, 9110)
           .WithPortBinding(4053, 4053)
           .WithEnvironment(new Dictionary<string, string>()
           {
                { "ACTORSYSTEM", "AkkaTrader" },
                { "CLUSTER_PORT", "4053"},
                { "CLUSTER_IP", "lighthouse"},
                { "CLUSTER_SEEDS", "akka.tcp://AkkaTrader@lighthouse:4053"}

           })

           .Build();

            await lighthouse.StartAsync();

            await network.CreateAsync()
            .ConfigureAwait(false);

            await msSqlContainer.StartAsync()
              .ConfigureAwait(false);

            return (msSqlContainer, network, lighthouse);   

        }
        private async Task TestDispose(MsSqlContainer msSqlContainer, INetwork network, IContainer lighthouse)
        {

            await msSqlContainer.StopAsync()
              .ConfigureAwait(false);

            await network.DeleteAsync()
            .ConfigureAwait(false);

            await lighthouse.StopAsync();
        }
    }
}