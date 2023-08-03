namespace WalletBusiness.Tests;

public class CoinsHelperTest
{
    public static IEnumerable<object[]> Data_for_list_of_coins_addup_amount()
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

    public static IEnumerable<object[]> Data_for_list_of_coins_addup_amount_unlimited_availability()
    {
        yield return new object[] {
            new List<int> { 25, 10, 5 },
            30,
            new List<int>{ 25, 5 } };
        yield return new object[] {
            new List<int> { 25, 10 },
            30,
            new List<int>{ 10, 10, 10 } };
        yield return new object[] {
            new List<int> { 5 },
            30,
            new List<int>{ 5, 5, 5, 5, 5, 5 } };
        yield return new object[] {
            new List<int> { 9, 6, 5, 1 },
            11,
            new List<int> { 6, 5 } };
    }

    public static IEnumerable<object[]> Data_for_Create_quantity_dictionary_by_list_of_coins()
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

    public static IEnumerable<object[]> Data_for_coin_sets_addition()
    {
        yield return new object[] {
            new Dictionary<int, int>()
            {
                {50, 3 },
                {20, 1 },
                {10, 0 },
                { 5, 0 },
                { 2, 0 },
                { 1, 0 }
            },
            new Dictionary<int, int>()
            {
                {50, 1 },
                {20, 1 },
                {10, 0 },
                { 5, 0 },
                { 2, 1 },
                { 1, 0 }
            },
            new Dictionary<int, int>()
            {
                {50, 4 },
                {20, 2 },
                {10, 0 },
                { 5, 0 },
                { 2, 1 },
                { 1, 0 }
            }
        };
    }

    [Theory]
    [MemberData(nameof(Data_for_list_of_coins_addup_amount))]
    public void Get_list_of_coins_that_add_up_amount_when_there_is_limited_amount_of_coins_available(
        List<int> denomations,
        List<int> quantities,
        int amount,
        List<int> expected)
    {
        var result = CoinsHelper.GetCoinsThatAddUpAmount(denomations, quantities, amount);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(Data_for_list_of_coins_addup_amount_unlimited_availability))]
    public void Get_list_of_coins_that_add_up_amount_when_there_is_unlimited_amount_of_coins_available(
        List<int> denomations,
        int amount,
        List<int> expected)
    {
        var result = CoinsHelper.GetCoinsThatAddUpAmountInf(denomations, amount);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(Data_for_Create_quantity_dictionary_by_list_of_coins))]
    public void Create_quantity_dictionary_by_list_of_coins(
        List<int> coins,
        Dictionary<int, int> expected
        )
    {
        var result = CoinsHelper.CoinListToQuantityDictionary(coins);
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(Data_for_coin_sets_addition))]
    public void Coin_quantities_are_added_together(
        Dictionary<int, int> set1,
        Dictionary<int, int> set2,
        Dictionary<int, int> expected
        )
    {
        var result = CoinsHelper.AddCoinQuantities(set1, set2);
        Assert.Equal(expected, result);
    }
}

