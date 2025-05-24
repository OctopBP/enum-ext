using SourceGeneration.Utils.Common;

namespace EnumExt.EnumExtensions;

public static class ConversionStrategyEnum
{
    public static readonly string EnumText =
        Utils.Enum("ConversionStrategy", members: ["Name", "SnakeCase", "Value"]);
}