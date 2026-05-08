using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Coin : MonoBehaviour{
    public float rotateSpeed = 1f;
    public float startY = 2.5f, amplitude = 0.15f;

    // Movement / input settings
    [Header("Mouse Movement")]
    public bool requireMouseButton = true; // when true, only move while LMB is held
    public bool useRigidbodyMovement = true; // when true use AddForce on Rigidbody; otherwise fallback to transform-based
    public float forceSensitivity = 30f; // tuning multiplier for force (higher = stronger)
    public ForceMode forceMode = ForceMode.Acceleration; // choose how AddForce is applied
    public float maxSpeed = 10f; // clamp speed (units/sec)
    public float linearDrag = 2f; // mapped to Rigidbody.drag

    [Header("Movement Bounds")]
    public Vector2 boundsX = new Vector2(-10f, 10f);
    public Vector2 boundsZ = new Vector2(-6f, 6f);

    // Internal state
    private Vector3 velocity = Vector3.zero; // used by transform-based fallback
    private Rigidbody rb;
    private Vector3 storedInput = Vector3.zero; // captures mouse velocity in screen pixels/sec mapped to x/z
    private Vector3 prevMousePos = Vector3.zero;

    void Start(){
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.linearDamping = linearDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // initialize prevMousePos to avoid spikes
        prevMousePos = Input.mousePosition;
    }

    public void Update(){
        // rotation (preserve original behavior visually)
        transform.Rotate(Vector3.up, rotateSpeed * Time.timeScale, Space.World);

        // If cursor is locked, Input.mousePosition won't change; use GetAxis instead
        if (Cursor.lockState == CursorLockMode.Locked){
            if (!requireMouseButton || Input.GetMouseButton(0)){
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                // scale axes into a similar magnitude to previous pixels/sec path
                storedInput = new Vector3(mx, 0f, my) * 200f; // 200 is empirical; tune via forceSensitivity
            }
            else {
                storedInput = Vector3.zero;
            }

            // keep prevMousePos in sync in case lock state changes
            prevMousePos = Input.mousePosition;
        }
        else {
            // Sample raw mouse delta (mouse position) to get a more natural AddForce feel when unlocked
            Vector3 currentMouse = Input.mousePosition;
            if (!requireMouseButton || Input.GetMouseButton(0)){
                // delta in pixels
                Vector3 delta = currentMouse - prevMousePos;
                // convert to pixels per second to be framerate independent
                float dt = Mathf.Max(Time.deltaTime, 1e-6f);
                Vector3 mouseVel = delta / dt; // pixels/sec
                // Map screen X,Y to world X,Z
                storedInput = new Vector3(mouseVel.x, 0f, mouseVel.y);
            }
            else {
                storedInput = Vector3.zero;
            }
            prevMousePos = currentMouse;
        }

        // For fallback transform-based movement keep old code path's input handling
        if (!useRigidbodyMovement){
            if (!requireMouseButton || Input.GetMouseButton(0)){
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                Vector3 input = new Vector3(mx, 0f, my);
                velocity += input * (forceSensitivity * 0.1f); // scale a bit for parity
            }

            // Apply decay to velocity
            velocity = Vector3.Lerp(velocity, Vector3.zero, Mathf.Clamp01(linearDrag * Time.deltaTime));
            if (velocity.magnitude > maxSpeed) velocity = velocity.normalized * maxSpeed;

            Vector3 pos = transform.position;
            pos += velocity * Time.deltaTime;
            float bobY = startY + Mathf.Sin(Time.time * 2f) * amplitude;
            pos.y = bobY;
            pos.x = Mathf.Clamp(pos.x, boundsX.x, boundsX.y);
            pos.z = Mathf.Clamp(pos.z, boundsZ.x, boundsZ.y);

            // zero velocity components if against bounds
            if ((pos.x <= boundsX.x && velocity.x < 0f) || (pos.x >= boundsX.y && velocity.x > 0f)) velocity.x = 0f;
            if ((pos.z <= boundsZ.x && velocity.z < 0f) || (pos.z >= boundsZ.y && velocity.z > 0f)) velocity.z = 0f;

            transform.position = pos;
        }
    }

    void FixedUpdate(){
        if (!useRigidbodyMovement) return;

        // keep Rigidbody settings in sync with inspector
        if (rb.linearDamping != linearDrag) rb.linearDamping = linearDrag;

        // Apply force based on stored mouse velocity (pixels/sec or axis-based scaled value)
        if (storedInput != Vector3.zero){
            // Convert storedInput to world force; scale down since pixels/axis values can be large
            Vector3 force = new Vector3(storedInput.x, 0f, storedInput.z) * (forceSensitivity * 0.01f);
            rb.AddForce(force, forceMode);
        }

        // Clamp horizontal velocity (xz plane)
        Vector3 vel = rb.linearVelocity;
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);
        float mag = velXZ.magnitude;
        if (mag > maxSpeed){
            velXZ = velXZ.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(velXZ.x, 0f, velXZ.z);
        }
        else {
            // ensure no vertical velocity accumulates
            rb.linearVelocity = new Vector3(vel.x, 0f, vel.z);
        }

        // Calculate bobbing Y and set final position while letting physics control x/z
        float bobY = startY + Mathf.Sin(Time.time * 2f) * amplitude;
        Vector3 targetPos = new Vector3(rb.position.x, bobY, rb.position.z);

        // Clamp to bounds and zero velocity components pushing out if necessary
        float clampedX = Mathf.Clamp(targetPos.x, boundsX.x, boundsX.y);
        float clampedZ = Mathf.Clamp(targetPos.z, boundsZ.x, boundsZ.y);

        if (clampedX != targetPos.x){
            // Hitting X bound
            targetPos.x = clampedX;
            if ((rb.linearVelocity.x > 0f && targetPos.x >= boundsX.y) || (rb.linearVelocity.x < 0f && targetPos.x <= boundsX.x)){
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
            }
        }
        if (clampedZ != targetPos.z){
            // Hitting Z bound
            targetPos.z = clampedZ;
            if ((rb.linearVelocity.z > 0f && targetPos.z >= boundsZ.y) || (rb.linearVelocity.z < 0f && targetPos.z <= boundsZ.x)){
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f);
            }
        }

        // Move the rigidbody to the target (preserve physics interpolation)
        rb.MovePosition(targetPos);

        // Keep transform aligned (MovePosition will update rb.position, but ensure transform y matches bobbing)
        transform.position = new Vector3(rb.position.x, bobY, rb.position.z);
    }

    public void ChangePosition(){
        // stop any momentum when changing position
        if (rb != null) rb.linearVelocity = Vector3.zero;

        SoundSys.PlaySound("Coins").audioSource.volume = 0.3f;
        var vfx = Resources.Load<GameObject>("Prefabs/CoinVFX");
        vfx = Instantiate(vfx, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
        Tools.CallDelayed(() => {
            if (Game.instance.matchStarted && Game.instance.matchRunning){
                Vector3 newPos = new Vector3(Tools.RandomNum(-10, 10), transform.position.y, Tools.RandomNum(-6, 6));
                transform.position = newPos;
                if (rb != null) rb.position = new Vector3(newPos.x, newPos.y, newPos.z);
                gameObject.SetActive(true);
                Destroy(vfx.gameObject);
            }
        }, 0.5f);
    }

    // Public utility to immediately stop movement (useful for external reset or UI actions)
    public void StopMovement(){
        storedInput = Vector3.zero;
        velocity = Vector3.zero;
        if (rb != null){
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Alias for clarity
    public void ResetVelocity(){
        StopMovement();
    }

}
