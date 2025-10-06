using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using static Tools;
using Object = UnityEngine.Object;

public static class SoundSys{
	public static event Action<float> onVolumeChanged;

	public static Sound PlaySound(string name, bool loop = false, float delay = 0, bool threeD = false, float volume = 1){
		AudioClip clip = ResourceManager.Load<AudioClip>("Sounds/" + name);
		if (clip){
			var sound = (Sound)Create("Sound");
			sound.PlaySound(clip, delay, loop);
			if (threeD){
				sound.audioSource.spatialBlend = 1;
			}
			else{
				sound.audioSource.spatialBlend = 0;
			}

			if (!loop){
				CallDelayedAsync(() => { Object.DestroyImmediate(sound.gameObject); }, clip.length);
			}

			sound.audioSource.volume = volume * Settings.data.volume;
			return sound;
		}

		return null;
	}

	public static Sound PlaySound(string name, Vector3 pos, bool loop = false, float delay = 0, bool threeD = false){
		AudioClip clip = ResourceManager.Load<AudioClip>("Sounds/" + name);
		if (clip){
			var sound = (Sound)Create("Sound");
			sound.transform.position = pos;
			sound.PlaySound(clip, delay, loop);
			if (threeD){
				sound.audioSource.spatialBlend = 1;
			}
			else{
				sound.audioSource.spatialBlend = 0;
			}

			if (!loop){
				CallDelayedAsync(() => { GameObject.DestroyImmediate(sound.gameObject); }, clip.length);
			}

			return sound;
		}

		return null;
	}

	public static void UpdateVolume(){
		float volume = Settings.data.volume;
		onVolumeChanged?.Invoke(volume);
	}
}