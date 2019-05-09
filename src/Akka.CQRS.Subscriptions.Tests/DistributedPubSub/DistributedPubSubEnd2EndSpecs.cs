using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Akka.CQRS.Subscriptions.DistributedPubSub;
using Xunit;
using Xunit.Abstractions;

namespace Akka.CQRS.Subscriptions.Tests.DistributedPubSub
{
    public class DistributedPubSubEnd2EndSpecs : TestKit.Xunit2.TestKit
    {
        private static readonly Config ClusterConfig = @"
            akka.actor.provider = cluster
        ";

        public DistributedPubSubEnd2EndSpecs(ITestOutputHelper output)
                        : base(ClusterConfig, output: output) { }

        public Address SelfAddress => Cluster.Cluster.Get(Sys).SelfAddress;

        [Fact(DisplayName = "Should be able to subscribe and publish to trade event topics.")]
        public async Task ShouldSubscribeAndPublishToTradeEventTopics()
        {
            // Join the cluster
            Within(TimeSpan.FromSeconds(5), () =>
            {
                Cluster.Cluster.Get(Sys).Join(SelfAddress);
                AwaitCondition(
                    () => Cluster.Cluster.Get(Sys).State.Members.Count(x => x.Status == MemberStatus.Up) == 1);
            });

            // Start DistributedPubSub
            var subManager = DistributedPubSubTradeEventSubscriptionManager.For(Sys);
            var published = DistributedPubSubTradeEventPublisher.For(Sys);

            // Subscribe to all topics
            var subAck = await subManager.Subscribe("MSFT",
                new[] {TradeEventType.Ask, TradeEventType.Bid, TradeEventType.Fill, TradeEventType.Match}, TestActor);
        }
    }
}
