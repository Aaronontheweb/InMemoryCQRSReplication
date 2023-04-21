using System;

namespace Akka.CQRS
{
    /// <summary>
    /// Utility class for helping generate <see cref="Random"/> <see cref="decimal"/> values
    /// within a <see cref="PriceRange"/>.
    /// </summary>
    public static class PriceRangeExtensions
    {
        public static decimal WithinRange(this Random r, PriceRange range)
        {
            var sample = NextDecimalSample(r);
            return range.Max * sample + range.Min * (1 - sample);
        }

        /*
         * Random RNG algorithms provided by Jon Skeet's answer: https://stackoverflow.com/a/609529/377476
         * And by Bryan Loeper's answer: https://stackoverflow.com/a/28860710/377476
         *
         */
        public static int NextInt32(this Random rng)
        {
            var firstBits = rng.Next(0, 1 << 4) << 28;
            var lastBits = rng.Next(0, 1 << 28);
            return firstBits | lastBits;
        }

        public static decimal NextDecimalSample(this Random random)
        {
            var sample = 1m;
            //After ~200 million tries this never took more than one attempt but it is possible to generate combinations of a, b, and c with the approach below resulting in a sample >= 1.
            while (sample >= 1)
            {
                var a = random.NextInt32();
                var b = random.NextInt32();
                //The high bits of 0.9999999999999999999999999999m are 542101086.
                var c = random.Next(542101087);
                sample = new decimal(a, b, c, false, 28);
            }
            return sample;
        }
    }
}
