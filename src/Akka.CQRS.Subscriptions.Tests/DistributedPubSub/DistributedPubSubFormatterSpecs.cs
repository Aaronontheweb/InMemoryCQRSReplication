using System;
using System.Collections.Generic;
using FluentAssertions;
using static Akka.CQRS.Subscriptions.DistributedPubSub.DistributedPubSubTopicFormatter;
using Xunit;

namespace Akka.CQRS.Subscriptions.Tests.DistributedPubSub
{
    public class DistributedPubSubFormatterSpecs
    {
        public static IEnumerable<object[]> GetTradeTopics()
        {
            yield return new object[] { "msft", TradeEventType.Ask, "msft-Ask" };
            yield return new object[] { "msft", TradeEventType.Bid, "msft-Bid" };
            yield return new object[] { "msft", TradeEventType.Match, "msft-Match" };
            yield return new object[] { "msft", TradeEventType.Fill, "msft-Fill" };
        }

        [Theory(DisplayName = "Should format name of ticker symbol + event in the format expected by DistributedPubSub")]
        [MemberData(nameof(GetTradeTopics))]
        public void ShouldFormatDistributedPubSubTopic(string ticker, TradeEventType tradeEvent, string expectedTopic)
        {
            ToTopic(ticker, tradeEvent).Should().Be(expectedTopic);
        }
    }
}
