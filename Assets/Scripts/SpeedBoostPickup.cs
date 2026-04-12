using System.Collections;
using UnityEngine;

// Speed Boost — temporarily multiplies the player's acc and maxSpeed.
public class SpeedBoostPickup : PickupBase {
    public float duration = 4f;
    public float multiplier = 1.5f;

    protected override void ApplyEffect(PlayerController p){
        SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
        p.StartCoroutine(Routine(p, duration, multiplier));
        p.SetBuffColor(new Color(2f, 1.5f, 0.2f), duration);
    }

    static IEnumerator Routine(PlayerController p, float duration, float multiplier){
        float origAcc = p.acc;
        float origMax = p.maxSpeed;
        p.acc = origAcc * multiplier;
        p.maxSpeed = origMax * multiplier;
        p.SetSmokeIntensitySmooth(2f, 0.1f);
        yield return new WaitForSeconds(duration);
        p.acc = origAcc;
        p.maxSpeed = origMax;
        p.SetSmokeIntensitySmooth(0f, 0.3f);
    }
}
