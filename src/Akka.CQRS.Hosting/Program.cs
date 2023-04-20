
using Akka.CQRS.Hosting;
using Akka.Hosting;
using Microsoft.Extensions.Hosting;

public class Program
{
    static string _sqlConnectionString = "Server=sql,1633;User Id=sa;Password=This!IsOpenSource1;TrustServerCertificate=true";

    static string clusterSystem = "AkkaTrader";
    public static void Main(params string[] args)
    {
        var builder = new HostBuilder();
        builder.ConfigureServices((context, service) =>
        {
            service.AddAkka(clusterSystem, options =>
            {
                options                    
                .ConfigureTradeProcessor(_sqlConnectionString);
            });
        });

        builder.Build().Run();

        Console.ReadLine();
    }
}

