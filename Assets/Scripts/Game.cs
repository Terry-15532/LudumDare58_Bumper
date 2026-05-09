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
    public InputAction leftAction, rightAction, selectAction, escapeAction;

    public CinemachineCamera mainVirtualCamera;
    public bool matchStarted = false, matchRunning = false;

    public TMP_FontAsset reactionFont;

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
        cloned.devices = new[] {
            Joystick.all[0], Joystick.all[1], (InputDevice)Keyboard.current
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

        playerBlue.profileUid = playerBlue.profileName = playerBlue.profileEmoji = "";
        playerRed.profileUid = playerRed.profileName = playerRed.profileEmoji = "";

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
        PickupSpawner.instance.Reset();

        nfcMode = false;
        nfcModeArmed = false;
        blueSlot.Clear();
        redSlot.Clear();

        if (_nfcRoot) _nfcRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    GameObject _nfcRoot;
    TextMeshProUGUI _nfcTitle, _nfcBlueLabel, _nfcRedLabel;

    void BuildNFCUI(){
        Transform canvas = beforeMatchUI.transform.parent;

        _nfcRoot = new GameObject("NFC_UI");
        _nfcRoot.transform.SetParent(canvas, false);
        var rootRect = _nfcRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        var bg = _nfcRoot.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);

        TMP_FontAsset font = countdownText.font;
        Material fontMat = countdownText.fontSharedMaterial;

        _nfcTitle = CreateLabel(
            _nfcRoot.transform,
            "NFC_Title",
            font,
            fontMat,
            100,
            Color.white,
            new Vector2(0.5f, 0.7f),
            new Vector2(0f, 100f)
        );
        _nfcTitle.text = "SWIPE TO JOIN";

        _nfcBlueLabel = CreateLabel(
            _nfcRoot.transform,
            "NFC_Blue",
            font,
            fontMat,
            80,
            new Color(0.3f, 0.6f, 1f),
            new Vector2(0.25f, 0.4f),
            new Vector2(0f, 100f)
        );
        _nfcBlueLabel.text = BuildSlotText(blueSlot);
        _nfcBlueLabel.fontStyle |= FontStyles.Bold | FontStyles.SmallCaps;

        _nfcRedLabel = CreateLabel(
            _nfcRoot.transform,
            "NFC_Red",
            font,
            fontMat,
            80,
            new Color(1f, 0.35f, 0.35f),
            new Vector2(0.75f, 0.4f),
            new Vector2(0f, 100f)
        );
        _nfcRedLabel.text = BuildSlotText(redSlot);
        _nfcRedLabel.fontStyle |= FontStyles.Bold | FontStyles.SmallCaps;

        _nfcRoot.SetActive(false);
    }

    TextMeshProUGUI CreateLabel(
        Transform parent,
        string name,
        TMP_FontAsset font,
        Material fontMat,
        float size,
        Color color,
        Vector2 anchorPos,
        Vector2 anchoredPos
    ){
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchorPos;
        rect.sizeDelta = new Vector2(900, 300);
        rect.anchoredPosition = anchoredPos;

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

    TextMeshProUGUI _blueNameLabel, _redNameLabel;

    void BuildMatchNameLabels(){
        if (scoreTextBlue) _blueNameLabel = CreateNameLabel(scoreTextBlue, "BLUE");
        if (scoreTextRed) _redNameLabel = CreateNameLabel(scoreTextRed, "RED");
    }

    TextMeshProUGUI CreateNameLabel(TextMeshProUGUI anchor, string fallback){
        if (anchor == null) return null;
        var go = new GameObject(anchor.name + "_Name");
        go.transform.SetParent(anchor.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(700, 90);
        rect.anchoredPosition = new Vector2(0, 50);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = anchor.font;
        if (anchor.fontSharedMaterial) tmp.fontSharedMaterial = anchor.fontSharedMaterial;
        tmp.color = anchor.color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 40f;
        tmp.fontSizeMax = 90f;
        tmp.text = fallback;
        return tmp;
    }

    void RefreshMatchNameLabels(){
        if (_blueNameLabel) _blueNameLabel.text = BuildScoreboardLabel(playerBlue, "BLUE");
        if (_redNameLabel) _redNameLabel.text = BuildScoreboardLabel(playerRed, "RED");
    }

    static string BuildScoreboardLabel(PlayerController p, string fallback){
        if (string.IsNullOrEmpty(p.profileName)) return fallback;
        return p.profileName;
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
        Commentator.instance?.TriggerEvent(CommentaryEvent.GameStart, 3f);
        RefreshMatchNameLabels();
        mainMenuUI.SetActive(false);
        beforeMatchUI.SetActive(true);
        coin.SetActive(false);
        playerBlue.gameObject.SetActive(true);
        playerRed.gameObject.SetActive(true);
        countdownText.color = new Color(1f, 0.7f, 0f);
        countdownText.text = "3";
        matchStarted = true;
        mainVirtualCamera.Follow = playerCombinedTarget.transform;

        SoundSys.PlaySound("Countdown", volume: 0.3f).audioSource.volume = 0.3f;

        for (int i = 3; i >= 0; i--) {
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
            Commentator.instance?.TriggerEvent(CommentaryEvent.GameStart, 3f);
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
            if (Commentator.instance != null) {
                if (increment == 3) Commentator.instance.TriggerEvent(CommentaryEvent.BlueKill, 2f);
            }
        }
        else {
            scores.y += increment;
            scoreTextRed.text = scores.y.ToString();
            playerRed.SetSmokeIntensitySmooth(1.7f, 0.2f);
            BlinkScreen(BlinkSide.Right);
            Tools.CallDelayed(() => playerRed.SetSmokeIntensitySmooth(0f, 0.2f), 0.3f * increment);
            if (Commentator.instance != null) {
                if (increment == 3) Commentator.instance.TriggerEvent(CommentaryEvent.RedKill, 2f);
            }
        }
    }

    public void EndMatch(){
        SoundSys.PlaySound("Cheer", volume: 0.15f).audioSource.volume = 0.1f;
        mainVirtualCamera.Follow = cameraDefaultTarget.transform;
        Commentator.instance?.TriggerEvent(CommentaryEvent.Victory, 10f);
        bool blueWon = scores.x > scores.y;
        bool redWon = scores.y > scores.x;

        coin.gameObject.SetActive(false);
        PickupSpawner.instance.Reset();

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

        MatchOutcome blueOutcome = blueWon ? MatchOutcome.Win : redWon ? MatchOutcome.Loss : MatchOutcome.Draw;
        MatchOutcome redOutcome = redWon ? MatchOutcome.Win : blueWon ? MatchOutcome.Loss : MatchOutcome.Draw;
        if (!string.IsNullOrEmpty(playerBlue.profileUid))
            ArcadeProfileManager.RecordMatchResult(playerBlue.profileUid, blueOutcome);
        if (!string.IsNullOrEmpty(playerRed.profileUid))
            ArcadeProfileManager.RecordMatchResult(playerRed.profileUid, redOutcome);

        matchRunning = false;

        playerBlue.gameObject.SetActive(true);
        playerBlue.ResetPosition();
        playerBlue.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.enabled = false);
        playerBlue.GetComponentsInChildren<SpriteRenderer>().ToList().ForEach(sr => sr.enabled = false);
        playerBlue.GetComponentInChildren<TrailRenderer>().enabled = false;

        playerRed.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.enabled = false);
        playerRed.GetComponentsInChildren<SpriteRenderer>().ToList().ForEach(sr => sr.enabled = false);
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
        }, 4f);

        Tools.CallDelayed(() => {
            matchStarted = false;
            playerBlue.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.enabled = true);
            playerBlue.GetComponentsInChildren<SpriteRenderer>().ToList().ForEach(sr => sr.enabled = true);
            playerBlue.GetComponentInChildren<TrailRenderer>().enabled = true;

            playerRed.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.enabled = true);
            playerRed.GetComponentsInChildren<SpriteRenderer>().ToList().ForEach(sr => sr.enabled = true);
            playerRed.GetComponentInChildren<TrailRenderer>().enabled = true;
            playerBlue.gameObject.SetActive(false);
            playerRed.gameObject.SetActive(false);
            matchEndUI.SetActive(false);
            mainMenuUI.SetActive(true);
            Reset();
        }, 5f);

        coin.SetActive(false);
        beforeMatchUI.SetActive(false);
        matchUI.SetActive(false);

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
    }

    [FormerlySerializedAs("selectedMode")]
    public bool selectedSingleMode = false;

    public bool nfcMode = false;
    public bool nfcModeArmed = false;

    public enum NfcSlotState { Empty, PendingNewPlayer, PendingReady, Ready }

    public class NfcSlot {
        public NfcSlotState state {
            get;
            set;
        }
        public string uid;
        public string name;
        public string emoji;
        public bool isNewPlayer;

        public void Clear(){
            state = NfcSlotState.Empty;
            uid = null;
            name = null;
            emoji = null;
            isNewPlayer = false;
        }
    }

    public NfcSlot blueSlot = new(), redSlot = new();

    public void BeginNFCModeFromMenu(){
        nfcModeArmed = true;
        EnterNFCMode();
    }

    public void EnterNFCMode(){
        if (!nfcModeArmed) return;
        nfcModeArmed = false;

        Reset();
        mainMenuUI.SetActive(false);
        nfcMode = true;

        blueSlot.Clear();
        redSlot.Clear();

        _nfcTitle.text = "SWIPE TO JOIN";
        _nfcTitle.color = Color.white;
        _nfcTitle.fontStyle |= FontStyles.Bold;
        RebuildNFCLabels();
        _nfcRoot.SetActive(true);
    }

    public void OnNFCSwipe(string uid){
        if (!nfcMode || string.IsNullOrEmpty(uid)) return;

        if (blueSlot.uid == uid || redSlot.uid == uid) return;

        NfcSlot target = blueSlot.state == NfcSlotState.Empty ? blueSlot
            : redSlot.state == NfcSlotState.Empty ? redSlot
            : null;
        if (target == null) return;

        PlayerSide side = (target == blueSlot) ? PlayerSide.Blue : PlayerSide.Red;
        target.uid = uid;

        var profile = ArcadeProfileManager.Get(uid);
        if (profile != null) {
            target.state = NfcSlotState.PendingReady;
            target.name = profile.name;
            target.emoji = string.IsNullOrEmpty(profile.emoji) ? EmojiPalette.Random() : profile.emoji;
            target.isNewPlayer = false;

            if (string.IsNullOrEmpty(profile.emoji)) ArcadeProfileManager.SetEmoji(uid, target.emoji);
            NFCPlayerReadyFX(side);

            Commentator.instance?.AnnouncePlayerIntro(
                target.name,
                profile.wins,
                profile.losses,
                profile.matchesPlayed,
                false
            );
        }
        else {
            target.state = NfcSlotState.PendingNewPlayer;
            target.name = NameGenerator.Roll();
            target.emoji = EmojiPalette.Random();
            target.isNewPlayer = true;
            NFCSwipeFX(side);
        }

        RebuildNFCLabels();
    }

    public void TryConfirmNewPlayer(PlayerSide side){
        NfcSlot slot = side == PlayerSide.Blue ? blueSlot : redSlot;
        if (slot.state != NfcSlotState.PendingNewPlayer) return;

        var p = ArcadeProfileManager.Register(slot.uid, slot.name);

        if (!string.IsNullOrEmpty(slot.emoji)) ArcadeProfileManager.SetEmoji(slot.uid, slot.emoji);
        else slot.emoji = p.emoji;

        slot.state = NfcSlotState.PendingReady;
        slot.isNewPlayer = false;

        Commentator.instance?.AnnouncePlayerIntro(
            slot.name,
            p.wins,
            p.losses,
            p.matchesPlayed,
            true
        );

        RebuildNFCLabels();
    }

    public void TryToggleReady(PlayerSide side){
        NfcSlot slot = side == PlayerSide.Blue ? blueSlot : redSlot;

        if (slot.state == NfcSlotState.PendingReady) {
            slot.state = NfcSlotState.Ready;
            NFCPlayerReadyFX(side);
            RebuildNFCLabels();
            MaybeStart();
            return;
        }

        if (slot.state == NfcSlotState.Ready) {
            if (string.IsNullOrEmpty(slot.uid)) {
                slot.Clear();
            }
            else {
                slot.state = NfcSlotState.PendingReady;
            }
            RebuildNFCLabels();
        }
    }

    public void TryJoinAnonymous(PlayerSide side){
        NfcSlot slot = side == PlayerSide.Blue ? blueSlot : redSlot;
        if (slot.state != NfcSlotState.Empty) return;

        slot.uid = "-1";
        slot.name = "Anonymous Player";
        slot.emoji = EmojiPalette.Random();
        slot.isNewPlayer = false;
        slot.state = NfcSlotState.PendingReady;


        Commentator.instance?.AnnouncePlayerIntro(slot.name, 0, 0, 0, true);

        NFCPlayerReadyFX(side);
        RebuildNFCLabels();
        MaybeStart();
    }

    public void TryRerollNewPlayer(PlayerSide side){
        NfcSlot slot = side == PlayerSide.Blue ? blueSlot : redSlot;

        if (slot.state == NfcSlotState.PendingNewPlayer) {
            slot.name = NameGenerator.Roll();
            slot.emoji = EmojiPalette.Random();
        }
        else if (slot.state == NfcSlotState.PendingReady && !string.IsNullOrEmpty(slot.uid)) {
            slot.emoji = EmojiPalette.Random();
            ArcadeProfileManager.SetEmoji(slot.uid, slot.emoji);
        }
        else return;

        var s = SoundSys.PlaySound("Coins");
        if (s != null) s.audioSource.volume = 0.35f;
        RebuildNFCLabels();
    }

    void MaybeStart(){
        if (blueSlot.state == NfcSlotState.Ready && redSlot.state == NfcSlotState.Ready) {
            NFCStartMatch();
        }
    }

    void RebuildNFCLabels(){
        _nfcBlueLabel.text = BuildSlotText(blueSlot);
        _nfcRedLabel.text = BuildSlotText(redSlot);
    }

    static string BuildDisplayName(NfcSlot slot){
        string emoji = string.IsNullOrEmpty(slot.emoji) ? "" : $"{slot.emoji} ";
        return $"<size=100>{emoji}{slot.name}</size>";
    }

    static string BuildWinLossLine(NfcSlot slot){
        if (string.IsNullOrEmpty(slot.uid)) return "";
        var p = ArcadeProfileManager.Get(slot.uid);
        if (p == null) return "";
        return $"\n<size=100><color=#FFFFFF>W: {p.wins} L:{p.losses}</color></size>";
    }

    static string BuildReadyLine(bool ready){
        return ready
            ? "<size=72><color=#00FF00>[√]</color> <color=#00FF00>Ready</color></size>"
            : "<size=72><color=#00FF00>[ ]</color> <color=#BFBFBF>Ready</color></size>";
    }

    static string BuildSlotText(NfcSlot slot){
        switch (slot.state) {
            case NfcSlotState.Empty:
                return "<size=80><color=#FFFFFF>Waiting...</color></size>\n\n"
                    + "<size=50><color=#AAAAAA>Or:<color=#00FF00> Join Anonymously</color></size>";

            case NfcSlotState.PendingNewPlayer:
                return
                    $"{BuildDisplayName(slot)}\n" +
                    $"<size=80><color=#FFFFFF>Select Username</color></size>\n" +
                    $"<size=60><color=#00FF00>CONFIRM</color>  <color=#FF5555>REROLL</color></size>";

            case NfcSlotState.PendingReady:
                return
                    $"{BuildDisplayName(slot)}\n" +
                    $"{BuildReadyLine(false)}" +
                    $"{BuildWinLossLine(slot)}";

            case NfcSlotState.Ready:
                return
                    $"{BuildDisplayName(slot)}\n" +
                    $"{BuildReadyLine(true)}" +
                    $"{BuildWinLossLine(slot)}";
        }

        return "<size=80><color=#555555>Waiting...</color></size>\n"
            + "<size=60>Or:<color=#00FF00> Join Anonymously</color></size>";
    }

    public void NFCPlayerReady(PlayerSide side) => NFCPlayerReadyFX(side);

    void NFCPlayerReadyFX(PlayerSide side){
        if (!nfcMode) return;
        if (side == PlayerSide.Blue) {
            BlinkScreen(BlinkSide.Left);
            SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
            CameraShake.Shake(0.25f, 0.15f);
            playerBlue.SetSmokeIntensitySmooth(1.5f, 0.1f);
            Tools.CallDelayed(() => playerBlue.SetSmokeIntensitySmooth(0f, 0.3f), 0.3f);
        }
        else {
            BlinkScreen(BlinkSide.Right);
            SoundSys.PlaySound("Coins").audioSource.volume = 0.5f;
            CameraShake.Shake(0.25f, 0.15f);
            playerRed.SetSmokeIntensitySmooth(1.5f, 0.1f);
            Tools.CallDelayed(() => playerRed.SetSmokeIntensitySmooth(0f, 0.3f), 0.3f);
        }
    }

    void NFCSwipeFX(PlayerSide side){
        if (side == PlayerSide.Blue) {
            BlinkScreen(BlinkSide.Left);
            playerBlue.SetSmokeIntensitySmooth(0.8f, 0.1f);
            Tools.CallDelayed(() => playerBlue.SetSmokeIntensitySmooth(0f, 0.3f), 0.2f);
        }
        else {
            BlinkScreen(BlinkSide.Right);
            playerRed.SetSmokeIntensitySmooth(0.8f, 0.1f);
            Tools.CallDelayed(() => playerRed.SetSmokeIntensitySmooth(0f, 0.3f), 0.2f);
        }
        var s = SoundSys.PlaySound("Coins");
        if (s != null) s.audioSource.volume = 0.3f;
    }

    public void NFCStartMatch(){
        if (!nfcMode) return;

        _nfcTitle.text = "GO!";
        _nfcTitle.color = new Color(0, 1f, 0.3f);
        BlinkScreen(BlinkSide.Fullscreen);
        CameraShake.Shake(0.4f, 0.2f);
        // SoundSys.PlaySound("Countdown", volume: 0.3f).audioSource.volume = 0.3f;

        Tools.CallDelayed(() => {
            nfcMode = false;
            _nfcRoot.SetActive(false);

            playerBlue.device = PlayerControlDevice.Gamepad;
            playerRed.device = PlayerControlDevice.Gamepad;
            playerBlue.Init();
            playerRed.Init();

            string blueUid = blueSlot.uid, blueName = blueSlot.name, blueEmoji = blueSlot.emoji;
            string redUid = redSlot.uid, redName = redSlot.name, redEmoji = redSlot.emoji;

            StartMatch();

            playerBlue.profileUid = blueUid;
            playerBlue.profileName = blueName;
            playerBlue.profileEmoji = blueEmoji;
            playerRed.profileUid = redUid;
            playerRed.profileName = redName;
            playerRed.profileEmoji = redEmoji;
            RefreshMatchNameLabels();
        }, 0.6f);
    }

    public void Update(){
        if (!matchRunning && !countdownText.IsActive() && selectAction.WasPressedThisFrame()) {
            Reset();
            StopAllCoroutines();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (nfcMode && !matchStarted && !countdownText.IsActive()) {
            if (escapeAction.WasPressedThisFrame()) {
                nfcMode = false;
                Reset();
                return;
            }

            if (blueSlot.state == NfcSlotState.Empty && playerBlue.placeBomb.WasPressedThisFrame()) TryJoinAnonymous(PlayerSide.Blue);
            else if (redSlot.state == NfcSlotState.Empty && playerRed.placeBomb.WasPressedThisFrame()) TryJoinAnonymous(PlayerSide.Red);
            else if (playerBlue.placeBomb.WasPressedThisFrame()) {
                if (blueSlot.state == NfcSlotState.PendingNewPlayer) TryConfirmNewPlayer(PlayerSide.Blue);
                else TryToggleReady(PlayerSide.Blue);
            }
            else if (playerRed.placeBomb.WasPressedThisFrame()) {
                if (redSlot.state == NfcSlotState.PendingNewPlayer) TryConfirmNewPlayer(PlayerSide.Red);
                else TryToggleReady(PlayerSide.Red);
            }

            if (playerBlue.dash.WasPressedThisFrame()) TryRerollNewPlayer(PlayerSide.Blue);
            if (playerRed.dash.WasPressedThisFrame()) TryRerollNewPlayer(PlayerSide.Red);


            return;
        }

        if (!matchStarted && !countdownText.IsActive() && rightAction.WasPressedThisFrame()) {
            BeginNFCModeFromMenu();
            return;
        }
        else if (!matchStarted && leftAction.WasPressedThisFrame() && !countdownText.IsActive()) {
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
        else if (!matchStarted && !countdownText.IsActive() && selectAction.WasPressedThisFrame() && selectedSingleMode) {
            playerRed.device = PlayerControlDevice.AI;
            playerRed.aiDecisionInterval = 0.01f;
            playerRed.Init();
            playerBlue.device = PlayerControlDevice.AI;
            playerBlue.aiDecisionInterval = 0.01f;
            playerBlue.Init();
            StartMatch();
        }

        if (matchStarted && !countdownText.IsActive()) {
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
    }
}
