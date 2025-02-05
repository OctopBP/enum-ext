#nullable enable
using Microsoft.CodeAnalysis;

namespace EnumExt.ValueArrayConverter;

internal sealed record EnumToProcess(ITypeSymbol EnumSymbol, string? FullNamespace, string ConversionStrategy)
{
    public string FullCsharpName { get; } = EnumSymbol.ToDisplayString();
    public string DocumentationId { get; } = DocumentationCommentId.CreateDeclarationId(EnumSymbol);
        
    public ITypeSymbol EnumSymbol { get; } = EnumSymbol;
    public string? FullNamespace { get; } = FullNamespace;
    public string ConversionStrategy { get; } = ConversionStrategy;
}