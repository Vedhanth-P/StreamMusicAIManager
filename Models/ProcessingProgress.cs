namespace StreamMusicAIManager.Models;

public sealed class ProcessingProgress
{
    public int ProcessedItems { get; init; }
    public int TotalItems { get; init; }
    public string CurrentFile { get; init; } = string.Empty;
    public double Percentage { get; init; }
}