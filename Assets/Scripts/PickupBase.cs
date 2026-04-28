using UnityEngine;

// Base class for all pickups. Attach a concrete subclass to a prefab with a trigger Collider.
// PlayerController.OnTriggerEnter will detect any PickupBase component and call OnPickup.
public abstract class PickupBase : MonoBehaviour {
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 2f;
    public float rotateSpeed = 1f;

    float baseY;

    protected virtual void Start(){
        baseY = transform.position.y;
    }

    protected virtual void Update(){
        transform.Rotate(Vector3.up, rotateSpeed * Time.timeScale, Space.World);
        var p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = p;
    }

    public void OnPickup(PlayerController p){
        ApplyEffect(p);
        PickupSpawner.NotifyConsumed(this);
        Destroy(gameObject);
    }

    protected abstract void ApplyEffect(PlayerController p);
}
