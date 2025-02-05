#nullable enable
using Microsoft.CodeAnalysis;

namespace EnumExt.JsonArrayConverter;

internal sealed record EnumToProcess(ITypeSymbol EnumSymbol, string? FullNamespace, string JsonType, string ConversionStrategy)
{
    public string FullCsharpName { get; } = EnumSymbol.ToDisplayString();
    public string DocumentationId { get; } = DocumentationCommentId.CreateDeclarationId(EnumSymbol);
        
    public ITypeSymbol EnumSymbol { get; } = EnumSymbol;
    public string? FullNamespace { get; } = FullNamespace;
    public string JsonType { get; } = JsonType;
    public string ConversionStrategy { get; } = ConversionStrategy;
}