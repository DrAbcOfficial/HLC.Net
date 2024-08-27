using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using HLC.Net.Setting;
using HLC.Net.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Color = Avalonia.Media.Color;

namespace HLC.Net.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    protected override void OnClosed(EventArgs e)
    {
        SettingManager.SaveSettings();
        base.OnClosed(e);
    }
    private int PixelLimit
    {
        get
        {
            if (DataContext is not MainWindowViewModel model)
                return 0;
            if (model.m_bUseSvenCoopLimit)
                return 14336;
            return 12288;
        }
    }

    private async void SendMessage(string message, Icon icon)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            CanResize = false,
            ContentMessage = message,
            ShowInCenter = true,
            CloseOnClickAway = true,
            SizeToContent = SizeToContent.WidthAndHeight,
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = icon,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.BorderOnly
        });
        await box.ShowWindowDialogAsync(this);
    }
    #region Button Commands
    public async void Button_OpenFile(object sender, RoutedEventArgs args)
    {
        try
        {
            var file = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });
            if (file == null || file.Count <= 0)
                return;
            if (DataContext is not MainWindowViewModel model)
                return;
            model.PreviewImage?.Dispose();
            model.PreviewImage = new Bitmap(file[0].OpenReadAsync().Result);
            model.Footer_LoadedPath = file[0].TryGetLocalPath()!;
            model.Footer_LastLoadTime = DateTime.Now.ToLongTimeString();
        }
        catch (Exception ex)
        {
            SendMessage(ex.ToString(), MsBox.Avalonia.Enums.Icon.Error);
        }
    }
    public async void Button_SaveFile(object sender, RoutedEventArgs args)
    {
        try
        {

            if (DataContext is not MainWindowViewModel model || model.PreviewImage == null)
            {
                SendMessage(Assets.Resources.Message_NullImage, MsBox.Avalonia.Enums.Icon.Warning);
                return;
            }
            if (model.PreviewImage.Size.Width * model.PreviewImage.Size.Height > PixelLimit)
            {
                SendMessage(Assets.Resources.Message_TooBigImage, MsBox.Avalonia.Enums.Icon.Warning);
                return;
            }
            var wadtype = new FilePickerFileType("GoldSrc WAD3")
            {
                Patterns = ["tempdecal.wad"],
                AppleUniformTypeIdentifiers = ["public.wad3"],
                MimeTypes = ["wad3/tempdecal"]
            };
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(UserSetting.GamePath),
                SuggestedFileName = "tempdecal",
                DefaultExtension = ".wad",
                FileTypeChoices = [wadtype]
            });
            if (file == null)
                return;
            using MemoryStream memoryStream = new();
            model.PreviewImage.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            using Image<Rgba32> imageSharpImage = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);
            IQuantizer quantizer = model.SelectedQuantizer switch
            {
                0 => new WuQuantizer(new QuantizerOptions
                {
                    Dither = null,
                    MaxColors = model.m_bIsAlphaTest ? 255 : 256
                }),
                1 => new WernerPaletteQuantizer(new QuantizerOptions
                {
                    Dither = null,
                    MaxColors = model.m_bIsAlphaTest ? 255 : 256
                }),
                2 => new OctreeQuantizer(new QuantizerOptions
                {
                    Dither = null,
                    MaxColors = model.m_bIsAlphaTest ? 255 : 256
                }),
                3 => new WebSafePaletteQuantizer(new QuantizerOptions
                {
                    Dither = null,
                    MaxColors = model.m_bIsAlphaTest ? 255 : 256
                }),
                _ => new WuQuantizer(new QuantizerOptions
                {
                    Dither = null,
                    MaxColors = model.m_bIsAlphaTest ? 255 : 256
                }),
            };
            imageSharpImage.Mutate(x => x.Quantize(quantizer));
            //生成色板
            List<KeyValuePair<Rgba32, byte>> palette = [];
            for (int j = 0; j < imageSharpImage.Height; j++)
            {
                for (int i = 0; i < imageSharpImage.Width; i++)
                {
                    Rgba32 rgba32 = imageSharpImage[i, j];
                    if (!palette.Exists(x => x.Key == rgba32))
                        palette.Add(new KeyValuePair<Rgba32, byte>(rgba32, (byte)palette.Count));
                }
            }
            if (palette.Count < (model.m_bIsAlphaTest ? 255 : 256))
            {
                for (int i = palette.Count; i < (model.m_bIsAlphaTest ? 255 : 256); i++)
                {
                    palette.Add(new KeyValuePair<Rgba32, byte>(new Rgba32(0, 0, 0, 255), (byte)palette.Count));
                }
            }
            if (model.m_bIsAlphaTest)
                palette.Add(new KeyValuePair<Rgba32, byte>(new Rgba32(0, 0, 255, 255), 255));

            int size = imageSharpImage.Width * imageSharpImage.Height;
            await using BinaryWriter sw = new(await file.OpenWriteAsync());
            //Magic code
            //WAD3
            sw.Write(Encoding.UTF8.GetBytes("WAD3"));
            //Head buf
            sw.Write((uint)1);
            //Lump offset
            sw.Write((uint)0);
            //mips
            //char name[16];
            sw.Write(Encoding.UTF8.GetBytes("{LOGO\0\0\0\0\0\0\0\0\0\0\0"));
            //unsigned int width;
            sw.Write((uint)imageSharpImage.Width);
            //unsigned int height;
            sw.Write((uint)imageSharpImage.Height);
            //unsigned int offsets[4];
            sw.Write((uint)(40));
            sw.Write((uint)(40 + size));
            sw.Write((uint)(40 + size + (size / 4)));
            sw.Write((uint)(40 + size + (size / 4) + (size / 16)));
            //mips data
            for (int mips = 0; mips < 4; mips++)
            {
                int lv = (int)Math.Pow(2, mips);
                for (int j = 0; j < imageSharpImage.Height; j += lv)
                {
                    for (int i = 0; i < imageSharpImage.Width; i += lv)
                    {
                        Rgba32 rgba32 = imageSharpImage[i, j];
                        if (model.m_bIsAlphaTest && rgba32.A <= 128)
                            sw.Write(palette.Last().Value);
                        else
                        {
                            var c = palette.Find(x => x.Key == rgba32);
                            sw.Write(c.Value);
                        }
                    }
                }
            }
            //color used
            sw.Write((short)256);
            //Palette
            foreach (var c in palette)
            {
                sw.Write(c.Key.R);
                sw.Write(c.Key.G);
                sw.Write(c.Key.B);
            }
            static int RequiredPadding(int length, int padToMultipleOf)
            {
                var excess = length % padToMultipleOf;
                return excess == 0 ? 0 : padToMultipleOf - excess;
            }
            int pad = RequiredPadding((int)sw.BaseStream.Position, 4);
            //dummy pad
            for(int i = 0; i < pad; i++)
            {
                sw.Write((byte)0x00);
            }
            long lumpoffset = sw.BaseStream.Position;
            //lump
            //lump int textureoffset;
            sw.Write(12);
            int sizeOnDisk = 40 + size + (size / 4) + (size / 16) + (size / 64) + sizeof(short) + 256 * 3 + RequiredPadding(2 + 256 * 3, 4);
            //lump int sizeOnDisk;
            //lump int size;
            sw.Write(sizeOnDisk);
            sw.Write(sizeOnDisk);
            //lump char type;
            sw.Write((byte)0x43);
            //lump bool compression;
            sw.Write((byte)0x00);
            //lump short dummy;
            sw.Write((short)0x0000);
            //lump char name[16];
            sw.Write(Encoding.UTF8.GetBytes("{LOGO\0\0\0\0\0\0\0\0\0\0\0"));
            //lump offset
            sw.Seek(8, SeekOrigin.Begin);
            sw.Write((uint)lumpoffset);

            if (model.m_bSaveWithImage)
                imageSharpImage.SaveAsPng(Path.ChangeExtension(file.TryGetLocalPath()!, "png"));
            SendMessage(Assets.Resources.Message_SaveDone, MsBox.Avalonia.Enums.Icon.Success);
        }
        catch (Exception ex)
        {
            SendMessage(ex.ToString(), MsBox.Avalonia.Enums.Icon.Error);
        }
    }
    public async void Button_OpenConfig(object sender, RoutedEventArgs args)
    {
        var createNew = new ConfigWindow();
        var createNewData = new ConfigWindowViewModel();
        createNew.DataContext = createNewData;
        await createNew.ShowDialog(this);
    }
    public void Button_OpenHelp(object sender, RoutedEventArgs args)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/DrAbcOfficial/HLC.Net/wiki",
            UseShellExecute = true
        });
    }
    public async void Button_OpenAbout(object sender, RoutedEventArgs args)
    {
        var createNew = new AboutWindow();
        await createNew.ShowDialog(this);
    }
    public void Button_Exit(object sender, RoutedEventArgs args)
    {
        Close();
    }
    #endregion

    #region Content Commands
    public void Content_MaxOutWidth(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.ImageHeight <= 0)
            return;
        model.ImageWidth = PixelLimit / model.ImageHeight;
        Content_AutoResize(sender, args);
    }
    public void Content_MaxOutHeight(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.ImageWidth <= 0)
            return;
        model.ImageHeight = PixelLimit / model.ImageWidth;
        Content_AutoResize(sender, args);
    }
    public void Content_AutoResize(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model)
            return;
        float fw = model.ImageWidth;
        float fh = model.ImageHeight;
        if (fw * fh > PixelLimit)
        {
            if (fw > fh)
            {
                fh = fh / fw * 256.0f;
                fw = 256.0f;
            }
            else
            {
                fw = fw / fh * 256.0f;
                fh = 256.0f;
            }
            while (fw * fh > PixelLimit)
            {
                fw *= 0.97f;
                fh *= 0.97f;
            }
        }
        int w = (int)Math.Round(fw);
        int h = (int)Math.Round(fh);
        const int gap = 16;
        int dw = w % gap;
        if (dw > gap / 2)
            w += gap - dw;
        else
            w -= dw;
        int dh = h % gap;
        if (dh > gap / 2)
            h += gap - dh;
        else
            h -= dh;
        model.ImageWidth = w;
        model.ImageHeight = h;
    }
    public void Content_ApplyResize(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.PreviewImage == null)
        {
            SendMessage(Assets.Resources.Message_NullImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        int width = model.ImageWidth;
        int height = model.ImageHeight;
        if (width * height > PixelLimit)
        {
            SendMessage(Assets.Resources.Message_TooBigImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        var renderTarget = new RenderTargetBitmap(new PixelSize(width, height));
        using var context = renderTarget.CreateDrawingContext();
        context.DrawImage(model.PreviewImage, new Rect(0, 0, width, height));
        model.PreviewImage.Dispose();
        model.PreviewImage = renderTarget;
    }
    #endregion

    #region Operate Commands
    private readonly LinkedList<Bitmap> m_aryBitmaps = [];
    public void Operate_Undo(object sender, RoutedEventArgs args)
    {
        if (m_aryBitmaps.Count <= 1)
            return;
        if (DataContext is not MainWindowViewModel model)
            return;
        Bitmap bitmap = m_aryBitmaps.Last();
        m_aryBitmaps.RemoveLast();
        model.PreviewImage?.Dispose();
        model.PreviewImage = bitmap;
    }
    private Bitmap AdjustBitmap(Bitmap bitmap, Action<IImageProcessingContext> action)
    {
        m_aryBitmaps.AddLast(bitmap);
        //max deepth = 5
        if (m_aryBitmaps.Count > 5)
        {
            Bitmap first = m_aryBitmaps.First();
            m_aryBitmaps.RemoveFirst();
            first.Dispose();
        }
        using MemoryStream memoryStream = new();
        bitmap.Save(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        Image<Rgba32> imageSharpImage = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);
        imageSharpImage.Mutate(action);
        using MemoryStream saveStream = new();
        imageSharpImage.SaveAsPng(saveStream);
        saveStream.Seek(0, SeekOrigin.Begin);
        Bitmap adjustedAvaloniaBitmap = new(saveStream);
        return adjustedAvaloniaBitmap;
    }
    public void Operate_Sharper(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.PreviewImage == null)
        {
            SendMessage(Assets.Resources.Message_NullImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        if (model.PreviewImage.Size.Width * model.PreviewImage.Size.Height > PixelLimit)
        {
            SendMessage(Assets.Resources.Message_TooBigImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        model.PreviewImage = AdjustBitmap(model.PreviewImage, x => x.GaussianSharpen());
    }
    public void Operate_Blur(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.PreviewImage == null)
        {
            SendMessage(Assets.Resources.Message_NullImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        if (model.PreviewImage.Size.Width * model.PreviewImage.Size.Height > PixelLimit)
        {
            SendMessage(Assets.Resources.Message_TooBigImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        model.PreviewImage = AdjustBitmap(model.PreviewImage, x => x.GaussianBlur());
    }
    public void Operate_MoreBright(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.PreviewImage == null)
        {
            SendMessage(Assets.Resources.Message_NullImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        if (model.PreviewImage.Size.Width * model.PreviewImage.Size.Height > PixelLimit)
        {
            SendMessage(Assets.Resources.Message_TooBigImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        model.PreviewImage = AdjustBitmap(model.PreviewImage, x => x.Brightness(1.1f));
    }
    public void Operate_LessBright(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.PreviewImage == null)
        {
            SendMessage(Assets.Resources.Message_NullImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        if (model.PreviewImage.Size.Width * model.PreviewImage.Size.Height > PixelLimit)
        {
            SendMessage(Assets.Resources.Message_TooBigImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        model.PreviewImage = AdjustBitmap(model.PreviewImage, x => x.Brightness(0.9f));
    }
    public void Operate_MoreContrast(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.PreviewImage == null)
        {
            SendMessage(Assets.Resources.Message_NullImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        if (model.PreviewImage.Size.Width * model.PreviewImage.Size.Height > PixelLimit)
        {
            SendMessage(Assets.Resources.Message_TooBigImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        model.PreviewImage = AdjustBitmap(model.PreviewImage, x => x.Contrast(1.1f));
    }
    public void Operate_LessContrast(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel model || model.PreviewImage == null)
        {
            SendMessage(Assets.Resources.Message_NullImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        if (model.PreviewImage.Size.Width * model.PreviewImage.Size.Height > PixelLimit)
        {
            SendMessage(Assets.Resources.Message_TooBigImage, MsBox.Avalonia.Enums.Icon.Warning);
            return;
        }
        model.PreviewImage = AdjustBitmap(model.PreviewImage, x => x.Contrast(0.9f));
    }
    #endregion
}