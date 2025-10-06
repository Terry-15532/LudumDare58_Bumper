using System;
using UnityEngine;

public class UIAdjuster : MonoBehaviour{
	public static UIAdjuster instance;
	public static int unscaledTimeID = Shader.PropertyToID("_UnscaledTime");
	
	public void Awake(){
		if (instance != null){
			Destroy(gameObject);
			return;
		}

		DontDestroyOnLoad(gameObject);
		instance = this;
		Tools.CallDelayedAsync(() => { UIElementPosAdjuster.screenSize = new Vector2(Screen.width, Screen.height); }, 0.1f);
	}

	public void Update(){
		Shader.SetGlobalFloat(unscaledTimeID, Time.unscaledTime);
		
		var size = new Vector2(Screen.width, Screen.height);
		if (size != UIElementPosAdjuster.screenSize){
			Tools.CallDelayedAsync(() => { UIElementPosAdjuster.screenSize = size; }, 0.1f);
		}
	}
}