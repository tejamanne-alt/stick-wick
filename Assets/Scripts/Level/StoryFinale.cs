using System.Collections;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>THE LAST ROOF (docs/story/chapter5_fortress.md, the finale): the tower stair in the storm,
    /// the terrace over the great hall, VOSS at the broken parapet holding Maya. He tells him what
    /// happened (the stills: render_finale.py), and he remembers. The duel in three phases (I THE
    /// TEACHER, II THE GUN: slip the shot, get in close, take it off him; III THE EDGE), the fall, her,
    /// the ribbon, the dawn, and the road home.</summary>
    public partial class StoryIntro
    {
        Enemy voss, maya;
        int vossStage, vossFlash, shotsFired;            // vossStage: 1 the teacher, 2 the gun, 3 the edge, 5 down
        bool mayaHere, finaleOver, aiming, disarming;
        float nextAim, aimU = -1f, gunPromptUntil, dawn, theEndAt = -1f, flashCutAt;
        Transform vossGun;
        LineRenderer aimLine;
        Texture2D[] vossStills;

        static Vector3 Roof(float x, float z = 0f) => new Vector3(x, FortressPlan.RoofY, z);
        static string Pick(ClipPlayer b, params string[] names)
        {
            foreach (var n in names) if (b != null && b.Has(n)) return n;
            return "idle";
        }

        IEnumerator LastRoof(int from)
        {
            fZone = 6;
            FortressCheckpoint = 7;
            hero.CinePuppet();
            barsPx = 92f;
            if (from <= 7 || from == -1)
            {
                if (from == 7) yield return FadeIn(0.4f);
                Sound.Music("storm", 0.5f);
                yield return TheStair();
                yield return TheRoof();
            }
            if (from <= 8 || from == -1)
            {
                if (from == 8) { RoofCast(); rainAmount = 1f; yield return FadeIn(0.4f); }
                yield return TheDuel();
                while (vossStage < 5) yield return null;
            }
            if (from == 9)
            {
                RoofCast();
                rainAmount = 1f;
                if (voss != null) voss.PuppetPose(Roof(FortressPlan.EdgeX - 2.6f, 0f));
                hero.CinePose(Roof(FortressPlan.EdgeX - 4.2f));
                yield return FadeIn(0.4f);
            }
            yield return TheFall();
            yield return Reunion();
            yield return Dawn();
        }

        // ================================================================ the cast on the roof

        /// <summary>Voss at the broken end of the terrace with Maya held in front of him; the hero in the doorway.</summary>
        void RoofCast()
        {
            AttachRibbon();
            mayaHere = true;
            if (director != null && voss == null)
            {
                WaveDirector.BossWeapon = FightStyle.Fists;
                WaveDirector.StoryBoss = true;
                voss = director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor);
                if (voss != null)
                {
                    voss.MakeDriver(voss.maxHealth * 1.45f);
                    voss.transform.localScale = Vector3.one * 1.04f;          // a man, not a monster
                    voss.SetBaseColor(new Color(0.07f, 0.07f, 0.08f));
                    Dress(voss.body, Look.Voss);
                    voss.Puppet();
                }
            }
            if (maya == null) maya = SpawnMaya(Roof(159.0f, -0.55f), -1);
            if (voss != null) { voss.PuppetPose(Roof(159.6f, -0.4f)); voss.SetFacing(-1); voss.PuppetPlay("idle", 0f); }
            if (maya != null) { maya.PuppetPose(Roof(159.0f, -0.55f)); maya.SetFacing(-1); maya.PuppetPlay(Pick(maya.body, "struggle", "idle"), 0f); }
            hero.CinePose(Roof(FortressPlan.RoofDoorX - 0.4f, 0f));
            hero.SetFacing(1);
        }

        // ================================================================ the stair, the roof

        /// <summary>Up the outside of the tower in the storm: his shape against the strikes, and the voices.</summary>
        IEnumerator TheStair()
        {
            hero.CinePuppet();
            barsPx = 92f;
            float face = FortressPlan.TowerZ - 4.6f;
            float[] hs = { 2.6f, 6.4f, 10.2f };
            hero.body.Play("walk", 0.2f, true, 0.9f);
            for (int i = 0; i < hs.Length; i++)
            {
                var p0 = new Vector3(FortressPlan.TowerX - 2.4f + i * 1.6f, FortressPlan.Up + hs[i], face - 0.2f);
                hero.CinePose(p0);
                hero.SetFacing(1);
                Strike(1f, i == 0 ? 0.4f : 0.15f);
                if (i == 1) Say("HER", "Promise me you'll always come home.", 2.8f);
                if (i == 2) Say("MAYA", "Papa! Look!", 1.8f);
                // low, beside him, up past him at the sky: a silhouette on the lightning
                var camAt = p0 + (i == 1 ? new Vector3(2.2f, -1.4f, -2.4f) : new Vector3(-1.6f, -1.5f, -2.2f));
                yield return Cut(i == 1 ? 2.8f : 1.9f, u => Static(camAt, hero.transform.position + new Vector3(0f, 1.6f, 0f), 40f), 0.03f,
                    u => hero.CinePose(p0 + new Vector3(u * 1.2f, u * 1.1f, 0f)));
            }
            // high, down the tower's wall: the courtyard far below him, the storm round him
            var top = new Vector3(FortressPlan.TowerX + 1.0f, FortressPlan.RoofY - 0.6f, face - 0.2f);
            hero.CinePose(top);
            yield return Cut(2.0f, u => Static(top + new Vector3(-1.4f, 4.2f, -2.2f), top + new Vector3(0f, -0.4f, 0f), 44f), 0.04f,
                u => hero.CinePose(top + Vector3.up * u * 0.6f));
        }

        IEnumerator TheRoof()
        {
            RoofCast();
            var door = Roof(FortressPlan.RoofDoorX, FortressPlan.RoofDoorZ);
            hero.CinePose(door + new Vector3(0f, 0f, -0.4f));
            hero.body.Play("idle", 0.1f);
            // (a) the door bursts open onto the terrace
            Sound.Play(Sfx.Door, 1f, door, 0.6f);
            Sound.Play(Sfx.Boom, 0.6f, door);
            if (cam != null) cam.Shake(0.25f, 0.3f);
            if (InkFx.Instance != null) InkFx.Instance.Splash(door + Vector3.up * 1.2f, Vector3.back, 24, new Color(0.85f, 0.85f, 0.85f));
            Strike(1f, 0.1f);
            yield return Cut(1.6f, u => Static(Roof(FortressPlan.RoofDoorX - 2.6f, -1.2f) + Vector3.up * 0.35f, door + Vector3.up * 1.3f, 42f), 0.02f,
                u => hero.CinePose(Vector3.Lerp(door + new Vector3(0f, 0f, -0.4f), Roof(FortressPlan.RoofDoorX, 0.4f), u)));
            hero.CinePose(Roof(FortressPlan.RoofDoorX + 0.4f, 0f));
            hero.SetFacing(1);
            // (b) the reverse, down the terrace: a man at the broken end, holding a child, in the rain
            var vh = voss != null ? HeadOf(voss) : null;
            System.Func<Vector3> vHead = () => vh != null ? vh.position : Roof(159.6f) + Vector3.up * 1.7f;
            Strike(0.9f, 0.3f);
            yield return Cut(3.0f, u => CineCam.DollyIn(vHead(), new Vector3(1f, -0.12f, 0.5f), u, 10f, 6.5f, 30f), 0.03f);
            var mh = maya != null ? HeadOf(maya) : null;
            Say("MAYA", "PAPA!", 1.8f, mh);
            yield return Cut(1.8f, u => Static((mh != null ? mh.position : vHead()) + new Vector3(-1.1f, 0.05f, -0.9f), mh != null ? mh.position : vHead(), 30f), 0.015f);
            yield return Cut(1.4f, u => Static(HeroHead() + new Vector3(1.0f, 0.0f, -1.0f), HeroHead(), 30f), 0.01f);
            // (c) VOSS, low: the face from the photo, twelve years older
            if (director != null) director.ShowBanner("VOSS", 2.6f);
            Sound.Play(Sfx.CineMassive, 0.6f);
            Say("VOSS", "Look at you. Twelve years, and you still walk like one of mine.", 3.4f, vh);
            yield return Cut(3.4f, u => CineCam.LowAngleArc(vHead() + Vector3.down * 0.5f, u, 2.8f, -40f, -20f, 34f), 0.015f);
            Say("YOU", "Let her go.", 1.8f);
            yield return Cut(2.0f, u => CineCam.DollyIn(HeroHead(), new Vector3(-0.5f, -0.05f, 1f), u, 2.2f, 1.4f, 30f), 0.015f);
            // flash 3: three nights... last night, the window, the phone: the order he gave
            Say("VOSS", "You know why we're here. You remember now. That night.", 3.0f, vh);
            yield return Cut(2.4f, u => Static(vHead() + new Vector3(-1.4f, -0.1f, -1.2f), vHead(), 28f), 0.015f);
            yield return VossFlash(2, 3.0f, "VOSS", "Bring them to me. All of them. Alive.");
            Say("VOSS", "Six years I lived on your mercy. One phone call, and I'm finished.", 3.6f, vh);
            yield return Cut(3.6f, u => CineCam.DollyIn(vHead(), new Vector3(1f, -0.1f, 0.6f), u, 2.4f, 1.6f, 28f), 0.015f);
            Say("VOSS", "Then a detective at your door. What was I meant to do? Wait?", 3.4f, vh);
            yield return Cut(3.4f, u => Static(Roof(154f, -6.5f) + Vector3.up * 1.6f, Roof(154.5f, 0f) + Vector3.up * 1.2f, 40f), 0.025f);
            Say("VOSS", "I told him to bring her. He never listened. Not once in his life.", 3.6f, vh);
            yield return Cut(3.6f, u => Static(vHead() + new Vector3(-0.9f, 0.3f, -0.9f), vHead(), 28f), 0.015f);
            Say("YOU", "Your boy killed Elena.", 2.2f);
            Sound.Play(Sfx.Heartbeat, 0.9f);
            yield return Cut(2.4f, u => CineCam.DollyIn(HeroHead(), new Vector3(0.4f, 0.2f, 1f), u, 1.5f, 1.0f, 28f), 0.01f);
            Say("VOSS", "...I know. And I still loved him. That's what fathers do. You'd know.", 4.0f, vh);
            yield return Cut(4.0f, u => Static(vHead() + new Vector3(-0.8f, -0.2f, -0.8f), vHead(), 26f), 0.01f);
            Say("YOU", "And I killed him.", 2.0f);
            yield return Cut(2.2f, u => Static(HeroHead() + new Vector3(-0.8f, -0.3f, -0.8f), HeroHead(), 30f), 0.01f);
            Say("VOSS", "Yes. So here we are.", 2.2f, vh);
            yield return Cut(2.4f, u => Static(Roof(156f, -7.5f) + Vector3.up * 2.2f, Roof(156.5f, 0f) + Vector3.up * 1.1f, 44f), 0.03f);
            // THE RAIN: it has held off all night. Now, here, with the two of them face to face, it comes.
            yield return TheRain(vHead);
            // THE TRADE: everything he sent has failed; this is his last card, and it's force
            if (maya != null) { maya.PuppetPose(Roof(FortressPlan.EdgeX - 0.6f, -2.4f)); maya.PuppetPlay(Pick(maya.body, "struggle", "idle"), 0.1f); }
            if (voss != null) voss.PuppetPose(Roof(FortressPlan.EdgeX - 1.2f, -2.0f));
            Sound.Play(Sfx.Struggle, 0.6f, mh != null ? mh.position : vHead());
            Say("VOSS", "You've killed my whole house to get here. Now: the book. Or she learns to fly.", 4.0f, vh);
            yield return Cut(4.0f, u => Static(Roof(FortressPlan.EdgeX - 3.0f, -5.2f) + Vector3.up * 0.6f, Roof(FortressPlan.EdgeX - 0.8f, -2.2f) + Vector3.up * 0.9f, 36f), 0.03f);
            Walkie("...Papa?", 1.4f);
            yield return Cut(1.6f, u => Static((mh != null ? mh.position : vHead()) + new Vector3(-0.9f, 0.0f, -0.8f), mh != null ? mh.position : vHead(), 30f), 0.015f);
            // he slides it across the wet stones to him
            bookProp = MakeBook();
            bookProp.position = hero.Chest + Vector3.right * 0.3f;
            var b0 = Roof(hero.transform.position.x + 0.6f, -0.4f) + Vector3.up * 0.04f;
            var b1 = Roof(FortressPlan.EdgeX - 1.8f, -1.8f) + Vector3.up * 0.04f;
            System.Action tradeShot = () => Cine(Static(Roof((b0.x + b1.x) * 0.5f, -5.0f) + Vector3.up * 0.8f, Roof((b0.x + b1.x) * 0.5f, -1.0f) + Vector3.up * 0.3f, 40f));
            StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, tradeShot));
            yield return DragTo("SLIDE THE BOOK TO HIM", Vector2.right, p => { if (bookProp != null) bookProp.position = Vector3.Lerp(b0, b1, p); }, b0);
            Sound.Play(Sfx.Cloth, 0.6f, b1);
            yield return Cut(1.2f, u => Static(b1 + new Vector3(-0.6f, 0.4f, -0.8f), b1, 30f), 0.01f);
            // he lets her go: she runs to the door
            Say("VOSS", "Run along, little one.", 2.0f, vh);
            if (voss != null) { voss.PuppetPose(Roof(FortressPlan.EdgeX - 1.9f, -1.6f)); voss.PuppetPlay(Pick(voss.body, "kneel_hold", "idle"), 0.2f); }
            if (maya != null) maya.PuppetPlay(Pick(maya.body, "run", "walk"), 0.1f, 1.3f);
            var m0 = maya != null ? maya.transform.position : Vector3.zero;
            var m1 = Roof(FortressPlan.RoofDoorX + 1.6f, 3.4f);
            yield return Cut(2.6f, u => Static(Roof(155.5f, -5.5f) + Vector3.up * 1.4f, Roof(155.5f, 0.5f) + Vector3.up * 0.8f, 44f), 0.02f,
                u => { if (maya != null) { maya.SetFacing(-1); maya.PuppetPose(Vector3.Lerp(m0, m1, Mathf.SmoothStep(0f, 1f, u))); } });
            if (maya != null) maya.PuppetPlay(Pick(maya.body, "kneel_hurt", "idle"), 0.2f);
            // he weighs the book, lets it fall, and draws the blade from his cane
            if (bookProp != null) bookProp.position = Roof(FortressPlan.EdgeX - 1.9f, -1.5f) + Vector3.up * 0.04f;
            if (voss != null) voss.PuppetPlay("idle", 0.2f);
            Say("VOSS", "...I did teach you better than to trust me.", 2.8f, vh);
            Sound.Play(Sfx.SwordSwing, 0.9f, vHead());
            Sound.Play(Sfx.Glint, 0.8f, vHead());
            yield return Cut(2.8f, u => CineCam.LowAngleArc(vHead() + Vector3.down * 0.6f, u, 2.4f, -20f, 10f, 32f), 0.015f);
            Say("YOU", "...I remember. All of it.", 2.4f);
            yield return Cut(2.4f, u => CineCam.DollyIn(HeroHead(), new Vector3(0.4f, 0.2f, 1f), u, 1.5f, 0.9f, 28f), 0.01f);
        }

        Transform bookProp;

        /// <summary>The first rain of the night, at the peak of the face-off: silence; one fat drop on the
        /// stone between them; a second on the ribbon at his wrist; a look up as the sky splits with the
        /// biggest strike yet; then the downpour sweeps across the terrace onto them, wide.</summary>
        IEnumerator TheRain(System.Func<Vector3> vHead)
        {
            var mid = Roof((hero.transform.position.x + (voss != null ? voss.transform.position.x : 159f)) * 0.5f, -0.2f);
            Sound.Ambience(0.1f, 0.25f, 22000f);                                // the wind drops: a held breath
            Sound.MusicStop(1.2f);
            yield return Cut(1.4f, u => Static(HeroHead() + new Vector3(0.9f, -0.05f, -1.0f), HeroHead(), 28f), 0.006f);
            // (1) one fat drop on the stone between them: a low close insert, a ring spreading
            var drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(drop.GetComponent<Collider>());
            drop.transform.localScale = new Vector3(0.035f, 0.05f, 0.035f);
            drop.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.85f, 0.88f, 0.92f) };
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.7f, 0.72f, 0.76f) };
            ring.SetActive(false);
            yield return Cut(1.2f, u => Static(mid + new Vector3(-0.5f, 0.12f, -0.6f), mid + Vector3.up * 0.15f, 30f), 0.002f,
                u => drop.transform.position = mid + Vector3.up * Mathf.Lerp(1.6f, 0.02f, u * u));
            Sound.Play(Sfx.Drip, 1f, mid, 0.8f);
            drop.SetActive(false);
            ring.SetActive(true);
            ring.transform.position = mid + Vector3.up * 0.005f;
            yield return Cut(1.0f, u => Static(mid + new Vector3(-0.5f, 0.12f, -0.6f), mid + Vector3.up * 0.15f, 30f), 0.002f,
                u => ring.transform.localScale = new Vector3(0.05f + u * 0.5f, 0.001f, 0.05f + u * 0.5f));
            ring.SetActive(false);
            // (2) the second, on the ribbon at his wrist
            var wrist = wristBand != null ? wristBand.position : hero.Chest;
            Sound.Play(Sfx.Drip, 0.8f, wrist, 1.1f);
            yield return Cut(1.0f, u => Static(wrist + new Vector3(0.12f, 0.2f, -0.45f), wrist, 24f), 0.002f);
            // (3) he looks up; Voss looks up; the sky splits
            yield return Cut(1.2f, u => Static(vHead() + new Vector3(-0.9f, -0.4f, -0.8f), vHead() + Vector3.up * 0.4f, 30f), 0.005f);
            Strike(1f, 0f);
            Sound.Play(Sfx.Thunder, 1f);
            if (cam != null) cam.Shake(0.3f, 0.6f);
            Sound.Music("duel", 0.55f, true, 2f);
            // (4) the downpour sweeps across the terrace onto them: wide, as it arrives
            yield return Cut(3.2f, u => Static(Roof(156f, -8.5f) + Vector3.up * 3.2f, Roof(156.5f, 0f) + Vector3.up * 1.0f, Mathf.Lerp(40f, 46f, u)), 0.03f,
                u => rainAmount = Mathf.SmoothStep(0f, 1f, u));
            rainAmount = 1f;
            Destroy(drop); Destroy(ring);
            // the rain running off him
            yield return Cut(1.8f, u => Static(HeroHead() + new Vector3(0.7f, 0.0f, -0.9f), HeroHead(), 28f), 0.008f);
        }

        /// <summary>The black book: a small dark block with a pale page edge.</summary>
        static Transform MakeBook()
        {
            var root = new GameObject("TheBook").transform;
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cover.GetComponent<Collider>());
            cover.transform.SetParent(root, false);
            cover.transform.localScale = new Vector3(0.2f, 0.035f, 0.14f);
            cover.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.03f, 0.03f, 0.035f) };
            var pages = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(pages.GetComponent<Collider>());
            pages.transform.SetParent(root, false);
            pages.transform.localPosition = new Vector3(0.005f, 0f, 0f);
            pages.transform.localScale = new Vector3(0.19f, 0.026f, 0.142f);
            pages.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.86f, 0.85f, 0.8f) };
            return root;
        }

        /// <summary>A memory, broken: one of Voss's stills, flickering, a line over it.</summary>
        IEnumerator VossFlash(int which, float secs, string who, string line)
        {
            Sound.Play(Sfx.FlashCut, 0.9f);
            Sound.Play(Sfx.Tinnitus, 0.4f);
            if (which == 3) { Sound.Play(Sfx.PipeHit, 1f); Sound.Play(Sfx.Bone, 0.8f); }
            vossFlash = which;
            flashCutAt = Time.unscaledTime;
            if (who != null) Say(who, line, secs);
            yield return RealWait(secs);
            vossFlash = 0;
            Sound.Play(Sfx.FlashCut, 0.6f);
        }

        // ================================================================ the duel

        IEnumerator TheDuel()
        {
            if (voss == null) RoofCast();
            if (voss == null) { vossStage = 5; yield break; }
            if (maya != null) { maya.PuppetPose(Roof(FortressPlan.RoofDoorX + 1.6f, 3.4f)); maya.PuppetPlay(Pick(maya.body, "kneel_hurt", "idle"), 0f); }
            voss.PuppetPose(Roof(157.5f));
            voss.SetFacing(-1);
            hero.CinePose(Roof(152.5f));
            hero.SetFacing(1);
            Enemy.StoryFinale = (e, hit) => { if (vossStage < 5) vossStage = 5; };
            // he squares up: the stance he taught him
            voss.PuppetPlay(Pick(voss.body, "block_high", "idle"), 0.2f);
            Sound.Music("duel", 0.7f, true, 1.2f);
            Strike(1f, 0.1f);
            Say("VOSS", "Show me what's left of my best student.", 2.6f, HeadOf(voss));
            yield return Cut(2.6f, u => CineCam.LowAngleArc(new Vector3(155f, FortressPlan.RoofY + 1.2f, 0f), u, 5.0f, -70f, -110f, 40f), 0.02f);
            SetArena(138f, FortressPlan.RoofX1);
            FortressCheckpoint = 7;
            CineOff();
            barsPx = 0f;
            voss.PuppetRelease();
            voss.Passive = false;
            hero.CineRelease();
            hero.runSpeed = runSpeed;
            vossStage = 1;
            shotsFired = 0;
            ShowStreetBanner("I  ·  THE TEACHER");
            Sound.Music("duel", 0.7f, true, 1.0f);
            Sound.Tension(true);
        }

        void UpdateVoss()
        {
            if (!mayaHere) return;
            if (vossStage >= 5) { UpdateDawn(); return; }
            if (vossStage == 0 || voss == null) return;
            if (!voss.Alive) { vossStage = 5; return; }
            float hp = voss.HealthFraction;
            if (vossStage == 1 && hp <= 0.62f)
            {
                vossStage = 2;
                nextAim = Time.time + 0.8f;
                ShowStreetBanner("II  ·  THE GUN");
                Say("VOSS", "Every one of them down there, you taught. I taught you.", 3.0f, HeadOf(voss));
                ShowGun(true);
                Sound.Play(Sfx.GunDraw, 0.9f, voss.Chest);
            }
            if (vossStage == 2 && !aiming && !disarming)
            {
                if (hp <= 0.3f) StartCoroutine(Disarm());
                else if (Time.time >= nextAim && !hero.IsDead && !hero.CinematicLock) StartCoroutine(VossAim());
            }
            if (vossStage == 3 && nextStrike > Time.unscaledTime + 4f) nextStrike = Time.unscaledTime + Random.Range(1.6f, 3.6f);   // the storm on top of them
        }

        void ShowGun(bool on)
        {
            if (vossGun == null && on && voss != null && voss.body != null)
            {
                var hand = voss.body.Bone("hand.R");
                if (hand == null) return;
                vossGun = new GameObject("VossGun").transform;
                vossGun.SetParent(hand, false);
                vossGun.localPosition = new Vector3(0f, 0.06f, 0f);
                var ink = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.02f, 0.02f, 0.02f) };
                foreach (var (p, s) in new[] { (Vector3.zero, new Vector3(0.05f, 0.11f, 0.035f)), (new Vector3(0f, 0.06f, 0.05f), new Vector3(0.045f, 0.045f, 0.16f)) })
                {
                    var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(c.GetComponent<Collider>());
                    c.transform.SetParent(vossGun, false);
                    c.transform.localPosition = p;
                    c.transform.localScale = s;
                    c.GetComponent<Renderer>().sharedMaterial = ink;
                }
            }
            if (vossGun != null) vossGun.gameObject.SetActive(on);
        }

        Vector3 VossMuzzle => vossGun != null ? vossGun.position + Vector3.right * (voss != null ? voss.Facing : 1) * 0.14f : voss != null ? voss.Chest + Vector3.up * 0.25f : Vector3.zero;

        void AimLine(bool on, float u = 0f)
        {
            if (aimLine == null)
            {
                var go = new GameObject("VossAim");
                aimLine = go.AddComponent<LineRenderer>();
                aimLine.positionCount = 2;
                aimLine.useWorldSpace = true;
                aimLine.sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.85f, 0.05f, 0.06f) };
                aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            aimLine.enabled = on && (u < 0.75f || Mathf.Repeat(Time.unscaledTime * 18f, 1f) > 0.35f);
            if (!on) return;
            float wdt = Mathf.Lerp(0.008f, 0.022f, u);
            aimLine.startWidth = wdt;
            aimLine.endWidth = wdt * 0.6f;
            aimLine.SetPosition(0, VossMuzzle);
            aimLine.SetPosition(1, hero.Chest + Vector3.up * 0.25f);
        }

        /// <summary>Phase II: he steps off, raises the gun, the red line finds him, and he fires. Slip it
        /// (dodge as it goes off) or get in close while he aims and take it off him.</summary>
        IEnumerator VossAim()
        {
            aiming = true;
            voss.Puppet();
            int d = hero.transform.position.x > voss.transform.position.x ? 1 : -1;
            voss.SetFacing(d);
            // a step off him first, if he's too close to aim
            float gap = Mathf.Abs(hero.transform.position.x - voss.transform.position.x);
            if (gap < 3.6f)
            {
                var v0 = voss.transform.position;
                float tx = Mathf.Clamp(v0.x - d * (3.8f - gap), 139f, FortressPlan.RoofX1 - 0.4f);
                voss.PuppetPlay(Pick(voss.body, "dodge_back", "walk"), 0.05f, 1.6f);
                for (float t = 0f; t < 0.32f; t += Time.deltaTime)
                {
                    voss.PuppetPose(Vector3.Lerp(v0, new Vector3(tx, v0.y, v0.z), t / 0.32f));
                    yield return null;
                }
            }
            voss.PuppetPlay(Pick(voss.body, "gun_aim", "gun_idle", "idle"), 0.08f);
            Sound.Play(Sfx.GunCock, 1f, voss.Chest);
            gunPromptUntil = Time.unscaledTime + 1.8f;
            float aimFor = shotsFired == 0 ? 1.7f : Random.Range(1.2f, 1.5f), t0 = 0f;
            bool closed = false;
            while (t0 < aimFor)
            {
                t0 += Time.deltaTime;
                aimU = t0 / aimFor;
                voss.SetFacing(hero.transform.position.x > voss.transform.position.x ? 1 : -1);
                AimLine(true, aimU);
                if (Mathf.Abs(hero.transform.position.x - voss.transform.position.x) < 1.5f && !hero.IsDead) { closed = true; break; }
                yield return null;
            }
            AimLine(false);
            aimU = -1f;
            if (closed) { yield return Disarm(); aiming = false; yield break; }
            // the shot
            Sound.Play(Sfx.GunshotIndoor, 1f, VossMuzzle);
            Sound.Play(Sfx.ShellCasing, 0.6f, VossMuzzle);
            if (InkFx.Instance != null) { InkFx.Instance.ImpactFlash(0.05f); InkFx.Instance.Splash(VossMuzzle, Vector3.right * voss.Facing, 8, Color.white); }
            if (cam != null) cam.Shake(0.12f, 0.15f);
            shotsFired++;
            if (hero.Invulnerable || hero.Ducking)
            {
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(200, "SLIPPED THE SHOT");
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.35f, 0.02f, 0.2f);
                if (InkFx.Instance != null) InkFx.Instance.Splash(hero.Chest + new Vector3(voss.Facing * 2.5f, 0.2f, 1.5f), Vector3.up, 12, new Color(0.9f, 0.9f, 0.9f));
            }
            else if (!hero.IsDead)
                hero.TakeHit(new HitInfo { damage = 14f, direction = voss.Facing, knockback = 3.5f, heavy = true });
            yield return new WaitForSeconds(0.4f);
            if (voss != null && voss.Alive && vossStage == 2)
            {
                voss.PuppetRelease();
                voss.Passive = false;
            }
            nextAim = Time.time + Random.Range(3.4f, 5.2f);
            aiming = false;
        }

        /// <summary>In close: he turns the wrist, the shot goes into the sky, the gun goes off the roof.</summary>
        IEnumerator Disarm()
        {
            disarming = true;
            AimLine(false);
            aimU = -1f;
            hero.CinePuppet();
            voss.Puppet();
            int d = voss.transform.position.x > hero.transform.position.x ? 1 : -1;
            hero.SetFacing(d);
            voss.SetFacing(-d);
            var vp = voss.transform.position;
            hero.CinePose(new Vector3(vp.x - d * 0.85f, FortressPlan.RoofY, 0f));
            hero.body.Play(Pick(hero.body, "gedan_barai", "parry", "block_high"), 0.04f);
            voss.PuppetPlay(Pick(voss.body, "gun_aim", "gun_idle", "idle"), 0.04f);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.2f, 1.6f, 0.02f, 0.3f);
            Sound.Play(Sfx.SlowmoIn, 0.8f);
            var mid = (hero.transform.position + vp) * 0.5f + Vector3.up * 1.2f;
            // low and close on the two of them and the gun between them
            yield return Cut(0.8f, u => Static(mid + new Vector3(-d * 0.4f, -0.7f, -1.6f), mid, 34f), 0.01f);
            Sound.Play(Sfx.GunshotIndoor, 1f, mid + Vector3.up * 0.4f);
            Strike(1f, 0f);
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.06f);
            hero.body.Play(Pick(hero.body, "gyaku_zuki", "oi_zuki", "punch"), 0.03f);
            voss.PuppetPlay(Pick(voss.body, "hit_front", "idle"), 0.03f, 0.7f);
            Sound.Play(Sfx.PunchHeavy, 1f, mid);
            Sound.Play(Sfx.Bone, 0.7f, mid);
            // the gun: out of his hand, over the parapet, into the storm
            Vector3 g0 = vossGun != null ? vossGun.position : mid;
            if (vossGun != null) vossGun.SetParent(null, true);
            var gv = new Vector3(d * 2.2f, 4.5f, -3.4f);
            yield return Cut(1.6f, u => Static(g0 + new Vector3(-d * 2.4f, 0.6f, -1.2f), vossGun != null ? vossGun.position : g0, 40f), 0.02f,
                u =>
                {
                    if (vossGun == null) return;
                    float tt = u * 1.6f;
                    vossGun.position = g0 + gv * tt + Vector3.down * 4.9f * tt * tt;
                    vossGun.Rotate(0f, 0f, 900f * Time.unscaledDeltaTime);
                });
            if (vossGun != null) { Destroy(vossGun.gameObject); vossGun = null; }
            Sound.Play(Sfx.SlowmoOut, 0.6f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(400, "DISARMED");
            // him: furious now
            Say("VOSS", "...Then let's finish what I started.", 2.4f, HeadOf(voss));
            Strike(0.9f, 0.2f);
            yield return Cut(2.2f, u => CineCam.DollyIn(HeadOf(voss).position, new Vector3(-d * 1f, -0.15f, 0.6f), u, 2.4f, 1.5f, 30f), 0.015f);
            vossStage = 3;
            voss.DriverPhase = 3;
            voss.QueueEnrage();
            ShowStreetBanner("III  ·  THE EDGE");
            CineOff();
            barsPx = 0f;
            hero.CineRelease();
            hero.runSpeed = runSpeed;
            voss.PuppetRelease();
            voss.Passive = false;
            disarming = false;
        }

        // ================================================================ the fall

        IEnumerator TheFall()
        {
            Sound.Tension(false);
            AimLine(false);
            vossStage = 5;
            if (voss == null) yield break;
            voss.Puppet();
            yield return RealWait(0.4f);
            hero.CinePuppet();
            barsPx = 92f;
            var edge = Roof(FortressPlan.EdgeX + 0.2f, -2.9f);
            // the last blow drives him back to the broken end
            var v0 = voss.transform.position;
            hero.CinePose(Roof(Mathf.Min(v0.x - 1.2f, FortressPlan.EdgeX - 2.2f)));
            hero.SetFacing(1);
            voss.SetFacing(-1);
            voss.PuppetPlay(Pick(voss.body, "stagger", "hit_front", "idle"), 0.05f, 0.6f);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 1.4f, 0.02f, 0.3f);
            Strike(1f, 0.1f);
            Sound.Music("storm", 0.45f, true, 2f);
            yield return Cut(1.8f, u => Static(Roof(FortressPlan.EdgeX - 3.4f, -5.2f) + Vector3.up * 0.4f, Roof(FortressPlan.EdgeX - 0.6f, -1.6f) + Vector3.up * 1.2f, 40f), 0.02f,
                u => voss.PuppetPose(Vector3.Lerp(v0, edge, Mathf.SmoothStep(0f, 1f, u))));
            // over: one hand on the broken stone
            Sound.Play(Sfx.BodyFall, 0.9f, edge);
            Sound.Play(Sfx.Crate, 0.6f, edge);
            var hang = edge + new Vector3(0.1f, -1.85f, -0.5f);
            voss.PuppetPose(hang);
            voss.PuppetPlay(Pick(voss.body, "hang", "idle"), 0.05f);
            hero.CinePose(Roof(FortressPlan.EdgeX - 0.4f, -2.0f));
            hero.body.Play(Pick(hero.body, "kneel_hand", "kneel_hold", "idle"), 0.2f);
            var vh = HeadOf(voss);
            // high, over the edge, down at him: the drop under him, the valley's lights
            yield return Cut(2.4f, u => Static(edge + new Vector3(-0.8f, 2.6f, 1.2f), vh.position + Vector3.down * 0.6f, 44f), 0.03f);
            Say("VOSS", "We're the same, you and me. You know we are.", 3.2f, vh);
            yield return Cut(3.2f, u => Static(vh.position + new Vector3(0.7f, -1.2f, -1.6f), vh.position + Vector3.up * 0.4f, 30f), 0.02f);
            Say("VOSS", "Give me your hand, son.", 2.2f, vh);
            StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(Static(edge + new Vector3(-0.8f, 2.6f, 1.2f), vh.position + Vector3.down * 0.6f, 44f))));
            yield return ChooseFrom("REACH FOR HIM", "LET GO");
            reached = chosen == 0;
            if (reached)
            {
                // he reaches; Voss's glove comes off in his hand, empty
                hero.body.Play(Pick(hero.body, "kneel_hand", "kneel_hold", "idle"), 0.1f);
                Sound.Play(Sfx.Cloth, 0.8f, vh.position);
                Say("YOU", "Take it.", 1.4f);
                yield return Cut(1.6f, u => Static(edge + new Vector3(-0.3f, 0.9f, 0.6f), edge + Vector3.down * 0.6f, 32f), 0.02f);
                Sound.Play(Sfx.Crate, 0.7f, edge, 0.8f);
                Say("VOSS", "...Good boy.", 1.8f, vh);
                yield return Cut(2.0f, u => Static(vh.position + new Vector3(0.7f, -1.2f, -1.6f), vh.position + Vector3.up * 0.4f, 30f), 0.02f);
            }
            else
            {
                Say("YOU", "No. We're not.", 1.8f);
                yield return Cut(2.0f, u => Static(HeroHead() + new Vector3(0.6f, -0.6f, -1.0f), HeroHead(), 30f), 0.01f);
                Say("YOU", "I came home.", 2.4f);
                yield return Cut(2.6f, u => CineCam.DollyIn(HeroHead(), new Vector3(-0.3f, 0.2f, 1f), u, 1.5f, 1.0f, 28f), 0.01f);
            }
            // the lightning finds the broken parapet; the stone gives
            Lightning(1f, 1f, 0f);
            if (LightningSwap.Instance != null) LightningSwap.Instance.Bolt(edge + new Vector3(2f, 40f, 4f), edge + Vector3.up * 0.2f, 0.9f);
            nextStrike = Time.unscaledTime + 4f;
            Sound.Play(Sfx.Boom, 1f, edge);
            Sound.Play(Sfx.Thunder, 1f, edge);
            if (cam != null) cam.Shake(0.4f, 0.6f);
            if (InkFx.Instance != null) { InkFx.Instance.ImpactFlash(0.1f); InkFx.Instance.Splash(edge, Vector3.up, 40, new Color(0.85f, 0.85f, 0.85f)); }
            yield return Cut(0.9f, u => Static(Roof(FortressPlan.EdgeX - 4f, -6f) + Vector3.up * 1.0f, edge, 40f), 0.08f);
            // he falls into the storm
            Sound.Play(Sfx.FallScream, 1f, hang);
            var lookFrom = edge + new Vector3(-0.4f, 2.0f, 0.6f);
            yield return Cut(2.6f, u => Static(lookFrom, voss.transform.position, 46f), 0.03f,
                u => { float tt = u * 2.6f; voss.PuppetPose(hang + new Vector3(0.3f * tt, -4.9f * tt * tt, -0.9f * tt)); });
            voss.gameObject.SetActive(false);
            // on him, in the rain, on his knees at the edge
            hero.body.Play(Pick(hero.body, "kneel_hurt", "kneel_hand", "idle"), 0.3f);
            yield return Cut(2.4f, u => CineCam.DronePullAway(hero.transform.position + Vector3.up * 0.7f, u, 2.2f, 7f, 0.6f, 5f, 40f), 0.03f);
        }

        // ================================================================ her

        IEnumerator Reunion()
        {
            Sound.Music("finale", 0.75f, false, 2.5f);
            hero.CinePose(Roof(FortressPlan.EdgeX - 2.4f));
            hero.SetFacing(-1);
            hero.body.Play("idle", 0.3f);
            if (maya == null) yield break;
            // she runs to him across the wet stones
            var m0 = Roof(FortressPlan.RoofDoorX + 1.6f, 1.0f);
            var m1 = Roof(FortressPlan.EdgeX - 3.0f, 0f);
            maya.PuppetPose(m0);
            maya.SetFacing(1);
            maya.PuppetPlay(Pick(maya.body, "run", "walk"), 0.1f, 1.3f);
            Sound.Play(Sfx.Splash, 0.5f, m0);
            yield return Cut(3.2f, u => Static(Roof(FortressPlan.EdgeX - 6f, -5.5f) + Vector3.up * 1.0f, Vector3.Lerp(m0, m1, u) + Vector3.up * 0.6f, 38f), 0.025f,
                u => maya.PuppetPose(Vector3.Lerp(m0, m1, u)));
            // he goes down to her; she's in his arms
            hero.body.Play(Pick(hero.body, "kneel_hold", "kneel_hand", "idle"), 0.25f);
            maya.PuppetPose(Roof(FortressPlan.EdgeX - 2.85f, -0.1f));
            maya.PuppetPlay("idle", 0.2f);
            var mh = HeadOf(maya);
            Sound.Play(Sfx.Cloth, 0.8f, m1);
            yield return Cut(2.4f, u => CineCam.DollyIn((mh.position + HeroHead()) * 0.5f, new Vector3(0.2f, -0.1f, 1f), u, 3.0f, 2.0f, 34f), 0.015f);
            Say("MAYA", "Papa... you came.", 2.4f, mh);
            yield return Cut(2.6f, u => Static(mh.position + new Vector3(0.7f, 0.0f, -0.9f), mh.position, 28f), 0.01f);
            Say("YOU", "I promised, sweetheart.", 2.4f);
            yield return Cut(2.6f, u => Static(HeroHead() + new Vector3(-0.7f, -0.05f, -0.9f), HeroHead(), 28f), 0.01f);
            Say("MAYA", "Mama said you'd always come home.", 3.0f, mh);
            yield return Cut(3.0f, u => CineCam.DollyIn(mh.position, new Vector3(-0.4f, 0.05f, 1f), u, 1.6f, 1.0f, 28f), 0.01f);
            Say("YOU", "...Always.", 2.2f);
            Sound.Play(Sfx.MemoryMaya, 0.5f);
            yield return Cut(2.6f, u => Static(HeroHead() + new Vector3(0.5f, 0.1f, -0.9f), HeroHead(), 26f), 0.01f);
            // the ribbon, off his wrist, back in her hair: the player ties it
            {
                var hairAt = mh.position + Vector3.up * 0.09f;
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(CineCam.DollyIn(hairAt, new Vector3(0.3f, -0.4f, 1f), 1f, 0.9f, 0.6f, 26f))));
                yield return DragTo("TIE HER RIBBON", Vector2.up, null, hairAt);
            }
            TieRibbon();
            Sound.Play(Sfx.Cloth, 0.7f, mh.position);
            Sound.Play(Sfx.MusicNote, 0.5f);
            yield return Cut(3.0f, u => CineCam.DollyIn(mh.position + Vector3.up * 0.08f, new Vector3(0.3f, -0.4f, 1f), u, 0.9f, 0.6f, 26f), 0.01f);
        }

        /// <summary>The red ribbon he wore all the way here: a bow in her hair again.</summary>
        void TieRibbon()
        {
            if (maya == null) return;
            var hd = HeadOf(maya);
            if (wristBand != null) { Destroy(wristBand.gameObject); wristBand = null; }
            var red = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.78f, 0.05f, 0.06f) };
            var bow = new GameObject("MayaBow").transform;
            bow.SetParent(hd, false);
            bow.position = hd.position + Vector3.up * 0.09f + Vector3.left * 0.03f;
            foreach (float s in new[] { -1f, 1f })
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(bow, true);
                c.transform.position = bow.position + Vector3.right * s * 0.045f;
                c.transform.rotation = Quaternion.Euler(0f, 0f, s * 25f);
                c.transform.localScale = new Vector3(0.07f, 0.045f, 0.02f) / Mathf.Max(0.01f, bow.lossyScale.x) * bow.localScale.x;
                c.GetComponent<Renderer>().sharedMaterial = red;
            }
        }

        // ================================================================ the dawn

        void UpdateDawn()
        {
            if (storm) return;
            lensRain = Mathf.MoveTowards(lensRain, 0f, Time.unscaledDeltaTime * 0.15f);
        }

        bool reached;

        IEnumerator Dawn()
        {
            // the storm goes over: the rain thins and stops, the sky comes up
            storm = false;
            if (rainPs != null) rainPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Sound.Ambience(0.25f, 0.25f, 22000f);
            Sound.Play(Sfx.Wind, 0.35f);
            LightningSwap.Held = true;
            yield return Cut(5.0f, u => Static(Roof(FortressPlan.EdgeX + 4f, -8f) + Vector3.up * 2.6f, Roof(FortressPlan.EdgeX - 3f) + Vector3.up * 0.8f, Mathf.Lerp(30f, 46f, u)), 0.01f,
                u => dawn = u);
            // 6.10 the book lies open in the rain where he dropped it: he picks it up, and puts it inside his coat
            var bp = bookProp != null ? bookProp.position : Roof(FortressPlan.EdgeX - 1.9f, -1.5f);
            hero.CinePose(new Vector3(bp.x - 0.7f, FortressPlan.RoofY, bp.z + 0.2f));
            hero.SetFacing(1);
            hero.body.Play(Pick(hero.body, "kneel_hand", "idle"), 0.3f);
            if (maya != null) { maya.PuppetPose(new Vector3(bp.x - 1.3f, FortressPlan.RoofY, bp.z + 0.4f)); maya.SetFacing(1); maya.PuppetPlay("idle", 0.2f); }
            yield return Cut(3.0f, u => Static(bp + new Vector3(-0.5f, 0.6f, -0.9f), bp, 30f), 0.01f);
            if (bookProp != null) bookProp.gameObject.SetActive(false);
            Sound.Play(Sfx.Cloth, 0.7f, hero.Chest);
            Say("YOU", "...Forty-one names.", 2.2f);
            yield return Cut(2.4f, u => Static(HeroHead() + new Vector3(0.7f, -0.1f, -1.0f), HeroHead(), 30f), 0.01f);
            hero.body.Play("idle", 0.3f);
            Walkie("Papa? ...Can we go home now? Over.", 2.8f);
            yield return Cut(3.0f, u => Static(Roof(bp.x - 1.0f, -3.5f) + Vector3.up * 1.0f, Roof(bp.x - 1.0f, bp.z) + Vector3.up * 0.8f, 36f), 0.01f);
            Say("YOU", "...Over and out, sweetheart. We're going home.", 3.0f);
            yield return Cut(5.0f, u => CineCam.DronePullAway(hero.transform.position + Vector3.up * 0.8f, u, 2.4f, 22f, 0.8f, 14f, 44f), 0.01f);
            cardAt = Time.unscaledTime;
            cardLine = reached ? "He reached for him anyway." : "No. I came home.";
            cardTitle = "THE STORM";
            yield return RealWait(6.0f);
            CineOff();
            Sound.MusicStop(4f);
            phase = Phase.End;
        }

        // ================================================================ on screen

        void FinaleGUI(float w, float h)
        {
            if (dawn > 0f && cardAt < 0f && blackout < 0.99f)
            {
                Fill(new Rect(0, 0, w, h), new Color(1f, 0.96f, 0.9f, 0.1f * dawn));
                for (int i = 0; i < 8; i++) Fill(new Rect(0, h * i / 16f, w, h / 16f), new Color(1f, 0.95f, 0.88f, 0.05f * dawn * (1f - i / 8f)));
            }
            if (vossFlash > 0)
            {
                if (vossStills == null)
                {
                    vossStills = new Texture2D[4];
                    for (int i = 1; i <= 3; i++) vossStills[i] = Resources.Load<Texture2D>($"Story/Flash/voss_{i}");
                }
                float t0 = Time.unscaledTime - flashCutAt;
                Fill(new Rect(0, 0, w, h), Color.black);
                var tex = vossStills[vossFlash];
                if (tex != null && Mathf.Repeat(t0 * 6f, 1f) > 0.06f && t0 > 0.05f)
                {
                    float zoom = 1.06f + 0.04f * t0;
                    float jx = (Mathf.PerlinNoise(t0 * 9f, 0.2f) - 0.5f) * 14f, jy = (Mathf.PerlinNoise(0.6f, t0 * 9f) - 0.5f) * 10f;
                    float iw = w * zoom, ih = iw * 9f / 16f;
                    if (ih < h * zoom) { ih = h * zoom; iw = ih * 16f / 9f; }
                    GUI.DrawTexture(new Rect((w - iw) * 0.5f + jx, (h - ih) * 0.5f + jy, iw, ih), tex);
                }
            }
            if (voss != null && vossStage >= 1 && vossStage < 5 && voss.Alive)
            {
                float bw = Mathf.Min(560f, w - 80f), x = (w - bw) * 0.5f, y = 54f;
                Fill(new Rect(x - 2, y - 2, bw + 4, 14), new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(x, y, bw * voss.HealthFraction, 10), new Color(0.85f, 0.85f, 0.85f));
                foreach (float mk in new[] { 0.62f, 0.3f }) Fill(new Rect(x + bw * mk - 1, y - 3, 2, 16), new Color(0.8f, 0.06f, 0.07f));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
                string ph = vossStage >= 3 ? "III  ·  THE EDGE" : vossStage == 2 ? "II  ·  THE GUN" : "I  ·  THE TEACHER";
                GUI.Label(new Rect(x, y - 30, bw, 26), Spaced("VOSS") + "     " + ph, cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            if (aimU >= 0f && Time.unscaledTime < gunPromptUntil + 1f)
            {
                float a = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 12f);
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 0.25f, 0.25f, a);
                GUI.Label(new Rect(0, h * 0.24f, w, 30), Application.isMobilePlatform ? "SWIPE TO SLIP THE SHOT   ·   OR GET IN CLOSE" : "DODGE TO SLIP THE SHOT   ·   OR GET IN CLOSE", cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            if (false && theEndAt > 0f && phase != Phase.End)
            {
                float t0 = Time.unscaledTime - theEndAt;
                float a = Mathf.Clamp01(t0 / 1.6f);
                Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, a));
                float ta = Mathf.Clamp01((t0 - 1.4f) / 1.2f);
                int bs = big.fontSize;
                big.fontSize = 96;
                big.normal.textColor = new Color(1f, 1f, 1f, ta);
                GUI.Label(new Rect(0, h * 0.36f, w, 110), Spaced("THE END"), big);
                big.fontSize = bs;
                Fill(new Rect(w * 0.5f - 150 * ta, h * 0.36f + 112, 300 * ta, 3), new Color(0.8f, 0.06f, 0.07f, ta));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.75f * Mathf.Clamp01((t0 - 2.8f) / 1f));
                GUI.Label(new Rect(0, h * 0.36f + 128, w, 30), Spaced("HE CAME HOME"), cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
        }
    }
}
