using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

using System.Reflection;





#if DEBUG
using UnityEditor;
[CustomEditor(typeof(Checkbox))]
public class CheckboxInspector : Editor {
    public override void OnInspectorGUI() {

        Checkbox checkbox = (Checkbox)target;

        if (checkbox.changeSettings) {

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Corresponding Variable", EditorStyles.boldLabel, GUILayout.Width(150));

            var allFields = typeof(SettingsData).GetFields();
            List<string> fields = new();
            foreach (var f in allFields) {
                if (f.FieldType == typeof(bool)) {
                    fields.Add(f.Name);
                }
            }

            var names = new string[fields.Count];
            int i = 0;
            foreach (var field in fields) {
                names[i] = Tools.ToNormalEnglish(field);
                i++;
            }
            int selected = fields.IndexOf(checkbox.variableName);
            if (selected == -1) {
                selected = 0;
            }
            selected = EditorGUILayout.Popup(selected, names);
            checkbox.variableName = fields[selected];

            EditorGUILayout.EndHorizontal();
        }
        else {
            checkbox.variable = null;
        }

        base.OnInspectorGUI();
    }
}
#endif

public class Checkbox : Button {
    [Header("Is it a settings option?")]
    public bool changeSettings = true;

    public UnityEvent<bool> onValueChanged;
    public CustomUIElement toggle;
    public bool _value = false;
    public float toggleFadeTime = 0;

    [HideInInspector]
    public string variableName;
    [HideInInspector]
    public FieldInfo variable;

    public bool value {
        get { return _value; }
        set {
            _value = value;
            if (onValueChanged != null) {
                onValueChanged.Invoke(value);
            }
            if (variable != null) {
                variable.SetValue(Settings.data, _value);
                Settings.SaveSettings();
            }
            UpdateUI();
        }
    }


    public override void Awake() {
        variable = typeof(SettingsData).GetField(variableName);
        base.Awake();
        UpdateUI();
        if (changeSettings) {
            Settings.onSettingsLoaded += UpdateData;
        }
        //toggle.SetActive(Activated);
        //_Activated = Activated;
    }

    public void OnDestroy() {
        Settings.onSettingsLoaded -= UpdateData;
    }

    public override void OnMouseDown() {
        base.OnMouseDown();
        SoundSys.PlaySound("SFX/Collect");
    }

    public override void OnMouseUpAsButton() {
        base.OnMouseUpAsButton();
        value = !value;
    }

    public void UpdateData() {
        value = (bool)variable.GetValue(Settings.data);
    }

    public void UpdateUI() {
        if (!value) {
            toggle.SetAttrAni(0, toggleFadeTime, ColorAttr.a, true, scaled: false);
        }
        else {
            toggle.SetActive(true);
            toggle.SetAttrAni(0, 1, toggleFadeTime, ColorAttr.a, scaled: false);
        }
    }

}