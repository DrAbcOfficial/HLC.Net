using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace HLC.Net.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Property
#pragma warning disable CA1822 // Mark members as static
    private int m_iImageWidth = 0;
    public int ImageWidth
    {
        get => m_iImageWidth;
        set
        {
            m_iImageWidth = value;
            m_iPixelCount = m_iImageHeight * m_iImageWidth;
            OnPropertyChanged(nameof(PixelCount));
            OnPropertyChanged(nameof(ImageWidth));
        }
    }
    private int m_iImageHeight = 0;
    public int ImageHeight
    {
        get => m_iImageHeight;
        set
        {
            m_iImageHeight = value;
            m_iPixelCount = m_iImageHeight * m_iImageWidth;
            OnPropertyChanged(nameof(PixelCount));
            OnPropertyChanged(nameof(ImageHeight));
        }
    }
    private int m_iPixelCount = 0;
    public string PixelCount
    {
        get
        {
            string temp;
            //Half life Cant use
            if (m_iPixelCount > 12288)
            {
                if (m_iPixelCount > 14336)
                {
                    m_cPixelCountColor = Color.FromArgb(255, 255, 100, 100);
                    temp = $"{m_iPixelCount} (Invalid Size)";
                }
                else
                {
                    m_cPixelCountColor = Color.FromArgb(255, 255, 255, 100);
                    temp = $"{m_iPixelCount} (Sven Co-op Only)";
                }
            }
            else
            {
                m_cPixelCountColor = Color.FromArgb(255, 144, 144, 144);
                temp = m_iPixelCount.ToString();
            }
            OnPropertyChanged(nameof(PixelCountColor));
            return temp;
        }
    }
    private Color m_cPixelCountColor;
    public string PixelCountColor
    {
        get => m_cPixelCountColor.ToString();
        set => m_cPixelCountColor = Color.Parse(value);
    }

    public bool m_bUseSvenCoopLimit = true;
    public string UseSvenCoopLimit
    {
        get => m_bUseSvenCoopLimit.ToString();
        set => m_bUseSvenCoopLimit = bool.Parse(value);
    }
    public bool m_bIsAlphaTest = true;
    public string IsAlphaTest
    {
        get => m_bIsAlphaTest.ToString();
        set => m_bIsAlphaTest = bool.Parse(value);
    }
    public bool m_bSaveWithImage = false;
    public string SaveWithImage
    {
        get => m_bSaveWithImage.ToString();
        set => m_bSaveWithImage = bool.Parse(value);
    }
    public int SelectedQuantizer { get; set; } = 0;

    private Bitmap? m_pBitmap;
    public Bitmap? PreviewImage
    {
        get => m_pBitmap;
        set
        {
            m_pBitmap = value;
            m_iImageWidth = m_pBitmap!.PixelSize.Width;
            m_iImageHeight = m_pBitmap!.PixelSize.Height;
            m_iPixelCount = m_iImageHeight * m_iImageWidth;
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(ImageWidth));
            OnPropertyChanged(nameof(ImageHeight));
            OnPropertyChanged(nameof(PixelCount));
            OnPropertyChanged(nameof(PixelCountColor));
            OnPropertyChanged(nameof(Footer_ImageSize));
        }
    }
    private string m_szFooter_LoadedPath = string.Empty;
    public string Footer_LoadedPath
    {
        get => m_szFooter_LoadedPath;
        set
        {
            m_szFooter_LoadedPath = value;
            OnPropertyChanged(nameof(Footer_LoadedPath));
        }
    }
    public string Footer_ImageSize
    {
        get
        {
            if (m_pBitmap != null)
                return m_pBitmap.Size.ToString();
            else
                return string.Empty;
        }
    }
    private string m_szFooter_LastLoadTime = string.Empty;
    public string Footer_LastLoadTime
    {
        get => m_szFooter_LastLoadTime;
        set
        {
            m_szFooter_LastLoadTime = value;
            OnPropertyChanged(nameof(Footer_LastLoadTime));
        }
    }
#pragma warning restore CA1822 // Mark members as static
    #endregion
}
