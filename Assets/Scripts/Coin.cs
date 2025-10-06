using UnityEngine;

public class Coin : MonoBehaviour{
    public float rotateSpeed = 1f;

    public void Update(){
        transform.Rotate(Vector3.up, rotateSpeed, Space.World);
        transform.position = new Vector3(transform.position.x, 2.5f + Mathf.Sin(Time.time * 2) * 0.15f, transform.position.z);
    }

    public void ChangePosition(){
        var vfx = Resources.Load<GameObject>("Prefabs/CoinVFX");
        vfx = Instantiate(vfx, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
        transform.position = new Vector3(Tools.RandomNum(-9, 10), transform.position.y, Tools.RandomNum(-6, 7));
        Tools.CallDelayed(() => {
            gameObject.SetActive(true);
            Destroy(vfx.gameObject);
        }, 0.5f);
    }


}
