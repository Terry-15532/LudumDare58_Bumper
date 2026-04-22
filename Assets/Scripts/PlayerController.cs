using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public enum PlayerControlDevice {
    Keyboard,
    Joystick,
    AI
}

public enum PlayerSide {
    Blue,
    Red
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

    public GameObject BombPrefab;

    public KeyCode[] keys = new KeyCode[5];//up, down, left, right, skill


    public static Vector3 blueBirthPoint = new Vector3(10, 1.7f, 0), redBirthPoint = new Vector3(-10f, 1.7f, 0);

    public static KeyCode[] blueKeys = {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space
    };
    public static KeyCode[] redKeys = {
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Return
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
        if (rb != null) rb.mass = baseMass;
    }

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
        keys = (side == PlayerSide.Blue) ? blueKeys : redKeys;
        baseAcc = acc;
        baseMaxSpeed = maxSpeed;
        baseMass = rb.mass;
        var mats = mr.materials;
        originalMaterialColors = new Color[mats.Length];
        for (int i = 0; i < mats.Length; i++)
            originalMaterialColors[i] = mats[i].GetColor(buffColorId);
        ResetPosition();
        Shader.SetGlobalFloat(side == PlayerSide.Blue ? _SmokeIntensityBlue : _SmokeIntensityRed, 0f);
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
            BuffVisual.SpeedBoost => new Color(2f, 1.5f, 0.2f),
            BuffVisual.DoubleScore => new Color(0.2f, 2f, 0.2f),
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
        if (device == PlayerControlDevice.Keyboard) {
            direction = Vector3.zero;
            if (Input.GetKey(keys[0])) {
                direction -= Vector3.forward;
            }
            if (Input.GetKey(keys[1])) {
                direction -= Vector3.back;
            }
            if (Input.GetKey(keys[2])) {
                direction -= Vector3.left;
            }
            if (Input.GetKey(keys[3])) {
                direction -= Vector3.right;
            }
            if (!bombPlaced && Input.GetKeyDown(keys[4]) && Game.instance.matchRunning) {
                Instantiate(BombPrefab, player.transform.position, BombPrefab.transform.rotation);
                bombPlaced = true;
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

            if (device == PlayerControlDevice.Keyboard) {
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

    public void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("DeathZone")) {
            Game.instance.AddScore(side == PlayerSide.Blue ? PlayerSide.Red : PlayerSide.Blue, 5);
            gameObject.SetActive(false);
            ResetPosition();
            SoundSys.PlaySound("Drop").audioSource.volume = 0.5f;
            Tools.CallDelayed(() => {
                gameObject.SetActive(true);
            }, 1f);
            // Tools.CallDelayed(() => {
            //     gameObject.SetActive(true);
            // }, 0.3f);
        }
        else if (other.gameObject.name == "Coin") {
            other.gameObject.GetComponent<Coin>().ChangePosition();
            int mult = Time.time < doubleScoreUntil ? 2 : 1;
            Game.instance.AddScore(side, mult);
        }
        else {
            var pickup = other.GetComponentInParent<PickupBase>();
            if (pickup != null) {
                pickup.OnPickup(this);
            }
        }
    }

    public void OnCollisionEnter(Collision collision){
        if (!collision.gameObject.CompareTag("Floor")) {
            mr.materials[0].SetVector(Wall.outerColorIndex, side == PlayerSide.Blue ? new Vector4(0.1f, 0.5f, 1.2f, 1) * 6f : 7f * new Vector4(1.2f, 0.1f, 0.1f, 1));
            Tools.CallDelayed(() => mr.materials[0].SetVector(Wall.outerColorIndex, Vector4.zero), 0.1f);
            // CameraShake.Shake(player.transform, collision.GetContact(0).normal, 1f, 0.3f, 0.2f);
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
