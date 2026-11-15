using System.Text.Json;
using MayorElection.Services.DTOs;

namespace MayorElection.Services;

public sealed class ExportService(CandidateService candidates, UserSession session)
{
    private const int MaximumFileBytes = 10 * 1024 * 1024;
    private const int MaximumCandidates = 10_000;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    public async Task SaveJsonAsync(string path, CancellationToken cancellationToken = default)
    {
        session.RequireUser();
        var list = await candidates.ListAsync(cancellationToken: cancellationToken);
        if (list.Count > MaximumCandidates) throw new InvalidDataException("Слишком много записей для экспорта.");
        var data = JsonSerializer.SerializeToUtf8Bytes(new CandidateFile(1, list.ToArray()), JsonOptions);
        if (data.Length > MaximumFileBytes) throw new InvalidDataException("Размер списка превышает 10 МБ.");
        var destination = Path.GetFullPath(path);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, data, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, destination, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    /// <summary>Replaces the entire list atomically after validating every imported entry.</summary>
    public async Task LoadJsonAsync(string path, CancellationToken cancellationToken = default)
    {
        session.RequireUser();
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            4096, FileOptions.Asynchronous);
        if (input.Length > MaximumFileBytes) throw new InvalidDataException("Размер файла превышает 10 МБ.");
        CandidateFile file;
        try
        {
            file = await JsonSerializer.DeserializeAsync<CandidateFile>(input, JsonOptions, cancellationToken)
                ?? throw new InvalidDataException("Пустой файл списка.");
        }
        catch (JsonException exception) { throw new InvalidDataException("Некорректный JSON списка.", exception); }
        if (file.Version != 1 || file.Candidates is null || file.Candidates.Length > MaximumCandidates
            || file.Candidates.Any(x => x is null))
            throw new InvalidDataException("Неподдерживаемая версия или некорректный список.");
        await candidates.ReplaceAsync(file.Candidates, cancellationToken);
    }

    private sealed record CandidateFile(int Version, CandidateDto[] Candidates);
}
