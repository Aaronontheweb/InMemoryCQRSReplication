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

### Trading Services Domain
The write-side cluster, the Trading Services are primarily interested in the placement and matching of new trade orders for buying and selling of specific stocks.
