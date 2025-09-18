using FvpScriptRunner.Core;

namespace FvpScriptRunner.Core;

public record class Instruction(uint Address, OpCodeType? OpCode, object? Operand);