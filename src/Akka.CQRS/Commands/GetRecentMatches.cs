namespace Akka.CQRS.Commands
{
    /// <summary>
    /// Query an order book for the set of recent matches
    /// </summary>
    public sealed class GetRecentMatches : IWithStockId
    {
        public GetRecentMatches(string stockId)
        {
            StockId = stockId;
        }

        public string StockId { get; }
    }
}
