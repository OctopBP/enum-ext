#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SourceGeneration.Utils.Common;

namespace EnumExt.EnumTypeFor;

internal sealed record EnumToProcess(
    ITypeSymbol EnumSymbol,
    ISymbol ForTypeSymbol,
    List<string> Generics,
    List<EnumMemberToProcess> Members,
    string? FullNamespace,
    string? CustomName,
    bool UnitySerializable)
{
    public string FullCsharpName { get; } = EnumSymbol.ToDisplayString();
    public string DocumentationId { get; } = DocumentationCommentId.CreateDeclarationId(EnumSymbol);
    public string? CustomName { get; } = CustomName;
    public bool UnitySerializable { get; } = UnitySerializable;

    public ITypeSymbol EnumSymbol { get; } = EnumSymbol;
    public ISymbol ForTypeSymbol { get; } = ForTypeSymbol;
    public List<string> Generics { get; } = Generics;
    public List<EnumMemberToProcess> Members { get; } = Members;
    public string? FullNamespace { get; } = FullNamespace;

    public string ClassName { get; } = CustomName ??
                                       $"{string.Join("_", Generics.Select(type => type.FirstCharToUpper()))}For{EnumSymbol.Name}";
}