using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Events
{
    /// <summary>
    /// Matches a buy / sell-side order
    /// </summary>
    public sealed class Match : IWithStockId, ITradeEvent, IEquatable<Match>
    {
        public Match(string stockId, string buyOrderId, string sellOrderId, decimal settlementPrice, double quantity, DateTimeOffset timeStamp)
        {
            StockId = stockId;
            SettlementPrice = settlementPrice;
            Quantity = quantity;
            TimeStamp = timeStamp;
            BuyOrderId = buyOrderId;
            SellOrderId = sellOrderId;
        }

        public string StockId { get; }

        public string BuyOrderId { get; }

        public string SellOrderId { get; }

        public decimal SettlementPrice { get; }

        public double Quantity { get; }

        public DateTimeOffset TimeStamp { get; }

        public bool Equals(Match other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(StockId, other.StockId) 
                   && string.Equals(BuyOrderId, other.BuyOrderId) 
                   && string.Equals(SellOrderId, other.SellOrderId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Match other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StockId.GetHashCode();
                hashCode = (hashCode * 397) ^ BuyOrderId.GetHashCode();
                hashCode = (hashCode * 397) ^ SellOrderId.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Match left, Match right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Match left, Match right)
        {
            return !Equals(left, right);
        }
    }
}
