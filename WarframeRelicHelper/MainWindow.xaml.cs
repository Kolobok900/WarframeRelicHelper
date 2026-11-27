using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.Net;
using Windows.Media.Ocr;
using System.Drawing;
using FuzzySharp;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace WarframeRelicHelper;
public partial class MainWindow : Window
{
    const int GWL_EXSTYLE = -20;
    const int WS_EX_TRANSPARENT = 0x00000020;

    private MarketService _marketService = new();
    private OcrService _ocrService = new();
    private IntPtr _windowHandle;

    public MainWindow()
    {
        InitializeComponent();

        CloudflareBypasser.NavigationCompleted += OnNavigationCompleted;

        string itemUrl = "https://api." + "warframe.market" + "/v2/items";
        CloudflareBypasser.Source = new Uri(itemUrl);
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        var baseUri = new Uri("https://api." + "warframe.market" + "/v2/");

        string rawJson = await CloudflareBypasser.ExecuteScriptAsync("document.body.innerText");
        string cleanJson = JsonSerializer.Deserialize<string>(rawJson) ?? "";

        if (!cleanJson.TrimStart().StartsWith("{"))
        {
            StatusText.Text = "Doing CloudFlare check...(if checkbox will show press it)";
            return;
        }

        var webViewCookies = await CloudflareBypasser.CoreWebView2.CookieManager.GetCookiesAsync(baseUri.GetLeftPart(UriPartial.Authority));
        string userAgent = CloudflareBypasser.CoreWebView2.Settings.UserAgent;
        var cookieContainer = new CookieContainer();

        foreach (var cookie in webViewCookies)
        {
            cookieContainer.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
        }

        _marketService.Initialize(cookieContainer, userAgent);

        CloudflareBypasser.NavigationCompleted -= OnNavigationCompleted;
        
        CloudflareBypasser.Visibility = Visibility.Collapsed;

        SetWindowLong(_windowHandle, GWL_EXSTYLE, GetWindowLong(_windowHandle, GWL_EXSTYLE) | WS_EX_TRANSPARENT);

        int? count = _marketService.GetAllPrimeItems(cleanJson);
        StatusText.Text = $"Check was done! Loading item base. Loaded: {count}";

        await GetTextAsync();
    }

    private async Task GetTextAsync()
    {
        int? price = await _marketService.GetLowestPriceAsync("braton_prime_barrel");
        ResultText.Text = $"Price for braton prime barrel: {price} pl.\n";
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(_windowHandle)?.AddHook(HwndHook);

        RegisterHotKey(_windowHandle, 9000, 0x0000, 0x78);
    }
    protected override void OnClosed(EventArgs e)
    {

        base.OnClosed(e);
        UnregisterHotKey(_windowHandle, 9000);
    }
    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && wParam.ToInt32() == 9000)
        {
            ScanRewardsAsync();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private async void ScanRewardsAsync()
    {
        ResultText.Text = "";
        var engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));

        using var fullBmp = _ocrService.CaptureScreen();

        var rect = new Rectangle();

        rect.X = (int)(fullBmp.Width * 0.25);
        rect.Y = (int)(fullBmp.Height * 0.355);
        rect.Width = (int)(fullBmp.Width * 0.50);
        rect.Height = (int)(fullBmp.Height * 0.095);

        using var crop = fullBmp.Clone(rect, fullBmp.PixelFormat);

        var rewards = await _ocrService.RecognizeBitmapAsync(crop, engine);

        foreach (var reward in rewards)
        {
            try
            {
                ResultText.Text += $"{reward} | Price:" + await _marketService.GetPriceByRawNameAsync(reward.ToString()) + " \n";
            }
            catch
            {
                ResultText.Text += $"{reward} | Price: ? \n";
            }
            await Task.Delay(350);
        }
       
    }

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int index, int newStyle);
}