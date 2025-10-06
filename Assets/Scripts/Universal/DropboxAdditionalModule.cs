using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

#if DEBUG
using UnityEditor;
[CustomEditor(typeof(DropboxAdditionalModule))]
public class DropboxInspector : Editor {
    public override void OnInspectorGUI() {

        DropboxAdditionalModule dropbox = (DropboxAdditionalModule)target;
        if (dropbox.dropdown == null) {
            dropbox.dropdown = dropbox.GetComponent<TMP_Dropdown>();
        }

        if (dropbox.changeSettings) {

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Corresponding Variable", GUILayout.Width(150));

            var allFields = typeof(SettingsData).GetFields();
            List<string> fields = new();
            foreach (var f in allFields) {
                if (f.FieldType.IsEnum) {
                    fields.Add(f.Name);
                }
            }

            var names = new string[fields.Count];
            int i = 0;
            foreach (var field in fields) {
                names[i] = Tools.ToNormalEnglish(field);
                i++;
            }
            int selected = fields.IndexOf(dropbox.variableName);
            if (selected == -1) {
                selected = 0;
            }
            selected = EditorGUILayout.Popup(selected, names);
            dropbox.variableName = fields[selected];

            EditorGUILayout.EndHorizontal();
        }
        else {
            dropbox.variable = null;
        }

        base.OnInspectorGUI();
    }
}
#endif

//[RequireComponent(typeof(TMP_Dropdown))]
public class DropboxAdditionalModule : MonoBehaviour, IPointerDownHandler {
    public TMP_Dropdown dropdown;

    public bool changeSettings = true;


    [HideInInspector]
    public FieldInfo variable;

    [HideInInspector]
    public string variableName;

    //public FieldInfo variable {
    //    get { return _variable; }
    //    set {
    //        if (value == null) {
    //            Debug.Log("Variable is Null");
    //        }
    //        else if (_variable == null) {
    //            Debug.Log("Variable Assigned");
    //        }
    //        _variable = value;
    //    }
    //}

    public void Awake() {
        variable = typeof(SettingsData).GetField(variableName);
        if (dropdown == null) {
            dropdown = GetComponent<TMP_Dropdown>();
        }
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(SetValue);
        Settings.onSettingsLoaded += UpdateData;
    }

    public void OnDestroy() {
        Settings.onSettingsLoaded -= UpdateData;
    }

    public void UpdateData() {
        dropdown.value = (int)variable.GetValue(Settings.data);
        dropdown.RefreshShownValue();
    }

    public void SetValue(int i) {
        variable.SetValue(Settings.data, dropdown.value);
        Settings.SaveSettings();
    }

    public void OnPointerDown(PointerEventData eventData) {
        SoundSys.PlaySound("SFX/Collect");
    }
}
