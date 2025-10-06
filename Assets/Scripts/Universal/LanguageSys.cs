
public enum Language {
    English, Chinese
}

public static class LanguageSys {

    public static string LocalizeTxt(params string[] texts) {
        if ((int)Settings.data.language < texts.Length) {
            return texts[(int)Settings.data.language];
        }
        else {
            return texts[0];
        }
    }
}