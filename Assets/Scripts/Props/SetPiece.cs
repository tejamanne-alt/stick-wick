using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>A thing in a level the hero can use (chapter 6 v3): the truck's handbrake, the bell rope, the hay
    /// hoist, the photograph, the telescope, the dome's crank. A tap on it walks him there and uses it
    /// (GestureInput.ThingAt → HeroController.InteractV4 → ArriveAtThing). It glints softly while it can be used.</summary>
    public class SetPiece : MonoBehaviour
    {
        public static readonly List<SetPiece> All = new List<SetPiece>();
        public string label;
        public System.Action<HeroController> onUse;
        public bool once = true;
        public bool usable = true;
        public float stand = 0.6f;               // how far from it he stands to use it
        Transform glint;
        float t;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static SetPiece Make(string label, Vector3 at, System.Action<HeroController> use, bool once = true)
        {
            var go = new GameObject("SetPiece_" + label);
            go.transform.position = at;
            var s = go.AddComponent<SetPiece>();
            s.label = label;
            s.onUse = use;
            s.once = once;
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(go.transform, false);
            g.transform.localPosition = new Vector3(0f, 1.2f, -0.6f);
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.9f, 0.9f, 0.92f) };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.glint = g.transform;
            return s;
        }

        public void Use(HeroController h)
        {
            if (!usable) return;
            if (once) usable = false;
            onUse?.Invoke(h);
            if (!usable && glint != null) glint.gameObject.SetActive(false);
        }

        void Update()
        {
            if (glint == null) return;
            t += Time.unscaledDeltaTime;
            glint.localScale = Vector3.one * (usable ? 0.08f + 0.05f * Mathf.Sin(t * 3.4f) : 0f);
        }
    }
}
