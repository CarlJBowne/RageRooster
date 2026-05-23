using System;
using System.Collections.Generic;

[System.Serializable]
public class BitwiseEnum : IEquatable<BitwiseEnum>
{
    /// <summary>
    /// Backing integer value representing the bitmask.
    /// </summary>
    public int intValue;

    /// <summary>
    /// Create a BitwiseEnum with specific integer bitmask.
    /// </summary>
    /// <param name="intValue">Integer bitmask.</param>
    public BitwiseEnum(int intValue) => this.intValue = intValue;

    /// <summary>
    /// Create a BitwiseEnum from an array of booleans. Each true value sets the corresponding bit.
    /// </summary>
    /// <param name="inputs">Boolean array where index i sets bit i if true.</param>
    public BitwiseEnum(params bool[] inputs)
    {
        intValue = 0;
        if (inputs == null) return;
        int maxBits = sizeof(int) * 8;
        int len = Math.Min(inputs.Length, maxBits);
        for (int i = 0; i < len; i++)
            if (inputs[i]) intValue |= 1 << i;
    }

    /// <summary>
    /// Creates a new instance of the current runtime type with the specified integer value.
    /// Derived classes can override this to ensure operators produce instances of the derived type.
    /// Default implementation returns a new <see cref="BitwiseEnum"/>.
    /// </summary>
    /// <param name="value">Integer bitmask for the created instance.</param>
    /// <returns>New instance of the runtime type carrying <paramref name="value"/>.</returns>
    protected virtual BitwiseEnum CreateFromValue(int value) => new(value);

    /// <summary>
    /// Returns a copy/clone of this instance (same runtime type).
    /// </summary>
    public BitwiseEnum Clone() => CreateFromValue(intValue);

    /// <summary>
    /// Clones the data value from source to target.
    /// </summary>
    /// <param name="source">The Source <see cref="BitwiseEnum"/> to clone data from.</param>
    /// <param name="target">The Target <see cref="BitwiseEnum"/> to clone data into.</param>
    /// <returns>The target</returns>
    public static BitwiseEnum Clone(BitwiseEnum source, BitwiseEnum target)
    {
        if (target == null) target = new();
        target.intValue = source.intValue;
        return target;
    }

    /// <summary>
    /// Indexer to get or set an individual bit.
    /// Valid indices are 0..31. Negative or out-of-range indices throw <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="i">Bit index (0-based).</param>
    public bool this[int i]
    {
        get
        {
            if (i < 0 || i >= sizeof(int) * 8) throw new ArgumentOutOfRangeException(nameof(i));
            return (intValue & (1 << i)) != 0;
        }
        set
        {
            if (i < 0 || i >= sizeof(int) * 8) throw new ArgumentOutOfRangeException(nameof(i));
            if (value) intValue |= 1 << i;
            else intValue &= ~(1 << i);
        }
    }

    /// <summary>
    /// Explicit conversion to <see cref="int"/> returning the underlying bitmask.
    /// If <paramref name="value"/> is null, 0 is returned.
    /// </summary>
    public static explicit operator int(BitwiseEnum value) => value?.intValue ?? 0;

    /// <summary>
    /// Explicit conversion from <see cref="int"/> to <see cref="BitwiseEnum"/>.
    /// </summary>
    public static explicit operator BitwiseEnum(int value) => new(value);

    /// <summary>
    /// Explicit conversion from <see cref="bool[]"/> to <see cref="BitwiseEnum"/>.
    /// </summary>
    public static explicit operator BitwiseEnum(bool[] inputs) => new(inputs);

    // Helper: get int values with null treated as 0
    private static int IntOrZero(BitwiseEnum e) => e?.intValue ?? 0;

    // Helper: choose factory: prefer left operand's runtime type, otherwise right operand's, otherwise base type
    private static BitwiseEnum CreateResult(BitwiseEnum left, BitwiseEnum right, int result)
    {
        if (left != null) return left.CreateFromValue(result);
        if (right != null) return right.CreateFromValue(result);
        return new BitwiseEnum(result);
    }

    /// <summary>
    /// Bitwise OR operator. Returns a new instance with bits set where either operand has bits set.
    /// Treats null as an empty (zero) mask. Result instance type follows the left operand's runtime type if present, otherwise right, otherwise base type.
    /// </summary>
    public static BitwiseEnum operator |(BitwiseEnum L, BitwiseEnum R)
    {
        int res = IntOrZero(L) | IntOrZero(R);
        return CreateResult(L, R, res);
    }

    /// <summary>
    /// Bitwise AND operator. Returns a new instance with bits set only where both operands have bits set.
    /// Treats null as an empty (zero) mask.
    /// </summary>
    public static BitwiseEnum operator &(BitwiseEnum L, BitwiseEnum R)
    {
        int res = IntOrZero(L) & IntOrZero(R);
        return CreateResult(L, R, res);
    }

    /// <summary>
    /// Bitwise XOR operator. Returns a new instance with bits set where operands differ.
    /// Treats null as an empty (zero) mask.
    /// </summary>
    public static BitwiseEnum operator ^(BitwiseEnum L, BitwiseEnum R)
    {
        int res = IntOrZero(L) ^ IntOrZero(R);
        return CreateResult(L, R, res);
    }

    /// <summary>
    /// Alias for bitwise OR. Returns a new instance. Same semantics as <c>|</c>, effectively adds the Enums together.
    /// </summary>
    public static BitwiseEnum operator +(BitwiseEnum L, BitwiseEnum R) => L | R;

    /// <summary>
    /// Subtract bits: returns a new instance with bits from R cleared from L (L & ~R).
    /// If L is null, treat as zero and return a new zero-valued instance (of R's runtime type if provided).
    /// </summary>
    public static BitwiseEnum operator -(BitwiseEnum L, BitwiseEnum R)
    {
        int res = IntOrZero(L) & ~IntOrZero(R);
        return CreateResult(L, R, res);
    }

    /// <summary>
    /// Multiply defined as AND (alias). Returns a new instance.
    /// </summary>
    public static BitwiseEnum operator *(BitwiseEnum L, BitwiseEnum R) => L & R;

    /// <summary>
    /// Divide defined as XOR (alias). Returns a new instance.
    /// </summary>
    public static BitwiseEnum operator /(BitwiseEnum L, BitwiseEnum R) => L ^ R;

    /// <summary>
    /// Add a single bit index to the bitmask. Returns a new instance with bit R set.
    /// Index must be in 0..31.
    /// </summary>
    public static BitwiseEnum operator +(BitwiseEnum L, int R)
    {
        if (R < 0 || R >= sizeof(int) * 8) throw new ArgumentOutOfRangeException(nameof(R));
        int baseVal = IntOrZero(L);
        int res = baseVal | (1 << R);
        return CreateResult(L, null, res);
    }

    /// <summary>
    /// Remove a single bit index from the bitmask. Returns a new instance with bit R cleared.
    /// Index must be in 0..31.
    /// </summary>
    public static BitwiseEnum operator -(BitwiseEnum L, int R)
    {
        if (R < 0 || R >= sizeof(int) * 8) throw new ArgumentOutOfRangeException(nameof(R));
        int baseVal = IntOrZero(L);
        int res = baseVal & ~(1 << R);
        return CreateResult(L, null, res);
    }

    /// <summary>
    /// Add multiple indices (array) to the bitmask. Returns a new instance with those bits set.
    /// Handles null array by returning a clone of L (or zero instance).
    /// </summary>
    public static BitwiseEnum operator +(BitwiseEnum L, int[] R)
    {
        int baseVal = IntOrZero(L);
        if (R != null)
        {
            int maxBits = sizeof(int) * 8;
            for (int i = 0; i < R.Length; i++)
            {
                int idx = R[i];
                if (idx < 0 || idx >= maxBits) throw new ArgumentOutOfRangeException(nameof(R), "Bit index out of range.");
                baseVal |= 1 << idx;
            }
        }
        return CreateResult(L, null, baseVal);
    }

    /// <summary>
    /// Remove multiple indices (array) from the bitmask. Returns a new instance with those bits cleared.
    /// </summary>
    public static BitwiseEnum operator -(BitwiseEnum L, int[] R)
    {
        int baseVal = IntOrZero(L);
        if (R != null)
        {
            int maxBits = sizeof(int) * 8;
            for (int i = 0; i < R.Length; i++)
            {
                int idx = R[i];
                if (idx < 0 || idx >= maxBits) throw new ArgumentOutOfRangeException(nameof(R), "Bit index out of range.");
                baseVal &= ~(1 << idx);
            }
        }
        return CreateResult(L, null, baseVal);
    }

    /// <summary>
    /// Add multiple indices (list) to the bitmask. Returns a new instance with those bits set.
    /// </summary>
    public static BitwiseEnum operator +(BitwiseEnum L, List<int> R)
    {
        int baseVal = IntOrZero(L);
        if (R != null)
        {
            int maxBits = sizeof(int) * 8;
            for (int i = 0; i < R.Count; i++)
            {
                int idx = R[i];
                if (idx < 0 || idx >= maxBits) throw new ArgumentOutOfRangeException(nameof(R), "Bit index out of range.");
                baseVal |= 1 << idx;
            }
        }
        return CreateResult(L, null, baseVal);
    }

    /// <summary>
    /// Remove multiple indices (list) from the bitmask. Returns a new instance with those bits cleared.
    /// </summary>
    public static BitwiseEnum operator -(BitwiseEnum L, List<int> R)
    {
        int baseVal = IntOrZero(L);
        if (R != null)
        {
            int maxBits = sizeof(int) * 8;
            for (int i = 0; i < R.Count; i++)
            {
                int idx = R[i];
                if (idx < 0 || idx >= maxBits) throw new ArgumentOutOfRangeException(nameof(R), "Bit index out of range.");
                baseVal &= ~(1 << idx);
            }
        }
        return CreateResult(L, null, baseVal);
    }

    /// <summary>
    /// Bitwise NOT (invert) operator. Returns a new instance with all bits inverted.
    /// </summary>
    public static BitwiseEnum operator ~(BitwiseEnum L)
    {
        if (L == null) return new BitwiseEnum(~0);
        return L.CreateFromValue(~L.intValue);
    }

    /// <summary>
    /// Equality operator. True if both are the same reference or both non-null with equal integer masks.
    /// </summary>
    public static bool operator ==(BitwiseEnum L, BitwiseEnum R)
    {
        if (ReferenceEquals(L, R)) return true;
        if (L is null || R is null) return false;
        return L.intValue == R.intValue;
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(BitwiseEnum L, BitwiseEnum R) => !(L == R);

    /// <summary>
    /// Determines whether the specified object is equal to the current BitwiseEnum.
    /// Accepts BitwiseEnum or int for comparison.
    /// </summary>
    public override bool Equals(object obj)
        => ReferenceEquals(this, obj)
           || (obj is BitwiseEnum other && intValue == other.intValue)
           || (obj is int i && intValue == i);

    /// <summary>
    /// IEquatable implementation.
    /// </summary>
    public bool Equals(BitwiseEnum other) => this == other;

    /// <summary>
    /// Hash code based on the integer mask.
    /// </summary>
    public override int GetHashCode() => intValue;

    /// <summary>
    /// String representation of the integer mask.
    /// </summary>
    public override string ToString() => intValue.ToString();

    // Convenience methods that inline operator logic for performance-sensitive callers.

    /// <summary>
    /// Performs bitwise OR with <paramref name="Other"/> and returns a new instance.
    /// Inline of operator | to avoid extra dispatch.
    /// </summary>
    public BitwiseEnum Or(BitwiseEnum Other) => this | Other;

    /// <summary>
    /// Performs bitwise AND with <paramref name="Other"/> and returns a new instance.
    /// </summary>
    public BitwiseEnum And(BitwiseEnum Other) => this & Other;

    /// <summary>
    /// Performs bitwise XOR with <paramref name="Other"/> and returns a new instance.
    /// </summary>
    public BitwiseEnum Xor(BitwiseEnum Other) => this ^ Other;

    /// <summary>
    /// Combine as XOR (alias).
    /// </summary>
    public BitwiseEnum Combine(BitwiseEnum Other) => this ^ Other;

    /// <summary>
    /// Add another BitwiseEnum as OR (alias).
    /// </summary>
    public BitwiseEnum Add(BitwiseEnum Other) => this | Other;

    /// <summary>
    /// Subtract bits present in <paramref name="Other"/> (alias).
    /// </summary>
    public BitwiseEnum Subtract(BitwiseEnum Other) => this - Other;

    /// <summary>
    /// Multiply as AND (alias).
    /// </summary>
    public BitwiseEnum Multiply(BitwiseEnum Other) => this & Other;

    /// <summary>
    /// Divide as XOR (alias).
    /// </summary>
    public BitwiseEnum Divide(BitwiseEnum Other) => this ^ Other;

    /// <summary>
    /// Add single bit index. Inline of operator +(BitwiseEnum, int).
    /// </summary>
    public BitwiseEnum Add(int Other) => this + Other;

    /// <summary>
    /// Remove single bit index. Inline of operator -(BitwiseEnum, int).
    /// </summary>
    public BitwiseEnum Subtract(int Other) => this - Other;

    /// <summary>
    /// Add array of bit indices. Inline.
    /// </summary>
    public BitwiseEnum Add(int[] Other) => this + Other;

    /// <summary>
    /// Remove array of bit indices. Inline.
    /// </summary>
    public BitwiseEnum Subtract(int[] Other) => this - Other;

    /// <summary>
    /// Add list of bit indices. Inline.
    /// </summary>
    public BitwiseEnum Add(List<int> Other) => this + Other;

    /// <summary>
    /// Remove list of bit indices. Inline.
    /// </summary>
    public BitwiseEnum Subtract(List<int> Other) => this - Other;

    /// <summary>
    /// Returns a new instance with all bits inverted.
    /// </summary>
    public BitwiseEnum Inverted() => ~this;

    /// <summary>
    /// True if this instance contains the specified bit index.
    /// </summary>
    public bool Contains(int Other)
    {
        if (Other < 0 || Other >= sizeof(int) * 8) throw new ArgumentOutOfRangeException(nameof(Other));
        return (intValue & (1 << Other)) != 0;
    }

    /// <summary>
    /// Returns true if any bit in <paramref name="Other"/> is also set in this instance.
    /// Treats null as empty set.
    /// </summary>
    public bool ContainsAnyFrom(BitwiseEnum Other)
    {
        if (Other == null) return false;
        return (intValue & Other.intValue) != 0;
    }

    /// <summary>
    /// Returns true if all bits set in <paramref name="Other"/> are also set in this instance.
    /// If <paramref name="Other"/> is null or zero, returns true.
    /// </summary>
    public bool ContainsAllOf(BitwiseEnum Other)
    {
        if (Other == null) return true;
        return (intValue & Other.intValue) == Other.intValue;
    }
}