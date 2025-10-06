using System;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class Game : MonoBehaviour{

    private static Game _instance;

    public static Game instance{
        get{
            return _instance;
        }
    }

    public Vector2 scores;
    public float timer;
    public PlayerController playerBlue;
    public PlayerController playerRed;
    public TextMeshProUGUI scoreTextBlue, scoreTextRed, timerText, countdownText;
    public GameObject beforeMatchUI, matchUI, mainMenuUI, coin;

    // public CinemachineCamera mainVirtualCamera;
    public bool matchStarted = false, matchRunning = false;

    // public static Game CreateGame(){
    //     //create new GameObject with game component
    //     GameObject gameObject = new GameObject("Game");
    //     Game controller = gameObject.AddComponent<Game>();
    //     _instance = controller;
    //     return controller;
    // }

    public void Awake(){
        if (_instance != null && _instance != this){
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
    }

    public void Reset(){
        playerBlue.ResetPosition();
        playerRed.ResetPosition();
        _instance = this;
        scores = Vector2.zero;
        timer = 180;
        matchUI.SetActive(false);
        beforeMatchUI.SetActive(false);
        mainMenuUI.SetActive(true);
        scoreTextBlue.text = scoreTextRed.text = "0";
    }

    public void StartMatch(){
        Reset();
        mainMenuUI.SetActive(false);
        beforeMatchUI.SetActive(true);
        coin.SetActive(false);
        countdownText.color = new Color(1f, 0.7f, 0f);
        matchStarted = true;

        for (int i = 3; i >= 0; i--){
            int count = i;
            Tools.CallDelayed(() => {
                if (count > 0){
                    countdownText.text = count.ToString();
                }
                else{
                    countdownText.text = "GO";
                    countdownText.color = new Color(0, 1f, 0.3f);
                    playerBlue.SetSmokeIntensitySmooth(2f, 0.1f);
                    playerRed.SetSmokeIntensitySmooth(2f, 0.1f);
                }
            }, 3 - i);
        }

        Tools.CallDelayed(() => {
            matchRunning = true;
            beforeMatchUI.SetActive(false);
            matchUI.SetActive(true);
            coin.transform.position = new Vector3(1.2f, 2.5f + Mathf.Sin(Time.time * 2) * 0.15f, 0);
            coin.SetActive(true);
            Tools.CallDelayed(() => {
                playerBlue.SetSmokeIntensitySmooth(0f, 0.3f);
                playerRed.SetSmokeIntensitySmooth(0f, 0.3f);
            }, 0.3f);
        }, 4.1f);
    }

    public void AddScore(PlayerSide side, int increment){
        if (side == PlayerSide.Blue){
            scores.x += increment;
            scoreTextBlue.text = scores.x.ToString();
            playerBlue.SetSmokeIntensitySmooth(1.2f, 0.2f);
            Tools.CallDelayed(() => playerBlue.SetSmokeIntensitySmooth(0f, 0.2f), 0.3f * increment);
        }
        else{
            scores.y += increment;
            scoreTextRed.text = scores.y.ToString();
            playerRed.SetSmokeIntensitySmooth(1.7f, 0.2f);
            Tools.CallDelayed(() => playerRed.SetSmokeIntensitySmooth(0f, 0.2f), 0.3f * increment);
        }
    }

    public void EndMatch(){
        matchRunning = false;
        matchStarted = false;
        beforeMatchUI.SetActive(false);
        matchUI.SetActive(false);
        mainMenuUI.SetActive(true);
    }

    public void Update(){
        if (matchStarted){
            timer -= Time.deltaTime;
            if (timer < 0) timer = 0;
            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
        else if (Input.GetKeyDown(KeyCode.Space)){
            StartMatch();
        }
    }


}
