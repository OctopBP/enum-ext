using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.JsonArrayConverter;

public static class JsonArrayConverterAttribute
{
    public const string AttributeName = "JsonArrayConverter";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText =
        Utils.Attribute(AttributeName, typeof(JsonArrayConverterGenerator), AttributeTargets.Enum, allowMultiple: false,
            fields: [("JsonConverterType", "type", null), ("ConversionStrategy", "conversion", null)]);
}