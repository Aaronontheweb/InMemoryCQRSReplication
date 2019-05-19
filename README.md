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

A real-life trading system introduces many more requirements and infrastructure needs than this Akka.CQRS sample does, but for our purposes.