namespace FvpScriptRunner;

public interface ISyscallInvoker
{
    object? Invoke(object[] args);
}