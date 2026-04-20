[System.Serializable]
public class BitwiseEnum
{
    public int intValue;

    public BitwiseEnum(int intValue) => this.intValue = intValue;
    public BitwiseEnum(params bool[] inputs)
    {
        intValue = 0;
        for (int i = 0; i < inputs.Length; i++) intValue |= 1 << i;
    }
    public static implicit operator int(BitwiseEnum value) => value.intValue;
    public static implicit operator BitwiseEnum(int value) => value;
    public static implicit operator BitwiseEnum(bool[] inputs) => new(inputs);

    public bool this[int i]
    {
        get => (intValue & (1 << i)) != 0;
        set
        {
            if (value) intValue |= 1 << i;
            else intValue &= ~(1 << i);
        }
    }
}