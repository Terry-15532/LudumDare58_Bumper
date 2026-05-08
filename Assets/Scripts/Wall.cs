using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


public class Wall : MonoBehaviour {
    public static readonly int outerColorIndex = Shader.PropertyToID("_OuterOutlineColor");
    public Vector4 redHighlight = new Vector4(1.2f, 0.1f, 0.1f, 1f) * 10f, blueHighlight = new Vector4(0.1f, 0.5f, 1.2f, 1f) * 10f;
    private Vector3 startPos;
    private Quaternion startRot;

    public MeshRenderer mr;

    public void Awake(){
        mr = GetComponent<MeshRenderer>();
        startPos = transform.position;
        startRot = transform.rotation;
        Game.instance.walls.Add(this);
    }

    public void Reset(){
        //     var rb = GetComponent<Rigidbody>();
        //     if (rb){
        //         rb.isKinematic = true;
        //         Tools.CallDelayed(() => rb.isKinematic = false, 1f);
        //     }
        transform.position = startPos;
        transform.rotation = startRot;
        mr.materials[0].SetVector(outerColorIndex, Vector4.zero);

    }

    public void Update(){
        if (transform.position.y < -10) {
            gameObject.SetActive(false);
        }
    }

    public void SetOutlineBlink(PlayerSide hitSide){
        mr.materials[0].SetVector(outerColorIndex, hitSide == PlayerSide.Blue ? blueHighlight : redHighlight);
        try { Tools.CallDelayed(() => mr.materials[0].SetVector(outerColorIndex, Vector4.zero), 0.2f); }
        finally { }
    }


}
