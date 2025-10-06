using System;
using TMPro;
using UnityEngine;
using static GameInfo;
using static Tools;

public enum MsgBoxType{
	msg,
	input
}

public class MsgBox : CustomUIElement{
	public MsgBoxType type;
	public TMP_InputField inputField;
	public CustomUIElement background, box;
	public TextMeshProUGUI msg;
	public Action<bool, string> buttonPressed;
	public bool canBeClosed, hidden = false;
	public static bool msgShowing = false;

	public static MsgBox Create(string s, MsgBoxType t, Action<bool, string> buttonPressedAction = null, string defaultContent = "", string placeHolder = ""){
		if (!msgShowing){
			var msgBox = Instantiate(ResourceManager.Load<MsgBox>("Prefabs/MsgBox"), canvasRectTransform, false);
			msgBox.type = t;
			msgBox.msg.text = s;
			msgBox.buttonPressed = buttonPressedAction;
			msgBox.inputField.text = defaultContent;
			msgBox.inputField.placeholder.GetComponent<TextMeshProUGUI>().text = placeHolder;
			msgBox.Show();
			return msgBox;
		}
		else return null;
	}

	public void Show(){
		msgShowing = true;
		SoundSys.PlaySound("SFX/Whoosh");
		SoundSys.PlaySound("SFX/Hover");
		rectTransform.SetAsLastSibling();
		position = new Vector2(0, 0);
		hidden = false;
		scale = new Vector2(1, 1);
		background.SetActive(true);
		box.SetActive(true);
		background.SetAttrAni(0, 0.5f, 0.4f, ColorAttr.a, scaled: false);
		box.SetAttrAni(0, 1, 0.2f, ColorAttr.a, scaled: false);
		box.position = new Vector2(-2500, 0);
		box.SetPositionAni(new Vector2(0, 0), 0.2f, scaled: false);
		inputField.Select();
		if (type == MsgBoxType.msg){
			inputField.gameObject.SetActive(false);
			msg.rectTransform.anchoredPosition = new Vector2(0, 0);
		}
	}

	public void Hide(bool confirm){
		if (!hidden){
			SoundSys.PlaySound("SFX/Whoosh");
			hidden = true;
			background.SetAttrAni(0, 0.4f, ColorAttr.a, true, scaled: false);
			box.SetAttrAni(0, 0.2f, ColorAttr.a, true, scaled: false);
			box.SetPositionAni(new Vector2((confirm ? 1 : -1) * 2500, 0), 0.2f, true, scaled: false);
			CallDelayedAsync(() => msgShowing = false, 0.2f);
			CallDelayedAsync(() => {
				try{
					Destroy(this.gameObject);
				}
				catch{ }
			}, 0.5f);
		}
	}

	public void Confirm(){
		canBeClosed = true;
		buttonPressed?.Invoke(true, inputField.text);
		if (canBeClosed) Hide(true);
	}

	public void Update(){
		if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Z)){
			Cancel();
		}

		if (Input.GetKeyDown(KeyCode.Return)){
			Confirm();
		}
	}

	public void Cancel(){
		canBeClosed = true;
		buttonPressed?.Invoke(false, inputField.text);
		if (canBeClosed){
			Hide(false);
		}
	}
}