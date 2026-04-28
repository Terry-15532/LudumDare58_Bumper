using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Serialization;
using UnityEngine.XR;
using InputDevice = UnityEngine.InputSystem.InputDevice;

public enum PlayerControlDevice {
    Keyboard,
    Gamepad,
    AI
}

public enum PlayerSide {
    Blue = 0,
    Red = 1
}

public class PlayerController : MonoBehaviour {

    public GameObject player, head;

    [FormerlySerializedAs("speed")]
    public float acc = 20.0f;

    public float maxSpeed = 10.0f;
    public PlayerControlDevice device;
    public PlayerSide side;
    public Rigidbody rb;
    public MeshRenderer mr;

    // Populated by Game.NFCStartMatch when a match begins via NFC.
    // Empty strings in keyboard / AI mode — UI should fall back to the side label.
    [System.NonSerialized] public string profileUid   = "";
    [System.NonSerialized] public string profileName  = "";
    [System.NonSerialized] public string profileEmoji = "";

    public InputActionAsset input;
    public InputAction movement, placeBomb, dash, shield, taunt;
    public InputAction trackball;

    public GameObject BombPrefab;



    static readonly Vector3 blueBirthPoint = new Vector3(10, 1.7f, 0), redBirthPoint = new Vector3(-10f, 1.7f, 0);

    // Used by Game.cs NFC mode for confirm (index 4) / reroll (index 5) per side.
    // Indexes: [0-3] up/down/left/right  [4] bomb  [5] dash  [6] shield  [7] taunt.
    public static KeyCode[] blueKeys = {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D,
        KeyCode.Space, KeyCode.LeftShift, KeyCode.LeftControl, KeyCode.Z
    };
    public static KeyCode[] redKeys = {
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.Return, KeyCode.RightShift, KeyCode.RightControl, KeyCode.Slash
    };
    static readonly int _SmokeIntensityBlue = Shader.PropertyToID("_BlueSmokeStrength");
    static readonly int _SmokeIntensityRed = Shader.PropertyToID("_RedSmokeStrength");

    // public static PlayerController CreatePlayer(PlayerControlDevice device, PlayerSide side){
    //     GameObject playerPrefab = Resources.Load<GameObject>("Player");
    //     GameObject playerInstance = Instantiate(playerPrefab);
    //     PlayerController controller = playerInstance.GetComponent<PlayerController>();
    //     controller.device = device;
    //     controller.side = side;
    //     return controller;
    // }

    public void ResetPosition(){
        if (side == PlayerSide.Blue) {
            player.transform.position = blueBirthPoint;
        }
        else {
            player.transform.position = redBirthPoint;
        }
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, side == PlayerSide.Blue ? 90f : -90f, 0);
        ClearBuffs();
    }

    public void ClearBuffs(){
        speedBoostUntil = 0f;
        invincibleUntil = 0f;
        doubleScoreUntil = 0f;
        dashCooldownLeft = 0f;
        shieldCooldownLeft = 0f;
        shieldActive = false;
        shieldActiveLeft = 0f;
        tauntCooldownLeft = 0f;
        if (rb != null) rb.mass = baseMass;
    }

    public int gamePadIndex;

    public void Awake(){
        Init();
    }

    Vector3 lastVelocity;
    public Vector3 acceleration;

    void CalculateAcc(){
        acceleration = (rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = rb.linearVelocity;
    }

    public void UpdateHeadAnimation(){
        var targetpos = Quaternion.Inverse(player.transform.rotation) * new Vector3(-rb.linearVelocity.x * 0.03f - acceleration.x * 0.0025f, 0, -rb.linearVelocity.z * 0.03f - acceleration.z * 0.0025f);
        //damp
        head.transform.localPosition = Vector3.Lerp(head.transform.localPosition, targetpos, Time.deltaTime * 20);
    }

    public void Init(){
        // mr = player.GetComponentInChildren<MeshRenderer>();
        rb = player.GetComponent<Rigidbody>();
        // keys = (side == PlayerSide.Blue) ? blueKeys : redKeys;
        // input.SwitchCurrentActionMap(side == PlayerSide.Blue ? "BluePlayer" : "RedPlayer");
        baseAcc = acc;
        baseMaxSpeed = maxSpeed;
        baseMass = rb.mass;
        var mats = mr.materials;
        originalMaterialColors = new Color[mats.Length];
        for (int i = 0; i < mats.Length; i++)
            originalMaterialColors[i] = mats[i].GetColor(buffColorId);
        ResetPosition();
        Shader.SetGlobalFloat(side == PlayerSide.Blue ? _SmokeIntensityBlue : _SmokeIntensityRed, 0f);

        if (device == PlayerControlDevice.Gamepad) {
            // input.user.UnpairDevices();
            // InputUser.PerformPairingWithDevice(Gamepad.all[(int)side], input.user);
            if (gamePadIndex < 0 || gamePadIndex >= Gamepad.all.Count) {
                Debug.LogWarning($"{name}: gamePadIndex {gamePadIndex} but only {Gamepad.all.Count} gamepad(s) connected — skipping gamepad bind.");
                return;
            }
            Gamepad pad = Gamepad.all[gamePadIndex];
            var cloned = Instantiate(input).FindActionMap("PlayerMovement");
            movement = cloned.FindAction("Movement");
            placeBomb = cloned.FindAction("PlaceBomb");
            dash = cloned.FindAction("Dash");
            shield = cloned.FindAction("Shield");
            // taunt = cloned.FindAction("Taunt");
            trackball = cloned.FindAction("Trackball");
            // foreach (var action in cloned.actionMaps) {
            //     action.ApplyBindingOverride(new InputBinding() {
            //         overrideInteractions = "",
            //         groups = "Gamepad"
            //     });
            // }
            // InputSystem.AddDevice(pad);
            cloned.devices = new[] {
                pad, (InputDevice)Keyboard.current
            };
            cloned.Enable();
        }

    }

    static readonly int buffColorId = Shader.PropertyToID("_Color");
    Color[] originalMaterialColors;

    // Buff timestamps — a buff is active while Time.time < *Until.
    // Each frame UpdateBuffs() applies runtime effects and material color based on these.
    public float speedBoostUntil = 0f;
    public float invincibleUntil = 0f;
    public float speedBoostMultiplier = 1.5f;

    // Most-recently-applied durations for each buff. Used by PickupHUD to compute progress bar fill.
    [HideInInspector] public float speedBoostDuration = 0f;
    [HideInInspector] public float invincibleDuration = 0f;
    [HideInInspector] public float doubleScoreDuration = 0f;

    // Baselines captured in Init so runtime values are always derived from a known-good source,
    // never from a possibly-already-boosted "current" value. Prevents compounding stack bugs.
    float baseAcc, baseMaxSpeed, baseMass;

    enum BuffVisual { None, SpeedBoost, DoubleScore, Invincible }
    BuffVisual lastBuffVisual = BuffVisual.None;

    public void ApplySpeedBoost(float duration, float multiplier){
        speedBoostUntil = Time.time + duration;
        speedBoostDuration = duration;
        speedBoostMultiplier = multiplier;
    }

    public void ApplyInvincibility(float duration){
        invincibleUntil = Time.time + duration;
        invincibleDuration = duration;
    }

    public void ApplyDoubleScore(float duration){
        doubleScoreUntil = Time.time + duration;
        doubleScoreDuration = duration;
    }

    void UpdateBuffs(){
        bool speedActive = Time.time < speedBoostUntil;
        bool invincibleActive = Time.time < invincibleUntil;
        bool doubleActive = Time.time < doubleScoreUntil;

        acc = speedActive ? baseAcc * speedBoostMultiplier : baseAcc;
        maxSpeed = speedActive ? baseMaxSpeed * speedBoostMultiplier : baseMaxSpeed;
        rb.mass = invincibleActive ? 100000f : baseMass;

        BuffVisual current = BuffVisual.None;
        if (invincibleActive) current = BuffVisual.Invincible;
        else if (speedActive) current = BuffVisual.SpeedBoost;
        else if (doubleActive) current = BuffVisual.DoubleScore;

        if (current == lastBuffVisual) return;
        lastBuffVisual = current;
        Color c = current switch {
            BuffVisual.Invincible => new Color(2f, 2f, 2f),
            BuffVisual.SpeedBoost => new Color(0.2f, 2f, 0.2f),
            BuffVisual.DoubleScore => new Color(2f, 1.5f, 0.2f),
            _ => Color.clear
        };
        var mats = mr.materials;
        for (int i = 0; i < mats.Length; i++) {
            mats[i].SetColor(buffColorId, current == BuffVisual.None ? originalMaterialColors[i] : c);
        }
    }

    Coroutine smokeCoroutine;

    public void SetSmokeIntensitySmooth(float targetIntensity, float duration){
        if (smokeCoroutine != null) {
            StopCoroutine(smokeCoroutine);
        }
        smokeCoroutine = Tools.callDelayedHelper.StartCoroutine(SmoothIntensity(targetIntensity, duration));

    }

    IEnumerator SmoothIntensity(float targetIntensity, float duration){
        var id = side == PlayerSide.Blue ? _SmokeIntensityBlue : _SmokeIntensityRed;
        float initialIntensity = Shader.GetGlobalFloat(id);
        float elapsed = 0f;
        var interval = new WaitForEndOfFrame();
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float newIntensity = Mathf.Lerp(initialIntensity, targetIntensity, elapsed / duration);
            Shader.SetGlobalFloat(id, newIntensity);
            // print(name + " , newIntensity: " + newIntensity + ", init: " + initialIntensity + ", target:" + targetIntensity + 
            //     ", elapsed: " + elapsed);
            yield return interval;
        }
        Shader.SetGlobalFloat(id, targetIntensity);
    }

    Vector3 direction = Vector3.zero;

    public bool bombPlaced = false;

    // ── Dash ──────────────────────────────────────────────────────────────
    public float dashForce = 28f;
    public float dashCooldown = 2f;
    float dashCooldownLeft = 0f;

    // ── Shield ────────────────────────────────────────────────────────────
    public float shieldDuration = 1.5f;
    public float shieldCooldown = 4f;
    public float shieldReflectForce = 18f;
    float shieldCooldownLeft = 0f;
    public bool shieldActive = false;
    float shieldActiveLeft = 0f;

    // ── Taunt ─────────────────────────────────────────────────────────────
    public float tauntCooldown = 3f;
    float tauntCooldownLeft = 0f;

    public float doubleScoreUntil = 0f;

    void LateUpdate(){
        UpdateBuffs();
    }

    // AI decision delay variables
    public float aiDecisionInterval = 0.3f;// seconds between decisions
    private float aiDecisionTimer = 0f;
    private Vector3 aiLastDirection = Vector3.zero;

    // AI bomb placement variables
    public float aiBombDecisionInterval = 0.5f;// check bomb placement less frequently
    private float aiBombDecisionTimer = 0f;

    void Update(){
        // Cooldown timers (unscaled so they tick during pause-exempt moments, scaled otherwise)
        dashCooldownLeft = Mathf.Max(0, dashCooldownLeft - Time.deltaTime);
        shieldCooldownLeft = Mathf.Max(0, shieldCooldownLeft - Time.deltaTime);
        tauntCooldownLeft = Mathf.Max(0, tauntCooldownLeft - Time.deltaTime);

        // Shield active timer
        if (shieldActive) {
            shieldActiveLeft -= Time.deltaTime;
            if (shieldActiveLeft <= 0f) shieldActive = false;
        }

        // if (device == PlayerControlDevice.Keyboard) {
        //     direction = Vector3.zero;
        //     if (Input.GetKey(keys[0])) direction -= Vector3.forward;
        //     if (Input.GetKey(keys[1])) direction -= Vector3.back;
        //     if (Input.GetKey(keys[2])) direction -= Vector3.left;
        //     if (Input.GetKey(keys[3])) direction -= Vector3.right;
        //
        //     if (!Game.instance.matchRunning) return;
        //
        //     // Bomb
        //     if (!bombPlaced && Input.GetKeyDown(keys[4])) {
        //         Instantiate(BombPrefab, player.transform.position, BombPrefab.transform.rotation);
        //         bombPlaced = true;
        //     }
        //     // Dash
        //     if (Input.GetKeyDown(keys[5]) && dashCooldownLeft <= 0f) {
        //         PerformDash();
        //     }
        //     // Shield
        //     if (Input.GetKeyDown(keys[6]) && shieldCooldownLeft <= 0f) {
        //         ActivateShield();
        //     }
        //     // Taunt
        //     if (Input.GetKeyDown(keys[7]) && tauntCooldownLeft <= 0f) {
        //         PerformTaunt();
        //     }
        // }
        if (device == PlayerControlDevice.Gamepad) {
            var v = movement.ReadValue<Vector2>();
            // Debug.Log("Side: " + side + ", " + v);
            direction = new Vector3(-v.x, 0, -v.y);
            if (!bombPlaced && placeBomb.WasPressedThisFrame() && Game.instance.matchRunning) {
                Instantiate(BombPrefab, player.transform.position, BombPrefab.transform.rotation);
                bombPlaced = true;
            }

            if (!bombPlaced && dash.WasPressedThisFrame() && Game.instance.matchRunning) {
                PerformDash();
            }

            if (!bombPlaced && shield.WasPressedThisFrame() && Game.instance.matchRunning) {
                ActivateShield();
            }

            if (!bombPlaced && taunt.WasPressedThisFrame() && Game.instance.matchRunning) {
                PerformTaunt();
            }

            if (rb.linearVelocity.magnitude > maxSpeed && Vector3.Dot(rb.linearVelocity, direction) > 0) {
                direction = Vector3.ProjectOnPlane(direction, rb.linearVelocity);
            }
        }

    }

    void FixedUpdate(){
        if (!Game.instance.matchRunning) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }
        else {
            CalculateAcc();
            UpdateHeadAnimation();

            if (device == PlayerControlDevice.Keyboard || device == PlayerControlDevice.Gamepad) {
                rb.AddForce(direction.normalized * acc, ForceMode.Acceleration);
            }
            else if (device == PlayerControlDevice.AI) {
                direction = Vector3.zero;

                aiDecisionTimer -= Time.fixedDeltaTime;
                if (aiDecisionTimer <= 0f) {
                    aiDecisionTimer = aiDecisionInterval;
                    // AI logic: chase coin, avoid map edge, avoid enemy
                    GameObject coinObj = Game.instance.coin;
                    GameObject enemyObj = (side == PlayerSide.Blue) ? Game.instance.playerRed.gameObject : Game.instance.playerBlue.gameObject;
                    Vector3 coinPos = coinObj.transform.position;
                    Vector3 enemyPos = enemyObj.transform.position;
                    Vector3 myPos = player.transform.position;

                    // Move towards coin
                    Vector3 toCoin = (coinPos - myPos);
                    toCoin.y = 0;// ignore vertical for movement
                    Vector3 moveDir = toCoin.normalized;

                    // Avoid map edge
                    float safeX = Mathf.Clamp(myPos.x, -9.5f, 9.5f);
                    float safeY = Mathf.Clamp(myPos.y, -5.5f, 5.5f);
                    Vector3 safePos = new Vector3(safeX, safeY, myPos.z);
                    Vector3 awayFromEdge = (safePos - myPos) * 2f;// steer back in if near edge

                    // Avoid enemy if close and between AI and coin
                    float enemyDist = Vector3.Distance(myPos, enemyPos);
                    float coinDist = Vector3.Distance(myPos, coinPos);
                    Vector3 toEnemy = (enemyPos - myPos);
                    toEnemy.y = 0;
                    bool enemyBlocking = (Vector3.Dot(toCoin.normalized, toEnemy.normalized) > 0.7f) && (enemyDist < coinDist) && (enemyDist < 4f);
                    Vector3 avoidEnemy = Vector3.zero;
                    if (enemyBlocking) {
                        // sidestep perpendicular to enemy direction
                        avoidEnemy = Vector3.Cross(toEnemy.normalized, Vector3.up) * 2f;
                    }

                    aiLastDirection = (moveDir + awayFromEdge + avoidEnemy).normalized;
                }

                // AI bomb placement logic
                aiBombDecisionTimer -= Time.fixedDeltaTime;
                if (!bombPlaced && aiBombDecisionTimer <= 0f) {
                    aiBombDecisionTimer = aiBombDecisionInterval;

                    GameObject enemyObj = (side == PlayerSide.Blue) ? Game.instance.playerRed.gameObject : Game.instance.playerBlue.gameObject;
                    Vector3 enemyPos = enemyObj.transform.position;
                    Vector3 myPos = player.transform.position;

                    float enemyDist = Vector3.Distance(myPos, enemyPos);

                    // Place bomb when enemy is at optimal distance (1.5-3.5 units away)
                    // With 0.3s flicker + 0.5s explosion, need closer range for effective push
                    if (enemyDist >= 1.5f && enemyDist <= 7.5f) {
                        // Check if enemy is moving towards us or stationary
                        PlayerController enemy = enemyObj.GetComponent<PlayerController>();
                        Vector3 enemyVelocity = enemy.rb.linearVelocity;
                        Vector3 toMe = (myPos - enemyPos);
                        toMe.y = 0;

                        // Place bomb if enemy is approaching or within good range
                        bool enemyApproaching = Vector3.Dot(enemyVelocity.normalized, toMe.normalized) > 0.3f;
                        bool goodPosition = enemyDist < 2.5f;// closer range = higher priority

                        if (enemyApproaching || goodPosition) {
                            Instantiate(BombPrefab, player.transform.position, BombPrefab.transform.rotation);
                            bombPlaced = true;
                        }
                    }
                }

                direction = aiLastDirection;
                if (rb.linearVelocity.magnitude > maxSpeed && Vector3.Dot(rb.linearVelocity, direction) > 0) {
                    direction = Vector3.ProjectOnPlane(direction, rb.linearVelocity);
                }
                rb.AddForce(direction.normalized * acc, ForceMode.Acceleration);
            }
        }
    }

    // public void UseSkill(){
    //     rb.linearVelocity = direction.normalized * 30f;
    //     rb.angularVelocity = Vector3.zero;
    // }

    // ── Dash ──────────────────────────────────────────────────────────────
    Color SideTint() => side == PlayerSide.Blue ? new Color(0.4f, 0.7f, 1f) : new Color(1f, 0.45f, 0.45f);

    // NotoEmoji yellow-face sprites — passed Color.white so the sprite color shows through.
    const string DashEmoji    = "<sprite=\"NotoEmoji\" name=\"1f60e\">"; // sunglasses
    const string ShieldEmoji  = "<sprite=\"NotoEmoji\" name=\"1f923\">"; // rofl
    const string TauntEmoji   = "<sprite=\"NotoEmoji\" name=\"1f61c\">"; // tongue + wink
    const string KillEmoji    = "<sprite=\"NotoEmoji\" name=\"1f602\">"; // tears of joy
    const string CoinFallback = "<sprite=\"NotoEmoji\" name=\"1f60a\">"; // blush

    public void PerformDash() {
        EmojiReaction.Spawn(player.transform, DashEmoji);
        // Dash towards the opponent; fall back to movement direction or forward
        PlayerController opponent = side == PlayerSide.Blue ? Game.instance.playerRed : Game.instance.playerBlue;
        Vector3 toOpponent = opponent.player.transform.position - player.transform.position;
        toOpponent.y = 0;
        Vector3 d = toOpponent.magnitude > 0.5f ? toOpponent.normalized : (direction.magnitude > 0.1f ? direction.normalized : transform.forward);
        rb.linearVelocity = d * dashForce;
        dashCooldownLeft = dashCooldown;

        SoundSys.PlaySound("Hit").audioSource.volume = 0.5f;
        CameraShake.Shake(0.35f, 0.18f);

        // Smoke burst
        SetSmokeIntensitySmooth(2.5f, 0.05f);
        Tools.CallDelayed(() => SetSmokeIntensitySmooth(0f, 0.3f), 0.2f);

        // Outline flash in player color
        var dashColor = side == PlayerSide.Blue
            ? new Vector4(0.1f, 0.5f, 1.2f, 1) * 12f
            : new Vector4(1.2f, 0.1f, 0.1f, 1) * 12f;
        mr.materials[0].SetVector(Wall.outerColorIndex, dashColor);
        Tools.CallDelayed(() => mr.materials[0].SetVector(Wall.outerColorIndex, Vector4.zero), 0.2f);

        // Screen blink on own side
        Game.instance.BlinkScreen(side == PlayerSide.Blue ? BlinkSide.Left : BlinkSide.Right);
    }

    // ── Shield ────────────────────────────────────────────────────────────
    public void ActivateShield(){
        shieldActive = true;
        shieldActiveLeft = shieldDuration;
        shieldCooldownLeft = shieldCooldown;

        SoundSys.PlaySound("BombPlaced").audioSource.volume = 0.6f;

        // Force UpdateBuffs to re-evaluate on next frame
        lastBuffVisual = BuffVisual.None;

        // Cyan glow
        var mats = mr.materials;
        foreach (var m in mats) m.SetColor(buffColorId, new Color(0.2f, 2f, 2f));

        // Cyan outline
        mr.materials[0].SetVector(Wall.outerColorIndex, new Vector4(0.2f, 1.5f, 2f, 1) * 8f);

        // Smoke burst
        SetSmokeIntensitySmooth(1.8f, 0.08f);
        Tools.CallDelayed(() => SetSmokeIntensitySmooth(0.4f, 0.3f), 0.15f);

        // Screen blink
        Game.instance.BlinkScreen(side == PlayerSide.Blue ? BlinkSide.Left : BlinkSide.Right);

        // When shield expires, clean up and let UpdateBuffs restore correct color
        Tools.CallDelayed(() => {
            if (!shieldActive) {
                mr.materials[0].SetVector(Wall.outerColorIndex, Vector4.zero);
                SetSmokeIntensitySmooth(0f, 0.3f);
                lastBuffVisual = BuffVisual.None;
            }
        }, shieldDuration);
    }

    // ── Taunt ─────────────────────────────────────────────────────────────
    public void PerformTaunt() {
        EmojiReaction.Spawn(player.transform, TauntEmoji);
        tauntCooldownLeft = tauntCooldown;
        PlayerController opponent = side == PlayerSide.Blue ? Game.instance.playerRed : Game.instance.playerBlue;

        SoundSys.PlaySound("Taunt _Robotic Voice").audioSource.volume = 0.5f;

        // Smoke puff on opponent only
        opponent.SetSmokeIntensitySmooth(1.2f, 0.1f);
        Tools.CallDelayed(() => opponent.SetSmokeIntensitySmooth(0f, 0.4f), 0.3f);

        // Triple-flash opponent's screen side
        BlinkSide oppBlink = side == PlayerSide.Blue ? BlinkSide.Right : BlinkSide.Left;
        Game.instance.BlinkScreen(oppBlink);
        Tools.CallDelayed(() => Game.instance.BlinkScreen(oppBlink), 0.15f);
        Tools.CallDelayed(() => Game.instance.BlinkScreen(oppBlink), 0.30f);

        // Fullscreen blink finale
        Tools.CallDelayed(() => Game.instance.BlinkScreen(BlinkSide.Fullscreen), 0.45f);

        // Camera nudge
        CameraShake.Shake(0.2f, 0.12f);
    }

    public void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("DeathZone")) {
            // Killer = the opposite side.
            PlayerController killer = side == PlayerSide.Blue ? Game.instance.playerRed : Game.instance.playerBlue;
            EmojiReaction.Spawn(killer.player.transform, KillEmoji);
            Game.instance.AddScore(side == PlayerSide.Blue ? PlayerSide.Red : PlayerSide.Blue, 5);
            gameObject.SetActive(false);
            ResetPosition();
            SoundSys.PlaySound("Drop").audioSource.volume = 0.5f;
            CameraShake.Shake(0.8f, 0.35f);
            Tools.CallDelayed(() => { gameObject.SetActive(true); }, 1f);
        }
        else if (other.gameObject.name == "Coin") {
            // Personal-touch reaction: show the player's own emoji.
            string sym = string.IsNullOrEmpty(profileEmoji) ? CoinFallback : profileEmoji;
            EmojiReaction.Spawn(player.transform, sym);
            other.gameObject.GetComponent<Coin>().ChangePosition();
            int mult = Time.time < doubleScoreUntil ? 2 : 1;
            Game.instance.AddScore(side, mult);
        }
        else {
            var pickup = other.GetComponentInParent<PickupBase>();
            if (pickup != null) pickup.OnPickup(this);
        }
    }

    public void OnCollisionEnter(Collision collision){
        if (!collision.gameObject.CompareTag("Floor")) {
            mr.materials[0].SetVector(Wall.outerColorIndex,
                side == PlayerSide.Blue
                    ? new Vector4(0.1f, 0.5f, 1.2f, 1) * 6f
                    : 7f * new Vector4(1.2f, 0.1f, 0.1f, 1));
            Tools.CallDelayed(() => mr.materials[0].SetVector(Wall.outerColorIndex, Vector4.zero), 0.1f);
            CameraShake.Shake(0.25f, 0.18f);
        }

        // Shield reflect: push the colliding player away
        if (shieldActive && collision.gameObject.CompareTag("Player")) {
            var other = collision.gameObject.GetComponent<PlayerController>()
                ?? collision.gameObject.GetComponentInParent<PlayerController>();
            if (other != null && other != this) {
                EmojiReaction.Spawn(player.transform, ShieldEmoji);
                Vector3 dir = (other.player.transform.position - player.transform.position).normalized;
                dir.y = 0;
                other.rb.AddForce(dir * shieldReflectForce, ForceMode.Impulse);

                SoundSys.PlaySound("Explosion").audioSource.volume = 0.3f;
                CameraShake.Shake(0.6f, 0.25f);

                // Smoke burst on both players
                SetSmokeIntensitySmooth(2f, 0.05f);
                Tools.CallDelayed(() => SetSmokeIntensitySmooth(0f, 0.2f), 0.15f);
                other.SetSmokeIntensitySmooth(1.5f, 0.05f);
                Tools.CallDelayed(() => other.SetSmokeIntensitySmooth(0f, 0.2f), 0.15f);

                // Outline flash on the reflected player
                var reflectColor = new Vector4(0.2f, 1.5f, 2f, 1) * 10f;
                other.mr.materials[0].SetVector(Wall.outerColorIndex, reflectColor);
                Tools.CallDelayed(() => other.mr.materials[0].SetVector(Wall.outerColorIndex, Vector4.zero), 0.2f);

                // Fullscreen blink
                Game.instance.BlinkScreen(BlinkSide.Fullscreen);
            }
        }
        if (collision.gameObject.CompareTag("Wall")) {
            Wall wall = collision.gameObject.GetComponent<Wall>();
            if (wall == null) {
                wall = collision.gameObject.GetComponentInParent<Wall>();
            }
            wall?.SetOutlineBlink(side);
        }
    }

    // Returns true if dash is pressed for this player
    // public bool IsDashPressed() {
    //     if (device == PlayerControlDevice.Keyboard) {
    //         if (side == PlayerSide.Blue) {
    //             return Input.GetKeyDown(dashKeyBlue);
    //         } else {
    //             return Input.GetKeyDown(dashKeyRed);
    //         }
    //     }
    //     return false;
    // }
}
