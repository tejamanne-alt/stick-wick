namespace ProjectInk
{
    /// <summary>Tuning for one attack. Kept in code for the prototype; moves to
    /// ScriptableObjects in the vertical slice.</summary>
    public struct AttackData
    {
        public string clip;
        public float damage, reach, knockback;
        public bool knockdown, heavy;
        public int hitStopFrames;
        public float shake;

        public static readonly AttackData Jab = new AttackData { clip = "jab", damage = 8, reach = 0.95f, knockback = 1.5f, hitStopFrames = 2, shake = 0.04f };
        public static readonly AttackData Cross = new AttackData { clip = "cross", damage = 11, reach = 1.0f, knockback = 2.5f, hitStopFrames = 3, shake = 0.06f };
        public static readonly AttackData FrontKick = new AttackData { clip = "front_kick", damage = 14, reach = 1.25f, knockback = 5f, hitStopFrames = 4, shake = 0.1f, heavy = true };
        public static readonly AttackData Roundhouse = new AttackData { clip = "roundhouse", damage = 20, reach = 1.3f, knockback = 7f, hitStopFrames = 6, shake = 0.16f, heavy = true, knockdown = true };
        public static readonly AttackData BackKick = new AttackData { clip = "back_kick", damage = 18, reach = 1.3f, knockback = 7f, hitStopFrames = 5, shake = 0.14f, heavy = true, knockdown = true };
        public static readonly AttackData FlyingKick = new AttackData { clip = "flying_kick", damage = 24, reach = 1.35f, knockback = 9f, hitStopFrames = 6, shake = 0.18f, heavy = true, knockdown = true };
        public static readonly AttackData Knee = new AttackData { clip = "knee", damage = 12, reach = 0.8f, knockback = 3f, hitStopFrames = 4, shake = 0.09f };
        public static readonly AttackData SpinKick = new AttackData { clip = "spin_kick", damage = 30, reach = 1.35f, knockback = 10f, hitStopFrames = 8, shake = 0.22f, heavy = true, knockdown = true };
        public static readonly AttackData Counter = new AttackData { clip = "cross", damage = 30, reach = 1.05f, knockback = 8f, hitStopFrames = 8, shake = 0.2f, heavy = true, knockdown = true };

        public static readonly AttackData EnemyJab = new AttackData { clip = "jab", damage = 7, reach = 1.0f, knockback = 2f, hitStopFrames = 3, shake = 0.08f };
        public static readonly AttackData EnemyCross = new AttackData { clip = "cross", damage = 10, reach = 1.05f, knockback = 3f, hitStopFrames = 3, shake = 0.1f };
        public static readonly AttackData EnemyKick = new AttackData { clip = "front_kick", damage = 12, reach = 1.3f, knockback = 4f, hitStopFrames = 4, shake = 0.12f };

        /// <summary>The hero's combo string, one step per tap (all motion-captured).</summary>
        public static readonly AttackData[] Combo = { Jab, Cross, Knee, FrontKick, Jab, Cross, Roundhouse };
    }
}
