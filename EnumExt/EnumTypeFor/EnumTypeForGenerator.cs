using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SourceGeneration.Utils.CodeAnalysisExtensions;
using SourceGeneration.Utils.CodeBuilder;
using SourceGeneration.Utils.Common;

namespace EnumExt.EnumTypeFor;

[Generator]
public sealed class EnumTypeForGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enums = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsSyntaxTargetForGeneration(node),
                transform: static (syntaxContext, token) => GetSemanticTargetForGeneration(syntaxContext, token))
            .SelectMany(static (array, _) => array)
            .Collect()
            .SelectMany(static (array, _) => array);

        context.RegisterPostInitializationOutput(i => i.AddSource(
            $"{EnumTypeForAttribute.AttributeFullName}.g", EnumTypeForAttribute.AttributeText));

        context.RegisterSourceOutput(enums, GenerateCode);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is EnumDeclarationSyntax;

    private static List<EnumToProcess> GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx,
        CancellationToken token
    )
    {
        var enumDeclarationSyntax = (EnumDeclarationSyntax) ctx.Node;

        var enumDeclarationSymbol = ctx.SemanticModel.GetDeclaredSymbol(enumDeclarationSyntax, token);
        if (enumDeclarationSymbol is not ITypeSymbol enumDeclarationTypeSymbol)
        {
            return [];
        }

        var enumNamespace = enumDeclarationTypeSymbol.GetNamespace();

        var membersToProcess = new List<EnumMemberToProcess>();
        foreach (var enumMemberDeclarationSyntax in enumDeclarationSyntax.Members)
        {
            membersToProcess.Add(new EnumMemberToProcess(enumMemberDeclarationSyntax.Identifier.Text));
        }

        var list = new List<EnumToProcess>();
        foreach (var attributeSyntax in enumDeclarationSyntax.AllAttributesWhere
                     (syntax => syntax.Name.AttributeIsEqualByName(EnumTypeForAttribute.AttributeName)))
        {
            if (attributeSyntax.ArgumentList is null)
            {
                continue;
            }

            var arguments = attributeSyntax.ArgumentList.Arguments;
            if (arguments.Count == 0)
            {
                continue;
            }

            if (arguments[0].Expression is not TypeOfExpressionSyntax typeOfExpressionSyntax)
            {
                continue;
            }

            var forTypeSyntax = typeOfExpressionSyntax.Type;
            var forTypeSymbol = ctx.SemanticModel.GetSymbolInfo(forTypeSyntax).Symbol;
            if (forTypeSymbol is null)
            {
                continue;
            }

            var generics = CheckAndAdd(forTypeSyntax)
                // .Select(typeSyntax => typeSyntax.ToString())
                // .Select(typeSyntax => ctx.SemanticModel.GetSymbolInfo(typeSyntax).Symbol)
                // .Where(symbol => symbol != null)
                .ToList();

            // TODO: Rework this logic
            string customName = null;
            var unitySerializable = true;
            var generateEditor = true;
            
            foreach (var argument in arguments.Skip(1))
            {
                if (argument.NameColon is not null)
                {
                    var name = argument.NameColon.Name.GetNameText();
                    if (name == "customName" && argument.Expression is LiteralExpressionSyntax customNameLiteral)
                    {
                        customName = customNameLiteral.Token.Text.Trim('"');
                    }
                    else if (name == "unitySerializable" && argument.Expression is LiteralExpressionSyntax unityLiteral)
                    {
                        unitySerializable = unityLiteral.Token.Text != "false";
                    }
                    else if (name == "generateEditor" && argument.Expression is LiteralExpressionSyntax editorLiteral)
                    {
                        generateEditor = editorLiteral.Token.Text != "false";
                    }
                }
                else if (argument.Expression is LiteralExpressionSyntax literalExpressionSyntax)
                {
                    // Positional arguments: [1] = customName, [2] = unitySerializable, [3] = generateEditor
                    var index = arguments.IndexOf(argument);
                    if (index == 1)
                    {
                        customName = literalExpressionSyntax.Token.Text.Trim('"');
                    }
                    else if (index == 2 && literalExpressionSyntax.Token.Text == "false")
                    {
                        unitySerializable = false;
                    }
                    else if (index == 3 && literalExpressionSyntax.Token.Text == "false")
                    {
                        generateEditor = false;
                    }
                }
            }

            list.Add(new EnumToProcess(enumDeclarationTypeSymbol, forTypeSymbol, generics, membersToProcess,
                enumNamespace, customName, unitySerializable, generateEditor));
        }

        return list;

        List<string> CheckAndAdd(TypeSyntax syntax)
        {
            if (syntax is not GenericNameSyntax generics)
            {
                if (syntax is QualifiedNameSyntax qualifiedName)
                {
                    return [qualifiedName.Right.ToString()];
                }

                return [syntax.ToString()];
            }

            var types = new List<string> { generics.Identifier.ToString() };
            foreach (var argumentType in generics.TypeArgumentList.Arguments)
            {
                types.AddRange(CheckAndAdd(argumentType));
            }

            return types;
        }
    }

    private static void GenerateCode(SourceProductionContext context, EnumToProcess enumToProcess)
    {
        var code = GenerateCode(enumToProcess);
        context.AddSource($"{enumToProcess.ClassName}.g", SourceText.From(code, Encoding.UTF8));

        if (enumToProcess.UnitySerializable && enumToProcess.GenerateEditor)
        {
            var drawerCode = GenerateDrawerCode(enumToProcess);
            context.AddSource($"{enumToProcess.ClassName}Drawer.g", SourceText.From(drawerCode, Encoding.UTF8));
        }
    }

    private static string GenerateCode(EnumToProcess enumToProcess)
    {
        var builder = new CodeBuilder();

        var isVisible = enumToProcess.EnumSymbol.IsVisibleOutsideOfAssembly();
        var methodVisibility = isVisible ? "public" : "internal";
        var className = enumToProcess.ClassName;

        var typeName = enumToProcess.ForTypeSymbol.ToDisplayString();

        builder.Append(Utils.AutoGenerated());

        builder.Append(Utils.GeneratedEnumByAttributeSummary(EnumTypeForAttribute.AttributeFullName,
            enumToProcess.FullCsharpName));

        if (enumToProcess.UnitySerializable)
        {
            builder.AppendLineWithIdent("[System.Serializable]");
        }

        builder.AppendIdent().Append(methodVisibility).Append(" class ").AppendLine(className);
        using (new BracketsBlock(builder))
        {
            Fields();
            PublicAccessors();
            DefaultConstructor();
            Constructor();
            Get();
            Set();
            Apply();
            AllValue();
            Dict();
        }

        return builder.ToString();

        void Fields()
        {
            foreach (var member in enumToProcess.Members)
            {
                builder.AppendIdent();
                if (enumToProcess.UnitySerializable)
                {
                    builder.Append("[UnityEngine.SerializeField] ");
                }

                builder.Append("private ").Append(typeName).Append(" ").Append(member.Name).AppendLine(";");
            }
        }

        void PublicAccessors()
        {
            builder.AppendLine();
            foreach (var member in enumToProcess.Members)
            {
                builder.AppendIdent().Append("public ").Append(typeName).Append(" Get")
                    .Append(member.Name.FirstCharToUpper())
                    .Append(" => ").Append(member.Name).AppendLine(";");
            }
        }

        void DefaultConstructor()
        {
            builder.AppendLine();
            builder.AppendIdent().Append("public ").Append(className).Append("() { }").AppendLine();
        }

        void Constructor()
        {
            builder.AppendLine();
            builder
                .AppendIdent().Append("public ").Append(className)
                .Append("(")
                .Append(string.Join(separator: ", ",
                    enumToProcess.Members.Select(member => $"{typeName} {member.Name.FirstCharToLower()}")))
                .Append(")")
                .AppendLine();

            using (new BracketsBlock(builder))
            {
                foreach (var member in enumToProcess.Members)
                {
                    builder
                        .AppendIdent().Append("this.").Append(member.Name).Append(" = ")
                        .Append(member.Name.FirstCharToLower()).Append(";")
                        .AppendLine();
                }
            }
        }

        void Get()
        {
            builder.AppendLine();
            builder
                .AppendIdent().Append("public ").Append(typeName)
                .Append(" Get(").Append(enumToProcess.FullCsharpName).AppendLine(" key)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("return key switch");
                using (new BracketsBlock(builder, withSemicolon: true))
                {
                    foreach (var member in enumToProcess.Members)
                    {
                        builder.AppendIdent().Append(enumToProcess.FullCsharpName).Append(".").Append(member.Name)
                            .Append(" => ").Append(member.Name).AppendLine(",");
                    }

                    builder.AppendLineWithIdent(
                        "_ => throw new System.ArgumentOutOfRangeException(nameof(key), key, null),");
                }
            }
        }

        void Set()
        {
            builder.AppendLine();
            builder
                .AppendIdent().Append("public void Set(").Append(enumToProcess.FullCsharpName)
                .Append(" key, ").Append(typeName).AppendLine(" value)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("switch (key)");
                using (new BracketsBlock(builder))
                {
                    foreach (var member in enumToProcess.Members)
                    {
                        builder.AppendIdent().Append("case ").Append(enumToProcess.FullCsharpName).Append(".")
                            .Append(member.Name)
                            .Append(": ").Append(member.Name).AppendLine(" = value; break;");
                    }

                    builder.AppendLineWithIdent(
                        "default: throw new System.ArgumentOutOfRangeException(nameof(key), key, null);");
                }
            }
        }

        void Apply()
        {
            builder.AppendLine();
            builder
                .AppendIdent().Append("public void Apply(").Append(enumToProcess.FullCsharpName)
                .Append(" key, System.Func<").Append(typeName).Append(", ").Append(typeName).AppendLine("> func)");

            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("switch (key)");
                using (new BracketsBlock(builder))
                {
                    foreach (var member in enumToProcess.Members)
                    {
                        builder.AppendIdent().Append("case ").Append(enumToProcess.FullCsharpName).Append(".")
                            .Append(member.Name)
                            .Append(": ").Append(member.Name).Append(" = func(").Append(member.Name)
                            .AppendLine("); break;");
                    }

                    builder.AppendLineWithIdent(
                        "default: throw new System.ArgumentOutOfRangeException(nameof(key), key, null);");
                }
            }
        }

        void AllValue()
        {
            builder.AppendLine();
            builder.AppendIdent().Append("public ").Append(typeName).Append("[] Values => new[]").AppendLine();
            using (new BracketsBlock(builder, withSemicolon: true))
            {
                foreach (var member in enumToProcess.Members)
                {
                    builder.AppendIdent().Append(member.Name).Append(",").AppendLine();
                }
            }
        }

        void Dict()
        {
            builder.AppendLine();
            builder.AppendIdent()
                .Append("public System.Collections.Generic.Dictionary<").Append(enumToProcess.FullCsharpName)
                .Append(", ").Append(typeName)
                .Append(">  Dict => new System.Collections.Generic.Dictionary<").Append(enumToProcess.FullCsharpName)
                .Append(", ").Append(typeName)
                .Append(">()").AppendLine();
            using (new BracketsBlock(builder, withSemicolon: true))
            {
                foreach (var member in enumToProcess.Members)
                {
                    builder.AppendIdent().Append("{ ").Append(enumToProcess.FullCsharpName).Append(".")
                        .Append(member.Name).Append(", ").Append(member.Name).Append(" },").AppendLine();
                }
            }
        }
    }

    private static string GenerateDrawerCode(EnumToProcess enumToProcess)
    {
        var builder = new CodeBuilder();
        var className = enumToProcess.ClassName;
        var editorNs = enumToProcess.FullNamespace;
        var namespaceName = string.IsNullOrEmpty(editorNs) ? "Editor" : $"{editorNs}.Editor";
        var typeNameShort = enumToProcess.ForTypeSymbol.Name;
        var enumName = enumToProcess.EnumSymbol.Name;

        builder.AppendLine("#if UNITY_EDITOR");
        builder.AppendLine("using System.Text;");
        builder.AppendLine("using UnityEditor;");
        builder.AppendLine("using UnityEngine;");
        builder.AppendLine();
        builder.AppendLine($"namespace {namespaceName}");
        using (new BracketsBlock(builder))
        {
            builder.AppendLineWithIdent($"[CustomPropertyDrawer(typeof({className}))]");
            builder.AppendIdent().Append("public class ").Append(className).AppendLine("Drawer : PropertyDrawer");
            using (new BracketsBlock(builder))
            {
                GenerateConstants();
                GenerateFields();
                GenerateInitializeStyles();
                GenerateLayoutHelpers();
                GenerateGetPropertyHeight();
                GenerateOnGUI();
                GenerateFormatCellIdName();
            }
        }

        builder.AppendLine("#endif");

        return builder.ToString();

        void GenerateConstants()
        {
            builder.AppendLineWithIdent("private const float RowHeight = 24f;");
            builder.AppendLineWithIdent("private const float HeaderHeight = 20f;");
            builder.AppendLineWithIdent("private const float LabelWidthRatio = 0.35f;");
            builder.AppendLineWithIdent("private const float BorderWidth = 1f;");
            builder.AppendLineWithIdent("private const float HorizontalPadding = 6f;");
            builder.AppendLineWithIdent("private const float VerticalPadding = 4f;");
        }

        void GenerateFields()
        {
            builder.AppendLine();
            builder.AppendLineWithIdent("private static readonly string[] FieldNames =");
            using (new BracketsBlock(builder, withSemicolon: true))
            {
                foreach (var member in enumToProcess.Members)
                {
                    builder.AppendLineWithIdent($"\"{member.Name}\",");
                }
            }

            builder.AppendLine();
            builder.AppendLineWithIdent("private static readonly GUIStyle _headerStyle = new GUIStyle();");
            builder.AppendLineWithIdent("private static readonly GUIStyle _columnHeaderStyle = new GUIStyle();");
            builder.AppendLineWithIdent("private static readonly GUIStyle _cellStyle = new GUIStyle();");
            builder.AppendLineWithIdent("private static Color _tableBorderColor;");
            builder.AppendLineWithIdent("private static Color _borderColor;");
            builder.AppendLineWithIdent("private static Color _stripeColor;");
            builder.AppendLineWithIdent("private static bool _stylesInitialized;");
        }

        void GenerateInitializeStyles()
        {
            builder.AppendLine();
            builder.AppendLineWithIdent("private static void InitializeStyles()");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("if (_stylesInitialized) return;");
                builder.AppendLine();
                builder.AppendLineWithIdent("_headerStyle.normal.textColor = EditorStyles.label.normal.textColor;");
                builder.AppendLineWithIdent("_headerStyle.alignment = TextAnchor.MiddleLeft;");
                builder.AppendLineWithIdent(
                    "_headerStyle.padding = new RectOffset(left: 5, right: 5, top: 2, bottom: 2);");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "_columnHeaderStyle.normal.textColor = EditorStyles.label.normal.textColor;");
                builder.AppendLineWithIdent("_columnHeaderStyle.alignment = TextAnchor.MiddleCenter;");
                builder.AppendLineWithIdent(
                    "_columnHeaderStyle.padding = new RectOffset(left: 5, right: 5, top: 2, bottom: 2);");
                builder.AppendLine();
                builder.AppendLineWithIdent("_cellStyle.normal.textColor = EditorStyles.label.normal.textColor;");
                builder.AppendLineWithIdent("_cellStyle.alignment = TextAnchor.MiddleLeft;");
                builder.AppendLineWithIdent(
                    "_cellStyle.padding = new RectOffset(left: 5, right: 5, top: 2, bottom: 2);");
                builder.AppendLine();
                builder.AppendLineWithIdent("_tableBorderColor = EditorGUIUtility.isProSkin");
                builder.IncreaseIdent();
                builder.AppendLineWithIdent("? new Color(r: 0.1f, g: 0.1f, b: 0.1f, a: 1f)");
                builder.AppendLineWithIdent(": new Color(r: 0.4f, g: 0.4f, b: 0.4f, a: 1f);");
                builder.DecreaseIdent();
                builder.AppendLine();
                builder.AppendLineWithIdent("_borderColor = EditorGUIUtility.isProSkin");
                builder.IncreaseIdent();
                builder.AppendLineWithIdent("? new Color(r: 0.15f, g: 0.15f, b: 0.15f, a: 1f)");
                builder.AppendLineWithIdent(": new Color(r: 0.5f, g: 0.5f, b: 0.5f, a: 1f);");
                builder.DecreaseIdent();
                builder.AppendLine();
                builder.AppendLineWithIdent("_stripeColor = EditorGUIUtility.isProSkin");
                builder.IncreaseIdent();
                builder.AppendLineWithIdent("? new Color(r: 1f, g: 1f, b: 1f, a: 0.05f)");
                builder.AppendLineWithIdent(": new Color(r: 0f, g: 0f, b: 0f, a: 0.05f);");
                builder.DecreaseIdent();
                builder.AppendLine();
                builder.AppendLineWithIdent("_stylesInitialized = true;");
            }
        }

        void GenerateLayoutHelpers()
        {
            builder.AppendLine();
            builder.AppendLineWithIdent("private static float EstimateDrawerContentWidth()");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent(
                    "return Mathf.Max(280f, EditorGUIUtility.currentViewWidth - 56f);");
            }

            builder.AppendLine();
            builder.AppendLineWithIdent("private static float GetValueColumnContentWidth(float tableOuterWidth)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("var innerContentWidth = tableOuterWidth - BorderWidth * 2f;");
                builder.AppendLineWithIdent("var labelColumnWidth = innerContentWidth * LabelWidthRatio;");
                builder.AppendLineWithIdent("var valueColumnWidth = innerContentWidth - labelColumnWidth;");
                builder.AppendLineWithIdent("return valueColumnWidth - BorderWidth - HorizontalPadding * 2f;");
            }

            builder.AppendLine();
            builder.AppendLineWithIdent(
                "private static float ChildFieldLabelWidth(float valueCellWidth, float inspectorDefaultLabelWidth)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent(
                    "return Mathf.Min(inspectorDefaultLabelWidth, Mathf.Max(72f, valueCellWidth * 0.42f));");
            }

            builder.AppendLine();
            builder.AppendLineWithIdent(
                "private static float GetValueCellContentHeight(SerializedProperty fieldProperty, float valueCellWidth)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("var oldLabelWidth = EditorGUIUtility.labelWidth;");
                builder.AppendLineWithIdent("try");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent(
                        "EditorGUIUtility.labelWidth = ChildFieldLabelWidth(valueCellWidth, oldLabelWidth);");
                    builder.AppendLineWithIdent("if (!fieldProperty.hasVisibleChildren)");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent(
                            "var leafOnlyHeight = EditorGUI.GetPropertyHeight(fieldProperty, GUIContent.none);");
                        builder.AppendLineWithIdent("return leafOnlyHeight + VerticalPadding * 2f;");
                    }

                    builder.AppendLine();
                    builder.AppendLineWithIdent("var iterator = fieldProperty.Copy();");
                    builder.AppendLineWithIdent("var endProperty = iterator.GetEndProperty();");
                    builder.AppendLineWithIdent("if (!iterator.NextVisible(enterChildren: true))");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent(
                            "var leafHeight = EditorGUI.GetPropertyHeight(fieldProperty, GUIContent.none);");
                        builder.AppendLineWithIdent("return leafHeight + VerticalPadding * 2f;");
                    }

                    builder.AppendLineWithIdent("var inner = 0f;");
                    builder.AppendLineWithIdent("do");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent("var fieldLabel = new GUIContent(iterator.displayName);");
                        builder.AppendLineWithIdent("inner += EditorGUI.GetPropertyHeight(iterator, fieldLabel);");
                    }

                    builder.AppendLineWithIdent(
                        "while (iterator.NextVisible(enterChildren: false) && !SerializedProperty.EqualContents(iterator, endProperty));");
                    builder.AppendLineWithIdent("return inner + VerticalPadding * 2f;");
                }

                builder.AppendLineWithIdent("finally");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent("EditorGUIUtility.labelWidth = oldLabelWidth;");
                }
            }

            builder.AppendLine();
            builder.AppendLineWithIdent(
                "private static void DrawValueCellContent(Rect valueRect, SerializedProperty fieldProperty, float valueCellWidth)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("var oldLabelWidth = EditorGUIUtility.labelWidth;");
                builder.AppendLineWithIdent("try");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent(
                        "EditorGUIUtility.labelWidth = ChildFieldLabelWidth(valueCellWidth, oldLabelWidth);");
                    builder.AppendLineWithIdent("if (!fieldProperty.hasVisibleChildren)");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent(
                            "EditorGUI.PropertyField(valueRect, fieldProperty, GUIContent.none);");
                        builder.AppendLineWithIdent("return;");
                    }

                    builder.AppendLine();
                    builder.AppendLineWithIdent("var iterator = fieldProperty.Copy();");
                    builder.AppendLineWithIdent("var endProperty = iterator.GetEndProperty();");
                    builder.AppendLineWithIdent("if (!iterator.NextVisible(enterChildren: true))");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent(
                            "EditorGUI.PropertyField(valueRect, fieldProperty, GUIContent.none);");
                        builder.AppendLineWithIdent("return;");
                    }

                    builder.AppendLineWithIdent("var y = valueRect.y;");
                    builder.AppendLineWithIdent("var w = valueRect.width;");
                    builder.AppendLineWithIdent("do");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent("var fieldLabel = new GUIContent(iterator.displayName);");
                        builder.AppendLineWithIdent("var h = EditorGUI.GetPropertyHeight(iterator, fieldLabel);");
                        builder.AppendLineWithIdent("var r = new Rect(valueRect.x, y, w, h);");
                        builder.AppendLineWithIdent("EditorGUI.PropertyField(r, iterator, fieldLabel);");
                        builder.AppendLineWithIdent("y += h;");
                    }

                    builder.AppendLineWithIdent(
                        "while (iterator.NextVisible(enterChildren: false) && !SerializedProperty.EqualContents(iterator, endProperty));");
                }

                builder.AppendLineWithIdent("finally");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent("EditorGUIUtility.labelWidth = oldLabelWidth;");
                }
            }
        }

        void GenerateGetPropertyHeight()
        {
            builder.AppendLine();
            builder.AppendLineWithIdent(
                "public override float GetPropertyHeight(SerializedProperty property, GUIContent label)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("var totalHeight = BorderWidth;");
                builder.AppendLineWithIdent("totalHeight += HeaderHeight;");
                builder.AppendLine();
                builder.AppendLineWithIdent("if (property.isExpanded)");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent("totalHeight += BorderWidth;");
                    builder.AppendLineWithIdent("totalHeight += HeaderHeight;");
                    builder.AppendLineWithIdent("totalHeight += BorderWidth;");
                    builder.AppendLine();
                    builder.AppendLineWithIdent("var estimatedTableWidth = EstimateDrawerContentWidth();");
                    builder.AppendLineWithIdent("var valueCellWidth = GetValueColumnContentWidth(estimatedTableWidth);");
                    builder.AppendLine();
                    builder.AppendLineWithIdent("for(var i = 0; i < FieldNames.Length; i++)");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent("var fieldName = FieldNames[i];");
                        builder.AppendLineWithIdent("var fieldProperty = property.FindPropertyRelative(fieldName);");
                        builder.AppendLine();
                        builder.AppendLineWithIdent("if (fieldProperty != null)");
                        using (new BracketsBlock(builder))
                        {
                            builder.AppendLineWithIdent(
                                "var valueCellContentHeight = GetValueCellContentHeight(fieldProperty, valueCellWidth);");
                            builder.AppendLineWithIdent(
                                "totalHeight += System.Math.Max(valueCellContentHeight, RowHeight);");
                        }

                        builder.AppendLineWithIdent("else");
                        using (new BracketsBlock(builder))
                        {
                            builder.AppendLineWithIdent("totalHeight += RowHeight;");
                        }

                        builder.AppendLine();
                        builder.AppendLineWithIdent("if (i < FieldNames.Length - 1)");
                        using (new BracketsBlock(builder))
                        {
                            builder.AppendLineWithIdent("totalHeight += BorderWidth;");
                        }
                    }
                }

                builder.AppendLine();
                builder.AppendLineWithIdent("totalHeight += BorderWidth;");
                builder.AppendLine();
                builder.AppendLineWithIdent("return totalHeight;");
            }
        }

        void GenerateOnGUI()
        {
            builder.AppendLine();
            builder.AppendLineWithIdent(
                "public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("InitializeStyles();");
                builder.AppendLine();
                builder.AppendLineWithIdent("EditorGUI.BeginProperty(position, label, property);");
                builder.AppendLine();
                builder.AppendLineWithIdent("var indent = EditorGUI.indentLevel;");
                builder.AppendLineWithIdent("EditorGUI.indentLevel = 0;");
                builder.AppendLine();
                builder.AppendLineWithIdent("var tableRect = position;");
                builder.AppendLineWithIdent("var tableHeight = tableRect.height;");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var backgroundRect = new Rect(tableRect.x, tableRect.y, tableRect.width, tableHeight);");
                builder.AppendLineWithIdent(
                    "EditorGUI.DrawRect(backgroundRect, new Color(r: 0.25f, g: 0.25f, b: 0.25f, a: 0.1f));");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, tableRect.width, BorderWidth), _tableBorderColor);");
                builder.AppendLineWithIdent(
                    "EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y + tableHeight - BorderWidth, tableRect.width, BorderWidth), _tableBorderColor);");
                builder.AppendLineWithIdent(
                    "EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, BorderWidth, tableHeight), _tableBorderColor);");
                builder.AppendLineWithIdent(
                    "EditorGUI.DrawRect(new Rect(tableRect.x + tableRect.width - BorderWidth, tableRect.y, BorderWidth, tableHeight), _tableBorderColor);");
                builder.AppendLine();
                builder.AppendLineWithIdent("var contentX = tableRect.x + BorderWidth;");
                builder.AppendLineWithIdent("var contentWidth = tableRect.width - BorderWidth * 2;");
                builder.AppendLineWithIdent("var currentY = tableRect.y + BorderWidth;");
                builder.AppendLineWithIdent("var labelWidth = contentWidth * LabelWidthRatio;");
                builder.AppendLineWithIdent("var valueWidth = contentWidth - labelWidth;");
                builder.AppendLineWithIdent(
                    "var valueCellWidth = valueWidth - BorderWidth - HorizontalPadding * 2;");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var firstHeaderRect = new Rect(contentX, currentY, contentWidth, HeaderHeight);");
                builder.AppendLineWithIdent("GUI.Box(firstHeaderRect, GUIContent.none, _headerStyle);");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "if (Event.current.type == EventType.MouseDown && firstHeaderRect.Contains(Event.current.mousePosition))");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent("property.isExpanded = !property.isExpanded;");
                    builder.AppendLineWithIdent("Event.current.Use();");
                }

                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var foldoutRect = new Rect(firstHeaderRect.x + 4, firstHeaderRect.y, width: 12, firstHeaderRect.height);");
                builder.AppendLineWithIdent(
                    "EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, toggleOnLabelClick: true);");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var fieldNameRect = new Rect(firstHeaderRect.x + 16, firstHeaderRect.y, contentWidth - 24, firstHeaderRect.height);");
                builder.AppendLineWithIdent("GUI.Label(fieldNameRect, label.text, _headerStyle);");
                builder.AppendLine();
                builder.AppendLineWithIdent("currentY += HeaderHeight;");
                builder.AppendLine();
                builder.AppendLineWithIdent("if (!property.isExpanded)");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent("EditorGUI.indentLevel = indent;");
                    builder.AppendLineWithIdent("EditorGUI.EndProperty();");
                    builder.AppendLineWithIdent("return;");
                }

                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var firstHeaderBorderRect = new Rect(contentX, currentY, contentWidth, BorderWidth);");
                builder.AppendLineWithIdent("EditorGUI.DrawRect(firstHeaderBorderRect, _borderColor);");
                builder.AppendLineWithIdent("currentY += BorderWidth;");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var secondHeaderRect = new Rect(contentX, currentY, contentWidth, HeaderHeight);");
                builder.AppendLineWithIdent("GUI.Box(secondHeaderRect, GUIContent.none, _headerStyle);");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var headerLabelRect = new Rect(secondHeaderRect.x, secondHeaderRect.y, labelWidth, secondHeaderRect.height);");
                builder.AppendLineWithIdent(
                    "var headerValueRect = new Rect(secondHeaderRect.x + labelWidth + BorderWidth, secondHeaderRect.y, valueWidth - BorderWidth, secondHeaderRect.height);");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    $"GUI.Label(headerLabelRect, text: FormatCellIdName(\"{enumName}\"), _columnHeaderStyle);");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var headerVerticalBorder = new Rect(secondHeaderRect.x + labelWidth, secondHeaderRect.y, BorderWidth, secondHeaderRect.height);");
                builder.AppendLineWithIdent("EditorGUI.DrawRect(headerVerticalBorder, _borderColor);");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    $"GUI.Label(headerValueRect, text: FormatCellIdName(\"{typeNameShort}\"), _columnHeaderStyle);");
                builder.AppendLine();
                builder.AppendLineWithIdent("currentY += HeaderHeight;");
                builder.AppendLine();
                builder.AppendLineWithIdent(
                    "var headerBorderRect = new Rect(contentX, currentY, contentWidth, BorderWidth);");
                builder.AppendLineWithIdent("EditorGUI.DrawRect(headerBorderRect, _borderColor);");
                builder.AppendLineWithIdent("currentY += BorderWidth;");
                builder.AppendLine();
                builder.AppendLineWithIdent("for(var i = 0; i < FieldNames.Length; i++)");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent("var fieldName = FieldNames[i];");
                    builder.AppendLineWithIdent("var fieldProperty = property.FindPropertyRelative(fieldName);");
                    builder.AppendLine();
                    builder.AppendLineWithIdent("if (fieldProperty == null)");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent(
                            $"UnityEngine.Debug.LogWarning($\"Field '{{fieldName}}' not found in {className}\");");
                        builder.AppendLineWithIdent("continue;");
                    }

                    builder.AppendLine();
                    builder.AppendLineWithIdent(
                        "var valueCellContentHeight = GetValueCellContentHeight(fieldProperty, valueCellWidth);");
                    builder.AppendLineWithIdent(
                        "var rowHeight = System.Math.Max(valueCellContentHeight, RowHeight);");
                    builder.AppendLine();
                    builder.AppendLineWithIdent("var rowRect = new Rect(contentX, currentY, contentWidth, rowHeight);");
                    builder.AppendLine();
                    builder.AppendLineWithIdent("if (i % 2 == 1)");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent("EditorGUI.DrawRect(rowRect, _stripeColor);");
                    }

                    builder.AppendLine();
                    builder.AppendLineWithIdent(
                        "var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);");
                    builder.AppendLineWithIdent(
                        "EditorGUI.LabelField(labelRect, FormatCellIdName(fieldName), _cellStyle);");
                    builder.AppendLine();
                    builder.AppendLineWithIdent(
                        "var verticalBorderRect = new Rect(rowRect.x + labelWidth, rowRect.y, BorderWidth, rowRect.height);");
                    builder.AppendLineWithIdent("EditorGUI.DrawRect(verticalBorderRect, _borderColor);");
                    builder.AppendLine();
                    builder.AppendLineWithIdent("var valueRect = new Rect(");
                    builder.IncreaseIdent();
                    builder.AppendLineWithIdent("rowRect.x + labelWidth + BorderWidth + HorizontalPadding,");
                    builder.AppendLineWithIdent("rowRect.y + VerticalPadding,");
                    builder.AppendLineWithIdent("valueCellWidth,");
                    builder.AppendLineWithIdent("rowRect.height - VerticalPadding * 2);");
                    builder.DecreaseIdent();
                    builder.AppendLineWithIdent(
                        "DrawValueCellContent(valueRect, fieldProperty, valueCellWidth);");
                    builder.AppendLine();
                    builder.AppendLineWithIdent("currentY += rowHeight;");
                    builder.AppendLine();
                    builder.AppendLineWithIdent("if (i < FieldNames.Length - 1)");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent(
                            "var horizontalBorderRect = new Rect(contentX, currentY, contentWidth, BorderWidth);");
                        builder.AppendLineWithIdent("EditorGUI.DrawRect(horizontalBorderRect, _borderColor);");
                        builder.AppendLineWithIdent("currentY += BorderWidth;");
                    }
                }

                builder.AppendLine();
                builder.AppendLineWithIdent("EditorGUI.indentLevel = indent;");
                builder.AppendLineWithIdent("EditorGUI.EndProperty();");
            }
        }

        void GenerateFormatCellIdName()
        {
            builder.AppendLine();
            builder.AppendLineWithIdent("private static string FormatCellIdName(string fieldName)");
            using (new BracketsBlock(builder))
            {
                builder.AppendLineWithIdent("var result = new StringBuilder();");
                builder.AppendLineWithIdent("for(var i = 0; i < fieldName.Length; i++)");
                using (new BracketsBlock(builder))
                {
                    builder.AppendLineWithIdent(
                        "if (i > 0 && char.IsUpper(fieldName[i]) && !char.IsUpper(fieldName[i - 1]))");
                    using (new BracketsBlock(builder))
                    {
                        builder.AppendLineWithIdent("result.Append(' ');");
                    }

                    builder.AppendLineWithIdent("result.Append(fieldName[i]);");
                }

                builder.AppendLineWithIdent("return result.ToString();");
            }
        }
    }
}