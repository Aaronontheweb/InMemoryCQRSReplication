using System;

namespace Akka.CQRS.Pricing.Commands
{
    /// <summary>
    /// Used to heartbeat an Akka.Cluster.Sharding entity for a specific ticker symbol.
    /// </summary>
    public sealed class Ping : IWithStockId, IEquatable<Ping>
    {
        public Ping(string stockId)
        {
            StockId = stockId;
        }

        public string StockId { get; }

        public bool Equals(Ping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(StockId, other.StockId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Ping other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StockId.GetHashCode();
        }

        public static bool operator ==(Ping left, Ping right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Ping left, Ping right)
        {
            return !Equals(left, right);
        }
    }
}
