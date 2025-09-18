namespace FvpScriptRunner.Services;

public interface ISyscallResolver
{
    object? Invoke(string name, object[] args);
}