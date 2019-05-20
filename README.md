# Akka.CQRS
Akka.CQRS is a reference architecture for [Akka.NET](https://getakka.net/), intended to illustrate the following Akka.NET techniques and principles:

1. [Command-Query Responsibility Segregation](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs) - the Akka.NET actors who consume write events use distinctly different interfaces from those who consume read events
2. [Akka.Cluster](https://getakka.net/articles/clustering/cluster-overview.html) - a module that allows Akka.NET developers to create horizontally scalable, peer-to-peer, fault-tolerant, and elastic networks of Akka.NET actors.
3. [Akka.Cluster.Sharding](https://getakka.net/articles/clustering/cluster-sharding.html) - a fault-tolerant, distributed tool for maintaining a single source of truth for all domain entities. 
4. [Akka.Persistence](https://getakka.net/articles/persistence/event-sourcing.html) - a database-agnostic event-sourcing engine Akka.NET actors can use to persist and recover their data, thereby making it possible to move a persistent entity actor from one node in the cluster to another.
5. [Akka.Persistence.Query](https://getakka.net/articles/persistence/persistence-query.html) - a read-side compliment to Akka.Persistence, Akka.Peristence.Query is able to stream events persisted by the `PersistentActor`s into new views and aggregates.
6. [Akka.Cluster.Tools](https://getakka.net/articles/clustering/distributed-publish-subscribe.html) - this sample makes use of `DistributedPubSub` for publishing events across the different nodes in the cluster and `ClusterSingleton`, to ensure that all read-side entities are up and running at all times.
7. [Petabridge.Cmd](https://cmd.petabridge.com/) - a command-line interface for Akka.NET that we use for watching multiple nodes in the cluster all maintain their own eventually consistent, but independent views of the read-side data produced by Akka.Persistence.Query.
8. [Akka.Bootstrap.Docker](https://github.com/petabridge/akkadotnet-bootstrap/tree/dev/src/Akka.Bootstrap.Docker) - this sample uses Docker and `docker-compose` to run the sample, and Akka.Bootstrap.Docker is used to inject runtime environment variables into the Akka.NET HOCON configuration at run-time.

## Goals
The goal of this sample is to show Akka.NET developers how to leverage all of the above components in order to produce an architecture that:

1. Follows the CQRS pattern using Akka.Persistence best practices;
2. Uses in-memory replication of "source of truth" data via publish-subscribe, in order to ensure that each individual read-side node has throughput speeds are not affected by network latency;
3. Uses Akka.Cluster.Sharding to ensure that network partitions and changes in network topology (deployments, scale-down, restarts, etc) don't make "source of truth" data unavailable for any material length of time - the system can continue to serve requests and be available even as shards are moved onto different nodes inside the cluster; and
4. Use Docker to make it easy for developers to replicate this setup on their own machine without expensive setup / configuration steps.

## Domain
Akka.CQRS uses a simple stock trading domain which consists of two major parts, each one residing in their own separate Akka.NET Cluster:

* **Trading Services** - the first cluster serves as the write-side architecture and essentially acts as a stock trading platform, akin to the NYSE or NASDAQ. There's actors who maintain "order book" data for each stock, all of the bids (buy) and asks (sell) that have not yet been fulfilled, and actors who randomly try to place trades within a limited price range. When two opposing trades match (bid >= ask) this produces a new "match" event for that stock which results in a change in the estimated price for that specific stock.
* **Pricing Services** - the second cluster doesn't communicate directly with the first. Rather, it consumes all of the "match" events that are produced via the Trading Services cluster using Akka.Persistence.Query - using that data the Pricing Services aggregate an estimated weighted moving average for both the settlement price and total matched volume (number of shares exchanged between buyer and seller). This pricing data can then be consumed and shared through the use of some application-specific [Petabridge.Cmd palettes developed for this sample](src/Akka.CQRS.Pricing.Cli).

A real-life trading system introduces many more requirements and infrastructure needs than this Akka.CQRS sample does, but for our purposes this is sufficient.

### Events, Commands, and Shared Infrastructure
All of the key shared events, commands, and entity definitions are stored inside the [`src/Akka.CQRS`](src/Akka.CQRS) project and each piece of code is documented with XML-DOC comments.

However, for an explanation of the domain - here are the key events and best practices:

1. All events and commands that are specific to a single stock ticker, i.e. `MSFT`, `FB`, `AAPL`, and so on, are decorated with the [`IWithStockId` interface](src/Akka.CQRS/IWithStockId.cs). __This interface is crucial, and implements an Akka.NET best practice__, because it allows Akka.Cluster.Sharding and other actors within the Akka.CQRS solution to route domain events and commands to the appropriate place without having to create separate `Receive` handlers for each individual event.
2. [`Bid`, `Ask`, `Fill`, and `Match` events](src/Akka.CQRS/Events) represent the concrete domain events that are journaled to Akka.Persistence by the Trading Services - and they're also the events consumed by Akka.Persistence.Query in the Pricing Services in order to produce `IPriceUpdate` read-events and aggregates. They represent the shared context used to power both parts of the Akka.CQRS application.
3. [`Akka.CQRS.Subscriptions`](src/Akka.CQRS.Subscriptions) is a project utilized by both domains to help standardize the `Akka.Cluster.Tools.DistributedPubSub` topic names (which are always strings) and to abstract `DistributedPubSub` away from the domain actors for testing purposes.
4. [`Akka.CQRS.Infrastructure`](src/Akka.CQRS.Infrastructure) is a critical library that provides some very important pieces of raw Akka.NET infrastructure, needed to make Akka.Persistence.Query and Akka.Cluster.Sharding happy:

#### Akka.Persistence.Query and `Tagged` Events
In order to make it easy for us to work with the read-side of the Akka.CQRS application, we choose to [leverage the  Akka.Persistence `IEventAdapter`](https://getakka.net/articles/persistence/event-adapters.html) and `Tagged` feature of Akka.Persistence so we could use [some of the built-in Akka.Persistence.Query queries](https://getakka.net/articles/persistence/persistence-query.html#predefined-queries):

```csharp
/// <summary>
/// Used to tag trade events so they can be consumed inside Akka.Persistence.Query
/// </summary>
public sealed class StockEventTagger : IWriteEventAdapter
{
    public string Manifest(object evt)
    {
        return string.Empty;
    }

    public object ToJournal(object evt)
    {
        switch (evt)
        {
            case Ask ask:
                return new Tagged(evt, ImmutableHashSet<string>.Empty.Add(ask.StockId).Add("Ask"));
            case Bid bid:
                return new Tagged(evt, ImmutableHashSet<string>.Empty.Add(bid.StockId).Add("Bid"));
            case Fill fill:
                return new Tagged(evt, ImmutableHashSet<string>.Empty.Add(fill.StockId).Add("Fill"));
            case Match match:
                return new Tagged(evt, ImmutableHashSet<string>.Empty.Add(match.StockId).Add("Match"));
            default:
                return evt;
        }
    }
}
```

> The `Tagged` class is a built-in type in Akka.Persistence, and it's explicitly intended for use in combination with Akka.Persistence.Query. If you wrap your saved events inside `Tagged`, those events will still be replayed as their underlying types inside your persistent actors. For instance, a `Match` event saved inside a `Tagged` class will still be replayed as a `Match` event inside the `Recover` methods in your `PersistentActor`s.

The `IStockEventTagger` is used by the Trading Services at the time when an `IWithStockId` event is persisted to automatically apply a relevant Akka.Persistence "tag" to that event, in this case we tag both the ticker symbol ("MSFT", "AMD", "FB", etc) and the type of event ("bid", "match", "ask", or "fill".)

We configure this `IWriteEventAdapter` to run automatically behind the scenes in the Trading Services via [Akka.Persistence HOCON](src/Akka.CQRS.TradeProcessor.Service/app.conf#L27-L45) so we don't have to inject this decorator code directly into our persistent actors themselves:

```
akka{
	# cluster, remoting configs...
	persistence{
		journal {
		    plugin = "akka.persistence.journal.mongodb"
			mongodb.class = "Akka.Persistence.MongoDb.Journal.MongoDbJournal, Akka.Persistence.MongoDb"
			mongodb.collection = "EventJournal"
			mongodb.event-adapters = {
				stock-tagger = "Akka.CQRS.Infrastructure.StockEventTagger, Akka.CQRS.Infrastructure"
			}
			mongodb.event-adapter-bindings = {
				"Akka.CQRS.IWithStockId, Akka.CQRS" = stock-tagger
			}
		}

		snapshot-store {
		    plugin = "akka.persistence.snapshot-store.mongodb"
			mongodb.class = "Akka.Persistence.MongoDb.Snapshot.MongoDbSnapshotStore, Akka.Persistence.MongoDb"
			mongodb.collection = "SnapshotStore"
		}
	}
}
```

From there, this data can be consumed using an `IEventsByTagQuery` inside the Pricing Services actors, specifically [the `MatchAggregator` actor](src/Akka.CQRS.Pricing.Actors/MatchAggregator.cs):

```csharp
public class MatchAggregator : ReceivePersistentActor{
	// rest of actor

	/// <summary>
	/// Akka.Persistence.Recovery has completed successfully.
	/// </summary>
	protected override void OnReplaySuccess()
	{
	    var mat = Context.Materializer();
	    var self = Self;
	
	    // transmit all tag events to myself
	    _eventsByTag.EventsByTag(TickerSymbol, Offset.Sequence(QueryOffset))
	        .Where(x => x.Event is Match) // only care about Match events
	        .RunWith(Sink.ActorRef<EventEnvelope>(self, UnexpectedEndOfStream.Instance), mat);
	
	    _publishPricesTask = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(10),
	        TimeSpan.FromSeconds(10), Self, PublishEvents.Instance, ActorRefs.NoSender);
	
	    base.OnReplaySuccess();
	}
}
```

Akka.Persistence.Query depends on Akka.Streams under the covers, but as you can see - the syntax looks pretty similar to what you'd expect from LINQ. In this instance, this `IEventsByTagQuery` will read all events that have been tagged with the matching ticker symbol and will deliver only the events that are of type `Match` to the `MatchAggregator` actor as an `EventEnvelope`, the native message type used by Akka.Persistence.Query when replaying events from elsewhere.

##### Best Practice: Materializing Views with Akka.Persistence.Query
**Also known as: don't replay your entity's history all the way from the beginning every single time.**
 
Without duplicating [all of Akka.Persistence.Query's documentation here](https://getakka.net/articles/persistence/persistence-query.html), the basic fashion in which Akka.Persistence.Query works is that it executes queries against the same event journal that is written to using Akka.Persistence.

Each time Akka.Persistence.Query is started, say when an actor restarts or when an actor is killed on one node and recreated onto another, the query needs to run and replay all of the events that match the query until it catches up to the current state of the system.

The longer the history of your system, the more expensive this process becomes. Therefore, we want to _materialize_ the views and projections we create using Akka.Persistence.Query so we don't have to start from zero every single time we restart a view actor - and we do this by saving:

1. A copy of our projected state;
2. The cursor or "offset" used by Akka.Persistence.Query from when that state we saved - that way we can start replaying events the occurred only _after_ our most recently persisted projection of our state.

In our Pricing Services, we accomplish this inside the [the `MatchAggregator` actor](src/Akka.CQRS.Pricing.Actors/MatchAggregator.cs) via routinely saving a `MatchAggregatorSnapshot` to Akka.Persistence:

```csharp
/// <summary>
/// Represents the point-in-time state of the match aggregator at any given time.
/// </summary>
public sealed class MatchAggregatorSnapshot
{
    public MatchAggregatorSnapshot(long queryOffset, decimal avgPrice, double avgVolume, 
        IReadOnlyList<IPriceUpdate> recentPriceUpdates, IReadOnlyList<IVolumeUpdate> recentVolumeUpdates)
    {
        QueryOffset = queryOffset;
        AvgPrice = avgPrice;
        AvgVolume = avgVolume;
        RecentPriceUpdates = recentPriceUpdates;
        RecentVolumeUpdates = recentVolumeUpdates;
    }

    /// <summary>
    /// The sequence number of the Akka.Persistence.Query object to begin reading from.
    /// </summary>
    public long QueryOffset { get; }

    /// <summary>
    /// The most recently saved average price.
    /// </summary>
    public decimal AvgPrice { get; }

    /// <summary>
    /// The most recently saved average volume.
    /// </summary>
    public double AvgVolume { get; }

    public IReadOnlyList<IPriceUpdate> RecentPriceUpdates { get; }

    public IReadOnlyList<IVolumeUpdate> RecentVolumeUpdates { get; }
}
```

The critical property on this snapshot is the `QueryOffset` - this is a `long` value given to us on each one of the `EventEnvelope` objects replayed by Akka.Persistence.Query. We save this value periodically when replaying those `EventEnvelope`s inside the `MatchAggregator`:

```csharp
Command<EventEnvelope>(e =>
{
    if (e.Event is Match m)
    {
        // update the offset
        if (e.Offset is Sequence s)
        {
            QueryOffset = s.Value;
        }

        if (_matchAggregate == null)
        {
            _matchAggregate = new MatchAggregate(TickerSymbol, m.SettlementPrice, m.Quantity);
            return;
        }

        if (!_matchAggregate.WithMatch(m))
        {
            _log.Warning("Received Match for ticker symbol [{0}] - but we only accept symbols for [{1}]", m.StockId, TickerSymbol);
        }
    }
});
```

The `MatchAggregateSnapshot` is saved later when a `PublishEvents` message is received by this same actor - but we're always updating the `QueryOffset` value inside this actor and that's what we use in the `IEventsByTagQuery` when this actor starts up. It's key to keeping the turn-around times short on resuming queries for actors that have long trade histories.

#### Akka.Cluster.Sharding and Message Routing
In both the Trading and Pricing Services domains, we make heavy use of Akka.Cluster.Sharding in order to guarantee that there's a single instance of a particular domain entity present in the cluster at any given time.

A brief overview of Akka.Cluster.Sharding: 

1. Every entity type has its own `ShardRegion` - so in the case of the Trading Services, we have "orderBook" entities - each one representing the order book for a specific stock ticker symbol. In the Pricing Services we have "priceAggregator" entities.
2. A `ShardRegion` can host an arbitrary number of entity actors, defined using the `Props` passed into the `ClusterSharding.Start` method - each one of these entity actors represents a globally unique entity of their shardRegion type.
3. The `ShardRegion` aggregates these entity actors underneath parent root actors called "shards" - a shard is just an arbitrarily large number of entity actors grouped together for the purposes of ease-of-distribution across the Cluster. [The `ShardRegion` distributes these shards evenly across the cluster and will re-balance the distribution of shards in the event of a new node joining the cluster or an old node leaving](https://petabridge.com/blog/cluster-sharding-technical-overview-akkadotnet/).
4. In the event of a `ShardRegion` node becoming unreachable due to a network partition, node of the shards and entity actors on the unreachable node will be moved until that node (a) becomes reachable again or (b) [is marked as DOWN by another node in the cluster and kicked out](https://petabridge.com/blog/proper-care-of-akkadotnet-clusters/). This is done in order to guarantee that there's never more than 1 instance of a given entity actor at any time.
5. Any entity actors hosted by a `ShardRegion` can be accessed from other non-`ShardRegion` nodes through the use of a `ShardRegionProxy`, a router that uses the same message distribution mechanism as the `ShardRegion`. Therefore, sharded entity actors are always accessible to anyone in the cluster.
6. In the event that a shard and its entities are moved onto a new node, all of the messages intended for entity actors hosted on the affected shards are buffered by the `ShardRegion` and the `ShardRegionProxy` and released only once the shard actors have been successfully recreated on their new node.

All of these mechanisms are designed to provide a high degree of consistency, fault tolerance, and ease-of-use for Akka.NET users - hence why we make heavy use of Akka.Cluster.Sharding in the Akka.CQRS code sample.

The key to making sharding work smoothly across all of the nodes in the cluster, however, is ensuring that the same `IMessageExtractor` implementation is available - which is what we did with the [`StockShardMsgRouter` inside Akka.CQRS.Infrastructure](src/Akka.CQRS.Infrastructure/StockShardMsgRouter.cs):

```csharp
/// <summary>
/// Used to route sharding messages to order book actors hosted via Akka.Cluster.Sharding.
/// </summary>
public sealed class StockShardMsgRouter : HashCodeMessageExtractor
{
    /// <summary>
    /// 3 nodes hosting order books, 10 shards per node.
    /// </summary>
    public const int DefaultShardCount = 30;

    public StockShardMsgRouter() : this(DefaultShardCount)
    {
    }

    public StockShardMsgRouter(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    public override string EntityId(object message)
    {
        if (message is IWithStockId stockMsg)
        {
            return stockMsg.StockId;
        }

        switch (message)
        {
            case ConfirmableMessage<Ask> a:
                return a.Message.StockId;
            case ConfirmableMessage<Bid> b:
                return b.Message.StockId;
            case ConfirmableMessage<Fill> f:
                return f.Message.StockId;
            case ConfirmableMessage<Match> m:
                return m.Message.StockId;
        }

        return null;
    }
}
```

This message extractor works by extracting the `StockId` property from messages with `IWithStockId` defined, since those are the events and commands we're sending to our sharded entity actors in both the Trading and Pricing services. It's worth noting, however, that we're also making use of the [`IComfirmableMessage`](https://devops.petabridge.com/api/Akka.Persistence.Extras.IConfirmableMessage.html) type from Akka.Persistence.Extras along with the `PersistenceSuperivsor` from that same package, hence why we've added handling for the `ConfirmableMessage<T>` types inside the `StockShardMsgRouter`.

> Rule of thumb: when trying to choose the number of shards you want to have in Akka.Cluster.Sharding, use the following formula: `max # of nodes who can host enities of this type * 10 = shard count`. This will give you a moderate amount of shards and ensure that re-balancing of shards doesn't happen too often and when it does happen it doesn't impact an unacceptably large number of entities all at once.

With this `StockShardMsgRouter` in-hand, we can [start our `ShardRegion` inside the Trading Services' "OrderBook" nodes](src/Akka.CQRS.TradeProcessor.Service/Program.cs#L42-L48):

```csharp
Cluster.Cluster.Get(actorSystem).RegisterOnMemberUp(() =>
{
    var sharding = ClusterSharding.Get(actorSystem);


    var shardRegion = sharding.Start("orderBook", s => OrderBookActor.PropsFor(s), ClusterShardingSettings.Create(actorSystem),
        new StockShardMsgRouter());
});
```

Or a [`ShardProxy` inside the Trading Service's "Trade Placer" nodes](src/Akka.CQRS.TradePlacers.Service/Program.cs#L32-L55):

```csharp
Cluster.Cluster.Get(actorSystem).RegisterOnMemberUp(() =>
{
    var sharding = ClusterSharding.Get(actorSystem);


    var shardRegionProxy = sharding.StartProxy("orderBook", "trade-processor", new StockShardMsgRouter());
    foreach (var stock in AvailableTickerSymbols.Symbols)
    {
        var max = (decimal)ThreadLocalRandom.Current.Next(20, 45);
        var min = (decimal) ThreadLocalRandom.Current.Next(10, 15);
        var range = new PriceRange(min, 0.0m, max);


        // start bidders
        foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 6)))
        {
            actorSystem.ActorOf(Props.Create(() => new BidderActor(stock, range, shardRegionProxy)));
        }


        // start askers
        foreach (var i in Enumerable.Repeat(1, ThreadLocalRandom.Current.Next(1, 6)))
        {
            actorSystem.ActorOf(Props.Create(() => new AskerActor(stock, range, shardRegionProxy)));
        }
    }
});
```

One final, important thing to note about working with Akka.Cluster.Sharding - _we need to specify the roles that shards are hosted on_. Otherwise Akka.Cluster.Sharding will try to communicate with `ShardRegion` hosts on all nodes in the cluster. You can do this via the `ClusterShardingSettings` class or via the `akka.cluster.sharding` HOCON, which we did in this sample:

```
akka{
    # rest of HOCON configuration
    cluster {
        #will inject this node as a self-seed node at run-time
        seed-nodes = ["akka.tcp://AkkaTrader@127.0.0.1:5055"] 
        roles = ["trade-processor" , "trade-events"]

        pub-sub{
            role = "trade-events"
        }

        sharding{
            role = "trade-processor"
        }
    }
}
```

#### Dealing with Unreachable Nodes and Failover in Akka.Cluster.Sharding

One other major concern we need to address when working with Akka.Cluster.Sharding is ensuring that unreachable nodes in the cluster are downed quickly in the event that they don't recover. In most production scenarios, a node is only unreachable for a brief period of time - typically the result of a temporary network partition, therefore nodes usually recover back to a reachable state rather quickly. 

However, in the event of an issue like an outright hardware failure, a process crash, or an unclean shutdown (not letting the node leave the cluster gracefully prior to termination) then these "unreachable" nodes are truly permanently unavailable. Therefore, we should remove those nodes from the cluster's membership. Today you can do this manually with a tool like [Petabridge.Cmd.Cluster](https://cmd.petabridge.com/articles/commands/cluster-commands.html) via the `cluster down-unreachable` command, but a better method for accomplishing this is to [use the built-in Split Brain Resolvers inside Akka.Cluster](https://getakka.net/articles/clustering/split-brain-resolver.html).

Inside the [Akka.CQRS.Infrastructure/Ops folder we have an embedded HOCON file and a utility class for parsing it](src/Akka.CQRS.Infrastructure/Ops) - the HOCON file contains a straight-forward split brain resolver configuration that we standardize across all nodes in both the Trading Services and Pricing Services clusters.

```
# Akka.Cluster split-brain resolver configurations
akka.cluster{
    downing-provider-class = "Akka.Cluster.SplitBrainResolver, Akka.Cluster"
    split-brain-resolver {
        active-strategy = keep-majority
    }
}
```

The `keep-majority` strategy works as follows: if there's 10 nodes and 4 suddenly become unreachable, the remaining part of the cluster with 6 nodes still standing kicks the remaining 4 nodes out of the cluster via a DOWN command if those nodes have been unreachable for longer than 45 seconds ([read the documentation for a full explanation of the algorithm, including tie-breakers.](https://getakka.net/articles/clustering/split-brain-resolver.html))

We then ensure via the `OpsConfig` class that this configuration is used _uniformly throughout all of our cluster nodes_, since any of those nodes can be the leader of the cluster and it's the leader who has to execute the split brain resolver strategy code. You can see an example here where we [start up the Pricing Service in its `Program.cs`](https://github.com/Aaronontheweb/InMemoryCQRSReplication/blob/3cbf46da0cf4e9735204c2750e2df9e3bead3eca/src/Akka.CQRS.Pricing.Service/Program.cs#L41-L44):

```csharp
// get HOCON configuration
var conf = ConfigurationFactory.ParseString(config).WithFallback(GetMongoHocon(mongoConnectionString))
    .WithFallback(OpsConfig.GetOpsConfig())
    .WithFallback(ClusterSharding.DefaultConfig())
    .WithFallback(DistributedPubSub.DefaultConfig());
```

When this is used in combination with Akka.Cluster.Sharding the split brain resolver guarantees that no entity in a `ShardRegion` will be unavailable for longer than the split brain resolver's downing duration. This works because whenever an unreachable `ShardRegion` node is DOWNed, all of its shards will be automatically re-allocated onto one or more of the other available `ShardRegion` host nodes remaining in the cluster. It provides automatic fault-tolerance even in the case of total loss of availability for one or more affected nodes.

### Trading Services Domain
The write-side cluster, the Trading Services are primarily interested in the placement and matching of new trade orders for buying and selling of specific stocks.

![Akka.CQRS Architectural overview](docs/images/akka-cqrs-architectural-overview.png)

The Trading Services are driven primarily through the use of three actor types:

1. [`BidderActor`](src/Akka.CQRS.TradeProcessor.Actors/BidderActor.cs) - runs inside the "Trade Placement" services and randomly bids on a specific stock;
2. [`AskerActor`](rc/Akka.CQRS.TradeProcessor.Actors/AskerActor.cs) - runs inside the "Trade Placement" services and randomly asks (sells) a specific stock; and
3. [`OrderBookActor`](src/Akka.CQRS.TradeProcessor.Actors/OrderBookActor.cs) - the most important actor in this scenario, it is hosted on the "Trace Processor" service and it's responsible for matching bids with asks, and when it does it publishes `Match` and `Fill` events across the cluster using `DistributedPubSub`. This is how the `AskerActor` and the `BidderActor` involved in making the trade are notified that their trades have been settled. All events received and produced by the `OrderBookActor` are persisted using Akka.Persistence.MongoDb.

The domain design is relatively simple otherwise and we'd encourage you to look at the code directly for more details about how it all works. 

### Pricing Services Domain
The read-side cluster, the Pricing Services consume the `Match` events for specific ticker symbols produced by the `OrderBookActor`s inside the Trading Services domain by replaying them over Akka.Persistence.Query.

The [`MatchAggregator` actors hosted inside Akka.Cluster.Sharding on the Pricing Services nodes](src/Akka.CQRS.Pricing.Actors/MatchAggregator.cs) are the ones who actually execute the Akka.Persistence.Query and aggregate the `Match.SettlementPrice` and `Match.Quantity` to produce an estimated, weighted moving average of both volume and price. 

These actors also use `DistributedPubSub` to periodically publish `IPriceUpdate` events out to the rest of the Pricing Services cluster:

```csharp
Command<PublishEvents>(p =>
{
    if (_matchAggregate == null)
        return;

    var (latestPrice, latestVolume) = _matchAggregate.FetchMetrics(_timestamper);

    // Need to update pricing records prior to persisting our state, since this data is included in
    // output of SaveAggregateData()
    _priceUpdates.Add(latestPrice);
    _volumeUpdates.Add(latestVolume);

    PersistAsync(SaveAggregateData(), snapshot =>
    {
        _log.Info("Saved latest price {0} and volume {1}", snapshot.AvgPrice, snapshot.AvgVolume);
        if (LastSequenceNr % SnapshotEveryN == 0)
        {
            SaveSnapshot(snapshot);
        }
    });

    // publish updates to in-memory replicas
    _mediator.Tell(new Publish(_priceTopic, latestPrice));
    _mediator.Tell(new Publish(_volumeTopic, latestVolume));
});
```

#### In-Memory Replication of Price and Volume Data
One of the innovations used in the Akka.CQRS.Pricing.Service is that we have two different layers available for reads:

![Akka.CQRS In-memory replication](docs/images/akka-cqrs-inmemory-replication.png)

In addition to having source of truth `MatchAggregator` actors, who create pricing projections based on the `Match` events created on the write side of the application, the `MatchAggregator` actors also publish their `IPriceUpdate` and `IVolumeUpdate` events via `DistributedPubSub` to [`PriceVolumeViewActor` instances](/src/Akka.CQRS.Pricing.Actors/PriceViewActor.cs). There's exactly 1 `PriceVolumeViewActor` _on every node inside the Pricing Service Cluster_ for every single ticker symbol available.

This has some interesting performance, consistency, and availability implications:

1. The price of any stock can be queried locally, in-memory at virtually no cost (can process millions of queries per second per node and
2. In the event that the `MatchAggregator` actor dies and has to be moved to another node (per Akka.Cluster.Sharding's mechanics), the local `PriceVolumeViewActor` instances will still be able to successfully serve requests (they stay available), but at the cost of some consistency since no one is reading new `Match` events coming in from the Trading Services.

The `PriceVolumeViewActor` communicates with the `MatchAggregator` initially through the `ShardRegion` `IActorRef`, but once it's subscribed via `DistributedPubSub` to the `MatchAggregator`'s price updates for the same ticker symbol (i.e. the "MSFT" `PriceVolumeViewActor` subscribes to the "MSFT-price" and "MSFT-volume" events published by the "MSFT" `MatchAggregator`).

In the event that the `MatchAggregator` producing this pricing data dies, the `PriceVolumeViewActor` will re-request the current price and volume snapshot recovered by the new incarnation of the `MatchAggregator` as part of its `PersistentActor` methods - and it will replace its current state using this data:

```csharp
  /// <summary>
    /// In-memory, replicated view of the current price and volume for a specific stock.
    /// </summary>
    public sealed class PriceVolumeViewActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly string _tickerSymbol;
        private ICancelable _pruneTimer;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        // the Cluster.Sharding proxy
        private readonly IActorRef _priceActorGateway;

        // the DistributedPubSub mediator
        private readonly IActorRef _mediator;

        private IActorRef _tickerEntity;
        private PriceHistory _history;
        private readonly string _priceTopic;

        private sealed class Prune
        {
            public static readonly Prune Instance = new Prune();
            private Prune() { }
        }

        public PriceVolumeViewActor(string tickerSymbol, IActorRef priceActorGateway, IActorRef mediator)
        {
            _tickerSymbol = tickerSymbol;
            _priceActorGateway = priceActorGateway;
            _priceTopic = PriceTopicHelpers.PriceUpdateTopic(_tickerSymbol);
            _mediator = mediator;
            _history = new PriceHistory(_tickerSymbol, ImmutableSortedSet<IPriceUpdate>.Empty);

            WaitingForPriceAndVolume();
        }

        public IStash Stash { get; set; }

        private void WaitingForPriceAndVolume()
        {
            Receive<PriceAndVolumeSnapshot>(s =>
            {
                if (s.PriceUpdates.Length == 0) // empty set - no price data yet
                {
                    _history = new PriceHistory(_tickerSymbol, ImmutableSortedSet<IPriceUpdate>.Empty);
                    _log.Info("Received empty price history for [{0}]", _history.StockId);
                }
                else
                {
                    _history = new PriceHistory(_tickerSymbol, s.PriceUpdates.ToImmutableSortedSet());
                    _log.Info("Received recent price history for [{0}] - current price is [{1}] as of [{2}]", _history.StockId, _history.CurrentPrice, _history.Until);
                }
                
                _tickerEntity = Sender;

                // DistributedPubSub mediator subscription
                _mediator.Tell(new Subscribe(_priceTopic, Self));
            });

            Receive<SubscribeAck>(ack =>
            {
                _log.Info("Subscribed to {0} - ready for real-time processing.", _priceTopic);
                Become(Processing);
                Context.Watch(_tickerEntity);
                Context.SetReceiveTimeout(null);
            });

            Receive<ReceiveTimeout>(_ =>
            {
                _log.Warning("Received no initial price values for [{0}] from source of truth after 5s. Retrying..", _tickerSymbol);
                _priceActorGateway.Tell(new FetchPriceAndVolume(_tickerSymbol));
            });
        }

        // rest of actor
    }   
}
```

This in-memory replication technique is extremely useful for improving both latency and availability in any Akka.Cluster application, but it comes at the cost of some consistency.

#### Hydrating All Ticker Symbols in Cluster.Sharding
One small issue with the design of Akka.Cluster.Sharding is that entity actors are spawned on-demand - when a message is routed from a `ShardRegion` or a `ShardRegionProxy` to a specific entity actor using an `IMessageExtractor` such as the `StockShardMsgRouter`. Given that all of our entity actors need to be awake at all times, initiated by consuming data from Akka.Persistence rather than responding to external events from other actors inside the Pricing Services, how can we ensure that 100% of our ticker symbols are covered?

The answer is through the use of the [`PriceInitiatorActor`](src/Akka.CQRS.Pricing.Actors/PriceInitiatorActor.cs), a [Cluster Singleton actor](https://getakka.net/articles/clustering/cluster-singleton.html) responsible for generating heartbeat messages to initialize the creation of all `MatchAggregator` actors through Akka.Cluster.Sharding:

```csharp
// <summary>
/// Intended to be a Cluster Singleton. Responsible for ensuring there's at least one instance
/// of a <see cref="MatchAggregator"/> for every single persistence id found inside the datastore.
/// </summary>
public sealed class PriceInitiatorActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IPersistenceIdsQuery _tradeIdsQuery;
    private readonly IActorRef _pricingQueryProxy;
    private readonly HashSet<string> _tickers = new HashSet<string>();

    /*
     * Used to periodically ping Akka.Cluster.Sharding and ensure that all pricing
     * entities are up and producing events for their in-memory replicas over the network.
     *
     * Technically, akka.cluster.sharding.remember-entities = on should take care of this
     * for us in the initial pass, but the impact of having this code is virtually zero
     * and in the event of a network partition or an error somewhere, will effectively prod
     * the non-existent entity into action. Worth having it.
     */
    private ICancelable _heartbeatInterval;


    private class Heartbeat
    {
        public static readonly Heartbeat Instance = new Heartbeat();
        private Heartbeat() { }
    }

    public PriceInitiatorActor(IPersistenceIdsQuery tradeIdsQuery, IActorRef pricingQueryProxy)
    {
        _tradeIdsQuery = tradeIdsQuery;
        _pricingQueryProxy = pricingQueryProxy;

        Receive<Ping>(p =>
        {
            _tickers.Add(p.StockId);
            _pricingQueryProxy.Tell(p);
        });

        Receive<Heartbeat>(h =>
        {
            foreach (var p in _tickers)
            {
                _pricingQueryProxy.Tell(new Ping(p));
            }
        });

        Receive<UnexpectedEndOfStream>(end =>
        {
            _log.Warning("Received unexpected end of PersistenceIds stream. Restarting.");
            throw new ApplicationException("Restart me!");
        });
    }

    protected override void PreStart()
    {
        var mat = Context.Materializer();
        var self = Self;
        _tradeIdsQuery.PersistenceIds()
            .Where(x => x.EndsWith(EntityIdHelper
                .OrderBookSuffix)) // skip persistence ids belonging to price entities
            .Select(x => new Ping(EntityIdHelper.ExtractTickerFromPersistenceId(x)))
            .RunWith(Sink.ActorRef<Ping>(self, UnexpectedEndOfStream.Instance), mat);

        _heartbeatInterval = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30), Self, Heartbeat.Instance, ActorRefs.NoSender);
    }

    protected override void PostStop()
    {
        _heartbeatInterval?.Cancel();
    }
}
```

This actor uses Akka.Persistence.Query's `IPersistenceIdsQuery` to fetch a list of all available Akka.Persistence IDs, and it's able to fetch all of the entity ID's specific to `OrderBook` entities in the Trading Service and extract just the stock ticker symbol from them. From there, this actor communicates with the `priceAggregator` ShardRegion and fires a `Ping` message at every single ticker symbol, which is responsible for creating and starting all of the `MatchAggregator` actors if they don't already exist.

> Akka.Cluster.Sharding has a built-in feature that should [keep these entity actors alive automatically once they're started for the first time, but need to move to another cluster: `akka.cluster.sharding.remember-entities = true`.](https://getakka.net/articles/clustering/cluster-sharding.html#remembering-entities)

## Running Akka.CQRS
Akka.CQRS can be run easily using Docker and `docker-compose`. You'll want to make sure you have [Docker for Windows](https://docs.docker.com/docker-for-windows/) installed with Linux containers enabled if you're running on a Windows PC.

First, clone this repository and run the [`build.cmd` script](build.cmd) in the root of this repository:

```
PS> ./build.cmd docker
```

The `docker` build stage will create three Docker images locally:

* `akka.cqrs.tradeprocessor`
* `akka.cqrs.traders`
* `akka.cqrs.pricing`

All of these Docker images will be tagged with the `latest` tag and the version number found at the topmost entry of [`RELEASE_NOTES.md`](RELEASE_NOTES.md).

From there, we can start up both cluster via `docker-compose`:

```
PS> docker-compose up
```

This will create both clusters, the MongoDb database they depend on, and will also expose the Pricing node's `Petabridge.Cmd.Host` port on a randomly available port. [See the `docker-compose.yaml` file for details](docker-compose.yaml).

### Testing the Consistency and Failover Capabilities of Akka.CQRS
If you want to test the consistency and fail-over capabilities of this sample, then start by [installing the `pbm` commandline tool](https://cmd.petabridge.com/articles/install/index.html):

```
PS> dotnet tool install --global pbm 
```

Next, connect to the first one of the pricing nodes we have running inside of Docker. If you're using [Kitematic, part of Docker for Windows](https://kitematic.com/), you can see the random port that the `akka.cqrs.pricing` containers expose their port on by clicking on the running container instance and going to **Networking**:

![Docker for Windows host container networking](/docs/images/docker-for-windows-networking.png)

Connect to `Petabridge.Cmd` on this node via the following command:

```
PS> pbm 127.0.0.1:32773 (in this instance - your port number will be chosen at random by docker)
```

Once you're connected, you'll be able to access all sorts of information about this Pricing node, including [accessing some customer Petabridge.Cmd palettes designed specifically for accessing stock price information inside this sample](rc/Akka.CQRS.Pricing.Cli).

Go ahead and start a price-tracking command and see how it goes:

```
pbm> price track -s MSFT
```

Next, use `docker-compose` to bring up a few more Pricing containers in another terminal window:

```
PS> docker-compose up scale pricing-engine=4
```

This will create another 3 containers running the `akka.cqrs.pricing` image - all of them will join the cluster automatically, which you can verify using a `cluster show` command in Petabridge.Cmd.

Once this is done, start two more terminals and _connect to two of the new `akka.cqrs.pricing` nodes you just started_ and execute the same `price track -s MSFT` command. You should see that the stream of updated prices is uniform across all three nodes.

Now go and kill the original node you were connected to - the first one of the `akka.cqrs.pricing` nodes you were connected to. This node definitely has some shards hosted on it, if not all of the shards given how few entities there are, so this will prompt a fail-over to happen and for the sharded entity to move across the cluster. What you should see is that the pricing data for the other two nodes you're connected to remains consistent both before, during, and after the fail-over. It may have taken some time for the new `MatchAggregator` to come online and begin updating prices again, but once it came back online the `PriceVolumeViewActor`s that the Akka.CQRS.Pricing.Cli commands uses were able to re-acquire their pricing information from Akka.Cluster.Sharding and continue running normally.

##### Known Issues
This sample is currently affected by https://github.com/akkadotnet/akka.net/issues/3414, so you will need to explicitly delete the MongoDb container (not just stop it - DELETE it) every time you restart this Docker cluster from scratch. We're working on fixing that issue and should have a patch for it shortly.