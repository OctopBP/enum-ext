using VerifyXunit;
using Xunit;

namespace EnumExt.Test.JsonArrayConverter;

[UsesVerify]
public class JsonArrayConverterSnapshotTests
{
    [Fact]
    public Task Generate_SystemTextJson_Name()
    {
        const string source =
            """
            [JsonArrayConverter(JsonConverterLibrary.SystemTextJson, ConversionStrategy.Name)]
            public enum Role
            {
                User,
                Admin,
                Guest,
            }
            """;

        return TestHelper.Verify(
            source,
            "JsonArrayConverter/Tests",
            new EnumExt.EnumExtensions.EnumExtensionsGenerator(),
            new EnumExt.JsonArrayConverter.JsonArrayConverterGenerator());
    }
}
