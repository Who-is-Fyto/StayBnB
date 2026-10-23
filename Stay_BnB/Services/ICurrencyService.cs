namespace Stay_BnB.Services;

public class CurrencyInfo
{
    public string Code { get; set; } = "ZAR";
    public string Symbol { get; set; } = "R";
    public string Name { get; set; } = "South African Rand";
    public decimal Rate { get; set; } = 1.0m;
}

public interface ICurrencyService
{
    CurrencyInfo CurrentCurrency { get; }
    string CurrentCode { get; }
    string CurrentSymbol { get; }
    decimal CurrentRate { get; }
    IReadOnlyList<CurrencyInfo> AvailableCurrencies { get; }
    decimal Convert(decimal amountInZar, string? targetCode = null);
    string Format(decimal amountInZar, string? targetCode = null, bool includeDecimals = false);
}
