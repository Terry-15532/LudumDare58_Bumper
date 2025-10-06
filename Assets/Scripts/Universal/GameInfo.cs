using UnityEngine;

public enum UIType {
    PC, Mobile
}

public static class GameInfo {
    public static string currScene;
    private static RectTransform _canvasRectTransform;
    private static Camera _UICamera, _mainCamera;
    public static UIType UIType = UIType.Mobile;
    public static Camera mainCamera {
        get {
            if (_mainCamera != null) {
                return _mainCamera;
            }
            else {
                _mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
                return _mainCamera;
            }
        }
    }

    public static Camera UICamera {
        get {
            if (_UICamera != null) {
                return _UICamera;
            }
            else {
                _UICamera = GameObject.Find("UICamera").GetComponent<Camera>();
                return _UICamera;
            }
        }
    }

    public static RectTransform canvasRectTransform {
        get {
            if (!_canvasRectTransform) {
                _canvasRectTransform = GameObject.Find("MainCanvas").GetComponent<RectTransform>();
            }
            return _canvasRectTransform;
        }
        set { _canvasRectTransform = value; }
    }
}
