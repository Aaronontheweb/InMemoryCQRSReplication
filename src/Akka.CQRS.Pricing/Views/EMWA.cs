using System;

namespace Akka.CQRS.Pricing
{
    /// <summary>
    ///     Simple data structure for self-contained EMWA mathematics.
    /// </summary>
    public struct EMWA
    {
        public EMWA(double alpha, double currentAvg)
        {
            Alpha = alpha;
            CurrentAvg = currentAvg;
        }

        public double Alpha { get; }

        public double CurrentAvg { get; }

        public EMWA Next(double nextValue)
        {
            return new EMWA(Alpha, Alpha * nextValue + (1 - Alpha) * CurrentAvg);
        }

        public static EMWA Init(int sampleSize, double firstReading)
        {
            var alpha = 2.0 / (sampleSize + 1);
            return new EMWA(alpha, firstReading);
        }

        public static double operator %(EMWA e1, EMWA e2)
        {
            return (e1.CurrentAvg - e2.CurrentAvg) / e1.CurrentAvg;
        }

        public static EMWA operator +(EMWA e1, double next)
        {
            return e1.Next(next);
        }
    }

    /// <summary>
    ///     Simple data structure for self-contained EMWA mathematics using <see cref="decimal"/> precision.
    /// </summary>
    public struct EMWAm
    {
        public EMWAm(decimal alpha, decimal currentAvg)
        {
            Alpha = alpha;
            CurrentAvg = currentAvg;
        }

        public decimal Alpha { get; }

        public decimal CurrentAvg { get; }

        public EMWAm Next(decimal nextValue)
        {
            return new EMWAm(Alpha, Alpha * nextValue + (1 - Alpha) * CurrentAvg);
        }

        public static EMWAm Init(int sampleSize, decimal firstReading)
        {
            var alpha = 2.0m / (sampleSize + 1);
            return new EMWAm(alpha, firstReading);
        }

        public static decimal operator %(EMWAm e1, EMWAm e2)
        {
            return (e1.CurrentAvg - e2.CurrentAvg) / e1.CurrentAvg;
        }

        public static EMWAm operator +(EMWAm e1, decimal next)
        {
            return e1.Next(next);
        }
    }
}
