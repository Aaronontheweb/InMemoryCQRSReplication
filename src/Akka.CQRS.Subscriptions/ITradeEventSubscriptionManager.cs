// -----------------------------------------------------------------------
// <copyright file="ITradeEventSubscriptionManager.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.CQRS.Subscriptions
{
    /// <summary>
    /// Abstraction used to manage subscriptions for <see cref="ITradeEvent"/>s.
    /// </summary>
    public interface ITradeEventSubscriptionManager
    {
        Task<TradeSubscribeAck> Subscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber);

        Task<TradeUnsubscribeAck> Unsubscribe(string tickerSymbol, TradeEventType[] events, IActorRef subscriber);
    }
}