using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.JsonConverter;

public static class JsonConverterAttribute
{
    public const string AttributeName = "JsonConverter";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText =
        Utils.Attribute(AttributeName, typeof(JsonConverterGenerator), AttributeTargets.Enum, allowMultiple: true,
            fields: [("EnumExt.JsonConverterLibrary", "type", null), ("EnumExt.ConversionStrategy", "conversion", null)]);
}