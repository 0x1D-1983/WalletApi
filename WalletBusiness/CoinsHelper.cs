using WalletDomain;

namespace WalletBusiness;

public static class CoinsHelper
{
    public static List<int> Denominations = new List<int>() { 50, 20, 10, 5, 2, 1 };

    /// <summary>
    /// Get list of minimum number of coins that make a given value
    /// </summary>
    /// <param name="coins">List of coins</param>
    /// <param name="quantities">Availability of each coin</param>
    /// <param name="amount">The change amount</param>
    /// <returns></returns>
    public static List<int> GetCoinsThatAddUpAmount(
        List<int> coins, List<int> quantities, int amount)
    {
        var result = new PriorityQueue<List<int>, int>();
        var partial = new List<int>();
        List<int> backup = new List<int>();
        quantities.ForEach(backup.Add);

        coins = coins.OrderByDescending(_ => _).ToList();

        int k = 0;
        while (k < coins.Count)
        {
            int i = k;
            partial = new List<int>();

            while (i < coins.Count)
            {
                if (partial.Sum() == amount)
                {
                    break;
                }

                if (quantities[i] == 0)
                {
                    i++;
                    continue;
                }

                if (coins[i] <= amount - partial.Sum())
                {
                    partial.Add(coins[i]);
                    quantities[i] -= 1;
                    continue;
                }

                i++;
            }

            if (partial.Sum() == amount)
            {
                result.Enqueue(partial, partial.Count);
            }

            k++;
        }

        return result.Dequeue();
    }

    /// <summary>
    /// Get list of minimum number of coins that make a given value (infinite supply)
    /// </summary>
    /// <param name="coins"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static List<int> GetCoinsThatAddUpAmountInf(
        List<int> coins, int amount)
    {
        var result = new PriorityQueue<List<int>, int>();
        var partial = new List<int>();

        coins = coins.OrderByDescending(_ => _).ToList();

        int k = 0;
        while (k < coins.Count)
        {
            int i = k;
            partial = new List<int>();

            while (i < coins.Count)
            {
                if (partial.Sum() == amount)
                {
                    break;
                }

                if (coins[i] <= amount - partial.Sum())
                {
                    partial.Add(coins[i]);
                    continue;
                }

                i++;
            }

            if (partial.Sum() == amount)
            {
                result.Enqueue(partial, partial.Count);
            }

            k++;
        }

        return result.Dequeue();
    }

    public static Dictionary<int, int> AddCoinQuantities(Dictionary<int, int> set1, Dictionary<int, int> set2)
    {
        var result = InitQuantityDictionary();

        foreach (int key in result.Keys)
        {
            result[key] = set1[key] + set2[key];
        }

        return result;
    }

    public static Dictionary<int, int> SubtractCoinQuantities(Dictionary<int, int> set1, Dictionary<int, int> set2)
    {
        var result = InitQuantityDictionary();

        foreach (int key in result.Keys)
        {
            result[key] = set1[key] - set2[key];
        }

        return result;
    }

    /// <summary>
    /// Converts a list of coind to a dictionary where each coin has the quantity added
    /// </summary>
    /// <param name="coins">List of coins</param>
    /// <returns>Quantities</returns>
    public static Dictionary<int, int> CoinListToQuantityDictionary(List<int> coins)
    {
        var result = InitQuantityDictionary();

        foreach (int coin in coins)
        {
            result[coin]++;
        }

        return result;
    }

    public static Dictionary<int, int> InitQuantityDictionary() =>
        new Dictionary<int, int>
            {
                { 50, 0 },
                { 20, 0 },
                { 10, 0 },
                { 5, 0 },
                { 2, 0 },
                { 1, 0 }
            };

    public static BalanceDetailsModel ToBalanceDetailsModel(this decimal amount)
    {
        DecimalValue credit = new DecimalValue(amount);

        var coins = GetCoinsThatAddUpAmountInf(Denominations, credit.Nanos);
        var quantities = CoinListToQuantityDictionary(coins);

        return new BalanceDetailsModel(amount, credit.Units, quantities);
    }

    public static Dictionary<int, int> ToQuantityDictionary(this BalanceDetailsModel balanceDetails)
    {
        var result = InitQuantityDictionary();

        result[1] = balanceDetails.OnePenny;
        result[2] = balanceDetails.TwoPence;
        result[5] = balanceDetails.FivePence;
        result[10] = balanceDetails.TenPence;
        result[20] = balanceDetails.TwentyPence;
        result[50] = balanceDetails.FiftyPence;

        return result;
    }
}

