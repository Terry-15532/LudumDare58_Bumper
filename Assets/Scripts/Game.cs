using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum BlinkSide {
    Left,
    Right,
    Fullscreen
}

public class Game : MonoBehaviour {

    private static Game _instance;

    public static Game instance {
        get {
            return _instance;
        }
    }

    public Vector2 scores;
    public float timer, maxTime;
    public PlayerController playerBlue;
    public PlayerController playerRed;
    public TextMeshProUGUI scoreTextBlue, scoreTextRed, timerText, countdownText;
    public GameObject beforeMatchUI, matchUI, mainMenuUI, difficultyUI, coin, leftBlink, rightBlink, fullscreenBlink, matchEndUI, redWinUI, blueWinUI, drawUI;
    public List<Wall> walls = new List<Wall>();
    public GameObject cameraDefaultTarget, playerCombinedTarget;

    public InputActionAsset input;
    public InputAction leftAction, rightAction, selectAction, escapeAction, nfcAction;

    public CinemachineCamera mainVirtualCamera;
    public bool matchStarted = false, matchRunning = false;

    // public static Game CreateGame(){
    //     //create new GameObject with game component
    //     GameObject gameObject = new GameObject("Game");
    //     Game controller = gameObject.AddComponent<Game>();
    //     _instance = controller;
    //     return controller;
    // }

    public void Awake(){
        if (_instance != null && _instance != this) {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;

        var cloned = Instantiate(input).FindActionMap("UIActionMap");
        leftAction = cloned.FindAction("Left");
        rightAction = cloned.FindAction("Right");
        selectAction = cloned.FindAction("Select");
        escapeAction = cloned.FindAction("Escape");
        nfcAction = cloned.FindAction("NFC");
        cloned.devices = new[] {
            Gamepad.all[0], Gamepad.all[1], (InputDevice)Keyboard.current
        };
        cloned.Enable();

    }

    public void BlinkScreen(BlinkSide side){
        if (side == BlinkSide.Left) {
            leftBlink.SetActive(true);
            Tools.CallDelayed(() => leftBlink.SetActive(false), 0.1f);
        }
        else if (side == BlinkSide.Right) {
            rightBlink.SetActive(true);
            Tools.CallDelayed(() => rightBlink.SetActive(false), 0.1f);
        }
        else {
            fullscreenBlink.SetActive(true);
            Tools.CallDelayed(() => fullscreenBlink.SetActive(false), 0.1f);
        }
    }

    public void Reset(){
        playerBlue.ResetPosition();
        playerRed.ResetPosition();
        _instance = this;
        scores = Vector2.zero;
        timer = maxTime;
        matchUI.SetActive(false);
        beforeMatchUI.SetActive(false);
        mainMenuUI.SetActive(true);
        scoreTextBlue.text = scoreTextRed.text = "0";
        walls.ForEach(w => w.Reset());
        coin.SetActive(false);
        playerBlue.gameObject.SetActive(false);
        playerRed.gameObject.SetActive(false);
        mainVirtualCamera.Follow = cameraDefaultTarget.transform;
        matchStarted = false;
        matchRunning = false;
        selectedSingleMode = false;
        difficultyUI.SetActive(false);
        if (_nfcRoot) _nfcRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    // ── NFC UI (code-created) ─────────────────────────────────────────────
    GameObject _nfcRoot;
    TextMeshProUGUI _nfcTitle, _nfcBlueLabel, _nfcRedLabel;

    void BuildNFCUI(){
        // Parent to the same canvas as the other UI panels
        Transform canvas = beforeMatchUI.transform.parent;

        // Root panel — full-screen overlay, same as beforeMatchUI
        _nfcRoot = new GameObject("NFC_UI");
        _nfcRoot.transform.SetParent(canvas, false);
        var rootRect = _nfcRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        // Semi-transparent background
        var bg = _nfcRoot.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);

        // Copy font settings from countdownText
        TMP_FontAsset font = countdownText.font;
        Material fontMat = countdownText.fontSharedMaterial;

        // Title: "SWIPE TO JOIN"
        _nfcTitle = CreateLabel(_nfcRoot.transform, "NFC_Title", font, fontMat,
            72, Color.white, new Vector2(0.5f, 0.7f));
        _nfcTitle.text = "SWIPE TO JOIN";

        // Blue ready label
        _nfcBlueLabel = CreateLabel(_nfcRoot.transform, "NFC_Blue", font, fontMat,
            52, new Color(0.3f, 0.6f, 1f), new Vector2(0.25f, 0.4f));
        _nfcBlueLabel.text = "BLUE\n<size=36><color=#666>WAITING...</color></size>";

        // Red ready label
        _nfcRedLabel = CreateLabel(_nfcRoot.transform, "NFC_Red", font, fontMat,
            52, new Color(1f, 0.35f, 0.35f), new Vector2(0.75f, 0.4f));
        _nfcRedLabel.text = "RED\n<size=36><color=#666>WAITING...</color></size>";

        _nfcRoot.SetActive(false);
    }

    TextMeshProUGUI CreateLabel(Transform parent, string name, TMP_FontAsset font,
        Material fontMat, float size, Color color, Vector2 anchorPos){
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchorPos;
        rect.sizeDelta = new Vector2(500, 200);
        rect.anchoredPosition = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        if (fontMat) tmp.fontSharedMaterial = fontMat;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.richText = true;
        return tmp;
    }

    public void Start(){
        BuildNFCUI();
        nfcReader = gameObject.AddComponent<NFCReader>();
        Reset();
    }

    public NFCReader nfcReader;

    private Coroutine timerCoroutine;

    private IEnumerator TimerCoroutine(){
        timer = maxTime;
        while (true) {
            try {
                if (matchRunning) {
                    timer -= 1f;
                    if (timer < 0) {
                        timer = 0;
                        EndMatch();
                    }
                    if (timer < 10) {
                        timerText.color = new Color(1f, 0.2f, 0.2f);
                    }
                    else {
                        timerText.color = Color.white;
                    }
                    if (timer is < 3 and >= 2) {
                        SoundSys.PlaySound("Countdown", volume: 1f).audioSource.volume = 0.15f;
                    }
                    if (timer < 5) {
                        BlinkScreen(BlinkSide.Fullscreen);
                        // SoundSys.PlaySound("Countdown").audioSource.volume = 0.1f;
                    }
                    int minutes = Mathf.FloorToInt(timer / 60);
                    int seconds = Mathf.FloorToInt(timer % 60);
                    timerText.text = $"{minutes:00}:{seconds:00}";
                }
                yield return new WaitForSeconds(1f);
            }
            finally { }
        }
    }

    public void StartMatch(){
        Reset();
        mainMenuUI.SetActive(false);
        beforeMatchUI.SetActive(true);
        coin.SetActive(false);
        playerBlue.gameObject.SetActive(true);
        playerRed.gameObject.SetActive(true);
        // walls.ForEach(w => w.gameObject.SetActive(true));
        countdownText.color = new Color(1f, 0.7f, 0f);
        countdownText.text = "3";
        matchStarted = true;
        mainVirtualCamera.Follow = playerCombinedTarget.transform;

        SoundSys.PlaySound("Countdown", volume: 0.3f).audioSource.volume = 0.3f;

        for (int i = 3; i >= 0; i--) {
            // walls.ForEach(w => w.gameObject.SetActive(false));
            int count = i;
            Tools.CallDelayed(() => {
                if (count > 0) {
                    countdownText.text = count.ToString();
                }
                else {
                    countdownText.text = "GO";
                    countdownText.color = new Color(0, 1f, 0.3f);
                    playerBlue.SetSmokeIntensitySmooth(2f, 0.1f);
                    playerRed.SetSmokeIntensitySmooth(2f, 0.1f);
                    SoundSys.PlaySound("Cheer", volume: 1f).audioSource.volume = 0.1f;

                }
            }, 3 - i);
        }

        Tools.CallDelayed(() => {
            matchRunning = true;
            beforeMatchUI.SetActive(false);
            matchUI.SetActive(true);
            coin.transform.position = new Vector3(0f, 2.5f + Mathf.Sin(Time.time * 2) * 0.15f, 0);
            coin.SetActive(true);
            Tools.CallDelayed(() => {
                playerBlue.SetSmokeIntensitySmooth(0f, 0.3f);
                playerRed.SetSmokeIntensitySmooth(0f, 0.3f);
            }, 0.3f);
            // Start the timer coroutine
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(TimerCoroutine());
        }, 4.1f);
    }

    public void AddScore(PlayerSide side, int increment){
        if (side == PlayerSide.Blue) {
            scores.x += increment;
            scoreTextBlue.text = scores.x.ToString();
            playerBlue.SetSmokeIntensitySmooth(1.2f, 0.2f);
            BlinkScreen(BlinkSide.Left);
            Tools.CallDelayed(() => playerBlue.SetSmokeIntensitySmooth(0f, 0.2f), 0.3f * increment);
        }
        else {
            scores.y += increment;
            scoreTextRed.text = scores.y.ToString();
            playerRed.SetSmokeIntensitySmooth(1.7f, 0.2f);
            BlinkScreen(BlinkSide.Right);
            Tools.CallDelayed(() => playerRed.SetSmokeIntensitySmooth(0f, 0.2f), 0.3f * increment);
        }
    }

    public void EndMatch(){
        SoundSys.PlaySound("Cheer", volume: 0.15f).audioSource.volume = 0.1f;
        mainVirtualCamera.Follow = cameraDefaultTarget.transform;
        if (scores.x > scores.y) {
            blueWinUI.SetActive(true);
            redWinUI.SetActive(false);
            drawUI.SetActive(false);
        }
        else if (scores.y > scores.x) {
            blueWinUI.SetActive(false);
            redWinUI.SetActive(true);
            drawUI.SetActive(false);
        }
        else {
            drawUI.SetActive(true);
            blueWinUI.SetActive(false);
            redWinUI.SetActive(false);
        }
        matchRunning = false;

        playerBlue.gameObject.SetActive(true);
        playerBlue.ResetPosition();
        playerBlue.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.enabled = false);
        playerBlue.GetComponentInChildren<TrailRenderer>().enabled = false;
        playerRed.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.enabled = false);
        playerRed.GetComponentInChildren<TrailRenderer>().enabled = false;
        playerRed.gameObject.SetActive(true);
        playerRed.ResetPosition();
        matchEndUI.SetActive(true);
        mainMenuUI.SetActive(false);

        playerBlue.SetSmokeIntensitySmooth(1f, 0.2f);
        playerRed.SetSmokeIntensitySmooth(1f, 0.2f);
        Tools.CallDelayed(() => {
            playerBlue.SetSmokeIntensitySmooth(0f, 0.4f);
            playerRed.SetSmokeIntensitySmooth(0f, 0.4f);
        }, 2f);

        Tools.CallDelayed(() => {
            matchStarted = false;
            playerBlue.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.enabled = true);
            playerBlue.GetComponentInChildren<TrailRenderer>().enabled = true;

            playerRed.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.enabled = true);
            playerRed.GetComponentInChildren<TrailRenderer>().enabled = true;
            playerBlue.gameObject.SetActive(false);
            playerRed.gameObject.SetActive(false);
            matchEndUI.SetActive(false);
            mainMenuUI.SetActive(true);
            Reset();
        }, 3f);
        coin.SetActive(false);
        beforeMatchUI.SetActive(false);
        matchUI.SetActive(false);

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
    }

    [FormerlySerializedAs("selectedMode")]
    public bool selectedSingleMode = false;

    // ── NFC selection flow ────────────────────────────────────────────────
    public bool nfcMode = false;

    public void EnterNFCMode(){
        Reset();
        mainMenuUI.SetActive(false);
        nfcMode = true;

        // Show players on stage
        playerBlue.gameObject.SetActive(true);
        playerRed.gameObject.SetActive(true);

        // Reset labels
        _nfcBlueLabel.text = "BLUE\n<size=36><color=#666>WAITING...</color></size>";
        _nfcRedLabel.text = "RED\n<size=36><color=#666>WAITING...</color></size>";
        _nfcTitle.text = "SWIPE TO JOIN";
        _nfcTitle.color = Color.white;
        _nfcRoot.SetActive(true);
    }

    public void NFCPlayerReady(PlayerSide side){
        if (!nfcMode) return;
        if (side == PlayerSide.Blue) {
            _nfcBlueLabel.text = "BLUE\n<size=36><color=#55AAFF>READY!</color></size>";
            BlinkScreen(BlinkSide.Left);
            SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
            CameraShake.Shake(0.25f, 0.15f);
            playerBlue.SetSmokeIntensitySmooth(1.5f, 0.1f);
            Tools.CallDelayed(() => playerBlue.SetSmokeIntensitySmooth(0f, 0.3f), 0.3f);
        }
        else {
            _nfcRedLabel.text = "RED\n<size=36><color=#FF5555>READY!</color></size>";
            BlinkScreen(BlinkSide.Right);
            SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
            CameraShake.Shake(0.25f, 0.15f);
            playerRed.SetSmokeIntensitySmooth(1.5f, 0.1f);
            Tools.CallDelayed(() => playerRed.SetSmokeIntensitySmooth(0f, 0.3f), 0.3f);
        }
    }

    public void NFCStartMatch(){
        if (!nfcMode) return;

        // Brief "GO!" flash before hiding NFC UI
        _nfcTitle.text = "GO!";
        _nfcTitle.color = new Color(0, 1f, 0.3f);
        BlinkScreen(BlinkSide.Fullscreen);
        CameraShake.Shake(0.4f, 0.2f);
        SoundSys.PlaySound("Countdown", volume: 0.3f).audioSource.volume = 0.3f;

        Tools.CallDelayed(() => {
            nfcMode = false;
            _nfcRoot.SetActive(false);

            playerBlue.device = PlayerControlDevice.Gamepad;
            playerRed.device = PlayerControlDevice.Gamepad;
            playerBlue.Init();
            playerRed.Init();
            StartMatch();
        }, 0.6f);
    }

    public void Update(){
        if (!matchRunning && escapeAction.WasPressedThisFrame()) {
            Reset();
            StopAllCoroutines();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (nfcMode) {
            if (escapeAction.WasPressedThisFrame()) {
                nfcMode = false;
                Reset();
            }
            return;
        }
        if (matchStarted) {
            if (escapeAction.WasPressedThisFrame()) {
                matchRunning = !matchRunning;
                timerText.text = "PAUSED";
                timerText.ForceMeshUpdate();
                if (!matchRunning) {
                    Time.timeScale = 0;
                }
                else {
                    Time.timeScale = 1;
                    int minutes = Mathf.FloorToInt(timer / 60);
                    int seconds = Mathf.FloorToInt(timer % 60);
                    timerText.text = $"{minutes:00}:{seconds:00}";
                }
            }
        }
        else if (leftAction.WasPressedThisFrame()) {
            if (selectedSingleMode) {
                playerBlue.device = PlayerControlDevice.Gamepad;
                playerRed.device = PlayerControlDevice.AI;
                playerRed.aiDecisionInterval = 0.18f;
                playerRed.Init();
                StartMatch();
                difficultyUI.SetActive(false);
            }
            else {
                selectedSingleMode = true;
                difficultyUI.SetActive(true);
            }
        }
        else if (rightAction.WasPressedThisFrame()) {
            if (selectedSingleMode) {
                playerBlue.device = PlayerControlDevice.Gamepad;
                playerRed.device = PlayerControlDevice.AI;
                playerRed.aiDecisionInterval = 0.05f;
                playerRed.Init();
                StartMatch();
            }
            else {
                playerBlue.device = PlayerControlDevice.Gamepad;
                playerRed.device = PlayerControlDevice.Gamepad;
                playerRed.Init();
                StartMatch();
            }
        }
        else if (nfcAction.WasPressedThisFrame() && !selectedSingleMode) {
            EnterNFCMode();
        }
        else if (selectAction.WasPressedThisFrame() && selectedSingleMode) {
            playerRed.device = PlayerControlDevice.AI;
            playerRed.aiDecisionInterval = 0.01f;
            playerRed.Init();
            playerBlue.device = PlayerControlDevice.AI;
            playerBlue.aiDecisionInterval = 0.01f;
            playerBlue.Init();
            StartMatch();
        }
    }


}
