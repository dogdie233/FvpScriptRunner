using FvpScriptRunner.Operands;

namespace FvpScriptRunner;

public static class HcbParser
{
    public static ScriptMetadata ParseMetadata(Reader reader)
    {
        reader.SeekTo(0);
        var offset = reader.Read<uint>();
        reader.SeekTo(offset);

        var entryPoint = reader.Read<uint>();
        var globalCount = reader.Read<ushort>();
        var volatileGlobalCount = reader.Read<ushort>();
        var resolutionMode = reader.Read<ushort>();
        var gameTitle = reader.ReadString();

        var syscallCount = reader.Read<byte>();
        var syscalls = new ScriptMetadata.Syscall[syscallCount];
        for (var i = 0; i < syscallCount; i++)
        {
            var argCount = reader.Read<byte>();
            var name = reader.ReadString();
            syscalls[i] = new ScriptMetadata.Syscall(name, argCount);
        }

        return new ScriptMetadata
        {
            MetadataOffset = offset,
            EntryPointAddress = entryPoint,
            GlobalCount = globalCount,
            VolatileGlobalCount = volatileGlobalCount,
            ResolutionMode = resolutionMode,
            GameTitle = gameTitle,
            Syscalls = syscalls
        };
    }

    public static List<Instruction> ParseCodeArea(Reader reader, ScriptMetadata metadata)
    {
        reader.SeekTo(4);
        var instructions = new List<Instruction>();
        while (reader.Position < metadata.MetadataOffset)
        {
            var address = (uint)reader.Position;
            var opCode = (OpCodeType)reader.Read<byte>();
            object? operand = opCode switch
            {
                OpCodeType.InitStack => new InitStackOperand(reader.Read<byte>(), reader.Read<byte>()),
                OpCodeType.Call => reader.Read<uint>(),
                OpCodeType.Syscall => reader.Read<ushort>(),
                OpCodeType.Jmp => reader.Read<uint>(),
                OpCodeType.Jz => reader.Read<uint>(),
                OpCodeType.PushI32 => reader.Read<int>(),
                OpCodeType.PushI16 => reader.Read<short>(),
                OpCodeType.PushI8 => reader.Read<sbyte>(),
                OpCodeType.PushF32 => reader.Read<float>(),
                OpCodeType.PushString => reader.ReadString(),
                OpCodeType.PushGlobal => reader.Read<ushort>(),
                OpCodeType.PushLocal => reader.Read<sbyte>(),
                OpCodeType.PushGlobalTable => reader.Read<ushort>(),
                OpCodeType.PushLocalTable => reader.Read<sbyte>(),
                OpCodeType.PopGlobal => reader.Read<ushort>(),
                OpCodeType.PopLocal => reader.Read<sbyte>(),
                OpCodeType.PopGlobalTable => reader.Read<ushort>(),
                OpCodeType.PopLocalTable => reader.Read<sbyte>(),
                _ => null
            };
            instructions.Add(new Instruction(address, opCode, operand));
        }
        return instructions;
    }
}