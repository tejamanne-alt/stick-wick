namespace ProjectInk
{
    /// <summary>The only randomness that may touch gameplay: wave make-up, spawn timing and enemy
    /// spacing. It is seeded at the start of every wave (by the floor and wave number), so a
    /// wave arrives the same way on every attempt and can be learnt. Hero move choice and hit
    /// results use no randomness at all. Cosmetics (sparks, ink, sound variants) keep using
    /// UnityEngine.Random.</summary>
    public static class CombatRandom
    {
        static System.Random rng = new System.Random(1);

        public static void Seed(int seed) => rng = new System.Random(seed);

        /// <summary>0 (inclusive) .. 1 (exclusive).</summary>
        public static float Value => (float)rng.NextDouble();

        public static float Range(float min, float max) => min + (max - min) * Value;

        /// <summary>min (inclusive) .. max (exclusive), like UnityEngine.Random.Range for ints.</summary>
        public static int Range(int min, int max) => max <= min ? min : rng.Next(min, max);
    }
}
