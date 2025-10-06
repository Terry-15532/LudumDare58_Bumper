using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;
using System.Collections.Generic;


#if DEBUG
using UnityEditor;

[CustomEditor(typeof(CustomSlider))]
public class SliderInspector : Editor{
	public override void OnInspectorGUI(){
		CustomSlider slider = (CustomSlider)target;
		slider.UpdateUI();

		if (slider.changeSettings){
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField("Corresponding Variable", GUILayout.Width(150));

			var allFields = typeof(SettingsData).GetFields();
			List<string> fields = new();
			foreach (var f in allFields){
				if (f.FieldType == typeof(float) || f.FieldType == typeof(int) || f.FieldType == typeof(double)){
					fields.Add(f.Name);
				}
			}

			var names = new string[fields.Count];
			int i = 0;
			foreach (var field in fields){
				names[i] = Tools.ToNormalEnglish(field);
				i++;
			}

			int selected = fields.IndexOf(slider.variableName);
			if (selected == -1){
				selected = 0;
			}

			selected = EditorGUILayout.Popup(selected, names);
			slider.variableName = fields[selected];

			EditorGUILayout.EndHorizontal();
		}
		else{
			slider.variable = null;
		}

		base.OnInspectorGUI();
	}
}
#endif

public class CustomSlider : MonoBehaviour{
	public bool changeSettings = false;

	public CustomUIElement track, fillArea;
	public SliderHandle handle;
	public RectTransform start, end;
	public TextMeshProUGUI valueTxt;
	public UnityEvent<float> onValueChanged;

	[HideInInspector] public FieldInfo variable;
	[HideInInspector] public string variableName;


	public float _value;

	public float value{
		get{ return _value; }
		set{
			float tmp = Mathf.Clamp((float)Math.Round(value, roundAccuracy), minValue, maxValue);
			if (tmp != _value){
				SoundSys.PlaySound("SFX/Slider");
				_value = tmp;
				if (onValueChanged != null){
					onValueChanged.Invoke(_value);
				}

				if (variable != null){
					variable.SetValue(Settings.data, _value);
					Settings.SaveSettings();
				}

				UpdateUI();
			}
		}
	}

	public float maxValue, minValue;

	public Vector2 handlePosDelta = Vector2.zero;
	public Vector2 startPos, endPos;
	public bool interactable;
	public bool fill, roundActualValue;
	public int roundAccuracy, displayedAccuracy;

	public void Awake(){
		if (changeSettings){
			variable = typeof(SettingsData).GetField(variableName);
		}
	}

	public void Start(){
		Init();
		if (changeSettings){
			Settings.onSettingsLoaded += UpdateData;
		}
	}

	public void OnDestroy(){
		Settings.onSettingsLoaded -= UpdateData;
	}

	public void Init(){
		handle.slider = this;
		if (fill){
			fillArea.SetActive(true);
			fillArea.fillAmount = valuePercentage;
		}

		UpdateUI();
	}

	public float valuePercentage{
		get{ return (value - minValue) / (maxValue - minValue); }
		set{ this.value = minValue.Lerp(maxValue, value); }
	}

	public void UpdateUI(){
		RefreshStartEndPoint();
		if (fill){
			fillArea.SetActive(true);
			fillArea.fillAmount = valuePercentage;
		}

		valueTxt.text = Math.Round(value, displayedAccuracy).ToString();
		handle.position = Vector2.Lerp(startPos, endPos, valuePercentage) + handlePosDelta;
	}

	public void UpdateData(){
		value = (float)(variable.GetValue(Settings.data));
	}

	public void RefreshStartEndPoint(){
		startPos = start.anchoredPosition;
		endPos = end.anchoredPosition;
	}
}