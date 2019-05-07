using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.CQRS.Events;
using Akka.CQRS.Util;
using Akka.Event;

namespace Akka.CQRS.Matching
{
    /// <summary>
    /// The matching engine for a single ticker symbol.
    /// </summary>
    public sealed class MatchingEngine
    {
        public static readonly IEnumerable<ITradeEvent> EmptyTradeEvents = new ITradeEvent[0];
        public static readonly IEnumerable<Order> EmptyOrders = new Order[0];

        public MatchingEngine(string stockId, ILoggingAdapter logger = null, ITimestamper timestamper = null)
        : this(stockId, new Dictionary<string, Order>(), new Dictionary<string, Order>(), logger, timestamper)
        {
        }

        public MatchingEngine(string stockId, Dictionary<string, Order> bids, Dictionary<string, Order> asks, ILoggingAdapter logger, ITimestamper timestamper)
        {
            StockId = stockId;
            _bids = bids;
            _asks = asks;
            _logger = logger;
            _timestamper = timestamper ?? CurrentUtcTimestamper.Instance;

            // create both price indicies at startup
            RebuildAskIndex();
            RebuildBidIndex();
        }

        private readonly ITimestamper _timestamper;
        private readonly ILoggingAdapter _logger;

        /// <summary>
        /// The ticker symbol for the stock being matched
        /// </summary>
        public string StockId { get; }

        private readonly Dictionary<string, Order> _bids;
        public IReadOnlyDictionary<string, Order> BidTrades => _bids;

        private readonly Dictionary<string, Order> _asks;
        public IReadOnlyDictionary<string, Order> AskTrades => _asks;

        public SortedSet<Order> AsksByPrice { get; private set; }

        public SortedSet<Order> BidsByPrice { get; private set; }

        public IEnumerable<ITradeEvent> WithAsk(Ask a)
        {
            if (AskTrades.ContainsKey(a.OrderId))
            {
                _logger?.Warning("Already have trade with ID {0} recorded for symbol {1}. Ignoring duplicate Ask.", a.OrderId, a.StockId);
                return EmptyTradeEvents;
            }

            var order = a.ToOrder();

            var (hasMatch, matches) = HasMatches(order, BidsByPrice);
            if (!hasMatch) // no matches
            {
                /*
                 * Save order into our matching system and rebuild the index.
                 */
                _asks[order.OrderId] = order;
                RebuildAskIndex();
                return EmptyTradeEvents; // no new events
            }

            var events = new List<ITradeEvent>();
            var time = _timestamper.Now;

            // process all matches
            foreach (var e in matches)
            {
                var (bidFill, askFill) = FillOrders(e, order, _timestamper);

                events.Add(askFill);
                events.Add(bidFill);

                order = order.WithFill(askFill);

                // Update bid-side matching engine state
                UpdateOrder(e, bidFill, _bids);

                // generate match notification
                var match = new Match(order.StockId, order.OrderId, e.OrderId, askFill.Price, askFill.Quantity, time);
                events.Add(match);
            }

            // need to rebuild the bids that have been modified
            RebuildBidIndex();

            if (!order.Completed) // Ask was not completely filled
            {
                // need to save it back into matching engine
                _asks[order.OrderId] = order;
                RebuildAskIndex();
            }

            return events;
        }

        public IEnumerable<ITradeEvent> WithBid(Bid b)
        {
            if (BidTrades.ContainsKey(b.OrderId))
            {
                _logger?.Warning("Already have trade with ID {0} recorded for symbol {1}. Ignoring duplicate Bid.", b.OrderId, b.StockId);
                return EmptyTradeEvents;
            }

            var order = b.ToOrder();

            var (hasMatch, matches) = HasMatches(order, AsksByPrice);
            if (!hasMatch) // no matches
            {
                /*
                 * Save order into our matching system and rebuild the index.
                 */
                _bids[order.OrderId] = order;
                RebuildBidIndex();
                return EmptyTradeEvents; // no new events
            }

            var events = new List<ITradeEvent>();
            var time = _timestamper.Now;

            // process all matches
            foreach (var e in matches)
            {
                var (bidFill, askFill) = FillOrders(e, order, _timestamper);

                events.Add(askFill);
                events.Add(bidFill);

                order = order.WithFill(bidFill);

                // Update bid-side matching engine state
                UpdateOrder(e, askFill, _asks);

                // generate match notification
                var match = new Match(order.StockId, e.OrderId, order.OrderId, askFill.Price, askFill.Quantity, time);
                events.Add(match);
            }

            // need to rebuild the Asks that have been modified
            RebuildAskIndex();

            if (!order.Completed) // Ask was not completely filled
            {
                // need to save it back into matching engine
                _bids[order.OrderId] = order;
                RebuildBidIndex();
            }

            return events;
        }

        private static void UpdateOrder(Order bid, Fill bidFill, Dictionary<string, Order> orderBook)
        {
            var newBid = bid.WithFill(bidFill);
            if (newBid.Completed) // order was completely filled
            {
                // remove from matching engine - can't be matched again
                orderBook.Remove(newBid.OrderId);
                return;
            }

            // order was only partially filled. Need to keep it in matching engine
            orderBook[newBid.OrderId] = newBid;
        }

        /*
         * Internal implementation note: these price indicies would not scale
         * in a real trading system with hundreds of thousands of open orders per symbol,
         * since we're sorting the entire order-book via LINQ on read.
         *
         * Better way to do this would probably be to update the index on write using something
         * with a constant insertion time, i.e. a Dictionary<price, Order> or something
         * more along those lines.
         */
        private void RebuildAskIndex()
        {
            // highest possible buy == front of the list
            AsksByPrice = new SortedSet<Order>(_asks.Values.OrderByDescending(x => x, OrderPriceComparer.Instance));
        }

        private void RebuildBidIndex()
        {
            // lowest possible sell == front of list
            BidsByPrice = new SortedSet<Order>(_bids.Values.OrderBy(x => x, OrderPriceComparer.Instance));
        }

        /// <summary>
        /// Fill a matching bid and ask.
        /// </summary>
        /// <param name="bid">Buy-side order.</param>
        /// <param name="ask">Sell-side order.</param>
        /// <param name="timeService">Optional. The time-stamping service. Can be overloaded for testing purposes.</param>
        /// <returns>A set of <see cref="Fill"/> and <see cref="Match"/> events.</returns>
        public static (Fill bidFill, Fill askFill) FillOrders(Order bid, Order ask, ITimestamper timeService = null)
        {
            timeService = timeService ?? CurrentUtcTimestamper.Instance;
            var settlementPrice = bid.Price;

            // pick the lower of the two values - if I'm buying 5 but was only sold 2, actual is 2.
            var actualSold = Math.Min(bid.RemainingQuantity, ask.RemainingQuantity);

            // generate a fill for each order
            var time = timeService.Now;
            var sellFill = new Fill(ask.OrderId, actualSold, settlementPrice, bid.OrderId, time);
            var buyFill = new Fill(bid.OrderId, actualSold, settlementPrice, ask.OrderId, time);

            return (buyFill, sellFill);
        }

        /// <summary>
        /// Find matches on the opposing side of the trade, aiming for profit maximization.
        ///
        /// If we're buying, we want the cheapest possible price.
        ///
        /// If we're selling, we want the highest possible price.
        /// </summary>
        /// <param name="o">The current trade.</param>
        /// <param name="oppositeSide">All of the trades on the opposite side of the order book.</param>
        /// <returns>A tuple indicating whether or not any matches were found and the matching orders, in the event that there are many.</returns>
        public static (bool hasMatch, IEnumerable<Order> matches) HasMatches(Order o, SortedSet<Order> oppositeSide)
        {
            if (oppositeSide.Any(x => o.Match(x)))
            {
                var matches = new List<Order>();
                var remainingQuantity = o.RemainingQuantity;
                foreach (var oppositeOrder in oppositeSide.Where(x => x.Match(o)))
                {
                    matches.Add(oppositeOrder);
                    remainingQuantity -= oppositeOrder.RemainingQuantity;
                    if (remainingQuantity <= 0.0)
                        break;
                }
                return (true, matches);
            }

            // no matches
            return (false, EmptyOrders);
        }
    }
}
