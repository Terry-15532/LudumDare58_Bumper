using UnityEngine;
using UnityEditor;

namespace Editor
{
    public class MeshReplacer : EditorWindow
    {
        private GameObject sourceModel;
        private GameObject targetModel;

        [MenuItem("Tools/Replace Meshes")]
        public static void ShowWindow()
        {
            GetWindow<MeshReplacer>("Replace Meshes");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select Source and Target Models", EditorStyles.boldLabel);

            sourceModel = (GameObject)EditorGUILayout.ObjectField("Source Model (.blend)", sourceModel, typeof(GameObject), false);
            targetModel = (GameObject)EditorGUILayout.ObjectField("Target Model (.fbx)", targetModel, typeof(GameObject), false);

            if (GUILayout.Button("Replace Meshes"))
            {
                if (sourceModel != null && targetModel != null)
                {
                    ReplaceSelectedMeshes();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select both source and target models.", "OK");
                }
            }
        }

        private void ReplaceSelectedMeshes()
        {
            string blendPath = AssetDatabase.GetAssetPath(sourceModel);
            string fbxPath = AssetDatabase.GetAssetPath(targetModel);

            // Load all meshes from the target fbx
            Object[] allFbxAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            System.Collections.Generic.Dictionary<string, Mesh> fbxMeshes = new System.Collections.Generic.Dictionary<string, Mesh>();
            foreach (Object obj in allFbxAssets)
            {
                if (obj is Mesh mesh)
                {
                    fbxMeshes[mesh.name] = mesh;
                }
            }

            int replaceCount = 0;

            // Replace MeshFilters
            MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                    if (assetPath == blendPath)
                    {
                        if (fbxMeshes.TryGetValue(mf.sharedMesh.name, out Mesh replacementMesh))
                        {
                            Undo.RecordObject(mf, "Replace Mesh");
                            mf.sharedMesh = replacementMesh;
                            replaceCount++;
                            EditorUtility.SetDirty(mf);
                        }
                        else
                        {
                            Debug.LogWarning($"Mesh '{mf.sharedMesh.name}' not found in target model!");
                        }
                    }
                }
            }

            // Replace SkinnedMeshRenderers
            SkinnedMeshRenderer[] skinnedMeshRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
            {
                if (smr.sharedMesh != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(smr.sharedMesh);
                    if (assetPath == blendPath)
                    {
                        if (fbxMeshes.TryGetValue(smr.sharedMesh.name, out Mesh replacementMesh))
                        {
                            Undo.RecordObject(smr, "Replace Skinned Mesh");
                            smr.sharedMesh = replacementMesh;
                            replaceCount++;
                            EditorUtility.SetDirty(smr);
                        }
                        else
                        {
                            Debug.LogWarning($"SkinnedMesh '{smr.sharedMesh.name}' not found in target model!");
                        }
                    }
                }
            }

            Debug.Log($"Replaced {replaceCount} meshes from {sourceModel.name} with {targetModel.name} successfully.");
        }
    }
}
