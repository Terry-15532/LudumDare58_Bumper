using System.Collections.Generic;
using UnityEngine;

public static class BGMSystem{
	public static Sound BGM;
	public static Sound environment;
	public static Coroutine bgmFade;

	public static void Init(){
		BGM = SoundSys.PlaySound("Music/Start", loop: true);
		BGM.volume = 0.7f;
	}

	public static void PlayStart(){
		SwitchBGMWithFade("Start");
	}

	public static void PlayBattle(){
		SwitchBGMWithFade("Battle", 0.7f);
	}

	public static void SwitchBGMWithFade(string name, float volume = 0.5f){
		var lastBGM = BGM;
		lastBGM.SetVolumeSmooth(0, 1);
		Tools.CallDelayedAsync(() => {
			BGM = SoundSys.PlaySound("Music/" + name, loop: true);
			BGM.volume = 0;
			BGM.SetVolumeSmooth(volume, 1);
		}, 0.5f);
		Tools.CallDelayedAsync(() => { Object.Destroy(lastBGM.gameObject); }, 1);
	}
}