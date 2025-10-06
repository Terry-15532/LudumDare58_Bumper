using System;
using UnityEngine;
using static Tools;

public class Sound : MonoBehaviour {
    public Coroutine volumeFade;
    public float baseVolume = 1;

    public float volume {
        get { return baseVolume; }
        set {
            baseVolume = value;
            audioSource.volume = value * Settings.data.volume;
        }
    }

    void Start() {
        SoundSys.onVolumeChanged += OnVolumeChanged;
        DontDestroyOnLoad(this);
    }

    public AudioSource audioSource;

    public void PlaySound(AudioClip clip, float delay = 0, bool loop = false) {
        audioSource.clip = clip;
        audioSource.loop = loop;
        if (delay > 0)
            CallDelayedAsync(() => audioSource.Play(), delay);
        else audioSource.Play();
    }

    public void SetVolumeSmooth(float endVolume, float time) {
        if (volumeFade != null) {
            StopCoroutine(volumeFade);
        }

        float currV = volume;

        volumeFade = StartCoroutine(Tools.Repeat((f) => { volume = currV.Lerp(endVolume, f); }, time, new WaitForEndOfFrame(), scaled: false));
    }

    public void OnDestroy() {
        SoundSys.onVolumeChanged -= OnVolumeChanged;
    }

    public void OnVolumeChanged(float volume) {
        audioSource.volume = baseVolume * volume;
    }
}