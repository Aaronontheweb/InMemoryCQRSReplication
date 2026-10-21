using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.CQRS.Events;
using FluentAssertions;
using Xunit;

namespace Akka.CQRS.Matching.Tests
{
    public class MatchingEngineSpecs
    {
        public const string TickerSymbol = "PTB";
        private readonly MatchingEngine _matchingEngine = new MatchingEngine(TickerSymbol);

        [Fact(DisplayName = "MatchingEngine should generate correct events for ask then bid")]
        public void MatchingEngine_should_generate_correct_order_events_for_ask_then_bid()
        {
            var ask = new Ask(TickerSymbol, "foo", 10.0m, 4.0, DateTimeOffset.Now);

            // bid will be slightly higher than ask - settlement price should be bid price
            var bid = new Bid(TickerSymbol, "bar", 11.0m, 4.0, DateTimeOffset.Now);

            var askEvents = _matchingEngine.WithAsk(ask);
            askEvents.Should().BeEmpty();
            _matchingEngine.AskTrades.Count.Should().Be(1);
            
            var bidEvents = _matchingEngine.WithBid(bid).ToList();

            // validate the correct number of outputs and remaining orders first
            bidEvents.Should().NotBeEmpty();
            bidEvents.Count.Should().Be(3);

            _matchingEngine.AskTrades.Count.Should().Be(0);
            _matchingEngine.AsksByPrice.Count.Should().Be(0);
            _matchingEngine.BidTrades.Count.Should().Be(0);
            _matchingEngine.BidsByPrice.Count.Should().Be(0);

            // all orders should have been completely filled
            var fills = bidEvents.Where(x => x is Fill).Cast<Fill>().ToList();
            fills[0].Partial.Should().BeFalse();
            fills[1].Partial.Should().BeFalse();
            
            // filled price should be the bid price
            fills[0].Price.Should().Be(bid.BidPrice);

            // match information should reflect the same
            var match = (Match)bidEvents.Single(x => x is Match);
            match.BuyOrderId.Should().Be(bid.OrderId);
            match.SellOrderId.Should().Be(ask.OrderId);
            match.StockId.Should().Be(TickerSymbol);
            match.SettlementPrice.Should().Be(bid.BidPrice);
            match.Quantity.Should().Be(bid.BidQuantity);
        }

        [Fact(DisplayName = "MatchingEngine should generate correct events for bid then ask")]
        public void MatchingEngine_should_generate_correct_order_events_for_bid_then_ask()
        {
            var ask = new Ask(TickerSymbol, "foo", 10.0m, 4.0, DateTimeOffset.Now);

            // bid will be slightly higher than ask - settlement price should be bid price
            var bid = new Bid(TickerSymbol, "bar", 11.0m, 4.0, DateTimeOffset.Now);

            var bidEvents = _matchingEngine.WithBid(bid);
            bidEvents.Should().BeEmpty();
            _matchingEngine.BidTrades.Count.Should().Be(1);

            var askEvents = _matchingEngine.WithAsk(ask).ToList();

            // validate the correct number of outputs and remaining orders first
            askEvents.Should().NotBeEmpty();
            askEvents.Count.Should().Be(3);

            _matchingEngine.AskTrades.Count.Should().Be(0);
            _matchingEngine.AsksByPrice.Count.Should().Be(0);
            _matchingEngine.BidTrades.Count.Should().Be(0);
            _matchingEngine.BidsByPrice.Count.Should().Be(0);

            // all orders should have been completely filled
            var fills = askEvents.Where(x => x is Fill).Cast<Fill>().ToList();
            fills[0].Partial.Should().BeFalse();
            fills[1].Partial.Should().BeFalse();

            // filled price should be the bid price
            fills[0].Price.Should().Be(bid.BidPrice);

            // match information should reflect the same
            var match = (Match)askEvents.Single(x => x is Match);
            match.BuyOrderId.Should().Be(bid.OrderId);
            match.SellOrderId.Should().Be(ask.OrderId);
            match.StockId.Should().Be(TickerSymbol);
            match.SettlementPrice.Should().Be(bid.BidPrice);
            match.Quantity.Should().Be(bid.BidQuantity);
        }

        [Fact(DisplayName = "Should create valid OrderbookSnapshot and recreate matching engine from snapshot")]
        public void ShouldCreateAndRecreateFromOrderbookSnapshot()
        {
            var ask = new Ask(TickerSymbol, "foo", 12.0m, 5.0, DateTimeOffset.Now);

            // bid is lower than ask - no trades
            var bid = new Bid(TickerSymbol, "bar", 11.0m, 4.0, DateTimeOffset.Now);

            var bidEvents = _matchingEngine.WithBid(bid);
            var askEvents = _matchingEngine.WithAsk(ask);
            askEvents.Should().BeEmpty();
            bidEvents.Should().BeEmpty();

            _matchingEngine.AskTrades.Count.Should().Be(1);
            _matchingEngine.BidTrades.Count.Should().Be(1);

            // create a snapshot
            var snapshot = _matchingEngine.GetSnapshot();

            // add a second ask to matching engine (need to verify immutability)
            // still higher than original bid - won't trigger any trades
            var ask2 = new Ask(TickerSymbol, "fuber", 13.0m, 5.0, DateTimeOffset.Now);
            var ask2Events = _matchingEngine.WithAsk(ask2);
            ask2Events.Should().BeEmpty();

            snapshot.Asks.Count.Should().Be(1);
            snapshot.Bids.Count.Should().Be(1);
            snapshot.StockId.Should().Be(_matchingEngine.StockId);
            _matchingEngine.AskTrades.Count.Should().Be(2);
        }

        [Fact(DisplayName = "Orders at the same price should remain distinct and match on both sides")]
        public void MatchingEngine_should_retain_orders_with_equal_prices()
        {
            var time = DateTimeOffset.UtcNow;

            var asks = new MatchingEngine(TickerSymbol);
            asks.WithAsk(new Ask(TickerSymbol, "ask-1", 10.0m, 2.0d, time)).Should().BeEmpty();
            asks.WithAsk(new Ask(TickerSymbol, "ask-2", 10.0m, 2.0d, time)).Should().BeEmpty();
            asks.AskTrades.Should().HaveCount(2);
            asks.AsksByPrice.Should().HaveCount(2);

            var askSideMatches = asks.WithBid(new Bid(TickerSymbol, "bid-1", 10.0m, 4.0d, time))
                .OfType<Match>()
                .ToList();
            askSideMatches.Should().HaveCount(2);
            asks.AskTrades.Should().BeEmpty();
            asks.BidTrades.Should().BeEmpty();

            var bids = new MatchingEngine(TickerSymbol);
            bids.WithBid(new Bid(TickerSymbol, "bid-2", 10.0m, 2.0d, time)).Should().BeEmpty();
            bids.WithBid(new Bid(TickerSymbol, "bid-3", 10.0m, 2.0d, time)).Should().BeEmpty();
            bids.BidTrades.Should().HaveCount(2);
            bids.BidsByPrice.Should().HaveCount(2);

            var bidSideMatches = bids.WithAsk(new Ask(TickerSymbol, "ask-3", 10.0m, 4.0d, time))
                .OfType<Match>()
                .ToList();
            bidSideMatches.Should().HaveCount(2);
            bids.BidTrades.Should().BeEmpty();
            bids.AskTrades.Should().BeEmpty();
        }

        [Fact(DisplayName = "Matching uses the best price first on both sides")]
        public void MatchingEngine_should_use_best_price_first()
        {
            var time = DateTimeOffset.UtcNow;

            var bidBook = new MatchingEngine(TickerSymbol);
            bidBook.WithBid(new Bid(TickerSymbol, "bid-low", 11.0m, 1.0d, time));
            bidBook.WithBid(new Bid(TickerSymbol, "bid-high", 12.0m, 1.0d, time));
            var bidMatch = bidBook.WithAsk(new Ask(TickerSymbol, "ask", 10.0m, 1.0d, time))
                .OfType<Match>()
                .Single();
            bidMatch.BuyOrderId.Should().Be("bid-high");

            var askBook = new MatchingEngine(TickerSymbol);
            askBook.WithAsk(new Ask(TickerSymbol, "ask-high", 12.0m, 1.0d, time));
            askBook.WithAsk(new Ask(TickerSymbol, "ask-low", 11.0m, 1.0d, time));
            var askMatch = askBook.WithBid(new Bid(TickerSymbol, "bid", 13.0m, 1.0d, time))
                .OfType<Match>()
                .Single();
            askMatch.SellOrderId.Should().Be("ask-low");
        }
    }
}
