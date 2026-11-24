using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using JsonModel;
using System.Net.Http;
using System.Net.Http.Json;

namespace WarframeRelicHelper;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Dictionary<string, string> _primeItems = new();
    private HttpClient _httpClient = new();

    public MainWindow()
    {
        InitializeComponent();

        CloudflareBypasser.NavigationCompleted += OnNavigationCompleted;

        string itemUrl = "https://api." + "warframe.market" + "/v2/items";
        CloudflareBypasser.Source = new Uri(itemUrl);
    }
    private async Task InitHttpClientFromWebViewAsync()
    {
        var baseUri = new Uri("https://api." + "warframe.market" + "/v2/");
        var webViewCookies = await CloudflareBypasser.CoreWebView2.CookieManager.GetCookiesAsync(baseUri.GetLeftPart(UriPartial.Authority));
        string userAgent = CloudflareBypasser.CoreWebView2.Settings.UserAgent;

        var cookieContainer = new System.Net.CookieContainer();
        foreach (var cookie in webViewCookies)
        {
            cookieContainer.Add(new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
        }

        var handler = new HttpClientHandler { CookieContainer = cookieContainer };
        _httpClient = new HttpClient(handler) { BaseAddress = baseUri };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        _httpClient.DefaultRequestHeaders.Add("Platform", "pc");
        _httpClient.DefaultRequestHeaders.Add("Language", "en");
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        string rawJson = await CloudflareBypasser.ExecuteScriptAsync("document.body.innerText");
        string cleanJson = JsonSerializer.Deserialize<string>(rawJson) ?? "";

        if (!cleanJson.TrimStart().StartsWith("{"))
        {
            StatusText.Text = "Doing CloudFlare check...(if checkbox will show press it)";
            return;
        }

        await InitHttpClientFromWebViewAsync();

        CloudflareBypasser.NavigationCompleted -= OnNavigationCompleted;
        var itemResponse = JsonSerializer.Deserialize<V2ItemsResponse>(cleanJson);
        if (itemResponse?.Data != null)
        {
            _primeItems = itemResponse.Data
                .Where(i => i.Slug.Contains("_prime_") && !i.Slug.EndsWith("_set"))
                .ToDictionary(i => i.I18n.En.Name.ToLowerInvariant(), i => i.Slug);
        }

        StatusText.Text = $"Check was done! Loading item base. Loaded: {_primeItems.Count}";

        int? price = await GetLowestPriceViaBrowserAsync("braton_prime_barrel");
        ResultText.Text = $"Price for braton prime barrel: {price} pl.";
    }

    private async Task<int?> GetLowestPriceViaBrowserAsync(string slug)
    {
        var orders = await _httpClient.GetFromJsonAsync<V2OrdersResponse>($"orders/item/{slug}");
        return orders?.Data?
            .Where(o => o.Type == "sell" && o.User.Status == "ingame")
            .OrderBy(o => o.Platinum)
            .Select(o => (int?)o.Platinum)
            .FirstOrDefault();
    }
}