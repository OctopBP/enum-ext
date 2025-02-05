namespace EnumExt.EnumExtensions;

internal sealed record EnumMemberToProcess(string Name, int Value)
{
    public string Name { get; } = Name;
    public int Value { get; } = Value;
}