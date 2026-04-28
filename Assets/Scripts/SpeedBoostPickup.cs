using UnityEngine;

// Speed Boost — for `duration` seconds, the player's acc and maxSpeed are multiplied.
// Just sets a timestamp on the player; PlayerController.UpdateBuffs applies the effect each frame.
public class SpeedBoostPickup : PickupBase {
    public float duration = 4f;
    public float multiplier = 1.5f;

    protected override void ApplyEffect(PlayerController p){
        SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
        p.ApplySpeedBoost(duration, multiplier);
    }
}
