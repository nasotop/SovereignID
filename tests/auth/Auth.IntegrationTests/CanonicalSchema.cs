using System.Reflection;

namespace Auth.IntegrationTests;

internal static class CanonicalSchema
{
    private const string RelativePath = "database/BBDD_SovereignID.sql";

    public static string Read()
    {
        var directory = new DirectoryInfo(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, RelativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate '{RelativePath}' walking up from the test output directory.");
    }
}
