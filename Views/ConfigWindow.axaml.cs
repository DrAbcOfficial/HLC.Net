using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HLC.Net.Setting;
using HLC.Net.ViewModels;
using System.IO;

namespace HLC.Net;

public partial class ConfigWindow : Window
{
    public ConfigWindow() => InitializeComponent();

    private void Button_Save(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SettingManager.SaveSettings();
        Close(true);
    }

    private void Button_Cancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }
    private async void Button_OpenGameFolder(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var type = new FilePickerFileType("Game Excute File")
        {
            Patterns = ["hl.exe", "svencoop.exe", "cstrike.exe", "bshift.exe", "tfc.exe"],
        };
        var file = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [type]
        });
        if (file == null || file.Count <= 0)
            return;
        var di = Directory.GetParent(file[0].TryGetLocalPath()!);
        if (di == null)
            return;
        if (DataContext is not ConfigWindowViewModel model)
            return;
        model.GamePath = di.FullName;
    }
}