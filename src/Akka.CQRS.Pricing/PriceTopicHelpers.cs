namespace Akka.CQRS.Pricing
{
    /// <summary>
    /// Helper methods for working with price and volume updates.
    /// </summary>
    public static class PriceTopicHelpers
    {
        public static string PriceUpdateTopic(string tickerSymbol)
        {
            return $"{tickerSymbol}-price";
        }

        public static string VolumeUpdateTopic(string tickerSymbol)
        {
            return $"{tickerSymbol}-update";
        }
    }
}
