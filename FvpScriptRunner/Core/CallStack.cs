using System.Runtime.InteropServices;
using FvpScriptRunner.Exceptions;
using FvpScriptRunner.Runtime;

namespace FvpScriptRunner.Core;

public class CallStack
{
    private List<object> Stack { get; } = [];
    private StackFrameSave CurrentFrame { get; set; } = default;
    private int FrameSize { get; set; } = 0;

    public void Push(object value)
    {
        Stack.Add(value);
        FrameSize++;
    }

    public object Pop()
    {
        if (FrameSize == 0)
            throw new StackBreakException("Call stack is empty.");

        var value = Stack[^1];
        Stack.RemoveAt(Stack.Count - 1);
        FrameSize--;
        return value;
    }

    public void SetLocal(int index, object value)
    {
        if (index >= CurrentFrame.LocalCount || index < 0)
            throw new StackBreakException($"Invalid local index. {index}");
        
        Stack[CurrentFrame.FrameBase + index] = value;
    }

    public object GetLocal(int index)
    {
        if (index >= CurrentFrame.LocalCount || index < ~(int)CurrentFrame.ArgCount)
            throw new StackBreakException($"Invalid local index. {index}");

        return Stack[CurrentFrame.FrameBase + index];
    }

    public void PushCall(uint returnAddress, byte argCount, byte localCount)
    {
        Stack.Add(CurrentFrame);
        CurrentFrame = new StackFrameSave(returnAddress, argCount, localCount, Stack.Count);
        CollectionsMarshal.SetCount(Stack, Stack.Count + localCount);  // Ensure local space
        FrameSize = 0;
    }

    public uint PopCall()
    {
        if (FrameSize != 0)
            throw new StackBreakException("Cannot return from function with non-empty stack.");
        
        var prevFrame = (StackFrameSave)Stack[^(CurrentFrame.LocalCount + 1)];
        CollectionsMarshal.SetCount(Stack, Stack.Count - (CurrentFrame.LocalCount + 1 + CurrentFrame.ArgCount));
        var returnAddress = CurrentFrame.ReturnAddress;
        CurrentFrame = prevFrame;
        FrameSize = Stack.Count - CurrentFrame.FrameBase - CurrentFrame.LocalCount;

        return returnAddress;
    }

    public object Peek()
    {
        if (Stack.Count == 0)
            throw new StackBreakException("Call stack is empty.");

        return Stack[^1];
    }
}