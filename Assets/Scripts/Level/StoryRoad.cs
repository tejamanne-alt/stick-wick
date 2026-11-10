using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using SKey = UnityEngine.InputSystem.Key;
#endif

namespace ProjectInk
{
    /// <summary>CHAPTER 5 · THE ROAD (docs/story/story_v2.md): the drive up Kessel Hill in the Handler's
    /// car, the storm building, about four minutes, no fighting. Only him, and her voice on the walkie
    /// on the empty seat beside him: the hot chocolate, counting the thunder, "Are you a bad man?", the
    /// music box song sung wrong, the battery. Then the last hairpin, the floodlights, and the truth: with
    /// the book out of the box, Voss doesn't need him alive any more. It was never a trade.
    ///
    /// Camera: high on the headlights in the hairpins; inside, close on his hands on the wheel; the
    /// "two-shot of nobody" (him and the walkie on the empty seat, framed as if she were there); a slow
    /// push-in on his face; the windscreen filling with the fortress.</summary>
    public partial class StoryIntro
    {
        /// <summary>TEST: 0 the start, 1 the thunder, 2 "are you a bad man?", 3 the battery, 4 the last hairpin.</summary>
        public static int RoadFrom;

        Transform roadCar, carInterior, wiperL, wiperR, seatWalkie, batteryPip;
        float drive, driveSpeed = 1f / 210f, roadTitleAt = -1f, roadRain;
        bool driving;

        void BeginRoad()
        {
            phase = Phase.Road;
            HeadWound.Chapter = 3;
            AttachRibbon();
            roadCar = Find("RoadCar");
            carInterior = Find("CarInterior");
            wiperL = Find("WiperL");
            wiperR = Find("WiperR");
            // the storm, building: rain round the camera, far lightning
            FortProps();
            storm = true;
            nextStrike = Time.unscaledTime + 4f;
            Sound.Ambience(0.25f, 0.4f, 1800f);                    // inside a car: the wind, muffled; no rain yet
            Sound.Music("storm", 0.35f, true, 3f);
            // the walkie on the passenger seat
            seatWalkie = MakeWalkie("SeatWalkie");
            seatWalkie.position = RoadPlan.PassengerSeat + Vector3.up * 0.02f;
            seatWalkie.rotation = Quaternion.Euler(-70f, 90f, 0f);
            var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(pip.GetComponent<Collider>());
            pip.transform.SetParent(seatWalkie, false);
            pip.transform.localPosition = new Vector3(-0.02f, 0.13f, -0.02f);
            pip.transform.localScale = Vector3.one * 0.012f;
            pip.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.85f, 0.05f, 0.06f) };
            batteryPip = pip.transform;
            batteryPip.gameObject.SetActive(false);
            int from = RoadFrom;
            RoadFrom = 0;
            StartCoroutine(Road(from));
        }

        void SeatHero(bool talking)
        {
            hero.CinePuppet();
            hero.SetFacing(1);
            hero.CinePose(RoadPlan.DriverHead - new Vector3(0f, 0.86f, 0f));
            string clip = talking ? "sit_drive_talk" : "sit_drive";
            hero.body.Play(hero.body.Has(clip) ? clip : hero.body.Has("sit_bound") ? "sit_bound" : "idle", 0.25f);
            HoldWalkie(talking);
        }

        void UpdateRoad()
        {
            UpdateStorm();
            if (driving) drive = Mathf.Min(1f, drive + driveSpeed * Time.unscaledDeltaTime);
            if (roadCar != null)
            {
                roadCar.position = RoadPlan.Sample(drive * RoadPlan.PathLength, out var dir);
                roadCar.rotation = Quaternion.Slerp(roadCar.rotation, RoadPlan.CarRotation(dir), Time.unscaledDeltaTime * 4f);
            }
            // the wipers, slow, then faster as the rain gets heavier
            float wa = 0f;                                         // the wipers rest: no rain yet
            if (wiperL != null) wiperL.localRotation = Quaternion.AngleAxis(wa, RoadPlan.WiperAxis);
            if (wiperR != null) wiperR.localRotation = Quaternion.AngleAxis(wa, RoadPlan.WiperAxis);
            if (batteryPip != null && batteryPip.gameObject.activeSelf) batteryPip.GetComponent<Renderer>().enabled = Mathf.Repeat(Time.unscaledTime * 1.6f, 1f) < 0.5f;
        }

        // ---------------------------------------------------------------- the shots

        CineCam.Shot Hands() => Static(RoadPlan.DriverHead + new Vector3(0.55f, -0.35f, -0.35f), RoadPlan.DriverHead + new Vector3(0.45f, -0.55f, 0.05f), 34f);
        CineCam.Shot Nobody() => Static(RoadPlan.Windscreen + new Vector3(-0.15f, -0.2f, 0f), Vector3.Lerp(RoadPlan.DriverHead, RoadPlan.PassengerSeat + Vector3.up * 0.6f, 0.5f), 54f);
        CineCam.Shot OnWalkie() => Static(RoadPlan.PassengerSeat + new Vector3(0.45f, 0.35f, 0.1f), RoadPlan.PassengerSeat + Vector3.up * 0.05f, 30f);
        CineCam.Shot Face(float u) => CineCam.DollyIn(RoadPlan.DriverHead + Vector3.up * 0.08f, new Vector3(-1f, 0.02f, -0.15f), u, 1.1f, 0.65f, 32f);
        CineCam.Shot Pov() => Static(RoadPlan.DriverHead + new Vector3(0.05f, 0.02f, 0f), RoadPlan.OutsideAhead, 58f);

        IEnumerator Talk(string line, float secs, System.Func<CineCam.Shot> shot)
        {
            Say("YOU", line, secs);
            yield return Cut(secs + 0.2f, u => shot(), 0.006f);
        }

        IEnumerator Her(string line, float secs, System.Func<CineCam.Shot> shot)
        {
            Walkie(line, secs);
            yield return Cut(secs + 0.2f, u => shot(), 0.006f);
        }

        IEnumerator Road(int from)
        {
            barsPx = 92f;
            SeatHero(false);
            driving = true;
            if (from <= 0)
            {
                // ------------------------------------------------ 5.0 black: the title
                blackout = 1f;
                roadTitleAt = Time.unscaledTime;
                Sound.Play(Sfx.CarRev, 0.6f);
                yield return RealWait(3.6f);
                // ------------------------------------------------ 5.1 high: headlights climbing the hairpins, the first drops
                StartCoroutine(FadeIn(1.2f));
                if (carInterior != null) carInterior.gameObject.SetActive(false);
                hero.gameObject.SetActive(false);
                yield return Cut(5.5f, u => Static(roadCar != null ? roadCar.position + new Vector3(-14f, 16f, -12f) : RoadPlan.CarPath[0] + new Vector3(-14f, 16f, -12f),
                    roadCar != null ? roadCar.position : RoadPlan.CarPath[0], 30f), 0.02f);
                // ------------------------------------------------ 5.2 inside: his hands on the wheel, the ribbon on his wrist; the walkie crackles back to life
                if (carInterior != null) carInterior.gameObject.SetActive(true);
                hero.gameObject.SetActive(true);
                SeatHero(false);
                yield return Cut(3.0f, u => Hands(), 0.008f);
                Sound.Play(Sfx.Static, 0.5f, RoadPlan.PassengerSeat, 1.4f);
                yield return Cut(1.2f, u => OnWalkie(), 0.006f);
                yield return Her("...pa? ...Papa?", 2.0f, OnWalkie);
                SeatHero(true);
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(Face(0f))));
                yield return HoldTo("HOLD TO TALK", 1.0f, null);
                yield return Talk("I'm here, sweetheart. I'm driving to you.", 2.6f, () => Face(0.2f));
                // ------------------------------------------------ 5.3 the two-shot of nobody
                yield return Her("They put me in a big room with a window. There's a giant telescope.", 3.8f, Nobody);
                yield return Her("The man with the cane gave me hot chocolate.", 2.8f, Nobody);
                yield return Talk("Don't drink anything he gives you.", 2.2f, Nobody);
                yield return Cut(1.0f, u => Nobody(), 0.006f);
                yield return Her("...I already drank it. It was nice.", 2.6f, OnWalkie);
                yield return Cut(2.0f, u => Face(0.3f), 0.006f);          // he almost smiles
                SeatHero(false);
            }
            if (from <= 1)
            {
                if (from == 1) { yield return FadeIn(0.5f); drive = 0.3f; }
                // ------------------------------------------------ 5.4 the thunder: count with her
                roadRain = 0.4f;
                yield return Her("I don't like the thunder.", 2.2f, OnWalkie);
                SeatHero(true);
                yield return Talk("Count with me. When the light comes, count. The thunder comes after.", 3.6f, () => Face(0.35f));
                yield return Talk("The bigger the number, the further away it is.", 2.8f, () => Face(0.4f));
                SeatHero(false);
                int[] counts = { 4, 3, 2 };
                for (int r = 0; r < counts.Length; r++)
                {
                    yield return Cut(Random.Range(1.2f, 2.2f), u => Pov(), 0.006f);
                    Lightning(r == 2 ? 0.9f : 0.5f, 0.01f, 99f);              // the flash only: the thunder comes when she gets there
                    if (LightningSwap.Instance != null) LightningSwap.Instance.Bolt(RoadPlan.OutsideAhead + new Vector3(Random.Range(-30f, 30f), 60f, 40f), RoadPlan.OutsideAhead + new Vector3(Random.Range(-20f, 20f), 0f, 40f), 0.5f);
                    // the player marks the flash
                    StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(Pov())));
                    yield return SwipeTo("THE FLASH: TAP", 0, 2.2f);
                    for (int c = 1; c <= counts[r]; c++)
                    {
                        Walkie(c == 1 ? "One..." : c == 2 ? "Two..." : c == 3 ? "Three..." : "Four...", 0.9f);
                        yield return Cut(0.95f, u => Nobody(), 0.006f);
                    }
                    Sound.Play(Sfx.Thunder, 0.5f + r * 0.25f);
                    if (cam != null) cam.Shake(0.05f + r * 0.05f, 0.4f);
                    if (r == 0) yield return Her("It's far away!", 1.8f, OnWalkie);
                    if (r == 1) yield return Her("...It's getting closer, Papa.", 2.4f, OnWalkie);
                    if (r == 2) { roadRain = 0.8f; yield return Her("Two! It's right on top of us!", 2.4f, OnWalkie); }
                }
                yield return Talk("That's where I'm going. Right under it.", 2.6f, () => Face(0.5f));
            }
            if (from <= 2)
            {
                if (from == 2) { yield return FadeIn(0.5f); drive = 0.5f; roadRain = 0.8f; }
                // ------------------------------------------------ 5.5 "are you a bad man?"
                yield return Cut(1.4f, u => Face(u * 0.2f + 0.5f), 0.006f);
                yield return Her("Papa? The cane man says you used to be like him.", 3.0f, Nobody);
                yield return Her("Are you a bad man?", 2.2f, OnWalkie);
                SeatHero(true);
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(Face(0.7f))));
                yield return ChooseFrom("\"I was.\"", "\"Not any more.\"", "\"I don't know, sweetheart.\"");
                if (chosen == 0)
                {
                    yield return Talk("I was.", 1.8f, () => Face(0.75f));
                    yield return Her("Mama says people can change. Like caterpillars.", 3.2f, OnWalkie);
                }
                else if (chosen == 1)
                {
                    yield return Talk("Not any more.", 1.8f, () => Face(0.75f));
                    yield return Her("Good. Because I need a good one.", 2.6f, OnWalkie);
                }
                else
                {
                    yield return Talk("I don't know, sweetheart.", 2.0f, () => Face(0.75f));
                    yield return Her("I know. You're my papa.", 2.4f, OnWalkie);
                }
                SeatHero(false);
                yield return Cut(1.6f, u => Nobody(), 0.006f);
                // ------------------------------------------------ 5.6 the music box song: he hums it, badly
                yield return Her("Can you sing the music box song? Mama sings it when it's dark.", 3.4f, OnWalkie);
                SeatHero(true);
                int note = 0;
                int[] wrong = { 0, 4, 7, 5, 3, 9, 2 };
                float nextNote = 0f;
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(Face(0.8f))));
                yield return HoldTo("HOLD TO HUM", 4.0f, p =>
                {
                    if (Time.unscaledTime >= nextNote && (PointerDown() || KeyHeld(SKey.E, SKey.Space)))
                    {
                        nextNote = Time.unscaledTime + 0.55f;
                        Sound.Play(Sfx.MusicNote, 0.35f, null, Mathf.Pow(2f, wrong[note++ % wrong.Length] / 12f) * 0.7f);
                    }
                });
                yield return Her("Hee hee... That's wrong.", 2.2f, OnWalkie);
                yield return Talk("I'll practise.", 1.8f, () => Face(0.85f));
                SeatHero(false);
            }
            if (from <= 3)
            {
                if (from == 3) { yield return FadeIn(0.5f); drive = 0.75f; roadRain = 0.9f; }
                // ------------------------------------------------ 5.7 the battery
                if (batteryPip != null) batteryPip.gameObject.SetActive(true);
                yield return Cut(1.6f, u => OnWalkie(), 0.006f);
                yield return Her("Papa, the light is blinking.", 2.2f, OnWalkie);
                SeatHero(true);
                yield return Talk("That's the battery. Turn it off now, and keep it hidden.", 3.0f, () => Face(0.85f));
                yield return Talk("Turn it on only when you see me. Promise?", 2.6f, () => Face(0.9f));
                yield return Her("Promise. Over and out.", 2.2f, OnWalkie);
                Sound.Play(Sfx.PhoneClick, 0.6f, RoadPlan.PassengerSeat);
                if (batteryPip != null) batteryPip.gameObject.SetActive(false);
                SeatHero(false);
                yield return Cut(3.0f, u => Nobody(), 0.004f);           // silence; the wipers
            }
            // ------------------------------------------------ 5.8 the last hairpin: the fortress fills the windscreen; the floodlights; the truth
            if (from == 4) { yield return FadeIn(0.5f); roadRain = 1f; }
            drive = Mathf.Max(drive, 0.92f);
            roadRain = 1f;
            Strike(1f, 0.2f);
            Sound.Music("storm", 0.55f, true, 1.5f);
            yield return Cut(3.0f, u => Static(RoadPlan.DriverHead + new Vector3(-0.35f, 0.05f, 0.1f), RoadPlan.Fortress, Mathf.Lerp(56f, 30f, u)), 0.01f);
            drive = 1f;
            driving = false;
            Sound.Play(Sfx.Heartbeat, 0.8f);
            yield return Cut(2.4f, u => Face(1f), 0.006f);
            Say("YOU", "...Floodlights on every wall. Men on every wall.", 3.0f);
            yield return Cut(3.0f, u => Static(RoadPlan.Windscreen + new Vector3(0.6f, 0.1f, 0f), RoadPlan.Fortress, 38f), 0.01f);
            Say("YOU", "The book's out of the box. He doesn't need me alive any more.", 3.4f);
            yield return Cut(3.4f, u => Face(1f), 0.006f);
            Say("YOU", "It was never a trade.", 2.4f);
            Strike(0.9f, 0.1f);
            yield return Cut(2.6f, u => Face(1f), 0.006f);
            // ------------------------------------------------ 5.9 the walkie on the empty seat; then he gets out into the rain
            yield return Cut(2.0f, u => OnWalkie(), 0.006f);
            Say("YOU", "Then I'll go through all of them.", 2.4f);
            yield return Cut(2.6f, u => Nobody(), 0.006f);
            Sound.Play(Sfx.CarDoor, 0.9f, RoadPlan.DriverHead);
            Sound.Ambience(0.4f, 0.25f, 22000f);                   // out into the wind (no rain yet)
            if (seatWalkie != null) seatWalkie.gameObject.SetActive(false);
            Say("YOU", "...Over and out.", 1.8f);
            if (carInterior != null) carInterior.gameObject.SetActive(false);
            hero.CinePose(RoadPlan.GetOutSpot);
            hero.body.Play("walk", 0.2f, true, 0.8f);
            yield return Cut(5.0f, u => Static(RoadPlan.CarStop + new Vector3(-6f, 3f, -9f), RoadPlan.Fortress, Mathf.Lerp(40f, 32f, u)), 0.03f);
            cardAt = Time.unscaledTime;
            cardLine = "It was never a trade.";
            cardTitle = "OVER AND OUT";
            yield return RealWait(6.0f);
            CineOff();
            phase = Phase.End;
        }

        // ---------------------------------------------------------------- on screen

        void RoadGUI(float w, float h)
        {
            // no rain on the windscreen: the storm is dry until the roof
            if (roadTitleAt > 0f)
            {
                float t0 = Time.unscaledTime - roadTitleAt;
                float a = Mathf.Clamp01(t0 / 0.8f) * Mathf.Clamp01((3.6f - t0) / 0.8f);
                if (a > 0f)
                {
                    cap.alignment = TextAnchor.MiddleCenter;
                    cap.fontSize = 22;
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f * a);
                    GUI.Label(new Rect(0, h * 0.38f, w, 30), Spaced("CHAPTER 5"), cap);
                    int bs = big.fontSize;
                    big.fontSize = 84;
                    big.normal.textColor = new Color(1f, 1f, 1f, a);
                    GUI.Label(new Rect(0, h * 0.38f + 30, w, 96), Spaced("THE ROAD"), big);
                    big.fontSize = bs;
                    Fill(new Rect(w * 0.5f - 110 * a, h * 0.38f + 128, 220 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
                    cap.alignment = TextAnchor.MiddleLeft;
                    cap.fontSize = 20;
                }
            }
            PromptsGUI(w, h);
        }
    }
}
