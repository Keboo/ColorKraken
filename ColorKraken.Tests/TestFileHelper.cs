using System.IO;

namespace ColorKraken.Tests;

public static class TestFileHelper
{
    public static string CreateTestFile()
    {
        string filePath = Path.GetTempFileName();

        File.WriteAllText(filePath,
            @"
{
    ""meta"": {
      ""name"": ""Kevin Dark"",
      ""scheme"": ""dark"",
      ""color-kraken-basedon"": ""Foo""
    },
    ""themeValues"": {
      ""root"": {
        ""red"": ""#FF0000"",
        ""orange"": ""#DE9B43"",
        ""yellow"": ""#1CEC55"",
        ""green"": ""#D3AF5D"",
        ""blue"": ""#4D88FF""
      }
    }
}");

        return filePath;
    }
}
