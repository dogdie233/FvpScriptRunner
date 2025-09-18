using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using FvpScriptRunner.Runtime;

namespace FvpScriptRunner.Services;

public delegate object? SyscallDelegate(object? instance, object[] args);

public class SimpleSyscallResolver : ISyscallResolver
{
    public record struct SyscallMethod(object? Instance, MethodInfo Method, SyscallDelegate? Delegate);

    private Dictionary<string, SyscallMethod> Methods { get; } = [];

    public object? Invoke(string name, object[] args)
    {
        if (!Methods.TryGetValue(name, out var method))
            throw new NotImplementedException($"Syscall '{name}' is not implemented.");

        method.Delegate ??= CreateDelegate(method.Instance, method.Method);

        // Convert Nil to null
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == Nil.Shared || args[i] is Nil)
                args[i] = null!;
        }

        // Invoke the delegate
        var returnValue = method.Delegate(method.Instance, args);

        // Convert null back to Nil
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == null)
                args[i] = Nil.Shared;
        }

        return returnValue;
    }

    public SimpleSyscallResolver Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(T? instance) where T : class
    {
        return Register(typeof(T), instance);
    }

    public SimpleSyscallResolver Register([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, object? instance)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            if (!method.IsStatic && instance == null)
                continue;

            var attributes = method.GetCustomAttributes<SyscallImplAttribute>();

            foreach (var attribute in attributes)
                Methods.TryAdd(attribute.Name, new SyscallMethod(instance, method, null));
        }

        return this;
    }

    public SimpleSyscallResolver Register(string name, SyscallDelegate del)
    {
        if (!Methods.TryAdd(name, new SyscallMethod(null, null!, del)))
            throw new ArgumentException($"A syscall with the name '{name}' is already registered.", nameof(name));

        return this;
    }

    private static SyscallDelegate CreateDelegate(object? instance, MethodInfo method)
    {
        if (instance == null && !method.IsStatic)
            throw new ArgumentException($"Method '{method.Name}' is not static, but no instance was provided.");

        if (!RuntimeFeature.IsDynamicCodeSupported)
            return CreateDelegateReflection(method);
        else
            return CreateDelegateIL(instance, method);
    }

    private static SyscallDelegate CreateDelegateReflection(MethodInfo method)
    {
        return method.Invoke;
    }

    [RequiresDynamicCode("Calls System.Reflection.Emit.DynamicMethod.DynamicMethod(String, Type, Type[], Boolean)")]
    private static SyscallDelegate CreateDelegateIL(object? instance, MethodInfo method)
    {
        var parameters = method.GetParameters();
        var dynamicMethod = new DynamicMethod(
            $"Syscall_{method.Name}",
            typeof(object),
            [typeof(object), typeof(object[])],
            restrictedSkipVisibility: true);

        var il = dynamicMethod.GetILGenerator();

        // Load instance for non-static methods
        if (!method.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_0); // Load the first argument (object instance)
            if (instance!.GetType().IsValueType)
                il.Emit(OpCodes.Unbox_Any, instance.GetType());
            else
                il.Emit(OpCodes.Castclass, instance.GetType());
        }

        // Load method parameters
        for (var i = 0; i < parameters.Length; i++)
        {
            il.Emit(OpCodes.Ldarg_1); // Load the second argument (object[] args)
            il.Emit(OpCodes.Ldc_I4, i); // Load the index of the parameter
            il.Emit(OpCodes.Ldelem_Ref); // Load the argument at the index

            var paramType = parameters[i].ParameterType;
            if (paramType.IsByRef)
                paramType = paramType.GetElementType()!;

            if (paramType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, paramType);
            else
                il.Emit(OpCodes.Castclass, paramType);
        }

        // Call the method
        if (method.IsStatic || instance!.GetType().IsSealed)
            il.EmitCall(OpCodes.Call, method, null);
        else
            il.EmitCall(OpCodes.Callvirt, method, null);

        // Handle the return value
        if (method.ReturnType == typeof(void))
            il.Emit(OpCodes.Ldnull); // If void, load null as return value
        else if (method.ReturnType.IsValueType)
            il.Emit(OpCodes.Box, method.ReturnType); // Box value types

        il.Emit(OpCodes.Ret);

        return (SyscallDelegate)dynamicMethod.CreateDelegate(typeof(SyscallDelegate));
    }
}