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

        [Fact(DisplayName = "MatchingEngine should generate correct events for simple orders")]
        public void MatchingEngine_should_generate_correct_order_events_for_simple_orders()
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
        }
    }
}
