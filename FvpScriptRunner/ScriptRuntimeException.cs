namespace FvpScriptRunner;

public class ScriptRuntimeException(uint pc, string message, Exception? innerException) : Exception(message, innerException)
{
    public ScriptRuntimeException(uint pc, string message) : this(pc, message, null) { }

    public uint PC { get; } = pc;
}

public class StackBreakException(string message) : Exception(message)
{
}