namespace FvpScriptRunner;

public class ScriptContext
{
    private Reader Reader { get; }
    private ScriptMetadata Metadata { get; }
    private CallStack Stack { get; } = new();

    public ScriptContext(Reader reader, ScriptMetadata metadata)
    {
        Reader = reader;
        Metadata = metadata;
        Reader.SeekTo(metadata.EntryPointAddress);
    }

    public void Execute()
    {
        var opCode = (OpCodeType)Reader.Read<byte>();

    }

    #region Instruction Handlers
    private void HandleNop() { }
    #endregion
}