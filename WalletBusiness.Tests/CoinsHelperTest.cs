namespace WalletBusiness.Tests;

public class CoinsHelperTest
{
    public static IEnumerable<object[]> CoinsTestData()
    {
        yield return new object[] {
            new List<int> { 25, 10, 5 },
            new List<int> { 1, 1, 1 },
            30,
            new List<int>{ 25, 5 } };
        yield return new object[] {
            new List<int> { 25, 10, 5 },
            new List<int> { 0, 3, 1 },
            30,
            new List<int>{ 10, 10, 10 } };
        yield return new object[] {
            new List<int> { 25, 10, 5 },
            new List<int> { 0, 1, 10 },
            30,
            new List<int>{ 10, 5, 5, 5, 5 } };
        yield return new object[] {
            new List<int> { 9, 6, 5, 1 },
            new List<int> { 1, 1, 1, 1 },
            11,
            new List<int> { 6, 5 } };
    }

    public static IEnumerable<object[]> QuantityDictionaryIsCreatedByCoinListData()
    {
        yield return new object[] {
            new List<int> { 50, 50, 50, 20 },
            new Dictionary<int, int>()
            {
                {50, 3 },
                {20, 1 },
                {10, 0 },
                { 5, 0 },
                { 2, 0 },
                { 1, 0 }
            }
        };
        yield return new object[] {
            new List<int> { 50, 50, 50, 20, 10 },
            new Dictionary<int, int>()
            {
                {50, 3 },
                {20, 1 },
                {10, 1 },
                { 5, 0 },
                { 2, 0 },
                { 1, 0 }

            }
        };
    }

    [Theory]
    [MemberData(nameof(CoinsTestData))]
    public void MinimumNumberOfCoinsIsCalculated(
        List<int> coins,
        List<int> quantities,
        int amount,
        List<int> expectedCoins)
    {
        var result = CoinsHelper.GetCoinsThatAddUpAmount(coins, quantities, amount);

        Assert.Equal(expectedCoins, result);
    }

    [Theory]
    [MemberData(nameof(QuantityDictionaryIsCreatedByCoinListData))]
    public void QuantityDictionaryIsCreatedByCoinList(
        List<int> coins,
        Dictionary<int, int> expected
        )
    {
        var result = CoinsHelper.CoinListToQuantityDictionary(coins);
        Assert.Equal(expected, result);
    }
}

