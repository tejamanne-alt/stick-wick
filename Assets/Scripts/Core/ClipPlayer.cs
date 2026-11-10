using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ProjectInk
{
    /// <summary>Plays MoveSet clips on the stickman rig with smooth blending, using the Playables
    /// API (no Animator Controller asset). Up to six clips blend at once: a new clip eases in
    /// while everything that was playing eases out from wherever it was, so even rapid combos
    /// never pop. Turning around is eased too. Also works out which way the imported model
    /// faces and turns it so "forward" is the parent's +X.</summary>
    [RequireComponent(typeof(Animator))]
    public class ClipPlayer : MonoBehaviour
    {
        public MoveSet moves;
        [Tooltip("Degrees per second for easing a turn-around.")]
        public float turnSpeed = 1500f;

        const int Slots = 6;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly AnimationClipPlayable[] slot = new AnimationClipPlayable[Slots];
        readonly MoveSet.Entry[] slotEntry = new MoveSet.Entry[Slots];
        readonly float[] weight = new float[Slots];
        readonly float[] fadeFrom = new float[Slots];
        int current = -1;
        float fade = 1f, fadeTime = 0.1f;
        Quaternion calibration = Quaternion.identity;
        float turnOffset;

        public MoveSet.Entry Current => current >= 0 ? slotEntry[current] : null;
        public string CurrentName => Current != null ? Current.name : "";
        public float Time => current >= 0 && slot[current].IsValid() ? (float)slot[current].GetTime() : 0f;
        public bool Finished => Current != null && !Current.loop && Time >= Current.Length;

        /// <summary>Bone transforms by name (e.g. "shin.R").</summary>
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
            // a pure turn about the vertical axis (FromToRotation is ambiguous for opposite vectors)
            float yaw = Vector3.SignedAngle(fwd.normalized, Vector3.right, Vector3.up);
            calibration = Quaternion.AngleAxis(yaw, Vector3.up);
            transform.localRotation = calibration;
        }

        /// <summary>The parent just turned round instantly: ease the body round instead.</summary>
        public void EaseTurn(float degrees)
        {
            turnOffset = Mathf.Repeat(turnOffset + degrees + 180f, 360f) - 180f;
        }

        /// <summary>Warn if the visible body is not standing on the fighter's position.</summary>
        public void CheckVisible()
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null) { Debug.LogWarning($"[ClipPlayer] {name}: no SkinnedMeshRenderer"); return; }
            var b = smr.bounds;
            float baseY = transform.position.y;
            if (b.max.y < baseY + 0.4f || b.min.y < baseY - 0.5f || b.size.y < 0.4f || b.size.y > 5f)
                Debug.LogWarning($"[ClipPlayer] {transform.parent?.name}: body bounds look wrong (min {b.min}, max {b.max}, size {b.size}) - send this line to Claude");
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
            mixer = AnimationMixerPlayable.Create(graph, Slots);
            output.SetSourcePlayable(mixer);
            graph.Play();
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        public bool Has(string clipName) => moves != null && moves.Get(clipName) != null;

        /// <summary>Play a clip by name, blending from whatever is playing now.</summary>
        public MoveSet.Entry Play(string clipName, float crossfade = 0.1f, bool restart = true, float speed = 1f)
        {
            var entry = moves != null ? moves.Get(clipName) : null;
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[ClipPlayer] missing clip '{clipName}'");
                return null;
            }
            if (!restart && current >= 0 && slotEntry[current] == entry)
            {
                slot[current].SetSpeed(speed);
                return entry;
            }

            // a free slot, or else the faintest one that is not the current clip
            int s = -1;
            float faintest = float.MaxValue;
            for (int i = 0; i < Slots; i++)
            {
                if (!slot[i].IsValid()) { s = i; break; }
                if (i != current && weight[i] < faintest) { faintest = weight[i]; s = i; }
            }
            if (s < 0) s = (current + 1) % Slots;
            if (slot[s].IsValid())
            {
                graph.Disconnect(mixer, s);
                slot[s].Destroy();
            }

            var p = AnimationClipPlayable.Create(graph, entry.clip);
            p.SetApplyFootIK(false);
            p.SetSpeed(speed);
            if (!entry.loop) p.SetDuration(entry.Length);
            graph.Connect(p, 0, mixer, s);
            slot[s] = p;
            slotEntry[s] = entry;
            weight[s] = 0f;

            // everything else fades out from where it is now
            for (int i = 0; i < Slots; i++) fadeFrom[i] = weight[i];
            current = s;
            // a little longer blends read as smoother; very short ones are kept for impacts
            fadeTime = Mathf.Max(0.04f, crossfade * 1.25f);
            bool anythingPlaying = false;
            for (int i = 0; i < Slots; i++) if (i != s && weight[i] > 0.001f) anythingPlaying = true;
            fade = anythingPlaying ? 0f : 1f;
            ApplyWeights();
            return entry;
        }

        /// <summary>Hold the current clip at a time (seconds): a pose scrubbed by the player's finger.</summary>
        public void SetTime(float seconds)
        {
            if (current >= 0 && slot[current].IsValid()) slot[current].SetTime(Mathf.Clamp(seconds, 0f, slotEntry[current].Length));
        }

        public void SetSpeed(float speed)
        {
            if (current >= 0 && slot[current].IsValid()) slot[current].SetSpeed(speed);
        }

        void Update()
        {
            float dt = UnityEngine.Time.deltaTime;
            if (fade < 1f)
            {
                fade = Mathf.Min(1f, fade + dt / fadeTime);
                ApplyWeights();
            }
            for (int i = 0; i < Slots; i++)
            {
                if (!slot[i].IsValid() || slotEntry[i] == null) continue;
                // loop clips wrap; one-shots hold their last frame
                double t = slot[i].GetTime();
                float len = slotEntry[i].Length;
                if (slotEntry[i].loop && t > len) slot[i].SetTime(t % len);
                else if (!slotEntry[i].loop && t > len) slot[i].SetTime(len);
                // clean up clips that have fully faded out
                if (i != current && fade >= 1f && weight[i] <= 0.0001f)
                {
                    graph.Disconnect(mixer, i);
                    slot[i].Destroy();
                    slotEntry[i] = null;
                }
            }
        }

        void LateUpdate()
        {
            if (Mathf.Abs(turnOffset) > 0.01f)
                turnOffset = Mathf.MoveTowards(turnOffset, 0f, turnSpeed * UnityEngine.Time.deltaTime);
            transform.localRotation = calibration * Quaternion.Euler(0f, turnOffset, 0f);
        }

        void ApplyWeights()
        {
            // ease-in-out blend, so neither end of a transition jolts
            float e = fade * fade * (3f - 2f * fade);
            for (int i = 0; i < Slots; i++)
            {
                if (!slot[i].IsValid()) { weight[i] = 0f; mixer.SetInputWeight(i, 0f); continue; }
                weight[i] = i == current ? (fadeFrom[i] + (1f - fadeFrom[i]) * e) : fadeFrom[i] * (1f - e);
                mixer.SetInputWeight(i, weight[i]);
            }
        }
    }
}
