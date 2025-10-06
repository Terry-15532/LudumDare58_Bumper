using System.Collections;
using TMPro;
using UnityEngine;
using static GameInfo;

public enum ElementType {
    Sprite,
    TMP,
    TMPUGUI,
    Image,
}

public class CustomElement : MonoBehaviour {
    private Coroutine positionAni, scaleAni, rotationAni, hueAni, saturationAni, brightnessAni, alphaAni, colorAni;
    protected object visualElement;
    public ElementType elementType;
    public bool changeColorWithParent = true;
    public bool staticChildrenElements = true, interactionAllowed = true;

    protected CustomElement[] _childElements;

    public CustomElement[] childElements {
        get {
            if (staticChildrenElements) {
                return _childElements;
            }

            return GetComponentsInChildren<CustomElement>();
        }
    }

    public void SetActive(bool active) {
        gameObject.SetActive(active);
    }

    public virtual Vector3 position {
        get { return transform.localPosition; }
        set { transform.localPosition = value; }
    }

    public virtual Vector3 worldPosition {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public virtual Vector3 rotation {
        get { return transform.eulerAngles; }
        set { transform.eulerAngles = value; }
    }

    public virtual Vector3 scale {
        get { return transform.localScale; }
        set { transform.localScale = value; }
    }

    public virtual Color color {
        get {
            switch (elementType) {
                case ElementType.Sprite:
                    return ((SpriteRenderer)visualElement).color;
                case ElementType.TMP:
                    return ((TextMeshPro)visualElement).color;
            }

            return new();
        }
        set {
            switch (elementType) {
                case ElementType.Sprite:
                    ((SpriteRenderer)visualElement).color = value;
                    return;
                case ElementType.TMP:
                    ((TextMeshPro)visualElement).color = value;
                    return;
            }
        }
    }

    public virtual void Awake() {
        _childElements = GetComponentsInChildren<CustomElement>();
        var sprite = GetComponent<SpriteRenderer>();
        var tmp = GetComponent<TextMeshPro>();
        if (sprite != null) {
            visualElement = sprite;
            elementType = ElementType.Sprite;
        }
        else if (tmp != null) {
            visualElement = tmp;
            elementType = ElementType.TMP;
        }
    }

    public void SetColor(Color c) {
        foreach (CustomElement element in GetComponentsInChildren<CustomElement>()) {
            if (element == this || element.changeColorWithParent) {
                element.color = c;
            }
        }
    }

    public static Vector2 GetPosOnCanvas(Vector3 pos) {
        if (Camera.main != null) {
            Vector2 viewportPosition = Camera.main.WorldToViewportPoint(pos);
            return new Vector2(
                ((viewportPosition.x * canvasRectTransform.sizeDelta.x) - (canvasRectTransform.sizeDelta.x * 0.5f)),
                ((viewportPosition.y * canvasRectTransform.sizeDelta.y) - (canvasRectTransform.sizeDelta.y * 0.5f))
            );
        }

        return Vector2.zero;
    }

    public Vector2 GetPosOnCanvas() {
        return GetPosOnCanvas(position);
    }


    public void SetAlpha(float a, bool forceChangeAll = false) {
        foreach (CustomElement element in childElements) {
            if (element == this || element.changeColorWithParent || forceChangeAll) {
                var c = element.color;
                element.color = new Color(c.r, c.g, c.b, a);
            }
        }
    }

    public void SetBrightness(float v, bool forceChangeAll = false) {
        foreach (CustomElement element in childElements) {
            if (element == this || element.changeColorWithParent || forceChangeAll) {
                element.color = element.color.SetV(v);
            }
        }
    }

    public void SetHue(float h, bool forceChangeAll = false) {
        foreach (CustomElement element in childElements) {
            if (element == this || element.changeColorWithParent || forceChangeAll) {
                element.color = element.color.SetH(h);
            }
        }
    }

    public void SetSaturation(float s, bool forceChangeAll = false) {
        foreach (CustomElement element in childElements) {
            if (element == this || element.changeColorWithParent || forceChangeAll) {
                element.color = element.color.SetS(s);
            }
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public void SetAttr(float value, ColorAttr attr, bool forceChangeAll = false) {
        switch (attr) {
            case ColorAttr.h:
                SetHue(value, forceChangeAll);
                break;
            case ColorAttr.s:
                SetSaturation(value, forceChangeAll);
                break;
            case ColorAttr.v:
                SetBrightness(value, forceChangeAll);
                break;
            case ColorAttr.a:
                SetAlpha(value, forceChangeAll);
                break;
        }
    }

    //IEnumerator Animation part

    public void SetAttrAni(float endValue, float time, ColorAttr attribute, bool hide = false, bool forceChangeAll = false, bool scaled = true) {
        float initValue = 0;
        switch (attribute) {
            case ColorAttr.h:
                initValue = color.h();
                break;
            case ColorAttr.s:
                initValue = color.s();
                break;
            case ColorAttr.v:
                initValue = color.v();
                break;
            case ColorAttr.a:
                initValue = color.a;
                break;
        }

        SetAttrAni(initValue, endValue, time, attribute, hide, forceChangeAll, scaled);
    }

    public void SetAttrAni(float initValue, float value, float time, ColorAttr attribute, bool hide = false, bool forceChangeAll = false,
        bool scaled = true) {
        ref Coroutine c = ref hueAni;
        switch (attribute) {
            case ColorAttr.h:
                c = ref hueAni;
                break;
            case ColorAttr.s:
                c = ref saturationAni;
                break;
            case ColorAttr.v:
                c = ref brightnessAni;
                break;
            case ColorAttr.a:
                c = ref alphaAni;
                break;
        }

        if (c != null) {
            StopCoroutine(c);
        }

        try {
            if (gameObject.activeSelf)
                c = StartCoroutine(SetAttribute(initValue, value, time, attribute, hide, forceChangeAll, scaled));
        } catch { }
    }

    public void SetColorAni(Color target, float time, bool hide = false, bool scaled = true) {
        SetColorAni(color, target, time, hide, scaled);
    }

    public void SetColorAni(Color original, Color target, float time, bool hide = false, bool scaled = true) {
        if (brightnessAni != null) {
            StopCoroutine(brightnessAni);
        }

        if (saturationAni != null) {
            StopCoroutine(saturationAni);
        }

        if (hueAni != null) {
            StopCoroutine(hueAni);
        }

        if (colorAni != null) {
            StopCoroutine(colorAni);
        }

        colorAni = StartCoroutine(SetColor(original, target, time, hide, scaled));
    }

    IEnumerator SetColor(Color original, Color target, float time, bool hideAfterAni, bool scaled = true) {
        color = original;
        var interval = new WaitForEndOfFrame();
        float elapsedTime = 0;
        while (elapsedTime < time) {
            color = Color.Lerp(original, target, elapsedTime / time);
            yield return interval;
            elapsedTime += scaled ? Time.deltaTime : Time.unscaledDeltaTime;
        }

        color = target;
        if (hideAfterAni) {
            try {
                SetActive(false);
            } catch { }
        }
    }

    IEnumerator SetAttribute(float initValue, float endValue, float time, ColorAttr attribute, bool hide, bool forceChangeAll = false,
        bool scaled = true) {
        var interval = new WaitForEndOfFrame();
        int n = (int)(time / Time.deltaTime);
        float rate = (endValue - initValue) / n;
        float elapsedTime = 0;
        SetAttr(initValue, attribute, forceChangeAll);
        elapsedTime = 0;
        while (elapsedTime < time) {
            SetAttr(initValue.Lerp(endValue, elapsedTime / time), attribute, forceChangeAll);
            elapsedTime += scaled ? Time.deltaTime : Time.unscaledDeltaTime;
            yield return interval;
        }

        SetAttr(endValue, attribute, forceChangeAll);
        switch (attribute) {
            case ColorAttr.h:
                hueAni = null;
                break;
            case ColorAttr.s:
                saturationAni = null;
                break;
            case ColorAttr.v:
                brightnessAni = null;
                break;
        }

        if (hide) gameObject.SetActive(false);
    }


    public void SetScaleAni(Vector3 newScale, float time, bool hide = false, bool scaled = true) {
        SetScaleAni(scale, newScale, time, hide, scaled);
    }

    public void SetScaleAni(Vector3 original, Vector3 newScale, float time, bool hide = false, bool scaled = true) {
        if (scaleAni != null) {
            StopCoroutine(scaleAni);
        }

        scaleAni = StartCoroutine(SetScale(original, newScale, time, hide, scaled));
    }

    IEnumerator SetScale(Vector3 original, Vector3 newScale, float time, bool hide, bool scaled = true) {
        scale = original;
        var interval = new WaitForEndOfFrame();
        float elapsedTime = 0;
        while (elapsedTime < time) {
            float t = Mathf.Sin((Mathf.PI / 2) * (elapsedTime / time));
            Vector3 currScale = Vector3.Lerp(original, newScale, t);
            scale = currScale;
            elapsedTime += scaled ? Time.deltaTime : Time.unscaledDeltaTime;
            yield return interval;
        }

        scaleAni = null;
        scale = newScale;
        if (hide) gameObject.SetActive(false);
    }


    public void SetPositionAni(Vector3 newPos, float time, bool hide = false, bool scaled = true) {
        SetPositionAni(position, newPos, time, hide, scaled);
    }

    public void SetPositionAni(Vector3 original, Vector3 newPos, float time, bool hide = false, bool scaled = true) {
        if (positionAni != null) {
            StopCoroutine(positionAni);
        }

        positionAni = StartCoroutine(SetPosition(original, newPos, time, hide, scaled));
    }

    IEnumerator SetPosition(Vector3 original, Vector3 newPos, float time, bool hide, bool scaled = true) {
        position = original;
        var interval = new WaitForEndOfFrame();

        float elapsedTime = 0;
        while (elapsedTime < time) {
            float t = Mathf.Sin((Mathf.PI / 2) * (elapsedTime / time));
            Vector3 currentPosition = Vector3.Lerp(original, newPos, t);
            try {
                position = currentPosition;
            } catch { }

            elapsedTime += scaled ? Time.deltaTime : Time.unscaledDeltaTime;
            yield return interval;
        }

        positionAni = null;
        position = newPos;
        if (hide) gameObject.SetActive(false);
    }

    public void SetRotationAni(Quaternion q, float t, bool scaled = true) {
        SetRotationAni(q.eulerAngles, t, scaled);
    }

    public void SetRotationAni(Vector3 v, float t, bool scaled = true) {
        if (rotationAni != null) {
            StopCoroutine(rotationAni);
        }

        rotationAni = StartCoroutine(RotateTo(v, t, scaled));
    }

    IEnumerator RotateTo(Vector3 target, float t, bool scaled = true) {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(target);
        var interval = new WaitForEndOfFrame();
        float elapsedTime = 0;

        while (elapsedTime < t) {
            elapsedTime += scaled ? Time.deltaTime : Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Sin((Mathf.PI / 2) * (elapsedTime / t));
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, normalizedTime);

            yield return interval;
        }

        transform.rotation = targetRotation;
    }
}

public enum ColorAttr {
    h,
    s,
    v,
    a
}