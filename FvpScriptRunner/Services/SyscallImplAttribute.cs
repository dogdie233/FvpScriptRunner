namespace FvpScriptRunner.Services;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class SyscallImplAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}