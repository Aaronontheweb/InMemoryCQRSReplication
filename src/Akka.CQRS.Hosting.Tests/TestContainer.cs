

namespace Akka.CQRS.Hosting.Tests
{
    internal class TestContainer : IAsyncLifetime
    {
        private readonly string _temporarySqlServerPassword = Guid.NewGuid().ToString();

        private IContainer _dockerContainer = null!;
        public async Task DisposeAsync()
        {
            await _dockerContainer.StopAsync();
        }

        public async Task InitializeAsync()
        {
            _dockerContainer = new ContainerBuilder()
           .WithName(Guid.NewGuid().ToString("D"))
           .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
           .WithPortBinding(1433, 1433)
           .WithEnvironment(new Dictionary<string, string>()
           {
                { "ACCEPT_EULA", "Y" },
                { "MSSQL_SA_PASSWORD", _temporarySqlServerPassword }
           })
           .Build();

            await _dockerContainer.StartAsync();
        }
    }
}
