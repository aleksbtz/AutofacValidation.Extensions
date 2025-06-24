using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using ILAnalysisExtensions.Models;

#region ReSharperSettings
// ReSharper disable InconsistentNaming
// ReSharper disable FunctionRecursiveOnAllPaths
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable ConvertToConstant.Local
// ReSharper disable ConvertToLocalFunction
// ReSharper disable UnusedVariable
#pragma warning disable CS8321
#pragma warning disable CS0219
#endregion

namespace ILAnalysisExtensions.Tests;

[TestFixture]
public class AnalyzeILInstructionsTests
{
    private static IEnumerable<TestCaseData> FindMethodCallTestCases
    {
        get
        {
            yield return CreateTestCase(() =>
            {
                void MethodForAnalyze()
                {
                    var a = 0;
                    Console.WriteLine("Hello world");
                    var b = 0;
                }

                return WrapLocalFuncToDelegate(MethodForAnalyze);
            }, 1).SetName("FindMethodCall");


            Delegate GetMethodForRecursionTest()
            {
                void MethodForAnalyze()
                {
                    MethodForAnalyze();
                    Console.WriteLine("Hello world");
                }

                return WrapLocalFuncToDelegate(MethodForAnalyze);
            }

            yield return CreateTestCase(GetMethodForRecursionTest, 1, 1000, true)
                .SetName("RecursionMustStop");
            yield return CreateTestCase(GetMethodForRecursionTest, 1, 1, false)
                .SetName("RecursionMustStopIfCacheDisabled");
            yield return CreateTestCase(GetMethodForRecursionTest, 99, 99, false)
                .SetName("RecursionMustStopIfCacheDisabled");

            yield return CreateTestCase(() =>
            {
                Action GetTargetMethod(int value) => () => Console.WriteLine(value);
                var targetMethod = GetTargetMethod(1);
                void TestMethod() => targetMethod();
                return WrapLocalFuncToDelegate(TestMethod);
            }, 1).SetName("ClosureFuncInsideMethod");

            yield return CreateTestCase(() =>
            {
                void TargetMethod(int value) => Console.WriteLine(value);
                void Method1(int value) => TargetMethod(value);
                void Method2(int value) => Method1(value);
                void Method3(int value) => Method2(value);
                void Method4(int value) => Method3(value);
                void Method5(int value) => Method4(value);
                return WrapLocalFuncToDelegate(Method5);
            }, 1).SetName("DeepCallStack");

            yield return CreateTestCase(() =>
            {
                Action GetTargetMethod() => () => Console.WriteLine("hello");
                var targetMethod = GetTargetMethod();
                void Method1() => targetMethod();
                void Method2() => Method1();
                void Method3() => Method2();
                void Method4() => Method3();
                void Method5() => Method4();
                return WrapLocalFuncToDelegate(Method5);
            }, 1).SetName("DeepCallStackWithClosure");

            yield return CreateTestCase(() =>
            {
                var targetMethod1 = () => Console.WriteLine("targetMethod1");
                var targetMethod2 = () => Console.WriteLine("targetMethod2");
                var closuredMethod1 = () => Console.Write("method1");
                var closuredMethod2 = () => Console.Write("method2");
                var closuredMethod3 = () => Console.Write("method3");
                var closuredMethod4 = () => Console.Write("method4");

                void MethodForAnalyze()
                {
                    var someFlag = 64314;
                    closuredMethod1();
                    if (someFlag >= 0)
                    {
                        closuredMethod2();
                        var someList = new List<int> { 1, 2, 3, 4 };
                        var count = someList.Count;
                        targetMethod1();
                        var list = someList.Select(x => x + count);
                    }
                    else
                    {
                        closuredMethod3();
                        for (var i = 0; i < 10; i++)
                        {
                            targetMethod2();
                        }

                        closuredMethod4();
                    }
                }

                return WrapLocalFuncToDelegate(MethodForAnalyze);
            }, 2).SetName("ManyOtherInstructions");

            Delegate GetMethodForCacheTest()
            {
                var targetMethod1 = () => Console.WriteLine("targetMethod1");
                var targetMethod2 = () => Console.WriteLine("targetMethod2");
                var closuredMethod1 = () => Console.Write("method1");
                var closuredMethod2 = () => Console.Write("method2");

                void MethodForAnalyze()
                {
                    targetMethod1();
                    closuredMethod1();
                    targetMethod2();
                    closuredMethod2();
                    targetMethod1();
                    targetMethod1();
                    targetMethod1();
                    targetMethod2();
                }

                return WrapLocalFuncToDelegate(MethodForAnalyze);
            }

            yield return CreateTestCase(GetMethodForCacheTest, 6, 100, false)
                .SetName("CacheTest");
            yield return CreateTestCase(GetMethodForCacheTest, 2, 100, true)
                .SetName("CacheTest");

            yield return CreateTestCase(() =>
            {
                void TestMethod()
                {
                    void LocalAction() => Console.WriteLine("Hello world");
                }

                return WrapLocalFuncToDelegate(TestMethod);
            }, 0).SetName("LocalActionWithoutCall");

            yield return CreateTestCase(() =>
            {
                void TestMethod()
                {
                    var lambdaWithoutCall = () => Console.WriteLine("Hello world");
                }

                return WrapLocalFuncToDelegate(TestMethod);
            }, 0).SetName("LocalLambdaWithoutCall");
            
            yield return CreateTestCase(() =>
            {
                void TestMethod()
                {
                    var lambdaWithCall = () => Console.WriteLine("Hello world");
                    lambdaWithCall();
                }

                return WrapLocalFuncToDelegate(TestMethod);
            }, 0).SetName("LocalLambdaWithCall");

            var LocalLambdaWithCallTargetMethod = () => Console.WriteLine("Hello world");
            yield return CreateTestCase(() =>
            {
                void TestMethod()
                {
                    var lambdaClosure = LocalLambdaWithCallTargetMethod;
                    var trap = () => Console.Write("Hello world");
                    trap();
                    lambdaClosure();
                }

                return WrapLocalFuncToDelegate(TestMethod);
            }, 1).SetName("LocalDelegateWithSameTypeAsExternal");
            
            //not support
            //right result is 0
            var LocalDelegateWithSameTypeAsExternalWithoutCall = () => Console.WriteLine("Hello world");
            yield return CreateTestCase(() =>
            {
                void TestMethod()
                {
                    var lambdaClosure = LocalDelegateWithSameTypeAsExternalWithoutCall;
                    var lambdaType = lambdaClosure.Method;
                    var trap = () => Console.Write("Hello world");
                    trap();
                }                

                return WrapLocalFuncToDelegate(TestMethod);
            }, 1).SetName("LocalDelegateWithSameTypeAsExternalWithoutCall");

            TestCaseData CreateTestCase(
                Func<Delegate> getMethodFoAnalysis,
                int expectedFoundInstructionsCount,
                int? maxCallStackDeep = 1000,
                bool? cacheEnabled = true) => new(
                getMethodFoAnalysis(),
                expectedFoundInstructionsCount,
                maxCallStackDeep,
                cacheEnabled);
        }
    }

    [Test]
    [TestCaseSource(nameof(FindMethodCallTestCases))]
    public void FindMethodCallTest(
        Delegate methodFoAnalysis, 
        int expectedFoundInstructionsCount, 
        int maxCallStackDeep,
        bool cacheEnabled = true)
    {
        var result = new List<object>();

        //The test task is to count the number of "Console.WriteLine" call in the delegate and nested calls
        methodFoAnalysis.AnalyzeILInstructions(GetCommonIlAnalyzeFunc(result), maxCallStackDeep, cacheEnabled);

        result.Count.Should().Be(expectedFoundInstructionsCount);
    }

    private static Delegate WrapLocalFuncToDelegate(Delegate @delegate) => @delegate;

    private static bool CommonIlInstructionCheck(ILInstruction ilInstruction) =>
        ilInstruction.Code.FlowControl == FlowControl.Call &&
        ilInstruction.Operand is MethodInfo methodInfo &&
        methodInfo.DeclaringType == typeof(Console) &&
        methodInfo.Name == nameof(Console.WriteLine) &&
        methodInfo.IsPublic;

    private static Action<ILInstruction> GetCommonIlAnalyzeFunc(ICollection<object> result) =>
        iLInstruction =>
        {
            if (CommonIlInstructionCheck(iLInstruction)) result.Add(iLInstruction.Operand);
        };
}