using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.ValueArrayConverter;

public static class ValueArrayConverterAttribute
{
    public const string AttributeName = "ValueArrayConverter";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText = Utils.SimpleAttribute(AttributeName, AttributeTargets.Enum);
}