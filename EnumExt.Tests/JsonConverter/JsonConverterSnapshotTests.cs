using VerifyXunit;
using Xunit;

namespace EnumExt.Test.JsonConverter;

[UsesVerify]
public class JsonConverterSnapshotTests
{
    [Fact]
    public Task Generate_SystemTextJson_Name()
    {
        const string source =
            """
            [JsonConverter(JsonConverterLibrary.SystemTextJson, ConversionStrategy.Name)]
            public enum Status
            {
                Pending,
                Active,
                Done,
            }
            """;

        return TestHelper.Verify(
            source,
            "JsonConverter/Tests",
            new EnumExt.EnumExtensions.EnumExtensionsGenerator(),
            new EnumExt.JsonConverter.JsonConverterGenerator());
    }
}
