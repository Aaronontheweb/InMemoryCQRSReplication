using System;
using Akka.Cluster.Sharding;

namespace Akka.CQRS.Infrastructure
{
    /// <summary>
    /// Used to route sharding messages to order book actors hosted via Akka.Cluster.Sharding.
    /// </summary>
    public sealed class StockShardMsgRouter : HashCodeMessageExtractor
    {
        /// <summary>
        /// 5 nodes hosting order books, 10 shards per node.
        /// </summary>
        public const int DefaultShardCount = 50;

        public StockShardMsgRouter() : this(DefaultShardCount)
        {
        }

        public StockShardMsgRouter(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            if (message is IWithStockId stockMsg)
            {
                return stockMsg.StockId;
            }

            return null;
        }
    }
}
