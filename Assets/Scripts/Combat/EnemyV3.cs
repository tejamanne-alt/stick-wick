using UnityEngine;

namespace ProjectInk
{
    /// <summary>Combat 3.0's enemy side (combat_v3.md §1 WHEN, §4).
    /// TELEGRAPH  the wind-up is the blow itself: his attack clip plays slowly up to its loaded pose
    ///            (the cocked fist, the chambered knee), then unloads at full speed. Meanwhile he flashes
    ///            WHITE (block it) or RED (you can't: a blade, a bar, a grab, a brute's smash: evade it).
    ///            The height of the loaded limb says where it is going: head, body or legs.
    /// DEFENCE    the hero judges each blow as it lands (HeroController.V3Defend): blocked, parried,
    ///            evaded, or not.</summary>
    public partial class Enemy
    {
        bool v3Wound;

        /// <summary>Can't be blocked: evade it.</summary>
        public bool V3Red
        {
            get
            {
                string c = attack.clip ?? "";
                return Armed || c == "grab_attack" || c.StartsWith("sword") || c == "headbutt" ||
                       (Type == EnemyType.Brute && attack.heavy);
            }
        }

        /// <summary>Where the blow goes: 0 head, 1 body, 2 legs.</summary>
        public int V3Height
        {
            get
            {
                string c = attack.clip ?? "";
                if (c == "low_kick" || c == "slide_kick" || c == "sweep") return 2;
                if (c == "mae_geri" || c == "front_kick" || c == "body_shot" || c == "knee" || c == "oi_zuki" || c == "grab_attack" ||
                    c == "sword_a" || c == "sword_c" || c == "mawashi_geri") return 1;
                return 0;
            }
        }

        /// <summary>A long, wide blow you can step inside (not a quick straight).</summary>
        public bool V3Long
        {
            get
            {
                string c = attack.clip ?? "";
                return attack.heavy || c == "hook" || c == "lead_hook" || c.StartsWith("sword") || c == "mae_geri" || c == "front_kick" ||
                       c == "mawashi_geri" || c == "roundhouse";
            }
        }

        Color V3FlashColor()
        {
            Color c = V3Red ? new Color(0.95f, 0.08f, 0.05f) : new Color(0.96f, 0.96f, 0.92f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 34f);
            return Color.Lerp(InkColor, c, 0.45f + 0.4f * pulse);
        }

        /// <summary>The wind-up: his blow's own clip, slowly up to the loaded pose.</summary>
        void V3Windup()
        {
            if (!CombatV3.On || body == null) return;
            var e = body.moves != null && attack.clip != null ? body.moves.Get(attack.clip) : null;
            if (e == null || e.loadTime <= 0f) return;
            float t = Gunner ? 0.75f : Type == EnemyType.Thrower ? 0.55f : WindupTime;
            body.Play(attack.clip, 0.1f, true, e.loadTime / Mathf.Max(0.08f, t));
        }

        /// <summary>The blow goes: carry on from the loaded pose at full speed.</summary>
        bool V3ContinueAttack()
        {
            if (!CombatV3.On || body == null || body.CurrentName != attack.clip) return false;
            var e = body.Current;
            if (e == null || e.loadTime <= 0f) return false;
            body.Play(attack.clip, 0f, false, 1f);
            return true;
        }

        /// <summary>His punch slapped aside at the last instant: thrown across, open.</summary>
        public void V3Parried(int fromDir)
        {
            Parried(fromDir);
            hurtFor = Boss ? 0.8f : 1.1f;
            if (body.Has("parried_v")) body.Play("parried_v", 0.02f);
        }

        /// <summary>The hero stepped out of it at the last instant: he overreaches into nothing.</summary>
        public void V3Whiff()
        {
            if (Boss && Enraged) return;
            ReleaseToken();
            chain.Clear(); chained = false; feinting = false;
            state = State.Hurt;
            stateTime = 0f;
            hurtFor = Boss ? 0.7f : 0.95f;
            velocity.x = Facing * 1.2f;
            body.Play(body.Has("whiff_stumble") ? "whiff_stumble" : "hit_front", 0.04f);
        }
    }
}
