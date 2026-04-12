using System.Collections;
using UnityEngine;

// Invincibility — for duration, the player can't be pushed by enemies, bombs, or walls.
// Bumps rb.mass massively. The player's own movement uses ForceMode.Acceleration
// (mass-independent) so they keep full control, but external impulses become negligible.
public class InvincibilityPickup : PickupBase {
    public float duration = 4f;

    protected override void ApplyEffect(PlayerController p){
        SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
        p.StartCoroutine(Routine(p, duration));
        p.SetBuffColor(new Color(2f, 2f, 2f), duration);
    }

    static IEnumerator Routine(PlayerController p, float duration){
        float origMass = p.rb.mass;
        p.rb.mass = 100000f;
        yield return new WaitForSeconds(duration);
        p.rb.mass = origMass;
    }
}
