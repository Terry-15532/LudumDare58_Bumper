#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

public class StageInclineController : MonoBehaviour {
    public float maxInclineAngle = 5f;    // max tilt angle in degrees for both pitch (X) and roll (Z)
    public float inclineSpeedX = 1f;      // responsiveness for pitch (X axis)
    public float inclineSpeedZ = 1f;      // responsiveness for roll  (Z axis)
    public float mouseSensitivity = 2f;   // scales raw mouse delta input
    public float angularLerp = 0.2f;      // how quickly rb.angularVelocity moves toward desired (0..1)
    public float smallAngleThresholdDeg = 0.5f; // below this angle, treat as zero to avoid jitter

    // Auto-return settings: when there's no input, stage returns to level at this speed (deg/sec)
    public float autoReturnSpeed = 30f;
    public float inputDeadzone = 0.01f; // threshold for mouse delta to be considered 'input'

    // New: cursor lock controls so we get reliable mouse delta
    public bool lockCursor;
    public KeyCode toggleLockKey = KeyCode.Escape;

    // Optionally use the new Input System (if the package is installed). Falls back to old Input.
    public bool useNewInputSystem = false;

    // Debug: logs to help verify mouse delta and angular velocity at runtime
    public bool debugInput;

    // Control mode: choose whether to directly set angularVelocity or use AddTorque as VelocityChange
    public enum ControlMode { AngularVelocity, Torque }
    public ControlMode controlMode;
    public float torqueGain = 1f; // multiplier for torque-based control (VelocityChange)

    public Quaternion inputRot;
    public Rigidbody rb;

    // internal state
    Quaternion _initialRotation;
    Vector2 _currentTiltEuler = Vector2.zero; // x = pitch (rotation around local X), y = roll (rotation around local Z)
    Quaternion _targetRotation;

    void Reset() {
        // Try to auto-assign a Rigidbody when adding component in editor
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
        // Try parents/children if not found
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
        if (rb == null) rb = GetComponentInChildren<Rigidbody>();
    }

    void Start() {
        // remember the initial rotation as the neutral (no-tilt) orientation
        _initialRotation = transform.localRotation;
        _targetRotation = _initialRotation;
        inputRot = _targetRotation;

        // Lock cursor if requested so Input.GetAxisRaw returns reliable delta
        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update() {
        // Re-ensure we have an rb reference if it was missing at Awake
        if (rb == null) {
            rb = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>() ?? GetComponentInChildren<Rigidbody>();
            if (rb == null && debugInput) Debug.LogWarning("[StageIncline] Rigidbody not found on object or parents/children.");
        }

        // Allow toggling cursor lock (press Escape to toggle)
        if (Input.GetKeyDown(toggleLockKey)) {
            lockCursor = !lockCursor;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }

        // Some platforms / editors may release the cursor; keep enforcing if requested
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Allow runtime reset of initial rotation (press R)
        if (Input.GetKeyDown(KeyCode.R)) {
            // Use central reset logic
            ResetIncline();
            _initialRotation = transform.localRotation;
            if (debugInput) Debug.Log("[StageIncline] Reset initial rotation at runtime.");
        }

        // Read raw mouse delta (not position). Support both input systems.
        float mx = 0f, my = 0f;
#if ENABLE_INPUT_SYSTEM
        if (useNewInputSystem && Mouse.current != null) {
            var delta = Mouse.current.delta.ReadValue();
            mx = delta.x;
            my = delta.y;
            if (debugInput) Debug.Log($"[StageIncline] Using New InputSystem mouse delta={delta}");
        } else {
            mx = Input.GetAxisRaw("Mouse X");
            my = Input.GetAxisRaw("Mouse Y");
        }
#else
        mx = Input.GetAxisRaw("Mouse X");
        my = Input.GetAxisRaw("Mouse Y");
#endif

        if (debugInput) {
            Debug.Log($"[StageIncline] lockCursor={lockCursor} useNewInputSystem={useNewInputSystem} mx={mx:F3} my={my:F3} currentTilt=({_currentTiltEuler.x:F2},{_currentTiltEuler.y:F2}) rb={(rb==null?"null":"ok")}");
        }

        // Determine whether there is user input this frame (use deadzone)
        bool hasInput = Mathf.Abs(mx) > inputDeadzone || Mathf.Abs(my) > inputDeadzone;

        // Compute change scaled by sensitivity. We keep inclineSpeed as per-axis responsiveness used to convert angle error to angular velocity later.
        // Negative on my because moving mouse up usually means tilting away (adjust as needed)
        float deltaPitch = -my * mouseSensitivity;
        float deltaRoll = mx * mouseSensitivity;

        if (hasInput) {
            // Update current tilt and clamp to allowed range based on input
            _currentTiltEuler.x = Mathf.Clamp(_currentTiltEuler.x + deltaPitch, -maxInclineAngle, maxInclineAngle);
            _currentTiltEuler.y = Mathf.Clamp(_currentTiltEuler.y + deltaRoll, -maxInclineAngle, maxInclineAngle);
        } else {
            // No input: move tilt back toward zero at autoReturnSpeed (degrees per second)
            _currentTiltEuler.x = Mathf.MoveTowards(_currentTiltEuler.x, 0f, autoReturnSpeed * Time.deltaTime);
            _currentTiltEuler.y = Mathf.MoveTowards(_currentTiltEuler.y, 0f, autoReturnSpeed * Time.deltaTime);
        }

        // Compose target rotation relative to the initial orientation. We only change local X and Z.
        _targetRotation = _initialRotation * Quaternion.Euler(_currentTiltEuler.x, 0f, _currentTiltEuler.y);

        // Publish inputRot for external use/inspection
        inputRot = _targetRotation;
    }

    void FixedUpdate() {
        if (rb == null) return;

        // Compute the current local rotation relative to the stored initial rotation
        Quaternion currentLocalQuat = Quaternion.Inverse(_initialRotation) * transform.localRotation;

        // Current local euler (0..360) -> we'll use DeltaAngle to get signed differences
        Vector3 currentLocalEuler = currentLocalQuat.eulerAngles;

        // Desired local Euler is simply the _currentTiltEuler stored in Update (X and Z), Y stays 0
        float desiredX = _currentTiltEuler.x;
        float desiredZ = _currentTiltEuler.y;

        // Compute signed shortest angle differences (degrees)
        float deltaXDeg = Mathf.DeltaAngle(currentLocalEuler.x, desiredX);
        float deltaZDeg = Mathf.DeltaAngle(currentLocalEuler.z, desiredZ);

        // Small deadzone to avoid jitter
        if (Mathf.Abs(deltaXDeg) < smallAngleThresholdDeg) deltaXDeg = 0f;
        if (Mathf.Abs(deltaZDeg) < smallAngleThresholdDeg) deltaZDeg = 0f;

        // Convert to radians (rb.angularVelocity is in radians/sec)
        float deltaXRad = deltaXDeg * Mathf.Deg2Rad;
        float deltaZRad = deltaZDeg * Mathf.Deg2Rad;

        // Desired local angular velocity (radians/sec) per-axis, independent control
        // We use inclineSpeedX/Z as gains (higher = faster to correct angle)
        Vector3 desiredLocalAngVel = new Vector3(deltaXRad * inclineSpeedX, 0f, deltaZRad * inclineSpeedZ);

        // Transform desired local angular velocity to world space
        Vector3 desiredWorldAngVel = transform.TransformDirection(desiredLocalAngVel);

        if (debugInput) {
            Debug.Log($"[StageIncline] desiredWorldAngVel={desiredWorldAngVel:F3} deltaDeg=({deltaXDeg:F2},{deltaZDeg:F2})");
        }

        // Ensure rb.maxAngularVelocity can accommodate the requested speed
        float desiredMag = desiredWorldAngVel.magnitude;
        if (rb.maxAngularVelocity < desiredMag) {
            rb.maxAngularVelocity = desiredMag + 1f; // add a small margin
            if (debugInput) Debug.Log($"[StageIncline] increased rb.maxAngularVelocity to {rb.maxAngularVelocity:F3}");
        }

        // Control mode: either steer angularVelocity directly, or apply torque as a velocity change
        float t = Mathf.Clamp01(angularLerp);
        if (controlMode == ControlMode.AngularVelocity) {
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, desiredWorldAngVel, t);
        } else {
            // Compute delta angular velocity needed
            Vector3 deltaAngVel = desiredWorldAngVel - rb.angularVelocity;
            // Apply a scaled velocity-change torque to move toward the desired angular velocity quickly
            Vector3 torque = deltaAngVel * torqueGain;
            rb.AddTorque(torque, ForceMode.VelocityChange);
            // Optionally damp toward desired using angularLerp (we still lerp current angularVelocity a bit to avoid overshoot)
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, desiredWorldAngVel, t * 0.5f);
        }
    }

    /// <summary>
    /// Resets the incline to neutral (initial) orientation.
    /// </summary>
    public void ResetIncline() {
        _currentTiltEuler = Vector2.zero;
        _targetRotation = _initialRotation;
        inputRot = _targetRotation;

        if (rb != null) {
            // Stop motion safely via Sleep (clears velocities) rather than assigning angularVelocity directly
            rb.Sleep();
        }
    }
}
