using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommissionSystem : MonoBehaviour {

    public static List<Action> actions = new();
    public static CommissionSystem instance;

    public void Start() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }
    public void Update() {
        if (actions.Count != 0) {
            while (actions.Count > 0) {
                try {
                    actions[0].Invoke();
                    actions.RemoveAt(0);

                } catch (Exception e) {
                    try {
                        actions.RemoveAt(0);
                    } catch { }
                    Debug.Log(e.Message);
                }
            }
        }
    }
}
