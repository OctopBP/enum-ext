﻿//HintName: ColorTestForClassWithInt.g.cs
/// <auto-generated />

[System.Serializable]
public class ColorTestForClassWithInt
{
    [UnityEngine.SerializeField] private Test.ClassWithInt Red;
    [UnityEngine.SerializeField] private Test.ClassWithInt Blue;
    [UnityEngine.SerializeField] private Test.ClassWithInt Green;

    public ColorTestForClassWithInt() { }

    public ColorTestForClassWithInt(Test.ClassWithInt red, Test.ClassWithInt blue, Test.ClassWithInt green)
    {
        this.Red = red;
        this.Blue = blue;
        this.Green = green;
    }

    public Test.ClassWithInt Get(ColorTest key)
    {
        return key switch
        {
            ColorTest.Red => Red,
            ColorTest.Blue => Blue,
            ColorTest.Green => Green,
            _ => throw new System.ArgumentOutOfRangeException(nameof(key), key, null),
        };
    }

    public void Set(ColorTest key, Test.ClassWithInt value)
    {
        switch (key)
        {
            case ColorTest.Red: Red = value; break;
            case ColorTest.Blue: Blue = value; break;
            case ColorTest.Green: Green = value; break;
            default: throw new System.ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }

    public void Apply(ColorTest key, System.Func<Test.ClassWithInt, Test.ClassWithInt> func)
    {
        switch (key)
        {
            case ColorTest.Red: Red = func(Red); break;
            case ColorTest.Blue: Blue = func(Blue); break;
            case ColorTest.Green: Green = func(Green); break;
            default: throw new System.ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }

    public Test.ClassWithInt[] Values => new[]
    {
        Red,
        Blue,
        Green,
    };
}
