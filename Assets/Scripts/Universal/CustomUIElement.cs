using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomUIElement : CustomElement {
    public RectTransform _rectTransform;
    public Coroutine sizeAni;

    public RectTransform rectTransform {
        get {
            if (!_rectTransform) {
                _rectTransform = GetComponent<RectTransform>();
            }

            return _rectTransform;
        }
        set { _rectTransform = value; }
    }

    public float fillAmount {
        get {
            if (elementType == ElementType.Image) {
                return ((Image)visualElement).fillAmount;
            }
            else return 0;
        }
        set {
            if (elementType == ElementType.Image) {
                ((Image)visualElement).fillAmount = value;
            }
        }
    }

    public override Color color {
        get {
            switch (elementType) {
                case ElementType.TMPUGUI:
                    return ((TextMeshProUGUI)visualElement).color;
                case ElementType.Image:
                    return ((Image)visualElement).color;
            }
            return new();
        }
        set {
            switch (elementType) {
                case ElementType.TMPUGUI:
                    ((TextMeshProUGUI)visualElement).color = value;
                    return;
                case ElementType.Image:
                    ((Image)visualElement).color = value;
                    return;
            }
        }
    }

    public Vector2 sizeDelta {
        get { return rectTransform.sizeDelta; }
        set { rectTransform.sizeDelta = value; }
    }

    public override void Awake() {
        _childElements = GetComponentsInChildren<CustomElement>();
        var tmpUGUI = GetComponent<TextMeshProUGUI>();
        var image = GetComponent<Image>();
        if (tmpUGUI) {
            visualElement = tmpUGUI;
            elementType = ElementType.TMPUGUI;
        }
        else if (image) {
            visualElement = image;
            elementType = ElementType.Image;
        }

        rectTransform = GetComponent<RectTransform>();
    }

    public override Vector3 position {
        get { return rectTransform.anchoredPosition; }
        set { rectTransform.anchoredPosition = value; }
    }

    public void SetSizeAni(Vector2 target, float time, bool hide = false) {
        SetSizeAni(sizeDelta, target, time, hide);
    }

    public void SetSizeAni(Vector2 original, Vector2 target, float time, bool hide = false) {
        if (sizeAni != null) {
            StopCoroutine(sizeAni);
        }

        sizeAni = StartCoroutine(SetSizeDelta(original, target, time, hide));
    }

    public IEnumerator SetSizeDelta(Vector2 original, Vector2 target, float time, bool hideAfterAni) {
        var interval = new WaitForEndOfFrame();
        float elapsedTime = 0;
        sizeDelta = original;
        while (elapsedTime < time) {
            float t = Mathf.Sin((Mathf.PI / 2) * (elapsedTime / time));
            sizeDelta = Vector2.Lerp(original, target, t);
            elapsedTime += Time.deltaTime;
            yield return interval;
        }

        sizeDelta = target;
        if (hideAfterAni) {
            try {
                SetActive(false);
            } catch {
                // ignored
            }
        }
    }
}