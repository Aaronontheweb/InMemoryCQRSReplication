using System;
using System.Collections;
using System.Collections.Generic;
using Akka.CQRS.Events;
using FluentAssertions;
using Xunit;

namespace Akka.CQRS.Subscriptions.Tests
{
    public class TradeEventExtensionsSpecs
    {
        public static IEnumerable<object[]> GetTradeEvents()
        {
            yield return new object[] {new Ask("foo", "foo", 10.0m, 1.0d, DateTimeOffset.UtcNow), TradeEventType.Ask};
            yield return new object[] { new Bid("foo", "foo", 10.0m, 1.0d, DateTimeOffset.UtcNow), TradeEventType.Bid };
            yield return new object[] { new Fill("foo", 1.0d, 10.0m, "bar", DateTimeOffset.UtcNow), TradeEventType.Fill };
            yield return new object[] { new Match("foo", "bar", "fuber", 10.0m, 1.0d, DateTimeOffset.UtcNow), TradeEventType.Match };

        }

        [Theory(DisplayName = "Should detect correct TradeEventType for ITradeEvent")]
        [MemberData(nameof(GetTradeEvents))]
        public void ShouldMatchEventWithTradeType(ITradeEvent tradeEvent, TradeEventType expectedType)
        {
            tradeEvent.ToTradeEventType().Should().Be(expectedType);
        }
    }
}
