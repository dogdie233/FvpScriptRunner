namespace FvpScriptRunner.Runtime;

public readonly struct Nil : IEquatable<Nil>
{
    public readonly static object Shared = new Nil();

    public override string ToString() => "nil";

    public override int GetHashCode() => 0;

    public override bool Equals(object? obj) => obj is Nil;

    public bool Equals(Nil other) => true;

    public static bool operator ==(Nil left, Nil right) => true;

    public static bool operator !=(Nil left, Nil right) => false;
}