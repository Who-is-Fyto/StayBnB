using Microsoft.AspNetCore.Http;

namespace Stay_BnB.Services;

public class CurrencyService : ICurrencyService
{
    public const string CookieName = "StayBnB_Currency";
    public const string DefaultCurrencyCode = "ZAR";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public static readonly IReadOnlyList<CurrencyInfo> Currencies = new List<CurrencyInfo>
    {
        new CurrencyInfo { Code = "ZAR", Symbol = "R", Name = "South African Rand", Rate = 1.0m },
        new CurrencyInfo { Code = "USD", Symbol = "$", Name = "US Dollar", Rate = 0.055m },
        new CurrencyInfo { Code = "EUR", Symbol = "€", Name = "Euro", Rate = 0.051m },
        new CurrencyInfo { Code = "GBP", Symbol = "£", Name = "British Pound", Rate = 0.043m }
    };

    public CurrencyService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<CurrencyInfo> AvailableCurrencies => Currencies;

    public CurrencyInfo CurrentCurrency
    {
        get
        {
            var cookieValue = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
            if (!string.IsNullOrWhiteSpace(cookieValue))
            {
                var match = Currencies.FirstOrDefault(c => c.Code.Equals(cookieValue.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            return Currencies[0]; // Default to South African Rand (ZAR)
        }
    }

    public string CurrentCode => CurrentCurrency.Code;
    public string CurrentSymbol => CurrentCurrency.Symbol;
    public decimal CurrentRate => CurrentCurrency.Rate;

    public decimal Convert(decimal amountInZar, string? targetCode = null)
    {
        var currency = string.IsNullOrWhiteSpace(targetCode)
            ? CurrentCurrency
            : Currencies.FirstOrDefault(c => c.Code.Equals(targetCode.Trim(), StringComparison.OrdinalIgnoreCase)) ?? CurrentCurrency;

        return amountInZar * currency.Rate;
    }

    public string Format(decimal amountInZar, string? targetCode = null, bool includeDecimals = false)
    {
        var currency = string.IsNullOrWhiteSpace(targetCode)
            ? CurrentCurrency
            : Currencies.FirstOrDefault(c => c.Code.Equals(targetCode.Trim(), StringComparison.OrdinalIgnoreCase)) ?? CurrentCurrency;

        var converted = amountInZar * currency.Rate;
        if (includeDecimals || currency.Code != "ZAR")
        {
            return $"{currency.Symbol}{converted:N2}";
        }

        return converted % 1 == 0 ? $"{currency.Symbol}{converted:N0}" : $"{currency.Symbol}{converted:N2}";
    }
}
