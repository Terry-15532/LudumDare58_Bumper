using UnityEngine;

// Double Score — for `duration` seconds, every coin the player eats is worth 2 points.
// Re-picking up while active refreshes the timer (extends until Time.time + duration).
public class DoubleScorePickup : PickupBase {
    public float duration = 6f;

    protected override void ApplyEffect(PlayerController p){
        SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
        p.doubleScoreUntil = Time.time + duration;
        p.SetBuffColor(new Color(0.2f, 2f, 0.2f), duration);
    }
}
