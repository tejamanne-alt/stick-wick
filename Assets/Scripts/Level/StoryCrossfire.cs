using UnityEngine;

namespace ProjectInk
{
    /// <summary>CROSSFIRE in every chapter (docs/design/crossfire.md). The move works wherever the story's
    /// combat runs, but it needs a man in front of him and one behind, and the men mostly come at him from
    /// one side. So in every fight, in every chapter, they now box him in now and then, deliberately: with
    /// two or more men on him, one of them is told to go round ("Get behind him!") and works his way to the
    /// hero's other side (Enemy.ContactFlank). The first time comes early in a fight; after that, every 25 s
    /// or so of fighting. Only while CROSSFIRE is his to use (bare hands, the knife, the pipe, the katana).</summary>
    public partial class StoryIntro
    {
        float cfFighting, cfLastPincerAt = -99f;
        int cfLine;
        bool cfHooked;

        static readonly string[] PincerLines =
        {
            "Get behind him!", "Round the back. Go.", "Box him in!", "Other side. Now.",
            "Take his back!", "I've got the front. Go round.",
        };

        void CrossfireDirector()
        {
            if (!cfHooked) { cfHooked = true; Enemy.PincerStarted += OnPincer; }
            if (hero == null || hero.IsDead || TimeController.Paused) return;
            bool bare = hero.HasCrossfire;                                       // bare hands, the knife, the pipe, the katana
            int near = 0;
            var hp = hero.transform.position;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Alive || e.Boss || e.IsDown || e.Held) continue;
                if (Mathf.Abs(e.transform.position.x - hp.x) < 6f && Mathf.Abs(e.transform.position.y - hp.y) < 1f) near++;
            }
            if (near < 2 || !bare) { cfFighting = Mathf.Max(0f, cfFighting - Time.deltaTime); return; }
            cfFighting += Time.deltaTime;
            bool first = Time.time - cfLastPincerAt > 90f;                       // a new fight: early
            if (!Enemy.PincerNext && cfFighting > (first ? 5f : 25f) && Time.time - cfLastPincerAt > 20f)
            {
                Enemy.PincerNext = true;
                cfFighting = 0f;
            }
        }

        void OnPincer(Enemy e)
        {
            cfLastPincerAt = Time.time;
            if (this == null || e == null) return;
            Say("THUG", PincerLines[cfLine++ % PincerLines.Length], 1.4f, HeadOf(e));
        }

        /// <summary>A weapon lying in the set for the taking (placed: it never fades). Not if one of that kind
        /// already lies near (a zone started again after a defeat).</summary>
        void PlaceWeapon(FightStyle kind, int uses, Vector3 at)
        {
            if (director == null) return;
            foreach (var w in WeaponPickup.All)
                if (w != null && w.kind == kind && Mathf.Abs(w.transform.position.x - at.x) < 3f) return;
            WeaponPickup.Spawn(kind, uses, at, director.weaponMaterial);
        }

        void CrossfireUnhook() { if (cfHooked) { Enemy.PincerStarted -= OnPincer; cfHooked = false; } Enemy.PincerNext = false; }
    }
}
