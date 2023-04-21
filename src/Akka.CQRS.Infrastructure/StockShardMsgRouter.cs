using Akka.Cluster.Sharding;
using Akka.CQRS.Events;
using Akka.Persistence.Extras;

namespace Akka.CQRS.Infrastructure
{
    /// <summary>
    /// Used to route sharding messages to order book actors hosted via Akka.Cluster.Sharding.
    /// </summary>
    public sealed class StockShardMsgRouter : HashCodeMessageExtractor
    {
        /// <summary>
        /// 3 nodes hosting order books, 10 shards per node.
        /// </summary>
        public const int DefaultShardCount = 30;

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

            switch (message)
            {
                case ConfirmableMessage<Ask> a:
                    return a.Message.StockId;
                case ConfirmableMessage<Bid> b:
                    return b.Message.StockId;
                case ConfirmableMessage<Fill> f:
                    return f.Message.StockId;
                case ConfirmableMessage<Match> m:
                    return m.Message.StockId;
            }

            return null;
        }
    }
}
