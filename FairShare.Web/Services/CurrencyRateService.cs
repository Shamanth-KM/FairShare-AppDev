using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace FairShare.Web.Services
{
    public class CurrencyRateService : ICurrencyRateService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CurrencyRateService> _logger;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

        public CurrencyRateService(HttpClient http, IMemoryCache cache, ILogger<CurrencyRateService> logger)
        {
            _http = http;
            _cache = cache;
            _logger = logger;
        }

        public async Task<decimal?> GetRateAsync(string from, string to, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)) return null;
            from = from.Trim().ToUpperInvariant();
            to   = to.Trim().ToUpperInvariant();
            if (from == to) return 1m;

            var cacheKey = $"fx:{from}:{to}";
            if (_cache.TryGetValue(cacheKey, out decimal cached))
                return cached;

            // Try Frankfurter (no key)
            var rate = await TryFrankfurter(from, to, ct);
            if (rate is null)
            {
                // Fallback: exchangerate.host (no key)
                rate = await TryExchangerateHost(from, to, ct);
            }

            if (rate is null)
            {
                _logger.LogWarning("FX rate fetch failed for {From}->{To}", from, to);
                return null;
            }

            _cache.Set(cacheKey, rate.Value, CacheTtl);
            return rate;
        }

        private async Task<decimal?> TryFrankfurter(string from, string to, CancellationToken ct)
        {
            try
            {
                var url = $"https://api.frankfurter.app/latest?from={from}&to={to}";
                var json = await _http.GetFromJsonAsync<FrankfurterLatest>(url, ct);
                if (json?.rates != null && json.rates.TryGetValue(to, out var v)) return v;
            }
            catch (Exception ex) { _logger.LogDebug(ex, "Frankfurter failed"); }
            return null;
        }

        private async Task<decimal?> TryExchangerateHost(string from, string to, CancellationToken ct)
        {
            try
            {
                var url = $"https://api.exchangerate.host/latest?base={from}&symbols={to}";
                var json = await _http.GetFromJsonAsync<ExchangerateHostLatest>(url, ct);
                if (json?.rates != null && json.rates.TryGetValue(to, out var v)) return v;
            }
            catch (Exception ex) { _logger.LogDebug(ex, "exchangerate.host failed"); }
            return null;
        }

        private sealed class FrankfurterLatest
        {
            public Dictionary<string, decimal> rates { get; set; } = new();
            public string @base { get; set; } = "";
            public string date { get; set; } = "";
        }

        private sealed class ExchangerateHostLatest
        {
            public bool success { get; set; }
            public Dictionary<string, decimal> rates { get; set; } = new();
            public string @base { get; set; } = "";
            public string date { get; set; } = "";
        }
    }
}
