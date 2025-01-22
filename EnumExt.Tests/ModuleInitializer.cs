using System.Runtime.CompilerServices;
using VerifyTests;

namespace EnumExt.Test;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Enable();
    }
}