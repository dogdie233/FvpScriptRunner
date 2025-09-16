namespace FvpScriptRunner;

public record class Instruction(uint Address, OpCodeType? OpCode, object? Operand);