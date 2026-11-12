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

        IEnumerator Walk(Enemy companion, Vector3 a, Vector3 b, float secs, System.Func<float, CineCam.Shot> shot)
        {
            hero.body.Play("walk", 0.2f, true, 0.7f);
            if (companion != null) companion.PuppetPlay("walk", 0.1f, 1.2f);
            yield return Move(secs, shot, u =>
            {
                var p = Vector3.Lerp(a, b, u);
                hero.CinePose(p);
                if (companion != null) companion.PuppetPose(p + new Vector3(-0.45f, 0f, -0.35f));
                if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.55f; Sound.Play(Sfx.Footstep, 0.35f, p, 1.0f); }
            });
            hero.body.Play(hero.body.Has("hold_hand") ? "hold_hand" : "idle", 0.3f);
            if (companion != null) companion.PuppetPlay("idle", 0.2f);
        }

        IEnumerator FadeOut(float s)
        {
            for (float t0 = 0f; t0 < s; t0 += Time.unscaledDeltaTime) { blackout = t0 / s; yield return null; }
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
                    harrowMan.SetFacing(-1);
                    harrowMan.PuppetPlay("idle", 0f);
                }
                var a = EpiloguePlan.RoadSpot + new Vector3(-6f, 0f, 0f);
                hero.CinePose(a);
                StartCoroutine(FadeIn(1.4f));
                yield return Walk(maya, a, EpiloguePlan.RoadSpot, 5.0f, u => CineCam.DollyOut(EpiloguePlan.RoadSpot + Vector3.up * 1.0f, new Vector3(-1f, -0.1f, 0.3f), u, 3f, 7f, 40f));
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
                hero.body.Play(hero.body.Has("kneel_grave") ? "kneel_grave" : "kneel_hand", 0.4f);
                var stone = EpiloguePlan.ElenaStone;
                System.Action atStone = () => Cine(new CineCam.Shot(stone + new Vector3(-1.4f, 0.3f, -1.6f), stone + Vector3.down * 0.3f, 34f));
                yield return Hold(2.4f, atStone);
                // she puts her walkie on the stone
                if (maya != null) { maya.PuppetPose(stone + new Vector3(-0.35f, -stone.y + EpiloguePlan.KneelSpot.y, -0.5f)); maya.PuppetPlay(Pick(maya.body, "kneel_hurt", "idle"), 0.3f); }
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
                hero.body.Play(hero.body.Has("push_swing") ? "push_swing" : "idle", 0.2f);
                if (maya != null) { maya.PuppetPose(EpiloguePlan.SwingSeat + Vector3.down * 0.28f); maya.PuppetPlay(Pick(maya.body, "sit_drive", "sit_bound", "idle"), 0f); }
                Sound.Music("finale", 0.7f, false, 1.0f);
                StartCoroutine(FadeIn(1.2f));
                var seat = EpiloguePlan.SwingSeat;
                float swing = 0f, vel = 0f;
                System.Action swingFrame = () =>
                {
                    vel += -swing * 3.2f * Time.unscaledDeltaTime;
                    vel *= 1f - 0.12f * Time.unscaledDeltaTime;
                    swing += vel * Time.unscaledDeltaTime;
                    float len = EpiloguePlan.SwingPivot.y - seat.y;
                    var off = new Vector3(Mathf.Sin(swing) * len, (1f - Mathf.Cos(swing)) * len, 0f);
                    if (swingSeat != null) swingSeat.localRotation = Quaternion.Euler(0f, 0f, swing * Mathf.Rad2Deg);
                    if (maya != null) maya.PuppetPose(seat + Vector3.down * 0.28f + off);
                    Cine(new CineCam.Shot(seat + new Vector3(-0.8f, 0.4f, -3.4f), seat + new Vector3(0.6f, -0.3f, 0f), 40f));
                };
                yield return Hold(2.0f, swingFrame);
                // he pushes: tap as she comes back to him
                for (int i = 0; i < 5; i++)
                {
                    StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, swingFrame));
                    yield return SwipeTo(i == 0 ? "PUSH" : "AGAIN", 0, 2.2f);
                    vel += 1.1f;
                    Sound.Play(Sfx.SwingCreak, 0.5f, seat);
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
                hero.CinePose(EpiloguePlan.HallSpot);
                hero.SetFacing(-1);
                hero.body.Play("idle", 0f);
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
                hero.body.Play("walk", 0.2f, true, 0.8f);
                var h0 = hero.transform.position;
                var toDoor = new Vector3(EpiloguePlan.FrontDoor.x, EpiloguePlan.HallFloorY, EpiloguePlan.FrontDoor.z - 0.7f);
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
