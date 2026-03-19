using VerifyXunit;
using Xunit;

namespace EnumExt.Test.ValueArrayConverter;

[UsesVerify]
public class ValueArrayConverterSnapshotTests
{
    [Fact]
    public Task Generate_SnakeCase()
    {
        const string source =
            """
            [ValueArrayConverter(ConversionStrategy.SnakeCase)]
            public enum Tag
            {
                Feature,
                Bugfix,
                Hotfix,
            }
            """;

        return TestHelper.Verify(
            source,
            "ValueArrayConverter/Tests",
            new EnumExt.EnumExtensions.EnumExtensionsGenerator(),
            new EnumExt.ValueArrayConverter.ValueArrayConverterGenerator());
    }
}
