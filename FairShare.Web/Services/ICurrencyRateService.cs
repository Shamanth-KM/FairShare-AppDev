namespace FairShare.Web.Services
{
    public interface ICurrencyRateService
    {
        /// Returns the rate to convert 1 {from} into {to}; null if unavailable.
        Task<decimal?> GetRateAsync(string from, string to, CancellationToken ct = default);
    }
}
