using JsonModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FuzzySharp;

namespace WarframeRelicHelper
{
    class MarketService
    {
        private Dictionary<string, string> _primeItems = new();
        private HttpClient _httpClient = new();

        public void Initialize(CookieContainer cookieContainer, string userAgent)
        {
            var baseUri = new Uri("https://api." + "warframe.market" + "/v2/");
            var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            _httpClient = new HttpClient(handler) { BaseAddress = baseUri };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            _httpClient.DefaultRequestHeaders.Add("Platform", "pc");
            _httpClient.DefaultRequestHeaders.Add("Language", "en");
        }

        public int GetAllPrimeItems(string cleanJson)
        {
            var itemResponse = JsonSerializer.Deserialize<V2ItemsResponse>(cleanJson);
            if (itemResponse?.Data != null)
            {
                _primeItems = itemResponse.Data
                    .Where(i => i.Slug.Contains("_prime_") && !i.Slug.EndsWith("_set"))
                    .ToDictionary(i => i.I18n.En.Name.ToLowerInvariant(), i => i.Slug);
            }
            var count = _primeItems.Count;

            return count;
        }

        public async Task<int?> GetLowestPriceAsync(string slug)
        {
            var orders = await _httpClient.GetFromJsonAsync<V2OrdersResponse>($"orders/item/{slug}");
            return orders?.Data?
                .Where(o => o.Type == "sell" && o.User.Status == "ingame")
                .OrderBy(o => o.Platinum)
                .Select(o => (int?)o.Platinum)
                .FirstOrDefault();
        }

        public async Task<int?> GetPriceByRawNameAsync(string ocrText)
        {
            var item = Process.ExtractOne(ocrText.ToLowerInvariant(), _primeItems.Keys);
            if (item.Score >= 85)
            {
                return await GetLowestPriceAsync(_primeItems[item.Value]);
            }
            return null;
        }
    }
}
