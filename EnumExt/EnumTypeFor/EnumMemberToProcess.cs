namespace EnumExt.EnumTypeFor;

internal sealed record EnumMemberToProcess(string Name)
{
    public string Name { get; } = Name;
}