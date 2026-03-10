namespace Utils
{
    public class SleepUtil
    {
        private static readonly Random Random = new Random();
        /// <summary>
        /// Returns a jitter amount centered around baseLength, a random amount up to jitter away.
        /// </summary>
        public static int GetJitter(int baseLength, int jitter)
        {
            return baseLength + ((1 - (2 * Random.Next(1))) * Random.Next(jitter));
        }

        /// <summary>
        /// Sleeps the current thread for (baseLength +/- jitter) ms
        /// </summary>
        public static void SleepJitter(int baseLength, int jitter)
        {
            Thread.Sleep(GetJitter(baseLength, jitter));
        }
    }
}