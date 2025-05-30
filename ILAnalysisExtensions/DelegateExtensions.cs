using System.Reflection;
using System.Reflection.Emit;
using ILAnalysisExtensions.Models;

namespace ILAnalysisExtensions;

public static class DelegateExtensions
{
    public static void AnalyzeILInstructions(
        this Delegate delegateInstance,
        Action<ILInstruction> analyzeInstruction,
        int maxCallStackDeep = 1000,
        bool cacheEnable = true) =>
        AnalyzeILInstructions(
            delegateInstance,
            analyzeInstruction,
            new Stack<MethodCallInfo>(),
            maxCallStackDeep,
            new HashSet<MethodBase>(),
            cacheEnable);

    private static void AnalyzeILInstructions(
        this Delegate delegateInstance,
        Action<ILInstruction> analyzeInstruction,
        Stack<MethodCallInfo> callStack,
        int maxCallStackDeep,
        HashSet<MethodBase> cache, 
        bool cacheEnable)
    {
        if (callStack.Count >= maxCallStackDeep) return;
        if (cacheEnable)
        {
            if(cache.Contains(delegateInstance.Method)) return;
            cache.Add(delegateInstance.Method); 
        }
        
        callStack.Push(new MethodCallInfo(delegateInstance.Method, delegateInstance.Target));
        var delegateFuncMethodInfo = delegateInstance.GetMethodInfo();
        var cilInstructions = delegateFuncMethodInfo.GetILInstructions();

        for (var instructionIndex = 0; instructionIndex < cilInstructions.Count; instructionIndex++)
        {
            var ilInstruction = cilInstructions[instructionIndex];
            analyzeInstruction(ilInstruction);


            if (ilInstruction.Operand != null &&
                ilInstruction.Operand.GetType().IsSubclassOf(typeof(MethodInfo)) && 
                ilInstruction.Code.FlowControl == FlowControl.Call)
            {
                var nestedMethodInfoCall = (MethodInfo)ilInstruction.Operand;
                if (nestedMethodInfoCall.DeclaringType != null &&
                    nestedMethodInfoCall.DeclaringType.IsSubclassOf(typeof(Delegate)) &&
                    delegateInstance.Target != null)
                {
                    string? delegateNameForCall = null;
                    for (var reverseIndex = instructionIndex - 1; reverseIndex >= 0; reverseIndex--)
                    {
                        var prevInstruction = cilInstructions[reverseIndex];
                        if (prevInstruction.Code.OperandType != OperandType.InlineField ||
                            prevInstruction.Operand is not FieldInfo prevFieldInfo ||
                            prevFieldInfo.FieldType != nestedMethodInfoCall.DeclaringType ||
                            prevFieldInfo.DeclaringType != delegateFuncMethodInfo.DeclaringType) continue;
                        delegateNameForCall = prevFieldInfo.Name;
                        break;
                    }

                    if (delegateNameForCall == null) continue;
                    var delegateObjectFromContext = delegateInstance.Target
                        .GetType()
                        .GetField(delegateNameForCall)?
                        .GetValue(delegateInstance.Target);
                    if (delegateObjectFromContext is not Delegate delegateFuncFromContext) continue;
                    delegateFuncFromContext.AnalyzeILInstructions(analyzeInstruction, callStack, maxCallStackDeep, cache, cacheEnable);
                }
                else
                {
                    if (!nestedMethodInfoCall.IsStatic && cilInstructions[instructionIndex - 1].Code == OpCodes.Ldarg_0)
                    {
                        var delegateFromCurrentInstance = Delegate.CreateDelegate(delegateInstance.GetType(),
                            delegateInstance.Target, nestedMethodInfoCall);
                        delegateFromCurrentInstance.AnalyzeILInstructions(analyzeInstruction, callStack,
                            maxCallStackDeep, cache, cacheEnable);
                    }
                    else
                    {
                        nestedMethodInfoCall.AnalyzeILInstructions(analyzeInstruction, callStack, maxCallStackDeep, cache, cacheEnable);
                    }
                }
            }
        }

        callStack.Pop();
    }
}