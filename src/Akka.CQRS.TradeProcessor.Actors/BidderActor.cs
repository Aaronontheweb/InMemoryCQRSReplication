using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.CQRS.Events;
using Akka.CQRS.Subscriptions;
using Akka.CQRS.Subscriptions.DistributedPubSub;
using Akka.CQRS.Util;
using Akka.Event;
using Akka.Persistence.Extras;
using Akka.Util;

namespace Akka.CQRS.TradeProcessor.Actors
{
    /// <summary>
    /// Actor that randomly places bids for a specific ticker symbol.
    /// </summary>
    public sealed class BidderActor : ReceiveActor
    {
        private readonly string _tickerSymbol;
        private readonly string _traderId;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly ITradeEventSubscriptionManager _subscriptionManager;
        private readonly ITradeOrderIdGenerator _tradeOrderIdGenerator;
        private readonly ITimestamper _timestampGenerator;

        // tradeGateway is usefully going to be a Cluster.Sharding.RegionProxy
        private readonly IActorRef _tradeGateway;
        private readonly PriceRange _targetRange;
        private readonly Dictionary<string, Bid> _bids = new Dictionary<string, Bid>();
        private readonly List<Fill> _fills = new List<Fill>();
        private long _confirmationId = 0;
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

        public BidderActor(string tickerSymbol, PriceRange targetRange, IActorRef tradeGateway)
            : this(tickerSymbol, DistributedPubSubTradeEventSubscriptionManager.For(Context.System), tradeGateway, targetRange,
                GuidTradeOrderIdGenerator.Instance, CurrentUtcTimestamper.Instance)
        { }

        public BidderActor(string tickerSymbol, ITradeEventSubscriptionManager subscriptionManager, 
            IActorRef tradeGateway, PriceRange targetRange, ITradeOrderIdGenerator tradeOrderIdGenerator, 
            ITimestamper timestampGenerator)
        {
            _tickerSymbol = tickerSymbol;
            _subscriptionManager = subscriptionManager;
            _tradeGateway = tradeGateway;
            _targetRange = targetRange;
            _tradeOrderIdGenerator = tradeOrderIdGenerator;
            _timestampGenerator = timestampGenerator;
            _traderId = $"{_tickerSymbol}-{_tradeOrderIdGenerator.NextId()}";
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
                var bid = CreateBid();
                _bids[bid.OrderId] = bid;
                _tradeGateway.Tell(new ConfirmableMessage<Bid>(bid, _confirmationId++, _traderId));
                _log.Info("BID ${0} for {1} units of {2}", bid.BidPrice, bid.BidQuantity, _tickerSymbol);
            });

            Receive<Fill>(f => _bids.ContainsKey(f.OrderId), f =>
            {
                _fills.Add(f);
                _log.Info("Received FILL for BID order {0} of {1} stock @ ${2} per unit for {3} units", f.OrderId, f.StockId, f.Price, f.Quantity);
                _log.Info("We now own {0} units of {1} at AVG price of {2}", _fills.Sum(x => x.Quantity), _tickerSymbol, _fills.Average(x => (decimal)x.Quantity * x.Price));
            });
        }

        private Bid CreateBid()
        {
            var price = ThreadLocalRandom.Current.WithinRange(_targetRange);
            var quantity = ThreadLocalRandom.Current.Next(1, 20);
            var orderId = _tradeOrderIdGenerator.NextId();
            var bid = new Bid(_tickerSymbol, orderId, price, quantity, _timestampGenerator.Now);
            return bid;
        }

        protected override void PostStop()
        {
            _bidInterval?.Cancel();
        }
    }
}
