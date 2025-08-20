using Microsoft.AspNetCore.Mvc.TagHelpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spotters.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string DefaultFileName = "config.json";

    public async Task<AppConfig> LoadAsync()
    {
        var userPath = GetUserConfigPath();
        if (!File.Exists(userPath))
        {
            // First run: copy defaults from root.
            var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);
            AppConfig defaults = File.Exists(defaultPath)
                ? await DeserializeAsync(defaultPath)
                : new AppConfig();

            // Ensure folder exists and write
            Directory.CreateDirectory(Path.GetDirectoryName(userPath)!);
            await SerializeAsync(userPath, defaults);
            return defaults;
        }

        var result = await DeserializeAsync(userPath);

        if (result.Users.SelectMany(it => it.Characters).Any())
            return result;

        // if no characters saved, read characters from wwwroot/images
        var charactersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images");
        var spottersFolders = Directory.GetDirectories(charactersPath, "*", SearchOption.TopDirectoryOnly);

        foreach (var spotterFolder in spottersFolders)
        {
            var user = result.Users.FirstOrDefault(u => u.UserName.Equals(Path.GetFileName(spotterFolder), StringComparison.OrdinalIgnoreCase));

            if (user != null)
            {
                var characterFolders = Directory.GetDirectories(spotterFolder, "*", SearchOption.TopDirectoryOnly);

                foreach (var characterFolder in characterFolders)
                {
                    user.Characters.Add(new Character
                    {
                        Name = Path.GetFileName(characterFolder),
                        Active = characterFolder == characterFolders.First(),
                        Visible = false
                    });
                }
            }
        }
        return result;
    }

    public async Task SaveAsync(AppConfig config)
    {
        var userPath = GetUserConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(userPath)!);
        await SerializeAsync(userPath, config);
    }

    private static string GetUserConfigPath()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var folder = Path.Combine(docs, "Spotters");
        return Path.Combine(folder, DefaultFileName);
    }

    private static async Task<AppConfig> DeserializeAsync(string path)
    {
        await using var fs = File.OpenRead(path);
        var cfg = await JsonSerializer.DeserializeAsync<AppConfig>(fs, _jsonOptions);
        return cfg ?? new AppConfig();
    }

    private static async Task SerializeAsync(string path, AppConfig cfg, CancellationToken ct = default)
    {
        const int maxAttempts = 10;
        const int delayMs = 100;

        IOException? lastIo = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var fs = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);

                await JsonSerializer.SerializeAsync(fs, cfg, _jsonOptions, ct);
                return;
            }
            catch (IOException ex) when (attempt < maxAttempts)
            {
                lastIo = ex;
                await Task.Delay(delayMs, ct);
            }
        }

        throw new IOException($"Can't write to {path} with {maxAttempts} attemps every {delayMs}ms", lastIo);
    }
}
