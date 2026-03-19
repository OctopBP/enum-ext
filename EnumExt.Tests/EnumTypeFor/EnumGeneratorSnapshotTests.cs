using VerifyXunit;
using Xunit;

namespace EnumExt.Test.EnumTypeFor;

[UsesVerify]
public class EnumTypeForTests
{
    [Fact]
    public Task GenerateClass()
    {
        const string source =
            """
            namespace Test
            {{
                public class ClassWithInt
                {{
                    public int Value;
                }}
            }}
            
            public class ClassWithString
            {{
                public string Value;
            }}
            
            public class ClassWithT<T>
            {{
                public T Value;
            }}
            
            public class List<T>
            {{
                public T Value;
            }}
            
            public class Int {{}}
            
            [EnumTypeFor(typeof(Test.ClassWithInt))]
            [EnumTypeFor(typeof(ClassWithString), "CustomName")]
            [EnumTypeFor(typeof(ClassWithT<List<Int>>))]
            public enum ColorTest
            {
                Red,
                Blue,
                Green,
            }
            """;

        return TestHelper.Verify<EnumExt.EnumTypeFor.EnumTypeForGenerator>(source, "EnumTypeFor/Tests");
    }

    [Fact]
    public Task GenerateClass_SingleAttribute()
    {
        const string source =
            """
            public class Wrapper { public int Id; }

            [EnumTypeFor(typeof(Wrapper))]
            public enum State
            {
                Idle,
                Running,
            }
            """;

        return TestHelper.Verify<EnumExt.EnumTypeFor.EnumTypeForGenerator>(source, "EnumTypeFor/Tests");
    }

    [Fact]
    public Task Generate_EnumWithoutAttribute_ProducesOnlyAttributeSource()
    {
        const string source =
            """
            public enum Untouched
            {
                A,
                B,
            }
            """;

        return TestHelper.Verify<EnumExt.EnumTypeFor.EnumTypeForGenerator>(source, "EnumTypeFor/Tests");
    }
}