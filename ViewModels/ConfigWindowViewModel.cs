using HLC.Net.Setting;

namespace HLC.Net.ViewModels;
public partial class ConfigWindowViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public int SelectedLanguage
    {
        get
        {
            return SettingManager.GetLangIndex(UserSetting.Language);
        }
        set
        {
            UserSetting.Language = SettingManager.SupportedLang[value];
        }
    }
    public string GamePath 
    { 
        get => UserSetting.GamePath;
        set
        {
            UserSetting.GamePath = value;
            OnPropertyChanged(nameof(GamePath));
        }
    }
#pragma warning restore CA1822 // Mark members as static
}
