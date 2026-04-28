using UnityEngine;
using UnityEditor;

public class MeshReplacer : EditorWindow
{
    [MenuItem("Tools/Replace Character.blend with Character.fbx")]
    public static void ReplaceMeshes()
    {
        string blendPath = "Assets/Models/Character.blend"; // Assuming typical path, but we can search by name
        string fbxPath = "Assets/Models/Character.fbx";     // Can be customized or search through AssetDatabase

        // Find the fbx asset path
        string[] fbxAssets = AssetDatabase.FindAssets("Character t:Model");
        string exactFbxPath = "";
        foreach (string guid in fbxAssets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("Character.fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                exactFbxPath = path;
                break;
            }
        }

        if (string.IsNullOrEmpty(exactFbxPath))
        {
            Debug.LogError("Could not find Character.fbx in the project.");
            return;
        }

        // Load all meshes from the fbx
        Object[] allFbxAssets = AssetDatabase.LoadAllAssetsAtPath(exactFbxPath);
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
                if (assetPath.EndsWith("Character.blend", System.StringComparison.OrdinalIgnoreCase))
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
                        Debug.LogWarning($"Mesh '{mf.sharedMesh.name}' not found in Character.fbx!");
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
                if (assetPath.EndsWith("Character.blend", System.StringComparison.OrdinalIgnoreCase))
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
                        Debug.LogWarning($"SkinnedMesh '{smr.sharedMesh.name}' not found in Character.fbx!");
                    }
                }
            }
        }

        Debug.Log($"Replaced {replaceCount} meshes from Character.blend with Character.fbx successfully.");
    }
}
