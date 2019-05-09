using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.CQRS.Commands;
using Akka.CQRS.Events;
using Akka.CQRS.Matching;
using Akka.CQRS.Subscriptions;
using Akka.CQRS.Subscriptions.DistributedPubSub;
using Akka.Event;
using Akka.Persistence;
using Akka.Persistence.Extras;
using Akka.Util.Internal;

namespace Akka.CQRS.TradeProcessor.Actors
{
    /// <summary>
    /// Actor responsible for processing orders for a specific ticker symbol.
    /// </summary>
    public class OrderBookActor : ReceivePersistentActor
    {
        /// <summary>
        /// Take a snapshot every N messages persisted.
        /// </summary>
        public const int SnapshotInterval = 100;
        private MatchingEngine _matchingEngine;
        private readonly ITradeEventPublisher _publisher;
        private readonly ITradeEventSubscriptionManager _subscriptionManager;
        private readonly IActorRef _confirmationActor;

        private readonly ILoggingAdapter _log = Context.GetLogger();

        public OrderBookActor(string tickerSymbol, IActorRef confirmationActor) : this(tickerSymbol, null, DistributedPubSubTradeEventPublisher.For(Context.System), confirmationActor, subscriptionManager) { }
        public OrderBookActor(string tickerSymbol, MatchingEngine matchingEngine, ITradeEventPublisher publisher, IActorRef confirmationActor, ITradeEventSubscriptionManager subscriptionManager)
        {
            TickerSymbol = tickerSymbol;
            _matchingEngine = matchingEngine ?? CreateDefaultMatchingEngine(tickerSymbol, _log);
            _publisher = publisher;
            _confirmationActor = confirmationActor;
            _subscriptionManager = subscriptionManager;

            Recovers();
            Commands();
        }

        private static MatchingEngine CreateDefaultMatchingEngine(string tickerSymbol, ILoggingAdapter logger)
        {
            return new MatchingEngine(tickerSymbol, logger);
        }

        public string TickerSymbol { get; }
        public override string PersistenceId => TickerSymbol;

        private void Recovers()
        {
            Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is OrderbookSnapshot orderBook)
                {
                    _matchingEngine = MatchingEngine.FromSnapshot(orderBook, _log);
                }
            });

            Recover<Bid>(b => { _matchingEngine.WithBid(b); });
            Recover<Ask>(a => { _matchingEngine.WithAsk(a);});

            // Fill and Match can't modify the state of the MatchingEngine.
            Recover<Match>(m => { }); 
            Recover<Fill>(f => { });
        }

        private void Commands()
        {
            Command<ConfirmableMessage<Ask>>(a =>
            {
                
                // For the sake of efficiency - update orderbook and then persist all events
                var events = _matchingEngine.WithAsk(a.Message);
                var persistableEvents = new ITradeEvent[] {a.Message}.Concat<ITradeEvent>(events); // ask needs to go before Fill / Match

                PersistAll(persistableEvents, @event =>
                {
                    _log.Info("[{0}][{1}] - {2} units @ {3} per unit", PersistenceId, @event.ToTradeEventType(), a.Message.AskQuantity, a.Message.AskPrice);
                    if (@event is Ask)
                    {
                        _confirmationActor.Tell(new Confirmation(a.ConfirmationId, PersistenceId));
                    }
                    _publisher.Publish(PersistenceId, @event);

                    // Take a snapshot every N messages to optimize recovery time
                    if (LastSequenceNr % SnapshotInterval == 0)
                    {
                        SaveSnapshot(_matchingEngine.GetSnapshot());
                    }
                });
            });

            Command<ConfirmableMessage<Bid>>(b =>
            {
                // For the sake of efficiency -update orderbook and then persist all events
                var events = _matchingEngine.WithBid(b.Message);
                var persistableEvents = new ITradeEvent[] { b.Message }.Concat<ITradeEvent>(events); // bid needs to go before Fill / Match

                PersistAll(persistableEvents, @event =>
                {
                    _log.Info("[{0}][{1}] - {2} units @ {3} per unit", PersistenceId, @event.ToTradeEventType(), b.Message.BidQuantity, b.Message.BidPrice);
                    if (@event is Bid)
                    {
                        _confirmationActor.Tell(new Confirmation(b.ConfirmationId, PersistenceId));
                    }
                    _publisher.Publish(PersistenceId, @event);

                    // Take a snapshot every N messages to optimize recovery time
                    if (LastSequenceNr % SnapshotInterval == 0)
                    {
                        SaveSnapshot(_matchingEngine.GetSnapshot());
                    }
                });
            });

            /*
             * Handle subscriptions directly in case we're using in-memory, local pub-sub.
             */
            Command<TradeSubscribe>(sub =>
            {
                
            });

            Command<GetOrderBookSnapshot>(s =>
            {
                Sender.Tell(_matchingEngine.GetSnapshot());
            });
        }
    }
}
