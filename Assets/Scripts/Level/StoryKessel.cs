using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>Chapter 6 v3: KESSEL HILL, the ascent (docs/story/chapter6_fortress_v3.md). Eleven levels and the wall
    /// tower's passage, from the switchback road up to the observatory; then the roof and Voss (StoryFinale).
    /// The layout is KesselPlan (generated from tools/blender/kessel_plan.py, the same numbers the set is
    /// modelled on). Checkpoints: FortressCheckpoint 20 + the level.</summary>
    public partial class StoryIntro
    {
        readonly List<Enemy> kMen = new List<Enemy>();
        int kLevel = -1;
        bool kesselDark, bellCut, bellRung;
        float kx0, kx1, nextGust, nextBarrel, nextDrop, kBarkAt;
        readonly List<Enemy> kWatch = new List<Enemy>();
        readonly List<(Transform t, bool torn)> shoji = new List<(Transform, bool)>();
        Transform kPortA, kPortB;
        Vector3 kPortAUp, kPortBUp;

        static readonly string[] KesselBarks =
        {
            "The book! Get the book!", "Silas's place is mine!", "He's one man! ONE MAN!", "Boss is watching!",
            "Take it off his body!", "Whoever gets the book gets the chair!",
        };

        // ================================================================ the climb
        IEnumerator KesselAscent(int start)
        {
            kPortA = Find("KPortcullisA");
            kPortB = Find("KPortcullisB");
            if (kPortA != null) { kPortAUp = kPortA.position + Vector3.up * 4.2f; kPortA.position = kPortAUp; }
            if (kPortB != null) { kPortBUp = kPortB.position + Vector3.up * 4.2f; kPortB.position = kPortBUp; }
            for (int i = start; i < KesselPlan.Levels.Length; i++)
            {
                bool cut = i == start;
                switch (i)
                {
                    case 0: yield return KRoad(cut); break;
                    case 1: yield return KGreatStair(cut); break;
                    case 2: yield return KBarbican(cut); break;
                    case 3: yield return KPassage(cut); break;
                    case 4: yield return KWallWalk(cut); break;
                    case 5: yield return KWallTower(cut); break;
                    case 6: yield return KCourtyard(cut); break;
                    case 7: yield return KGallery(cut); break;
                    case 8: yield return KGreatHall(cut); break;
                    case 9: yield return KTowerStair(cut); break;
                    case 10: yield return KObservatory(cut); break;
                }
            }
            kLevel = -1;
        }

        /// <summary>Into a level: its bounds, its checkpoint, its banner; at a resume the hero is put at its start.</summary>
        IEnumerator BeginLevel(int i, bool cut, float startX = float.NaN)
        {
            kLevel = i;
            FortressCheckpoint = 20 + i;
            var L = KesselPlan.Levels[i];
            kx0 = L.x0; kx1 = L.x1;
            kMen.Clear();
            phase = Phase.Fortress;
            fZone = i == 8 ? 4 : 10 + i;                              // 4: the hall (the Warden's phases run in UpdateFortress)
            SetArena(kx0, kx1);
            if (cut)
            {
                float sx = float.IsNaN(startX) ? kx0 + 1.4f : startX;
                hero.CinePose(new Vector3(sx, FloorAt(sx, L.y), 0f));
                hero.SetFacing(1);
                yield return FadeIn(0.4f);
            }
            if (director != null) director.ShowBanner($"{i + 1}  ·  {L.name}", 2.2f);
            hero.CineRelease();
            hero.runSpeed = runSpeed;
            barsPx = 0f;
            CineOff();
        }

        /// <summary>The floor's top under x, nearest to the height expected.</summary>
        static float FloorAt(float x, float near)
        {
            float best = near, bd = 99f;
            foreach (var p in KesselPlan.Platforms)
                if (p.Contains(x, 0.3f) && Mathf.Abs(p.y - near) < bd) { bd = Mathf.Abs(p.y - near); best = p.y; }
            // on a flight of stairs: the step's height there
            foreach (var s in KesselPlan.Stairs)
            {
                float lo = Mathf.Min(s.x0, s.x1), hi = Mathf.Max(s.x0, s.x1);
                if (x < lo || x > hi) continue;
                float y = Mathf.Lerp(s.y0, s.y1, (x - s.x0) / (s.x1 - s.x0));
                if (Mathf.Abs(y - near) < bd) { bd = Mathf.Abs(y - near); best = y; }
            }
            return best;
        }

        /// <summary>A man of this level: placed on the floor at x, facing the hero.</summary>
        Enemy KMan(EnemyType t, float x, float nearY, bool watch = false, int face = 0)
        {
            if (director == null) return null;
            var e = director.SpawnScripted(t, SpawnKind.RightDoor);
            if (e == null) return null;
            e.arenaMinX = kx0; e.arenaMaxX = kx1;
            var at = new Vector3(x, FloorAt(x, nearY), 0f);
            int f = face != 0 ? face : (hero.transform.position.x < x ? -1 : 1);
            if (watch) { e.PostUnaware(at, f); kWatch.Add(e); }
            else e.PlaceAt(at, f);
            kMen.Add(e);
            return e;
        }

        bool KCleared() { kMen.RemoveAll(e => e == null || !e.Alive); return kMen.Count == 0; }

        IEnumerator KFight(System.Func<bool> extra = null)
        {
            Sound.Tension(true);
            Sound.Music(kesselDark ? "dark" : "fight", 0.55f, true, 1.0f);
            float clearAt = -1f;
            while (true)
            {
                if (KCleared() && (extra == null || extra()))
                {
                    if (clearAt < 0f) clearAt = Time.time;
                    if (Time.time - clearAt > 0.9f) break;
                }
                else clearAt = -1f;
                yield return null;
            }
            Sound.Tension(false);
            Sound.Music("storm", 0.45f, true, 3f);
            Sound.Play(Sfx.WaveClear, 0.6f);
            if (director != null) director.ShowBanner("CLEAR", 1.0f);
        }

        /// <summary>Every frame, while a level runs (from UpdateFortress).</summary>
        void UpdateKessel()
        {
            if (kLevel < 0 || hero == null || hero.IsDead) return;
            if (kMen.Count > 0 && Time.unscaledTime > kBarkAt)
            {
                kBarkAt = Time.unscaledTime + Random.Range(7f, 12f);
                Say("THUG", KesselBarks[Random.Range(0, KesselBarks.Length)], 2.0f);
            }
            switch (kLevel)
            {
                case 1: UpdateBarrels(); break;
                case 2: UpdateMurderHoles(); break;
                case 4: UpdateGusts(); break;
                case 6: UpdateBell(); break;
                case 7: UpdateShoji(); break;
            }
        }

        // ================================================================ L1 the switchback road
        IEnumerator KRoad(bool cut)
        {
            yield return BeginLevel(0, cut, 302f);
            // two men on watch by the gang's truck (the stealth start), more down the road, two on the upper road
            KMan(EnemyType.Brawler, 327.5f, 0f, true, 1);
            KMan(EnemyType.Brawler, 333.0f, 0f, true, -1);
            var tx = KesselPlan.Truck.x;
            SetPiece.Make("THE HANDBRAKE", new Vector3(tx + 2.4f, 0f, 0f), h => StartCoroutine(TruckRolls()));
            if (director != null) director.ShowBanner("TWO BY THE TRUCK  ·  CROUCH (SWIPE DOWN, HOLD)  ·  OR TAP THE TRUCK'S CAB", 3.2f);
            // the rest come when the watch is gone
            while (kWatch.Exists(w => w != null && w.Alive && w.Unaware)) yield return null;
            KMan(EnemyType.Brawler, 340f, 0f); KMan(EnemyType.Runner, 342f, 0f);
            var a = KMan(EnemyType.Brawler, 330f, 3.4f); if (a != null) { a.ThrowsDown = true; Throwable.Spawn(PropKind.Stone, a.transform.position); }
            var b = KMan(EnemyType.Brawler, 336f, 3.4f); if (b != null) { b.ThrowsDown = true; Throwable.Spawn(PropKind.Stone, b.transform.position); }
            yield return KFight();
            kWatch.Clear();
            // up the switchback to the stair
            SetArena(kx0, KesselPlan.Levels[1].x0 + 2f);
            yield return PlayerWalk(KesselPlan.Levels[1].x0 + 1.2f, kx0, 2.0f, Application.isMobilePlatform ? "HOLD TO GO UP" : "HOLD  D  TO GO UP");
        }

        IEnumerator TruckRolls()
        {
            var truck = Find("KTruck");
            if (truck == null) yield break;
            Sound.Play(Sfx.CarDoor, 1f, truck.position);
            yield return new WaitForSeconds(0.4f);
            Sound.Play(Sfx.Rumble, 1f, truck.position);
            if (director != null) director.AddStyle(200, "THE HANDBRAKE");
            var from = truck.position;
            float t = 0f;
            var hitAlready = new HashSet<Enemy>();
            while (t < 2.4f)
            {
                t += Time.deltaTime;
                float u = t / 2.4f;
                truck.position = from + Vector3.left * (u * u * 16f);
                foreach (var e in Enemy.All.ToArray())
                {
                    if (e == null || !e.Targetable || hitAlready.Contains(e)) continue;
                    if (Mathf.Abs(e.transform.position.y - from.y) > 1f) continue;
                    float dx = e.transform.position.x - truck.position.x;
                    if (dx > -3.2f && dx < 3.2f)
                    {
                        hitAlready.Add(e);
                        e.TakeHit(new HitInfo { damage = 60f, direction = -1, knockback = 9f, knockdown = true, heavy = true, hitsDowned = true, attacker = hero });
                        Sound.Play(Sfx.BodyFall, 1f, e.Chest);
                    }
                }
                yield return null;
            }
            Sound.Play(Sfx.Crate, 1f, truck.position);
            if (cam != null) cam.Shake(0.2f, 0.3f);
        }

        // ================================================================ L2 the great stair
        IEnumerator KGreatStair(bool cut)
        {
            yield return BeginLevel(1, cut);
            // the reveal: craning back to show the stair, the tiny figure, the gate and the tower above
            hero.CinePuppet();
            barsPx = 92f;
            Strike(1f, 0.3f);
            var me = hero.transform.position;
            yield return Cut(3.2f, u => CineCam.DronePullAway(me + Vector3.up * 1.0f, u, 3f, 30f, 6f, 20f, 46f), 0.03f);
            barsPx = 0f;
            CineOff();
            hero.CineRelease();
            nextBarrel = Time.time + 2.5f;
            KMan(EnemyType.Brawler, 393f, 7.4f); KMan(EnemyType.Brawler, 408f, 11.4f);
            KMan(EnemyType.Runner, 410f, 11.4f); KMan(EnemyType.Grabber, 424f, 15.4f);
            if (director != null) director.ShowBanner("BARRELS: SWIPE UP TO JUMP  ·  THE LIONS ARE COVER", 2.6f);
            yield return KFight(() => hero.transform.position.x > 412f);
            SetArena(kx0, KesselPlan.Levels[2].x0 + 2f);
            yield return PlayerWalk(KesselPlan.Levels[2].x0 + 1.0f, kx0, 2.0f, Application.isMobilePlatform ? "HOLD TO CLIMB" : "HOLD  D  TO CLIMB");
        }

        void UpdateBarrels()
        {
            if (Time.time < nextBarrel || hero.transform.position.x > 418f) return;
            nextBarrel = Time.time + Random.Range(3.2f, 4.6f);
            StartCoroutine(RollBarrel());
        }

        IEnumerator RollBarrel()
        {
            var path = new[] { new Vector3(420f, 15.4f), new Vector3(412f, 11.4f), new Vector3(406f, 11.4f), new Vector3(396f, 7.4f), new Vector3(390f, 7.4f), new Vector3(380f, 3.4f), new Vector3(372f, 3.4f) };
            var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(b.GetComponent<Collider>());
            b.transform.localScale = new Vector3(0.7f, 0.45f, 0.7f);
            b.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.16f, 0.15f, 0.14f) };
            Sound.Play(Sfx.Rumble, 0.8f, path[0]);
            var hit = new HashSet<Fighter>();
            for (int k = 0; k < path.Length - 1; k++)
            {
                var a = path[k]; var c = path[k + 1];
                float len = Vector3.Distance(a, c), t = 0f;
                while (t < len)
                {
                    t += Time.deltaTime * 5.5f;
                    var p = Vector3.Lerp(a, c, t / len) + Vector3.up * 0.4f;
                    b.transform.position = p;
                    b.transform.rotation = Quaternion.Euler(90f, 0f, -t * 160f);
                    // the hero: jumping clears it, a lion beside him shelters him
                    float hdx = hero.transform.position.x - p.x;
                    if (!hit.Contains(hero) && Mathf.Abs(hdx) < 0.5f && Mathf.Abs(hero.transform.position.y - (p.y - 0.4f)) < 0.7f && hero.Grounded && !NearLion(hero.transform.position.x))
                    {
                        hit.Add(hero);
                        hero.TakeHit(new HitInfo { damage = 12f, direction = -1, knockback = 4f, knockdown = true, heavy = true });
                    }
                    foreach (var e in Enemy.All.ToArray())
                    {
                        if (e == null || !e.Targetable || hit.Contains(e)) continue;
                        if (Mathf.Abs(e.transform.position.x - p.x) < 0.5f && Mathf.Abs(e.transform.position.y - (p.y - 0.4f)) < 0.7f)
                        {
                            hit.Add(e);
                            e.TakeHit(new HitInfo { damage = 25f, direction = -1, knockback = 4f, knockdown = true, heavy = true, attacker = hero });
                            if (director != null) director.AddStyle(80, "BARRELLED");
                        }
                    }
                    yield return null;
                }
            }
            Sound.Play(Sfx.Crate, 0.8f, b.transform.position);
            Object.Destroy(b);
        }

        static bool NearLion(float x)
        {
            foreach (var l in KesselPlan.Lion) if (Mathf.Abs(l.x - x) < 0.7f) return true;
            return false;
        }

        // ================================================================ L3 the barbican, the chain
        IEnumerator KBarbican(bool cut)
        {
            yield return BeginLevel(2, cut);
            nextDrop = Time.time + 3f;
            KMan(EnemyType.Brawler, 432f, 15.4f); KMan(EnemyType.Brawler, 438f, 15.4f);
            KMan(EnemyType.Runner, 441f, 15.4f); KMan(EnemyType.DoorMan, 436f, 15.4f);
            if (director != null) director.ShowBanner("STONES FROM THE HOLES ABOVE  ·  WATCH THE PALE CIRCLES", 2.4f);
            yield return KFight();
            // the drawbridge is up: the chain
            SetArena(kx0, 444f);
            yield return PlayerWalk(KesselPlan.DrawbridgeChain.x - 0.2f, kx0, 2.0f, Application.isMobilePlatform ? "HOLD TO GO TO THE CHAIN" : "HOLD  D  TO GO TO THE CHAIN");
            hero.CinePuppet();
            barsPx = 92f;
            var c0 = new Vector3(KesselPlan.DrawbridgeChain.x, KesselPlan.DrawbridgeChain.y, 0f);
            var c1 = new Vector3(KesselPlan.ChainTop.x, KesselPlan.ChainTop.y, 0f);
            hero.body.Play(hero.body.Has("hang") ? "hang" : "idle", 0.2f);
            Sound.Play(Sfx.Chain, 1f, c0);
            yield return HoldTo("CLIMB THE CHAIN", 2.8f, u =>
            {
                hero.CinePose(Vector3.Lerp(c0, c1 + Vector3.down * 1.6f, u));
                if (Random.value < 0.03f) Sound.Play(Sfx.Chain, 0.6f, hero.transform.position);
            }, c0 + Vector3.up * 2f);
            hero.body.Play(hero.body.Has("hang_climb") ? "hang_climb" : "idle", 0.1f);
            yield return RealWait(0.9f);
            hero.CinePose(c1);
            barsPx = 0f;
        }

        void UpdateMurderHoles()
        {
            if (Time.time < nextDrop || KCleared()) return;
            nextDrop = Time.time + Random.Range(2.4f, 3.4f);
            Vector2 best = KesselPlan.MurderHoles[0];
            foreach (var m in KesselPlan.MurderHoles)
                if (Mathf.Abs(m.x - hero.transform.position.x) < Mathf.Abs(best.x - hero.transform.position.x)) best = m;
            StartCoroutine(DropStone(new Vector3(best.x + Random.Range(-0.4f, 0.4f), best.y, 0f)));
        }

        IEnumerator DropStone(Vector3 at)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.position = at + Vector3.up * 0.02f;
            ring.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);
            var rm = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.9f, 0.9f, 0.92f, 0.35f) };
            ring.GetComponent<Renderer>().sharedMaterial = rm;
            Sound.Play(Sfx.Crawl, 0.6f, at + Vector3.up * 6f);       // grit falling
            float t = 0f;
            while (t < 1.2f) { t += Time.deltaTime; rm.color = new Color(0.9f, 0.9f, 0.92f, 0.2f + 0.3f * Mathf.Sin(t * 18f) * 0.5f + 0.15f); yield return null; }
            Object.Destroy(ring);
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(s.GetComponent<Collider>());
            s.transform.localScale = new Vector3(0.6f, 0.45f, 0.5f);
            s.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.32f, 0.31f, 0.3f) };
            float y = at.y + 6.5f;
            while (y > at.y + 0.3f) { y -= Time.deltaTime * 20f; s.transform.position = new Vector3(at.x, y, 0f); yield return null; }
            Sound.Play(Sfx.PunchHeavy, 1f, at);
            if (cam != null) cam.Shake(0.08f, 0.12f);
            if (Mathf.Abs(hero.transform.position.x - at.x) < 0.9f && Mathf.Abs(hero.transform.position.y - at.y) < 0.8f)
                hero.TakeHit(new HitInfo { damage = 14f, direction = hero.Facing, knockback = 1f, knockdown = true, heavy = true });
            foreach (var e in Enemy.All.ToArray())
                if (e != null && e.Targetable && Mathf.Abs(e.transform.position.x - at.x) < 0.9f && Mathf.Abs(e.transform.position.y - at.y) < 0.8f)
                {
                    e.TakeHit(new HitInfo { damage = 30f, direction = 1, knockback = 1f, knockdown = true, heavy = true, attacker = hero });
                    if (director != null) director.AddStyle(120, "FROM THEIR OWN WALL");
                }
            if (InkFx.Instance != null) InkFx.Instance.Splash(at, Vector3.up, 12, new Color(0.4f, 0.4f, 0.4f));
            Object.Destroy(s, 0.6f);
        }

        // ================================================================ L4 the gate passage
        IEnumerator KPassage(bool cut)
        {
            if (!cut)
            {
                // down through the murder hole into the passage
                hero.CinePuppet();
                var hole = new Vector3(KesselPlan.MurderHoleDrop.x, KesselPlan.MurderHoleDrop.y, 0f);
                hero.CinePose(hole);
                yield return Cut(0.8f, u => Static(hole + new Vector3(0f, -1.0f, -4.5f), hole, 40f), 0.02f,
                    u => hero.CinePose(Vector3.Lerp(hole, new Vector3(hole.x, 15.4f, 0f), u * u)));
                hero.body.Play(hero.body.Has("land_heavy") ? "land_heavy" : "idle", 0.05f);
                Sound.Play(Sfx.Land, 1.2f, hero.transform.position);
            }
            yield return BeginLevel(3, cut, cut ? 451f : float.NaN);
            // both gates slam down: the killing ground
            yield return Slam(kPortA, kPortAUp);
            yield return Slam(kPortB, kPortBUp);
            Say("VOICE, FROM ABOVE", "He's in. Close it.", 2.2f);
            KMan(EnemyType.Brawler, 462f, 15.4f); KMan(EnemyType.Grabber, 466f, 15.4f);
            KMan(EnemyType.Runner, 456f, 15.4f); KMan(EnemyType.Brawler, 468f, 15.4f);
            SetArena(449.6f, 470.4f);
            yield return KFight();
            // the windlass
            yield return PlayerWalk(KesselPlan.Windlass.x - 0.5f, 449.6f, 1.8f, Application.isMobilePlatform ? "HOLD TO GO TO THE WINDLASS" : "HOLD  D  TO GO TO THE WINDLASS");
            hero.CinePuppet();
            hero.body.Play(hero.body.Has("push") ? "push" : "idle", 0.15f);
            yield return MashTo("HEAVE THE WINDLASS", 16, u =>
            {
                if (kPortB != null) kPortB.position = Vector3.Lerp(kPortBUp - Vector3.up * 4.2f, kPortBUp, u);
                if (Random.value < 0.1f) Sound.Play(Sfx.Chain, 0.6f, kPortB != null ? kPortB.position : hero.transform.position);
            });
            Sound.Play(Sfx.Latch, 1f, hero.transform.position);
            SetArena(449.6f, KesselPlan.Levels[4].x0 + 2f);
            hero.CineRelease();
            yield return PlayerWalk(KesselPlan.Levels[4].x0 + 1.2f, 449.6f, 2.0f, Application.isMobilePlatform ? "HOLD TO GO UP TO THE WALL" : "HOLD  D  TO GO UP TO THE WALL");
        }

        IEnumerator Slam(Transform port, Vector3 up)
        {
            if (port == null) yield break;
            float y0 = up.y, y1 = up.y - 4.2f, t = 0f;
            while (t < 0.32f) { t += Time.deltaTime; port.position = new Vector3(up.x, Mathf.Lerp(y0, y1, t / 0.32f), up.z); yield return null; }
            Sound.Play(Sfx.MetalClang, 1.2f, port.position, 0.7f);
            if (cam != null) cam.Shake(0.25f, 0.3f);
            if (InkFx.Instance != null) InkFx.Instance.Splash(port.position, Vector3.up, 16, new Color(0.9f, 0.9f, 0.9f));
        }

        // ================================================================ L5 the wall walk
        IEnumerator KWallWalk(bool cut)
        {
            yield return BeginLevel(4, cut);
            nextGust = Time.time + 6f;
            // men from both towers
            KMan(EnemyType.Brawler, 484f, 22.4f, false, 1); KMan(EnemyType.Brawler, 514f, 22.4f);
            KMan(EnemyType.Runner, 516f, 22.4f);
            yield return KFight();
            // THE FIRST SWORDSMAN: out of the far tower. Beat him, take his blade.
            var s = KMan(EnemyType.Swordsman, 517f, 22.4f);
            if (s != null) s.SetMaxHealth(60f);
            Sound.Play(Sfx.SwordSwing, 0.6f, new Vector3(517f, 23f, 0f), 1.3f);
            if (director != null) director.ShowBanner("A SWORDSMAN  ·  BEAT HIM, TAKE HIS BLADE", 2.6f);
            Sound.Music("duel", 0.5f, true, 1f);                       // the drums arrive with the katana
            yield return KFight();
            yield return new WaitForSeconds(0.4f);
            if (hero.Style != FightStyle.Sword && director != null)
                director.ShowBanner("WALK OVER HIS KATANA", 2.0f);
            float wait = 0f;
            while (hero.Style != FightStyle.Sword && wait < 12f) { wait += Time.deltaTime; yield return null; }
            KMan(EnemyType.Swordsman, 515f, 22.4f);
            KMan(EnemyType.Brawler, 486f, 22.4f, false, 1);
            yield return KFight();
        }

        void UpdateGusts()
        {
            if (Time.time < nextGust) return;
            nextGust = Time.time + Random.Range(6.5f, 9f);
            int dir = Random.value < 0.5f ? -1 : 1;
            Sound.Play(Sfx.Wind, 1.2f, hero.transform.position);
            if (cam != null) cam.Shake(0.06f, 0.4f);
            hero.Push(dir * 2.4f);
            foreach (var e in Enemy.All) if (e != null && e.Alive && e.transform.position.y > 20f) e.Push(dir * 2.4f);
            if (director != null) director.AddStyle(0, dir > 0 ? "THE WIND →" : "← THE WIND");
        }

        // ================================================================ L6 the wall tower (the passage down)
        IEnumerator KWallTower(bool cut)
        {
            yield return BeginLevel(5, cut, cut ? 519f : float.NaN);
            SetArena(KesselPlan.Levels[5].x0 - 1f, KesselPlan.Levels[6].x0 + 3f);
            if (hero.Style == FightStyle.Sword && StoryIntro.Instance != null) Say("VOSS MEMORY", "Take the high step. Always.", 2.8f);
            // one ambush on the turn
            var s = KMan(EnemyType.Swordsman, 526f, 15.4f);
            if (s != null) { s.arenaMinX = KesselPlan.Levels[5].x0 - 1f; s.arenaMaxX = KesselPlan.Levels[6].x0 + 3f; }
            yield return KFight();
        }

        // ================================================================ L7 the courtyard
        IEnumerator KCourtyard(bool cut)
        {
            yield return BeginLevel(6, cut);
            bellCut = bellRung = false;
            kWatch.Clear();
            KMan(EnemyType.Swordsman, 535f, 15.4f, true, 1);
            KMan(EnemyType.Brawler, 545f, 15.4f, true, -1);
            SetPiece.Make("THE BELL ROPE", new Vector3(KesselPlan.BellRope.x - 0.4f, 15.4f, 0f), h =>
            {
                bellCut = true;
                Sound.Play(Sfx.SwordHit, 0.8f, h.transform.position + Vector3.up * 1.5f);
                if (director != null) director.AddStyle(150, "NO ALARM");
            });
            SetPiece.Make("THE HAY HOIST", new Vector3(KesselPlan.HayHoist.x, 15.4f, 0f), h => StartCoroutine(DropBale()));
            if (director != null) director.ShowBanner("TWO ON WATCH  ·  THE BELL ROPE (TAP IT) KEEPS IT QUIET", 3.0f);
            while (kWatch.Exists(w => w != null && w.Alive && w.Unaware) && hero.transform.position.x < 562f) yield return null;
            yield return KFight();
            // the blackout
            yield return KBlackout();
            KMan(EnemyType.Swordsman, 572f, 15.4f); KMan(EnemyType.Swordsman, 578f, 15.4f);
            KMan(EnemyType.Brawler, 540f, 15.4f, false, 1); KMan(EnemyType.Runner, 566f, 15.4f);
            if (bellRung) { KMan(EnemyType.Swordsman, 582f, 15.4f); KMan(EnemyType.Brawler, 531f, 15.4f, false, 1); }
            if (director != null) director.ShowBanner("IN THE DARK  ·  THREE IN A LINE: HOLD ON YOURSELF, DRAG THROUGH THEM", 3.0f);
            yield return KFight();
            SetArena(kx0, KesselPlan.Levels[7].x0 + 2f);
            yield return PlayerWalk(KesselPlan.Levels[7].x0 + 1.2f, kx0, 1.8f, Application.isMobilePlatform ? "HOLD TO GO IN" : "HOLD  D  TO GO IN");
        }

        void UpdateBell()
        {
            if (bellCut || bellRung || kesselDark) return;
            foreach (var w in kWatch)
                if (w != null && w.Alive && !w.Unaware)
                {
                    bellRung = true;
                    StartCoroutine(RingBell());
                    return;
                }
        }

        IEnumerator RingBell()
        {
            yield return new WaitForSeconds(2.0f);
            if (bellCut) { bellRung = false; yield break; }
            var at = new Vector3(KesselPlan.BellRope.x - 1f, 25f, 3f);
            for (int k = 0; k < 4; k++) { Sound.Play(Sfx.MetalClang, 1.2f, at, 0.45f); yield return new WaitForSeconds(0.7f); }
            if (director != null) director.ShowBanner("THE BELL: THE WHOLE BAILEY IS AWAKE", 2.0f);
        }

        IEnumerator DropBale()
        {
            var x = KesselPlan.HayHoist.x;
            Sound.Play(Sfx.Cloth, 1f, new Vector3(x, 20f, 0f));
            yield return new WaitForSeconds(0.25f);
            Sound.Play(Sfx.BodyFall, 1.2f, new Vector3(x, 15.4f, 0f));
            foreach (var e in Enemy.All.ToArray())
                if (e != null && e.Targetable && Mathf.Abs(e.transform.position.x - x) < 1.3f && Mathf.Abs(e.transform.position.y - 15.4f) < 0.8f)
                {
                    e.TakeHit(new HitInfo { damage = 30f, direction = 1, knockback = 0.5f, knockdown = true, heavy = true, attacker = hero });
                    if (director != null) director.AddStyle(150, "HAY");
                }
            if (InkFx.Instance != null) InkFx.Instance.Splash(new Vector3(x, 15.6f, 0f), Vector3.up, 30, new Color(0.7f, 0.68f, 0.6f));
        }

        IEnumerator KBlackout()
        {
            hero.CinePuppet();
            barsPx = 92f;
            var pole = new Vector3(KesselPlan.FloodCourt[1].x, 21.6f, 2.6f);
            yield return Cut(1.2f, u => Static(pole + new Vector3(-3.2f, -5.8f, -3.0f), pole, 44f), 0.03f);
            Strike(1f, 0.0f);
            Sound.Play(Sfx.Boom, 1f, pole);
            if (cam != null) cam.Shake(0.35f, 0.5f);
            var gen = new Vector3(KesselPlan.Generator.x, 16.7f, 2.6f);
            Sound.Play(Sfx.Glass, 1f, gen);
            Sound.Play(Sfx.Buzz, 1f, gen);
            if (InkFx.Instance != null) { InkFx.Instance.Splash(gen, Vector3.up, 60, new Color(0.95f, 0.95f, 0.95f)); InkFx.Instance.ImpactFlash(0.08f); }
            yield return Cut(1.2f, u => Static(gen + new Vector3(-2.6f, 0.4f, -3.2f), gen, 40f), 0.05f);
            fBlackout = true;
            kesselDark = true;
            Sound.Music("dark", 0.55f, true, 0.8f);
            Say("YOU", "...Good. I don't need to see you.", 2.6f);
            yield return Cut(2.4f, u => CineCam.DollyIn(HeroHead(), new Vector3(0.7f, -0.05f, 1f), u, 2.0f, 1.2f, 32f), 0.015f);
            CineOff();
            barsPx = 0f;
            hero.CineRelease();
        }

        // ================================================================ L8 Voss's gallery
        IEnumerator KGallery(bool cut)
        {
            kesselDark = false;
            fBlackout = false;
            indoors = true;
            yield return BeginLevel(7, cut);
            // the paper screens: men behind them are shadows; a cut through the paper tears it
            shoji.Clear();
            var paper = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.9f, 0.88f, 0.82f, 0.62f) };
            foreach (var m in KesselPlan.Shoji)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(q.GetComponent<Collider>());
                q.name = "Shoji";
                q.transform.position = new Vector3(m.x, m.y + 1.25f, -0.9f);
                q.transform.localScale = new Vector3(1.9f, 2.5f, 1f);
                q.GetComponent<Renderer>().sharedMaterial = paper;
                shoji.Add((q.transform, false));
                KMan(EnemyType.Swordsman, m.x + 0.2f, 15.4f);
            }
            if (director != null) director.ShowBanner("VOSS'S COLLECTION  ·  SHADOWS BEHIND THE PAPER", 2.6f);
            yield return KFight();
            SetPiece.Make("THE PHOTOGRAPH", new Vector3(KesselPlan.Photo.x, 15.4f, 0f), h => StartCoroutine(PhotoMemory()));   // once the room is clear
            if (SetPiece.All.Exists(p => p != null && p.label == "THE PHOTOGRAPH" && p.usable) && director != null)
                director.ShowBanner("A PHOTOGRAPH ON THE EASEL  ·  TAP IT", 2.4f);
            float wait = 0f;
            while (SetPiece.All.Exists(p => p != null && p.label == "THE PHOTOGRAPH" && p.usable) && wait < 20f) { wait += Time.deltaTime; yield return null; }
            while (memoryPlaying) yield return null;
        }

        bool memoryPlaying;

        /// <summary>The photograph: Voss, and him at twenty with a wooden sword. The Kata of Three, remembered.</summary>
        IEnumerator PhotoMemory()
        {
            memoryPlaying = true;
            hero.CinePuppet();
            barsPx = 92f;
            var ph = new Vector3(KesselPlan.Photo.x, 17.0f, 2.8f);
            Say("YOU", "...Me. Twenty. And him.", 2.4f);
            yield return Cut(2.6f, u => CineCam.DollyIn(ph, new Vector3(0f, 0f, -1f), u, 2.2f, 1.2f, 28f), 0.01f);
            Sound.Play(Sfx.FlashCut, 0.8f);
            Say("VOSS MEMORY", "Again. Three lines. One breath.", 2.8f);
            // the three lines of the kata, drawn in ink before his eyes
            var c = hero.transform.position + Vector3.up * 1.4f;
            yield return RealWait(0.6f);
            InkStroke.DrawWorld(c + new Vector3(-0.6f, 0.6f, 0f), c + new Vector3(0.6f, -0.6f, 0f), 2.4f, new Color(0.95f, 0.95f, 0.97f, 0.95f), 0.14f);
            Sound.Play(Sfx.SwordSwing, 1f);
            yield return RealWait(0.5f);
            InkStroke.DrawWorld(c + new Vector3(0.6f, -0.6f, 0f), c + new Vector3(-0.4f, 0.8f, 0f), 2.0f, new Color(0.95f, 0.95f, 0.97f, 0.95f), 0.14f);
            Sound.Play(Sfx.SwordSwing, 1f, null, 1.1f);
            yield return RealWait(0.5f);
            InkStroke.DrawWorld(c + new Vector3(-0.7f, 0.1f, 0f), c + new Vector3(0.8f, 0.1f, 0f), 1.6f, new Color(0.95f, 0.95f, 0.97f, 0.95f), 0.14f);
            Sound.Play(Sfx.SwordSwing, 1f, null, 1.2f);
            yield return RealWait(1.4f);
            Say("VOSS MEMORY", "You will use this on me one day. That's how I'll know I taught you well.", 4.0f);
            yield return Cut(4.0f, u => Static(HeroHead() + new Vector3(0.5f, 0.05f, -0.8f), HeroHead(), 28f), 0.01f);
            if (director != null) director.ShowBanner("THE KATA OF THREE: THREE CUTS WITHOUT A PAUSE", 2.6f);
            CineOff();
            barsPx = 0f;
            hero.CineRelease();
            memoryPlaying = false;
        }

        void UpdateShoji()
        {
            for (int i = 0; i < shoji.Count; i++)
            {
                var (t, torn) = shoji[i];
                if (torn || t == null) continue;
                if (hero.IsCutting && Mathf.Abs(hero.transform.position.x - t.position.x) < 1.6f)
                {
                    shoji[i] = (t, true);
                    StartCoroutine(Tear(t));
                }
            }
        }

        IEnumerator Tear(Transform t)
        {
            Sound.Play(Sfx.Cloth, 1.2f, t.position, 1.3f);
            var r = t.GetComponent<Renderer>();
            var halves = new[] { Object.Instantiate(t.gameObject), Object.Instantiate(t.gameObject) };
            r.enabled = false;
            for (int k = 0; k < 2; k++)
            {
                halves[k].transform.localScale = new Vector3(0.95f, 2.5f, 1f);
                halves[k].transform.position = t.position + Vector3.right * (k == 0 ? -0.48f : 0.48f);
            }
            float a = 0f;
            while (a < 1f)
            {
                a += Time.deltaTime * 1.4f;
                for (int k = 0; k < 2; k++)
                {
                    halves[k].transform.position += new Vector3((k == 0 ? -1 : 1) * Time.deltaTime * 0.6f, -Time.deltaTime * 2.4f * a, 0f);
                    halves[k].transform.rotation = Quaternion.Euler(0f, 0f, (k == 0 ? 1 : -1) * a * 40f);
                }
                yield return null;
            }
            foreach (var h in halves) Object.Destroy(h);
        }

        // ================================================================ L9 the great hall: THE WARDEN
        IEnumerator KGreatHall(bool cut)
        {
            indoors = true;
            yield return BeginLevel(8, cut);
            hallFloorY = 15.4f;
            hero.CinePuppet();
            WaveDirector.BossWeapon = FightStyle.Pipe;
            WaveDirector.StoryBoss = true;
            warden = director != null ? director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor) : null;
            WaveDirector.BossWeapon = FightStyle.Fists;
            if (warden == null) { hero.CineRelease(); yield break; }
            warden.MakeDriver(warden.maxHealth * 1.15f);
            warden.transform.localScale = Vector3.one * 1.22f;
            warden.ShowIron(true);
            warden.DriverPhase = 2;
            warden.arenaMinX = kx0; warden.arenaMaxX = kx1;
            warden.Puppet();
            warden.PuppetPose(new Vector3(641.6f, 15.4f, 0.9f));
            warden.SetFacing(-1);
            warden.PuppetPlay("idle", 0f);
            wardenStage = 0;
            Enemy.StoryFinale = (e, hit) => { if (wardenStage < 5) wardenStage = 5; };
            yield return Cut(3.6f, u => CineCam.LateralReveal(new Vector3(kx0 + 0.6f, 17.6f, -1.6f), new Vector3(kx0 + 4f, 17.8f, -2.4f), new Vector3(640f, 17f, 2f), u, 46f), 0.02f);
            Dress(warden.body, Look.Warden);
            Say("THE WARDEN", "Twenty years I kept his door. Boss says whoever brings the book gets the chair.", 3.6f, HeadOf(warden));
            yield return Cut(3.6f, u => CineCam.DollyIn(HeadOf(warden).position, new Vector3(1f, -0.1f, 0.4f), u, 7f, 3.2f, 30f), 0.015f);
            warden.PuppetPlay("walk", 0.2f, 0.7f);
            var w0 = warden.transform.position;
            Sound.Play(Sfx.Chain, 0.8f, w0);
            yield return Cut(2.6f, u => CineCam.LowAngleArc(warden.transform.position + Vector3.up * 1.3f, u, 3.6f, -60f, -10f, 36f), 0.02f,
                u => warden.PuppetPose(Vector3.Lerp(w0, new Vector3(624f, 15.4f, 0f), u)));
            warden.PuppetPlay("sword_idle", 0.2f);
            Say("THE WARDEN", "No further.", 1.6f, HeadOf(warden));
            if (director != null) director.ShowBanner("THE WARDEN", 2.6f);
            Sound.Play(Sfx.CineMassive, 0.7f);
            Strike(0.8f, 0.3f);
            hero.CinePose(new Vector3(kx0 + 4f, 15.4f, 0f));
            CineOff();
            barsPx = 0f;
            warden.PuppetRelease();
            warden.Passive = false;
            hero.CineRelease();
            hero.runSpeed = runSpeed;
            wardenStage = 1;
            ShowStreetBanner("I  ·  THE CHAIN");
            Sound.Music("boss", 0.65f, true, 1.0f);
            Sound.Tension(true);
            // the chain wraps the blade: let it go
            if (hero.Style == FightStyle.Sword)
            {
                yield return new WaitForSeconds(5f);
                if (warden != null && warden.Alive && hero.Style == FightStyle.Sword)
                {
                    Sound.Play(Sfx.Chain, 1.2f, hero.Chest);
                    Sound.Play(Sfx.SwordClash, 1f, hero.Chest, 0.7f);
                    if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.6f);
                    Say("THE WARDEN", "Mine now.", 1.6f, HeadOf(warden));
                    hero.DiscardKatana();
                    if (director != null)
                    {
                        WeaponPickup.Spawn(FightStyle.Sword, 999, new Vector3(kx1 - 3f, 15.4f, 0f), director.weaponMaterial);
                        director.ShowBanner("THE CHAIN TOOK YOUR BLADE  ·  FISTS: CALF, CALF, THEN THE HEAD", 3.0f);
                    }
                }
            }
            while (wardenStage < 5) yield return null;
            var at = warden != null ? warden.transform.position : hero.transform.position;
            Throwable.Spawn(PropKind.Chain, at + Vector3.right * 0.8f);
            Sound.Tension(false);
            Sound.Music("storm", 0.45f, true, 3f);
            foreach (var e in Enemy.All.ToArray()) if (e != null && e != warden && e.Alive) e.PuppetDie();
            yield return RealWait(0.6f);
            hero.CinePuppet();
            barsPx = 92f;
            if (warden != null) { warden.Puppet(); warden.PuppetPose(at); warden.SetFacing(-1); warden.PuppetPlay(warden.body.Has("lie_up") ? "lie_up" : "lie", 0.2f); }
            var wh = warden != null ? HeadOf(warden) : null;
            Sound.Play(Sfx.BruteLaugh, 0.7f, at);
            Say("THE WARDEN", "Go on up. He's waiting. He's been waiting years.", 3.4f, wh);
            yield return Cut(3.6f, u => Static(at + new Vector3(1.6f, 0.5f, -1.8f), wh != null ? wh.position : at + Vector3.up * 0.6f, 32f), 0.015f);
            Say("THE WARDEN", "Boss falls... hill burns. Nothing left... for Harrow.", 3.4f, wh);
            yield return Cut(3.4f, u => Static(at + new Vector3(0.9f, 0.3f, -1.2f), wh != null ? wh.position : at + Vector3.up * 0.6f, 28f), 0.012f);
            CineOff();
            barsPx = 0f;
            hero.CineRelease();
            // his katana's still by the fire: pick it up on the way, or don't
            if (hero.Style != FightStyle.Sword && director != null) director.ShowBanner("YOUR KATANA IS BY THE FIRE", 2.0f);
            SetArena(kx0, KesselPlan.Levels[9].x0 + 2f);
            yield return PlayerWalk(KesselPlan.Levels[9].x0 + 1.0f, kx0, 1.8f, Application.isMobilePlatform ? "HOLD TO GO OUT TO THE TOWER" : "HOLD  D  TO GO OUT TO THE TOWER");
            indoors = false;
        }

        // ================================================================ L10 the tower stair
        IEnumerator KTowerStair(bool cut)
        {
            // first, from below: the man and the child on the gallery, lit by a strike (the old tower's shots)
            if (!cut) { yield return TheTower(); }
            yield return BeginLevel(9, true, 659.4f);
            KMan(EnemyType.Swordsman, 673.5f, 21.4f); KMan(EnemyType.Swordsman, 668f, 18.4f);
            yield return KFight(() => hero.transform.position.x > 679f);
            // the broken stair: only the lightning shows the gap
            SetArena(kx0, 682.2f);
            yield return PlayerWalk(681.9f, kx0, 1.6f, Application.isMobilePlatform ? "HOLD TO GO ON UP" : "HOLD  D  TO GO ON UP");
            hero.CinePuppet();
            barsPx = 60f;
            if (director != null) director.ShowBanner("THE STEPS ARE GONE  ·  TAP WHEN THE LIGHTNING SHOWS THE GAP", 3.2f);
            float nextFlash = Time.unscaledTime + 1.6f, flashAt = -10f;
            var gi = GestureInput.Instance;
            bool made = false;
            while (!made)
            {
                if (Time.unscaledTime > nextFlash) { Strike(1f, 0.15f); flashAt = Time.unscaledTime; nextFlash = Time.unscaledTime + Random.Range(2.2f, 3.4f); }
                bool tap = gi != null && gi.AnyPressThisFrame;
                if (gi != null) gi.Flush();
                if (tap)
                {
                    bool inFlash = Time.unscaledTime - flashAt < 0.6f;
                    var a = hero.transform.position;
                    var b = new Vector3(685f, 24.4f, 0f);
                    hero.body.Play(hero.body.Has("jump_run") ? "jump_run" : "idle", 0.05f);
                    if (inFlash)
                    {
                        yield return Cut(0.7f, u => Static(Vector3.Lerp(a, b, 0.5f) + new Vector3(0f, 0.5f, -6f), Vector3.Lerp(a, b, u) + Vector3.up, 40f), 0.02f,
                            u => hero.CinePose(Vector3.Lerp(a, b, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.8f));
                        Sound.Play(Sfx.Land, 1f, b);
                        made = true;
                    }
                    else
                    {
                        // a miss: the hands catch the far edge, a heave up
                        yield return Cut(0.6f, u => Static(Vector3.Lerp(a, b, 0.5f) + new Vector3(0f, 0.2f, -6f), Vector3.Lerp(a, b, u), 40f), 0.03f,
                            u => hero.CinePose(Vector3.Lerp(a, b + Vector3.down * 1.6f, u)));
                        hero.body.Play(hero.body.Has("hang") ? "hang" : "idle", 0.05f);
                        Sound.Play(Sfx.HeroEffort, 1f, b);
                        yield return MashTo("PULL YOURSELF UP", 8);
                        hero.body.Play(hero.body.Has("hang_climb") ? "hang_climb" : "idle", 0.05f);
                        yield return RealWait(0.8f);
                        hero.CinePose(b);
                        made = true;
                    }
                }
                yield return null;
            }
            barsPx = 0f;
            hero.CineRelease();
            SetArena(683.8f, KesselPlan.Levels[10].x0 + 2f);
            HoldWalkie(true);
            Walkie("Papa! You jumped! You're so high!", 2.6f);
            yield return PlayerWalk(KesselPlan.Levels[10].x0 + 1.2f, 683.8f, 1.8f, Application.isMobilePlatform ? "HOLD TO CLIMB" : "HOLD  D  TO CLIMB");
            HoldWalkie(false);
        }

        // ================================================================ L11 the observatory
        IEnumerator KObservatory(bool cut)
        {
            indoors = true;
            yield return BeginLevel(10, cut);
            if (director != null) director.ShowBanner("VOSS'S BEST", 2.2f);
            var m1 = KMan(EnemyType.Swordsman, 709f, 30.4f); var m2 = KMan(EnemyType.Swordsman, 713f, 30.4f);
            foreach (var m in new[] { m1, m2 }) if (m != null) { m.Master = true; m.SetMaxHealth(90f); }
            SetPiece.Make("THE TELESCOPE", new Vector3(KesselPlan.Telescope.x - 0.8f, 30.4f, 0f), h => StartCoroutine(SwingTelescope()), false);
            yield return KFight();
            // the crank turns the dome's slot to the roof's door
            yield return PlayerWalk(KesselPlan.DomeCrank.x - 0.6f, kx0, 1.8f, Application.isMobilePlatform ? "HOLD TO GO TO THE CRANK" : "HOLD  D  TO GO TO THE CRANK");
            hero.CinePuppet();
            hero.body.Play(hero.body.Has("push") ? "push" : "idle", 0.2f);
            Sound.Play(Sfx.Rumble, 0.8f, hero.transform.position);
            yield return HoldTo("TURN THE CRANK", 2.6f, u => { if (Random.value < 0.05f) Sound.Play(Sfx.MetalClang, 0.4f, hero.transform.position, 0.6f); });
            Sound.Play(Sfx.Door, 1f, hero.transform.position, 0.6f);
            Sound.Music("storm", 0.3f, true, 1.5f);
            indoors = false;
            kLevel = -1;
        }

        float telescopeAt;

        IEnumerator SwingTelescope()
        {
            if (Time.time < telescopeAt) yield break;
            telescopeAt = Time.time + 6f;
            var t = Find("KTelescope");
            Sound.Play(Sfx.MetalClang, 1f, hero.transform.position, 0.7f);
            float a = 0f;
            var r0 = t != null ? t.rotation : Quaternion.identity;
            while (a < 1f)
            {
                a += Time.deltaTime * 2.2f;
                if (t != null) t.rotation = r0 * Quaternion.Euler(0f, Mathf.Sin(a * Mathf.PI) * 70f, 0f);
                yield return null;
            }
            foreach (var e in Enemy.All.ToArray())
                if (e != null && e.Targetable && Mathf.Abs(e.transform.position.x - KesselPlan.Telescope.x) < 3f && e.transform.position.y > 29f)
                {
                    e.TakeHit(new HitInfo { damage = 22f, direction = e.transform.position.x > KesselPlan.Telescope.x ? 1 : -1, knockback = 6f, knockdown = true, heavy = true, attacker = hero });
                    if (director != null) director.AddStyle(200, "THE TELESCOPE");
                }
        }
    }
}
