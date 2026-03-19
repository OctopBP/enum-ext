using VerifyXunit;
using Xunit;

namespace EnumExt.Test.EnumValuesList;

[UsesVerify]
public class EnumValuesListSnapshotTests
{
    [Fact]
    public Task GenerateClass()
    {
        const string source =
            """
            [EnumValuesList]
            public enum Color
            {
                Red,
                Green,
                Blue,
            }
            """;

        return TestHelper.Verify<EnumExt.EnumValuesList.EnumValuesListGenerator>(source, "EnumValuesList/Tests");
    }
}
