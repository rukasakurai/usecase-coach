using System.Text.Json;

namespace UsecaseCoach.Mcp;

/// <summary>Loads reference use cases from the data/reference-usecases directory.</summary>
public static class ReferenceUsecaseStore
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<ReferenceUsecase> LoadAll()
    {
        var dir = ResolveDirectory();
        var usecases = new List<ReferenceUsecase>();

        foreach (var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f))
        {
            if (Path.GetFileName(file).Equals("schema.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var json = File.ReadAllText(file);
            var usecase = JsonSerializer.Deserialize<ReferenceUsecase>(json, Options);

            if (usecase is null
                || string.IsNullOrWhiteSpace(usecase.Problem)
                || string.IsNullOrWhiteSpace(usecase.Approach)
                || string.IsNullOrWhiteSpace(usecase.Impact))
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(file)} is missing required problem/approach/impact fields.");
            }

            usecases.Add(usecase);
        }

        return usecases;
    }

    private static string ResolveDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("REFERENCE_USECASES_DIR");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "data", "reference-usecases");
                if (Directory.Exists(candidate))
                    return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate data/reference-usecases. Set REFERENCE_USECASES_DIR.");
    }
}
