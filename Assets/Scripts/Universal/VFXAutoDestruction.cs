using UnityEngine;
using System.Collections;
using UnityEngine.VFX;

public class VFXAutoDestruction : MonoBehaviour {
    private VisualEffect[] particleSystems;
    private bool stopped = false, started = false;

    void Start() {
        particleSystems = GetComponentsInChildren<VisualEffect>();
        Tools.CallDelayedAsync(() => {
            DestroyImmediate(gameObject);
        }, 10);
    }

    void LateUpdate() {
        if (!started) {
            foreach (VisualEffect ps in particleSystems) {
                if (ps.aliveParticleCount > 0) {
                    started = true;
                }
            }
        }
        else {
            stopped = true;
            foreach (VisualEffect vfx in particleSystems) {
                if (vfx.aliveParticleCount > 0) {
                    stopped = false;
                }
            }
            if (stopped)
                GameObject.Destroy(gameObject);
        }
    }
}