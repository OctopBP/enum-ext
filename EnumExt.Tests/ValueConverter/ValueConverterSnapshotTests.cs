using VerifyXunit;
using Xunit;

namespace EnumExt.Test.ValueConverter;

[UsesVerify]
public class ValueConverterSnapshotTests
{
    [Fact]
    public Task Generate_Name()
    {
        const string source =
            """
            [ValueConverter(ConversionStrategy.Name)]
            public enum Priority
            {
                Low,
                Normal,
                High,
            }
            """;

        return TestHelper.Verify(
            source,
            "ValueConverter/Tests",
            new EnumExt.EnumExtensions.EnumExtensionsGenerator(),
            new EnumExt.ValueConverter.ValueConverterGenerator());
    }
}
