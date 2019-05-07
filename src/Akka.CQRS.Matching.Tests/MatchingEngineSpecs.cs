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
    }
}
