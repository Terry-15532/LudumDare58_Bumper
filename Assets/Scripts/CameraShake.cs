// using System.Collections;
// using Unity.Cinemachine;
// using UnityEngine;
//
// public class CameraShake{
//     public static CinemachineCamera camera;
//     public static CinemachinePositionComposer transposer;
//     public static Coroutine cameraShakeAni;
//
//
//     public static void Shake(Transform t, Vector2 dir, float intensity = 1, float frequency = 0.3f, float duration = 0.3f){
//         if (!camera){
//             camera = Game.instance.mainVirtualCamera;
//             transposer = camera.GetComponent<CinemachinePositionComposer>();
//         }
//         if (cameraShakeAni != null){
//             Tools.callDelayedHelper.StopCoroutine(cameraShakeAni);
//         }
//         cameraShakeAni = Tools.callDelayedHelper.StartCoroutine(ShakeCamera(t, dir, intensity * Settings.data.cameraShakeIntensity, frequency, duration));
//     }
//
//     public static IEnumerator ShakeCamera(Transform t, Vector2 dir, float intensity, float frq, float duration){
//         float dt = 0;
//         while (dt < duration){
//             dt += Time.deltaTime * Time.timeScale;
//             transposer.TargetOffset = Quaternion.Euler(new Vector3(0, 0, -t.eulerAngles.z)) * dir * (intensity * Mathf.Sin(dt * frq * Mathf.PI));
//             yield return new WaitForEndOfFrame();
//         }
//         transposer.TargetOffset = Vector3.zero;
//     }
// }
