using System.Text.Json.Serialization;

namespace WalletDomain;

public record BalanceDetailsModel
{
    [JsonInclude]
    public decimal Total { get; set;}

    [JsonInclude]
    public int Pounds { get; set;}

    [JsonInclude]
    public int OnePenny { get; set;}

    [JsonInclude]
    public int TwoPence { get; set;}

    [JsonInclude]
    public int FivePence { get; set;}

    [JsonInclude]
    public int TenPence { get; set;}

    [JsonInclude]
    public int TwentyPence { get; set;}

    [JsonInclude]
    public int FiftyPence { get; set;}

    public BalanceDetailsModel()
    {
    }

    public BalanceDetailsModel(decimal value, int pounds, Dictionary<int, int> coins)
    {
        Total = value;
        Pounds = pounds;
        foreach (var kvp in coins.Where(_ => _.Value > 0))
        {
            switch (kvp.Key)
            {
                case 1:
                    OnePenny = kvp.Value;
                    break;
                case 2:
                    TwoPence = kvp.Value;
                    break;
                case 5:
                    FivePence = kvp.Value;
                    break;
                case 10:
                    TenPence = kvp.Value;
                    break;
                case 20:
                    TwentyPence = kvp.Value;
                    break;
                case 50:
                    FiftyPence = kvp.Value;
                    break;
            }
        }
    }
}