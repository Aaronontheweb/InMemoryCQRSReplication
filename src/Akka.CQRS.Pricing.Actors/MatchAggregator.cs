using System;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.CQRS.Events;
using Akka.CQRS.Pricing.Commands;
using Akka.CQRS.Pricing.Events;
using Akka.CQRS.Pricing.Views;
using Akka.CQRS.Util;
using Akka.Event;
using Akka.Persistence;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Petabridge.Collections;

namespace Akka.CQRS.Pricing.Actors
{
    /// <summary>
    /// Used to aggregate <see cref="Akka.CQRS.Events.Match"/> events via Akka.Persistence.Query
    /// </summary>
    public class MatchAggregator : ReceivePersistentActor
    {
        // Take a snapshot every 10 journal entries
        public const int SnapshotEveryN = 10; 

        private readonly IEventsByTagQuery _eventsByTag;
        private MatchAggregate _matchAggregate;
        private readonly IActorRef _mediator;
        private readonly ITimestamper _timestamper;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly CircularBuffer<IPriceUpdate> _priceUpdates = new CircularBuffer<IPriceUpdate>(MatchAggregate.DefaultSampleSize);
        private readonly CircularBuffer<IVolumeUpdate> _volumeUpdates = new CircularBuffer<IVolumeUpdate>(MatchAggregate.DefaultSampleSize);
        private ICancelable _publishPricesTask;

        private readonly string _priceTopic;
        private readonly string _volumeTopic;

        public readonly string TickerSymbol;
        public override string PersistenceId { get; }

        public long QueryOffset { get; private set; }

        private class PublishEvents
        {
            public static readonly PublishEvents Instance = new PublishEvents();
            private PublishEvents() { }
        }

        public MatchAggregator(string tickerSymbol, IEventsByTagQuery eventsByTag)
         : this(tickerSymbol, eventsByTag, DistributedPubSub.Get(Context.System).Mediator, CurrentUtcTimestamper.Instance)
        {
        }

        public MatchAggregator(string tickerSymbol, IEventsByTagQuery eventsByTag, IActorRef mediator, ITimestamper timestamper)
        {
            TickerSymbol = tickerSymbol;
            _priceTopic = PriceTopicHelpers.PriceUpdateTopic(TickerSymbol);
            _volumeTopic = PriceTopicHelpers.VolumeUpdateTopic(TickerSymbol);
            _eventsByTag = eventsByTag;
            _mediator = mediator;
            _timestamper = timestamper;
            PersistenceId = EntityIdHelper.IdForPricing(tickerSymbol);
            
            Receives();
            Commands();
        }

        private void Receives()
        {
            /*
             * Can be saved as a snapshot or as an event
             */
            Recover<SnapshotOffer>(o =>
            {
                if (o.Snapshot is MatchAggregatorSnapshot s)
                {
                    RecoverAggregateData(s);
                }
            });

            Recover<MatchAggregatorSnapshot>(s => { RecoverAggregateData(s); });
        }

        /// <summary>
        /// Recovery has completed successfully.
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

        private void RecoverAggregateData(MatchAggregatorSnapshot s)
        {
            _matchAggregate = new MatchAggregate(TickerSymbol, s.AvgPrice, s.AvgVolume);
            QueryOffset = s.QueryOffset;
        }

        private MatchAggregatorSnapshot SaveAggregateData()
        {
            return new MatchAggregatorSnapshot(QueryOffset, _matchAggregate.AvgPrice.CurrentAvg, _matchAggregate.AvgVolume.CurrentAvg);
        }

        private void Commands()
        {
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

            // Command sent by a PriceViewActor to pull down a complete snapshot of active pricing data
            Command<FetchPriceAndVolume>(f =>
            {
                // no price data yet
                if (_priceUpdates.Count == 0 || _volumeUpdates.Count == 0)
                {
                    Sender.Tell(PriceAndVolumeSnapshot.Empty(TickerSymbol));
                }
                else
                {
                    Sender.Tell(new PriceAndVolumeSnapshot(TickerSymbol, _priceUpdates.ToArray(), _volumeUpdates.ToArray()));
                }
                
            });

            Command<PublishEvents>(p =>
            {
                if (_matchAggregate == null)
                    return;

                var (latestPrice, latestVolume) = _matchAggregate.FetchMetrics(_timestamper);
                
                PersistAsync(SaveAggregateData(), snapshot =>
                {
                    _log.Info("Saved latest price {0} and volume {1}", snapshot.AvgPrice, snapshot.AvgVolume);
                    if (LastSequenceNr % SnapshotEveryN == 0)
                    {
                        SaveSnapshot(snapshot);
                    }
                });

                _priceUpdates.Add(latestPrice);
                _volumeUpdates.Add(latestVolume);

                // publish updates to in-memory replicas
                _mediator.Tell(new Publish(_priceTopic, latestPrice));
                _mediator.Tell(new Publish(_volumeTopic, latestVolume));
            });

            Command<Ping>(p =>
            {
                if (_log.IsDebugEnabled)
                {
                    _log.Debug("pinged via {0}", Sender);
                }
            });

            Command<SaveSnapshotSuccess>(s =>
            {
                // clean-up prior snapshots and journal events
                DeleteSnapshots(new SnapshotSelectionCriteria(s.Metadata.SequenceNr-1));
                DeleteMessages(s.Metadata.SequenceNr);
            });
        }

        protected override void PostStop()
        {
            _publishPricesTask?.Cancel();
            base.PostStop();
        }
    }
}
