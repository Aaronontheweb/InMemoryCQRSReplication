namespace Akka.CQRS
{
    /// <summary>
    /// Utility class for naming some of our persistent entities
    /// </summary>
    public static class EntityIdHelper
    {
        public const string OrderBookSuffix = "-orderBook";
        public const string PriceSuffix = "-prices";

        public static string IdForOrderBook(string tickerSymbol)
        {
            return tickerSymbol + OrderBookSuffix;
        }

        public static string ExtractTickerFromPersistenceId(string persistenceId)
        {
            return persistenceId.Split('-')[0];
        }

        public static string IdForPricing(string tickerSymbol)
        {
            return tickerSymbol + PriceSuffix;
        }
    }
}
