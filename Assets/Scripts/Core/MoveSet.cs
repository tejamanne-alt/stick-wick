using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>Every animation clip of the stickman rig plus its timing data.
    /// Created by the PrototypeBuilder from Stickman.fbx and stickman_clips.json.</summary>
    [CreateAssetMenu(menuName = "Stick Wick/Move Set")]
    public class MoveSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string name;
            public AnimationClip clip;
            public bool loop;
            [Tooltip("Seconds into the clip where the strike connects (-1 = not an attack).")]
            public float hitTime = -1f;
            [Tooltip("Metres forward the body's hips end up at the clip's end (paired throws and slams): " +
                     "move the transform by this when the clip hands over, so he lies where he landed.")]
            public float endShift;
            [Tooltip("Combat 3.0 clips: seconds into the clip where the wind-up is fully loaded (-1 = none). An enemy's " +
                     "telegraph plays the clip slowly up to here, so the warning is the blow itself.")]
            public float loadTime = -1f;
            [Tooltip("Combat 3.0 clips: the distance (between the two men) at which the blow lands where it was keyed.")]
            public float reach;
            public float Length => clip != null ? clip.length : 0f;
        }

        public List<Entry> entries = new List<Entry>();

        Dictionary<string, Entry> lookup;

        public Entry Get(string clipName)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Entry>();
                foreach (var e in entries)
                    if (e != null && !string.IsNullOrEmpty(e.name)) lookup[e.name] = e;
            }
            lookup.TryGetValue(clipName, out var entry);
            return entry;
        }
    }
}
