using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.CQRS.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;

public class Program
{
    static string _sqlConnectionString = "Data Source=DESKTOP-GUGBB29\\\\SQLEXPRESS;Initial Catalog=Akka;User Id=sa;password=thegospel;MultipleActiveResultSets=True";

    static string clusterSystem = "AkkaTrader";
    public static void Main(params string[] args)
    {
        var builder = new HostBuilder();
        builder.ConfigureServices((context, service) =>
        {
            service.AddAkka(clusterSystem, options =>
            {
                options                    
                .ConfigureTradeProcessor(_sqlConnectionString)                
                //.ConfigurePrices()
                //.ConfigureTradeProcessorProxy()
                .AddPetabridgeCmd(cmd =>
                {
                    cmd.RegisterCommandPalette(ClusterCommands.Instance);
                    cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    cmd.RegisterCommandPalette(new RemoteCommands());
                    cmd.Start();
                });
            });
        });

        builder.Build().Run();

        Console.ReadLine();
    }
}

