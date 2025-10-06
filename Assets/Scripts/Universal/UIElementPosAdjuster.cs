using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public enum RefMode{
	Screen,
	Parent
}

#if DEBUG
[ExecuteAlways]
#endif
public class UIElementPosAdjuster : MonoBehaviour{
	public static Vector2 _screenSize = new Vector2(Screen.width, Screen.height);
	public Vector2 position;
	public Vector2 ratio;
	public RectTransform rectTransform;
	public static List<UIElementPosAdjuster> elements = new();
	public RefMode mode;

#if !DEBUG
    public static Vector2 screenSize {
        get { return _screenSize; }
        set {
            _screenSize = value;
            foreach (var e in elements) {
                e.UpdateUI();
            }
        }
    }

    void Awake() {
        elements.Add(this);
    }

    void UpdateUI() {
        rectTransform = GetComponent<RectTransform>();
        var p = transform.parent.GetComponent<RectTransform>();
        if (mode == RefMode.Parent) {
            Vector2 refSize;
            if (p != null) {
                refSize = p.sizeDelta;
            }
            else {
                refSize = screenSize;
            }
            rectTransform.anchoredPosition = refSize * ratio;
        }
        else {
            Vector2 screenPos = screenSize * ratio;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(p, screenPos, GameInfo.UICamera, out Vector2 localPoint);
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnDestroy() {
        elements.Remove(this);
    }
#endif

#if DEBUG
	public static Vector2 screenSize;

	public static Vector2 designedScreenSize = new(2560, 1440);

	public void OnEnable(){
		rectTransform = GetComponent<RectTransform>();

		UpdateRatio();
	}

	public void Update(){
		UpdateRatio();
	}

	public void UpdateRatio(){
		Vector2 refSize;
		var p = transform.parent.GetComponent<RectTransform>();
		if (mode == RefMode.Parent){
			if (p != null){
				refSize = p.sizeDelta;
			}
			else{
				refSize = designedScreenSize;
			}

			position = rectTransform.anchoredPosition;
		}
		else{
			position = RectTransformUtility.WorldToScreenPoint(GameInfo.UICamera, rectTransform.position);
			refSize = designedScreenSize;
		}

		ratio = position / refSize;
	}
#endif
}