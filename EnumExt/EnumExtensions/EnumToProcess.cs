#nullable enable
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace EnumExt.EnumExtensions;

internal sealed record EnumToProcess(
    ITypeSymbol EnumSymbol,
    List<EnumMemberToProcess> Members,
    string? FullNamespace,
    bool HasLanguageExt)
{
    public string FullCsharpName { get; } = EnumSymbol.ToDisplayString();
    public string DocumentationId { get; } = DocumentationCommentId.CreateDeclarationId(EnumSymbol);

    public ITypeSymbol EnumSymbol { get; } = EnumSymbol;
    public List<EnumMemberToProcess> Members { get; } = Members;
    public string? FullNamespace { get; } = FullNamespace;
    public bool HasLanguageExt { get; } = HasLanguageExt;
}