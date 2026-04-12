using UnityEngine;

// Invincibility — for `duration` seconds, external forces (enemy collisions, bombs, walls)
// have no effect. Player's own control is unaffected.
public class InvincibilityPickup : PickupBase {
    public float duration = 4f;

    protected override void ApplyEffect(PlayerController p){
        SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
        p.ApplyInvincibility(duration);
    }
}
