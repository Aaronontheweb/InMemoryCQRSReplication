namespace Akka.CQRS
{
    /// <summary>
    /// Represents a price band, typically weighted by buy/sell volume.
    /// </summary>
    public struct PriceRange
    {
        public PriceRange(decimal min, decimal mean, decimal max)
        {
            Min = min;
            Mean = mean;
            Max = max;
        }

        public decimal Min { get; }

        public decimal Mean { get; }

        public decimal Max { get; }
    }
}