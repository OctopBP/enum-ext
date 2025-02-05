using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.ValueArrayConverter;

public static class ValueArrayConverterAttribute
{
    public const string AttributeName = "ValueArrayConverter";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText =
        Utils.Attribute(AttributeName, null, AttributeTargets.Enum, allowMultiple: true,
            fields: [("ConversionStrategy", "conversion", null)]);
}