using System;
using System.Reflection;

namespace TibiaHuntAnalyzer.Desktop;

public static class ApplicationMetadata
{
    public const string CreatorName = "Aislan Michel";
    public const string GitHubProfileUrl = "https://github.com/aislan-michel";

    public static string VersionText
    {
        get
        {
            var assembly = typeof(ApplicationMetadata).Assembly;
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var trimmed = informationalVersion.Split('+', StringSplitOptions.TrimEntries)[0];
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    return trimmed;
                }
            }

            var version = assembly.GetName().Version;
            return version is null
                ? "1.0.0"
                : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
