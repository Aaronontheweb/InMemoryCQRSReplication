using System;
using Akka.CQRS.Events;
using FluentAssertions;
using Xunit;

namespace Akka.CQRS.Tests
{
    public class OrderSpecs
    {
        [Fact(DisplayName = "Sell-side orders should be able to be completely filled")]
        public void SellTradesShouldCompletelyFill()
        {
            var ask = new Ask("MSFT", "foo", 10.0m, 2.0d, DateTimeOffset.UtcNow);
            var trade = ask.ToOrder();

            var fill = new Fill(ask.OrderId, ask.AskQuantity, ask.AskPrice, "bar", DateTimeOffset.UtcNow);

            var filledTrade = trade.WithFill(fill);
            filledTrade.Completed.Should().BeTrue();
            filledTrade.RemainingQuantity.Should().Be(0.0D);
        }

        [Fact(DisplayName = "Sell-side orders should be able to be partially filled")]
        public void SellTradesShouldPartiallyFill()
        {
            var ask = new Ask("MSFT", "foo", 10.0m, 2.0d, DateTimeOffset.UtcNow);
            var trade = ask.ToOrder();

            // partial fill
            var fill = new Fill(ask.OrderId, ask.AskQuantity - 1.0d, ask.AskPrice, "bar", DateTimeOffset.UtcNow);

            var filledTrade = trade.WithFill(fill);
            filledTrade.Completed.Should().BeFalse();
            filledTrade.RemainingQuantity.Should().Be(1.0D);
        }

        [Fact(DisplayName = "Buy-side orders should be able to be completely filled")]
        public void BuyTradesShouldCompletelyFill()
        {
            var bid = new Bid("MSFT", "foo", 10.0m, 2.0d, DateTimeOffset.UtcNow);
            var trade = bid.ToOrder();

            var fill = new Fill(bid.OrderId, bid.BidQuantity, bid.BidPrice, "bar", DateTimeOffset.UtcNow);

            var filledTrade = trade.WithFill(fill);
            filledTrade.Completed.Should().BeTrue();
            filledTrade.RemainingQuantity.Should().Be(0.0D);
        }

        [Fact(DisplayName = "Buy-side orders should be able to be partially filled")]
        public void BuyTradesShouldPartiallyFill()
        {
            var bid = new Bid("MSFT", "foo", 10.0m, 2.0d, DateTimeOffset.UtcNow);
            var trade = bid.ToOrder();

            var fill = new Fill(bid.OrderId, bid.BidQuantity - 1.0d, bid.BidPrice, "bar", DateTimeOffset.UtcNow);

            var filledTrade = trade.WithFill(fill);
            filledTrade.Completed.Should().BeFalse();
            filledTrade.RemainingQuantity.Should().Be(1.0D);
        }

        [Theory(DisplayName = "Complementary orders should match correctly")]
        [InlineData(1.0, 1.0, true)]
        [InlineData(2.0, 1.0, true)]
        [InlineData(1.0, 2.0, false)]
        public void OrdersShouldMatch(decimal bidPrice, decimal askPrice, bool match)
        {
            var ask = new Ask("PTB", "foo", askPrice, 1.0d, DateTimeOffset.UtcNow);
            var bid = new Bid("PTB", "bar", bidPrice, 1.0d, DateTimeOffset.UtcNow);

            var aOrder = ask.ToOrder();
            var bOrder = bid.ToOrder();

            // matching rules must be consistent in both directions
            aOrder.Match(bOrder).Should().Be(match);
            bOrder.Match(aOrder).Should().Be(match);
        }
    }
}
