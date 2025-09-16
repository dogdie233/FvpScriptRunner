namespace FvpScriptRunner;

public class ScriptMetadata
{
    public record struct Syscall(string Name, byte ArgumentCount);

    public required uint MetadataOffset { get; init; }
    public required uint EntryPointAddress { get; init; }
    public required ushort GlobalCount { get; init; }
    public required ushort VolatileGlobalCount { get; init; }
    public required ushort ResolutionMode { get; init; }
    public required string GameTitle { get; init; }
    public required Syscall[] Syscalls { get; init; }
}
