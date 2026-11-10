using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using SKey = UnityEngine.InputSystem.Key;
#endif

namespace ProjectInk
{
    /// <summary>CHAPTER 6 · THE WALK-OUT ("OVER AND OUT", docs/story/walkout.md): the highlight of the
    /// game. One unbroken take, a reverse dolly in front of a father and his daughter walking out of
    /// Kessel Hill's great door at dawn, hand in hand, across the forecourt, through the gate, onto
    /// the bridge. Two toy walkies; the last of Voss's men at the plunger; "Over and out." and the
    /// click lands with the first blast; the whole hill going up behind them, white fire and black
    /// smoke; and the slow motion on the bridge: her look up at him, their hands swung, the ribbon
    /// whipping red in the hot wind, two clicks on a dead walkie, and two back.</summary>
    public partial class StoryIntro
    {
        // ---------------------------------------------------------------- the cast and the props
        PuppetRig rigH, rigM, rigMan;
        PuppetRig.Arm hNear, hFree, mNear, mFree;
        Enemy lastMan;
        Transform mWalkie, hWalkie, mPip, plungerBox, plungerHandle;
        LineRenderer plungerWire;
        ParticleSystem woDrift;
        readonly List<Transform> woGlows = new List<Transform>();

        // ---------------------------------------------------------------- the take's state
        float woT = -1f;                    // the take's clock (real seconds); -1 when not in the take
        float heroZ, mayaOff = -0.55f, mayaOffWant = -0.55f, mayaYaw = 90f, mayaYawWant = 90f, mayaHop, mayaZ, mayaZWant;
        bool woHeld;
        float swing, holdBack, plungerUp, woShake, woFlash, woOverText = -1f, heroHeight = 1.45f, woBurn;
        bool woWalking, pipDead;
        float nextStep, nextMayaStep, nextFire;
        string woHeroClip = "", woMayaClip = "";

        static float WoEase(float a, float b, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, t));
        static Vector3 WoAt(float x, float y, float z) => new Vector3(x, FortressPlan.Up + y, z);

        // ================================================================ the scene

        IEnumerator WalkOut()
        {
            // the morning: the dawn bake, the way out the front, no rain, no storm
            storm = false;
            rainAmount = 0f;
            if (rainPs != null) rainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            LightningSwap.Held = false;
            LightningSwap.Dawn = true;
            CutawayWall.ShowAll = false;
            lensRain = 0f;
            Sound.Ambience(0f, 0.15f, 22000f);
            Sound.MusicStop(1.0f);
            SetRipple(0f);
            if (voss != null) voss.gameObject.SetActive(false);
            BlastFx.Clear();
            woBurn = 0f;
            // above the sky's backdrop (the camera tilts up at the end): the morning's own grey-blue
            var skyCam = Camera.main;
            Color prevBg = skyCam != null ? skyCam.backgroundColor : Color.black;
            if (skyCam != null) { skyCam.clearFlags = CameraClearFlags.SolidColor; skyCam.backgroundColor = new Color(0.72f, 0.745f, 0.8f); }
            if (bookProp != null) bookProp.gameObject.SetActive(false);
            WoCast();
            blackout = 1f;

            // ------------------------------------------------ 0 the establishing wide: the whole of Kessel Hill at dawn
            Sound.Play(Sfx.Wind, 0.3f);
            Sound.PlayTake(Sfx.Birds, 0, 0.45f);
            StartCoroutine(FadeIn(1.4f));
            float est = 6.0f;
            for (float t0 = 0f; t0 < est; t0 += Time.unscaledDeltaTime)
            {
                float u = t0 / est;
                var p = Vector3.Lerp(WoAt(147.0f, 2.2f, -97f), WoAt(147.0f, 2.5f, -92.5f), CineCam.Ease(u)) + Sway(0.03f, 0.25f, 4f);
                Cine(new CineCam.Shot(p, WoAt(143.5f, 9.0f, 0f), 38f));
                if (t0 > 2.0f && t0 - Time.unscaledDeltaTime <= 2.0f) Sound.PlayTake(Sfx.Birds, 2, 0.35f);
                yield return null;
            }

            // ------------------------------------------------ the take
            Sound.Music("walkout", 0.9f, false, 0.05f);
            woT = 0f;
            heroZ = 0.3f;
            woWalking = true;
            nextStep = 0.4f; nextMayaStep = 0.55f;
            WoFrame(0f);
            var events = WoEvents();
            int next = 0;
            while (woT < 69.4f)
            {
                woT += Time.unscaledDeltaTime;
                while (next < events.Count && events[next].t <= woT) { events[next].act(); next++; }
                WoFrame(Time.deltaTime);
                yield return null;
            }
            // black: the last words, and the end of it
            blackout = 1f;
            if (TimeController.Instance != null) TimeController.Instance.CineScale = 1f;
            Sound.KeepClear = false;
            woOverText = Time.unscaledTime;
            yield return RealWait(4.6f);
            woOverText = -1f;
            woT = -1f;
            if (skyCam != null) skyCam.backgroundColor = prevBg;
            RibbonFlutter.Wind = 1f;
        }

        struct WoEvent { public float t; public System.Action act; public WoEvent(float t, System.Action a) { this.t = t; act = a; } }

        // ================================================================ the cast

        void WoCast()
        {
            hero.CinePuppet();
            barsPx = 92f;
            if (maya == null) maya = SpawnMaya(WoAt(146.75f, 0f, 0.3f), 1);
            mayaHere = true;
            if (maya != null && maya.body != null && maya.body.Bone("head") != null && maya.body.Bone("head").Find("RibbonBow") == null) TieRibbon();
            hero.SetFacing(1);
            if (maya != null) maya.SetFacing(1);
            heroHeight = Mathf.Max(0.8f, HeroHead().y - hero.transform.position.y);
            rigH = PuppetRig.On(hero.body);
            rigM = maya != null ? PuppetRig.On(maya.body) : null;
            WoPlace();
            hNear = rigH.Near(maya != null ? maya.transform.position : hero.transform.position + Vector3.left);
            hFree = rigH.Other(hNear);
            if (rigM != null) { mNear = rigM.Near(hero.transform.position); mFree = rigM.Other(mNear); }
            // the walkies: hers in her free hand (its red pip blinking: the battery), his in his coat
            if (rigM != null && mFree.hand != null)
            {
                mWalkie = MakeWalkie("MayaWalkie");
                WoHold(mWalkie, mFree.hand);
                var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(pip.GetComponent<Collider>());
                pip.name = "Pip";
                pip.transform.SetParent(mWalkie, false);
                pip.transform.localPosition = new Vector3(0.018f, 0.12f, -0.02f);
                pip.transform.localScale = Vector3.one * 0.014f;
                pip.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.9f, 0.05f, 0.06f) };
                mPip = pip.transform;
            }
            if (hFree.hand != null)
            {
                hWalkie = MakeWalkie("HeroWalkie");
                WoHold(hWalkie, hFree.hand);
                hWalkie.gameObject.SetActive(false);
            }
            WoFreeRest(50f);
            // hand in hand from the first frame
            hNear.To(WoHoldPoint, () => -rigH.Forward + Vector3.down * 0.3f, 50f);
            if (rigM != null) mNear.To(WoHoldPoint, () => Vector3.down + WoSide() * 0.4f, 50f);
            if (rigM != null) mFree.To(() => WoChestFront(maya, 0.13f, -0.08f), () => Vector3.down + WoSide() * 0.5f, 50f);

            // the last of Voss's men, waiting inside the east range's door
            if (director != null)
            {
                lastMan = director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
                if (lastMan != null)
                {
                    lastMan.Puppet();
                    lastMan.SetBaseColor(new Color(0.3f, 0.3f, 0.32f));
                    Dress(lastMan.body, Look.Driver);
                    lastMan.PuppetPose(WoAt(FortressPlan.RangeX0 + 0.6f, 0f, FortressPlan.ManDoorZ));
                    lastMan.PuppetPlay("idle", 0f);
                    lastMan.gameObject.SetActive(false);
                    rigMan = PuppetRig.On(lastMan.body);
                }
            }
            // the detonator: a wooden box, a T-handle on its plunger, the wire back to the house
            {
                plungerBox = new GameObject("Detonator").transform;
                plungerBox.position = WoAt(149.15f, 0f, -26.75f);           // at the gate, by the right-hand pier
                plungerBox.rotation = Quaternion.Euler(0f, -20f, 0f);
                var wood = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.22f, 0.2f, 0.18f) };
                var iron = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.08f, 0.08f, 0.09f) };
                Bit(plungerBox, PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.42f, 0.32f, 0.3f), wood);
                Bit(plungerBox, PrimitiveType.Cube, new Vector3(0f, 0.33f, 0f), new Vector3(0.44f, 0.03f, 0.32f), iron);
                Bit(plungerBox, PrimitiveType.Cube, new Vector3(-0.15f, 0.2f, -0.155f), new Vector3(0.04f, 0.04f, 0.02f), iron);
                Bit(plungerBox, PrimitiveType.Cube, new Vector3(0.15f, 0.2f, -0.155f), new Vector3(0.04f, 0.04f, 0.02f), iron);
                plungerHandle = new GameObject("Handle").transform;
                plungerHandle.SetParent(plungerBox, false);
                Bit(plungerHandle, PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.03f, 0.2f, 0.03f), iron);
                Bit(plungerHandle, PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.035f, 0.18f, 0.035f), iron, new Vector3(0f, 0f, 90f));
                plungerHandle.localPosition = new Vector3(0f, -0.04f, 0f);
                var wg = new GameObject("DetonatorWire");
                plungerWire = wg.AddComponent<LineRenderer>();
                plungerWire.sharedMaterial = iron;
                plungerWire.widthMultiplier = 0.018f;
                plungerWire.useWorldSpace = true;
                var pts = new[] { plungerBox.position + new Vector3(0.18f, 0.05f, 0.1f), WoAt(149.45f, 0.02f, -24.0f), WoAt(149.3f, 0.02f, -18.0f), WoAt(149.45f, 0.02f, -12.0f), WoAt(149.3f, 0.02f, -7.2f), WoAt(149.9f, 0.02f, -5.0f), WoAt(149.7f, 0.02f, -3.4f) };
                plungerWire.positionCount = pts.Length;
                plungerWire.SetPositions(pts);
            }
            // the lanterns still burning, small in the morning
            woGlows.Clear();
            foreach (var z in FortressPlan.CourtLampZ)
                for (int s = -1; s <= 1; s += 2) woGlows.Add(BlastFx.Glow(WoAt(FortressPlan.DoorX + s * 3.4f, 3.82f, z - 0.15f), 0.9f, 0.35f));
            foreach (var z in FortressPlan.BridgeLampZ)
                for (int s = -1; s <= 1; s += 2) woGlows.Add(BlastFx.Glow(WoAt(FortressPlan.DoorX + s * 2.8f, 3.64f, z - 0.15f), 0.9f, 0.35f));
            for (int s = -1; s <= 1; s += 2)
            {
                woGlows.Add(BlastFx.Glow(WoAt(FortressPlan.DoorX + s * 1.95f, 3.78f, FortressPlan.GateZ - 0.75f), 0.8f, 0.35f));
                woGlows.Add(BlastFx.Glow(WoAt(FortressPlan.DoorX + s * 5.2f, 2.95f, -4.05f), 0.8f, 0.3f));
            }
        }

        /// <summary>His free arm at rest: hanging a hand's width out from his side (so it reads against
        /// the ground, not lost on his body), swinging with his steps.</summary>
        void WoFreeRest(float rate) => hFree.To(WoFreeHand, () => Vector3.down * 0.4f - rigH.Forward * 0.6f - WoSide() * 0.5f, rate);

        Vector3 WoFreeHand()
        {
            if (hFree == null || hFree.upper == null) return hero.transform.position + Vector3.up * 0.6f;
            var sh = hFree.upper.position;
            float swingK = woWalking ? Mathf.Sin(Time.time * 7.3f + Mathf.PI) : 0f;
            return sh - WoSide() * 0.17f + Vector3.down * 0.37f + rigH.Forward * (0.02f + 0.11f * swingK);
        }

        // ================================================================ the player's part (no fail: it carries on without them)

        bool WoTapped()
        {
            bool down = PointerDown();
            var gi = GestureInput.Instance;
            bool tap = (down && !pointerWasDown) || AnyKeyTapped() || (gi != null && gi.AnyPressThisFrame);
            if (gi != null) gi.Flush();
            pointerWasDown = down;
            return tap;
        }

        /// <summary>HOLD to talk: his walkie comes up while it's held; held long enough, he answers her.
        /// If the player does nothing, he answers anyway (the take never stops).</summary>
        IEnumerator WoHoldTalk()
        {
            ScBegin(ScAsk.Hold, "ANSWER HER", M("PRESS AND HOLD TO TALK", "HOLD  E  TO TALK"));
            scHasWorld = true;
            float held = 0f, start = woT;
            bool said = false;
            while (!said)
            {
                woHeld = PointerDown() || KeyHeld(SKey.E, SKey.Space, SKey.Enter);
                bool auto = woT - start > 2.6f;
                if (woHeld || auto)
                {
                    held += Time.unscaledDeltaTime;
                    if (hWalkie != null && !hWalkie.gameObject.activeSelf) hWalkie.gameObject.SetActive(true);
                    hFree.To(() => WoMouth(rigH, 1f), () => Vector3.down - WoSide() * 0.5f, 3.2f);
                }
                else
                {
                    held = Mathf.Max(0f, held - Time.unscaledDeltaTime);
                    WoFreeRest(3f);
                }
                scWorld = HeroHead() + Vector3.up * 0.4f;
                scProgress = Mathf.Clamp01(held / 0.6f);
                if (held >= 0.6f)
                {
                    said = true;
                    Sound.PlayTake(Sfx.WalkieClick, 2, 0.35f, hero.transform.position);
                    Say("YOU", "I see you too, sweetheart. Over.", 2.6f);
                }
                yield return null;
            }
            ScEnd();
            for (float t0 = 0f; t0 < 2.6f; t0 += Time.unscaledDeltaTime) yield return null;
            WoFreeRest(2.5f);
            if (hWalkie != null) hWalkie.gameObject.SetActive(false);
            rigH.nod = 4f; rigH.turn = 0f; rigM.nod = 0f;
        }

        /// <summary>TAP: the click on "Over and out". It is the player's hand that ends it.</summary>
        IEnumerator WoTapClick()
        {
            ScBegin(ScAsk.Mash, "OVER AND OUT", M("TAP", "TAP  E"));
            scHasWorld = true;
            while (woT < 46.3f && !WoTapped())
            {
                scWorld = HeroHead() + Vector3.up * 0.4f;
                yield return null;
            }
            scProgress = 1f;
            Sound.PlayTake(Sfx.WalkieClick, 0, 0.85f, hero.transform.position);
            Haptics.Vibrate();
            ScEnd();
            for (float t0 = 0f; t0 < 0.3f; t0 += Time.unscaledDeltaTime) yield return null;
            hFree.To(WoCoat, () => Vector3.down - WoSide() * 0.6f, 4f);
        }

        /// <summary>TAP TWICE: two clicks back to her, from the walkie in his coat.</summary>
        IEnumerator WoTwoClicks()
        {
            hFree.To(WoCoat, () => Vector3.down - WoSide() * 0.6f, 3f);
            ScBegin(ScAsk.Mash, "ANSWER HER", M("TAP TWICE", "TAP  E  TWICE"));
            scHasWorld = true;
            int clicks = 0;
            float last = woT;
            while (clicks < 2)
            {
                scWorld = HeroHead() + Vector3.up * 0.4f;
                bool auto = woT > 64.6f && woT - last > 0.55f;
                if (WoTapped() || auto)
                {
                    Sound.PlayTake(Sfx.WalkieClick, clicks == 0 ? 2 : 0, 0.8f, hero.transform.position, 0.96f);
                    Haptics.Vibrate();
                    clicks++;
                    last = woT;
                    scProgress = clicks / 2f;
                }
                yield return null;
            }
            ScEnd();
            for (float t0 = 0f; t0 < 0.8f; t0 += Time.unscaledDeltaTime) yield return null;
            WoFreeRest(2f);
        }

        /// <summary>A prop in a hand, kept at its real size whatever the body's scale.</summary>
        static void WoHold(Transform prop, Transform hand)
        {
            prop.SetParent(hand, false);
            float s = Mathf.Max(0.01f, hand.lossyScale.x);
            prop.localScale = Vector3.one / s;
            prop.localPosition = new Vector3(0f, 0.03f / s, 0f);
            prop.localRotation = Quaternion.identity;
        }

        Vector3 WoSide() => hero != null && maya != null ? (maya.transform.position - hero.transform.position).normalized : Vector3.left;

        /// <summary>Where their joined hands are: between his hanging hand and her raised one, swung.</summary>
        Vector3 WoHoldPoint()
        {
            if (hNear == null || mNear == null || hNear.upper == null || mNear.upper == null) return hero.transform.position + Vector3.up * 0.6f;
            Vector3 sh = hNear.upper.position, sm = mNear.upper.position;
            float lh = (hNear.fore.position - sh).magnitude + (hNear.hand.position - hNear.fore.position).magnitude;
            float lm = (mNear.fore.position - sm).magnitude + (mNear.hand.position - mNear.fore.position).magnitude;
            var toH = sh - sm;
            var p = sm + toH.normalized * Mathf.Min(lm * 0.92f, toH.magnitude * 0.5f) + Vector3.down * lm * 0.12f;
            var f = rigH.Forward;
            p += f * (0.05f + 0.03f * Mathf.Sin(Time.time * 7.3f) * (woWalking ? 1f : 0f));
            p += -f * holdBack;
            p += (f * 0.32f + Vector3.up * 0.16f) * swing * lm / 0.27f;
            var d = p - sh;                                     // within his reach
            if (d.magnitude > lh * 0.97f) p = sh + d.normalized * lh * 0.97f;
            return p;
        }

        Vector3 WoChestFront(Enemy e, float fwd, float down)
        {
            var c = e != null && e.body != null ? e.body.Bone("chest") : null;
            float s = e != null ? e.transform.localScale.y : 1f;
            var basePos = c != null ? c.position : e.transform.position + Vector3.up * 0.9f * s;
            return basePos + rigM.Forward * fwd * s + Vector3.up * down * s;
        }

        Vector3 WoMouth(PuppetRig r, float scale)
        {
            var hd = r.Head;
            var c = hd != null ? hd.position + Vector3.up * 0.17f * scale : r.transform.position + Vector3.up * 1.2f * scale;
            return c + r.Forward * 0.16f * scale + Vector3.down * 0.03f * scale;
        }

        Vector3 WoCoat()
        {
            var c = hero.body.Bone("chest");
            return (c != null ? c.position : hero.transform.position + Vector3.up) + rigH.Forward * 0.14f + Vector3.up * 0.02f;
        }

        /// <summary>The back of her head, for his hand.</summary>
        Vector3 WoHerHead()
        {
            var hd = maya != null && maya.body != null ? maya.body.Bone("head") : null;
            float s = maya != null ? maya.transform.localScale.y : 0.58f;
            var c = hd != null ? hd.position + Vector3.up * 0.2f * s : maya.transform.position + Vector3.up * 0.75f;
            return c - rigH.Forward * 0.06f + (hero.transform.position - maya.transform.position).normalized * 0.03f;
        }

        Vector3 WoHisLeg()
        {
            var hp = hero.body.Bone("hips");
            var p = hp != null ? hp.position : hero.transform.position + Vector3.up * 0.7f;
            return p + Vector3.down * 0.12f + (maya.transform.position - hero.transform.position).normalized * 0.05f + rigH.Forward * 0.06f;
        }

        // ================================================================ every frame

        /// <summary>Their walking speed through the take (m/s): out of the dark, across the forecourt,
        /// strolling while the walkies talk, on through the gate and over the bridge; a falter at the
        /// blast; then the slow motion slows everything else.</summary>
        float WoSpeed(float t)
        {
            if (t < 4.3f) return 0.85f;
            if (t < 15.5f) return t > 10.4f && t < 12.2f ? 1.08f : 1.2f;
            if (t < 25f) return Mathf.Lerp(1.2f, 0.45f, WoEase(15.5f, 16.3f, t));
            if (t < 47.3f) return Mathf.Lerp(0.45f, 1.2f, WoEase(25f, 25.8f, t));
            if (t < 50f) return Mathf.Lerp(1.2f, 0.95f, WoEase(47.3f, 47.7f, t)) + 0.25f * WoEase(48.6f, 50f, t);
            return 1.2f;
        }

        void WoPlace()
        {
            var hp = WoAt(147.3f, 0f, heroZ);
            hero.CinePose(hp);
            hero.transform.rotation = Quaternion.Euler(0f, 90f, 0f);          // facing the camera (-z)
            if (maya != null)
            {
                maya.PuppetPose(WoAt(147.3f + mayaOff, mayaHop, heroZ + 0.04f + mayaZ));
                maya.transform.rotation = Quaternion.Euler(0f, mayaYaw, 0f);
            }
        }

        void WoFrame(float dt)
        {
            float t = woT;
            float v = WoSpeed(t);
            heroZ -= v * dt;
            // her place beside him, her turn, her hop
            float k = 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime);
            mayaOff = Mathf.Lerp(mayaOff, mayaOffWant, k);
            mayaZ = Mathf.Lerp(mayaZ, mayaZWant, k);
            mayaYaw = Mathf.Lerp(mayaYaw, mayaYawWant, 1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));
            mayaHop = 0f;
            if (t > 12.6f && t < 13.7f) mayaHop = 0.07f * Mathf.Abs(Mathf.Sin((t - 12.6f) / 1.1f * Mathf.PI * 2f));          // she skips
            swing = t > 50.2f && t < 52.6f ? Mathf.Sin((t - 50.2f) / 2.4f * Mathf.PI) : 0f;          // (slow motion: a long, floating swing)
            if (swing > 0f) mayaHop = 0.1f * swing;
            holdBack = t > 19.3f && t < 20.4f ? 0.12f * Mathf.Sin((t - 19.3f) / 1.1f * Mathf.PI) : 0f;          // her tug
            WoPlace();
            // walking or standing: the legs follow the speed
            bool walking = v > 0.06f;
            woWalking = walking;
            string hc = walking ? "walk" : (hero.body.Has("hold_hand") ? "hold_hand" : "idle");
            float hs = walking ? 0.93f * v : 1f;
            if (hc != woHeroClip) { hero.body.Play(hc, 0.25f, true, hs); woHeroClip = hc; }
            else if (walking) hero.body.Play(hc, 0.25f, false, hs);
            if (maya != null)
            {
                string mc = walking ? "walk" : "idle";
                float ms = walking ? 1.5f * v : 1f;
                if (mc != woMayaClip) { maya.PuppetPlay(mc, 0.2f, ms); woMayaClip = mc; }
                else if (walking) maya.body.Play(mc, 0.2f, false, ms);
            }
            // footsteps on stone, then gravel; hers lighter and quicker
            if (walking && Time.time >= nextStep)
            {
                nextStep = Time.time + 0.43f / Mathf.Max(0.3f, v);
                Sound.Play(Sfx.Footstep, heroZ > -3.4f ? 0.32f : 0.24f, hero.transform.position, heroZ > -3.4f ? 0.92f : 1f);
            }
            if (walking && Time.time >= nextMayaStep)
            {
                nextMayaStep = Time.time + 0.29f / Mathf.Max(0.3f, v);
                Sound.Play(Sfx.Footstep, 0.1f, maya != null ? maya.transform.position : hero.transform.position, 1.45f);
            }
            // her walkie's battery pip: blinking, then stuttering, then dead
            if (mPip != null)
            {
                bool on = pipDead ? false : t > WoB + 2.4f ? Mathf.Repeat(Time.unscaledTime * 9f, 1f) < 0.4f && Random.value > 0.3f : Mathf.Repeat(Time.unscaledTime * 0.85f, 1f) < 0.16f;
                if (t > WoB + 3.4f) pipDead = true;
                mPip.GetComponent<Renderer>().enabled = on;
            }
            // the plunger's handle
            if (plungerHandle != null) plungerHandle.localPosition = new Vector3(0f, Mathf.Lerp(-0.18f, 0.12f, plungerUp), 0f);
            // the fires: keep roaring under it all
            if (t > WoB + 1.4f && Time.unscaledTime >= nextFire)
            {
                nextFire = Time.unscaledTime + 5.4f;
                Sound.Play(Sfx.FireRoar, t > 52.6f ? 0.35f : 0.55f, WoAt(147f, 6f, 0f));
            }
            // the drift of ash and embers round the lens
            if (woDrift != null && Camera.main != null) woDrift.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 3f;
            woShake = Mathf.MoveTowards(woShake * Mathf.Exp(-2.4f * Time.unscaledDeltaTime), 0f, Time.unscaledDeltaTime * 0.002f);
            woFlash *= Mathf.Exp(-5.5f * Time.unscaledDeltaTime);
            RibbonFlutter.Wind = Mathf.Lerp(RibbonFlutter.Wind, t > WoB + 0.2f ? (t > 52.6f ? 2.6f : 3.4f) : 1f, 1f - Mathf.Exp(-2f * Time.unscaledDeltaTime));
            BlastFx.Tick(woBurn);
            WoCamera(t);
        }

        void WoCamera(float t)
        {
            var hp = hero.transform.position;
            // the reveal on the bridge: further back, wider, tilting up until the whole of Kessel Hill
            // is behind them when it goes; then the slow motion pulls out and up over the burning hill
            float reveal = WoEase(30.5f, 46.5f, t);
            float slow = WoEase(52.6f, 67f, t);
            float dist = 3.4f;
            dist = Mathf.Lerp(dist, 2.9f, WoEase(16.2f, 24.5f, t));
            dist = Mathf.Lerp(dist, 3.4f, WoEase(25.2f, 29.5f, t));
            dist = Mathf.Lerp(dist, 4.6f, reveal);
            dist = Mathf.Lerp(dist, 6.0f, slow);
            float camY = hp.y + heroHeight * 0.8f + 0.25f * reveal + 0.45f * slow;
            float lookY = hp.y + heroHeight * 0.7f + 1.4f * reveal + 0.9f * slow;
            float fov = 36f + 6f * reveal + 4f * slow;
            // waiting at the porch until they come to it, then backing away in front of them
            float zStatic = -8.6f, zFollow = hp.z - dist;
            float h = Mathf.Max(0.9f - Mathf.Abs(zStatic - zFollow), 0f) / 0.9f;
            float camZ = Mathf.Min(zStatic, zFollow) - h * h * 0.9f * 0.25f;
            float arc = 0.55f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - 5.5f) / 7.5f));
            float camX = FortressPlan.DoorX + 0.05f + arc - 0.25f * reveal;
            var pos = new Vector3(camX, camY, camZ);
            var subject = new Vector3(hp.x - 0.27f - 0.3f * reveal, lookY, hp.z + 0.6f);
            var door = WoAt(FortressPlan.DoorX, 1.55f, FortressPlan.DoorZ);
            var look = Vector3.Lerp(door, subject, WoEase(2.8f, 6.0f, t));
            pos += Sway(0.012f, 0.5f, 21f);
            look += Sway(0.008f, 0.4f, 9f);
            if (woShake > 0.0005f)
            {
                float T = Time.unscaledTime * 18f;
                var sh = new Vector3(Mathf.PerlinNoise(T, 1.3f) - 0.5f, Mathf.PerlinNoise(2.1f, T) - 0.5f, 0f) * woShake;
                pos += sh;
                look += sh * 1.6f;
            }
            Cine(new CineCam.Shot(pos, look, fov));
        }

        // ================================================================ the timeline

        const float WoB = 47.2f;          // the first blast: the whole of Kessel Hill behind them

        List<WoEvent> WoEvents()
        {
            var e = new List<WoEvent>();
            void At(float t, System.Action a) => e.Add(new WoEvent(t, a));

            At(0.3f, () => Sound.PlayTake(Sfx.Birds, 1, 0.35f));
            At(6.5f, () => Sound.PlayTake(Sfx.Birds, 0, 0.3f));
            At(13.0f, () => Sound.PlayTake(Sfx.Birds, 2, 0.25f));
            // ---- 16 they slow; she raises her walkie
            At(15.9f, () => { rigH.nod = 14f; rigH.LookTowards(maya.transform.position, 25f); });
            At(16.0f, () => { mFree.To(() => WoMouth(rigM, maya.transform.localScale.y), () => Vector3.down + WoSide() * 0.4f, 3.5f); rigM.nod = -12f; rigM.LookTowards(hero.transform.position, 35f); });
            At(16.3f, () => { Sound.PlayTake(Sfx.WalkieClick, 1, 0.35f, maya.transform.position); Walkie("Papa? ...I can see you. Over.", 2.6f); });
            At(18.9f, () => { mFree.To(() => WoChestFront(maya, 0.13f, -0.08f), () => Vector3.down + WoSide() * 0.5f, 3f); rigM.turn = 0f; });
            // ---- his walkie from his coat
            At(18.9f, () => StartCoroutine(WoHoldTalk()));                     // the player answers her: HOLD to talk
            // ---- 24 the stables' door bangs: the last man runs in from the right
            At(24.0f, () =>
            {
                Sound.Play(Sfx.Door, 0.8f, WoAt(FortressPlan.RangeX0, 1f, FortressPlan.ManDoorZ));
                if (lastMan != null) { lastMan.gameObject.SetActive(true); lastMan.PuppetPlay("run", 0.05f, 1.2f); }
                StartCoroutine(WoManRun());
            });
            At(27.9f, () => { Sound.PlayTake(Sfx.Plunger, 0, 0.75f, plungerBox.position); StartCoroutine(WoLerp(0.45f, u => plungerUp = u)); });
            At(28.6f, () =>
            {
                Say("MAN", "Boss said... if he falls, the hill burns.", 2.7f, lastMan != null ? HeadOf(lastMan) : null);
                if (lastMan != null) { rigH.nod = 4f; rigH.LookTowards(lastMan.transform.position, 70f); rigM.nod = 0f; rigM.LookTowards(lastMan.transform.position, 75f); }
                mayaOffWant = -0.38f;
            });
            At(31.3f, () => { rigH.turn = 0f; rigH.nod = 3f; Say("YOU", "...Then burn it.", 1.9f); });
            At(32.2f, () => { rigM.turn = 0f; rigM.nod = 0f; mayaOffWant = -0.55f; });
            // ---- on the bridge: "Can we go home now?"
            At(41.4f, () => { mFree.To(() => WoMouth(rigM, maya.transform.localScale.y), () => Vector3.down + WoSide() * 0.4f, 3.5f); rigM.nod = -8f; });
            At(41.5f, () => { Sound.PlayTake(Sfx.WalkieClick, 0, 0.35f, maya.transform.position); Walkie("Can we go home now? Over.", 2.6f); });
            At(44.1f, () => { mFree.To(() => WoChestFront(maya, 0.13f, -0.08f), () => Vector3.down + WoSide() * 0.5f, 3f); rigM.nod = 0f; });
            At(44.3f, () => hFree.To(() => WoMouth(rigH, 1f), () => Vector3.down - WoSide() * 0.5f, 3.4f));
            At(44.6f, () => Say("YOU", "Over and out.", 1.5f));
            At(45.0f, () => StartCoroutine(WoTapClick()));                     // the player's click: "Over and out"
            At(46.8f, () => { Sound.PlayTake(Sfx.Plunger, 1, 0.5f, plungerBox.position); StartCoroutine(WoLerp(0.18f, u => plungerUp = 1f - u)); });
            At(46.95f, () => { if (hWalkie != null) hWalkie.gameObject.SetActive(false); WoFreeRest(2.5f); });

            // ================================================================ the whole hill goes up
            float B = WoB;
            var back = Vector3.back;
            WoBlast(e, B, WoAt(FortressPlan.DoorX, 2.3f, -3.6f), 1.9f, back, 0, 1f);                                   // the great door
            WoBlast(e, B + 0.12f, WoAt(FortressPlan.DoorX, 8.4f, -5.9f), 1.4f, back + Vector3.up, 1, 0.5f);           // the porch's balcony
            float[] bays = { 140.85f, 153.15f, 135.75f, 158.25f, 131.1f, 162.9f };
            for (int i = 0; i < bays.Length; i++)
            {
                WoBlast(e, B + 0.25f + i * 0.13f, WoAt(bays[i], 3.6f, -3.9f), 1.25f, back + Vector3.up * 0.3f, 2 + i % 3, 0.45f);
                WoBlast(e, B + 0.55f + i * 0.1f, WoAt(bays[i], 8.2f, -3.8f), 1.0f, back + Vector3.up * 0.6f, 1 + i % 3, 0.3f, quiet: true);
            }
            WoBlast(e, B + 0.7f, WoAt(127.8f, 15f, -4f), 1.4f, Vector3.up + Vector3.left * 0.4f, 3, 0.4f);              // the corner turrets
            WoBlast(e, B + 1.0f, WoAt(166.2f, 15f, -4f), 1.4f, Vector3.up + Vector3.right * 0.4f, 4, 0.4f);
            for (int i = 0; i < 3; i++)                                                                                 // the roof terrace
                WoBlast(e, B + 1.0f + i * 0.15f, WoAt(134f + i * 13f, FortressPlan.RoofY - FortressPlan.Up + 1.5f, 0.2f), 2.0f, Vector3.up, i, 0.55f);
            WoBlast(e, B + 1.4f, WoAt(165f, 4.5f, -15f), 2.0f, Vector3.left + Vector3.up * 0.5f, 1, 0.6f);              // the stables
            WoBlast(e, B + 1.6f, WoAt(165f, 8f, -8f), 1.6f, Vector3.up, 2, 0.4f);
            WoBlast(e, B + 1.8f, WoAt(118f, 8f, 3f), 1.8f, Vector3.up + Vector3.back * 0.4f, 3, 0.45f);               // the courtyard's keep face
            WoBlast(e, B + 2.05f, WoAt(106f, 8f, 3f), 1.6f, Vector3.up + Vector3.back * 0.4f, 4, 0.4f);
            WoBlast(e, B + 2.2f, WoAt(138f, 22f, 18f), 1.8f, Vector3.up, 0, 0.45f);                                    // the towers behind
            WoBlast(e, B + 2.35f, WoAt(102f, 20f, 18f), 1.6f, Vector3.up, 1, 0.4f);
            WoBlast(e, B + 2.6f, WoAt(115f, 22f, 20f), 3.0f, Vector3.up, 0, 0.9f, big: true);                          // the keep
            WoBlast(e, B + 2.9f, WoAt(FortressPlan.TowerX, FortressPlan.GalleryY - FortressPlan.Up + 3f, FortressPlan.TowerZ - 1.5f), 3.4f, Vector3.up, 1, 1.2f, big: true);   // the dome
            WoBlast(e, B + 3.4f, WoAt(172f, 26f, 18f), 1.8f, Vector3.up, 2, 0.4f);
            WoBlast(e, B + 3.55f, WoAt(130f, 14f, 19f), 1.5f, Vector3.up, 3, 0.35f);
            WoBlast(e, B + 3.9f, WoAt(163f, 10f, -20f), 1.4f, Vector3.up, 4, 0.3f);
            WoBlast(e, B + 5.0f, WoAt(140f, 6f, -3.5f), 1.0f, back, 2, 0.25f);
            // what comes down: the turrets' spirelets, the chimneys, the dome off the tower
            At(B + 0.75f, () => WoFall("FortSpireW", WoAt(127.8f, 16.4f, -4.05f), new Vector3(-0.6f, 0f, -1f), 0f));
            At(B + 1.05f, () => WoFall("FortSpireE", WoAt(166.2f, 16.4f, -4.05f), new Vector3(0.5f, 0f, -1f), 0f));
            for (int i = 0; i < FortChimneyX.Length; i++)
            {
                int ci = i;
                At(B + 0.9f + i * 0.22f, () => WoFall("FortChimney" + ci, WoAt(FortChimneyX[ci], 12f, -1.2f), new Vector3(ci % 2 == 0 ? -0.3f : 0.3f, 0f, 1f), 0f));
            }
            At(B + 2.95f, () => WoFall("FortDome", WoAt(FortressPlan.TowerX, FortressPlan.GalleryY - FortressPlan.Up + 0.3f, FortressPlan.TowerZ), new Vector3(0.4f, 0f, 1f), 11f));
            // the sound of it: the pressure wave reaching them, glass, the masonry coming down, the roar under it all
            At(B + 0.15f, () => { Sound.Play(Sfx.Shockwave, 0.9f); woShake = Mathf.Min(0.22f, woShake + 0.12f); });
            At(B + 0.6f, () => Sound.Play(Sfx.GlassRain, 0.6f, WoAt(147f, 4f, -3.5f)));
            At(B + 3.3f, () => Sound.Play(Sfx.Collapse, 0.85f, WoAt(130f, 10f, 10f)));
            At(B + 5.2f, () => Sound.Play(Sfx.Collapse, 0.7f, WoAt(150f, 15f, 5f)));
            At(B + 4.0f, () => Sound.Play(Sfx.Rumble, 0.55f));
            At(B + 17.0f, () => Sound.Play(Sfx.Rumble, 0.4f));
            // the morning goes dark with smoke; the fortress fills with fire
            At(B + 0.6f, () => StartCoroutine(WoLerp(3.0f, u => woBurn = u)));
            At(B + 0.2f, () =>
            {
                // she flinches and starts to turn; he lets go, his hand on the back of her head, draws her in
                rigM.nod = 18f; mayaYawWant = 25f;
                hNear.To(WoHerHead, () => -rigH.Forward + WoSide() * 0.6f, 5f);
                mNear.Off(6f);
                mayaOffWant = -0.46f; mayaZWant = -0.14f;                   // close in, but clear of him: in sight
            });
            At(B + 0.5f, () => mNear.To(WoHisLeg, () => Vector3.down - rigM.Forward * 0.5f, 4f));
            At(B + 6.0f, () => mayaZWant = -0.05f);
            At(B + 1.6f, () => { mayaYawWant = 90f; rigM.nod = 8f; rigH.nod = 6f; });
            At(B + 1.2f, () => { Sound.Play(Sfx.Tinnitus, 0.22f); if (Camera.main != null) woDrift = BlastFx.Drift(Camera.main.transform); });

            // ================================================================ the slow motion
            At(52.6f, () => { Sound.KeepClear = true; StartCoroutine(WoLerp(1.6f, u => { if (TimeController.Instance != null) TimeController.Instance.CineScale = Mathf.Lerp(1f, 0.3f, u); })); Sound.PlayTake(Sfx.Ember, 0, 0.5f); });
            WoBlast(e, 54.4f, WoAt(147.5f, FortressPlan.RoofY - FortressPlan.Up + 2.5f, -1.0f), 2.6f, Vector3.up + Vector3.back * 0.2f, 0, 0.5f, pitch: 0.78f, big: true);
            At(55.6f, () => { rigM.nod = -38f; rigM.LookTowards(hero.transform.position, 30f); });
            At(56.1f, () => { rigH.nod = 24f; rigH.LookTowards(maya.transform.position, 20f); });
            At(57.0f, () => { rigH.chestNod = 8f; rigH.headRate = 1.6f; Sound.Play(Sfx.Breath, 0.22f, hero.transform.position, 0.9f); });
            At(57.8f, () => rigH.chestNod = 4f);
            At(58.2f, () =>
            {
                mNear.To(WoHoldPoint, () => Vector3.down + WoSide() * 0.4f, 2.6f);
                hNear.To(WoHoldPoint, () => -rigH.Forward + Vector3.down * 0.3f, 2.2f);
                mayaOffWant = -0.52f;
                rigM.nod = -16f; rigH.nod = 10f;
            });
            At(59.0f, () => Sound.PlayTake(Sfx.Ember, 1, 0.45f));
            // two clicks on a dead walkie, and two back
            At(61.4f, () => { mFree.To(() => WoChestFront(maya, 0.2f, 0.06f), () => Vector3.down + WoSide() * 0.5f, 3f); rigM.nod = 10f; rigM.turn = 0f; });
            At(62.0f, () => Sound.PlayTake(Sfx.WalkieClick, 0, 0.9f, maya.transform.position));
            At(62.55f, () => Sound.PlayTake(Sfx.WalkieClick, 1, 0.9f, maya.transform.position));
            At(63.0f, () => { rigM.nod = -30f; rigM.LookTowards(hero.transform.position, 30f); });
            At(63.2f, () => StartCoroutine(WoTwoClicks()));                    // the player answers: two clicks back
            At(65.2f, () => mFree.To(() => WoChestFront(maya, 0.13f, -0.08f), () => Vector3.down + WoSide() * 0.5f, 2f));
            At(65.4f, () => { rigM.nod = -6f; rigM.turn = 0f; rigH.nod = 4f; rigH.turn = 0f; });
            At(66.6f, () => StartCoroutine(WoLerp(2.6f, u => blackout = u * u)));
            e.Sort((a, b) => a.t.CompareTo(b.t));
            return e;
        }

        static readonly float[] FortChimneyX = { 131f, 136f, 158f, 163f };

        /// <summary>A blast on the timeline: the fire, its sound (late by its distance, like a real one),
        /// the stones raining after, the flash and the shake.</summary>
        void WoBlast(List<WoEvent> e, float t, Vector3 at, float k, Vector3 outward, int take, float flash, float pitch = 1f, bool big = false, bool quiet = false)
        {
            e.Add(new WoEvent(t, () =>
            {
                BlastFx.Blast(at, k, outward, FortressPlan.Up);
                var c = Camera.main != null ? Camera.main.transform.position : at;
                float d = Vector3.Distance(c, at);
                float vol = Mathf.Clamp01(k * 0.45f / (1f + d / 70f) + 0.3f) * (quiet ? 0.55f : 1f);
                var sfx = big ? Sfx.ExplosionBig : Sfx.Explosion;
                if (pitch < 1f) Sound.PlayTake(sfx, take, vol, at, pitch);       // in the slow motion: deep, slow
                else if (big) Sound.PlayLater(sfx, d / 343f, vol, at);
                else StartCoroutine(WoSoundLater(sfx, take, d / 343f, vol, at));
                if (!quiet) Sound.PlayLater(Sfx.Debris, d / 343f + 0.9f, vol * 0.6f, at);
                woFlash = Mathf.Max(woFlash, flash * Mathf.Clamp01(1.25f - d / 140f));
                StartCoroutine(WoShakeLater(d / 343f, 0.05f * k / (1f + d / 40f)));
            }));
        }

        IEnumerator WoSoundLater(Sfx s, int take, float secs, float vol, Vector3 at)
        {
            yield return new WaitForSecondsRealtime(secs);
            Sound.PlayTake(s, take, vol, at);
        }

        IEnumerator WoShakeLater(float secs, float amp)
        {
            yield return new WaitForSecondsRealtime(secs);
            woShake = Mathf.Min(0.22f, woShake + amp);
        }

        /// <summary>A piece of the fortress coming down: wrapped on a pivot at its foot, it topples
        /// over towards `lean` (a tower's spirelet, a chimney stack), or is thrown up off the tower
        /// and falls (the dome, `launch` m/s up); it is gone into the smoke when it drops low.</summary>
        void WoFall(string part, Vector3 pivotAt, Vector3 lean, float launch)
        {
            var t = Find(part);
            if (t == null) return;
            var pivot = new GameObject(part + "_Pivot").transform;
            pivot.position = pivotAt;
            t.SetParent(pivot, true);
            StartCoroutine(WoFallRun(pivot, lean.normalized, launch));
        }

        IEnumerator WoFallRun(Transform pivot, Vector3 lean, float launch)
        {
            var axis = Vector3.Cross(Vector3.up, lean).normalized;          // tipping the top towards `lean`
            float ang = 0f, w = launch > 0f ? 40f : 4f;
            var vel = launch > 0f ? new Vector3(lean.x * 2.5f, launch, lean.z * 2.5f) : Vector3.zero;
            bool free = launch > 0f;
            float life = 0f;
            while (pivot != null && life < 9f)
            {
                float dt = Time.deltaTime;
                life += dt;
                if (!free)
                {
                    // a toppling tower: it falls faster the further it leans, then breaks off and drops
                    w += (60f + 260f * Mathf.Sin(ang * Mathf.Deg2Rad)) * dt;
                    ang += w * dt;
                    pivot.rotation = Quaternion.AngleAxis(w * dt, axis) * pivot.rotation;
                    if (ang > 62f) { free = true; vel = lean * 3f + Vector3.down * 1f; }
                }
                else
                {
                    vel += Vector3.down * 9.8f * dt;
                    pivot.position += vel * dt;
                    pivot.rotation = Quaternion.AngleAxis(w * dt, axis) * pivot.rotation;
                    if (pivot.position.y < FortressPlan.Up + 2f) { pivot.gameObject.SetActive(false); yield break; }
                }
                yield return null;
            }
        }

        IEnumerator WoLerp(float secs, System.Action<float> set)
        {
            for (float t0 = 0f; t0 < secs; t0 += Time.unscaledDeltaTime) { set(Mathf.SmoothStep(0f, 1f, t0 / secs)); yield return null; }
            set(1f);
        }

        /// <summary>The last man: out of the east range's door, across the gravel to the box, down on
        /// his knees, his hands on the plunger's handle; after, he stays down, watching it burn.</summary>
        IEnumerator WoManRun()
        {
            if (lastMan == null) yield break;
            var a = WoAt(FortressPlan.RangeX0 - 0.4f, 0f, FortressPlan.ManDoorZ);
            var b = plungerBox.position + new Vector3(0.05f, 0f, 0.42f);
            var dir = (b - a); dir.y = 0f;
            float yaw = -Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
            for (float t0 = 0f; t0 < 3.6f; t0 += Time.unscaledDeltaTime)
            {
                float u = t0 / 3.6f;
                lastMan.PuppetPose(Vector3.Lerp(a, b, u * (2f - u) * 0.5f + u * 0.5f));
                lastMan.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                yield return null;
            }
            lastMan.PuppetPose(b);
            lastMan.PuppetPlay(lastMan.body.Has("kneel_hand") ? "kneel_hand" : "idle", 0.15f);
            Sound.Play(Sfx.Cloth, 0.6f, b);
            // facing them as they walk away; his hands on the T-handle
            var toThem = hero.transform.position - b; toThem.y = 0f;
            lastMan.transform.rotation = Quaternion.Euler(0f, -Mathf.Atan2(toThem.z, toThem.x) * Mathf.Rad2Deg, 0f);
            System.Func<Vector3> grip = () => plungerHandle.position + Vector3.up * 0.4f;
            rigMan.L.To(grip, () => Vector3.down + Vector3.right * 0.3f, 3f);
            rigMan.R.To(grip, () => Vector3.down - Vector3.right * 0.3f, 3f);
            rigMan.nod = -6f;
        }

        // ================================================================ on screen

        void WalkoutGUI(float w, float h)
        {
            if (woT >= 0f && woFlash > 0.003f) Fill(new Rect(0, 0, w, h), new Color(1f, 0.99f, 0.97f, Mathf.Clamp01(woFlash) * 0.55f));
        }

        /// <summary>The take's last words, on the black (drawn over the fade).</summary>
        void WalkoutTextGUI(float w, float h)
        {
            if (woOverText > 0f)
            {
                float t0 = Time.unscaledTime - woOverText;
                float a = Mathf.Clamp01((t0 - 0.4f) / 1.2f) * (1f - Mathf.Clamp01((t0 - 3.6f) / 0.9f));
                if (a > 0.01f)
                {
                    cap.alignment = TextAnchor.MiddleCenter;
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.85f * a);
                    GUI.Label(new Rect(0, h * 0.5f - 18, w, 36), Spaced("OVER AND OUT"), cap);
                    Fill(new Rect(w * 0.5f - 24 * a, h * 0.5f + 24, 48 * a, 2), new Color(0.8f, 0.06f, 0.07f, a));
                    cap.alignment = TextAnchor.MiddleLeft;
                }
            }
        }
    }
}
