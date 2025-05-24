using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.ValueConverter;

public static class ValueConverterAttribute
{
    public const string AttributeName = "ValueConverter";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText =
        Utils.Attribute(AttributeName, null, AttributeTargets.Enum, allowMultiple: true,
            fields: [("EnumExt.ConversionStrategy", "conversion", null)]);
}