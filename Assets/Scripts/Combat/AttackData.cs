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
        public float lift;    // launches the victim upwards
        public bool iai;      // the draw-cut: dashes through everyone in line, the cut lands a beat later
        public string react;  // how the man hit reacts (a clip name), empty = hit_front / hit_back
        public string name;   // a named combo's finisher: shown when it lands

        /// <summary>The same move as the last of a named string: it floors him, and its name is shown.</summary>
        public AttackData Finisher(string label)
        {
            var a = this;
            a.knockdown = true; a.heavy = true; a.name = label;
            a.damage *= 1.25f; a.knockback = UnityEngine.Mathf.Max(a.knockback, 6f);
            a.hitStopFrames += 2; a.shake += 0.05f;
            return a;
        }

        // ---- CONTACT (story mode): grounded strikes from karate, boxing and muay thai
        public static readonly AttackData SJab = new AttackData { clip = "jab", damage = 8, reach = 0.95f, knockback = 1.2f, hitStopFrames = 2, shake = 0.04f, react = "hit_front" };
        public static readonly AttackData SCross = new AttackData { clip = "cross", damage = 11, reach = 1.0f, knockback = 2f, hitStopFrames = 3, shake = 0.06f, react = "hit_chin" };
        public static readonly AttackData LeadHook = new AttackData { clip = "lead_hook", damage = 12, reach = 0.9f, knockback = 2f, hitStopFrames = 4, shake = 0.08f, react = "hit_chin" };
        public static readonly AttackData OiZuki = new AttackData { clip = "oi_zuki", damage = 13, reach = 1.15f, knockback = 3f, hitStopFrames = 4, shake = 0.09f, heavy = true, react = "hit_body" };
        public static readonly AttackData GyakuZuki = new AttackData { clip = "gyaku_zuki", damage = 14, reach = 1.0f, knockback = 3f, hitStopFrames = 4, shake = 0.1f, heavy = true, react = "hit_body" };
        public static readonly AttackData Elbow = new AttackData { clip = "elbow", damage = 15, reach = 0.75f, knockback = 2.5f, hitStopFrames = 5, shake = 0.12f, heavy = true, react = "hit_chin" };
        public static readonly AttackData BackFist = new AttackData { clip = "back_fist", damage = 9, reach = 0.95f, knockback = 1.5f, hitStopFrames = 3, shake = 0.06f, react = "hit_front" };
        public static readonly AttackData PalmStrike = new AttackData { clip = "palm_strike", damage = 18, reach = 0.85f, knockback = 3.5f, hitStopFrames = 6, shake = 0.14f, heavy = true, react = "stumble_back" };
        public static readonly AttackData HammerFist = new AttackData { clip = "hammer_fist", damage = 18, reach = 0.9f, knockback = 1f, hitStopFrames = 6, shake = 0.14f, heavy = true, react = "hit_body" };
        public static readonly AttackData LowKick = new AttackData { clip = "low_kick", damage = 10, reach = 1.15f, knockback = 1.5f, hitStopFrames = 4, shake = 0.08f, react = "hit_legs" };
        public static readonly AttackData MaeGeri = new AttackData { clip = "mae_geri", damage = 13, reach = 1.2f, knockback = 4f, hitStopFrames = 4, shake = 0.1f, heavy = true, react = "hit_body" };
        public static readonly AttackData Mawashi = new AttackData { clip = "mawashi_geri", damage = 16, reach = 1.25f, knockback = 4f, hitStopFrames = 5, shake = 0.12f, heavy = true, react = "hit_chin" };
        public static readonly AttackData SideKick = new AttackData { clip = "side_kick", damage = 18, reach = 1.3f, knockback = 6f, hitStopFrames = 5, shake = 0.14f, heavy = true, react = "stumble_back" };
        public static readonly AttackData SBackKick = new AttackData { clip = "back_kick", damage = 16, reach = 1.25f, knockback = 5f, hitStopFrames = 5, shake = 0.12f, heavy = true, behind = true, react = "hit_body" };
        // ---- CONTACT enemies: short strings of real strikes
        public static readonly AttackData EJab = new AttackData { clip = "jab", damage = 6, reach = 1.0f, knockback = 1.5f, hitStopFrames = 2, shake = 0.06f, react = "hit_front" };
        public static readonly AttackData ECross = new AttackData { clip = "cross", damage = 9, reach = 1.05f, knockback = 2.5f, hitStopFrames = 3, shake = 0.08f, react = "hit_chin" };
        public static readonly AttackData EHook = new AttackData { clip = "lead_hook", damage = 10, reach = 0.95f, knockback = 2.5f, hitStopFrames = 4, shake = 0.1f, react = "hit_chin" };
        public static readonly AttackData ELowKick = new AttackData { clip = "low_kick", damage = 7, reach = 1.15f, knockback = 1f, hitStopFrames = 3, shake = 0.07f, react = "hit_legs" };
        public static readonly AttackData EFrontKick = new AttackData { clip = "mae_geri", damage = 10, reach = 1.25f, knockback = 4f, hitStopFrames = 4, shake = 0.1f, react = "hit_body" };
        public static readonly AttackData ELunge = new AttackData { clip = "oi_zuki", damage = 11, reach = 1.2f, knockback = 3f, hitStopFrames = 4, shake = 0.1f, react = "hit_body" };
        public static readonly AttackData EElbow = new AttackData { clip = "elbow", damage = 12, reach = 0.8f, knockback = 2.5f, hitStopFrames = 5, shake = 0.12f, react = "hit_chin" };
        public static readonly AttackData EHaymaker = new AttackData { clip = "hook", damage = 15, reach = 1.0f, knockback = 5f, hitStopFrames = 6, shake = 0.16f, heavy = true, react = "stumble_back" };

        public static readonly AttackData Clothesline = new AttackData { clip = "clothesline", damage = 20, reach = 1.0f, knockback = 0f, hitStopFrames = 7, shake = 0.2f, heavy = true, name = "CLOTHESLINE" };

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
        public static readonly AttackData SwordRise = new AttackData { clip = "sword_rise", damage = 18, reach = 1.45f, knockback = 1.5f, lift = 9f, hitStopFrames = 5, shake = 0.12f, heavy = true, knockdown = true, cleave = true };
        public static readonly AttackData SwordIai = new AttackData { clip = "sword_iai", damage = 60, reach = 4.6f, knockback = 6f, hitStopFrames = 0, shake = 0.28f, heavy = true, knockdown = true, cleave = true, iai = true };
        public static readonly AttackData[] SwordCombo = { SwordA, SwordB, SwordA, SwordRise, SwordC };

        // ---- gun-fu: shots at range, pistol-whip and throw up close, a finishing shot on the ground
        public static readonly AttackData GunShoot = new AttackData { clip = "gun_shoot", damage = 34, reach = 14f, knockback = 5f, hitStopFrames = 2, shake = 0.06f, knockdown = true, ranged = true };
        public static readonly AttackData GunShootBack = new AttackData { clip = "gun_shoot_back", damage = 34, reach = 14f, knockback = 5f, hitStopFrames = 3, shake = 0.06f, knockdown = true, ranged = true, behind = true };
        public static readonly AttackData GunShootDown = new AttackData { clip = "gun_shoot_down", damage = 99, reach = 4f, knockback = 0f, hitStopFrames = 6, shake = 0.12f, heavy = true, ranged = true };
        public static readonly AttackData GunWhip = new AttackData { clip = "gun_whip", damage = 12, reach = 0.95f, knockback = 3f, hitStopFrames = 4, shake = 0.1f };
        public static readonly AttackData GunThrow = new AttackData { clip = "gun_throw", damage = 8, reach = 0.9f, knockback = 2f, hitStopFrames = 6, shake = 0.16f, heavy = true, knockdown = true };
        public const int MagazineSize = 12;

        // ---- pipe: a steel pipe swung like a staff (the sword's arcs, blunt and heavy)
        public static readonly AttackData PipeA = new AttackData { clip = "sword_a", damage = 16, reach = 1.6f, knockback = 6f, hitStopFrames = 4, shake = 0.1f, cleave = true };
        public static readonly AttackData PipeB = new AttackData { clip = "sword_b", damage = 18, reach = 1.65f, knockback = 7f, hitStopFrames = 5, shake = 0.12f, cleave = true };
        public static readonly AttackData PipeC = new AttackData { clip = "sword_c", damage = 30, reach = 1.85f, knockback = 11f, hitStopFrames = 7, shake = 0.2f, heavy = true, knockdown = true, cleave = true };
        public static readonly AttackData[] PipeCombo = { PipeA, PipeB, PipeC };

        // ---- knives: stab up close, throw at range
        public static readonly AttackData KnifeStab = new AttackData { clip = "jab", damage = 20, reach = 1.0f, knockback = 2f, hitStopFrames = 4, shake = 0.08f };
        public static readonly AttackData KnifeThrow = new AttackData { clip = "throw_weapon", damage = 0f, reach = 16f, ranged = true };

        // ---- grappling and bodies on the floor (fists)
        public static readonly AttackData GrabThrow = new AttackData { clip = "gun_throw", damage = 10, reach = 0.95f, knockback = 0f, hitStopFrames = 6, shake = 0.16f, heavy = true };
        public static readonly AttackData Stomp = new AttackData { clip = "stomp", damage = 999, reach = 1.1f, knockback = 0f, hitStopFrames = 7, shake = 0.2f, heavy = true };
        public static readonly AttackData SlideKick = new AttackData { clip = "slide_kick", damage = 14, reach = 1.3f, knockback = 6f, hitStopFrames = 4, shake = 0.12f, knockdown = true, cleave = true };

        public static readonly AttackData EnemyKnife = new AttackData { clip = "throw_weapon", damage = 10, reach = 14f, knockback = 2f, hitStopFrames = 3, shake = 0.08f, ranged = true };
        public static readonly AttackData EnemyPipe = new AttackData { clip = "sword_b", damage = 14, reach = 1.55f, knockback = 6f, hitStopFrames = 4, shake = 0.14f };
        public static readonly AttackData EnemyGrab = new AttackData { clip = "grab_attack", damage = 0, reach = 1.0f, knockback = 0f, hitStopFrames = 3, shake = 0.06f };
        public static readonly AttackData EnemyShot = new AttackData { clip = "gun_shoot", damage = 12, reach = 14f, knockback = 3f, hitStopFrames = 3, shake = 0.1f, ranged = true };
        public static readonly AttackData BruteSmash = new AttackData { clip = "cross", damage = 22, reach = 1.3f, knockback = 9f, hitStopFrames = 6, shake = 0.22f, heavy = true };
        // ---- the Foreman's crowbar: three swings, the third a heavy lunge that floors you (dodge it and he is open)
        public static readonly AttackData CrowbarA = new AttackData { clip = "sword_a", damage = 12, reach = 1.4f, knockback = 3f, hitStopFrames = 4, shake = 0.12f };
        public static readonly AttackData CrowbarB = new AttackData { clip = "sword_b", damage = 14, reach = 1.45f, knockback = 4f, hitStopFrames = 4, shake = 0.14f };
        public static readonly AttackData CrowbarC = new AttackData { clip = "sword_c", damage = 22, reach = 1.6f, knockback = 9f, hitStopFrames = 7, shake = 0.24f, heavy = true, knockdown = true };
        public static readonly AttackData Headbutt = new AttackData { clip = "headbutt", damage = 24, reach = 0.95f, knockback = 8f, hitStopFrames = 7, shake = 0.24f, heavy = true, knockdown = true };
        public static readonly AttackData BruteKick = new AttackData { clip = "front_kick", damage = 20, reach = 1.5f, knockback = 10f, hitStopFrames = 6, shake = 0.22f, heavy = true };
        public static readonly AttackData EnemySword = new AttackData { clip = "sword_b", damage = 14, reach = 1.4f, knockback = 4f, hitStopFrames = 4, shake = 0.12f };
        public static readonly AttackData EnemyJab = new AttackData { clip = "jab", damage = 7, reach = 1.0f, knockback = 2f, hitStopFrames = 3, shake = 0.08f };
        public static readonly AttackData EnemyCross = new AttackData { clip = "cross", damage = 10, reach = 1.05f, knockback = 3f, hitStopFrames = 3, shake = 0.1f };
        public static readonly AttackData EnemyKick = new AttackData { clip = "front_kick", damage = 12, reach = 1.3f, knockback = 4f, hitStopFrames = 4, shake = 0.12f };

        public static readonly AttackData Hook = new AttackData { clip = "hook", damage = 13, reach = 0.95f, knockback = 3f, hitStopFrames = 4, shake = 0.09f };
        public static readonly AttackData Uppercut = new AttackData { clip = "uppercut", damage = 22, reach = 0.95f, knockback = 2.5f, lift = 8.5f, hitStopFrames = 7, shake = 0.2f, heavy = true, knockdown = true };

        /// <summary>Tap high, again and again: jab, cross, jab, hook, uppercut (launches).</summary>
        public static readonly AttackData[] PunchCombo = { Jab, Cross, Jab, Hook, Uppercut };
        /// <summary>Tap low, again and again: front kick, roundhouse, knee, spinning kick (knocks down).</summary>
        public static readonly AttackData[] KickCombo = { FrontKick, Roundhouse, Knee, SpinKick };

        /// <summary>The hero's earlier mixed combo string (kept for reference).</summary>
        public static readonly AttackData[] Combo = { Jab, Cross, Knee, FrontKick, Jab, Cross, Roundhouse };
    }
}
