using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.Net;
using Windows.Media.Ocr;
using System.Drawing;
using FuzzySharp;

namespace WarframeRelicHelper;
public partial class MainWindow : Window
{
    private MarketService _marketService = new();
    private OcrService _ocrService = new();

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

        int? count = _marketService.GetAllPrimeItems(cleanJson);
        StatusText.Text = $"Check was done! Loading item base. Loaded: {count}";

        await GetTextAsync();
    }

    private async Task GetTextAsync()
    {
        int? price = await _marketService.GetLowestPriceAsync("braton_prime_barrel");
        ResultText.Text = $"Price for braton prime barrel: {price} pl.\n";
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));

        using var fullBmp = new Bitmap("test.jpg");

        var rect = new Rectangle();

        var i = 0;
        while(i < 4)
        {
            rect.X = (int)(fullBmp.Width * (0.252 + i * 0.124));
            rect.Y = (int)(fullBmp.Height * 0.385);
            rect.Width = (int)(fullBmp.Width * 0.122);
            rect.Height = (int)(fullBmp.Height * 0.065);
            
            using var crop = fullBmp.Clone(rect, fullBmp.PixelFormat);

            string text = await _ocrService.RecognizeBitmapAsync(crop, engine);

            text += " | Price:" + await _marketService.GetPriceByRawNameAsync(text);

            ResultText.Text += text + "\n";

            i++;
        }
    }
}