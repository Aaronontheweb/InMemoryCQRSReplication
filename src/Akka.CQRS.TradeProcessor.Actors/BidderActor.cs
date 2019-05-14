using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.CQRS.Events;
using Akka.CQRS.Subscriptions;
using Akka.Event;
using Akka.Util;

namespace Akka.CQRS.TradeProcessor.Actors
{
    /// <summary>
    /// Actor that randomly places bids for a specific ticker symbol.
    /// </summary>
    public sealed class BidderActor : ReceiveActor
    {
        private readonly string _tickerSymbol;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly ITradeEventSubscriptionManager _subscriptionManager;

        // tradeGateway is usefully going to be a Cluster.Sharding.RegionProxy
        private readonly IActorRef _tradeGateway;
        private readonly PriceRange _targetRange;
        private readonly Dictionary<string, Bid> _bids = new Dictionary<string, Bid>();
        private readonly List<Fill> _fills = new List<Fill>();
        private ICancelable _bidInterval;

        private class DoSubscribe
        {
            public static readonly DoSubscribe Instance = new DoSubscribe();
            private DoSubscribe() { }
        }

        private class DoBid
        {
            public static readonly DoBid Instance = new DoBid();
            private DoBid() { }
        }

        public BidderActor(string tickerSymbol, ITradeEventSubscriptionManager subscriptionManager, IActorRef tradeGateway, PriceRange targetRange)
        {
            _tickerSymbol = tickerSymbol;
            _subscriptionManager = subscriptionManager;
            _tradeGateway = tradeGateway;
            _targetRange = targetRange;
            Self.Tell(DoSubscribe.Instance);
            Subscribing();
        }

        private void Subscribing()
        {
            ReceiveAsync<DoSubscribe>(async _ =>
                {
                    try
                    {
                        var ack = await _subscriptionManager.Subscribe(_tickerSymbol, TradeEventType.Fill, Self);
                        Become(Bidding);
                        _bidInterval = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(10), Self, DoBid.Instance, ActorRefs.NoSender);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error while waiting for SubscribeAck for [{0}-{1}] - retrying in 5s.", _tickerSymbol, TradeEventType.Fill);
                        Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(5), Self, DoSubscribe.Instance, ActorRefs.NoSender);
                    }
                });
        }

        private void Bidding()
        {
            // Time to place a new bid
            Receive<DoBid>(_ =>
            {
                var price = ThreadLocalRandom.Current.WithinRange(_targetRange);
                var quantity = ThreadLocalRandom.Current.Next(1, 20);
                
            });
        }

        protected override void PostStop()
        {
            _bidInterval?.Cancel();
        }
    }
}
