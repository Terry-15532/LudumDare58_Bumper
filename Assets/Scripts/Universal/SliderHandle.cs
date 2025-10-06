using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class SliderHandle : Button {
    public CustomUIElement track;
    public bool pressed;
    public Vector2 startPoint, endPoint;
    public CustomSlider slider;

    public override void OnMouseEnter() {
        if (!pressed) {
            base.OnMouseEnter();
        }
    }

    public override void OnMouseDown() {
        base.OnMouseDown();
        pressed = true;
    }

    public override void OnMouseUpAsButton() {
        //base.OnMouseUpAsButton();
        //pressed = false;
    }

    public override void OnMouseExit() {
        if (!pressed) {
            base.OnMouseExit();
            //pressed = false;
        }
    }

    public void Update() {
        if (pressed && slider.interactable) {
            if (Input.GetMouseButton(0) == false) {
                pressed = false;
                base.OnMouseExit();
            }
            slider.valuePercentage = MousePosToValue();
        }
    }

    public float MousePosToValue() {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent.GetComponent<RectTransform>(), Input.mousePosition, GameInfo.UICamera, out pos);

        float f = Vector2.Dot(pos - slider.startPos, (slider.endPos - slider.startPos).normalized) / (slider.endPos - slider.startPos).magnitude;

        return f;

    }


}