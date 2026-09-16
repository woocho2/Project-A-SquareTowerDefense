using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CheckProjectileVisuals
{
    static CheckProjectileVisuals()
    {
        EditorApplication.delayCall += Check;
    }

    [MenuItem("Tools/Debug Projectile Visuals")]
    public static void Check()
    {
        int[] ids = new int[] { 101, 102, 103, 104, 105, 106, 107 };
        foreach (int id in ids)
        {
            GameObject prefab = Resources.Load<GameObject>($"Projectiles/{id}");
            if (prefab == null)
            {
                Debug.LogError($"[DebugProj] Prefab {id} is NULL!");
                continue;
            }

            SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
            Animator anim = prefab.GetComponent<Animator>();
            ProjectileHit2D hit = prefab.GetComponent<ProjectileHit2D>();

            string spriteName = sr != null && sr.sprite != null ? sr.sprite.name : "NULL";
            string matName = sr != null && sr.sharedMaterial != null ? sr.sharedMaterial.name : "NULL";
            string ctrlName = anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NULL";

            Debug.Log($"[DebugProj] {id}: Sprite='{spriteName}', Mat='{matName}', Layer='{sr?.sortingLayerName}'({sr?.sortingLayerID}), Order={sr?.sortingOrder}, Color={sr?.color}, Scale={prefab.transform.localScale}, AnimCtrl='{ctrlName}', Hit2D={(hit != null ? "OK" : "NULL")}");

            if (anim != null && anim.runtimeAnimatorController != null)
            {
                var clips = anim.runtimeAnimatorController.animationClips;
                foreach (var clip in clips)
                {
                    var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    foreach (var b in bindings)
                    {
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, b);
                        int nullFrames = 0;
                        foreach (var kf in keyframes)
                        {
                            if (kf.value == null) nullFrames++;
                        }
                        Debug.Log($"[DebugProj]   Clip '{clip.name}': {keyframes.Length} frames ({nullFrames} NULL frames).");
                    }
                }
            }
        }
    }
}
