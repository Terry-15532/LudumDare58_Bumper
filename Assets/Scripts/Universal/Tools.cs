using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using MonoBehaviour = UnityEngine.Object;


public static class Tools {
    private static EmptyMono _callDelayedHelper;

    public static EmptyMono callDelayedHelper {
        get {
            if (_callDelayedHelper == null) {
                _callDelayedHelper = new GameObject().AddComponent<EmptyMono>();
                GameObject.DontDestroyOnLoad(_callDelayedHelper.gameObject);
            }

            return _callDelayedHelper;
        }
        set { _callDelayedHelper = value; }
    }

    public static void Alert(string str) {
        Debug.LogWarning(str);
    }

    public static bool IsPointOverUIElement(Vector2 screenPosition) {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results) {
            if (result.gameObject.activeInHierarchy) {
                return true;
            }
        }

        return false;
    }

    public static Vector2 GetMousePosOnCanvas() {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(GameInfo.canvasRectTransform, Input.mousePosition, GameInfo.UICamera,
            out Vector2 pos);
        return pos;
    }

    public static Vector2 GetMousePosInRect(RectTransform t) {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(t.parent.GetComponent<RectTransform>(), Input.mousePosition, GameInfo.UICamera,
            out Vector2 pos);
        return pos;
    }

    /// <summary>
    /// returns a random number from lower to upper (inclusive)
    /// </summary>
    public static int RandomNum(int lower, int upper) {
        int GetRandomSeed() {
            byte[] bytes = new byte[4];
            System.Security.Cryptography.RNGCryptoServiceProvider rng = new();
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes);
        }

        return new System.Random(GetRandomSeed()).Next(lower, upper + 1);
        //System.Random.Next returns an int from lower to upper - 1
    }

    public static Vector3 GetMousePos(params string[] layers) {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layer = 0;
        foreach (string s in layers) {
            try {
                layer += (1 << LayerMask.NameToLayer(s));
            } catch (Exception e) {
                Debug.Log(e.Message);
            }
        }

        if (Physics.Raycast(ray, out RaycastHit hit, 100000, layer)) {
            return hit.point;
        }
        else {
            return Vector3.zero;
        }
    }

    public static Vector3 ScreenToWorldPos(Vector2 pos, params string[] layers) {
        Ray ray = Camera.main.ScreenPointToRay(pos);
        int layer = 0;
        foreach (string s in layers) {
            try {
                layer += (1 << LayerMask.NameToLayer(s));
            } catch (Exception e) {
                Debug.Log(e.Message);
            }
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100000, layer)) {
            return hit.point;
        }
        else {
            return Vector3.zero;
        }
    }

    public static MonoBehaviour Create(string s) {
        return MonoBehaviour.Instantiate(ResourceManager.Load<UnityEngine.MonoBehaviour>("Prefabs/" + s));
    }


    public static async void CallDelayedAsync(Action a, float t, CancellationTokenSource cancel = null) {
        try {
#if WEBGL
            CallDelayedUnscaled(a, t);
            return;
#else
            if (cancel != null) {
                await Task.Delay((int)(t * 1000), cancel.Token);
            }
            else {
                await Task.Delay((int)(t * 1000));
            }

            a.Invoke();
#endif
        } catch { }
    }

    public static Coroutine CallDelayed(Action a, float t) {
        return callDelayedHelper.StartCoroutine(I_CallDelayed(a, t));
    }

    public static Coroutine CallDelayedUnscaled(Action a, float t) {
        return callDelayedHelper.StartCoroutine(I_CallDelayedUnscaled(a, t));
    }

    private static IEnumerator I_CallDelayed(Action a, float t) {
        yield return new WaitForSeconds(t);
        a.Invoke();
    }

    private static IEnumerator I_CallDelayedUnscaled(Action a, float t) {
        yield return new WaitForSecondsRealtime(t);
        a.Invoke();
    }

    public static IEnumerator Repeat(Action<float> action, int count, YieldInstruction interval) {
        int i = 0;
        while (i < count) {
            action.Invoke(i / (float)count);
            yield return interval;
            i++;
        }
    }

    public static IEnumerator Repeat(Action<float> action, float time, YieldInstruction interval = null, Action endAction = null, bool scaled = true) {
        float t = 0;
        while (t < time) {
            action.Invoke(t / (float)time);
            yield return interval;
            t += scaled ? Time.deltaTime : Time.unscaledDeltaTime;
        }

        endAction?.Invoke();
    }

    public static void SetCaller(EmptyMono m) {
        callDelayedHelper = m;
    }


    public static string ToNormalEnglish(string codeStyleString) {
        string result = Regex.Replace(codeStyleString, "(?<!^)([A-Z])", " $1");

        result = char.ToUpper(result[0]) + result.Substring(1);

        return result;
    }
}


public class VectorHelper {
    public static Vector3 Parse3(string s) {
        var pos = s.Split(',');
        return new Vector3(float.Parse(pos[0][1..]), float.Parse(pos[1]), float.Parse(pos[2][..(pos[2].Length - 2)]));
    }

    public static Vector2 Parse2(string s) {
        var pos = s.Split(',');
        return new Vector2(float.Parse(pos[0][1..]), float.Parse(pos[1][..(pos[1].Length - 2)]));
    }
}

public static class ColorFileManager {
    public static float h(this Color c) {
        Color.RGBToHSV(c, out float H, out _, out _);
        return H;
    }

    public static Color SetH(this Color c, float H) {
        Color.RGBToHSV(c, out _, out float S, out float V);
        return Color.HSVToRGB(H, S, V);
    }


    public static float s(this Color c) {
        Color.RGBToHSV(c, out _, out float S, out _);
        return S;
    }

    public static Color SetS(this Color c, float S) {
        Color.RGBToHSV(c, out float H, out _, out float V);
        return Color.HSVToRGB(H, S, V);
    }

    public static float v(this Color c) {
        Color.RGBToHSV(c, out _, out _, out float V);
        return V;
    }

    public static Color SetV(this Color c, float V) {
        Color.RGBToHSV(c, out float H, out float S, out _);
        return Color.HSVToRGB(H, S, V);
    }
}

public static class TMPHelper {
    public static void SetTextAni(this TextMeshProUGUI tmp, float start, float end, float t, string prefix = "", string postfix = "",
        int precision = 0) {
        float delta = (end - start) / (t / Time.deltaTime);
        tmp.StartCoroutine(Tools.Repeat((_) => { tmp.text = prefix + Math.Round(start += delta, precision).ToString() + postfix; }, t,
            new WaitForEndOfFrame(), endAction: () => tmp.text = prefix + end + postfix));

        //Tools.CallDelayed(() => {
        //    tmp.text = prefix + end + postfix;
        //}, t + 0.1f);
    }

    public static void SetTextAni(this TextMeshProUGUI tmp, float end, float t, string prefix = "", string postfix = "", int precision = 0) {
        try {
            tmp.SetTextAni(float.Parse(tmp.text[prefix.Length..(tmp.text.Length - postfix.Length)]), end, t, prefix, postfix, precision);
        } catch {
            tmp.SetTextAni(0, end, t, prefix, postfix, precision);
        }
    }
}

public static class floatHelper {
    public static float Lerp(this float a, float b, float percent) {
        return a + (b - a) * percent;
    }
}

//public class WaitForSecondsUnscaled : CustomYieldInstruction {
//    private float waitTime;

//    public override bool keepWaiting {
//        get {
//            return Time.realtimeSinceStartup < waitTime;
//        }
//    }

//    public WaitForSecondsUnscaled(float time) {
//        waitTime = Time.realtimeSinceStartup + time;
//    }
//}