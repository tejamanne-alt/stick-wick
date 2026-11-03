using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ProjectInk
{
    /// <summary>Plays MoveSet clips on the stickman rig with short crossfades, using the
    /// Playables API so no Animator Controller asset is needed. Also works out which way
    /// the imported model faces and turns it so "forward" is the parent's +X.</summary>
    [RequireComponent(typeof(Animator))]
    public class ClipPlayer : MonoBehaviour
    {
        public MoveSet moves;

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable current, previous;
        float fade, fadeTime;
        MoveSet.Entry currentEntry;

        public MoveSet.Entry Current => currentEntry;
        public string CurrentName => currentEntry != null ? currentEntry.name : "";
        public float Time => current.IsValid() ? (float)current.GetTime() : 0f;
        public bool Finished => currentEntry != null && !currentEntry.loop && Time >= currentEntry.Length;

        /// <summary>Bone transforms by name (e.g. "shin.R"), cached on first use.</summary>
        public Transform Bone(string boneName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == boneName) return t;
            return null;
        }

        /// <summary>Rotate this model so its kicks and punches point along the parent's +X.
        /// Measured from the clips themselves, so it is right whatever axis conversion the
        /// FBX import applied.</summary>
        public void CalibrateFacing()
        {
            var kick = moves != null ? moves.Get("front_kick") : null;
            var hips = Bone("hips");
            var knee = Bone("shin.R");
            if (kick == null || kick.clip == null || hips == null || knee == null) return;
            transform.localRotation = Quaternion.identity;
            kick.clip.SampleAnimation(gameObject, kick.hitTime > 0 ? kick.hitTime : kick.Length * 0.4f);
            Vector3 fwd = transform.InverseTransformPoint(knee.position) - transform.InverseTransformPoint(hips.position);
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) return;
            transform.localRotation = Quaternion.FromToRotation(fwd.normalized, Vector3.right);
        }

        void Awake()
        {
            var animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = null;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create(name + ".Clips");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            output.SetSourcePlayable(mixer);
            graph.Play();
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        public bool Has(string clipName) => moves != null && moves.Get(clipName) != null;

        /// <summary>Play a clip by name. Restarts it if it is already playing and restart is true.</summary>
        public MoveSet.Entry Play(string clipName, float crossfade = 0.08f, bool restart = true, float speed = 1f)
        {
            var entry = moves != null ? moves.Get(clipName) : null;
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[ClipPlayer] missing clip '{clipName}'");
                return null;
            }
            if (!restart && entry == currentEntry) return entry;

            if (previous.IsValid())
            {
                graph.Disconnect(mixer, 1);
                previous.Destroy();
            }
            if (current.IsValid())
            {
                graph.Disconnect(mixer, 0);
                previous = current;
                graph.Connect(previous, 0, mixer, 1);
            }
            current = AnimationClipPlayable.Create(graph, entry.clip);
            current.SetApplyFootIK(false);
            current.SetSpeed(speed);
            if (!entry.loop) current.SetDuration(entry.Length);
            graph.Connect(current, 0, mixer, 0);
            currentEntry = entry;
            fadeTime = Mathf.Max(0.0001f, crossfade);
            fade = previous.IsValid() ? 0f : 1f;
            ApplyWeights();
            return entry;
        }

        public void SetSpeed(float speed)
        {
            if (current.IsValid()) current.SetSpeed(speed);
        }

        void Update()
        {
            if (fade < 1f)
            {
                fade = Mathf.Min(1f, fade + UnityEngine.Time.deltaTime / fadeTime);
                ApplyWeights();
            }
            // loop clips by wrapping time; one-shots hold their last frame
            if (currentEntry != null && current.IsValid())
            {
                double t = current.GetTime();
                if (currentEntry.loop && t > currentEntry.Length) current.SetTime(t % currentEntry.Length);
                else if (!currentEntry.loop && t > currentEntry.Length) current.SetTime(currentEntry.Length);
            }
        }

        void ApplyWeights()
        {
            mixer.SetInputWeight(0, fade);
            mixer.SetInputWeight(1, previous.IsValid() ? 1f - fade : 0f);
        }
    }
}
