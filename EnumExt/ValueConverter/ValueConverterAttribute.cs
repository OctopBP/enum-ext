using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.ValueConverter;

public static class ValueConverterAttribute
{
    public const string AttributeName = "ValueConverter";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText = Utils.SimpleAttribute(AttributeName, AttributeTargets.Enum);
}