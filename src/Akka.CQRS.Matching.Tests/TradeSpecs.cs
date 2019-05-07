using System;
using Akka.CQRS.Events;
using FluentAssertions;
using Xunit;

namespace Akka.CQRS.Matching.Tests
{
    public class TradeSpecs
    {
        [Fact(DisplayName = "Sell-side trades should be able to be completely filled")]
        public void SellTradesShouldCompletelyFill()
        {
            var ask = new Ask("MSFT", "foo", 10.0m, 2.0d, DateTimeOffset.UtcNow);
            var trade = ask.ToTrade();

            var fill = new Fill(ask.TradeId, ask.AskQuantity, ask.AskPrice, "bar", DateTimeOffset.UtcNow);

            var filledTrade = trade.WithFill(fill);
            filledTrade.Completed.Should().BeTrue();
            filledTrade.RemainingQuantity.Should().Be(0.0D);
        }

        [Fact(DisplayName = "Sell-side trades should be able to be partially filled")]
        public void SellTradesShouldPartiallyFill()
        {
            var ask = new Ask("MSFT", "foo", 10.0m, 2.0d, DateTimeOffset.UtcNow);
            var trade = ask.ToTrade();

            // partial fill
            var fill = new Fill(ask.TradeId, ask.AskQuantity - 1.0d, ask.AskPrice, "bar", DateTimeOffset.UtcNow);

            var filledTrade = trade.WithFill(fill);
            filledTrade.Completed.Should().BeFalse();
            filledTrade.RemainingQuantity.Should().Be(1.0D);
        }

        [Fact(DisplayName = "Buy-side trades should be able to be completely filled")]
        public void BuyTradesShouldCompletelyFill()
        {
            var bid = new Bid("MSFT", "foo", 10.0m, 2.0d, DateTimeOffset.UtcNow);
            var trade = bid.ToTrade();

            var fill = new Fill(bid.TradeId, bid.BidQuantity, bid.BidPrice, "bar", DateTimeOffset.UtcNow);

            var filledTrade = trade.WithFill(fill);
            filledTrade.Completed.Should().BeTrue();
            filledTrade.RemainingQuantity.Should().Be(0.0D);
        }

        [Fact(DisplayName = "Buy-side trades should be able to be partially filled")]
        public void BuyTradesShouldPartiallyFill()
        {
            var bid = new Bid("MSFT", "foo", 10.0m, 2.0d, DateTimeOffset.UtcNow);
            var trade = bid.ToTrade();

            var fill = new Fill(bid.TradeId, bid.BidQuantity - 1.0d, bid.BidPrice, "bar", DateTimeOffset.UtcNow);

            var filledTrade = trade.WithFill(fill);
            filledTrade.Completed.Should().BeFalse();
            filledTrade.RemainingQuantity.Should().Be(1.0D);
        }
    }
}
