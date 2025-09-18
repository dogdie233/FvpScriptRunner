namespace FvpScriptRunner.Exceptions;

public class ScriptRuntimeException(uint pc, string message, Exception? innerException) : Exception(message, innerException)
{
    public ScriptRuntimeException(uint pc, string message) : this(pc, message, null) { }

    public uint PC { get; } = pc;
}