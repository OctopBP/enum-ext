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

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is EnumDeclarationSyntax;
    }
    
    private static List<EnumToProcess> GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx,
        CancellationToken token)
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
            if (arguments.Count > 1)
            {
                var argument = arguments[1];
                if (argument.NameColon is not null
                    && argument.NameColon.Name.GetNameText() == "unitySerializable"
                    && argument.Expression is LiteralExpressionSyntax { Token.Text: "false" })
                {
                    unitySerializable = false;
                }
                else if (argument.Expression is LiteralExpressionSyntax literalExpressionSyntax)
                {
                    customName = literalExpressionSyntax.Token.Text.Trim('"');
                }
            }
            
            if (arguments.Count > 2)
            {
                var argument = arguments[2];
                if (argument.NameColon is not null
                    && argument.NameColon.Name.GetNameText() == "customName"
                    && argument.Expression is LiteralExpressionSyntax literalExpressionSyntax)
                {
                    customName = literalExpressionSyntax.Token.Text;
                }
                else if (argument.Expression is LiteralExpressionSyntax { Token.Text: "false" })
                {
                    unitySerializable = false;
                }
            }

            list.Add(new EnumToProcess(enumDeclarationTypeSymbol, forTypeSymbol, generics, membersToProcess,
                enumNamespace, customName, unitySerializable));
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
    }

    private static string GenerateCode(EnumToProcess enumToProcess)
    {
        var builder = new CodeBuilder();
        
        var isVisible = enumToProcess.EnumSymbol.IsVisibleOutsideOfAssembly();
        var methodVisibility = isVisible ? "public" : "internal";
        var className = enumToProcess.ClassName;

        var typeName = enumToProcess.ForTypeSymbol.ToDisplayString();
        
        builder.Append(Utils.AutoGenerated());
        
        builder.Append(Utils.GeneratedEnumByAttributeSummary(EnumTypeForAttribute.AttributeFullName, enumToProcess.FullCsharpName));
        
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
                .Append(string.Join(", ", enumToProcess.Members.Select(member => $"{typeName} {member.Name.FirstCharToLower()}")))
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
                .Append("public System.Collections.Generic.Dictionary<").Append(enumToProcess.FullCsharpName).Append(", ").Append(typeName)
                .Append(">  Dict => new System.Collections.Generic.Dictionary<").Append(enumToProcess.FullCsharpName).Append(", ").Append(typeName)
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
}