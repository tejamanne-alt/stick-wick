using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>CHAPTER 2: THE BANK (docs/story/chapter2_bank.md), shot by shot. The morning after:
    /// the key's mark is carved over MERIDIAN TRUST's doors. He walks in (you walk him), slides the
    /// key under the glass; the clerk's screen flags the box; Harrow takes him down through the gate
    /// and the round door; two keys turn; box 417. While he is alone, upstairs the clerk makes a
    /// call. In the box (you open it): three passports with three names and one face, a ring (his
    /// wedding comes back, and her: Elena), an envelope in her hand with cash and an address. He
    /// walks out into the white morning, and across the road a black car's door opens.</summary>
    public partial class StoryIntro
    {
        int chapter = 1;

        // the bank's plan (BankBuilder / build_bank_set.py)
        const float BankDoorX = -0.4f, TellerX = 16.1f, ClerkZ = 3.25f, GateX = 28.5f, VaultX = 33f, TableX = 39.5f, TableY = 0.93f, TableZ = 0.75f;
        static readonly Vector3 Box417 = new Vector3(37.25f, 1.45f, 2.72f);

        Enemy clerk, harrow, guard, waitA, waitB;
        Transform depositBox, boxLid, pouch, ringOnHand, phoneProp, carDoor;
        string tapTitle, tapHow;
        float tapAt = -1f;
        string walkTitle;
        float walkAlpha;
        int bankUi;                        // 0 none, 1 the clerk's screen, 2 the passports, 3 the ring, 4 the memory, 5 the envelope, 6 the title
        float bankUiAt;
        string titleBig, titleSmall;
        Texture2D[] memories;
        Font hand;

        void BeginBank()
        {
            phase = Phase.Bank;
            StartCoroutine(Bank());
        }

        IEnumerator Bank()
        {
            hero.CinePuppet();
            hero.SetFacing(1);
            hero.CinePose(new Vector3(-22f, 0f, 0f));
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0f);
            barsPx = 92f;
            SpawnBankPeople();
            BuildBankProps();

            // ------------------------------------------------------------ B1 the street at dawn
            Sound.Play(Sfx.Wind, 0.5f);
            ShowTitle("CHAPTER 2", "THE BANK");
            float t1 = 0f;
            while (t1 < 4.6f)
            {
                t1 += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t1 / 4.6f);
                EndShot(Vector3.Lerp(new Vector3(-31f, 9.5f, -23f), new Vector3(-27f, 7.5f, -18f), k), new Vector3(-6f, 3f, 4f), 46f);
                if (t1 > 2.2f && t1 - Time.unscaledDeltaTime <= 2.2f) Sound.Play(Sfx.Rotor, 0.12f, null, 0.5f);      // a far-off engine
                yield return null;
            }
            bankUi = 0;

            // ------------------------------------------------------------ B2 the match cut: the key, then the same mark in stone
            ShowKey(true);
            Sound.Play(Sfx.Drone, 0.4f);
            yield return Hold(2.4f, () => EndShot(HeadC() + new Vector3(0.4f, -0.4f, -1.0f), HeadC() + Vector3.down * 0.5f, 30f));
            ShowKey(false);
            Sound.Play(Sfx.FlashCut, 0.4f);
            float t2 = 0f;
            while (t2 < 3.0f)
            {
                t2 += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t2 / 3f);
                EndShot(Vector3.Lerp(new Vector3(-7f, 4.6f, 0.4f), new Vector3(-5.2f, 5.0f, 0.2f), k), new Vector3(0f, 5.3f, 0f), 34f);
                yield return null;
            }

            // ------------------------------------------------------------ B3 down the street: nine o'clock, the doors open
            Bell(3);
            if (guard != null) { guard.PuppetPose(new Vector3(-0.9f, 0f, 0.9f)); guard.SetFacing(-1); }
            Sound.PlayLater(Sfx.Latch, 1.6f, 0.8f, new Vector3(BankDoorX, 1f, 0f));
            yield return Hold(3.2f, () => EndShot(new Vector3(-23.2f, 1.5f, -2.4f), new Vector3(-6f, 2.4f, 0.8f), 40f));

            // ------------------------------------------------------------ B4 play: walk to the doors
            if (guard != null) { guard.PuppetPose(new Vector3(1.8f, 0f, 1.4f)); guard.SetFacing(-1); }
            yield return PlayerWalk(-1.2f, StreetLo(), 2.5f, Application.isMobilePlatform ? "HOLD THE RIGHT SIDE TO WALK" : "HOLD  D  OR  RIGHT ARROW  TO WALK");
            // through the revolving door: the street's air gives way to the hush of marble
            Sound.Play(Sfx.Door, 0.6f, new Vector3(0f, 1f, 0f), 0.8f);
            Sound.Play(Sfx.WhooshBig, 0.25f, null, 0.6f);
            Sound.Ambience(0.03f, 0.75f, 5000f);
            hero.CinePuppet();
            yield return PuppetWalk(2.2f, 1.6f, () => EndShot(new Vector3(3.6f, 1.7f, -1.4f), hero.transform.position + Vector3.up * 1.2f, 40f));

            // ------------------------------------------------------------ B5 the lobby, wide; the guard watches him pass
            if (guard != null) guard.FaceTowards(hero.transform.position.x);
            Sound.Play(Sfx.Footstep, 0.4f, null, 1.1f);
            yield return Hold(2.8f, () =>
            {
                if (guard != null) guard.FaceTowards(hero.transform.position.x);
                float k = Mathf.Clamp01(Time.unscaledDeltaTime);
                EndShot(new Vector3(2.0f, 2.2f, -1.2f), new Vector3(16f, 3.4f, 8f), 54f);
            });

            // ------------------------------------------------------------ B6 play: to the teller's window
            yield return PlayerWalk(TellerX - 0.3f, 0.6f, 2.4f, Application.isMobilePlatform ? "HOLD THE RIGHT SIDE: TO THE COUNTER" : "HOLD  D  OR  RIGHT ARROW: TO THE COUNTER");
            hero.CinePuppet();
            hero.SetFacing(1);
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f);

            // ------------------------------------------------------------ B7 through the glass
            System.Action overShoulder = () => EndShot(new Vector3(TellerX - 0.9f, 1.75f, -0.9f), Head(clerk, new Vector3(TellerX, 1.5f, ClerkZ)), 32f);
            Say("CLERK", "Good morning. How can I help you?", 2.4f, HeadT(clerk));
            yield return Hold(2.6f, overShoulder);
            Sound.Play(Sfx.KnifeGrab, 0.35f, null, 1.5f);                  // the key, slid under the glass
            yield return Hold(0.6f, overShoulder);
            Say("YOU", "Box four-seventeen.", 1.8f);
            yield return Hold(2.0f, overShoulder);

            // ------------------------------------------------------------ B8 her screen (only we see it)
            bankUi = 1; bankUiAt = Time.unscaledTime;
            Sound.Play(Sfx.Glint, 0.5f, null, 1.6f);
            Sound.Play(Sfx.Heartbeat, 0.35f);
            yield return Hold(2.8f, () => EndShot(new Vector3(TellerX + 0.2f, 1.45f, ClerkZ + 0.9f), new Vector3(TellerX - 0.2f, 1.3f, ClerkZ - 0.2f), 30f));
            bankUi = 0;

            // ------------------------------------------------------------ B9 her smile, a beat too bright
            Say("CLERK", "Of course. Mr. Harrow will take you down.", 2.6f, HeadT(clerk));
            yield return Hold(2.8f, () => EndShot(Head(clerk, new Vector3(TellerX, 1.5f, ClerkZ)) + new Vector3(-0.2f, 0.05f, -1.4f), Head(clerk, new Vector3(TellerX, 1.5f, ClerkZ)), 28f));

            // ------------------------------------------------------------ B10 Harrow; the corridor; the gate; the round door
            if (harrow != null)
            {
                harrow.PuppetPose(new Vector3(24.5f, 0f, 0.3f));
                harrow.SetFacing(-1);
                harrow.PuppetPlay("walk", 0.1f, 0.9f);
                float th = 0f;
                while (th < 2.6f)
                {
                    th += Time.unscaledDeltaTime;
                    harrow.PuppetPose(new Vector3(Mathf.Lerp(24.5f, TellerX + 1.4f, Mathf.SmoothStep(0f, 1f, th / 2.4f)), 0f, 0.3f));
                    EndShot(new Vector3(TellerX + 0.6f, 1.6f, -3.6f), new Vector3(TellerX + 1.6f, 1.3f, 0.3f), 38f);
                    yield return null;
                }
                harrow.PuppetPlay("idle", 0.2f);
            }
            Say("HARROW", "This way, sir.", 1.6f, HeadT(harrow));
            yield return Hold(1.6f, () => EndShot(new Vector3(TellerX + 0.6f, 1.6f, -3.6f), new Vector3(TellerX + 1.0f, 1.3f, 0.3f), 38f));
            // both walk: Harrow a step ahead
            if (harrow != null) { harrow.SetFacing(1); harrow.PuppetPlay("walk", 0.1f, 0.9f); }
            hero.SetFacing(1);
            if (hero.body.Has("walk")) hero.body.Play("walk", 0.2f, true, 0.8f);
            float tw = 0f, hx0 = hero.transform.position.x;
            bool buzzed = false, spoke = false;
            const float WalkDur = 9.5f;
            float endX = TableX - 1.2f;
            while (tw < WalkDur)
            {
                tw += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, tw / WalkDur);
                float x = Mathf.Lerp(hx0, endX, k);
                hero.CinePose(new Vector3(x, 0f, 0f));
                if (harrow != null) harrow.PuppetPose(new Vector3(x + 1.3f, 0f, 0.35f));
                if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.55f; Sound.Play(Sfx.Footstep, 0.45f, hero.transform.position, 1.05f); }
                if (!buzzed && x > GateX - 3.5f) { buzzed = true; Sound.Play(Sfx.Static, 0.25f, new Vector3(GateX, 1f, 0f), 1.4f); Sound.Play(Sfx.Latch, 0.7f, new Vector3(GateX, 1f, 0f)); }
                if (!spoke && x > GateX - 1.5f) { spoke = true; Say("HARROW", "We haven't seen you in a long time, sir.", 2.8f, HeadT(harrow)); Sound.Play(Sfx.Drone, 0.25f); }
                // tracking alongside, then on through the round door
                var cp = new Vector3(x + 0.8f, 1.55f, -2.9f);
                EndShot(cp, new Vector3(x + 1.6f, 1.3f, 0.6f), x > VaultX - 2f ? 46f : 40f);
                yield return null;
            }
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f);

            // ------------------------------------------------------------ B11 two keys; the long box comes out
            if (harrow != null)
            {
                harrow.PuppetPlay("walk", 0.1f, 0.8f);
                float tb = 0f;
                var from = harrow.transform.position;
                while (tb < 1.2f)
                {
                    tb += Time.unscaledDeltaTime;
                    harrow.PuppetPose(Vector3.Lerp(from, new Vector3(Box417.x + 0.5f, 0f, 1.9f), Mathf.SmoothStep(0f, 1f, tb / 1.2f)));
                    EndShot(new Vector3(Box417.x - 0.2f, 1.6f, -0.6f), new Vector3(Box417.x, 1.4f, 2.6f), 42f);
                    yield return null;
                }
                harrow.SetFacing(-1);
                harrow.PuppetPlay("idle", 0.2f);
            }
            System.Action keysShot = () => EndShot(Box417 + new Vector3(-0.35f, 0.08f, -0.75f), Box417, 26f);
            yield return Hold(0.6f, keysShot);
            Sound.Play(Sfx.Latch, 0.7f, Box417, 1.5f);                     // the bank's key
            yield return Hold(0.6f, keysShot);
            Sound.Play(Sfx.Latch, 0.8f, Box417, 1.35f);                    // and his
            yield return Hold(0.5f, keysShot);
            // the box slides out of the wall and is set on the table
            Sound.Play(Sfx.KnifeGrab, 0.6f, Box417, 0.7f);
            float ts = 0f;
            var tableAt = new Vector3(TableX, TableY + 0.13f, TableZ);
            while (ts < 2.0f)
            {
                ts += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, ts / 2f);
                var p = k < 0.45f ? Vector3.Lerp(Box417 + Vector3.back * 0.1f, Box417 + Vector3.back * 0.75f, k / 0.45f)
                                  : Vector3.Lerp(Box417 + Vector3.back * 0.75f, tableAt, (k - 0.45f) / 0.55f);
                if (depositBox != null) depositBox.position = p;
                EndShot(new Vector3(TableX - 1.6f, 1.8f, -1.6f), Vector3.Lerp(Box417, tableAt, k), 40f);
                yield return null;
            }
            Sound.Play(Sfx.Crate, 0.35f, tableAt, 1.4f);
            Say("HARROW", "Take all the time you need.", 2.2f, HeadT(harrow));
            yield return Hold(2.2f, () => EndShot(new Vector3(TableX - 1.6f, 1.8f, -1.6f), tableAt + Vector3.up * 0.3f, 40f));
            if (harrow != null)
            {
                harrow.SetFacing(-1);
                harrow.PuppetPlay("walk", 0.1f, 0.9f);
                var from = harrow.transform.position;
                float tl = 0f;
                while (tl < 1.8f)
                {
                    tl += Time.unscaledDeltaTime;
                    harrow.PuppetPose(Vector3.Lerp(from, new Vector3(VaultX - 1.5f, 0f, 0.4f), tl / 1.8f));
                    EndShot(new Vector3(TableX + 1.4f, 1.6f, -1.8f), new Vector3(VaultX + 1f, 1.4f, 0.4f), 44f);
                    yield return null;
                }
                harrow.gameObject.SetActive(false);
            }
            Sound.Play(Sfx.Door, 0.5f, new Vector3(VaultX, 1f, 0f), 0.7f);

            // ------------------------------------------------------------ B12 meanwhile, upstairs: a call
            if (clerk != null)
            {
                clerk.SetFacing(1);
                AttachPhone(clerk);
            }
            Say("CLERK", "He's here. Box four-seventeen.", 2.6f, HeadT(clerk));
            yield return Hold(3.0f, () => EndShot(Head(clerk, new Vector3(TellerX, 1.5f, ClerkZ)) + new Vector3(1.3f, 0.1f, -1.6f), Head(clerk, new Vector3(TellerX, 1.5f, ClerkZ)), 30f));
            if (phoneProp != null) phoneProp.gameObject.SetActive(false);

            // ------------------------------------------------------------ B13 alone. Play: open it
            hero.CinePose(new Vector3(TableX - 0.85f, 0f, 0f));
            hero.SetFacing(1);
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.2f);
            Sound.Ambience(0.0f, 0.4f, 3000f);
            System.Action boxShot = () => EndShot(new Vector3(TableX - 0.55f, 1.85f, TableZ - 0.85f), tableAt + new Vector3(0f, 0.05f, 0.05f), 34f);
            yield return WaitTap("OPEN THE BOX", "TAP", boxShot);
            Sound.Play(Sfx.Latch, 0.6f, tableAt, 1.2f);
            float to = 0f;
            while (to < 0.8f)
            {
                to += Time.unscaledDeltaTime;
                if (boxLid != null) boxLid.localRotation = Quaternion.Euler(-105f * Mathf.SmoothStep(0f, 1f, to / 0.8f), 0f, 0f);
                boxShot();
                yield return null;
            }
            yield return Hold(1.0f, boxShot);

            // ------------------------------------------------------------ B14 the passports: three names, one face
            bankUi = 2; bankUiAt = Time.unscaledTime;
            Sound.Play(Sfx.Cloth, 0.5f, null, 1.4f);
            yield return Hold(3.6f, boxShot);
            bankUi = 0;
            Sound.Play(Sfx.Cloth, 0.4f, null, 1.2f);

            // ------------------------------------------------------------ B15 play: the pouch; a ring
            yield return WaitTap("THE POUCH", "TAP", boxShot);
            if (pouch != null) pouch.gameObject.SetActive(false);
            Sound.Play(Sfx.Glint, 0.8f);
            bankUi = 3; bankUiAt = Time.unscaledTime;
            yield return Hold(2.8f, boxShot);

            // ------------------------------------------------------------ B16 the memory: their wedding, and her
            bankUi = 4; bankUiAt = Time.unscaledTime;
            Sound.Ambience(0f, 0.2f, 1500f);
            Sound.Play(Sfx.Piano, 0.7f);
            float tm = 0f;
            bool l1 = false, l2 = false, p2 = false;
            while (tm < MemorySeconds)
            {
                tm += Time.unscaledDeltaTime;
                if (!p2 && tm > 5.5f) { p2 = true; Sound.Play(Sfx.Piano, 0.55f, null, 0.89f); }
                if (!l1 && tm > MemorySeconds - 6.4f) { l1 = true; Say("HER", "Hey... look at me. You're here now.", 2.8f); }
                if (!l2 && tm > MemorySeconds - 3.4f) { l2 = true; Say("HER", "Promise me you'll always come home.", 3.0f); }
                boxShot();
                yield return null;
            }
            bankUi = 0;

            // ------------------------------------------------------------ B17 her name
            Sound.Ambience(0.0f, 0.35f, 3000f);
            Sound.Play(Sfx.Breath, 0.6f, null, 0.9f);
            Say("YOU", "...Elena.", 2.6f);
            yield return Hold(2.8f, () => EndShot(HeadC() + new Vector3(0.35f, 0.02f, -0.75f), HeadC(), 28f));

            // ------------------------------------------------------------ B18 play: put it on
            yield return WaitTap("PUT IT ON", "TAP", () => EndShot(HeadC() + new Vector3(0.35f, 0.02f, -0.75f), HeadC(), 28f));
            WearRing();
            Sound.Play(Sfx.Glint, 0.5f, null, 0.8f);
            var handL = hero.body != null ? hero.body.Bone("hand.L") : null;
            yield return Hold(2.2f, () =>
            {
                var hp = handL != null ? handL.position : hero.transform.position + Vector3.up;
                EndShot(hp + new Vector3(0.1f, 0.15f, -0.55f), hp, 24f);
            });

            // ------------------------------------------------------------ B19 play: the envelope; an address in her hand
            yield return WaitTap("THE ENVELOPE", "TAP", boxShot);
            Sound.Play(Sfx.Cloth, 0.7f, null, 1.5f);
            bankUi = 5; bankUiAt = Time.unscaledTime;
            yield return Hold(2.6f, boxShot);
            Say("YOU", "...Home.", 2.0f);
            yield return Hold(2.6f, boxShot);
            bankUi = 0;
            Sound.Play(Sfx.Cloth, 0.6f);

            // ------------------------------------------------------------ B20 play: back out through the lobby
            depositBox.gameObject.SetActive(false);
            hero.CinePose(new Vector3(21.5f, 0f, 0f));
            hero.SetFacing(-1);
            Sound.Ambience(0.03f, 0.75f, 5000f);
            if (clerk != null) clerk.SetFacing(1);                      // she won't meet his eyes
            if (guard != null) { guard.PuppetPose(new Vector3(3.2f, 0f, 1.3f)); guard.SetFacing(1); }
            Sound.Play(Sfx.Heartbeat, 0.35f);
            yield return PlayerWalk(-1.2f, 1.0f, 2.4f, Application.isMobilePlatform ? "HOLD THE LEFT SIDE TO WALK OUT" : "HOLD  A  OR  LEFT ARROW  TO WALK OUT", -1);

            // ------------------------------------------------------------ B21 outside: the white morning; across the road, a door opens
            hero.CinePuppet();
            Sound.Play(Sfx.Door, 0.6f, new Vector3(0f, 1f, 0f), 0.8f);
            Sound.Ambience(0.12f, 0.55f, 22000f);
            if (hero.body.Has("walk")) hero.body.Play("walk", 0.2f, true, 0.75f);
            float tx = 0f;
            var exit0 = hero.transform.position;
            bool engine = false, door = false;
            while (tx < 5.0f)
            {
                tx += Time.unscaledDeltaTime;
                hero.CinePose(Vector3.Lerp(exit0, new Vector3(-6f, 0f, -0.6f), Mathf.SmoothStep(0f, 1f, tx / 4.5f)));
                if (!engine && tx > 1.0f) { engine = true; Sound.Play(Sfx.Rotor, 0.25f, new Vector3(-6f, 0.6f, -26f), 0.35f); }
                if (!door && tx > 2.6f) { door = true; Sound.Play(Sfx.Door, 0.5f, new Vector3(-6f, 1f, -26f), 1.2f); }
                if (carDoor != null && tx > 2.6f) carDoor.localRotation = Quaternion.Euler(0f, -55f * Mathf.SmoothStep(0f, 1f, (tx - 2.6f) / 1.0f), 0f);
                EndShot(new Vector3(-1.4f, 1.55f, 1.2f), Vector3.Lerp(hero.transform.position + Vector3.up * 1.2f, new Vector3(-6f, 1.0f, -26f), Mathf.SmoothStep(0f, 1f, (tx - 1.8f) / 2f)), 38f);
                yield return null;
            }
            yield return Hold(2.0f, () => EndShot(new Vector3(-1.4f, 1.55f, 1.2f), new Vector3(-6f, 1.0f, -26f), 30f));

            // ------------------------------------------------------------ B22 black: chapter complete
            cardAt = Time.unscaledTime;
            cardLine = "Her name was Elena. She was waiting for you.";
            cardTitle = "THE RING";
            yield return RealWait(6.0f);
            phase = Phase.End;
        }

        // ---------------------------------------------------------------- the people of the bank

        void SpawnBankPeople()
        {
            if (director == null) return;
            Enemy Person(Vector3 at, int facing, Color c, string clip)
            {
                var e = director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
                if (e == null) return null;
                e.Puppet();
                e.Passive = true;
                e.PuppetPose(at);
                e.SetFacing(facing);
                e.SetBaseColor(c);
                e.PuppetPlay(clip, 0f);
                return e;
            }
            // civilians are greys (only fighters carry colour in this world)
            clerk = Person(new Vector3(TellerX, 0f, ClerkZ), -1, new Color(0.42f, 0.42f, 0.43f), "idle_loose");
            harrow = Person(new Vector3(24.5f, 0f, 0.3f), -1, new Color(0.26f, 0.26f, 0.27f), "idle_loose");
            guard = Person(new Vector3(-0.9f, 0f, 3.5f), -1, new Color(0.1f, 0.1f, 0.11f), "idle");
            waitA = Person(new Vector3(3.0f, 0f, 7.6f), 1, new Color(0.55f, 0.55f, 0.56f), "idle_loose");
            waitB = Person(new Vector3(8.6f, 0f, 7.7f), -1, new Color(0.34f, 0.34f, 0.35f), "idle_loose");
        }

        static Transform HeadT(Enemy e) => e != null && e.body != null ? (e.body.Bone("head") ?? e.transform) : null;

        static Vector3 Head(Enemy e, Vector3 fallback)
        {
            var h = HeadT(e);
            return h != null ? h.position + h.up * 0.2f : fallback;
        }

        void AttachPhone(Enemy e)
        {
            var h = HeadT(e);
            if (h == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "DeskPhone";
            phoneProp = go.transform;
            phoneProp.SetParent(h, false);
            phoneProp.localPosition = new Vector3(0.12f, 0.05f, 0f);
            phoneProp.localScale = new Vector3(0.05f, 0.2f, 0.06f) / Mathf.Max(0.01f, h.lossyScale.x);
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.06f, 0.06f, 0.07f) };
        }

        // ---------------------------------------------------------------- the props

        void BuildBankProps()
        {
            var unlit = Shader.Find("Unlit/Color");
            Transform Part(Transform parent, string n, Vector3 lp, Vector3 ls, Color c)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(g.GetComponent<Collider>());
                g.name = n;
                g.transform.SetParent(parent, false);
                g.transform.localPosition = lp;
                g.transform.localScale = ls;
                g.GetComponent<Renderer>().material = new Material(unlit) { color = c };
                return g.transform;
            }
            // box 417: a long steel box with a hinged lid; inside, three passports, a velvet pouch, an envelope
            depositBox = new GameObject("Box417").transform;
            depositBox.position = Box417 + Vector3.back * 0.1f;
            var steel = new Color(0.32f, 0.33f, 0.34f);
            Part(depositBox, "Bottom", new Vector3(0f, -0.12f, 0f), new Vector3(0.46f, 0.02f, 0.62f), steel);
            Part(depositBox, "SideL", new Vector3(-0.22f, 0f, 0f), new Vector3(0.02f, 0.24f, 0.62f), steel);
            Part(depositBox, "SideR", new Vector3(0.22f, 0f, 0f), new Vector3(0.02f, 0.24f, 0.62f), steel);
            Part(depositBox, "Front", new Vector3(0f, 0f, -0.3f), new Vector3(0.46f, 0.24f, 0.02f), steel * 1.1f);
            Part(depositBox, "Back", new Vector3(0f, 0f, 0.3f), new Vector3(0.46f, 0.24f, 0.02f), steel);
            var hinge = new GameObject("LidHinge").transform;
            hinge.SetParent(depositBox, false);
            hinge.localPosition = new Vector3(0f, 0.12f, 0.3f);
            boxLid = hinge;
            Part(hinge, "Lid", new Vector3(0f, 0.01f, -0.31f), new Vector3(0.47f, 0.02f, 0.63f), steel * 1.15f);
            for (int i = 0; i < 3; i++)
                Part(depositBox, "Passport", new Vector3(-0.1f + i * 0.015f, -0.1f + i * 0.008f, 0.12f), new Vector3(0.09f, 0.006f, 0.13f), new Color(0.07f + i * 0.02f, 0.07f, 0.075f));
            pouch = Part(depositBox, "Pouch", new Vector3(0.1f, -0.08f, 0.1f), new Vector3(0.09f, 0.05f, 0.11f), new Color(0.04f, 0.04f, 0.045f));
            Part(depositBox, "Envelope", new Vector3(0f, -0.105f, -0.12f), new Vector3(0.3f, 0.01f, 0.18f), new Color(0.82f, 0.81f, 0.78f));
            // the car across the road: its driver's door, which will open
            carDoor = new GameObject("CarDoorHinge").transform;
            carDoor.position = new Vector3(-6.0f + 1.0f, 0.65f, -25.75f);
            Part(carDoor, "CarDoor", new Vector3(-0.6f, 0f, 0f), new Vector3(1.2f, 0.9f, 0.05f), new Color(0.03f, 0.03f, 0.035f));
        }

        void WearRing()
        {
            var hand = hero.body != null ? hero.body.Bone("hand.L") : null;
            if (hand == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = "Ring";
            ringOnHand = go.transform;
            ringOnHand.SetParent(hand, false);
            ringOnHand.localPosition = new Vector3(0f, 0.06f, 0f);
            ringOnHand.localScale = new Vector3(0.05f, 0.008f, 0.05f) / Mathf.Max(0.01f, hand.lossyScale.x);
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.85f, 0.84f, 0.8f) };
        }

        // ---------------------------------------------------------------- beats the player plays

        float StreetLo() => -24.5f;

        /// <summary>Hand him over: hold to walk towards `toX` (or away, dir -1), at a walking pace,
        /// the camera following; back to the story when he gets there.</summary>
        IEnumerator PlayerWalk(float toX, float minX, float pace, string prompt, int dir = 1)
        {
            float lo = dir > 0 ? Mathf.Min(minX, hero.transform.position.x - 0.5f) : toX - 0.4f;
            float hi = dir > 0 ? toX + 0.4f : hero.transform.position.x + 0.5f;
            hero.arenaMinX = lo; hero.arenaMaxX = hi;
            if (cam != null) { cam.SetBounds(lo - 3f, hi + 3f); cam.Scripted(Vector3.zero, Vector3.zero, 0f); }
            barsPx = 0f;
            hero.runSpeed = pace;
            hero.SetFacing(dir);
            hero.CineRelease();
            walkTitle = prompt;
            float from = hero.transform.position.x;
            while (dir > 0 ? hero.transform.position.x < toX - 0.15f : hero.transform.position.x > toX + 0.15f)
            {
                float moved = Mathf.Abs(hero.transform.position.x - from);
                walkAlpha = Mathf.MoveTowards(walkAlpha, moved < 1.2f ? 1f : 0f, Time.unscaledDeltaTime * 2f);
                if (Time.unscaledTime > footAt && hero.Velocity.sqrMagnitude > 0.2f) { footAt = Time.unscaledTime + 0.5f; Sound.Play(Sfx.Footstep, 0.4f, hero.transform.position, 1.0f); }
                yield return null;
            }
            walkAlpha = 0f;
            walkTitle = null;
            hero.runSpeed = runSpeed;
            hero.CineLock();
            barsPx = 92f;
            if (cam != null) cam.ClearBounds();
        }

        /// <summary>A few steps he takes on his own (the story walks him).</summary>
        IEnumerator PuppetWalk(float toX, float seconds, System.Action frame)
        {
            if (hero.body.Has("walk")) hero.body.Play("walk", 0.2f, true, 0.8f);
            var from = hero.transform.position;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                hero.CinePose(new Vector3(Mathf.Lerp(from.x, toX, t / seconds), 0f, 0f));
                frame();
                yield return null;
            }
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f);
        }

        /// <summary>Wait for the player's tap (anything: a tap, a swipe, a key), the shot held.</summary>
        IEnumerator WaitTap(string title, string how, System.Action frame)
        {
            tapTitle = title; tapHow = how; tapAt = Time.unscaledTime;
            var gi = GestureInput.Instance;
            if (gi != null) gi.Flush();
            yield return null;
            bool done = false;
            while (!done)
            {
                if (gi != null) for (var c = gi.Next(); c != Command.None; c = gi.Next()) done = true;
                if (gi != null && gi.AnyPressThisFrame && Time.unscaledTime - tapAt > 0.3f) done = true;
                frame();
                yield return null;
            }
            Sound.Play(Sfx.Cloth, 0.4f, null, 1.3f);
            tapTitle = null;
            tapAt = -1f;
        }

        /// <summary>The clock strikes: `n` slow chimes in the hall.</summary>
        void Bell(int n)
        {
            for (int i = 0; i < n; i++) Sound.PlayLater(Sfx.MetalClang, 0.2f + i * 1.1f, 0.22f, new Vector3(13f, 9.6f, 15f));
        }

        void ShowTitle(string small, string big)
        {
            titleSmall = small; titleBig = big;
            bankUi = 6; bankUiAt = Time.unscaledTime;
        }

        // ---------------------------------------------------------------- what is drawn over the picture

        const float MemorySeconds = 13.5f;
        string cardTitle = "THE HITMAN";

        void BankGUI(float w, float h)
        {
            float t = Time.unscaledTime - bankUiAt;
            var oldCapAlign = cap.alignment;
            int capSize = cap.fontSize, bigSize = big.fontSize;
            cap.alignment = TextAnchor.MiddleCenter;
            switch (bankUi)
            {
                case 6:   // the chapter's title over the street
                    {
                        float a = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((4.4f - t) / 0.8f);
                        cap.fontSize = 22;
                        cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f * a);
                        GUI.Label(new Rect(0, h * 0.36f, w, 30), Spaced(titleSmall), cap);
                        big.fontSize = 78;
                        big.normal.textColor = new Color(1f, 1f, 1f, a);
                        GUI.Label(new Rect(0, h * 0.36f + 30, w, 90), Spaced(titleBig), big);
                        Fill(new Rect(w * 0.5f - 110 * a, h * 0.36f + 122, 220 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
                        break;
                    }
                case 1: ScreenGUI(w, h, t); break;
                case 2: PassportsGUI(w, h, t); break;
                case 3: RingGUI(w, h, t); break;
                case 4: MemoryGUI(w, h, t); break;
                case 5: EnvelopeGUI(w, h, t); break;
            }
            // the player's prompts: walk, and tap
            if (walkTitle != null && walkAlpha > 0.01f)
            {
                prompt.normal.textColor = new Color(1f, 1f, 1f, 0.9f * walkAlpha);
                GUI.Label(new Rect(0, h * 0.72f, w, 40), Spaced(walkTitle), prompt);
            }
            if (tapTitle != null)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - tapAt) / 0.4f);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                big.fontSize = 54;
                big.normal.textColor = new Color(1f, 1f, 1f, k);
                GUI.Label(new Rect(0, h * 0.16f, w, 70), Spaced(tapTitle), big);
                Fill(new Rect(w * 0.5f - 70 * k, h * 0.16f + 68, 140 * k, 3), new Color(0.8f, 0.06f, 0.07f, k));
                prompt.normal.textColor = new Color(1f, 1f, 1f, (0.55f + 0.45f * pulse) * k);
                GUI.Label(new Rect(0, h * 0.16f + 80, w, 36), Spaced(tapHow), prompt);
                if (ring != null)
                {
                    float r = 34f + 10f * pulse;
                    var o = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.6f * k);
                    GUI.DrawTexture(new Rect(w * 0.5f - r, h * 0.62f - r, r * 2f, r * 2f), ring);
                    GUI.color = o;
                }
            }
            cap.alignment = oldCapAlign; cap.fontSize = capSize; big.fontSize = bigSize;
        }

        /// <summary>B8: the clerk's screen. A flag on the box, blinking red.</summary>
        void ScreenGUI(float w, float h, float t)
        {
            float a = Mathf.Clamp01(t / 0.3f);
            float bh = barsPx * (h / 720f);
            float sh = Mathf.Min(h - bh * 2f - 60f, 380f), sw = sh * 1.5f;
            var r = new Rect((w - sw) * 0.5f, (h - sh) * 0.5f, sw, sh);
            Fill(new Rect(r.x - 10, r.y - 10, r.width + 20, r.height + 20), new Color(0.03f, 0.03f, 0.035f, a));
            Fill(r, new Color(0.1f, 0.11f, 0.12f, a));
            float u = sh / 100f;
            cap.alignment = TextAnchor.MiddleLeft;
            cap.fontSize = Mathf.RoundToInt(4.6f * u);
            cap.normal.textColor = new Color(0.7f, 0.72f, 0.75f, a);
            GUI.Label(new Rect(r.x + 6 * u, r.y + 4 * u, sw, 8 * u), Spaced("MERIDIAN TRUST") + "   ·   VAULT ACCESS", cap);
            Fill(new Rect(r.x + 6 * u, r.y + 13 * u, sw - 12 * u, 0.4f * u), new Color(1f, 1f, 1f, 0.2f * a));
            cap.fontSize = Mathf.RoundToInt(6.4f * u);
            cap.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(r.x + 6 * u, r.y + 20 * u, sw, 10 * u), "BOX   417", cap);
            cap.fontSize = Mathf.RoundToInt(5f * u);
            GUI.Label(new Rect(r.x + 6 * u, r.y + 34 * u, sw, 8 * u), "HOLDER", cap);
            Fill(new Rect(r.x + 40 * u, r.y + 35.5f * u, 60 * u, 5f * u), new Color(0f, 0f, 0f, a));
            GUI.Label(new Rect(r.x + 6 * u, r.y + 46 * u, sw, 8 * u), "LAST ACCESS        11 YEARS", cap);
            bool on = Mathf.Repeat(t * 2.2f, 1f) < 0.62f;
            var flag = new Rect(r.x + 6 * u, r.y + 64 * u, sw - 12 * u, 18 * u);
            Fill(flag, new Color(0.75f, 0.06f, 0.06f, (on ? 0.95f : 0.4f) * a));
            cap.alignment = TextAnchor.MiddleCenter;
            cap.fontSize = Mathf.RoundToInt(6f * u);
            cap.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.Label(flag, Spaced("FLAG") + "  :  " + Spaced("NOTIFY ON ACCESS"), cap);
        }

        /// <summary>B14: three passports, three names, the same grainy face.</summary>
        void PassportsGUI(float w, float h, float t)
        {
            float bh = barsPx * (h / 720f);
            float ph = Mathf.Min(h - bh * 2f - 70f, 330f), pw = ph * 0.72f;
            string[] names = { "DANIEL REYES", "MARCUS VALE", "VIKTOR SAND" };
            string[] lands = { "CANADA", "IRELAND", "SWEDEN" };
            for (int i = 0; i < 3; i++)
            {
                float a = Mathf.Clamp01((t - i * 0.5f) / 0.35f);
                if (a <= 0f) continue;
                float x = w * 0.5f + (i - 1) * (pw + 24f) - pw * 0.5f;
                var r = new Rect(x, (h - ph) * 0.5f + (1f - a) * 30f, pw, ph);
                Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0.05f, 0.05f, 0.055f, a));
                Fill(r, new Color(0.82f, 0.82f, 0.8f, a));
                float u = ph / 100f;
                cap.fontSize = Mathf.RoundToInt(4.2f * u);
                cap.normal.textColor = new Color(0.25f, 0.25f, 0.26f, a);
                GUI.Label(new Rect(r.x, r.y + 3 * u, pw, 6 * u), Spaced("PASSPORT") + "  ·  " + lands[i], cap);
                var o = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(new Rect(r.x + pw * 0.2f, r.y + 12 * u, pw * 0.6f, pw * 0.6f * 1.22f), Portrait());
                GUI.color = o;
                cap.fontSize = Mathf.RoundToInt(5.2f * u);
                cap.normal.textColor = new Color(0.08f, 0.08f, 0.09f, a);
                GUI.Label(new Rect(r.x, r.y + 12 * u + pw * 0.74f, pw, 8 * u), names[i], cap);
                Fill(new Rect(r.x + 8 * u, r.yMax - 12 * u, pw - 16 * u, 0.5f * u), new Color(0f, 0f, 0f, 0.3f * a));
                Fill(new Rect(r.x + 8 * u, r.yMax - 8 * u, pw - 16 * u, 0.5f * u), new Color(0f, 0f, 0f, 0.3f * a));
            }
        }

        /// <summary>B15: the ring in his palm, the words inside the band.</summary>
        void RingGUI(float w, float h, float t)
        {
            float a = Mathf.Clamp01(t / 0.5f);
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.55f * a));
            float s = Mathf.Min(h * 0.42f, 260f);
            var c = new Vector2(w * 0.5f, h * 0.46f);
            var o = GUI.color;
            if (ring != null)
            {
                GUI.color = new Color(0.86f, 0.85f, 0.8f, a);
                GUI.DrawTexture(new Rect(c.x - s * 0.5f, c.y - s * 0.5f, s, s), ring);
                GUI.color = new Color(1f, 1f, 1f, 0.5f * a * (0.6f + 0.4f * Mathf.Sin(t * 3f)));
                GUI.DrawTexture(new Rect(c.x - s * 0.18f, c.y - s * 0.52f, s * 0.36f, s * 0.12f), disc);   // a glint on the gold
            }
            GUI.color = o;
            if (hand == null) hand = Resources.Load<Font>("Fonts/PermanentMarker-Regular");
            var style = new GUIStyle(sub) { font = hand != null ? hand : sub.font, fontSize = Mathf.RoundToInt(s * 0.12f), alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = new Color(0.9f, 0.9f, 0.88f, Mathf.Clamp01((t - 0.8f) / 0.6f));
            GUI.Label(new Rect(0, c.y + s * 0.56f, w, s * 0.2f), "E. — always", style);
        }

        /// <summary>B16: the memory. Soft white stills of their wedding and their life, slowly
        /// pushing in and dissolving one into the next.</summary>
        void MemoryGUI(float w, float h, float t)
        {
            if (memories == null)
            {
                memories = new Texture2D[5];
                for (int i = 0; i < 5; i++) memories[i] = Resources.Load<Texture2D>($"Story/Memories/wed_{i + 1}");
            }
            float a = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((MemorySeconds - t) / 0.8f);
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, a));
            float per = MemorySeconds / 5f;
            for (int i = 0; i < 5; i++)
            {
                float lt = t - i * per;
                float ka = Mathf.Clamp01((lt + 0.6f) / 0.9f) * Mathf.Clamp01((per + 0.6f - lt) / 0.9f);
                if (ka <= 0f || memories[i] == null) continue;
                float zoom = 1.04f + 0.08f * Mathf.Clamp01(lt / per);
                float iw = w * zoom, ih = iw * 9f / 16f;
                if (ih < h * zoom) { ih = h * zoom; iw = ih * 16f / 9f; }
                var o = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, ka * a);
                GUI.DrawTexture(new Rect((w - iw) * 0.5f, (h - ih) * 0.5f, iw, ih), memories[i]);
                // the soft glow of remembering
                GUI.color = new Color(1f, 1f, 1f, 0.16f * ka * a);
                if (vig != null) GUI.DrawTexture(new Rect(-w * 0.1f, -h * 0.1f, w * 1.2f, h * 1.2f), disc);
                GUI.color = o;
            }
            if (vig != null) { var o = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.6f * a); GUI.DrawTexture(new Rect(-w * 0.05f, -h * 0.05f, w * 1.1f, h * 1.1f), vig); GUI.color = o; }
        }

        /// <summary>B19: the envelope: banded notes, and on its face, in her hand, the way home.</summary>
        void EnvelopeGUI(float w, float h, float t)
        {
            float a = Mathf.Clamp01(t / 0.45f);
            float bh = barsPx * (h / 720f);
            float eh = Mathf.Min(h - bh * 2f - 80f, 300f), ew = eh * 1.75f;
            var r = new Rect((w - ew) * 0.5f, (h - eh) * 0.5f - 10f, ew, eh);
            // the cash, fanned out behind
            for (int i = 0; i < 3; i++)
            {
                var n = new Rect(r.x + ew * 0.55f + i * 14f, r.y - eh * 0.18f + i * 8f, ew * 0.42f, eh * 0.4f);
                Fill(n, new Color(0.62f - i * 0.04f, 0.63f - i * 0.04f, 0.6f - i * 0.04f, a));
                Fill(new Rect(n.x + n.width * 0.42f, n.y, n.width * 0.08f, n.height), new Color(0.3f, 0.3f, 0.3f, a));     // the band
            }
            Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0.1f, 0.1f, 0.1f, a));
            Fill(r, new Color(0.86f, 0.85f, 0.82f, a));
            Fill(new Rect(r.x, r.y, r.width, 2), new Color(0.6f, 0.6f, 0.58f, a));
            if (hand == null) hand = Resources.Load<Font>("Fonts/PermanentMarker-Regular");
            var style = new GUIStyle(sub) { font = hand != null ? hand : sub.font, fontSize = Mathf.RoundToInt(eh * 0.1f), alignment = TextAnchor.MiddleCenter };
            float ta = Mathf.Clamp01((t - 0.5f) / 0.8f);
            style.normal.textColor = new Color(0.12f, 0.12f, 0.14f, a * ta);
            GUI.Label(new Rect(r.x, r.y + eh * 0.16f, ew, eh * 0.22f), "For when you need to come home.", style);
            style.fontSize = Mathf.RoundToInt(eh * 0.09f);
            GUI.Label(new Rect(r.x, r.y + eh * 0.38f, ew, eh * 0.16f), "— E.", style);
            style.fontSize = Mathf.RoundToInt(eh * 0.15f);
            style.normal.textColor = new Color(0.08f, 0.08f, 0.1f, a * Mathf.Clamp01((t - 1.2f) / 0.8f));
            GUI.Label(new Rect(r.x, r.y + eh * 0.6f, ew, eh * 0.26f), "14 ALDER LANE", style);
        }
    }
}
