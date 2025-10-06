using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public static class ResourceManager{
	public static Dictionary<string, object> resources = new();

	public static readonly string[] preloadedResources ={ "Prefabs/AcceleratorVFX", "Prefabs/AmplifierVFX", "Prefabs/AttackVFX", "Prefabs/PlugVFX", "Prefabs/HitVFX", "Prefabs/ParryVFX" };

	public static T Load<T>(string path) where T : Object{
		if (resources.TryGetValue(path, out object resource)){
			return (T)resource;
		}
		else{
			var r = Resources.Load<T>(path);
			resources.Add(path, r);
			return r;
		}
	}

	public static void Preload(){
		foreach (string path in preloadedResources){
			Load<VisualEffect>(path);
		}

		// Shader.WarmupAllShaders();
	}
}