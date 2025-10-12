#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;

public class AudioClipEditor : EditorWindow{
	[MenuItem("Window/AudioClipEditor")]
	public static void ShowWindow(){
		GetWindow<AudioClipEditor>("AudioEditor");
	}

	public AudioSource audioSource;
	public AudioClip audioClip, audioClipTemp;
	public float startTime, endTime;
	public float volume = 1.0f; // Added volume control
	private float[] samples;
	private Texture2D waveformTexture;

	private void OnEnable(){
		audioSource = EditorUtility.CreateGameObjectWithHideFlags("AudioSource", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
	}

	private void OnGUI(){
		EditorGUILayout.BeginVertical();

		audioClip = EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false) as AudioClip;
		if (audioClipTemp != audioClip){
			audioClipTemp = audioClip;
			Init();
		}

		if (audioClip){
			DisplayAudioClipInfo();
			AdjustClipSection();
			DisplayVolumeControl(); // Added volume control display

			GUILayout.Space(10);

			if (GUILayout.Button("Play", GUILayout.Height(40))){
				PlayTestAudio();
			}

			GUILayout.Space(10);

			if (GUILayout.Button("Save Clip", GUILayout.Height(40))){
				ClipAndSave();
			}
		}

		EditorGUILayout.EndVertical();
	}

	private void Init(){
		if (audioClip != null){
			endTime = audioClip.length;
			samples = new float[audioClip.samples * audioClip.channels];
			audioClip.GetData(samples, 0);
			UpdateWaveformTexture();
		}
	}

	private void DisplayAudioClipInfo(){
		EditorGUILayout.LabelField($"Start: {startTime:F2}s");
		EditorGUILayout.LabelField($"End: {endTime:F2}s");

		GUILayout.Space(10);
	}

	private void AdjustClipSection(){
		GUILayout.Label("Adjust Clip Section");

		Rect waveformRect = GUILayoutUtility.GetRect(position.width, 64);
		if (waveformTexture == null || waveformTexture.width != (int)waveformRect.width){
			UpdateWaveformTexture();
		}

		if (waveformTexture != null){
			EditorGUI.DrawPreviewTexture(waveformRect, waveformTexture);
		}

		EditorGUILayout.MinMaxSlider("", ref startTime, ref endTime, 0f, audioClip != null ? audioClip.length : 10f);
	}

	private void DisplayVolumeControl(){
		GUILayout.Label("Volume");
		volume = EditorGUILayout.Slider(volume, 0f, 10);
	}

	private void PlayTestAudio(){
		if (audioClip != null){
			AudioClip clippedAudioClip = ClipAudio(audioClip, startTime, endTime);
			audioSource.clip = clippedAudioClip;
			audioSource.volume = volume;
			audioSource.Play();
		}
		else{
			Debug.LogError("Missing Audio");
		}
	}

	private void ClipAndSave(){
		if (audioClip != null){
			AudioClip clippedAudioClip = ClipAudio(audioClip, startTime, endTime);
			string savePath = EditorUtility.SaveFilePanel("Save Audio Clip", "Assets", audioClipTemp.name + "_New", "wav");

			if (!string.IsNullOrEmpty(savePath)){
				SaveAudioClipToFile(clippedAudioClip, savePath);
			}
		}
		else{
			Debug.LogError("Missing Audio");
		}
	}

	private void UpdateWaveformTexture(){
		if (samples != null){
			waveformTexture = GenerateWaveformTexture(samples, audioClip.channels, (int)position.width, 64);
		}
	}

	private Texture2D GenerateWaveformTexture(float[] samples, int channels, int width, int height){
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		Color[] colors = new Color[width * height];
		for (int i = 0; i < colors.Length; i++){
			colors[i] = Color.black;
		}

		texture.SetPixels(colors);

		int packSize = (samples.Length / channels) / width;
		for (int x = 0; x < width; x++){
			float max = 0;
			for (int i = 0; i < packSize; i++){
				float sampleValue = samples[(x * packSize + i) * channels] * volume;
				if (sampleValue > max) max = sampleValue;
			}

			int heightValue = (int)(max * height);
			for (int y = 0; y < heightValue; y++){
				texture.SetPixel(x, y, Color.yellow);
			}
		}

		texture.Apply();
		return texture;
	}

	private AudioClip ClipAudio(AudioClip originalClip, float start, float end){
		float[] data = new float[originalClip.samples * originalClip.channels];
		originalClip.GetData(data, 0);

		int newSampleCount = (int)((end - start) * originalClip.frequency);
		float[] newData = new float[newSampleCount * originalClip.channels];

		int startIndex = (int)(start * originalClip.frequency * originalClip.channels);
		for (int i = 0; i < newSampleCount * originalClip.channels; i++){
			newData[i] = data[startIndex + i] * volume;
		}

		AudioClip clippedAudioClip = AudioClip.Create("ClippedAudio", newSampleCount, originalClip.channels, originalClip.frequency, false);
		clippedAudioClip.SetData(newData, 0);

		return clippedAudioClip;
	}

	private void SaveAudioClipToFile(AudioClip clip, string path){
		var samples = new float[clip.samples * clip.channels];
		clip.GetData(samples, 0);

		var fileStream = new FileStream(path, FileMode.Create);
		var writer = new BinaryWriter(fileStream);

// Write WAV header
		writer.Write("RIFF".ToCharArray());
		writer.Write(36 + samples.Length * 2);
		writer.Write("WAVE".ToCharArray());
		writer.Write("fmt ".ToCharArray());
		writer.Write(16);
		writer.Write((short)1);
		writer.Write((short)clip.channels);
		writer.Write(clip.frequency);
		writer.Write(clip.frequency * clip.channels * 2);
		writer.Write((short)(clip.channels * 2));
		writer.Write((short)16);
		writer.Write("data".ToCharArray());
		writer.Write(samples.Length * 2);

// Write sample data
		foreach (var sample in samples){
			writer.Write((short)(sample * short.MaxValue));
		}

		writer.Close();
		fileStream.Close();

		AssetDatabase.Refresh();
	}
}

#endif