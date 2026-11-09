using System.Collections;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>CHAPTER 3 · HOME, the story v2 beats (docs/story/story_v2.md): Harrow's card and Elena's
    /// note on the hall table, the photo of V behind the family photo (flash 2: "I'm out, Voss."),
    /// the walkie-talkie missing from Maya's shelf, and the kitchen chair: three questions for Silas,
    /// his father's call, the scalpel, and "...Silas? Silas!"</summary>
    public partial class StoryIntro
    {
        float hallCardAt = -1f, vPhotoAt = -1f, homeFlashAt = -1f, phoneAt = -1f, cutAt = -1f;
        Texture2D vPhotoTex, homeFlashTex;
        string phoneName = "V.";

        static Vector3 FindPos(string name, Vector3 fallback)
        {
            var g = GameObject.Find(name);
            return g != null ? g.transform.position : fallback;
        }

        // ---------------------------------------------------------------- the hall table

        IEnumerator HallCard()
        {
            var card = FindPos("HarrowCard", new Vector3(6.3f, 0.92f, 3.3f));
            System.Func<float, CineCam.Shot> shot = u => CineCam.DollyIn(card, new Vector3(0.15f, -0.6f, 1f), u, 2.2f, 1.2f, 36f);
            yield return Move(1.8f, shot);
            StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(shot(1f))));
            yield return TapThings(new[] { card }, new[] { "THE HALL TABLE" }, i => null);
            Sound.Play(Sfx.Cloth, 0.5f, card, 1.5f);
            hallCardAt = Time.unscaledTime;
            yield return Hold(1.4f, () => Cine(shot(1f)));
            Say("YOU", "\"Detective Harrow. Organised Crime.\"", 2.6f);
            yield return Hold(2.8f, () => Cine(shot(1f)));
            Say("YOU", "And a note... her hand.", 2.0f);
            yield return Hold(2.2f, () => Cine(shot(1f)));
            Say("HER", "\"He came again. Says you'd be safe if you talk. Call him?\"", 3.4f);
            yield return Hold(3.6f, () => Cine(shot(1f)));
            hallCardAt = -1f;
            Say("YOU", "...Someone came asking about me. That's why.", 2.8f);
            yield return Hold(3.0f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.6f, 0.05f, -1.1f), HeroHead(), 30f)));
        }

        // ---------------------------------------------------------------- the photo of V

        IEnumerator PhotoOfV()
        {
            // he turns the family photo over: another, older one tucked behind it
            yield return DragTo("TURN IT OVER", Vector2.left, p => { if (p > 0.5f && vPhotoAt < 0f) { vPhotoAt = Time.unscaledTime; photoAt = -1f; Sound.Play(Sfx.Cloth, 0.5f, null, 1.6f); } });
            if (vPhotoAt < 0f) { vPhotoAt = Time.unscaledTime; photoAt = -1f; }
            Sound.Play(Sfx.Piano, 0.5f, null, 0.8f);
            yield return RealWait(1.4f);
            Say("YOU", "Another photo, behind it.", 2.0f);
            yield return RealWait(2.2f);
            Say("YOU", "...Me. Twenty, maybe.", 2.2f);
            yield return RealWait(2.4f);
            Say("YOU", "And him. The coat.", 2.0f);
            yield return RealWait(2.2f);
            Say("YOU", "\"To my best student. — V.\"", 2.6f);
            Sound.Play(Sfx.Heartbeat, 0.8f);
            yield return RealWait(2.8f);
            // flash 2: his office, years ago; the gun on the desk
            yield return HomeFlash("Story/Flash/voss_1", 3.0f, "YOU", "I'm out, Voss.");
            Say("YOU", "...Voss.", 1.8f);
            yield return RealWait(2.0f);
            Say("YOU", "You raised me.", 2.0f);
            yield return RealWait(2.2f);
            Say("YOU", "And the boy behind you... who's he?", 2.6f);
            yield return RealWait(2.8f);
            vPhotoAt = -1f;
        }

        IEnumerator HomeFlash(string still, float secs, string who, string line)
        {
            homeFlashTex = Resources.Load<Texture2D>(still);
            Sound.Play(Sfx.FlashCut, 0.9f);
            Sound.Play(Sfx.Tinnitus, 0.4f);
            homeFlashAt = Time.unscaledTime;
            if (who != null) Say(who, line, secs);
            yield return RealWait(secs);
            homeFlashAt = -1f;
            Sound.Play(Sfx.FlashCut, 0.6f);
        }

        // ---------------------------------------------------------------- her shelf

        IEnumerator ShelfWalkie()
        {
            var w = FindPos("ShelfWalkie", hero.transform.position + new Vector3(1.4f, 1.1f, 2.6f));
            System.Func<float, CineCam.Shot> shot = u => CineCam.DollyIn(w, new Vector3(-0.3f, -0.2f, 1f), u, 2.0f, 1.0f, 34f);
            yield return Move(1.6f, shot);
            StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(shot(1f))));
            yield return TapThings(new[] { w }, new[] { "HER SHELF" }, i => null);
            Sound.Play(Sfx.Static, 0.25f, w, 1.8f);
            Say("YOU", "Walkie-talkies. A pair... and one's gone.", 2.8f);
            yield return Hold(3.0f, () => Cine(shot(1f)));
            Say("YOU", "She took one with her.", 2.2f);
            yield return Hold(2.4f, () => Cine(shot(1f)));
            var g = GameObject.Find("ShelfWalkie");
            if (g != null) g.SetActive(false);
            Sound.Play(Sfx.Cloth, 0.6f, hero.Chest, 1.3f);
            Say("YOU", "Then I'll keep the other.", 2.2f);
            yield return Hold(2.4f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.7f, -0.1f, -1.0f), HeroHead(), 30f)));
        }

        // ---------------------------------------------------------------- the kitchen chair

        IEnumerator ChairTalk(Transform ch, System.Func<Vector3> cHead, Vector3 two)
        {
            System.Action overShoulder = () => Cine(new CineCam.Shot(new Vector3(27.1f, 1.55f, 0.05f), cHead(), 34f));
            System.Action onSilas = () => Cine(new CineCam.Shot(new Vector3(30.1f, 0.95f, -1.3f), cHead(), 30f));
            System.Action onYou = () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.7f, -0.05f, -1.0f), HeroHead(), 30f));
            Say("YOU", "Look at me.", 1.6f);
            yield return Hold(1.8f, overShoulder);
            Say("THE CLEANER", "Go on, then. You taught me this too.", 2.6f, ch);
            yield return Hold(2.8f, onSilas);
            // three questions; one stays unasked
            var asked = new bool[4];
            string[] q = { "\"Where is my daughter?\"", "\"Why Elena?\"", "\"Who are you?\"", "\"Why are you still here?\"" };
            for (int round = 0; round < 3; round++)
            {
                var opts = new System.Collections.Generic.List<string>();
                var map = new System.Collections.Generic.List<int>();
                for (int i = 0; i < 4; i++) if (!asked[i]) { opts.Add(q[i]); map.Add(i); }
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, overShoulder));
                yield return ChooseFrom(opts.ToArray());
                int pick = map[chosen];
                asked[pick] = true;
                Say("YOU", q[pick].Trim('"'), 2.0f);
                yield return Hold(2.2f, onYou);
                switch (pick)
                {
                    case 0:
                        Say("THE CLEANER", "Alive. She's leverage. That's all you get.", 2.8f, ch);
                        yield return Hold(3.0f, onSilas);
                        break;
                    case 1:
                        Say("THE CLEANER", "He said bring her. ...I didn't feel like it.", 3.0f, ch);
                        yield return Hold(3.2f, onSilas);
                        Say("THE CLEANER", "Every day of my life it was you. \"He'd have done it cleaner.\"", 3.4f, ch);
                        yield return Hold(3.6f, onSilas);
                        Say("THE CLEANER", "So I did it cleaner.", 2.0f, ch);
                        Sound.Play(Sfx.Heartbeat, 0.8f);
                        yield return Hold(2.4f, onYou);
                        break;
                    case 2:
                        Say("THE CLEANER", "You don't even remember me.", 2.2f, ch);
                        yield return Hold(2.4f, onSilas);
                        Say("THE CLEANER", "...Of course you don't. Nobody ever remembers the son.", 3.0f, ch);
                        yield return Hold(3.2f, onSilas);
                        break;
                    case 3:
                        Say("THE CLEANER", "He sent me back to mop up my own mess. Like a dog.", 3.0f, ch);
                        yield return Hold(3.2f, onSilas);
                        Say("THE CLEANER", "Then the Driver called. Said you were coming home. Why not finish it myself?", 3.6f, ch);
                        yield return Hold(3.8f, onSilas);
                        break;
                }
            }
            // the phone on the table: V.
            Sound.Play(Sfx.PhoneBuzz, 1f, new Vector3(28.4f, 0.95f, 0.3f));
            phoneAt = Time.unscaledTime;
            phoneName = "V.";
            var phonePos = new Vector3(28.45f, 0.92f, 0.35f);
            System.Action onPhone = () => Cine(new CineCam.Shot(phonePos + new Vector3(-0.1f, 0.55f, -0.5f), phonePos, 30f));
            yield return Hold(1.4f, onPhone);
            Say("THE CLEANER", "...That'll be my father.", 2.2f, ch);
            yield return Hold(2.2f, onSilas);
            StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => { onPhone(); if (Random.value < 0.02f) Sound.Play(Sfx.PhoneBuzz, 0.6f, phonePos); }));
            yield return SwipeTo("ANSWER IT", 2, 0f);
            phoneAt = -1f;
            Sound.Play(Sfx.PhoneClick, 0.8f);
            Sound.MusicStop(1f);
            hero.body.Play("idle", 0.2f);
            System.Action phoneClose = () => Cine(new CineCam.Shot(HeroHead() + new Vector3(-0.55f, -0.02f, -0.75f), HeroHead(), 26f));
            Say("PHONE", "Silas? Is it done?", 2.2f);
            yield return Hold(2.6f, phoneClose);
            yield return Hold(1.2f, phoneClose);
            Say("PHONE", "...Ah. It's you.", 2.0f);
            yield return Hold(2.2f, phoneClose);
            Say("PHONE", "Hello, son.", 1.8f);
            Sound.Play(Sfx.Heartbeat, 0.7f);
            yield return Hold(2.2f, phoneClose);
            Say("PHONE", "A detective at your door... you know what that looks like to me.", 3.4f);
            yield return Hold(3.6f, phoneClose);
            Say("PHONE", "I'm sorry about Elena. That was never the plan.", 3.0f);
            yield return Hold(3.2f, phoneClose);
            Say("PHONE", "Maya's safe. Bring me the book, and you can take her home.", 3.4f);
            yield return Hold(3.2f, phoneClose);
            // behind him: the hand finds the scalpel taped under the seat
            if (cleaner != null) cleaner.PuppetPlay("sit_shock", 0.05f);
            Sound.Play(Sfx.Knife, 0.9f, cHead());
            Sound.Play(Sfx.SlowmoIn, 0.6f);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.25f, 1.4f, 0.02f, 0.25f);
            StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(new CineCam.Shot(new Vector3(29.4f, 1.2f, -1.6f), cHead() + Vector3.left * 0.5f, 34f))));
            yield return SwipeTo("BEHIND YOU", 1, 1.6f);
            // one move: the reflex he can't remember learning
            cutAt = Time.unscaledTime;
            if (!scOk) { hero.TakeHit(new HitInfo { damage = 6f, direction = -1, knockback = 0.5f }); Sound.Play(Sfx.HeroHurt, 0.8f); }
            Sound.Play(Sfx.PunchHeavy, 1f, cHead());
            Sound.Play(Sfx.Bone, 0.9f, cHead());
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.08f);
            hero.body.Play(hero.body.Has("gyaku_zuki") ? "gyaku_zuki" : "idle", 0.03f);
            if (cleaner != null) cleaner.PuppetPlay("sit_slump", 0.05f);
            Sound.Play(Sfx.DeathBreath, 0.8f, cHead());
            yield return Hold(1.2f, () => Cine(CineCam.Roll(new Vector3(28.4f, 1.3f, -3.4f), two, 1f, 0f, -9f, 36f)));
            // the phone on the floor, the line still open
            Sound.Play(Sfx.Crate, 0.4f, phonePos + Vector3.down * 0.9f, 1.6f);
            var floorPhone = new Vector3(28.6f, 0.02f, 0.1f);
            System.Action onFloor = () => Cine(new CineCam.Shot(floorPhone + new Vector3(-0.35f, 0.25f, -0.45f), floorPhone, 30f));
            phoneAt = Time.unscaledTime; phoneName = "CALL CONNECTED";
            yield return Hold(1.0f, onFloor);
            Say("PHONE", "...Silas?", 1.6f);
            yield return Hold(2.0f, onFloor);
            Say("PHONE", "Silas!", 1.4f);
            yield return Hold(2.6f, onFloor);
            yield return Hold(1.6f, onFloor);
            Say("PHONE", "...Bring me the book. Then we're even.", 3.0f);
            yield return Hold(3.2f, onFloor);
            Sound.Play(Sfx.PhoneClick, 0.7f);
            phoneAt = -1f;
            yield return Hold(1.0f, onFloor);
            Say("YOU", "...Silas.", 1.6f);
            yield return Hold(1.8f, onYou);
            Say("YOU", "Your son.", 1.8f);
            yield return Move(3.4f, u => CineCam.DollyOut(HeroHead(), new Vector3(0.5f, -0.1f, 1f), u, 1.4f, 4.6f, 34f));
        }

        // ---------------------------------------------------------------- on screen

        void HomeV2GUI(float w, float h)
        {
            if (markerFont == null) markerFont = Resources.Load<Font>("Fonts/PermanentMarker-Regular");
            if (hallCardAt > 0f)
            {
                float a = Mathf.Clamp01((Time.unscaledTime - hallCardAt) / 0.4f);
                Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.5f * a));
                // the card: crisp, printed
                float cw = Mathf.Min(w * 0.36f, 380f), chh = cw * 0.57f;
                var c = new Rect(w * 0.5f - cw - 20f, h * 0.5f - chh * 0.6f, cw, chh);
                Fill(new Rect(c.x - 3, c.y - 3, c.width + 6, c.height + 6), new Color(0f, 0f, 0f, 0.7f * a));
                Fill(c, new Color(0.93f, 0.93f, 0.91f, a));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.fontSize = Mathf.RoundToInt(chh * 0.1f);
                cap.normal.textColor = new Color(0.15f, 0.15f, 0.17f, a);
                GUI.Label(new Rect(c.x, c.y + chh * 0.1f, cw, chh * 0.16f), Spaced("CITY POLICE"), cap);
                cap.fontSize = Mathf.RoundToInt(chh * 0.15f);
                cap.normal.textColor = new Color(0.05f, 0.05f, 0.06f, a);
                GUI.Label(new Rect(c.x, c.y + chh * 0.34f, cw, chh * 0.2f), "DET. J. HARROW", cap);
                cap.fontSize = Mathf.RoundToInt(chh * 0.09f);
                cap.normal.textColor = new Color(0.3f, 0.3f, 0.32f, a);
                GUI.Label(new Rect(c.x, c.y + chh * 0.56f, cw, chh * 0.14f), Spaced("ORGANISED CRIME"), cap);
                GUI.Label(new Rect(c.x, c.y + chh * 0.74f, cw, chh * 0.14f), "555 0117", cap);
                cap.alignment = TextAnchor.MiddleLeft;
                cap.fontSize = 20;
                // the note: her hand
                float nw = cw * 0.95f, nh = nw * 0.7f;
                var n = new Rect(w * 0.5f + 20f, h * 0.5f - nh * 0.5f, nw, nh);
                var m0 = GUI.matrix;
                GUIUtility.RotateAroundPivot(3f, (Vector2)GUI.matrix.MultiplyPoint3x4(n.center));
                Fill(new Rect(n.x - 3, n.y - 3, n.width + 6, n.height + 6), new Color(0f, 0f, 0f, 0.6f * a));
                Fill(n, new Color(0.86f, 0.85f, 0.8f, a));
                var st = new GUIStyle(sub) { font = markerFont != null ? markerFont : sub.font, fontSize = Mathf.RoundToInt(nh * 0.1f), alignment = TextAnchor.MiddleLeft, wordWrap = true };
                st.normal.textColor = new Color(0.1f, 0.1f, 0.13f, a * Mathf.Clamp01((Time.unscaledTime - hallCardAt - 2.5f) / 0.8f));
                GUI.Label(new Rect(n.x + nw * 0.08f, n.y + nh * 0.06f, nw * 0.86f, nh * 0.88f), "He came again.\nSays you'd be safe if you talk.\nCall him?\n          — E.", st);
                GUI.matrix = m0;
            }
            if (vPhotoAt > 0f)
            {
                if (vPhotoTex == null) vPhotoTex = Resources.Load<Texture2D>("Story/Memories/vphoto");
                float t0 = Time.unscaledTime - vPhotoAt;
                float a = Mathf.Clamp01(t0 / 0.5f);
                Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.6f * a));
                float ph = Mathf.Min(h * 0.56f, 360f), pw = ph * 1.5f;
                var m0 = GUI.matrix;
                GUIUtility.RotateAroundPivot(2f, (Vector2)GUI.matrix.MultiplyPoint3x4(new Vector2(w * 0.5f, h * 0.46f)));
                var r = new Rect((w - pw) * 0.5f, h * 0.46f - ph * 0.5f, pw, ph);
                Fill(new Rect(r.x - 12, r.y - 12, r.width + 24, r.height + 56), new Color(0.8f, 0.78f, 0.72f, a));      // an older, yellowed border
                var o = GUI.color;
                GUI.color = new Color(0.95f, 0.92f, 0.86f, a);
                if (vPhotoTex != null) GUI.DrawTexture(r, vPhotoTex, ScaleMode.ScaleAndCrop);
                else Fill(r, new Color(0.3f, 0.3f, 0.3f, a));
                GUI.color = o;
                var st = new GUIStyle(sub) { font = markerFont != null ? markerFont : sub.font, fontSize = Mathf.RoundToInt(ph * 0.075f), alignment = TextAnchor.MiddleCenter };
                st.normal.textColor = new Color(0.12f, 0.12f, 0.14f, a * Mathf.Clamp01((t0 - 5.5f) / 0.8f));
                GUI.Label(new Rect(r.x, r.yMax + 6, pw, 40), "To my best student.  — V.", st);
                GUI.matrix = m0;
            }
            if (homeFlashAt > 0f && homeFlashTex != null)
            {
                float t0 = Time.unscaledTime - homeFlashAt;
                Fill(new Rect(0, 0, w, h), Color.black);
                if (Mathf.Repeat(t0 * 6f, 1f) > 0.07f && t0 > 0.05f)
                {
                    float zoom = 1.06f + 0.04f * t0;
                    float jx = (Mathf.PerlinNoise(t0 * 9f, 0.2f) - 0.5f) * 14f, jy = (Mathf.PerlinNoise(0.6f, t0 * 9f) - 0.5f) * 10f;
                    float iw = w * zoom, ih = iw * 9f / 16f;
                    if (ih < h * zoom) { ih = h * zoom; iw = ih * 16f / 9f; }
                    GUI.DrawTexture(new Rect((w - iw) * 0.5f + jx, (h - ih) * 0.5f + jy, iw, ih), homeFlashTex);
                }
            }
            if (flashAt > 0f)
            {
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
                GUI.Label(new Rect(0, h * 0.08f, w, 30), Spaced("WHAT HAPPENED HERE"), cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            if (phoneAt > 0f)
            {
                float t0 = Time.unscaledTime - phoneAt;
                float a = Mathf.Clamp01(t0 / 0.3f);
                float ph = Mathf.Min(h * 0.5f, 320f), pw = ph * 0.5f;
                var r = new Rect(w - pw - 50f, (h - ph) * 0.5f, pw, ph);
                Fill(new Rect(r.x - 6, r.y - 6, r.width + 12, r.height + 12), new Color(0.02f, 0.02f, 0.025f, a));
                Fill(r, new Color(0.08f, 0.09f, 0.1f, a));
                float pulse = 0.5f + 0.5f * Mathf.Sin(t0 * 6f);
                DrawRing(new Vector2(r.center.x, r.y + ph * 0.32f), pw * 0.22f + pulse * 6f, new Color(1f, 1f, 1f, 0.4f * a));
                big.fontSize = Mathf.RoundToInt(ph * 0.12f);
                big.normal.textColor = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(r.x, r.y + ph * 0.24f, pw, ph * 0.16f), phoneName, big);
                big.fontSize = 72;
                cap.alignment = TextAnchor.MiddleCenter;
                cap.fontSize = Mathf.RoundToInt(ph * 0.05f);
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.7f * a);
                GUI.Label(new Rect(r.x, r.y + ph * 0.5f, pw, ph * 0.08f), phoneName == "V." ? Spaced("INCOMING CALL") : Spaced("ON THE FLOOR"), cap);
                cap.alignment = TextAnchor.MiddleLeft;
                cap.fontSize = 20;
            }
            if (cutAt > 0f && Time.unscaledTime - cutAt < 0.12f) Fill(new Rect(0, 0, w, h), Color.white);
        }
    }
}
