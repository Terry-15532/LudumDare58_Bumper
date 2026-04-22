using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public static class CameraShake {
    static CinemachineCamera _camera;
    static CinemachinePositionComposer _composer;
    static Coroutine _shakeRoutine;

    static void EnsureRefs() {
        if (_camera) return;
        _camera   = Game.instance.mainVirtualCamera;
        _composer = _camera.GetComponent<CinemachinePositionComposer>();
    }

    public static void Shake(float intensity = 0.4f, float duration = 0.25f, float frequency = 18f) {
        EnsureRefs();
        if (_shakeRoutine != null)
            Tools.callDelayedHelper.StopCoroutine(_shakeRoutine);
        _shakeRoutine = Tools.callDelayedHelper.StartCoroutine(
            DoShake(intensity * Settings.data.cameraShakeIntensity, duration, frequency));
    }

    static IEnumerator DoShake(float intensity, float duration, float frequency) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float falloff = 1f - (elapsed / duration);
            float ox = Mathf.Sin(elapsed * frequency)        * intensity * falloff;
            float oy = Mathf.Sin(elapsed * frequency * 1.3f) * intensity * falloff;
            _composer.TargetOffset = new Vector3(ox, oy, 0f);
            yield return null;
        }
        _composer.TargetOffset = Vector3.zero;
    }
}
