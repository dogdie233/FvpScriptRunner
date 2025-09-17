using System.Numerics;
using FvpScriptRunner.Primitive;

namespace FvpScriptRunner;

public class ScriptContext
{
    private Reader Reader { get; }
    private ScriptMetadata Metadata { get; }
    private CallStack Stack { get; } = new();
    private ISyscallInvoker SyscallInvoker { get; }

    private object? ReturnValue { get; set; } = null;
    private object?[] GlobalVars { get; }

    private uint PC
    {
        get => (uint)Reader.Position;
        set => Reader.SeekTo(value);
    }

    public ScriptContext(Reader reader, ScriptMetadata metadata, ISyscallInvoker syscallInvoker)
    {
        Reader = reader;
        Metadata = metadata;
        SyscallInvoker = syscallInvoker;
        GlobalVars = new object?[metadata.GlobalCount];

        PC = metadata.EntryPointAddress;
    }

    public void Execute()
    {
        var opCode = (OpCodeType)Reader.Read<byte>();
        try
        {
            switch (opCode)
            {
                case OpCodeType.Nop: HandleNop(); break;
                case OpCodeType.InitStack: HandleInitStack(); break;
                case OpCodeType.Call: HandleCall(); break;
                case OpCodeType.Syscall: HandleSyscall(); break;
                case OpCodeType.Ret: HandleRet(); break;
                case OpCodeType.RetV: HandleRetV(); break;
                case OpCodeType.Jmp: HandleJmp(); break;
                case OpCodeType.Jz: HandleJz(); break;
                case OpCodeType.PushNil: HandlePushNil(); break;
                case OpCodeType.PushTrue: HandlePushTrue(); break;
                case OpCodeType.PushI32: HandlePushI32(); break;
                case OpCodeType.PushI16: HandlePushI16(); break;
                case OpCodeType.PushI8: HandlePushI8(); break;
                case OpCodeType.PushF32: HandlePushF32(); break;
                case OpCodeType.PushString: HandlePushString(); break;
                case OpCodeType.PushGlobal: HandlePushGlobal(); break;
                case OpCodeType.PushLocal: HandlePushLocal(); break;
                case OpCodeType.PushGlobalTable: HandlePushGlobalTable(); break;
                case OpCodeType.PushLocalTable: HandlePushLocalTable(); break;
                case OpCodeType.PushTop: HandlePushTop(); break;
                case OpCodeType.PushReturn: HandlePushReturn(); break;
                case OpCodeType.PopGlobal: HandlePopGlobal(); break;
                case OpCodeType.PopLocal: HandlePopLocal(); break;
                case OpCodeType.PopGlobalTable: HandlePopGlobalTable(); break;
                case OpCodeType.PopLocalTable: HandlePopLocalTable(); break;
                case OpCodeType.Neg: HandleNeg(); break;
                case OpCodeType.Add: HandleAdd(); break;
                case OpCodeType.Sub: HandleSub(); break;
                case OpCodeType.Mul: HandleMul(); break;
                case OpCodeType.Div: HandleDiv(); break;
                case OpCodeType.Mod: HandleMod(); break;
                case OpCodeType.BitTest: HandleBitTest(); break;
                case OpCodeType.And: HandleAnd(); break;
                case OpCodeType.Or: HandleOr(); break;
                case OpCodeType.SetEq: HandleSetEq(); break;
                case OpCodeType.SetNe: HandleSetNe(); break;
                case OpCodeType.SetGt: HandleSetGt(); break;
                case OpCodeType.SetLe: HandleSetLe(); break;
                case OpCodeType.SetLt: HandleSetLt(); break;
                case OpCodeType.SetGe: HandleSetGe(); break;
                default:
                    throw CreateScriptRuntimeException($"Unimplemented opcode {opCode}.");
            }
        }
        catch (Exception ex) when (ex is not ScriptRuntimeException)
        {
            throw new ScriptRuntimeException(PC, "Runtime error occurred.", ex);
        }
    }

    #region Instruction Handlers
    private void HandleNop() { }

    private void HandleInitStack()
    {
        var argCount = Reader.Read<byte>();
        var localCount = Reader.Read<byte>();

        if (PC != Metadata.EntryPointAddress + 3)
            throw CreateScriptRuntimeException("InitStack must be the first instruction executed.");

        Stack.PushCall(PC, argCount, localCount);
    }

    private void HandleCall()
    {
        var addr = Reader.Read<uint>();
        var pcSave = PC;
        PC = addr;
        var opCode = (OpCodeType)Reader.Read<byte>();
        if (opCode != OpCodeType.InitStack)
        {
            PC -= 1; // Rewind to before the opcode read
            throw CreateScriptRuntimeException("Called function must start with InitStack.");
        }

        var argCount = Reader.Read<byte>();
        var localCount = Reader.Read<byte>();
        Stack.PushCall(pcSave, argCount, localCount);
    }

    private void HandleSyscall()
    {
        var id = Reader.Read<ushort>();
        if (id < 0 || id >= Metadata.Syscalls.Length)
            throw CreateScriptRuntimeException($"Invalid syscall ID {id}.");

        var syscall = Metadata.Syscalls[id];
        var args = new object[syscall.ArgumentCount];

        for (var i = 0; i < syscall.ArgumentCount; i++)
            args[syscall.ArgumentCount - i - 1] = Stack.Pop();

        ReturnValue = SyscallInvoker.Invoke(args);
    }

    private void HandleRet()
    {
        var call = Stack.PopCall();

        ReturnValue = null;
        PC = call;
    }

    private void HandleRetV()
    {
        var retValue = Stack.Pop();
        var call = Stack.PopCall();

        ReturnValue = retValue;
        PC = call;
    }

    private void HandleJmp()
    {
        var addr = Reader.Read<uint>();
        PC = addr;
    }

    private void HandleJz()
    {
        var addr = Reader.Read<uint>();
        var cond = Stack.Pop();

        var isTrue = cond switch
        {
            Nil => false,
            _ => Convert.ToBoolean(cond),
        };
        if (!isTrue)
            PC = addr;
    }

    private void HandlePushNil()
    {
        Stack.Push(Nil.Shared);
    }

    private void HandlePushTrue()
    {
        Stack.Push(true);
    }

    private void HandlePushI32()
    {
        var imm = Reader.Read<int>();
        Stack.Push(imm);
    }

    private void HandlePushI16()
    {
        var imm = Reader.Read<short>();
        Stack.Push(imm);
    }

    private void HandlePushI8()
    {
        var imm = Reader.Read<sbyte>();
        Stack.Push(imm);
    }

    private void HandlePushF32()
    {
        var imm = Reader.Read<float>();
        Stack.Push(imm);
    }

    private void HandlePushString()
    {
        var imm = Reader.ReadString();
        Stack.Push(imm);
    }

    private void HandlePushGlobal()
    {
        var id = Reader.Read<ushort>();
        var val = GlobalVars[id] ?? throw CreateScriptRuntimeException($"Global variable {id} is not initialized.");
        Stack.Push(val);
    }

    private void HandlePushLocal()
    {
        var id = Reader.Read<byte>();
        var val = Stack.GetLocal(id);
        Stack.Push(val);
    }

    private void HandlePushGlobalTable()
    {
        var id = Reader.Read<ushort>();
        var key = Stack.Pop();

        if (id < 0 || id >= GlobalVars.Length)
            throw CreateScriptRuntimeException($"Invalid global variable ID {id}.");

        if (key is not int)
            throw CreateScriptRuntimeException("Table key must be an integer.");

        if (GlobalVars[id] is not Dictionary<int, object> table)
            throw CreateScriptRuntimeException($"Global variable {id} is not a table.");
        
        if (table.TryGetValue((int)key, out var val))
            Stack.Push(val);
        else
            Stack.Push(Nil.Shared);
    }

    private void HandlePushLocalTable()
    {
        var id = Reader.Read<byte>();
        var key = Stack.Pop();

        if (key is not int)
            throw CreateScriptRuntimeException("Table key must be an integer.");

        var local = Stack.GetLocal(id);
        if (local is not Dictionary<int, object> table)
            throw CreateScriptRuntimeException($"Local variable {id} is not a table.");

        if (table.TryGetValue((int)key, out var val))
            Stack.Push(val);
        else
            Stack.Push(Nil.Shared);
    }

    private void HandlePushTop()
    {
        var val = Stack.Peek();
        Stack.Push(val);
    }

    private void HandlePushReturn()
    {
        if (ReturnValue is null)
            throw CreateScriptRuntimeException("No return value available.");
        Stack.Push(ReturnValue);
        ReturnValue = null;
    }

    private void HandlePopGlobal()
    {
        var id = Reader.Read<ushort>();
        var val = Stack.Pop();
        GlobalVars[id] = val;
    }

    private void HandlePopLocal()
    {
        var id = Reader.Read<byte>();
        var val = Stack.Pop();
        Stack.SetLocal(id, val);
    }

    private void HandlePopGlobalTable()
    {
        var id = Reader.Read<ushort>();
        var val = Stack.Pop();
        var key = Stack.Pop();

        if (id < 0 || id >= GlobalVars.Length)
            throw CreateScriptRuntimeException($"Invalid global variable ID {id}.");

        if (key is not int)
            throw CreateScriptRuntimeException("Table key must be an integer.");

        if (GlobalVars[id] is not Dictionary<int, object> table)
        {
            table = [];
            GlobalVars[id] = table;
        }

        table[(int)key] = val;
    }

    private void HandlePopLocalTable()
    {
        var id = Reader.Read<byte>();
        var val = Stack.Pop();
        var key = Stack.Pop();

        if (key is not int)
            throw CreateScriptRuntimeException("Table key must be an integer.");

        var local = Stack.GetLocal(id);
        if (local is not Dictionary<int, object> table)
        {
            table = [];
            Stack.SetLocal(id, table);
        }

        table[(int)key] = val;
    }

    private void HandleNeg()
    {
        var val = Stack.Pop();
        if (val is int i)
            Stack.Push(-i);
        else if (val is float f)
            Stack.Push(-f);
        else
            throw CreateScriptRuntimeException("Negation is only supported for integers and floats.");
    }

    private void HandleAdd()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        if (a is int ai && b is int bi)
            Stack.Push(bi + ai);
        else if (a is float af && b is float bf)
            Stack.Push(bf + af);
        else if (a is int ai2 && b is float bf2)
            Stack.Push(bf2 + ai2);
        else if (a is float af2 && b is int bi2)
            Stack.Push(bi2 + af2);
        else if (a is string sa && b is string sb)
            Stack.Push(sa + sb);
        else
            throw CreateScriptRuntimeException("Addition is only supported for integers, floats, and strings.");
    }

    private void HandleSub()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        if (a is int ai && b is int bi)
            Stack.Push(bi - ai);
        else if (a is float af && b is float bf)
            Stack.Push(bf - af);
        else if (a is int ai2 && b is float bf2)
            Stack.Push(bf2 - ai2);
        else if (a is float af2 && b is int bi2)
            Stack.Push(bi2 - af2);
        else
            throw CreateScriptRuntimeException("Subtraction is only supported for integers and floats.");
    }

    private void HandleMul()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        if (a is int ai && b is int bi)
            Stack.Push(bi * ai);
        else if (a is float af && b is float bf)
            Stack.Push(bf * af);
        else if (a is int ai2 && b is float bf2)
            Stack.Push(bf2 * ai2);
        else if (a is float af2 && b is int bi2)
            Stack.Push(bi2 * af2);
        else
            throw CreateScriptRuntimeException("Multiplication is only supported for integers and floats.");
    }

    private void HandleDiv()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        if (a is int ai && b is int bi)
        {
            if (ai == 0)
                throw CreateScriptRuntimeException("Division by zero.");
            Stack.Push(bi / ai);
        }
        else if (a is float af && b is float bf)
        {
            if (af == 0)
                throw CreateScriptRuntimeException("Division by zero.");
            Stack.Push(bf / af);
        }
        else if (a is int ai2 && b is float bf2)
        {
            if (ai2 == 0)
                throw CreateScriptRuntimeException("Division by zero.");
            Stack.Push(bf2 / ai2);
        }
        else if (a is float af2 && b is int bi2)
        {
            if (af2 == 0)
                throw CreateScriptRuntimeException("Division by zero.");
            Stack.Push(bi2 / af2);
        }
        else
            throw CreateScriptRuntimeException("Division is only supported for integers and floats.");
    }

    private void HandleMod()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        if (a is int ai && b is int bi)
        {
            if (ai == 0)
                throw CreateScriptRuntimeException("Modulo by zero.");
            Stack.Push(bi % ai);
        }
        else
            throw CreateScriptRuntimeException("Modulo is only supported for integers.");
    }

    private void HandleBitTest()
    {
        var bit = Stack.Pop();
        var val = Stack.Pop();

        if (bit is not int bi)
            throw CreateScriptRuntimeException("Bit index must be an integer.");
        if (val is not int vi)
            throw CreateScriptRuntimeException("Value must be an integer.");

        if (bi < 0 || bi >= sizeof(int) * 8)
            throw CreateScriptRuntimeException("Bit index out of range.");

        var result = (vi & (1 << bi)) != 0;
        Stack.Push(result);
    }

    private void HandleAnd()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        Stack.Push(a == b && a is not Nil);
    }

    private void HandleOr()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        Stack.Push(a is not Nil || b is not Nil);
    }

    private void HandleSetEq()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        Stack.Push(a == b);
    }

    private void HandleSetNe()
    {
        var a = Stack.Pop();
        var b = Stack.Pop();

        Stack.Push(a != b);
    }

    private void HandleSetGt()
    {
        var b = Stack.Pop();
        var a = Stack.Pop();

        Stack.Push(Comparer<object>.Default.Compare(a, b) > 0);
    }

    private void HandleSetLe()
    {
        var b = Stack.Pop();
        var a = Stack.Pop();

        Stack.Push(Comparer<object>.Default.Compare(a, b) <= 0);
    }

    private void HandleSetLt()
    {
        var b = Stack.Pop();
        var a = Stack.Pop();

        Stack.Push(Comparer<object>.Default.Compare(a, b) < 0);
    }

    private void HandleSetGe()
    {
        var b = Stack.Pop();
        var a = Stack.Pop();

        Stack.Push(Comparer<object>.Default.Compare(a, b) >= 0);
    }

    #endregion

    private ScriptRuntimeException CreateScriptRuntimeException(string message)
        => new(PC, message);
}