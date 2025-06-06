using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.JsonArrayConverter;

public static class JsonArrayConverterAttribute
{
    public const string AttributeName = "JsonArrayConverter";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText =
        Utils.Attribute(AttributeName, typeof(JsonArrayConverterGenerator), AttributeTargets.Enum, allowMultiple: true,
            fields: [("EnumExt.JsonConverterLibrary", "type", null), ("EnumExt.ConversionStrategy", "conversion", null)]);
}