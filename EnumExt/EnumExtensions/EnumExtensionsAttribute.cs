using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.EnumExtensions;

public static class EnumExtensionsAttribute
{
    public const string AttributeName = "EnumExtensions";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText = Utils.SimpleAttribute(AttributeName, AttributeTargets.Enum);
}