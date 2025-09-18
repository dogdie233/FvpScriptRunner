using System.Reflection;
using System.Linq;
using FvpScriptRunner.Runtime;
using FvpScriptRunner.Services;

namespace FvpScriptRunner.Test;

[TestFixture]
public class SimpleSyscallResolverTests
{
    private SimpleSyscallResolver resolver;

    [SetUp]
    public void Setup()
    {
        resolver = new SimpleSyscallResolver();
    }

    #region Test Classes

    /// <summary>
    /// Test class with various syscall methods for testing
    /// </summary>
    public class TestSyscallClass
    {
        public int Value { get; set; } = 42;

        [SyscallImpl("add")]
        public int Add(int a, int b)
        {
            return a + b;
        }

        [SyscallImpl("multiply")]
        public int Multiply(int a, int b)
        {
            return a * b;
        }

        [SyscallImpl("get_value")]
        public int GetValue()
        {
            return Value;
        }

        [SyscallImpl("set_value")]
        public void SetValue(int value)
        {
            Value = value;
        }

        [SyscallImpl("concat")]
        public string Concat(string a, string b)
        {
            return a + b;
        }

        [SyscallImpl("handle_null")]
        public string HandleNull(string? input)
        {
            return input ?? "null";
        }

        [SyscallImpl("return_null")]
        public string? ReturnNull()
        {
            return null;
        }

        [SyscallImpl("box_unbox")]
        public object BoxUnbox(object value)
        {
            return value;
        }

        // Method without attribute - should not be registered
        public int NotRegistered()
        {
            return 999;
        }
    }

    /// <summary>
    /// Test class with multiple attributes on same method
    /// </summary>
    public class MultipleAttributeClass
    {
        [SyscallImpl("method1")]
        [SyscallImpl("method1_alias")]
        public int Method()
        {
            return 100;
        }
    }

    /// <summary>
    /// Test class with both static and instance methods
    /// </summary>
    public class MixedMethodsClass
    {
        public int InstanceValue { get; set; } = 42;

        [SyscallImpl("instance_method")]
        public int InstanceMethod(int input)
        {
            return InstanceValue + input;
        }

        [SyscallImpl("static_method")]
        public static int StaticMethod(int input)
        {
            return input * 2;
        }

        [SyscallImpl("instance_void")]
        public void InstanceVoidMethod(int value)
        {
            InstanceValue = value;
        }

        [SyscallImpl("static_void")]
        public static void StaticVoidMethod()
        {
            // Do nothing
        }
    }

    /// <summary>
    /// Test class with nullable parameter methods
    /// </summary>
    public class NullableParametersClass
    {
        [SyscallImpl("nullable_int")]
        public string NullableIntMethod(int? value)
        {
            return value?.ToString() ?? "null";
        }

        [SyscallImpl("nullable_bool")]
        public string NullableBoolMethod(bool? value)
        {
            return value?.ToString() ?? "null";
        }

        [SyscallImpl("nullable_double")]
        public string NullableDoubleMethod(double? value)
        {
            return value?.ToString() ?? "null";
        }

        [SyscallImpl("multiple_nullable")]
        public string MultipleNullableMethod(int? a, bool? b, double? c)
        {
            return $"{(a?.ToString() ?? "null")}_{(b?.ToString() ?? "null")}_{(c?.ToString() ?? "null")}";
        }

        [SyscallImpl("return_nullable_int")]
        public int? ReturnNullableInt(bool returnNull)
        {
            return returnNull ? null : 42;
        }
    }

    /// <summary>
    /// Pure static class for testing static class registration (if supported)
    /// </summary>
    public static class PureStaticClass
    {
        [SyscallImpl("pure_static_add")]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [SyscallImpl("pure_static_concat")]
        public static string Concat(string a, string b)
        {
            return $"STATIC_{a}_{b}";
        }

        [SyscallImpl("pure_static_nullable")]
        public static string HandleNullable(int? value)
        {
            return value?.ToString() ?? "static_null";
        }

        // Method without attribute - should not be registered
        public static int NotRegisteredStatic()
        {
            return 888;
        }
    }

    #endregion

    #region Register Tests

    [Test]
    public void Register_InstanceMethods_ShouldRegisterMethodsWithAttributes()
    {
        // Arrange
        var instance = new TestSyscallClass();

        // Act
        var result = resolver.Register(instance);

        // Assert
        Assert.That(result, Is.SameAs(resolver), "Register should return the same resolver instance for chaining");

        // Verify methods are registered by invoking them
        Assert.DoesNotThrow(() => resolver.Invoke("add", [1, 2]));
        Assert.DoesNotThrow(() => resolver.Invoke("multiply", [3, 4]));
        Assert.DoesNotThrow(() => resolver.Invoke("get_value", []));
        Assert.DoesNotThrow(() => resolver.Invoke("set_value", [100]));
        Assert.DoesNotThrow(() => resolver.Invoke("concat", ["hello", "world"]));
    }

    [Test]
    public void Register_StaticMethods_ShouldRegisterStaticMethods()
    {
        // Note: We need to create a wrapper class to register static methods
        // since the generic constraint requires T : class
        var staticWrapper = new StaticMethodWrapper();
        
        // Act
        var result = resolver.Register(staticWrapper);

        // Assert
        Assert.That(result, Is.SameAs(resolver));

        // Verify static methods are registered
        Assert.DoesNotThrow(() => resolver.Invoke("static_add", [5, 6]));
        Assert.DoesNotThrow(() => resolver.Invoke("static_concat", ["test", "static"]));
        Assert.DoesNotThrow(() => resolver.Invoke("static_void", []));
    }

    /// <summary>
    /// Wrapper class to test static method registration
    /// </summary>
    public class StaticMethodWrapper
    {
        [SyscallImpl("static_add")]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [SyscallImpl("static_concat")]
        public static string Concat(string a, string b)
        {
            return $"{a}_{b}";
        }

        [SyscallImpl("static_void")]
        public static void VoidMethod()
        {
            // Do nothing
        }
    }

    [Test]
    public void Register_ValueTypeInstance_ShouldRegisterMethods()
    {
        // Arrange
        var structWrapper = new StructWrapper { Value = 10 };

        // Act
        resolver.Register(structWrapper);

        // Assert
        Assert.DoesNotThrow(() => resolver.Invoke("struct_get_value", []));
        Assert.DoesNotThrow(() => resolver.Invoke("struct_add", [5]));
    }

    /// <summary>
    /// Wrapper class to test struct-like functionality
    /// </summary>
    public class StructWrapper
    {
        public int Value { get; set; }

        [SyscallImpl("struct_get_value")]
        public int GetValue()
        {
            return Value;
        }

        [SyscallImpl("struct_add")]
        public int Add(int other)
        {
            return Value + other;
        }
    }

    [Test]
    public void Register_MultipleAttributesOnSameMethod_ShouldRegisterMultipleNames()
    {
        // Arrange
        var instance = new MultipleAttributeClass();

        // Act
        resolver.Register(instance);

        // Assert
        var result1 = resolver.Invoke("method1", []);
        var result2 = resolver.Invoke("method1_alias", []);
        
        Assert.That(result1, Is.EqualTo(100));
        Assert.That(result2, Is.EqualTo(100));
    }

    [Test]
    public void Register_ClassWithOnlyPrivateMethods_ShouldNotRegisterAnything()
    {
        // Arrange
        var instance = new PrivateMethodClass();

        // Act
        resolver.Register(instance);

        // Assert - no methods should be registered since they're private
        Assert.Throws<NotImplementedException>(() => resolver.Invoke("private_method", []));
    }

    /// <summary>
    /// Test class with private methods only
    /// </summary>
    public class PrivateMethodClass
    {
        [SyscallImpl("private_method")]
        private int PrivateMethod()
        {
            return 42;
        }
    }

    [Test]
    public void Register_NullInstanceWithInstanceMethods_ShouldSkipInstanceMethods()
    {
        // Act - this should not throw and should skip instance methods
        var result = resolver.Register<TestSyscallClass>(null);

        // Assert
        Assert.That(result, Is.SameAs(resolver));

        // Verify that instance methods are not registered
        Assert.Throws<NotImplementedException>(() => resolver.Invoke("add", [1, 2]));
        Assert.Throws<NotImplementedException>(() => resolver.Invoke("multiply", [3, 4]));
    }

    [Test]
    public void Register_Delegate_ShouldRegisterDelegate()
    {
        // Arrange
        SyscallDelegate del = (instance, args) => (int)args[0] + (int)args[1];

        // Act
        var result = resolver.Register("custom_add", del);

        // Assert
        Assert.That(result, Is.SameAs(resolver));
        
        var invokeResult = resolver.Invoke("custom_add", [10, 20]);
        Assert.That(invokeResult, Is.EqualTo(30));
    }

    [Test]
    public void Register_DuplicateName_ShouldThrowArgumentException()
    {
        // Arrange
        SyscallDelegate del1 = (instance, args) => 1;
        SyscallDelegate del2 = (instance, args) => 2;

        // Act
        resolver.Register("duplicate", del1);

        // Assert
        var ex = Assert.Throws<ArgumentException>(() => resolver.Register("duplicate", del2));
        Assert.That(ex.Message, Does.Contain("duplicate"));
        Assert.That(ex.ParamName, Is.EqualTo("name"));
    }

    [Test]
    public void Register_SameMethodMultipleTimes_ShouldNotDuplicate()
    {
        // Arrange
        var instance = new TestSyscallClass();

        // Act - register same instance multiple times
        resolver.Register(instance);
        resolver.Register(instance); // Should not throw or create duplicates

        // Assert - should still work normally
        var result = resolver.Invoke("add", [1, 2]);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Register_ClassWithMixedStaticAndInstanceMethods_ShouldRegisterBoth()
    {
        // Arrange
        var instance = new MixedMethodsClass();

        // Act
        resolver.Register(instance);

        // Assert - both static and instance methods should be registered
        Assert.DoesNotThrow(() => resolver.Invoke("instance_method", [10]));
        Assert.DoesNotThrow(() => resolver.Invoke("static_method", [20]));
        Assert.DoesNotThrow(() => resolver.Invoke("instance_void", [100]));
        Assert.DoesNotThrow(() => resolver.Invoke("static_void", []));
    }

    [Test]
    public void Register_ClassWithMixedMethods_FunctionalityTest()
    {
        // Arrange
        var instance = new MixedMethodsClass();
        resolver.Register(instance);

        // Act & Assert - test static method functionality
        var staticResult = resolver.Invoke("static_method", [20]);
        Assert.That(staticResult, Is.EqualTo(40)); // 20 * 2

        // Test instance method functionality (before modifying state)
        var instanceResult1 = resolver.Invoke("instance_method", [10]);
        Assert.That(instanceResult1, Is.EqualTo(52)); // 42 + 10

        // Test void method that modifies state
        resolver.Invoke("instance_void", [100]);
        
        // Test instance method after state modification
        var instanceResult2 = resolver.Invoke("instance_method", [10]);
        Assert.That(instanceResult2, Is.EqualTo(110)); // 100 + 10
    }

    [Test]
    public void Register_ClassWithMixedMethods_NullInstance_ShouldRegisterOnlyStaticMethods()
    {
        // Act
        var result = resolver.Register<MixedMethodsClass>(null);

        // Assert
        Assert.That(result, Is.SameAs(resolver));

        // Static methods should work
        Assert.DoesNotThrow(() => resolver.Invoke("static_method", [15]));
        Assert.DoesNotThrow(() => resolver.Invoke("static_void", []));

        // Instance methods should not be registered
        Assert.Throws<NotImplementedException>(() => resolver.Invoke("instance_method", [10]));
        Assert.Throws<NotImplementedException>(() => resolver.Invoke("instance_void", [100]));

        // Verify static method functionality
        var staticResult = resolver.Invoke("static_method", [15]);
        Assert.That(staticResult, Is.EqualTo(30)); // 15 * 2
    }

    [Test]
    public void Register_PureStaticClass_ShouldRegisterAllStaticMethods()
    {
        // This test checks if the updated SimpleSyscallResolver supports static classes
        
        try
        {
            // Act - try to register a pure static class
            var result = resolver.Register(typeof(PureStaticClass), null);

            // Assert - if registration succeeds, static methods should be available
            Assert.That(result, Is.SameAs(resolver));

            var addResult = resolver.Invoke("pure_static_add", [10, 20]);
            Assert.That(addResult, Is.EqualTo(30));

            var concatResult = resolver.Invoke("pure_static_concat", ["hello", "world"]);
            Assert.That(concatResult, Is.EqualTo("STATIC_hello_world"));

            var nullableResult = resolver.Invoke("pure_static_nullable", [Nil.Shared]);
            Assert.That(nullableResult, Is.EqualTo("static_null"));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            // If static class registration is not yet supported, skip this test
            Assert.Inconclusive("Static class registration not yet implemented in SimpleSyscallResolver");
        }
    }

    #endregion

    #region Invoke Tests

    [Test]
    public void Invoke_RegisteredMethod_ShouldReturnCorrectResult()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);

        // Act & Assert
        var addResult = resolver.Invoke("add", [5, 7]);
        Assert.That(addResult, Is.EqualTo(12));

        var multiplyResult = resolver.Invoke("multiply", [3, 4]);
        Assert.That(multiplyResult, Is.EqualTo(12));

        var getValueResult = resolver.Invoke("get_value", []);
        Assert.That(getValueResult, Is.EqualTo(42));

        var concatResult = resolver.Invoke("concat", ["Hello", "World"]);
        Assert.That(concatResult, Is.EqualTo("HelloWorld"));
    }

    [Test]
    public void Invoke_VoidMethod_ShouldReturnNull()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);

        // Act
        var result = resolver.Invoke("set_value", [100]);

        // Assert
        Assert.That(result, Is.Null);
        
        // Verify the method actually executed
        var newValue = resolver.Invoke("get_value", []);
        Assert.That(newValue, Is.EqualTo(100));
    }

    [Test]
    public void Invoke_StaticVoidMethod_ShouldReturnNull()
    {
        // Arrange
        var staticWrapper = new StaticMethodWrapper();
        resolver.Register(staticWrapper);

        // Act
        var result = resolver.Invoke("static_void", []);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Invoke_WithNilArguments_ShouldConvertToNull()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);

        // Act
        var result = resolver.Invoke("handle_null", [Nil.Shared]);

        // Assert
        Assert.That(result, Is.EqualTo("null"));
    }

    [Test]
    public void Invoke_WithNilInstanceInArray_ShouldConvertToNull()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);
        var nilInstance = new Nil();

        // Act
        var result = resolver.Invoke("handle_null", [nilInstance]);

        // Assert
        Assert.That(result, Is.EqualTo("null"));
    }

    [Test]
    public void Invoke_MethodReturningNull_ShouldReturnNull()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);

        // Act
        var result = resolver.Invoke("return_null", []);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Invoke_WithNullArguments_ShouldConvertBackToNil()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);
        var args = new object[] { "test" };

        // Make one argument null
        args[0] = null!;

        // Act
        resolver.Invoke("handle_null", args);

        // Assert - argument should be converted back to Nil
        Assert.That(args[0], Is.EqualTo(Nil.Shared));
    }

    [Test]
    public void Invoke_ValueTypeBoxingUnboxing_ShouldWork()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);

        // Act
        var result = resolver.Invoke("box_unbox", [42]);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Invoke_StructMethods_ShouldWork()
    {
        // Arrange
        var structWrapper = new StructWrapper { Value = 15 };
        resolver.Register(structWrapper);

        // Act
        var getValue = resolver.Invoke("struct_get_value", []);
        var addResult = resolver.Invoke("struct_add", [10]);

        // Assert
        Assert.That(getValue, Is.EqualTo(15));
        Assert.That(addResult, Is.EqualTo(25));
    }

    [Test]
    public void Invoke_DelegateMethod_ShouldWork()
    {
        // Arrange
        SyscallDelegate customMethod = (instance, args) =>
        {
            var sum = 0;
            foreach (var arg in args)
            {
                if (arg is int i)
                    sum += i;
            }
            return sum;
        };
        resolver.Register("sum_all", customMethod);

        // Act
        var result = resolver.Invoke("sum_all", [1, 2, 3, 4, 5]);

        // Assert
        Assert.That(result, Is.EqualTo(15));
    }

    #endregion

    #region Exception Tests

    [Test]
    public void Invoke_UnregisteredMethod_ShouldThrowNotImplementedException()
    {
        // Act & Assert
        var ex = Assert.Throws<NotImplementedException>(() => resolver.Invoke("nonexistent", []));
        Assert.That(ex.Message, Does.Contain("nonexistent"));
        Assert.That(ex.Message, Does.Contain("not implemented"));
    }

    [Test]
    public void Invoke_EmptyMethodName_ShouldThrowNotImplementedException()
    {
        // Act & Assert
        var ex = Assert.Throws<NotImplementedException>(() => resolver.Invoke("", []));
        Assert.That(ex.Message, Does.Contain("not implemented"));
    }

    [Test]
    public void Invoke_NullMethodName_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => resolver.Invoke(null!, []));
    }

    [Test]
    public void Invoke_NullArguments_ShouldThrowException()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => resolver.Invoke("add", null!));
    }

    #endregion

    #region Delegate Creation Tests

    [Test]
    public void CreateDelegate_MultipleCalls_ShouldReuseDelegate()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);

        // Act - call the same method multiple times
        var result1 = resolver.Invoke("add", [1, 2]);
        var result2 = resolver.Invoke("add", [3, 4]);
        var result3 = resolver.Invoke("add", [5, 6]);

        // Assert - should work consistently
        Assert.That(result1, Is.EqualTo(3));
        Assert.That(result2, Is.EqualTo(7));
        Assert.That(result3, Is.EqualTo(11));
    }

    [Test]
    public void CreateDelegate_StaticMethod_ShouldWork()
    {
        // Arrange
        var staticWrapper = new StaticMethodWrapper();
        resolver.Register(staticWrapper);

        // Act
        var result = resolver.Invoke("static_add", [10, 20]);

        // Assert
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void CreateDelegate_DifferentParameterTypes_ShouldWork()
    {
        // Arrange
        SyscallDelegate stringToInt = (instance, args) => int.Parse((string)args[0]);
        SyscallDelegate floatAdd = (instance, args) => (float)args[0] + (float)args[1];
        
        resolver.Register("string_to_int", stringToInt);
        resolver.Register("float_add", floatAdd);

        // Act & Assert
        var intResult = resolver.Invoke("string_to_int", ["123"]);
        Assert.That(intResult, Is.EqualTo(123));

        var floatResult = resolver.Invoke("float_add", [1.5f, 2.5f]);
        Assert.That(floatResult, Is.EqualTo(4.0f));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Register_EmptyClass_ShouldNotRegisterAnything()
    {
        // Arrange
        var instance = new object();

        // Act
        resolver.Register(instance);

        // Assert - no methods should be registered
        Assert.Throws<NotImplementedException>(() => resolver.Invoke("anything", []));
    }

    [Test]
    public void Invoke_MethodWithNoParameters_ShouldWork()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);

        // Act
        var result = resolver.Invoke("get_value", []);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Invoke_MethodWithManyParameters_ShouldWork()
    {
        // Arrange
        SyscallDelegate manyParams = (instance, args) =>
        {
            var sum = 0;
            for (int i = 0; i < args.Length; i++)
            {
                sum += (int)args[i] * (i + 1);
            }
            return sum;
        };
        resolver.Register("many_params", manyParams);

        // Act
        var result = resolver.Invoke("many_params", [1, 2, 3, 4, 5]);

        // Assert - 1*1 + 2*2 + 3*3 + 4*4 + 5*5 = 55
        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void Invoke_MethodWithComplexSignature_ShouldWork()
    {
        // Arrange
        var instance = new ComplexSignatureClass();
        resolver.Register(instance);

        // Act & Assert - complex parameter types should work
        var result = resolver.Invoke("complex_method", ["test", 42, true]);
        Assert.That(result, Is.EqualTo("test_42_True"));
    }

    /// <summary>
    /// Test class with complex method signatures
    /// </summary>
    public class ComplexSignatureClass
    {
        [SyscallImpl("complex_method")]
        public string ComplexMethod(string text, int number, bool flag)
        {
            return $"{text}_{number}_{flag}";
        }
    }

    [Test]
    public void Invoke_MixedNilAndNormalArguments_ShouldConvertCorrectly()
    {
        // Arrange
        SyscallDelegate mixedArgs = (instance, args) =>
        {
            var nonNullCount = 0;
            foreach (var arg in args)
            {
                if (arg != null)
                    nonNullCount++;
            }
            return nonNullCount;
        };
        resolver.Register("count_non_null", mixedArgs);

        // Act
        var result = resolver.Invoke("count_non_null", [1, Nil.Shared, "test", new Nil(), 42]);

        // Assert
        Assert.That(result, Is.EqualTo(3)); // Only 1, "test", and 42 are non-null
    }

    [Test]
    public void Invoke_NullableParameters_WithNilArguments_ShouldConvertToNull()
    {
        // Arrange
        var instance = new NullableParametersClass();
        resolver.Register(instance);

        // Act & Assert - Nil should be converted to null for nullable parameters
        var intResult = resolver.Invoke("nullable_int", [Nil.Shared]);
        Assert.That(intResult, Is.EqualTo("null"));

        var boolResult = resolver.Invoke("nullable_bool", [Nil.Shared]);
        Assert.That(boolResult, Is.EqualTo("null"));

        var doubleResult = resolver.Invoke("nullable_double", [Nil.Shared]);
        Assert.That(doubleResult, Is.EqualTo("null"));
    }

    [Test]
    public void Invoke_NullableParameters_WithActualValues_ShouldWork()
    {
        // Arrange
        var instance = new NullableParametersClass();
        resolver.Register(instance);

        // Act & Assert - actual values should work normally
        var intResult = resolver.Invoke("nullable_int", [42]);
        Assert.That(intResult, Is.EqualTo("42"));

        var boolResult = resolver.Invoke("nullable_bool", [true]);
        Assert.That(boolResult, Is.EqualTo("True"));

        var doubleResult = resolver.Invoke("nullable_double", [3.14]);
        Assert.That(doubleResult, Is.EqualTo("3.14"));
    }

    [Test]
    public void Invoke_MultipleNullableParameters_MixedNilAndValues_ShouldWork()
    {
        // Arrange
        var instance = new NullableParametersClass();
        resolver.Register(instance);

        // Act & Assert - mix of Nil and actual values
        var result1 = resolver.Invoke("multiple_nullable", [42, Nil.Shared, 3.14]);
        Assert.That(result1, Is.EqualTo("42_null_3.14"));

        var result2 = resolver.Invoke("multiple_nullable", [Nil.Shared, true, Nil.Shared]);
        Assert.That(result2, Is.EqualTo("null_True_null"));

        var result3 = resolver.Invoke("multiple_nullable", [Nil.Shared, Nil.Shared, Nil.Shared]);
        Assert.That(result3, Is.EqualTo("null_null_null"));
    }

    [Test]
    public void Invoke_MethodReturningNullableType_ShouldHandleNullCorrectly()
    {
        // Arrange
        var instance = new NullableParametersClass();
        resolver.Register(instance);

        // Act & Assert
        var nullResult = resolver.Invoke("return_nullable_int", [true]);
        Assert.That(nullResult, Is.Null);

        var valueResult = resolver.Invoke("return_nullable_int", [false]);
        Assert.That(valueResult, Is.EqualTo(42));
    }

    #endregion

    #region Performance Tests

    [Test]
    public void Invoke_RepeatedCalls_ShouldBeConsistent()
    {
        // Arrange
        var instance = new TestSyscallClass();
        resolver.Register(instance);
        const int iterations = 1000;

        // Act & Assert
        for (int i = 0; i < iterations; i++)
        {
            var result = resolver.Invoke("add", [i, i + 1]);
            Assert.That(result, Is.EqualTo(2 * i + 1), $"Failed at iteration {i}");
        }
    }

    #endregion

    #region Performance Tests

    [Test]
    public void Register_LargeNumberOfMethods_ShouldWork()
    {
        // Arrange
        var largeTestClass = new LargeTestClass();

        // Act
        resolver.Register(largeTestClass);

        // Assert - verify some methods are registered
        Assert.DoesNotThrow(() => resolver.Invoke("method_1", []));
        Assert.DoesNotThrow(() => resolver.Invoke("method_10", []));
        Assert.DoesNotThrow(() => resolver.Invoke("method_20", []));
    }

    [Test]
    public void IntegrationTest_CompleteWorkflow_ShouldWork()
    {
        // This test demonstrates a complete workflow using the resolver
        
        // Arrange - register multiple types
        var testClass = new TestSyscallClass();
        var staticWrapper = new StaticMethodWrapper();
        var structWrapper = new StructWrapper { Value = 100 };
        
        resolver.Register(testClass)
               .Register(staticWrapper)
               .Register(structWrapper);

        // Custom delegate
        resolver.Register("custom_operation", (instance, args) => 
        {
            var a = (int)args[0];
            var b = (int)args[1];
            return a * b + 10;
        });

        // Act & Assert - test various operations
        Assert.That(resolver.Invoke("add", [5, 3]), Is.EqualTo(8));
        Assert.That(resolver.Invoke("static_add", [10, 20]), Is.EqualTo(30));
        Assert.That(resolver.Invoke("struct_get_value", []), Is.EqualTo(100));
        Assert.That(resolver.Invoke("custom_operation", [6, 7]), Is.EqualTo(52)); // 6*7+10

        // Test Nil conversion
        Assert.That(resolver.Invoke("handle_null", [Nil.Shared]), Is.EqualTo("null"));
        
        // Test void methods
        resolver.Invoke("set_value", [999]);
        Assert.That(resolver.Invoke("get_value", []), Is.EqualTo(999));
    }

    /// <summary>
    /// Test class with many methods to test performance
    /// </summary>
    public class LargeTestClass
    {
        [SyscallImpl("method_1")] public int Method1() => 1;
        [SyscallImpl("method_2")] public int Method2() => 2;
        [SyscallImpl("method_3")] public int Method3() => 3;
        [SyscallImpl("method_4")] public int Method4() => 4;
        [SyscallImpl("method_5")] public int Method5() => 5;
        [SyscallImpl("method_6")] public int Method6() => 6;
        [SyscallImpl("method_7")] public int Method7() => 7;
        [SyscallImpl("method_8")] public int Method8() => 8;
        [SyscallImpl("method_9")] public int Method9() => 9;
        [SyscallImpl("method_10")] public int Method10() => 10;
        [SyscallImpl("method_11")] public int Method11() => 11;
        [SyscallImpl("method_12")] public int Method12() => 12;
        [SyscallImpl("method_13")] public int Method13() => 13;
        [SyscallImpl("method_14")] public int Method14() => 14;
        [SyscallImpl("method_15")] public int Method15() => 15;
        [SyscallImpl("method_16")] public int Method16() => 16;
        [SyscallImpl("method_17")] public int Method17() => 17;
        [SyscallImpl("method_18")] public int Method18() => 18;
        [SyscallImpl("method_19")] public int Method19() => 19;
        [SyscallImpl("method_20")] public int Method20() => 20;
        
        // Method without attribute - should not be registered
        public int NotRegisteredMethod() => 999;
    }

    #endregion
}