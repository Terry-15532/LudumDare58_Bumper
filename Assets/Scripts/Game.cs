using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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
        Time.timeScale = 1f;
    }

    public void Start(){
        Reset();
    }

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

    public void Update(){
        if (Input.GetKeyDown(KeyCode.R)) {
            Reset();
            StopAllCoroutines();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (Input.GetKeyDown(KeyCode.F11)) {
            Screen.fullScreen = !Screen.fullScreen;
        }
        if (matchStarted) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
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
        else if (Input.GetKeyDown(KeyCode.Q)) {
            if (selectedSingleMode) {
                playerBlue.device = PlayerControlDevice.Keyboard;
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
        else if (Input.GetKeyDown(KeyCode.E)) {
            if (selectedSingleMode) {
                playerBlue.device = PlayerControlDevice.Keyboard;
                playerRed.device = PlayerControlDevice.AI;
                playerRed.aiDecisionInterval = 0.05f;
                playerRed.Init();
                StartMatch();
            }
            else {
                playerBlue.device = PlayerControlDevice.Keyboard;
                playerRed.device = PlayerControlDevice.Keyboard;
                playerRed.Init();
                StartMatch();
            }
        }
        else if (Input.GetKeyDown(KeyCode.F) && selectedSingleMode) {
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
