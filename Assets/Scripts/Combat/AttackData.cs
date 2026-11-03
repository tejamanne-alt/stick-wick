namespace ProjectInk
{
    /// <summary>Tuning for one attack. Kept in code for the prototype; moves to
    /// ScriptableObjects in the vertical slice.</summary>
    public struct AttackData
    {
        public string clip;
        public float damage, reach, knockback;
        public bool knockdown, heavy;
        public bool cleave;   // hits everyone in reach (swords), not just the target
        public bool ranged;   // a gunshot: no dash, hits the target at any distance
        public bool behind;   // strikes behind the hero without turning (back kick, back slash, back shot)
        public int hitStopFrames;
        public float shake;

        public static readonly AttackData Jab = new AttackData { clip = "jab", damage = 8, reach = 0.95f, knockback = 1.5f, hitStopFrames = 2, shake = 0.04f };
        public static readonly AttackData Cross = new AttackData { clip = "cross", damage = 11, reach = 1.0f, knockback = 2.5f, hitStopFrames = 3, shake = 0.06f };
        public static readonly AttackData FrontKick = new AttackData { clip = "front_kick", damage = 14, reach = 1.25f, knockback = 5f, hitStopFrames = 4, shake = 0.1f, heavy = true };
        public static readonly AttackData Roundhouse = new AttackData { clip = "roundhouse", damage = 20, reach = 1.3f, knockback = 7f, hitStopFrames = 6, shake = 0.16f, heavy = true, knockdown = true };
        public static readonly AttackData BackKick = new AttackData { clip = "back_kick", damage = 18, reach = 1.3f, knockback = 7f, hitStopFrames = 5, shake = 0.14f, heavy = true, knockdown = true, behind = true };
        public static readonly AttackData FlyingKick = new AttackData { clip = "flying_kick", damage = 24, reach = 1.35f, knockback = 9f, hitStopFrames = 6, shake = 0.18f, heavy = true, knockdown = true };
        public static readonly AttackData Knee = new AttackData { clip = "knee", damage = 12, reach = 0.8f, knockback = 3f, hitStopFrames = 4, shake = 0.09f };
        public static readonly AttackData SpinKick = new AttackData { clip = "spin_kick", damage = 30, reach = 1.35f, knockback = 10f, hitStopFrames = 8, shake = 0.22f, heavy = true, knockdown = true };
        public static readonly AttackData Counter = new AttackData { clip = "cross", damage = 30, reach = 1.05f, knockback = 8f, hitStopFrames = 8, shake = 0.2f, heavy = true, knockdown = true };

        // ---- sword: big arcs that cut through everyone in reach
        public static readonly AttackData SwordA = new AttackData { clip = "sword_a", damage = 20, reach = 1.45f, knockback = 3f, hitStopFrames = 3, shake = 0.07f, cleave = true };
        public static readonly AttackData SwordB = new AttackData { clip = "sword_b", damage = 24, reach = 1.5f, knockback = 4f, hitStopFrames = 4, shake = 0.1f, cleave = true };
        public static readonly AttackData SwordC = new AttackData { clip = "sword_c", damage = 40, reach = 1.75f, knockback = 7f, hitStopFrames = 7, shake = 0.18f, heavy = true, knockdown = true, cleave = true };
        public static readonly AttackData SwordBack = new AttackData { clip = "sword_back", damage = 26, reach = 1.5f, knockback = 5f, hitStopFrames = 5, shake = 0.12f, heavy = true, knockdown = true, cleave = true, behind = true };
        public static readonly AttackData SwordLeap = new AttackData { clip = "sword_leap", damage = 45, reach = 1.6f, knockback = 8f, hitStopFrames = 8, shake = 0.25f, heavy = true, knockdown = true, cleave = true };
        public static readonly AttackData[] SwordCombo = { SwordA, SwordB, SwordA, SwordB, SwordC };

        // ---- gun-fu: shots at range, pistol-whip and throw up close, a finishing shot on the ground
        public static readonly AttackData GunShoot = new AttackData { clip = "gun_shoot", damage = 34, reach = 14f, knockback = 5f, hitStopFrames = 2, shake = 0.06f, knockdown = true, ranged = true };
        public static readonly AttackData GunShootBack = new AttackData { clip = "gun_shoot_back", damage = 34, reach = 14f, knockback = 5f, hitStopFrames = 3, shake = 0.06f, knockdown = true, ranged = true, behind = true };
        public static readonly AttackData GunShootDown = new AttackData { clip = "gun_shoot_down", damage = 99, reach = 4f, knockback = 0f, hitStopFrames = 6, shake = 0.12f, heavy = true, ranged = true };
        public static readonly AttackData GunWhip = new AttackData { clip = "gun_whip", damage = 12, reach = 0.95f, knockback = 3f, hitStopFrames = 4, shake = 0.1f };
        public static readonly AttackData GunThrow = new AttackData { clip = "gun_throw", damage = 8, reach = 0.9f, knockback = 2f, hitStopFrames = 6, shake = 0.16f, heavy = true, knockdown = true };
        public const int MagazineSize = 12;

        public static readonly AttackData EnemyShot = new AttackData { clip = "gun_shoot", damage = 12, reach = 14f, knockback = 3f, hitStopFrames = 3, shake = 0.1f, ranged = true };
        public static readonly AttackData BruteSmash = new AttackData { clip = "cross", damage = 22, reach = 1.3f, knockback = 9f, hitStopFrames = 6, shake = 0.22f, heavy = true };
        public static readonly AttackData BruteKick = new AttackData { clip = "front_kick", damage = 20, reach = 1.5f, knockback = 10f, hitStopFrames = 6, shake = 0.22f, heavy = true };
        public static readonly AttackData EnemySword = new AttackData { clip = "sword_b", damage = 14, reach = 1.4f, knockback = 4f, hitStopFrames = 4, shake = 0.12f };
        public static readonly AttackData EnemyJab = new AttackData { clip = "jab", damage = 7, reach = 1.0f, knockback = 2f, hitStopFrames = 3, shake = 0.08f };
        public static readonly AttackData EnemyCross = new AttackData { clip = "cross", damage = 10, reach = 1.05f, knockback = 3f, hitStopFrames = 3, shake = 0.1f };
        public static readonly AttackData EnemyKick = new AttackData { clip = "front_kick", damage = 12, reach = 1.3f, knockback = 4f, hitStopFrames = 4, shake = 0.12f };

        /// <summary>The hero's combo string, one step per tap (all motion-captured).</summary>
        public static readonly AttackData[] Combo = { Jab, Cross, Knee, FrontKick, Jab, Cross, Roundhouse };
    }
}
