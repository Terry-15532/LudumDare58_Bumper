using TMPro;
using UnityEngine;
using static GameInfo;
using static Tools;
public class Message : CustomUIElement {

    public TextMeshProUGUI text;
    private float time, dist;
    private Vector2 pos;
    private bool moveUp;

    public override void Awake() {
        base.Awake();
        transform.SetParent(canvasRectTransform);
        //ShowAni(time, pos, dist, moveUp);
    }

    public void ShowAni() {
        //transform.SetParent(SceneInfo.canvasRectTransform);
        SoundSys.PlaySound("Error", volume: 0.5f);
        SetActive(true);
        position = pos + (moveUp ? -1 : +1) * new Vector2(0, dist);
        SetAttrAni(0, 1, time, ColorAttr.a);
        SetPositionAni(pos, time);
        CallDelayedAsync(() => {
            try {
                SetPositionAni(pos + (moveUp ? 1 : -1) * new Vector2(0, dist), time, true);
                SetAttrAni(1, 0, time, ColorAttr.a);
                CallDelayedAsync(() => {
                    try {
                        Destroy(gameObject);
                    } catch { }
                }, time);
            } catch { }
        }, time * 3);
    }

    public static Message Create(string txt, float time = 0.3f, Vector2 position = new Vector2(),
        float dist = 100f, bool moveUp = true, bool showAni = true) {
        var msg = Instantiate(ResourceManager.Load<Message>("Prefabs/Message"), GameInfo.canvasRectTransform);
        msg.text.text = txt;
        msg.time = time;
        msg.dist = dist;
        msg.moveUp = moveUp;
        msg.pos = position;
        if (showAni) {
            msg.Awake();
            msg.ShowAni();
        }
        return msg;
    }
}
