using UnityEngine;

namespace ProjectInk
{
    /// <summary>In the air with a weapon (docs/design/pipe.md, sword.md). With a pipe or a sword every swipe is
    /// a strike along its line, so the jump is a LEAP: swipe up while running. Both hands stay on the weapon
    /// through it (rod_jump / katana_jump, their running versions). In the air, swipe down (or tap): the pipe
    /// comes down FROM ABOVE (the heaviest pipe blow, always a knockdown) / the FALLING CUT (it cuts an
    /// ordinary man down if it's enough), landing as he does. The landings keep the weapon in hand.
    /// (A keyboard's jump key jumps standing too, with the same weapon jumps.)</summary>
    public partial class HeroController
    {
        bool airStrikeOn;

        /// <summary>The jump clip for what's in his hands (the pipe on the shoulder, the blade raised).</summary>
        string ArmedJump(string bare)
        {
            if (!KatanaMode) return bare;
            string c = (RodMode ? "rod_" : "katana_") + bare;
            return body.Has(c) ? c : bare;
        }

        /// <summary>Armed, at a run: a swipe up is the leap (standing, it stays the rising strike).</summary>
        bool ArmedLeapSwipe(Vector2 v)
        {
            if (!KatanaMode || !running || Mathf.Abs(velocity.x) < 2.6f || v.y < 0.75f || !Grounded || state != State.Idle) return false;
            var before = state;
            Jump();
            return state != before;                                           // jumped (or vaulted): the swipe is spent
        }

        /// <summary>In the air, armed: a swipe down or a tap brings the weapon down with the fall.</summary>
        bool ArmedAirStrike(Command cmd)
        {
            if (!KatanaMode || airStrikeOn) return false;
            bool down = cmd == Command.Cut && GestureInput.CutVector.y < -0.5f;
            bool tap = cmd == Command.StrikeLeft || cmd == Command.StrikeRight || cmd == Command.StrikeForward
                    || cmd == Command.KickLeft || cmd == Command.KickRight || cmd == Command.KickForward;
            if (!down && !tap) return false;
            string clip = RodMode ? "rod_air_smash" : "katana_air_cut";
            if (!body.Has(clip)) return false;
            airStrikeOn = true;
            body.Play(clip, 0.04f);
            velocity.y = Mathf.Min(velocity.y, -1.5f);                  // driven down into it
            SetTrails(true);
            Sound.Play(Sfx.WhooshBig, 1f, transform.position, RodMode ? 0.8f : 0.95f);
            Sound.Play(Sfx.HeroEffort, 0.7f, transform.position);
            return true;
        }

        /// <summary>Landing armed: the strike lands with him (if one was coming), then the knees take it, the
        /// weapon kept in hand.</summary>
        bool ArmedLand()
        {
            if (!KatanaMode) return false;
            if (airStrikeOn) { airStrikeOn = false; ArmedAirImpact(); }
            string c = RodMode ? "rod_land" : "katana_land";
            if (!body.Has(c)) return false;
            BeginTraverse(c, 0f);
            Sound.Play(Sfx.Land, 0.6f, transform.position);
            return true;
        }

        void ArmedAirImpact()
        {
            int n = 0;
            if (!RodMode) { katanaCut = "oroshi"; katanaLabel = "FALLING CUT"; EdgeNeutral(); }
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.Alive || !e.Targetable || Mathf.Abs(e.transform.position.y - transform.position.y) > 1.0f) continue;
                float dx = (e.transform.position.x - transform.position.x) * Facing;
                if (dx < -0.2f || dx > 1.45f) continue;
                n++;
                if (RodMode)
                {
                    e.TakeHit(new HitInfo { damage = 34f, direction = Facing, knockback = 3f, knockdown = true, heavy = true, react = "hit_body", attacker = this });
                    Sound.Play(Sfx.PipeHit, 1.2f, e.Chest, 0.85f);
                }
                else
                {
                    const float d = 30f;
                    if (!e.Boss && e.CanBeHeld && e.Health <= d) { CutDown(e); continue; }
                    e.TakeHit(new HitInfo { damage = d, direction = Facing, knockback = 2.5f, heavy = true, react = "hit_body", attacker = this });
                    Sound.Play(Sfx.SwordHit, 1f, e.Chest);
                }
                if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(Facing, -0.4f, 0f), 18, e.InkColor);
            }
            if (RodMode) Sound.Play(Sfx.MetalClang, n > 0 ? 0.7f : 1f, transform.position + Vector3.right * Facing, 0.8f);   // it rings off the floor
            if (n == 0) return;
            if (TimeController.Instance != null) TimeController.Instance.HitStop(9);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.24f, 0.2f);
            Haptics.Vibrate();
            if (WaveDirector.Instance != null && !(killedThisCut && !RodMode)) WaveDirector.Instance.AddStyle(220, RodMode ? "FROM ABOVE" : "FALLING CUT");
            if (RodMode) PipeWear();
            Tell("air_strike");
        }
    }
}
