using System;
using SourceGeneration.Utils.Common;

namespace EnumExt.ModelBinder;

public static class ModelBinderAttribute
{
    public const string AttributeName = "ModelBinder";
    public static readonly string AttributeFullName = AttributeName.WithAttributePostfix();
    public static readonly string AttributeText = Utils.SimpleAttribute(AttributeName, AttributeTargets.Enum);
}