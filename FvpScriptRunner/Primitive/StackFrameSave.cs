namespace FvpScriptRunner.Primitive;

public record struct StackFrameSave(uint ReturnAddress, byte ArgCount, byte LocalCount, int FrameBase);