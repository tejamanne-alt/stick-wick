using System.Collections;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>CHAPTER 4 · THE BACKYARD, the story v2 beats (docs/story/story_v2.md): FIND HER (Maya on
    /// the walkie, what happened last night in her words, the van far down the park and her face in
    /// its window), REACH HER (her voice between the fights, the van always in sight), LOSE HER (the
    /// chase, her hand on the glass, the static), then the Handler and the hill.</summary>
    public partial class StoryIntro
    {
        Transform van, vanWindow, vanLights, heldWalkie;
        Enemy vanMaya;
        int storyStill = -1;
        float storyStillAt;
        Texture2D[] storyStills;

        // ---------------------------------------------------------------- the cast

        /// <summary>Maya, as a small white figure: the rig at a child's size, her pigtails, her bow.</summary>
        Enemy SpawnMaya(Vector3 at, int facing)
        {
            if (director == null) return null;
            var m = director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
            if (m == null) return null;
            m.Puppet();
            m.SetBaseColor(new Color(0.9f, 0.9f, 0.89f));
            m.transform.localScale = Vector3.one * 0.58f;
            m.PuppetPose(at);
            m.SetFacing(facing);
            m.PuppetPlay(m.body.Has("idle_loose") ? "idle_loose" : "idle", 0f);     // a child standing, not a boxer's guard
            Dress(m.body, Look.Maya);
            return m;
        }

        void ParkCast()
        {
            van = Find("Van");
            if (van == null) van = Find("VanBody");
            // a pale window at the back of the van (a torch inside), and her small shape against it
            var vw = VanWindow();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "VanWindowGlow";
            q.transform.position = vw + Vector3.left * 0.03f;
            q.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            q.transform.localScale = new Vector3(0.7f, 0.42f, 1f);
            q.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.82f, 0.82f, 0.8f) };
            if (van != null) q.transform.SetParent(van, true);
            vanWindow = q.transform;
            vanWindow.gameObject.SetActive(false);
            vanMaya = SpawnMaya(vw + new Vector3(0.12f, -0.62f, 0f), -1);
            if (vanMaya != null)
            {
                if (van != null) vanMaya.transform.SetParent(van, true);
                // the set is baked opaque (the door's glass too), so she can't be seen through it: her shape is
                // drawn on the lit window instead, dark against it (her head, her pigtails, her shoulders; her hand
                // on the glass), just in front of the glow. It is hers: it shows and goes with her.
                var ink = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.05f, 0.05f, 0.06f) };
                var shape = new GameObject("VanMayaShape").transform;
                shape.position = vw + Vector3.left * 0.04f;
                void Disc(Vector2 yz, Vector2 size)
                {
                    var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(d.GetComponent<Collider>());
                    d.transform.SetParent(shape, false);
                    d.transform.localPosition = new Vector3(0f, yz.x, yz.y);
                    d.transform.localScale = new Vector3(0.006f, size.x, size.y);
                    d.GetComponent<Renderer>().sharedMaterial = ink;
                }
                Disc(new Vector2(0.01f, -0.04f), new Vector2(0.2f, 0.19f));                // her head
                Disc(new Vector2(0.07f, -0.17f), new Vector2(0.1f, 0.08f));                // the pigtails
                Disc(new Vector2(0.07f, 0.09f), new Vector2(0.1f, 0.08f));
                Disc(new Vector2(-0.16f, -0.04f), new Vector2(0.1f, 0.36f));               // the shoulders (inside the glow: it ends 0.21 down)
                Disc(new Vector2(0.04f, 0.2f), new Vector2(0.07f, 0.06f));                 // her hand on the glass
                shape.SetParent(vanMaya.transform, true);
                vanMaya.gameObject.SetActive(false);
            }
            // the van's lights: the two tail lamps at its back corners (z +-0.94 from its middle, 1 m up),
            // not either side of the window, which is in the left door (one of them hung in the air off the van)
            var lights = new GameObject("VanLights").transform;
            float vz = van != null ? van.position.z : ParkPlan.VanZ;
            for (int i = -1; i <= 1; i += 2)
            {
                var l = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(l.GetComponent<Collider>());
                l.transform.SetParent(lights, false);
                l.transform.position = new Vector3(vw.x + 0.03f, 1.02f, vz + i * 0.94f);
                l.transform.localScale = Vector3.one * 0.12f;
                l.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.62f, 0.05f, 0.06f) };
            }
            if (van != null) lights.SetParent(van, true);
            vanLights = lights;
            vanLights.gameObject.SetActive(false);
        }

        /// <summary>The van's back window (its back faces the park, towards the swings).</summary>
        Vector3 VanWindow()
        {
            var g = GameObject.Find("VanBackWindow");
            if (g != null) return g.transform.position;
            float x = van != null ? van.position.x : ParkPlan.VanX;
            float z = van != null ? van.position.z : ParkPlan.VanZ;
            return new Vector3(x - 2.45f, 1.35f, z);
        }

        void HoldWalkie(bool on)
        {
            if (on && heldWalkie == null && hero.body != null)
            {
                var hand = hero.body.Bone("hand.R");
                if (hand == null) return;
                heldWalkie = MakeWalkie("HisWalkie");
                heldWalkie.SetParent(hand, false);
                heldWalkie.localPosition = new Vector3(0f, 0.02f, 0f);
                heldWalkie.localScale = Vector3.one * 1.1f;
            }
            if (heldWalkie != null) heldWalkie.gameObject.SetActive(on);
        }

        // ---------------------------------------------------------------- her story

        static readonly (string still, string line, float secs)[] MayaTells =
        {
            ("Story/Flash/elena_1", "The bad men came. Lots of them, in the hall.", 3.0f),
            ("Story/Flash/elena_2", "Mama gave me the walkie and said run to the slide and hide, like we practised.", 4.4f),
            ("", "There was a big bang. I didn't look. I didn't look, Papa.", 3.8f),
            ("Story/Flash/taken_1", "I was so quiet under the slide... but my walkie went kssh.", 3.6f),
            ("Story/Flash/taken_2", "And they found me with a torch.", 2.6f),
            ("Story/Flash/taken_3", "I dropped my ribbon so you'd find the way. Mama said leave a trail if you're lost.", 4.4f),
        };

        IEnumerator MayaStory()
        {
            if (storyStills == null)
            {
                storyStills = new Texture2D[MayaTells.Length];
                for (int i = 0; i < MayaTells.Length; i++) storyStills[i] = MayaTells[i].still.Length > 0 ? Resources.Load<Texture2D>(MayaTells[i].still) : null;
            }
            Sound.MusicStop(1f);
            for (int i = 0; i < MayaTells.Length; i++)
            {
                storyStill = i;
                storyStillAt = Time.unscaledTime;
                Walkie(MayaTells[i].line, MayaTells[i].secs);
                if (i == 2) { Sound.PlayLater(Sfx.GunshotIndoor, 0.5f, 0.45f); Sound.MusicStop(0.2f); }
                if (i == 4) Sound.Play(Sfx.Struggle, 0.4f);
                yield return RealWait(MayaTells[i].secs + 0.3f);
            }
            storyStill = -1;
            Sound.Play(Sfx.Breath, 0.7f, hero.Chest, 0.9f);
        }

        // ---------------------------------------------------------------- between the fights

        IEnumerator BetweenStretches(int z)
        {
            yield return RealWait(1.4f);
            if (z == 0)
            {
                Walkie("There's lots of shouting. Is that you?", 2.6f);
                yield return RealWait(2.8f);
                Say("YOU", "Close your eyes and count to three, like the hopscotch. When you open them, I'll be closer.", 4.2f);
                yield return RealWait(4.4f);
                Walkie("One... two... three.", 2.4f);
            }
            else if (z == 1)
            {
                Walkie("Papa... did you find Mama?", 2.6f);
                yield return RealWait(2.8f);
                // he can't answer her: the player decides how. He stops walking to answer (the question never
                // sits over the next fight, and nobody comes at him while he's choosing).
                while (Enemy.All.Exists(e => e != null && e.Alive && e.Targetable)) yield return null;   // never mid-fight
                hero.CineLock();
                yield return ChooseFrom("\"Not yet, sweetheart.\"", "(say nothing)");
                hero.CineRelease();
                if (chosen == 0)
                {
                    Say("YOU", "Not yet, sweetheart.", 2.0f);
                    yield return RealWait(2.2f);
                    Walkie("Okay. She'll be worried.", 2.2f);
                }
                else
                {
                    yield return RealWait(1.6f);
                    Walkie("...Papa?", 1.6f);
                    yield return RealWait(1.8f);
                    Say("YOU", "Later. I'll tell you later.", 2.2f);
                }
            }
            else if (z == 2)
            {
                Sound.Play(Sfx.CarRev, 0.8f, new Vector3(ParkPlan.VanX, 1f, ParkPlan.VanZ));
                Walkie("They're starting the van! Papa, hurry!", 2.6f);
            }
        }

        // ---------------------------------------------------------------- the van gets away

        IEnumerator LoseHer()
        {
            hero.CinePuppet();
            barsPx = 92f;
            Sound.Tension(false);
            if (vanMaya != null) vanMaya.gameObject.SetActive(true);
            if (vanWindow != null) vanWindow.gameObject.SetActive(true);
            Sound.Play(Sfx.CarRev, 1f, van != null ? van.position : new Vector3(ParkPlan.VanX, 1f, ParkPlan.VanZ));
            var v0 = van != null ? van.position : new Vector3(ParkPlan.VanX, 0f, ParkPlan.VanZ);
            var r0 = van != null ? van.rotation : Quaternion.identity;
            float vx = 0f;
            // out under the barrier and away up the hill road, nose first: the road curves to +z and climbs
            // (ParkPlan.HillRoad). It used to slide sideways off at a slant, still facing +x, and sink into the
            // rising ground beside the road.
            System.Action drive = () =>
            {
                if (van == null) return;
                vx += StoryDt * Mathf.Lerp(1.2f, 6f, Mathf.Clamp01(vx / 6f));
                float x = v0.x + vx;
                var p = ParkPlan.HillRoad(x);
                var dir = ParkPlan.HillRoad(x + 1.5f) - ParkPlan.HillRoad(x - 1.5f);
                van.position = new Vector3(p.x, v0.y + p.y, p.z + (v0.z - ParkPlan.SightZ));
                van.rotation = Quaternion.FromToRotation(Vector3.right, dir.normalized) * r0;
            };
            // slow motion: the van pulling away, her small hand on the glass
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 2.2f, 0.05f, 0.3f);
            Walkie("PAPA!", 1.8f);
            Sound.Play(Sfx.SlowmoIn, 0.6f);
            var vw = VanWindow();
            yield return HoldFor(1.6f, () => { drive(); Cine(new CineCam.Shot(VanWindow() + new Vector3(-2.2f, 0.1f, -0.9f), VanWindow(), 30f)); });
            // he runs after it: the player drives his legs
            hero.body.Play("run", 0.1f, true, 1.5f);
            var h0 = hero.transform.position;
            float ran = 0f;
            StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () =>
            {
                drive();
                // never running on the spot between taps: a jog at least, faster as the player drives him, the
                // stride matched to the ground he covers (the drum on the lane is at x 112: he stops short of it)
                float was = ran;
                ran = Mathf.Min(10f, Mathf.Max(Mathf.MoveTowards(ran, scProgress * 9f, StoryDt * 6f), ran + StoryDt * 1.4f));
                if (StoryDt > 0f) hero.body.SetSpeed(Mathf.Clamp((ran - was) / StoryDt / 4.2f, 0.4f, 1.5f));
                hero.CinePose(h0 + Vector3.right * ran);
                if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.24f; Sound.Play(Sfx.Footstep, 0.7f, hero.transform.position, 1.2f); }
                Cine(new CineCam.Shot(hero.transform.position + new Vector3(-2.6f, 0.9f, -1.6f), Vector3.Lerp(HeroHead(), VanWindow(), 0.6f), 38f));
            }));
            yield return MashTo("RUN", 16);
            // he almost touches it... and it's gone up the hill road. He stands there, out of breath (no guard: nobody to fight)
            hero.body.Play(hero.body.Has("idle_alone") ? "idle_alone" : hero.body.Has("idle_loose") ? "idle_loose" : "idle", 0.3f);
            Sound.Play(Sfx.Breath, 1f, hero.Chest, 0.85f);
            Walkie("Papa... I can't... hear y—", 2.4f);
            Sound.Play(Sfx.Static, 0.8f, null, 0.9f);
            yield return HoldFor(2.6f, () => { drive(); Cine(new CineCam.Shot(HeroHead() + new Vector3(-1.2f, 0.4f, -2.2f), VanWindow(), 22f)); });
            Sound.MusicStop(0.5f);
            if (van != null) van.gameObject.SetActive(false);
            if (vanMaya != null) vanMaya.gameObject.SetActive(false);
            yield return Hold(2.4f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(1.0f, -0.1f, -1.2f), HeroHead(), 30f)));
            Say("YOU", "...Maya.", 2.0f);
            yield return Hold(2.2f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.8f, -0.1f, -1.0f), HeroHead(), 28f)));
        }

        // ---------------------------------------------------------------- on screen

        void BackyardV2GUI(float w, float h)
        {
            if (storyStill < 0 || storyStills == null) return;
            float t0 = Time.unscaledTime - storyStillAt;
            Fill(new Rect(0, 0, w, h), Color.black);
            var tex = storyStills[storyStill];
            if (tex != null)
            {
                float a = Mathf.Clamp01(t0 / 0.35f);
                float zoom = 1.03f + 0.03f * t0;
                float iw = w * zoom, ih = iw * 9f / 16f;
                if (ih < h * zoom) { ih = h * zoom; iw = ih * 16f / 9f; }
                var o = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(new Rect((w - iw) * 0.5f, (h - ih) * 0.5f, iw, ih), tex);
                GUI.color = o;
            }
            cap.alignment = TextAnchor.MiddleCenter;
            cap.normal.textColor = new Color(0.58f, 0.8f, 1f, 0.8f);
            GUI.Label(new Rect(0, h * 0.08f, w, 30), Spaced("LAST NIGHT  ·  AS SHE TELLS IT"), cap);
            cap.alignment = TextAnchor.MiddleLeft;
        }
    }
}
