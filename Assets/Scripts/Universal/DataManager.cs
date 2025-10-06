using System;
using UnityEngine;

public class DataManager {
    public static string fileContent;
    public static string fileName = "saves.txt";
    public static int stage;
    public static void SetFile(string str, bool inResources = false) {
        if (!inResources) {
            fileName = str;
            if (FileManager.ReadFile(fileName) == null) {
                FileManager.WriteInFile(fileName, "");
            }
            fileContent = FileManager.ReadFile(fileName);
        }
        else {
            fileContent = ResourceManager.Load<TextAsset>(fileName).text;
        }
    }

    public static void Write(string str, bool replace = true) {
        if (replace) FileManager.Delete(fileName);
        FileManager.WriteInFile(fileName, str);
        fileContent = FileManager.ReadFile(fileName);
    }

    public static string AddTag(object content, string tag) {
        return "<" + tag + ">" + content + "</" + tag + ">";
    }

    public static string GetTag(string s, string tag) {
        try {
            int idx = s.IndexOf("<" + tag + ">");
            if (idx != -1) {
                return s.Substring(idx + tag.Length + 2,
                    s[(idx + tag.Length + 2)..].IndexOf("</" + tag + ">"));
            }
        } catch (Exception e) { Debug.Log(e.Message); }

        Debug.Log("Tag Not Found! Tag: " + tag + "\nStr: " + s);
        return "";
    }

    public static string GetTag(string tag) {
        return GetTag(fileContent, tag);
    }
}
