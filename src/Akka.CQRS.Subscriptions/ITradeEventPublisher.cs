// -----------------------------------------------------------------------
// <copyright file="ITradeEventPublisher.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Abstraction used for publishing data about <see cref="ITradeEvent"/> instances.
    /// </summary>
    public interface ITradeEventPublisher
    {
        void Publish(string tickerSymbol, ITradeEvent @event);
    }

    /// <summary>
    /// Subscription to a specific ticker has been successful.
    /// </summary>
    public sealed class TradeSubscribeAck
    {
        public TradeSubscribeAck(string tickerSymbol, TradeEventType[] events)
        {
            TickerSymbol = tickerSymbol;
            Events = events;
        }

        public string TickerSymbol { get; }

        public TradeEventType[] Events { get; }
    }

    /// <summary>
    /// Unsubscription to a specific ticker has been successful.
    /// </summary>
    public sealed class TradeUnsubscribeAck
    {
        public TradeUnsubscribeAck(string tickerSymbol, TradeEventType[] events)
        {
            TickerSymbol = tickerSymbol;
            Events = events;
        }

        public string TickerSymbol { get; }

        public TradeEventType[] Events { get; }
    }

    /// <summary>
    /// Subscription to a specific ticker was not successful.
    /// </summary>
    public sealed class TradeSubscribeNack
    {
        public TradeSubscribeNack(string tickerSymbol, TradeEventType[] events, string reason)
        {
            TickerSymbol = tickerSymbol;
            Events = events;
            Reason = reason;
        }

        public string TickerSymbol { get; }

        public TradeEventType[] Events { get; }

        public string Reason { get; }
    }
}