using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(NormalTool))]
public class NormalToolInspector : UnityEditor.Editor{
	public override void OnInspectorGUI(){
		NormalTool component = (NormalTool)target;
		if (GUILayout.Button("Generate")){
			component.Generate();
		}

		base.OnInspectorGUI();
	}
}

[ExecuteAlways]
#endif
public class NormalTool : MonoBehaviour{
	public GameObject myGO;
	public ComputeShader computeShader;
	public float length = 0.1f, drawDuration = 1f;
	public bool drawNormals;

	public static List<Action> drawCommands;

	private static int realNormalsID = Shader.PropertyToID("realNormals");
	private static int verticesID = Shader.PropertyToID("vertices");
	private static int tangentsID = Shader.PropertyToID("tangents");
	private static int uv7ID = Shader.PropertyToID("uv7");
	private static int uv6ID = Shader.PropertyToID("uv6");
	private static int normalsID = Shader.PropertyToID("normals");
	private static int trianglesID = Shader.PropertyToID("triangles");

	public void Generate(){
		drawCommands = new List<Action>();
		foreach (var mf in myGO.GetComponentsInChildren<MeshFilter>()){
			mf.sharedMesh = CalcNormals(mf.sharedMesh, mf.transform);
		}

		foreach (var smr in myGO.GetComponentsInChildren<SkinnedMeshRenderer>()){
			smr.sharedMesh = CalcNormals(smr.sharedMesh, smr.transform);
		}
	}


	private int count = 0;

	public void Update(){
		if (drawNormals){
			count++;
			if (count > 60 && drawCommands != null){
				count = 0;
				foreach (var action in drawCommands){
					action.Invoke();
				}
			}
		}
	}


	public Mesh CalcNormals(Mesh mesh, Transform t){
		Vector3[] vertices = mesh.vertices;
		Vector3[] normals = mesh.normals;
		int[] triangles = mesh.triangles;
		int vertexCount = mesh.vertexCount;
		Vector4[] tangents = mesh.tangents;
		int triangleVertCount = triangles.Length;


		Debug.Log($"Mesh: {mesh.name}, Vertices: {vertexCount}, Triangle Vertices: {triangleVertCount}");

		if (vertexCount == 0){
			return mesh;
		}

		ComputeBuffer vertexBuffer = new(vertexCount, sizeof(float) * 3);
		ComputeBuffer normalBuffer = new(vertexCount, sizeof(float) * 3);
		ComputeBuffer realNormalBuffer = new(vertexCount, sizeof(float) * 4);
		ComputeBuffer tangentBuffer = new(vertexCount, sizeof(float) * 4);
		ComputeBuffer uv6Buffer = new(vertexCount, sizeof(float) * 2);
		ComputeBuffer uv7Buffer = new(vertexCount, sizeof(float) * 2);
		ComputeBuffer triangleBuffer = new(triangleVertCount, sizeof(int));

		vertexBuffer.SetData(vertices);
		// realNormalBuffer.SetData(realNormals);
		triangleBuffer.SetData(triangles);
		tangentBuffer.SetData(tangents);

		computeShader.SetBuffer(0, verticesID, vertexBuffer);
		computeShader.SetBuffer(0, trianglesID, triangleBuffer);
		computeShader.SetBuffer(0, realNormalsID, realNormalBuffer);

		computeShader.Dispatch(0, vertexCount / 128 + 1, 1, 1);

		normalBuffer.SetData(normals);

		computeShader.SetBuffer(1, verticesID, vertexBuffer);
		computeShader.SetBuffer(1, tangentsID, tangentBuffer);
		computeShader.SetBuffer(1, uv6ID, uv6Buffer);
		computeShader.SetBuffer(1, uv7ID, uv7Buffer);
		computeShader.SetBuffer(1, normalsID, normalBuffer);
		computeShader.SetBuffer(1, realNormalsID, realNormalBuffer);

		computeShader.Dispatch(1, vertexCount / 128 + 1, 1, 1);

		Vector2[] uv6 = new Vector2[vertexCount];
		Vector2[] uv7 = new Vector2[vertexCount];
		Vector4[] debugNormals = new Vector4[vertexCount];

		realNormalBuffer.GetData(debugNormals);

		uv6Buffer.GetData(uv6);
		uv7Buffer.GetData(uv7);
		mesh.SetUVs(6, uv6);
		mesh.SetUVs(7, uv7);

		var vertArr = mesh.vertices.ToArray();

		if (drawNormals){
			drawCommands.Add(() => DrawNormals(t, debugNormals, vertArr));
		}


		vertexBuffer.Dispose();
		normalBuffer.Dispose();
		tangentBuffer.Dispose();
		uv7Buffer.Dispose();
		triangleBuffer.Dispose();
		realNormalBuffer.Dispose();

		return mesh;
	}

	public void DrawNormals(Transform t, Vector3[] normals, Vector3[] vertices){
		int i = 0;
		foreach (var normal in normals){
			var start = t.localToWorldMatrix.MultiplyPoint(vertices[i]);
			Debug.DrawLine(start, start + t.localToWorldMatrix.MultiplyVector(normal) * length, Color.cyan, drawDuration, true);
			i++;
		}
	}

	public void DrawNormals(Transform t, Vector4[] normals, Vector3[] vertices){
		int i = 0;
		foreach (var normal in normals){
			var start = t.localToWorldMatrix.MultiplyPoint(vertices[i]);
			Debug.DrawLine(start, start + t.localToWorldMatrix.MultiplyVector(normal) * length, Color.cyan, drawDuration, true);
			i++;
		}
	}
}