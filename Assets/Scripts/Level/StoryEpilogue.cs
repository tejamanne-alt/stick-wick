using System.Collections;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>CHAPTER 7 · HOME, the epilogue (docs/story/story_v2.md): the morning after the storm, no
    /// fighting. The bottom of the hill road with the police lights and Detective Harrow ("Then I never
    /// saw you"); Elena's grave, where Maya leaves her walkie so Mama can hear them; the swing; the book
    /// locked in the drawer under Elena's photo; "Papa, I'm hungry!" "...Coming." THE END. Then THE WHOLE
    /// STORY (StoryRecap).</summary>
    public partial class StoryIntro
    {
        /// <summary>TEST: 0 the road, 1 the grave, 2 the swing, 3 the drawer, 4 the end and the recap, 5 the recap alone.</summary>
        public static int EpilogueFrom;

        Enemy harrowMan;
        Transform swingSeat, drawerFront, policeLights;
        Transform[] policeR;
        float epTitleAt = -1f, theEndCardAt = -1f, laterAt = -1f;

        void BeginEpilogue()
        {
            phase = Phase.Epilogue;
            HeadWound.Chapter = 3;
            LightningSwap.Held = true;
            Sound.Ambience(0.1f, 0.3f, 22000f);
            swingSeat = Find("SwingSeat");
            drawerFront = Find("HallDrawerFront");
            int from = EpilogueFrom;
            EpilogueFrom = 0;
            if (RecapOnly) { RecapOnly = false; from = 5; }
            StartCoroutine(Epilogue(from));
        }

        void UpdateEpilogue()
        {
            // the police lights, white only
            if (policeR == null)
            {
                policeR = new Transform[EpiloguePlan.PoliceLightCount];
                for (int i = 0; i < policeR.Length; i++) policeR[i] = Find("PoliceLight_" + i);
            }
            for (int i = 0; i < policeR.Length; i++)
                if (policeR[i] != null)
                {
                    bool on = Mathf.Repeat(Time.unscaledTime * 2.2f + (i % 2) * 0.5f + (i / 2) * 0.17f, 1f) < 0.5f;
                    foreach (var r in policeR[i].GetComponentsInChildren<Renderer>()) r.enabled = on;
                }
        }

        /// <summary>He walks from a to b, the companion a step behind him and on the camera's side (side: her
        /// place from him; by default 0.45 behind, whichever way he walks, and 0.35 nearer the camera).</summary>
        IEnumerator Walk(Enemy companion, Vector3 a, Vector3 b, float secs, System.Func<float, CineCam.Shot> shot, Vector3? side = null)
        {
            int dir = b.x >= a.x ? 1 : -1;
            var off = side ?? new Vector3(-0.45f * dir, 0f, -0.35f);
            hero.SetFacing(dir);
            if (companion != null) companion.SetFacing(dir);
            hero.body.Play("walk", 0.2f, true, 0.7f);
            if (companion != null) companion.PuppetPlay("walk", 0.1f, 1.2f);
            yield return Move(secs, shot, u =>
            {
                var p = Vector3.Lerp(a, b, u);
                hero.CinePose(p);
                if (companion != null) companion.PuppetPose(p + off);
                if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.55f; Sound.Play(Sfx.Footstep, 0.35f, p, 1.0f); }
            });
            hero.body.Play(hero.body.Has("hold_hand") ? "hold_hand" : "idle_loose", 0.3f);
            if (companion != null) companion.PuppetPlay(Pick(companion.body, "idle_loose", "idle"), 0.2f);     // a child standing, not a guard
        }

        /// <summary>His near hand and Maya's held together (PuppetRig reaches, as on the walk-out): the point
        /// between their shoulders, a little above hers, as far from his as his arm allows hanging down.
        /// Returns the two arms (to let go of with on = false).</summary>
        PuppetRig.Arm[] EpHoldHands(bool on, PuppetRig.Arm[] arms = null)
        {
            if (!on)
            {
                if (arms != null) foreach (var a in arms) if (a != null) a.Off(3f);
                return null;
            }
            if (maya == null || hero.body == null || maya.body == null) return null;
            var rigH = PuppetRig.On(hero.body);
            var rigM = PuppetRig.On(maya.body);
            var hArm = rigH.Near(maya.transform.position);
            var mArm = rigM.Near(hero.transform.position);
            if (hArm.upper == null || mArm.upper == null) return null;
            System.Func<Vector3> at = () =>
            {
                Vector3 sh = hArm.upper.position, sm = mArm.upper.position;
                var d = sm - sh; d.y = 0f;
                var p = sh + (d.sqrMagnitude > 1e-4f ? d.normalized : rigH.Forward) * Mathf.Min(0.3f, d.magnitude * 0.6f);
                p.y = sm.y + 0.03f;
                return p;
            };
            hArm.To(at, () => Vector3.down * 0.4f - rigH.Forward * 0.5f, 2.5f);
            mArm.To(at, () => Vector3.down + (hero.transform.position - maya.transform.position).normalized * -0.3f, 2.5f);
            return new[] { hArm, mArm };
        }

        /// <summary>Turn him (or her) round on the spot to face a point, over a moment (LookAtXZ snaps).</summary>
        IEnumerator EpTurnTo(Fighter f, Vector3 at, float secs, System.Action frame = null)
        {
            if (f == null) yield break;
            var d0 = f.transform.right; d0.y = 0f;
            var d1 = at - f.transform.position; d1.y = 0f;
            if (d0.sqrMagnitude < 1e-6f || d1.sqrMagnitude < 1e-6f) yield break;
            d0.Normalize(); d1.Normalize();
            for (float t0 = 0f; t0 < secs; t0 += StoryDt)
            {
                if (SkipNow) break;
                f.LookAtXZ(f.transform.position + Vector3.Slerp(d0, d1, Mathf.SmoothStep(0f, 1f, t0 / secs)));
                frame?.Invoke();
                yield return null;
            }
            f.LookAtXZ(at);
        }

        IEnumerator FadeOut(float s)
        {
            for (float t0 = 0f; t0 < s; t0 += StoryDt) { if (SkipNow) break; blackout = t0 / s; yield return null; }
            blackout = 1f;
        }

        IEnumerator Epilogue(int from)
        {
            hero.CinePuppet();
            hero.SetFacing(1);
            barsPx = 92f;
            if (from >= 5)
            {
                blackout = 1f;
                yield return RecapSequence();
                phase = Phase.End;
                yield break;
            }
            maya = SpawnMaya(EpiloguePlan.MayaRoadSpot, 1);
            // her photo in the frame over the drawer
            var ph = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(ph.GetComponent<Collider>());
            ph.name = "ElenaPhotoImage";
            ph.transform.position = EpiloguePlan.HallPhoto + Vector3.right * 0.012f;
            ph.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            ph.transform.localScale = new Vector3(0.24f, 0.17f, 1f);
            var pm = new Material(Shader.Find("Unlit/Texture"));
            pm.mainTexture = Resources.Load<Texture2D>("Story/Memories/wed_5");
            ph.GetComponent<Renderer>().sharedMaterial = pm;
            mayaHere = true;
            TieRibbon();
            Sound.Music("finale", 0.0f, false, 0.1f);
            Sound.MusicStop(0.1f);
            if (from <= 0)
            {
                // ------------------------------------------------ 7.0 black: the title
                blackout = 1f;
                epTitleAt = Time.unscaledTime;
                Sound.Play(Sfx.Wind, 0.3f);
                yield return RealWait(3.6f);
                // ------------------------------------------------ 7.1 the bottom of the hill road, morning: police lights, Harrow
                harrowMan = director != null ? director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background) : null;
                if (harrowMan != null)
                {
                    harrowMan.Puppet();
                    harrowMan.SetBaseColor(new Color(0.42f, 0.42f, 0.44f));
                    Dress(harrowMan.body, Look.Harrow);
                    harrowMan.PuppetPose(EpiloguePlan.HarrowSpot);
                    harrowMan.SetFacing(1);                                  // towards them: they come to him from the +x side
                    harrowMan.PuppetPlay(Pick(harrowMan.body, "idle_loose", "idle"), 0f);     // a detective by his car, not a boxer
                }
                // they walk up the road to him from +x and stop on his +x side, facing him (EpiloguePlan: "he
                // and Maya face him from the +x side"). They used to come from -x, walk straight through him
                // (both at z -0.45 to -0.8, him at x 0.2, z -0.6) and stop with their backs to him. The camera
                // is out in front of them on the road (its drop-side wall at z -6.4 is 0.73 high), pulling back
                // as they arrive: Harrow, and the nose of his car, come into the frame on the left.
                var a = EpiloguePlan.RoadSpot + new Vector3(4.6f, 0f, 0f);
                hero.CinePose(a);
                StartCoroutine(FadeIn(1.4f));
                yield return Walk(maya, a, EpiloguePlan.RoadSpot, 5.0f, u => CineCam.DollyOut(EpiloguePlan.RoadSpot + Vector3.up * 1.0f, new Vector3(0.35f, -0.1f, 1f), u, 3f, 7f, 40f),
                    EpiloguePlan.MayaRoadSpot - EpiloguePlan.RoadSpot);
                // hand in hand as they stand there ("the girl holding his hand"): his near hand down to hers and
                // hers up to his, half a metre apart (the clips alone left both hands hanging at their sides)
                var holding = EpHoldHands(true);
                var hh = harrowMan != null ? HeadOf(harrowMan) : null;
                System.Func<Vector3> hHead = () => hh != null ? hh.position : EpiloguePlan.HarrowSpot + Vector3.up * 1.2f;
                System.Action onHarrow = () => Cine(new CineCam.Shot(HeroHead() + new Vector3(-0.3f, 0.1f, -1.2f) + (hHead() - HeroHead()) * -0.25f, hHead(), 32f));
                System.Action onHim = () => Cine(new CineCam.Shot(hHead() + new Vector3(0.3f, 0.0f, -1.3f), HeroHead(), 32f));
                Say("DETECTIVE HARROW", "Kessel Hill's still burning. Half of Voss's men are in pieces.", 3.4f, hh);
                yield return Hold(3.6f, onHarrow);
                Say("DETECTIVE HARROW", "Was that you?", 1.8f, hh);
                yield return Hold(2.0f, onHarrow);
                yield return Hold(1.6f, onHim);
                Say("DETECTIVE HARROW", "Voss had a book, they say. Names. You wouldn't know anything about that?", 3.8f, hh);
                yield return Hold(4.0f, onHarrow);
                // close on the corner of the book inside his coat
                Sound.Play(Sfx.Heartbeat, 0.5f);
                yield return Hold(1.6f, () => Cine(new CineCam.Shot(hero.Chest + new Vector3(0.3f, 0.1f, -0.6f), hero.Chest, 26f)));
                Say("YOU", "Voss is at the bottom of the cliff. His son is in my kitchen.", 3.2f);
                yield return Hold(3.4f, onHim);
                Say("YOU", "That's all the names you need.", 2.2f);
                yield return Hold(2.6f, onHim);
                // a long look at the girl holding his hand
                yield return Hold(2.0f, () => Cine(new CineCam.Shot(HeadOf(maya).position + new Vector3(0.6f, 0.1f, -0.9f), HeadOf(maya).position, 30f)));
                Say("DETECTIVE HARROW", "...Then I never saw you.", 2.4f, hh);
                yield return Hold(2.6f, onHarrow);
                Say("DETECTIVE HARROW", "Take her home.", 1.8f, hh);
                yield return Hold(2.4f, onHarrow);
                yield return FadeOut(1.0f);
                EpHoldHands(false, holding);
            }
            if (from <= 1)
            {
                // ------------------------------------------------ the time cut: the grave is weeks on
                blackout = 1f;
                laterAt = Time.unscaledTime;
                yield return RealWait(2.8f);
                // ------------------------------------------------ 7.3 the grave, on the hill, at dawn
                var g0 = EpiloguePlan.KneelSpot + new Vector3(-7f, 0f, -0.9f);
                hero.CinePose(g0);
                if (maya != null) maya.PuppetPose(g0 + new Vector3(-0.45f, 0f, -0.35f));
                Sound.Play(Sfx.Wind, 0.4f);
                StartCoroutine(FadeIn(1.4f));
                yield return Walk(maya, g0, EpiloguePlan.KneelSpot, 7.0f, u => CineCam.CraneDown(EpiloguePlan.ElenaStone, u, 9f, 2.0f, 7f, 42f));
                // he turns to her stone (it is 0.7 m in front of him, across the depth: he walked in along x) and
                // kneels to it; his hand goes out to its face, not along the path
                var stone = EpiloguePlan.ElenaStone;
                yield return EpTurnTo(hero, stone, 0.6f, () => Cine(CineCam.CraneDown(EpiloguePlan.ElenaStone, 1f, 9f, 2.0f, 7f, 42f)));
                hero.body.Play(hero.body.Has("kneel_grave") ? "kneel_grave" : "kneel_hand", 0.4f);
                System.Action atStone = () => Cine(new CineCam.Shot(stone + new Vector3(-1.4f, 0.3f, -1.6f), stone + Vector3.down * 0.3f, 34f));
                yield return Hold(2.4f, atStone);
                // she puts her walkie on the stone: standing at its corner, facing it (she was kneeling side on to
                // it in a hurt man's pose, a hand on the grass, a metre from the top she was putting it on)
                if (maya != null)
                {
                    maya.PuppetPose(stone + new Vector3(-0.34f, -stone.y + EpiloguePlan.KneelSpot.y, -0.38f));
                    maya.LookAtXZ(stone);
                    maya.PuppetPlay(Pick(maya.body, "idle_loose", "idle"), 0.3f);
                }
                var w = MakeWalkie("StoneWalkie");
                w.position = stone + Vector3.up * 0.02f;
                Sound.Play(Sfx.Cloth, 0.4f, stone, 1.6f);
                Say("MAYA", "So Mama can hear us.", 2.2f, maya != null ? HeadOf(maya) : null);
                yield return Hold(2.6f, () => Cine(new CineCam.Shot(stone + new Vector3(-0.5f, 0.15f, -0.6f), stone, 28f)));
                // his hand on the stone
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, atStone));
                yield return HoldTo("YOUR HAND ON THE STONE", 2.6f, null, stone + Vector3.down * 0.2f);
                Sound.MusicStop(0.5f);
                yield return Hold(1.2f, atStone);
                Say("HER", "Promise me you'll always come home.", 3.0f);
                yield return Hold(3.2f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.7f, -0.05f, -1.0f), HeroHead(), 28f)));
                Say("YOU", "I came home.", 2.2f);
                yield return Hold(2.6f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.6f, -0.05f, -0.9f), HeroHead(), 26f)));
                Say("YOU", "...Always.", 2.0f);
                yield return Move(6.0f, u => CineCam.DronePullAway(EpiloguePlan.KneelSpot + Vector3.up * 0.8f, u, 2.0f, 16f, 0.6f, 9f, 40f));
                yield return FadeOut(1.0f);
            }
            if (from <= 2)
            {
                // ------------------------------------------------ 7.4 the swing, morning; the music box theme, whole for the first time
                hero.CinePose(EpiloguePlan.PushSpot);
                hero.SetFacing(1);
                hero.body.Play(hero.body.Has("push_swing") ? "push_swing" : "idle_loose", 0.2f);
                if (maya != null) { maya.SetFacing(1); maya.PuppetPose(EpiloguePlan.SwingSeat + Vector3.down * 0.28f); maya.PuppetPlay(Pick(maya.body, "sit_drive", "sit_bound", "idle"), 0f); }
                Sound.Music("finale", 0.7f, false, 1.0f);
                StartCoroutine(FadeIn(1.2f));
                var seat = EpiloguePlan.SwingSeat;
                var pivot = EpiloguePlan.SwingPivot;
                float len = pivot.y - seat.y;
                // her hands on the two chains (0.22 either side of the seat's middle, along z), a little over her
                // shoulders: the clip alone has them out on a steering wheel
                var mRig = maya != null ? PuppetRig.On(maya.body) : null;
                System.Func<float, float, Vector3> onChain = (sw, dz) =>
                    pivot + Quaternion.AngleAxis(sw * Mathf.Rad2Deg, EpiloguePlan.SwingAxis) * new Vector3(0f, -(len - 0.36f), dz);
                // the seat comes back no further than his hands (he stands 0.95 m behind it and reaches about
                // half a metre: about -0.2 rad), and never higher than 1.15 rad: unchecked, the first push sent
                // her back 0.6 rad, through him, and the later ones could loop her over the top bar
                const float caught = -0.2f, highest = 1.15f;
                float swing = caught, vel = 0f, queued = 0f;
                bool held = true;                                  // in his hands, until the first push
                System.Action swingFrame = () =>
                {
                    if (!held)
                    {
                        vel += -swing * 3.2f * StoryDt;
                        vel *= 1f - 0.12f * StoryDt;
                        swing += vel * StoryDt;
                        if (swing > highest) { swing = highest; vel = Mathf.Min(0f, vel); }
                        if (swing <= caught && vel <= 0f)
                        {
                            // back into his hands: he holds her there, or gives her the push he has ready
                            swing = caught;
                            vel = queued;
                            held = queued <= 0f;
                            if (!held) Sound.Play(Sfx.SwingCreak, 0.5f, seat);
                            queued = 0f;
                        }
                    }
                    var turn = Quaternion.AngleAxis(swing * Mathf.Rad2Deg, EpiloguePlan.SwingAxis);
                    if (swingSeat != null) swingSeat.localRotation = Quaternion.Euler(0f, 0f, swing * Mathf.Rad2Deg);
                    if (maya != null)
                    {
                        // she swings with the seat, about the top bar, tilting with it (not upright on a leaning seat)
                        var rest = seat + Vector3.down * 0.28f;
                        maya.PuppetPose(pivot + turn * (rest - pivot));
                        maya.transform.rotation = turn;
                    }
                    Cine(new CineCam.Shot(seat + new Vector3(-0.8f, 0.4f, -3.4f), seat + new Vector3(0.6f, -0.3f, 0f), 40f));
                };
                if (mRig != null)
                {
                    var nearR = mRig.Near(seat + Vector3.back * 0.22f);
                    nearR.To(() => onChain(swing, -0.22f), () => Vector3.down + Vector3.back * 0.6f, 50f);
                    mRig.Other(nearR).To(() => onChain(swing, 0.22f), () => Vector3.down + Vector3.forward * 0.6f, 50f);
                }
                yield return Hold(2.0f, swingFrame);
                // he pushes: tap as she comes back to him
                for (int i = 0; i < 5; i++)
                {
                    StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, swingFrame));
                    yield return SwipeTo(i == 0 ? "PUSH" : "AGAIN", 0, 2.2f);
                    // a push only lands with her in his hands: away, it waits for her to come back. Each a little
                    // harder ("Higher!"): from his hands they reach about 0.65 rad, then 0.75, 0.85, 0.95, 1.05
                    float push = 1.1f + 0.18f * i;
                    if (held) { held = false; vel = push; Sound.Play(Sfx.SwingCreak, 0.5f, seat); }
                    else queued = push;
                    if (i == 1) Say("MAYA", "Higher, Papa!", 1.8f, maya != null ? HeadOf(maya) : null);
                    if (i == 3) Say("MAYA", "HIGHER!", 1.4f, maya != null ? HeadOf(maya) : null);
                    yield return Hold(1.2f, swingFrame);
                }
                yield return Hold(3.0f, swingFrame);
                yield return FadeOut(1.0f);
            }
            if (from <= 3)
            {
                // ------------------------------------------------ 7.5 the hall: the book in the drawer under Elena's photo
                // at the drawer, a little off the plan's spot: there his back (0.16 deep) was 3.5 cm into the side
                // of the stairs (their string is at x 1001.075), and turned to the door his arm would be; he stands
                // at home, not in a guard
                hero.CinePose(EpiloguePlan.HallSpot + Vector3.left * 0.12f);
                hero.SetFacing(-1);
                hero.body.Play(hero.body.Has("idle_loose") ? "idle_loose" : "idle", 0f);
                if (maya != null) maya.gameObject.SetActive(false);
                StartCoroutine(FadeIn(1.0f));
                var drawer = EpiloguePlan.HallDrawer;
                System.Action atDrawer = () => Cine(CineCam.DollyIn(drawer + Vector3.up * 0.4f, new Vector3(-1f, -0.35f, 0.25f), 1f, 2.2f, 1.2f, 36f));
                yield return Move(2.6f, u => CineCam.DollyIn(drawer + Vector3.up * 0.4f, new Vector3(-1f, -0.35f, 0.25f), u, 2.2f, 1.2f, 36f));
                var book = MakeBook();
                book.position = drawer + new Vector3(0.3f, 0.12f, 0f);
                Sound.Play(Sfx.Cloth, 0.6f, drawer);
                yield return Hold(1.6f, atDrawer);
                Say("YOU", "Forty-one names.", 2.0f);
                yield return Hold(2.2f, atDrawer);
                Say("YOU", "Somebody should remember them. It'll be me.", 2.8f);
                yield return Hold(3.0f, atDrawer);
                // the drawer stands open; he lays the book in it and slides it shut (towards the wall, -x)
                var drawerOpen = drawerFront != null ? drawerFront.position + Vector3.right * EpiloguePlan.HallDrawerTravel : Vector3.zero;
                if (drawerFront != null) drawerFront.position = drawerOpen;
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, atDrawer));
                yield return DragTo("CLOSE THE DRAWER", Vector2.up, p =>
                {
                    if (book != null) book.gameObject.SetActive(p < 0.6f);
                    if (drawerFront != null) drawerFront.position = drawerOpen + Vector3.left * EpiloguePlan.HallDrawerTravel * p;
                }, drawer);
                Sound.Play(Sfx.Latch, 0.6f, drawer);
                yield return Hold(2.2f, () => Cine(new CineCam.Shot(EpiloguePlan.HallPhoto + new Vector3(1.1f, -0.1f, -0.25f), EpiloguePlan.HallPhoto, 30f)));    // up to her photo
                Say("MAYA", "Papa, I'm hungry!", 1.8f);
                yield return Hold(2.0f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.7f, -0.05f, -1.0f), HeroHead(), 30f)));
                Say("YOU", "...Coming.", 1.8f);
                yield return Hold(1.4f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.7f, -0.05f, -1.0f), HeroHead(), 30f)));
                var h0 = hero.transform.position;
                // down the hall to the front door: between the coats on the left wall (x 1000.16) and the stairs'
                // newel and string on the right (x 1001.06-1001.075), so his shoulder stays off the banister (the
                // door's middle, x 1000.9, put it 7 cm into it); turned to the door first, not walking sideways
                var toDoor = new Vector3(EpiloguePlan.HallX0 + 0.72f, EpiloguePlan.HallFloorY, EpiloguePlan.FrontDoor.z - 0.7f);
                System.Action hallShot = () => Cine(new CineCam.Shot(EpiloguePlan.HallSpot + new Vector3(0.8f, 1.5f, -2.6f), HeroHead(), 42f));
                yield return EpTurnTo(hero, toDoor, 0.45f, hallShot);
                hero.body.Play("walk", 0.2f, true, 0.8f);
                yield return Move(3.0f, u => new CineCam.Shot(EpiloguePlan.HallSpot + new Vector3(0.8f, 1.5f, -2.6f), HeroHead(), 42f), u => hero.CinePose(Vector3.Lerp(h0, toDoor, u)));
                Sound.Play(Sfx.Door, 0.7f, EpiloguePlan.FrontDoor, 0.7f);
                yield return FadeOut(1.6f);
            }
            // ------------------------------------------------ 7.6 THE END, then the whole story
            theEndCardAt = Time.unscaledTime;
            yield return RealWait(7f);
            theEndCardAt = -1f;
            yield return RecapSequence();
            CineOff();
            phase = Phase.End;
        }

        void EpilogueGUI(float w, float h)
        {
            if (epTitleAt > 0f)
            {
                float t0 = Time.unscaledTime - epTitleAt;
                float a = Mathf.Clamp01(t0 / 0.8f) * Mathf.Clamp01((3.6f - t0) / 0.8f);
                if (a > 0f)
                {
                    cap.alignment = TextAnchor.MiddleCenter;
                    cap.fontSize = 22;
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f * a);
                    GUI.Label(new Rect(0, h * 0.38f, w, 30), Spaced("CHAPTER 7"), cap);
                    int bs = big.fontSize;
                    big.fontSize = 84;
                    big.normal.textColor = new Color(1f, 1f, 1f, a);
                    GUI.Label(new Rect(0, h * 0.38f + 30, w, 96), Spaced("HOME"), big);
                    big.fontSize = bs;
                    Fill(new Rect(w * 0.5f - 110 * a, h * 0.38f + 128, 220 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
                    cap.alignment = TextAnchor.MiddleLeft;
                    cap.fontSize = 20;
                }
            }
            if (laterAt > 0f)
            {
                float t1 = Time.unscaledTime - laterAt;
                float la = Mathf.Clamp01(t1 / 0.6f) * Mathf.Clamp01((2.8f - t1) / 0.6f);
                if (la > 0f)
                {
                    cap.alignment = TextAnchor.MiddleCenter;
                    cap.fontSize = 26;
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.85f * la);
                    GUI.Label(new Rect(0, h * 0.46f, w, 34), Spaced("SIX WEEKS LATER"), cap);
                    cap.alignment = TextAnchor.MiddleLeft;
                    cap.fontSize = 20;
                }
            }
            if (theEndCardAt > 0f)
            {
                float t0 = Time.unscaledTime - theEndCardAt;
                Fill(new Rect(0, 0, w, h), Color.black);
                float ta = Mathf.Clamp01((t0 - 0.6f) / 1.2f) * Mathf.Clamp01((7f - t0) / 0.8f);
                int bs = big.fontSize;
                big.fontSize = 96;
                big.normal.textColor = new Color(1f, 1f, 1f, ta);
                GUI.Label(new Rect(0, h * 0.36f, w, 110), Spaced("THE END"), big);
                big.fontSize = bs;
                Fill(new Rect(w * 0.5f - 150 * ta, h * 0.36f + 112, 300 * ta, 3), new Color(0.8f, 0.06f, 0.07f, ta));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.75f * Mathf.Clamp01((t0 - 2.2f) / 1f) * Mathf.Clamp01((7f - t0) / 0.8f));
                GUI.Label(new Rect(0, h * 0.36f + 128, w, 30), Spaced("HE CAME HOME"), cap);
                GUI.Label(new Rect(0, h * 0.36f + 170, w, 30), Spaced("NEXT  ·  THE WHOLE STORY"), cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            PromptsGUI(w, h);
        }
    }
}
