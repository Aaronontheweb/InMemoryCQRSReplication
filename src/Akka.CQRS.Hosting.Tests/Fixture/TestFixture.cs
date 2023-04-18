using DotNet.Testcontainers.Networks;
using Testcontainers.MsSql;

namespace Akka.CQRS.Hosting.Tests.Fixture
{
    public class TestFixture : IAsyncLifetime
    {
        const string storage = "sql";
        public readonly string ConnectionString = "Server=sql,1633;User Id=sa;Password=This!IsOpenSource1;TrustServerCertificate=true";//$"server={storage},1633;user id={MsSqlBuilder.DefaultUsername};password={MsSqlBuilder.DefaultPassword};database={MsSqlBuilder.DefaultDatabase}";
        private readonly INetwork _network;
        private readonly MsSqlContainer _msSqlContainer;
        private IContainer _lighthouse = null!;

        private IContainer _dockerContainer = null!;
        public TestFixture()
        {
            _network = new NetworkBuilder()
            .Build();

            _msSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                .WithNetwork(_network)
                .WithPassword("This!IsOpenSource1")
                .WithNetworkAliases(storage)
                .WithPortBinding(1633, 1433)
                .WithName("sql")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .Build();

            #region lighthouse
            _lighthouse = new ContainerBuilder()
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

            
            #endregion
        }
        public async Task DisposeAsync()
        {

            await _msSqlContainer.StopAsync()
              .ConfigureAwait(false);

            await _network.DeleteAsync()
            .ConfigureAwait(false);

            await _lighthouse.StopAsync();
        }

        public async Task InitializeAsync()
        {
            await _lighthouse.StartAsync(); 

            await _network.CreateAsync()
            .ConfigureAwait(false);

            await _msSqlContainer.StartAsync()
              .ConfigureAwait(false);
            
            //await Task.Delay(1000);
        }
    }
}
