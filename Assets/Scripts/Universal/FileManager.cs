using System;
using System.IO;
using UnityEngine;

public class FileManager {
    public static string ReadFile(string fileName) {
        try {
            StreamReader sr = new(Application.persistentDataPath + "/" + fileName);
            string s = sr.ReadToEnd();
            sr.Close();
            return s;
        } catch (Exception e) {
            Debug.Log("Exception: " + e.Message);
            return "";
        }
    }
    public static void WriteInFile(string fileName, string str) {
        if (!File.Exists(Application.persistentDataPath + "/" + fileName)) {
            File.Create(Application.persistentDataPath + "/" + fileName).Dispose();
        }
        StreamWriter sw = new(Application.persistentDataPath + "/" + fileName);
        sw.Write(str);
        sw.Close();
    }

    public static void Delete(string path) {
        try {
            File.Delete(Application.dataPath + "/" + path);
        } catch (Exception e) {
            Debug.Log(e.Message);
        }
    }

}
