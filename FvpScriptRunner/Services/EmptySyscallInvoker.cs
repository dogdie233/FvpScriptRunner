using FvpScriptRunner.Runtime;

namespace FvpScriptRunner.Services;

public class EmptySyscallInvoker : ISyscallResolver
{
    public object? Invoke(string name, object[] args)
    {
        return Nil.Shared;
    }
}