using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        float heroZ, mayaOff = -0.55f, mayaOffWant = -0.55f, mayaYaw = 90f, mayaYawWant = 90f, mayaHop;
        float swing, holdBack, plungerUp, woShake, woFlash, woOverText = -1f, heroHeight = 1.45f;
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
            while (woT < 61.2f)
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
                plungerBox.position = WoAt(149.3f, 0f, -16.6f);
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
                var pts = new[] { plungerBox.position + new Vector3(0.18f, 0.05f, 0.1f), WoAt(149.6f, 0.02f, -14.5f), WoAt(149.2f, 0.02f, -10.5f), WoAt(149.4f, 0.02f, -7.2f), WoAt(149.9f, 0.02f, -5.0f), WoAt(149.7f, 0.02f, -3.4f) };
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

        float WoSpeed(float t)
        {
            if (t < 4.3f) return 0.85f;
            if (t < 19.5f) return t > 12.4f && t < 14.2f ? 0.92f : 1f;
            if (t < 30f) return 1f - WoEase(19.5f, 20.2f, t);
            if (t < 36.4f) return WoEase(30f, 30.9f, t);
            if (t < 39f) return Mathf.Lerp(1f, 0.82f, WoEase(36.4f, 36.8f, t)) + 0.18f * WoEase(37.4f, 39f, t);
            return 1f;
        }

        void WoPlace()
        {
            var hp = WoAt(147.3f, 0f, heroZ);
            hero.CinePose(hp);
            hero.transform.rotation = Quaternion.Euler(0f, 90f, 0f);          // facing the camera (-z)
            if (maya != null)
            {
                maya.PuppetPose(WoAt(147.3f + mayaOff, mayaHop, heroZ + 0.04f));
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
                bool on = pipDead ? false : t > 40.4f ? Mathf.Repeat(Time.unscaledTime * 9f, 1f) < 0.4f && Random.value > 0.3f : Mathf.Repeat(Time.unscaledTime * 0.85f, 1f) < 0.16f;
                if (t > 41.3f) pipDead = true;
                mPip.GetComponent<Renderer>().enabled = on;
            }
            // the plunger's handle
            if (plungerHandle != null) plungerHandle.localPosition = new Vector3(0f, Mathf.Lerp(-0.18f, 0.12f, plungerUp), 0f);
            // the fires: keep roaring under it all
            if (t > 37.6f && Time.unscaledTime >= nextFire)
            {
                nextFire = Time.unscaledTime + 5.4f;
                Sound.Play(Sfx.FireRoar, t > 44f ? 0.32f : 0.5f, WoAt(147f, 6f, 0f));
            }
            // the drift of ash and embers round the lens
            if (woDrift != null && Camera.main != null) woDrift.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 3f;
            woShake = Mathf.MoveTowards(woShake * Mathf.Exp(-2.4f * Time.unscaledDeltaTime), 0f, Time.unscaledDeltaTime * 0.002f);
            woFlash *= Mathf.Exp(-5.5f * Time.unscaledDeltaTime);
            RibbonFlutter.Wind = Mathf.Lerp(RibbonFlutter.Wind, t > 36.3f ? (t > 44.5f ? 2.6f : 3.4f) : 1f, 1f - Mathf.Exp(-2f * Time.unscaledDeltaTime));
            WoCamera(t);
        }

        void WoCamera(float t)
        {
            var hp = hero.transform.position;
            float slow = WoEase(44.5f, 58.5f, t);
            float dist = 3.4f;
            dist = Mathf.Lerp(dist, 2.75f, WoEase(19.6f, 28.6f, t));
            dist = Mathf.Lerp(dist, 3.3f, WoEase(29.2f, 32.5f, t));
            dist = Mathf.Lerp(dist, 4.7f, slow);
            float camY = hp.y + heroHeight * 0.8f + 0.4f * slow;
            float lookY = hp.y + heroHeight * 0.7f + 1.7f * slow;            // tilting up: the burning hill over them
            float fov = Mathf.Lerp(36f, 44f, slow);
            // waiting at the porch until they come to it, then backing away in front of them
            float zStatic = -8.6f, zFollow = hp.z - dist;
            float h = Mathf.Max(0.9f - Mathf.Abs(zStatic - zFollow), 0f) / 0.9f;
            float camZ = Mathf.Min(zStatic, zFollow) - h * h * 0.9f * 0.25f;
            float arc = 0.55f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - 5.5f) / 7.5f));
            float camX = FortressPlan.DoorX + 0.05f + arc - 0.15f * slow;
            var pos = new Vector3(camX, camY, camZ);
            var subject = new Vector3(hp.x - 0.27f, lookY, hp.z + 0.6f);
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

        List<WoEvent> WoEvents()
        {
            var e = new List<WoEvent>();
            void At(float t, System.Action a) => e.Add(new WoEvent(t, a));

            At(0.3f, () => Sound.PlayTake(Sfx.Birds, 1, 0.35f));
            At(6.5f, () => Sound.PlayTake(Sfx.Birds, 0, 0.3f));
            At(14.0f, () => Sound.PlayTake(Sfx.Birds, 2, 0.25f));
            // ---- 19.5 she stops him; she raises her walkie
            At(19.6f, () => { rigH.nod = 14f; rigH.LookTowards(maya.transform.position, 25f); });
            At(19.8f, () => { mFree.To(() => WoMouth(rigM, maya.transform.localScale.y), () => Vector3.down + WoSide() * 0.4f, 3.5f); rigM.nod = -12f; rigM.LookTowards(hero.transform.position, 35f); });
            At(20.0f, () => { Sound.PlayTake(Sfx.WalkieClick, 1, 0.35f, maya.transform.position); Walkie("Papa? ...I can see you. Over.", 2.6f); });
            // ---- 21.2 the east range's door: the last man runs out
            At(21.2f, () =>
            {
                Sound.Play(Sfx.Door, 0.75f, WoAt(FortressPlan.RangeX0, 1f, FortressPlan.ManDoorZ));
                if (lastMan != null) { lastMan.gameObject.SetActive(true); lastMan.PuppetPlay("run", 0.05f, 1.15f); }
                StartCoroutine(WoManRun());
            });
            At(22.7f, () => mFree.To(() => WoChestFront(maya, 0.13f, -0.08f), () => Vector3.down + WoSide() * 0.5f, 3f));
            // ---- 22.9 his walkie from his coat
            At(22.9f, () => { if (hWalkie != null) hWalkie.gameObject.SetActive(true); hFree.To(() => WoMouth(rigH, 1f), () => Vector3.down - WoSide() * 0.5f, 3.2f); });
            At(23.3f, () => Sound.PlayTake(Sfx.WalkieClick, 2, 0.35f, hero.transform.position));
            At(23.4f, () => Say("YOU", "I see you too, sweetheart. Over.", 2.6f));
            At(24.6f, () => { Sound.PlayTake(Sfx.Plunger, 0, 0.7f, plungerBox.position); StartCoroutine(WoLerp(0.45f, u => plungerUp = u)); });
            // ---- 26.0 the man: Voss's last order. They both turn to look at him
            At(26.0f, () =>
            {
                hFree.Off(2.5f);
                Say("MAN", "Boss said... if he falls, the hill burns.", 2.7f, lastMan != null ? HeadOf(lastMan) : null);
                if (lastMan != null) { rigH.nod = 4f; rigH.LookTowards(lastMan.transform.position, 70f); rigM.nod = 0f; rigM.LookTowards(lastMan.transform.position, 75f); }
                mayaOffWant = -0.36f;
            });
            At(28.8f, () => { rigH.turn = 0f; rigH.nod = 3f; Say("YOU", "...Then burn it.", 1.9f); });
            At(29.6f, () => { rigM.turn = 0f; rigM.nod = 0f; mayaOffWant = -0.55f; });
            // ---- 31.4 "Can we go home now?"
            At(31.4f, () => { mFree.To(() => WoMouth(rigM, maya.transform.localScale.y), () => Vector3.down + WoSide() * 0.4f, 3.5f); rigM.nod = -8f; });
            At(31.5f, () => { Sound.PlayTake(Sfx.WalkieClick, 0, 0.35f, maya.transform.position); Walkie("Can we go home now? Over.", 2.6f); });
            At(34.1f, () => { mFree.To(() => WoChestFront(maya, 0.13f, -0.08f), () => Vector3.down + WoSide() * 0.5f, 3f); rigM.nod = 0f; });
            At(34.2f, () => hFree.To(() => WoMouth(rigH, 1f), () => Vector3.down - WoSide() * 0.5f, 3.4f));
            At(34.4f, () => Say("YOU", "Over and out.", 1.5f));
            At(35.6f, () => Sound.PlayTake(Sfx.WalkieClick, 0, 0.8f, hero.transform.position));
            At(35.9f, () => hFree.To(WoCoat, () => Vector3.down - WoSide() * 0.6f, 4f));
            At(36.0f, () => { Sound.PlayTake(Sfx.Plunger, 1, 0.55f, plungerBox.position); StartCoroutine(WoLerp(0.18f, u => plungerUp = 1f - u)); });
            At(36.25f, () => { if (hWalkie != null) hWalkie.gameObject.SetActive(false); hFree.Off(2.5f); });
            // ---- 36.3 the first blast: the great door they walked out of
            WoBlast(e, 36.3f, WoAt(FortressPlan.DoorX, 2.3f, -3.6f), 1.35f, Vector3.back, 0, 1f);
            At(36.45f, () =>
            {
                // she flinches and starts to turn; he lets go, his hand on the back of her head, draws her in
                rigM.nod = 18f; mayaYawWant = 25f;
                hNear.To(WoHerHead, () => -rigH.Forward + WoSide() * 0.6f, 5f);
                mNear.Off(6f);
                mayaOffWant = -0.3f;
            });
            At(36.75f, () => mNear.To(WoHisLeg, () => Vector3.down - rigM.Forward * 0.5f, 4f));
            At(37.9f, () => { mayaYawWant = 90f; rigM.nod = 8f; rigH.nod = 6f; });
            // ---- the chain, left to right along the facade, then up the hill
            float[] order = { 140.85f, 153.15f, 135.75f, 158.25f, 131.1f, 162.9f };
            for (int i = 0; i < order.Length; i++)
                WoBlast(e, 36.62f + i * 0.17f, WoAt(order[i], 3.6f, -3.9f), 0.85f, Vector3.back + Vector3.up * 0.3f, 1 + i % 2, 0.65f);
            WoBlast(e, 37.05f, WoAt(FortressPlan.DoorX, 8.4f, -5.9f), 0.95f, Vector3.back + Vector3.up, 2, 0.7f);
            At(37.4f, () => { Sound.Play(Sfx.Tinnitus, 0.28f); if (Camera.main != null) woDrift = BlastFx.Drift(Camera.main.transform); });
            WoBlast(e, 37.8f, WoAt(139.0f, FortressPlan.RoofY - FortressPlan.Up + 1.2f, 0.5f), 1.35f, Vector3.up, 1, 0.8f);
            WoBlast(e, 37.95f, WoAt(155.5f, FortressPlan.RoofY - FortressPlan.Up + 1.2f, 0.5f), 1.3f, Vector3.up, 2, 0.8f);
            WoBlast(e, 38.25f, WoAt(165.0f, 4.5f, -15.0f), 1.3f, Vector3.left + Vector3.up * 0.5f, 1, 0.85f);
            WoBlast(e, 38.85f, WoAt(FortressPlan.TowerX, FortressPlan.GalleryY - FortressPlan.Up + 2.5f, FortressPlan.TowerZ - 2f), 2.4f, Vector3.up + Vector3.back * 0.3f, 2, 1.2f);
            WoBlast(e, 39.6f, WoAt(115f, 20f, 19f), 2.0f, Vector3.up, 0, 0.9f);
            WoBlast(e, 40.25f, WoAt(124f, 7f, 2.5f), 1.4f, Vector3.up + Vector3.back * 0.4f, 1, 0.7f);
            WoBlast(e, 42.6f, WoAt(136.5f, 6.0f, -3.6f), 0.8f, Vector3.back, 2, 0.4f);
            // ---- 44.5 the slow motion: the sound stays clear; time sinks
            At(44.5f, () => { Sound.KeepClear = true; StartCoroutine(WoLerp(1.6f, u => { if (TimeController.Instance != null) TimeController.Instance.CineScale = Mathf.Lerp(1f, 0.32f, u); })); Sound.PlayTake(Sfx.Ember, 0, 0.5f); });
            WoBlast(e, 45.7f, WoAt(147.5f, FortressPlan.RoofY - FortressPlan.Up + 2.2f, -1.0f), 2.0f, Vector3.up + Vector3.back * 0.2f, 2, 0.5f, 0.55f);
            // ---- 47 she looks up at him; he looks down at her; he can stand easy
            At(47.0f, () => { rigM.nod = -38f; rigM.LookTowards(hero.transform.position, 30f); });
            At(47.5f, () => { rigH.nod = 24f; rigH.LookTowards(maya.transform.position, 20f); });
            At(48.4f, () => { rigH.chestNod = 8f; rigH.headRate = 1.6f; Sound.Play(Sfx.Breath, 0.22f, hero.transform.position, 0.9f); });
            At(49.2f, () => rigH.chestNod = 4f);
            At(49.6f, () =>
            {
                // she reaches up and takes his hand again
                mNear.To(WoHoldPoint, () => Vector3.down + WoSide() * 0.4f, 2.6f);
                hNear.To(WoHoldPoint, () => -rigH.Forward + Vector3.down * 0.3f, 2.2f);
                mayaOffWant = -0.52f;
                rigM.nod = -16f; rigH.nod = 10f;
            });
            At(50.4f, () => Sound.PlayTake(Sfx.Ember, 1, 0.45f));
            // ---- 52.6 two clicks on a dead walkie
            At(52.6f, () => { mFree.To(() => WoChestFront(maya, 0.2f, 0.06f), () => Vector3.down + WoSide() * 0.5f, 3f); rigM.nod = 10f; rigM.turn = 0f; });
            At(53.2f, () => Sound.PlayTake(Sfx.WalkieClick, 0, 0.9f, maya.transform.position));
            At(53.75f, () => Sound.PlayTake(Sfx.WalkieClick, 1, 0.9f, maya.transform.position));
            At(54.2f, () => { rigM.nod = -30f; rigM.LookTowards(hero.transform.position, 30f); });
            // ---- 55 and two back, from the walkie in his coat
            At(55.0f, () => hFree.To(WoCoat, () => Vector3.down - WoSide() * 0.6f, 3f));
            At(55.4f, () => Sound.PlayTake(Sfx.WalkieClick, 2, 0.75f, hero.transform.position, 0.96f));
            At(55.95f, () => Sound.PlayTake(Sfx.WalkieClick, 0, 0.75f, hero.transform.position, 0.96f));
            At(56.4f, () => { hFree.Off(2f); mFree.To(() => WoChestFront(maya, 0.13f, -0.08f), () => Vector3.down + WoSide() * 0.5f, 2f); });
            At(57.0f, () => { rigM.nod = -6f; rigM.turn = 0f; rigH.nod = 4f; rigH.turn = 0f; });
            At(58.6f, () => StartCoroutine(WoLerp(2.4f, u => blackout = u * u)));
            e.Sort((a, b) => a.t.CompareTo(b.t));
            return e;
        }

        /// <summary>A blast on the timeline: the fire, its sound (late by its distance, like a real one),
        /// the stones raining after, the flash and the shake.</summary>
        void WoBlast(List<WoEvent> e, float t, Vector3 at, float k, Vector3 outward, int take, float flash, float pitch = 1f)
        {
            e.Add(new WoEvent(t, () =>
            {
                BlastFx.Blast(at, k, outward, FortressPlan.Up);
                var c = Camera.main != null ? Camera.main.transform.position : at;
                float d = Vector3.Distance(c, at);
                float vol = Mathf.Clamp01(k * 0.75f / (1f + d / 45f) + 0.25f);
                if (pitch < 1f) Sound.PlayTake(Sfx.Explosion, take, vol * 0.8f, at, pitch);      // in the slow motion: deep, slow
                else Sound.PlayLater(Sfx.Explosion, d / 343f, vol, at);
                Sound.PlayLater(Sfx.Debris, d / 343f + 0.7f, vol * 0.7f, at);
                woFlash = Mathf.Max(woFlash, flash * Mathf.Clamp01(1.2f - d / 120f));
                woShake = Mathf.Min(0.16f, woShake + 0.07f * k / (1f + d / 25f));
            }));
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
            for (float t0 = 0f; t0 < 3.2f; t0 += Time.unscaledDeltaTime)
            {
                float u = t0 / 3.2f;
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
