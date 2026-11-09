using UnityEngine;

namespace ProjectInk
{
    /// <summary>TEST CHECKPOINTS (MainMenu's test panel, MainMenu.DevShortcuts): jump straight to a
    /// point in the story, with the world set up as it would be there. For testing only.</summary>
    public partial class StoryIntro
    {
        /// <summary>Chapter 1's ending: 1 from the final blow (the boss beaten), 2 from the search of the dead man.</summary>
        public static int TestEnding;
        /// <summary>Chapter 2: 0 the start, 1 at the counter, 2 the vault (two keys), 3 the box, 4 the walk out.</summary>
        public static int BankFrom;

        /// <summary>The crane bay, the Foreman on his last legs: straight into the ending.</summary>
        void JumpToEnding()
        {
            int from = TestEnding;
            TestEnding = 0;
            EscapeSeen = true;
            Retry = true;
            HeadWound.Chapter = 1;
            phase = Phase.Hall;
            hallReady = true;
            LockZone(3);
            SetDoors(3);
            var sh = Find("InnerShutter");
            if (sh != null) foreach (var r in sh.GetComponentsInChildren<Renderer>()) r.enabled = false;
            hero.CinePose(new Vector3(54.2f, 0f, 0f));
            hero.SetFacing(1);
            if (hero.Style == FightStyle.Fists) hero.GiveWeapon(FightStyle.Knife, 3);
            Enemy boss = null;
            if (director != null)
            {
                WaveDirector.BossWeapon = FightStyle.Pipe;
                WaveDirector.StoryBoss = true;
                boss = director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor);
                WaveDirector.BossWeapon = FightStyle.Fists;
            }
            if (boss != null)
            {
                boss.Foreman = true;
                boss.Puppet();
                boss.PuppetPose(new Vector3(55.8f, 0f, 0f));
                boss.SetFacing(-1);
            }
            foreman = boss;
            EndingFrom = from;
            BeginEnding(boss);
        }
    }
}
