using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumExt.Test;

public static class TestHelper
{
    public static Task Verify<T>(string source, string directory) where T : IIncrementalGenerator, new()
    {
        var generator = new T();
        return Verify(source, directory, generator);
    }

    public static Task Verify(string source, string directory, params IIncrementalGenerator[] generators)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(assemblyName: "Tests", [syntaxTree]);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
        driver = driver.RunGenerators(compilation);

        var settings = new VerifySettings();
        settings.UseDirectory(directory);
        if (Environment.GetEnvironmentVariable("VERIFY_AUTOVERIFY") == "1")
        {
            settings.AutoVerify();
        }

        return Verifier.Verify(driver, settings);
    }
}