using System;
using UnityEngine;

public class SettingsData : ICloneable{
	public bool inverseMouseDirection;
	public bool fullScreen;
	public float volume;
	public float cameraShakeIntensity;
	public Language language;

	public void Init(){
		inverseMouseDirection = false;
		cameraShakeIntensity = 1.5f;
		language = Application.systemLanguage == SystemLanguage.Chinese ? Language.Chinese : Language.English;
		fullScreen = false;
		volume = 1.0f;
	}

	public object Clone(){
		return base.MemberwiseClone();
	}
}

public static class Settings{
	public static SettingsData data = new();

	public const string key = "MetaClashSettings";

	//public static List<Action<SettingsData>> callBack;

	public static event Action onSettingsLoaded;

	public static void SaveSettings(){
		var d = JsonUtility.ToJson(data);
		PlayerPrefs.SetString(key, d);
		PlayerPrefs.Save();
		UpdateSettings();
	}

	public static void LoadSettings(){
		if (PlayerPrefs.HasKey(key)){
			data = JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(key));
			UpdateSettings();
		}
		else{
			data = new SettingsData();
			data.Init();
			SaveSettings();
		}

		onSettingsLoaded?.Invoke();
	}

	public static SettingsData lastSettings = (SettingsData)data.Clone();

	public static void UpdateSettings(){
		if (lastSettings.language != data.language){
			TextLocalization.UpdateTexts();
		}

		if (lastSettings.fullScreen != data.fullScreen){
			UpdateFullScreen();
		}

		if (lastSettings.volume != data.volume){
			SoundSys.UpdateVolume();
		}

		lastSettings = (SettingsData)data.Clone();
	}

	public static Vector2Int tmpResolution = new(Screen.width, Screen.height);

	public static void UpdateFullScreen(){
		var f = data.fullScreen;
		if (f != Screen.fullScreen){
			if (f){
				tmpResolution.x = Screen.width;
				tmpResolution.y = Screen.height;
				Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
			}
			else{
				if (tmpResolution.x > 100){
					Screen.SetResolution(tmpResolution.x, tmpResolution.y, false);
				}
				else{
					Screen.SetResolution((int)(Screen.currentResolution.width * 0.8f), (int)(Screen.currentResolution.height * 0.8f), false);
					tmpResolution.x = Screen.width;
					tmpResolution.y = Screen.height;
				}
			}
		}
	}
}