namespace WalletDomain;

public class DecimalValue
{
    public int Units { get; set; }
    public int Nanos { get; set; }

    public static readonly decimal Factor = 100;

    public DecimalValue(int units, int nanos)
    {
        Units = units;
        Nanos = nanos;
    }

    public DecimalValue(decimal value)
    {
        Units = decimal.ToInt32(value);
        Nanos = decimal.ToInt32((value - Units) * Factor);
    }

    public decimal ToDecimal() => Units + Nanos / Factor;

    public static readonly DecimalValue Zero = new DecimalValue(0, 0);
    public static readonly DecimalValue One = new DecimalValue(1, 0);

    public static implicit operator decimal(DecimalValue value) =>
        value?.ToDecimal() ?? decimal.Zero;

    public static implicit operator DecimalValue(decimal value) =>
        value switch
        {
            decimal.Zero => Zero,
            decimal.One => One,
            _ => new(value)
        };

}

