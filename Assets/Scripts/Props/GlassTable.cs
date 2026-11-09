using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The living room's glass coffee table: a man thrown or knocked down onto it goes through
    /// it (extra damage, glass everywhere). Once.</summary>
    public class GlassTable : MonoBehaviour
    {
        bool broken;
        readonly HashSet<Enemy> down = new HashSet<Enemy>();

        void Update()
        {
            if (broken) return;
            float x = transform.position.x, y = transform.position.y;
            foreach (var e in Enemy.All)
            {
                if (e == null) continue;
                bool isDown = e.IsDown || !e.Alive;
                if (!isDown) { down.Remove(e); continue; }
                if (down.Contains(e)) continue;
                down.Add(e);                                  // he just went down: on the table?
                var p = e.transform.position;
                if (Mathf.Abs(p.x - x) < 0.95f && Mathf.Abs(p.y - y) < 0.6f) { Break(e); return; }
            }
        }

        void Break(Enemy e)
        {
            broken = true;
            var top = transform.Find("Top");
            if (top != null) top.gameObject.SetActive(false);
            var at = transform.position + Vector3.up * 0.42f;
            Sound.Play(Sfx.Glass, 1f, at);
            Sound.Play(Sfx.BodyFall, 1f, at, 0.85f);
            e.HeldDamage(15f);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(at, Vector3.up, 46, new Color(0.85f, 0.87f, 0.9f));
                InkFx.Instance.ImpactFlash(0.04f);
            }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.2f, 0.25f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(6);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(150, "THROUGH THE TABLE!");
        }
    }
}
