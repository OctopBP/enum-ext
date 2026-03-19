using VerifyXunit;
using Xunit;

namespace EnumExt.Test.ModelBinder;

[UsesVerify]
public class ModelBinderSnapshotTests
{
    [Fact]
    public Task GenerateClass()
    {
        const string source =
            """
            [ModelBinder]
            public enum SortOrder
            {
                Asc,
                Desc,
            }
            """;

        return TestHelper.Verify<EnumExt.ModelBinder.ModelBinderGenerator>(source, "ModelBinder/Tests");
    }
}
