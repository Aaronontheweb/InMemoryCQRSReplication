

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Configuration;
using Akka.Hosting;
using Akka.CQRS.Infrastructure;
using Akka.CQRS.TradeProcessor.Actors;
using Akka.Util;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;
using System.Net;
using FluentAssertions;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using Akka.CQRS.Pricing.Actors;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Microsoft.Win32;
using static LinqToDB.Reflection.Methods;

namespace Akka.CQRS.Hosting.Tests
{
    public class TradeProcessor : Akka.Hosting.TestKit.TestKit
    {
        private readonly string _sqlConnectionString = "Server=sql,1633;User Id=sa;Password=This!IsOpenSource1;TrustServerCertificate=true";
        private readonly string _sqlProviderName = "SqlServer.2019";
        private string _configTradeProcessor = ConfigurationFactory.ParseString(@"
            akka {{
	actor {{
		provider = cluster
	}}
						
	remote {{
		dot-netty.tcp {{
            hostname = ""127.0.0.1""
            port = 5055
        }}
	}}			

	cluster {{
		#will inject this node as a self-seed node at run-time
		seed-nodes = [""akka.tcp://AkkaTrader@127.0.0.1:5055""] 
		roles = [""trade-processor"" , ""trade-events""]

		pub-sub{{
			role = ""trade-events""
		}}

		sharding{{
			role = ""trade-processor""
		}}
	}}

	persistence{{
		journal {{
		    plugin = ""akka.persistence.journal.sql""
		    sql {{
                event-adapters {{
                    stock-tagger = ""Akka.CQRS.Infrastructure.StockEventTagger, Akka.CQRS.Infrastructure""
                }}
                event-adapter-bindings {{
                    ""Akka.CQRS.IWithStockId, Akka.CQRS"" = stock-tagger
                }}
		    }}
		}}

		snapshot-store {{
		    plugin = ""akka.persistence.snapshot-store.sql""
		}}
	}}
}}").ToString();
        private string _configTradePlacers = ConfigurationFactory.ParseString(@"
            akka {{
	actor {{
		provider = cluster
	}}

	remote {{
		dot-netty.tcp {{
            hostname = ""127.0.0.1""
            port = 5054
        }}
	}}			

	cluster {{
		#will inject this node as a self-seed node at run-time
		seed-nodes = [""akka.tcp://AkkaTrader@127.0.0.1:5055""] 
		roles = [""trader"", ""trade-events""]

		pub-sub{{
			role = ""trade-events""
		}}

		sharding{{
			role = ""trade-processor""
		}}
	}}
}}").ToString();
        private string _configPricing = ConfigurationFactory.ParseString(@"
           akka {{
	actor {{
		provider = cluster
	}}
						
	remote {{
		dot-netty.tcp {{
            hostname = ""127.0.0.1""
            port = 6055
        }}
	}}			

	cluster {{
		#will inject this node as a self-seed node at run-time
		seed-nodes = [""akka.tcp://AkkaPricing@127.0.0.1:6055""] 
		roles = [""pricing-engine"" , ""price-events""]

		pub-sub {{
			role = ""price-events""
		}}

		sharding {{
			role = ""pricing-engine""
		}}

		price-singleton {{
			singleton-name = ""price-initiator""
			role = ""pricing-engine""
			hand-over-retry-interval = 1s
			min-number-of-hand-over-retries = 10
		}}
	}}

	persistence {{
		journal {{
		    plugin = ""akka.persistence.journal.sql""
		    sql {{
                event-adapters = {{
                    stock-tagger = ""Akka.CQRS.Infrastructure.StockEventTagger, Akka.CQRS.Infrastructure""
                }}
                event-adapter-bindings = {{
                    ""Akka.CQRS.IWithStockId, Akka.CQRS"" = stock-tagger
                }}
		    }}
		}}

		snapshot-store {{
		    plugin = ""akka.persistence.snapshot-store.sql""
		}}
	}}
}}").ToString();

        private readonly ITestOutputHelper _output;
        public TradeProcessor(ITestOutputHelper output)
        {
            _output = output;
        }
        protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        {
            /*builder
                .AddHocon(_configTradeProcessor, HoconAddMode.Prepend)
                .WithRemoting(Dns.GetHostName(), 8110)
                .WithClustering()
                .WithShardRegion<OrderBookActor>("orderBook",
                (system, registry, resolver) => s => OrderBookActor.PropsFor(s),
                new StockShardMsgRouter(), new ShardOptions(){ })
                .WithSqlServerPersistence(_sqlConnectionString)
                
                 .AddPetabridgeCmd(cmd =>
                 {
                     _output.WriteLine("   PetabridgeCmd Added");
                     cmd.RegisterCommandPalette(ClusterCommands.Instance);
                     cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                     cmd.RegisterCommandPalette(new RemoteCommands());
                     cmd.Start();
                 })*/
            ;
            builder
                .AddHocon(_configPricing, HoconAddMode.Prepend)
                .WithClustering()
                .WithShardRegion<MatchAggregator>("priceAggregator",
                 (system, registry, resolver) => s => Props.Create(() => new MatchAggregator(s, system.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier))),
                 new StockShardMsgRouter(), new ShardOptions() { })
                .WithSingleton<PriceInitiatorActor>("price",
                     (_, _, resolver) => resolver.Props<PriceInitiatorActor>(),
                     new ClusterSingletonOptions() { })
                .WithActors((system, registry) =>
                {
                    //var priceViewMaster = Sys.ActorOf(Props.Create(() => new PriceViewMaster()), "prices");
                    //registry.Register<PriceViewMaster>(priceViewMaster);

                });
        }
        
        [Fact]
        public void Trade_Processor_Test()
        {
            // arrange
            var orderBook = ActorRegistry.Get<OrderBookActor>();
           
            // act

            // assert
            orderBook.Should().NotBeNull();
            //id.Should().Be("foo");
            //sourceRef.Should().Be(actorRegistry.Get<OrderBookActor>());
        }

        [Fact]
        public async Task Trade_Places_Test()
        {
            // arrange
            
            using var host1 = await TestHelper.CreateHost(builder =>
            {
                builder
               .AddHocon(_configTradePlacers, HoconAddMode.Prepend)
               .WithClustering()
               .WithShardRegionProxy<OrderBookActor>("orderBook", "trade-processor", new StockShardMsgRouter())
               .WithActors((system, registry) =>
               {
                   var order = registry.Get<OrderBookActor>();
                   foreach (var stock in AvailableTickerSymbols.Symbols)
                   {
                       var max = (decimal)ThreadLocalRandom.Current.Next(20, 45);
                       var min = (decimal)ThreadLocalRandom.Current.Next(10, 15);
                       var range = new PriceRange(min, 0.0m, max);

                       // start bidders
                       foreach (var i in System.Linq.Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                       {
                           system.ActorOf(Props.Create(() => new BidderActor(stock, range, order)));
                       }

                       // start askers
                       foreach (var i in System.Linq.Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 2)))
                       {
                           system.ActorOf(Props.Create(() => new AskerActor(stock, range, order)));
                       }
                   }
               });
                /*.AddPetabridgeCmd(cmd =>
                 {
                     cmd.RegisterCommandPalette(ClusterCommands.Instance);
                     cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                     cmd.RegisterCommandPalette(new RemoteCommands());
                     cmd.Start();
                 })*/
                ;
            }, new ClusterOptions() { /*SeedNodes = new[] { "akka.tcp://AkkaTrader@lighthouse:4053" } */}, _output);
            
            
            // act
            //var id = await shardRegion.Ask<string>(new OrderBookActor.GetId("foo"), TimeSpan.FromSeconds(3));
            // sourceRef = 
            // await shardRegion.Ask<IActorRef>(new OrderBookActor.GetSourceRef("foo"), TimeSpan.FromSeconds(3));

            // assert
            //id.Should().Be("foo");
            //sourceRef.Should().Be(actorRegistry.Get<OrderBookActor>());
        }

        [Fact]
        public async Task Trade_Price_Test()
        {
            // arrange

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
        }
    }
}
