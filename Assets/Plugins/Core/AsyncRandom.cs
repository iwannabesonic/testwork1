using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Core.Async
{
    public class AsyncRandom
    {
        private static RandomCore core = new RandomCore();
        public static int Next() => core.Next();
        public static int Next(int minVal, int maxVal) => core.Next(minVal, maxVal);
        public static void Regenerate() => core.Regenerate();
        public static double NextDouble() => core.NextDouble();
        /// <summary>
        /// Generate pseudo random number from 0.0 [include] to 1.0 [include]
        /// </summary>
        public static float Value => core.Value;

        private class RandomCore : IDisposable
        {
            private RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            private readonly byte[] fillArray = new byte[4];
            private Random random;
            public RandomCore()
            {
                Regenerate();
            }
            public void Regenerate()
            {
                provider.GetBytes(fillArray);
                int seed = BitConverter.ToInt32(fillArray,0);
                random = new Random(seed);
            }

            public int Next() => random.Next();
            public int Next(int minVal, int maxVal) => random.Next(minVal, maxVal);
            public double NextDouble() => random.NextDouble();

            public void Dispose()
            {
                ((IDisposable)provider).Dispose();
            }

            /// <summary>
            /// Generate pseudo random number from 0.0 [include] to 1.0 [include]
            /// </summary>
            public float Value
            {
                get
                {
                    provider.GetBytes(fillArray);
                    var unclamped = BitConverter.ToUInt32(fillArray, 0);
                    return unclamped / (float)uint.MaxValue;
                }
            }
        }
    }
}
