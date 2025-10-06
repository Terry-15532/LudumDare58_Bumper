using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameInfo;
using static Tools;

public class SceneSwitching : CustomUIElement{
	public CustomUIElement background;

	public static void SwitchTo(string scene, float time1 = 0.2f, float time2 = 0.2f, float timeMiddle = 0.1f, Action afterLoad = null){
		MsgBox.msgShowing = false;
		var s = Create();
		s.sizeDelta = GameInfo.canvasRectTransform.sizeDelta * 1.5f;
		s.SetActive(true);
		s.SetAttrAni(0, 1, time1, ColorAttr.a, scaled: false);
		//s.rectTransform.SetAsLastSibling();
		CallDelayedAsync(() => {
			SceneManager.LoadScene(scene);
			canvasRectTransform = null;
			currScene = scene;
			CallDelayedAsync(() => {
				s.SetAttrAni(1, 0, time2, ColorAttr.a, true, scaled: false);
				if (afterLoad != null){
					afterLoad();
				}

				CallDelayedAsync(() => { Destroy(s.gameObject); }, time2);
			}, timeMiddle);
		}, time1);
	}

	public static SceneSwitching Create(){
		var s = Instantiate(ResourceManager.Load<SceneSwitching>("Prefabs/SceneSwitching"));
		DontDestroyOnLoad(s);
		return s;
	}
}