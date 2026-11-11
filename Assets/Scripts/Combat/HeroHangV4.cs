using UnityEngine;

namespace ProjectInk
{
    /// <summary>Hanging from an edge, on the V4 body (chapter 1: the forklift, the racks, the mezzanine).
    ///   walk off a raised floor (not flat out) → he lowers himself down by the hands and hangs
    ///   HANGING   swipe down (or do nothing ~1.6 s)   let go and drop (knees soak it; from high: a knee and a hand)
    ///             swipe up                            pull back up onto the edge
    ///             hold left / right                   swing the legs; let go on the outward swing to land further out
    ///             tap a man below                     drop onto him, feet first: he goes down
    ///             tap a man at the edge above         grab his ankle and pull him off, past you
    ///             hit while hanging                   torn off the edge: a bad landing
    /// The hanging frame (tools/blender/v4_hang.py): the edge is EDGE_X in front of his root, its top
    /// H_TOP above it.</summary>
    public partial class HeroController
    {
        const float HangEdgeX = 0.22f, HangTop = 1.6f, HangTopX = 0.48f;
        enum HangMode { None, Lowering, Hanging, Climbing, Pulling }
        HangMode hang;
        float hangEdge, hangTopY, hangIdle, swingTime;
        int hangOut;                         // the way the drop lies, away from the platform
        bool fromHang, swinging;
        Enemy dropTarget, ankleFoe;

        /// <summary>Walking towards a raised floor's edge with a drop beyond it: lower himself down.</summary>
        bool TryLowerDown(int dir)
        {
            if (!MovesV4 || !LevelNav.Active || !Grounded || Mathf.Abs(velocity.x) > JogSpeed + 0.2f) return false;
            int pl = LevelNav.PlatformAt(transform.position);
            if (pl < 0) return false;
            var plat = LevelNav.Platforms[pl];
            if (plat.y < 0.9f) return false;
            float edge = dir > 0 ? plat.x1 : plat.x0;
            float to = (edge - transform.position.x) * dir;
            if (to > 0.22f || to < -0.05f) return false;
            float below = GroundY(new Vector3(edge + dir * 0.45f, plat.y + 0.2f, 0f), 0.1f);
            if (below > plat.y - 0.9f) return false;                         // stairs or another floor there: walk on
            hangEdge = edge;
            hangTopY = plat.y;
            hangOut = dir;
            running = false;
            velocity = Vector3.zero;
            SetFacing(-dir);                                                  // he turns to back down off it
            transform.position = new Vector3(edge + dir * HangEdgeX, plat.y - HangTop, transform.position.z);
            Grounded = false;
            hang = HangMode.Lowering;
            state = State.Hang;
            stateTime = 0f;
            body.Play("lower_down", 0.12f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);
            Tell("climb");
            return true;
        }

        void UpdateHangV4(float dt)
        {
            velocity = Vector3.zero;
            var gi = GestureInput.Instance;
            switch (hang)
            {
                case HangMode.Lowering:
                    if (body.Finished) { hang = HangMode.Hanging; hangIdle = 0f; body.Play("hang", 0.1f); }
                    return;
                case HangMode.Climbing:
                    if (body.Finished)
                    {
                        // standing on the top where the clip left him
                        transform.position = new Vector3(hangEdge - hangOut * (HangTopX - HangEdgeX), hangTopY, transform.position.z);
                        hang = HangMode.None;
                        Grounded = true;
                        EnterIdle();
                    }
                    return;
                case HangMode.Pulling:
                    if (ankleFoe != null && body.Current != null && body.Time >= body.Current.hitTime)
                    {
                        var foe = ankleFoe;
                        ankleFoe = null;
                        if (foe.Targetable)
                        {
                            foe.TakeHit(new HitInfo { damage = 14f, direction = hangOut, knockback = 3.5f, knockdown = true, heavy = true, react = "hit_legs", attacker = this });
                            Sound.Play(Sfx.EnemyHurt, 1f, foe.Chest);
                            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(120, "PULLED OFF THE EDGE!");
                            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.4f, 0.02f, 0.2f);
                        }
                    }
                    if (body.Finished) { hang = HangMode.Hanging; hangIdle = 0f; body.Play("hang", 0.1f); }
                    return;
            }

            // hanging
            hangIdle += dt;
            var cmd = gi != null ? gi.Next() : Command.None;
            switch (cmd)
            {
                case Command.ThrowWeapon: case Command.DropDown: LetGo("hang_drop", swinging && SwingingOut); return;
                case Command.JumpKick: case Command.JumpLeft: case Command.JumpRight:
                    hang = HangMode.Climbing;
                    body.Play("hang_climb", 0.08f);
                    Sound.Play(Sfx.HeroEffort, 0.8f, transform.position);
                    return;
                case Command.StrikeLeft: case Command.StrikeRight: case Command.KickLeft: case Command.KickRight:
                    {
                        int side = cmd == Command.StrikeLeft || cmd == Command.KickLeft ? -1 : 1;
                        var below = ManBelow();
                        var above = ManAtEdge();
                        if (above != null && side == -hangOut) { BeginAnklePull(above); return; }
                        if (below != null) { DropOnto(below); return; }
                        if (above != null) { BeginAnklePull(above); return; }
                        break;
                    }
            }
            if (cmd != Command.None) hangIdle = 0f;
            // hold left / right: swing the legs
            bool pump = Mathf.Abs(Axis) > 0.3f;
            if (pump)
            {
                hangIdle = 0f;
                if (!swinging) { swinging = true; swingTime = 0f; body.Play("hang_swing", 0.12f); }
                swingTime += dt;
            }
            else if (swinging) { swinging = false; LetGo("hang_drop", SwingingOut); return; }     // let go of the swing
            if (hangIdle > 1.6f) LetGo("hang_drop", false);
        }

        /// <summary>In the outward half of the swing (the legs driving away from the wall).</summary>
        bool SwingingOut
        {
            get
            {
                var e = body.Current;
                if (e == null || body.CurrentName != "hang_swing") return false;
                float u = (body.Time % e.Length) / e.Length;
                return u > 0.55f && u < 0.95f;
            }
        }

        void LetGo(string clip, bool far)
        {
            hang = HangMode.None;
            swinging = false;
            fromHang = true;
            state = State.Air;
            stateTime = 0f;
            Grounded = false;
            velocity = new Vector3(hangOut * (far ? 2.2f : 0.35f), 0f, 0f);
            body.Play(clip, 0.06f);
            Sound.Play(Sfx.Cloth, 0.8f, transform.position);
        }

        Enemy ManBelow()
        {
            Enemy best = null;
            float bd = 1.6f;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable) continue;
                float dy = transform.position.y - e.transform.position.y;
                float dx = (e.transform.position.x - transform.position.x) * hangOut;
                if (dy < 0.3f || dx < -0.4f || dx > 1.6f) continue;
                if (dx < bd) { bd = dx; best = e; }
            }
            return best;
        }

        Enemy ManAtEdge()
        {
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable || e.Type == EnemyType.Brute) continue;
                if (Mathf.Abs(e.transform.position.y - hangTopY) > 0.35f) continue;
                float inside = (hangEdge - e.transform.position.x) * hangOut;            // how far in from the edge
                if (inside > -0.05f && inside < 0.8f) return e;
            }
            return null;
        }

        void DropOnto(Enemy e)
        {
            float h = Mathf.Max(0.3f, transform.position.y + 0.2f - (e.transform.position.y + 1.2f));
            float t = Mathf.Sqrt(2f * h / gravity);
            LetGo("drop_on", false);
            velocity.x = Mathf.Clamp((e.transform.position.x - transform.position.x) / Mathf.Max(0.15f, t), -4f, 4f);
            dropTarget = e;
        }

        void BeginAnklePull(Enemy e)
        {
            ankleFoe = e;
            hang = HangMode.Pulling;
            body.Play("ankle_pull", 0.06f);
            Sound.Play(Sfx.HeroEffort, 0.9f, transform.position);
        }

        /// <summary>Every frame in the air after letting go: feet first onto the man below.</summary>
        void UpdateDropOn()
        {
            var e = dropTarget;
            if (e == null) return;
            if (!e.Targetable) { dropTarget = null; return; }
            float dx = Mathf.Abs(e.transform.position.x - transform.position.x);
            if (dx < 0.55f && transform.position.y <= e.transform.position.y + 1.25f)
            {
                dropTarget = null;
                e.TakeHit(new HitInfo { damage = 30f, direction = Facing, knockback = 2f, knockdown = true, heavy = true, hitsDowned = true, attacker = this });
                velocity.x = 0f;
                velocity.y = Mathf.Max(velocity.y, 1.5f);                  // a little bounce off him
                Sound.Play(Sfx.PunchHeavy, 1f, e.Chest);
                Sound.Play(Sfx.Bone, 0.8f, e.Chest);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.18f, 0.2f);
                if (TimeController.Instance != null) TimeController.Instance.HitStop(6);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(150, "FROM ABOVE!");
            }
        }

        /// <summary>Hit while hanging: torn off the edge.</summary>
        bool HangHit()
        {
            if (state != State.Hang) return false;
            hang = HangMode.None;
            swinging = false;
            fromHang = true;
            state = State.Air;
            stateTime = 0f;
            Grounded = false;
            velocity = new Vector3(hangOut * 0.8f, 0.5f, 0f);
            body.Play("hang_fall", 0.04f);
            return true;
        }

        /// <summary>Landing on the V4 body: the knees soak a short drop; from high, a knee and a hand.</summary>
        bool LandV4(float fallHeight)
        {
            if (!MovesV4 || state != State.Air) return false;
            bool fell = fromHang || fallHeight > 1.0f;
            fromHang = false;
            dropTarget = null;
            if (!fell) return false;
            BeginTraverse(fallHeight > 1.9f ? "land_heavy" : "land_soft", 0f);
            Sound.Play(Sfx.Land, fallHeight > 1.9f ? 1.2f : 0.8f, transform.position);
            if (fallHeight > 1.9f && CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.08f, 0.12f);
            return true;
        }
    }
}
