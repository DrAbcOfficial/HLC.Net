using System;
using System.IO;

namespace HLC.Net.Setting;

public static class SettingManager
{
    public readonly static string[] SupportedLang = ["en-US", "zh-CN"];
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HLC.Net",
        "settings");
    public static int GetLangIndex(string lang)
    {
        for (int i = 0; i < SupportedLang.Length; i++)
        {
            if (SupportedLang[i] == lang)
                return i;
        }
        return -1;
    }
    public static void LoadSettings()
    {
        if (File.Exists(SettingsFilePath))
        {
            using StreamReader sr = new(SettingsFilePath);
            string? temp = sr.ReadLine();
            if (temp != null)
                UserSetting.Language = temp;
            temp = sr.ReadLine();
            if (temp != null)
                UserSetting.GamePath = temp;
        }
    }
    public static void SaveSettings()
    {
        var directory = Path.GetDirectoryName(SettingsFilePath)!;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using StreamWriter sw = new(SettingsFilePath);
        sw.WriteLine(UserSetting.Language);
        sw.WriteLine(UserSetting.GamePath);
    }
}
