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

    // Wire a 3D-capable font asset here for floating emoji reactions; falls back to TMP_Settings.defaultFontAsset.
    public TMP_FontAsset reactionFont;

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

        Cursor.lockState = CursorLockMode.Locked;

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
        // Profile fields get re-populated by NFCStartMatch; clear so non-NFC
        // starts don't carry over a stale name from the previous match.
        playerBlue.profileUid = playerBlue.profileName = playerBlue.profileEmoji = "";
        playerRed.profileUid  = playerRed.profileName  = playerRed.profileEmoji  = "";
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
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.richText = true;
        return tmp;
    }

    public void Start(){
        BuildNFCUI();
        BuildMatchNameLabels();
        if (reactionFont != null) EmojiReaction.font = reactionFont;
        nfcReader = gameObject.AddComponent<NFCReader>();
        Reset();
    }

    // ── Match scoreboard name labels (code-created, sit above each score) ───
    TextMeshProUGUI _blueNameLabel, _redNameLabel;

    void BuildMatchNameLabels() {
        if (scoreTextBlue) _blueNameLabel = CreateNameLabel(scoreTextBlue, "BLUE");
        if (scoreTextRed)  _redNameLabel  = CreateNameLabel(scoreTextRed,  "RED");
    }

    TextMeshProUGUI CreateNameLabel(TextMeshProUGUI anchor, string fallback) {
        if (anchor == null) return null;
        var go = new GameObject(anchor.name + "_Name");
        go.transform.SetParent(anchor.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(700, 90);
        rect.anchoredPosition = new Vector2(0, 10);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = anchor.font;
        if (anchor.fontSharedMaterial) tmp.fontSharedMaterial = anchor.fontSharedMaterial;
        tmp.color     = anchor.color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 40f;
        tmp.fontSizeMax = 90f;
        tmp.text = fallback;
        return tmp;
    }

    void RefreshMatchNameLabels() {
        if (_blueNameLabel) _blueNameLabel.text = BuildScoreboardLabel(playerBlue, "BLUE");
        if (_redNameLabel)  _redNameLabel.text  = BuildScoreboardLabel(playerRed,  "RED");
    }

    static string BuildScoreboardLabel(PlayerController p, string fallback) {
        if (string.IsNullOrEmpty(p.profileName)) return fallback;
        return string.IsNullOrEmpty(p.profileEmoji) ? p.profileName : $"{p.profileEmoji} {p.profileName}";
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
        RefreshMatchNameLabels();
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
            beforeMatchUI?.SetActive(false);
            matchUI?.SetActive(true);
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
        bool blueWon = scores.x > scores.y;
        bool redWon  = scores.y > scores.x;
        if (blueWon) {
            blueWinUI.SetActive(true);
            redWinUI.SetActive(false);
            drawUI.SetActive(false);
        }
        else if (redWon) {
            blueWinUI.SetActive(false);
            redWinUI.SetActive(true);
            drawUI.SetActive(false);
        }
        else {
            drawUI.SetActive(true);
            blueWinUI.SetActive(false);
            redWinUI.SetActive(false);
        }
        // Stats tracking — only counts matches that started via NFC (non-empty uid).
        MatchOutcome blueOutcome = blueWon ? MatchOutcome.Win : redWon ? MatchOutcome.Loss : MatchOutcome.Draw;
        MatchOutcome redOutcome  = redWon  ? MatchOutcome.Win : blueWon ? MatchOutcome.Loss : MatchOutcome.Draw;
        if (!string.IsNullOrEmpty(playerBlue.profileUid))
            ArcadeProfileManager.RecordMatchResult(playerBlue.profileUid, blueOutcome);
        if (!string.IsNullOrEmpty(playerRed.profileUid))
            ArcadeProfileManager.RecordMatchResult(playerRed.profileUid, redOutcome);
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

    public enum NfcSlotState { Empty, PendingNewPlayer, Ready }
    public class NfcSlot {
        public NfcSlotState state = NfcSlotState.Empty;
        public string uid;
        public string name;
        public string emoji;
        public bool   isNewPlayer;
        public void Clear() {
            state = NfcSlotState.Empty;
            uid = null;
            name = null;
            emoji = null;
            isNewPlayer = false;
        }
    }

    public NfcSlot blueSlot = new(), redSlot = new();

    public void EnterNFCMode() {
        Reset();
        mainMenuUI.SetActive(false);
        nfcMode = true;

        // Show players on stage
        playerBlue.gameObject.SetActive(true);
        playerRed.gameObject.SetActive(true);

        blueSlot.Clear();
        redSlot.Clear();

        _nfcTitle.text  = "SWIPE TO JOIN";
        _nfcTitle.color = Color.white;
        RebuildNFCLabels();
        _nfcRoot.SetActive(true);
    }

    // Invoked by NFCReader on every (debounced) card swipe. Unity side does the
    // slot assignment, profile lookup, and new-player registration here.
    public void OnNFCSwipe(string uid) {
        if (!nfcMode || string.IsNullOrEmpty(uid)) return;

        // Already assigned to a slot? Ignore — let the player confirm/reroll.
        if (blueSlot.uid == uid || redSlot.uid == uid) return;

        NfcSlot target = blueSlot.state == NfcSlotState.Empty ? blueSlot
                       : redSlot.state  == NfcSlotState.Empty ? redSlot
                       : null;
        if (target == null) return; // both already filled

        PlayerSide side = (target == blueSlot) ? PlayerSide.Blue : PlayerSide.Red;
        target.uid = uid;

        var profile = ArcadeProfileManager.Get(uid);
        if (profile != null) {
            target.state       = NfcSlotState.Ready;
            target.name        = profile.name;
            target.emoji       = string.IsNullOrEmpty(profile.emoji) ? EmojiPalette.Random() : profile.emoji;
            target.isNewPlayer = false;
            // Backfill emoji for legacy profiles so it persists for next time.
            if (string.IsNullOrEmpty(profile.emoji)) ArcadeProfileManager.SetEmoji(uid, target.emoji);
            NFCPlayerReadyFX(side);
            MaybeStart();
        }
        else {
            target.state       = NfcSlotState.PendingNewPlayer;
            target.name        = NameGenerator.Roll();
            target.emoji       = EmojiPalette.Random();
            target.isNewPlayer = true;
            NFCSwipeFX(side); // gentle feedback; big flash only on confirm
        }
        RebuildNFCLabels();
    }

    // Player-specific key presses during NFC mode: confirm (bomb key) and reroll (dash key).
    public void TryConfirmNewPlayer(PlayerSide side) {
        NfcSlot slot = side == PlayerSide.Blue ? blueSlot : redSlot;
        if (slot.state != NfcSlotState.PendingNewPlayer) return;

        var p = ArcadeProfileManager.Register(slot.uid, slot.name);
        // Honour the emoji rolled in the slot (so reroll picks before confirm stick).
        if (!string.IsNullOrEmpty(slot.emoji)) ArcadeProfileManager.SetEmoji(slot.uid, slot.emoji);
        else slot.emoji = p.emoji;
        slot.state       = NfcSlotState.Ready;
        slot.isNewPlayer = false;
        NFCPlayerReadyFX(side);
        RebuildNFCLabels();
        MaybeStart();
    }

    public void TryRerollNewPlayer(PlayerSide side) {
        NfcSlot slot = side == PlayerSide.Blue ? blueSlot : redSlot;
        // New player: reroll name AND emoji. Returning player: cycle emoji only and persist.
        if (slot.state == NfcSlotState.PendingNewPlayer) {
            slot.name  = NameGenerator.Roll();
            slot.emoji = EmojiPalette.Random();
        }
        else if (slot.state == NfcSlotState.Ready && !string.IsNullOrEmpty(slot.uid)) {
            slot.emoji = EmojiPalette.Random();
            ArcadeProfileManager.SetEmoji(slot.uid, slot.emoji);
        }
        else return;
        var s = SoundSys.PlaySound("Coins");
        if (s != null) s.audioSource.volume = 0.35f;
        RebuildNFCLabels();
    }

    void MaybeStart() {
        if (blueSlot.state == NfcSlotState.Ready && redSlot.state == NfcSlotState.Ready) {
            NFCStartMatch();
        }
    }

    void RebuildNFCLabels() {
        _nfcBlueLabel.text = BuildSlotText("BLUE", blueSlot, "#55AAFF");
        _nfcRedLabel.text  = BuildSlotText("RED",  redSlot,  "#FF5555");
    }

    static string BuildSlotText(string header, NfcSlot slot, string colorHex) {
        string nameLine = string.IsNullOrEmpty(slot.emoji) ? slot.name : $"{slot.emoji} {slot.name}";
        switch (slot.state) {
            case NfcSlotState.Empty:
                return $"{header}\n<size=36><color=#666>WAITING...</color></size>";
            case NfcSlotState.PendingNewPlayer:
                return $"{header}\n<size=40>{nameLine}</size>\n" +
                       $"<size=26><color=#FFCC55>NEW PLAYER</color></size>\n" +
                       $"<size=22><color={colorHex}>[BOMB] CONFIRM  [DASH] REROLL</color></size>";
            case NfcSlotState.Ready:
                var p = ArcadeProfileManager.Get(slot.uid);
                string stats = p != null
                    ? $"\n<size=22>W:{p.wins}  L:{p.losses}  D:{p.draws}   PTS {p.points}</size>"
                    : "";
                return $"{header}\n<size=40>{nameLine}</size>\n" +
                       $"<size=28><color={colorHex}>READY!</color></size>" +
                       stats +
                       $"\n<size=20><color={colorHex}>[DASH] CHANGE EMOJI</color></size>";
        }
        return header;
    }

    // Kept as a public entry point for legacy callers / manual debug flashes.
    public void NFCPlayerReady(PlayerSide side) => NFCPlayerReadyFX(side);

    void NFCPlayerReadyFX(PlayerSide side) {
        if (!nfcMode) return;
        if (side == PlayerSide.Blue) {
            BlinkScreen(BlinkSide.Left);
            SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
            CameraShake.Shake(0.25f, 0.15f);
            playerBlue.SetSmokeIntensitySmooth(1.5f, 0.1f);
            Tools.CallDelayed(() => playerBlue.SetSmokeIntensitySmooth(0f, 0.3f), 0.3f);
        } else {
            BlinkScreen(BlinkSide.Right);
            SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
            CameraShake.Shake(0.25f, 0.15f);
            playerRed.SetSmokeIntensitySmooth(1.5f, 0.1f);
            Tools.CallDelayed(() => playerRed.SetSmokeIntensitySmooth(0f, 0.3f), 0.3f);
        }
    }

    // Small swipe-acknowledgement flash (used when a new card is first detected
    // but the player hasn't confirmed the rolled name yet).
    void NFCSwipeFX(PlayerSide side) {
        if (side == PlayerSide.Blue) {
            BlinkScreen(BlinkSide.Left);
            playerBlue.SetSmokeIntensitySmooth(0.8f, 0.1f);
            Tools.CallDelayed(() => playerBlue.SetSmokeIntensitySmooth(0f, 0.3f), 0.2f);
        } else {
            BlinkScreen(BlinkSide.Right);
            playerRed.SetSmokeIntensitySmooth(0.8f, 0.1f);
            Tools.CallDelayed(() => playerRed.SetSmokeIntensitySmooth(0f, 0.3f), 0.2f);
        }
        var s = SoundSys.PlaySound("Coins");
        if (s != null) s.audioSource.volume = 0.3f;
    }

    public void NFCStartMatch() {
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

            // StartMatch → Reset clears profile fields, so assign after and
            // refresh the scoreboard labels to reflect the NFC-chosen names.
            string blueUid = blueSlot.uid, blueName = blueSlot.name, blueEmoji = blueSlot.emoji;
            string redUid  = redSlot.uid,  redName  = redSlot.name,  redEmoji  = redSlot.emoji;
            StartMatch();
            playerBlue.profileUid   = blueUid;
            playerBlue.profileName  = blueName;
            playerBlue.profileEmoji = blueEmoji;
            playerRed.profileUid    = redUid;
            playerRed.profileName   = redName;
            playerRed.profileEmoji  = redEmoji;
            RefreshMatchNameLabels();
        }, 0.6f);
    }

    public void Update(){
        if (((!matchRunning || !matchStarted) && !selectedSingleMode && selectAction.WasPressedThisFrame())) {
            Reset();
            StopAllCoroutines();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (nfcMode) {
            if (escapeAction.WasPressedThisFrame()) {
                nfcMode = false;
                Reset();
                return;
            }
            // Each side uses its own bomb key (confirm) / dash key (reroll) while
            // the slot is in PendingNewPlayer state. Indexes match PlayerController.keys.
            if (Input.GetKeyDown(PlayerController.blueKeys[4])) TryConfirmNewPlayer(PlayerSide.Blue);
            if (Input.GetKeyDown(PlayerController.blueKeys[5])) TryRerollNewPlayer(PlayerSide.Blue);
            if (Input.GetKeyDown(PlayerController.redKeys[4]))  TryConfirmNewPlayer(PlayerSide.Red);
            if (Input.GetKeyDown(PlayerController.redKeys[5]))  TryRerollNewPlayer(PlayerSide.Red);
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
