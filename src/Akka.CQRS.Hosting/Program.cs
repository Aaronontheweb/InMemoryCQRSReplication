
using Akka.CQRS.Hosting;
using Akka.Hosting;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using Testcontainers.MsSql;

public class Program
{
    static string _sqlConnectionString = "Server=127.0.0.1,1633;User Id=sa;Password=yourStrong(!)Password;";

    static string clusterSystem = "AkkaTrader";
    public static async Task Main(params string[] args)
    {
        #region Docker
        var t = await Test();
        OpenSqlConnection();
        #endregion
        var builder = new HostBuilder();
        builder.ConfigureServices((context, service) =>
        {
            service.AddAkka(clusterSystem, options =>
            {
                options                    
                .ConfigureTradeProcessor(_sqlConnectionString)
                .AddPetabridgeCmd(cmd =>
                {
                    Console.WriteLine("   PetabridgeCmd Added");
                    cmd.RegisterCommandPalette(ClusterCommands.Instance);
                    cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    cmd.RegisterCommandPalette(new RemoteCommands());
                    cmd.Start();
                }); 
            });
        });

        builder.Build().Run();
        await TestDispose(t.ms, t.network, t.lighthouse);
        Console.ReadLine();
    }
    private static async Task<(MsSqlContainer ms, INetwork network, IContainer lighthouse)> Test()
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


        await network.CreateAsync()
        .ConfigureAwait(false);

        await msSqlContainer.StartAsync()
          .ConfigureAwait(false);


        await lighthouse.StartAsync();

        return (msSqlContainer, network, lighthouse);

    }
    private static async Task TestDispose(MsSqlContainer msSqlContainer, INetwork network, IContainer lighthouse)
    {

        await msSqlContainer.StopAsync()
          .ConfigureAwait(false);

        await network.DeleteAsync()
        .ConfigureAwait(false);

        await lighthouse.StopAsync();
    }
    private static void OpenSqlConnection()
    {
        string connectionString = GetConnectionString();

        using (SqlConnection connection = new SqlConnection())
        {
            connection.ConnectionString = connectionString;

            connection.Open();

            Console.WriteLine("State: {0}", connection.State);
            Console.WriteLine("ConnectionString: {0}",
                connection.ConnectionString);
        }
    }

    static private string GetConnectionString()
    {
        // To avoid storing the connection string in your code,
        // you can retrieve it from a configuration file.
        return _sqlConnectionString;
    }
}

