using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.CQRS.Subscriptions.InMem
{
    /// <summary>
    /// Used locally, in-memory by a single order book actor. Belongs to a single ticker symbol.
    /// </summary>
    public sealed class InMemoryTradeEventPublisher : ITradeEventPublisher, ITradeEventSubscriptionManager
    {
        private readonly Dictionary<TradeEventType, HashSet<IActorRef>> _subscribers;

        public InMemoryTradeEventPublisher() : this(new Dictionary<TradeEventType, HashSet<IActorRef>>()) { }

        public InMemoryTradeEventPublisher(Dictionary<TradeEventType, HashSet<IActorRef>> subscribers)
        {
            _subscribers = subscribers;
        }

        public void Publish(string tickerSymbol, ITradeEvent @event)
        {
            var eventType = @event.ToTradeEventType();
            EnsureSub(eventType);

            foreach(var sub in _subscribers[eventType])
                sub.Tell(@event);
        }

        public Task<TradeSubscribeAck> Subscribe(string tickerSymbol, IActorRef subscriber)
        {
            return Subscribe(tickerSymbol, TradeEventHelpers.AllTradeEventTypes, subscriber);
        }

        public Task<TradeSubscribeAck> Subscribe(string tickerSymbol, TradeEventType @event, IActorRef subscriber)
        {
            return Subscribe(tickerSymbol, new[] {@event}, subscriber);
        }

        public Task<TradeSubscribeAck> Subscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber)
        {
            foreach (var e in events)
            {
                EnsureSub(e);
                _subscribers[e].Add(subscriber);
            }

            return Task.FromResult(new TradeSubscribeAck(tickerSymbol, events));
        }

        private void EnsureSub(TradeEventType e)
        {
            if (!_subscribers.ContainsKey(e))
            {
                _subscribers[e] = new HashSet<IActorRef>();
            }
        }

        public Task<TradeUnsubscribeAck> Unsubscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber)
        {
            foreach (var e in events)
            {
                EnsureSub(e);
                _subscribers[e].Remove(subscriber);
            }

            return Task.FromResult(new TradeUnsubscribeAck(tickerSymbol, events));
        }

        public Task<TradeUnsubscribeAck> Unsubscribe(string tickerSymbol, TradeEventType @event, IActorRef subscriber)
        {
            return Unsubscribe(tickerSymbol, new []{ @event }, subscriber);
        }

        public Task<TradeUnsubscribeAck> Unsubscribe(string tickerSymbol, IActorRef subscriber)
        {
            return Unsubscribe(tickerSymbol, TradeEventHelpers.AllTradeEventTypes, subscriber);
        }
    }
}