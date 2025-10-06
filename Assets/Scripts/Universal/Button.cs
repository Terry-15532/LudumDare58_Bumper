using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;


public interface IAsButton : IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler { }

public class Button : CustomUIElement, IAsButton {
    public CustomUIElement highlighter;
    public UnityEvent myEvent;
    public CustomUIElement tmp;
    public bool changeBrightness, changeScale = true, hideTxt, scaled = true;
    public Vector2 normalScale;
    internal Vector2 mouseOverScale, clickedScale;
    public float mouseOverRatio = 1.03f, clickedRatio = 0.99f, brightnessOn = 0.9f, brightnessClicked = 0.7f, brightnessIdle = 1;
    public float time = 0.3f;

    public float clickedSpeedUpRatio = 0.5f;

    public override void Awake() {
        base.Awake();
        if (highlighter) {
            highlighter.SetActive(false);
        }

        if (normalScale == Vector2.zero)
            normalScale = this.scale;
        if (mouseOverScale == Vector2.zero)
            mouseOverScale = normalScale * mouseOverRatio;
        if (clickedScale == Vector2.zero)
            clickedScale = normalScale * clickedRatio;
        SetBrightness(brightnessIdle);
        scale = normalScale;
    }

    public virtual void OnMouseEnter() {
        if (changeBrightness)
            SetAttrAni(brightnessOn, time, ColorAttr.v, scaled: scaled);
        if (highlighter) {
            highlighter.SetActive(true);
            highlighter.SetAttrAni(0, 1, time, ColorAttr.a, scaled: scaled);
        }

        if (hideTxt) {
            tmp.SetActive(true);
            tmp.SetAttrAni(0, 1, time, ColorAttr.a, scaled: scaled);
        }

        if (changeScale)
            SetScaleAni(mouseOverScale, time, scaled: scaled);
    }

    public virtual void OnMouseExit() {
        if (changeBrightness)
            SetAttrAni(brightnessIdle, time, ColorAttr.v, scaled: scaled);
        if (changeScale)
            SetScaleAni(normalScale, time, scaled: scaled);
        if (highlighter) {
            highlighter.SetAttrAni(1, 0, time, ColorAttr.a, true, scaled: scaled);
        }

        if (hideTxt) {
            tmp.SetAttrAni(0, time, ColorAttr.a, true, scaled: scaled);
        }
    }

    public virtual void OnMouseDown() {
        OnMouseDown(true);
    }

    public void OnMouseDown(bool sound) {
        if (sound)
            SoundSys.PlaySound("Click");
        if (changeBrightness)
            SetAttrAni(brightnessClicked, time * clickedSpeedUpRatio, ColorAttr.v, scaled: scaled);
        if (changeScale)
            SetScaleAni(clickedScale, time * clickedSpeedUpRatio, scaled: scaled);
    }

    public virtual void OnMouseUpAsButton() {
        if (changeBrightness)
            SetAttrAni(brightnessOn, time * clickedSpeedUpRatio, ColorAttr.v, scaled: scaled);
        if (interactionAllowed) {
            if (changeScale)
                SetScaleAni(mouseOverScale, time * clickedSpeedUpRatio, scaled: scaled);
            myEvent.Invoke();
        }
    }

    public Button Create(string str = null) {
        if (str != null) {
            var button = (Button)Tools.Create("LWButton");
            try {
                button.tmp.GetComponent<TextMeshProUGUI>().text = str;
            } catch { }

            try {
                button.tmp.GetComponent<TextMeshPro>().text = str;
            } catch { }

            return button;
        }
        else return (Button)Tools.Create("LWButton");
    }

    public virtual void OnPointerEnter(PointerEventData data) {
        OnMouseEnter();
    }

    public virtual void OnPointerClick(PointerEventData data) {
        OnMouseUpAsButton();
    }

    public virtual void OnPointerExit(PointerEventData data) {
        OnMouseExit();
    }

    public virtual void OnPointerDown(PointerEventData data) {
        OnMouseDown();
    }
}

//public class CustomButton : Button {
//    public UnityEvent mouseEnter, mouseOver, mouseDown, mouseClicked, mouseExit;
//    private bool isMouseOver = false;

//    public override void OnPointerClick(PointerEventData data) {
//        mouseClicked?.Invoke();
//    }

//    public override void OnPointerDown(PointerEventData data) {
//        mouseDown?.Invoke();
//    }

//    public override void OnPointerEnter(PointerEventData eventData) {
//        mouseEnter?.Invoke();
//        isMouseOver = true;
//    }

//    public override void OnPointerExit(PointerEventData data) {
//        isMouseOver = false;
//        mouseExit?.Invoke();
//    }

//    public void Update() {
//        if (isMouseOver) {
//            mouseOver?.Invoke();
//        }
//    }
//}