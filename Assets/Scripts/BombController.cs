using System;
using System.Collections;
using UnityEngine;

public class BombController : MonoBehaviour {

    public float flickerDuration = 3f, flickerRate = 1f, explosionScale = 3f, explosionDuration = 0.5f, explosionForce = 10f;
    [SerializeField] SpriteRenderer VFX;
    [SerializeField] MeshRenderer Bomb;
    public static readonly int bombAlphaIndex = Shader.PropertyToID("_Alpha");
    public bool isBlue;

    void Start(){
        StartCoroutine(BombActions());
    }

    IEnumerator BombActions(){
        SoundSys.PlaySound("BombPlaced").audioSource.volume = 0.8f;
        var mats = Bomb.materials;
        float t = 0;
        while (t < flickerDuration) {
            t += Time.deltaTime;
            float alpha = Mathf.Sin(t / flickerDuration * flickerRate * Mathf.PI * 2) * 0.5f + 0.5f;
            foreach (var mat in mats) {
                mat.SetFloat(bombAlphaIndex, alpha);
            }
            // VFX.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        // SoundSys.PlaySound("BombExplosion").audioSource.volume = 0.5f;
        PlayerController p;
        if (isBlue) {
            p = Game.instance.playerRed;
        }
        else {
            p = Game.instance.playerBlue;
        }
        var dir = p.transform.position - transform.position;
        dir.y = 0;
        float dist = dir.magnitude;
        dir.Normalize();
        yield return new WaitForFixedUpdate();
        p.rb.AddForce(dir * explosionForce / Mathf.Max(dist, 3f));
        Bomb.enabled = false;
        t = 0;
        SoundSys.PlaySound("Explosion").audioSource.volume = 0.4f;
        while (t < explosionDuration) {
            t += Time.deltaTime;
            VFX.transform.localScale = Vector3.Lerp(0.23f * Vector3.one, explosionScale * Vector3.one, Mathf.Sin(t / explosionDuration));
            VFX.color = new Color(VFX.color.r, VFX.color.g, VFX.color.b, 1 - t / explosionDuration);
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDestroy(){
        if (isBlue) {
            Game.instance.playerBlue.bombPlaced = false;
        }
        else {
            Game.instance.playerRed.bombPlaced = false;
        }
    }
}
