using System.Collections.Generic;

[System.Serializable]
public class BitwiseEnum
{
    public int intValue;

    public BitwiseEnum(int intValue) => this.intValue = intValue;
    public BitwiseEnum(params bool[] inputs)
    {
        intValue = 0;
        for (int i = 0; i < inputs.Length; i++)
            if (inputs[i]) intValue |= 1 << i;
    }

    public bool this[int i]
    {
        get => (intValue & (1 << i)) != 0;
        set
        {
            if (value) intValue |= 1 << i;
            else intValue &= ~(1 << i);
        }
    }

    public static implicit operator int(BitwiseEnum value) => value.intValue;
    public static implicit operator BitwiseEnum(int value) => value;
    public static implicit operator BitwiseEnum(bool[] inputs) => new(inputs);

    public static BitwiseEnum operator |(BitwiseEnum L, BitwiseEnum R)
    {
        if (R is null) return L;
        if (L is null) return R;
        L.intValue |= R.intValue;
        return L;
    }
    public static BitwiseEnum operator &(BitwiseEnum L, BitwiseEnum R)
    {
        if (R is null) return L;
        if (L is null) return R;
        L.intValue &= R.intValue;
        return L;
    }
    public static BitwiseEnum operator ^(BitwiseEnum L, BitwiseEnum R)
    {
        if (R is null) return L;
        if (L is null) return R;
        L.intValue ^= R.intValue;
        return L;
    }
    public static BitwiseEnum operator +(BitwiseEnum L, BitwiseEnum R)
    {
        if (R is null) return L;
        if (L is null) return R;
        L.intValue |= R.intValue;
        return L;
    }
    public static BitwiseEnum operator -(BitwiseEnum L, BitwiseEnum R)
    {
        if (L is null) return null;
        if (R is null) return L;
        L.intValue &= ~R.intValue;
        return L;
    }
    public static BitwiseEnum operator *(BitwiseEnum L, BitwiseEnum R)
    {
        if (R is null) return L;
        if (L is null) return R;
        L.intValue &= R.intValue;
        return L;
    }
    public static BitwiseEnum operator /(BitwiseEnum L, BitwiseEnum R)
    {
        if (R is null) return L;
        if (L is null) return R;
        L.intValue ^= R.intValue;
        return L;
    }

    public static BitwiseEnum operator +(BitwiseEnum L, int R)
    {
        L ??= new(0);
        L[R] = true;
        return L;
    }
    public static BitwiseEnum operator -(BitwiseEnum L, int R)
    {
        if (L is null) return null;
        L[R] = false;
        return L;
    }
    public static BitwiseEnum operator +(BitwiseEnum L, int[] R)
    {
        L ??= new BitwiseEnum(0);
        for (int i = 0; i < R.Length; i++) L[R[i]] = true;
        return L;
    }
    public static BitwiseEnum operator -(BitwiseEnum L, int[] R)
    {
        if (L is null) return null;
        for (int i = 0; i < R.Length; i++) L[R[i]] = false;
        return L;
    }
    public static BitwiseEnum operator +(BitwiseEnum L, List<int> R)
    {
        L ??= new BitwiseEnum(0);
        for (int i = 0; i < R.Count; i++) L[R[i]] = true;
        return L;
    }
    public static BitwiseEnum operator -(BitwiseEnum L, List<int> R)
    {
        if (L is null) return null;
        for (int i = 0; i < R.Count; i++) L[R[i]] = false;
        return L;
    }

    public static BitwiseEnum operator ~(BitwiseEnum L)
    {
        if (L is null) return null;
        L.intValue = ~L.intValue;
        return L;
    }

    public static bool operator ==(BitwiseEnum L, BitwiseEnum R) => ReferenceEquals(L, R) || (L is not null && R is not null && L.intValue == R.intValue);

    public static bool operator !=(BitwiseEnum L, BitwiseEnum R) => !(L == R);

    public override bool Equals(object obj) 
        => ReferenceEquals(this, obj) || (obj is BitwiseEnum other ? intValue == other.intValue : obj is int i && intValue == i);

    public override int GetHashCode() => intValue;

    public override string ToString() => intValue.ToString();
}