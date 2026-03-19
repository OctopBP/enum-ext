using VerifyXunit;
using Xunit;

namespace EnumExt.Test.EnumExtensions;

[UsesVerify]
public class EnumExtensionsSnapshotTests
{
    [Fact]
    public Task Generate_PlainEnum()
    {
        const string source =
            """
            [EnumExtensions]
            public enum Status
            {
                Pending,
                Active,
                Done,
            }
            """;

        return TestHelper.Verify<EnumExt.EnumExtensions.EnumExtensionsGenerator>(source, "EnumExtensions/Tests");
    }

    [Fact]
    public Task Generate_FlagsEnum()
    {
        const string source =
            """
            [System.Flags]
            [EnumExtensions]
            public enum Permissions
            {
                None = 0,
                Read = 1,
                Write = 2,
                Execute = 4,
            }
            """;

        return TestHelper.Verify<EnumExt.EnumExtensions.EnumExtensionsGenerator>(source, "EnumExtensions/Tests");
    }
}
