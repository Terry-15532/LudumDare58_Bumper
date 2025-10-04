using UnityEngine;

[ExecuteAlways]
public class NormalDebugger : MonoBehaviour{
    public bool drawVertexNormals = true;
    public bool drawFaceNormals = false;
    public Color vertexNormalColor = Color.green;
    public Color faceNormalColor = Color.cyan;
    public float normalLength = 0.1f;
    private float drawDuration = 0.1f;

    [Tooltip("间隔多久（秒）重绘一次法线；0 表示每帧绘制")]
    public float updateInterval = 0.1f;

    private float timer;

    void OnEnable(){
        timer = 0f;
    }

    void Update(){
        if (updateInterval <= 0f) DrawAllNormals();
        else{
            timer -= Time.deltaTime;
            if (timer <= 0f){
                timer = updateInterval;
                drawDuration = updateInterval * 2;
                DrawAllNormals();
            }
        }
    }

    void DrawAllNormals(){
        if (drawVertexNormals){
            foreach (var mf in GetComponentsInChildren<MeshFilter>()) DrawVertexNormals(mf.sharedMesh, mf.transform);
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>()) DrawVertexNormals(smr.sharedMesh, smr.transform);
        }
        if (drawFaceNormals){
            foreach (var mf in GetComponentsInChildren<MeshFilter>()) DrawFaceNormals(mf.sharedMesh, mf.transform);
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>()) DrawFaceNormals(smr.sharedMesh, smr.transform);
        }
    }

    void DrawVertexNormals(Mesh mesh, Transform tf){
        if (mesh == null || mesh.vertexCount != mesh.normals.Length) return;

        var verts = mesh.vertices;
        var norms = mesh.normals;
        for (int i = 0; i < verts.Length; i++){
            Vector3 wp = tf.TransformPoint(verts[i]);
            Vector3 wn = tf.TransformDirection(norms[i].normalized);
            Debug.DrawLine(wp, wp + wn * normalLength, vertexNormalColor, drawDuration, true);
        }
    }

    void DrawFaceNormals(Mesh mesh, Transform tf){
        if (mesh == null) return;

        var verts = mesh.vertices;
        var tris = mesh.triangles;
        for (int i = 0; i < tris.Length; i += 3){
            Vector3 v0 = tf.TransformPoint(verts[tris[i]]);
            Vector3 v1 = tf.TransformPoint(verts[tris[i + 1]]);
            Vector3 v2 = tf.TransformPoint(verts[tris[i + 2]]);
            Vector3 center = (v0 + v1 + v2) / 3f;
            Vector3 wn = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            Debug.DrawLine(center, center + wn * normalLength, faceNormalColor, drawDuration, true);
        }
    }

}
