//Based on:
//https://www.codeproject.com/Articles/14058/Parsing-the-IL-of-a-Method-Body
//https://github.com/rhotav/Dis2Msil

using System.Reflection;
using System.Reflection.Emit;
using ILAnalysis.Extensions.Models;

namespace ILAnalysis.Extensions;

public static class MethodInfoExtensions
{
    private static readonly Dictionary<ushort, OpCode> OpCodesDictionary =
        typeof(OpCodes)
            .GetFields()
            .Where(fi => fi.FieldType == typeof(OpCode))
            .Select(fi => (OpCode)fi.GetValue(null))
            .ToDictionary(opCode => (ushort)opCode.Value);

    //ECMA-335 VI.C.2
    private static readonly Dictionary<OperandType, byte> OperandTypeBytesCount = new()
    {
        { OperandType.InlineBrTarget, 4 },
        { OperandType.InlineField, 4 },
        { OperandType.InlineI, 4 },
        { OperandType.InlineI8, 8 },
        { OperandType.InlineMethod, 4 },
        { OperandType.InlineNone, 0 },
        { OperandType.InlineR, 8 },
        { OperandType.InlineSig, 4 },
        { OperandType.InlineString, 4 },
        { OperandType.InlineSwitch, 4 },
        { OperandType.InlineTok, 4 },
        { OperandType.InlineType, 4 },
        { OperandType.InlineVar, 2 },
        { OperandType.ShortInlineBrTarget, 1 },
        { OperandType.ShortInlineI, 1 },
        { OperandType.ShortInlineR, 4 },
        { OperandType.ShortInlineVar, 1 },
    };

    private static readonly Dictionary<OperandType, Func<Module, int, Type[]?, Type[]?, object>>
        MetadataTokenRelolveFuncs = new()
        {
            { OperandType.InlineField, (module, metadataToken, gta, gma) => module.ResolveField(metadataToken, gta, gma) },
            { OperandType.InlineMethod, (module, metadataToken, gta, gma) => module.ResolveMethod(metadataToken, gta, gma) },
            { OperandType.InlineSig, (module, metadataToken, _, _) => module.ResolveSignature(metadataToken) },
            { OperandType.InlineType, (module, metadataToken, gta, gma) => module.ResolveType(metadataToken, gta, gma) },
            { OperandType.InlineString, (module, metadataToken, _, _) => module.ResolveString(metadataToken) },
        };


    //ECMA-335 VI.C.2
    //The in-line argument is stored with least significant byte first ("little endian")
    private static int ReadInt32(byte[] il) => il[0] | (il[1] << 8) | (il[2] << 16) | (il[3] << 24);

    public static List<ILInstruction> GetILInstructions(this MethodBase methodBase)
    {
        var instructions = new List<ILInstruction>();
        if (methodBase.DeclaringType?.Assembly.FullName == null ||
            methodBase.DeclaringType.Assembly.FullName.StartsWith("System"))
        {
            return instructions;
        }

        var module = methodBase.Module;
        var il = methodBase.GetMethodBody()?.GetILAsByteArray();
        if (il == null) return instructions;

        var position = 0;

        var genericTypeArguments = Array.Empty<Type>();
        if (methodBase.DeclaringType.IsGenericType)
            genericTypeArguments = methodBase.DeclaringType.GetGenericArguments();


        var genericMethodArguments = Array.Empty<Type>();
        if (methodBase.IsGenericMethod) genericMethodArguments = methodBase.GetGenericArguments();

        while (position < il.Length)
        {
            var instruction = new ILInstruction();

            //ECMA-335 III.1.2
            var opCodeValue = il[position] != 0xfe ? il[position] : (ushort)(0xfe00 | il[position + 1]);
            var opCode = OpCodesDictionary[opCodeValue];
            position += opCode.Size;

            instruction.Code = opCode;
            instruction.Offset = position - 1;

            var operandBytesCount = OperandTypeBytesCount[opCode.OperandType];
            var operandBytes = il.Skip(position).Take(operandBytesCount).ToArray();
            instruction.Bytes = new OperandBytes(operandBytes);
            position += operandBytesCount;

            if (MetadataTokenRelolveFuncs.ContainsKey(opCode.OperandType))
            {
                var metadataToken = ReadInt32(operandBytes);
                instruction.Operand =
                    MetadataTokenRelolveFuncs[opCode.OperandType](
                        module, metadataToken, genericTypeArguments, genericMethodArguments);
            }

            if (opCode.OperandType == OperandType.InlineSwitch) position += 4 * ReadInt32(operandBytes);

            instructions.Add(instruction);
        }

        return instructions;
    }

    public static void AnalyzeILInstructions(this MethodBase methodBase,
        Action<ILInstruction> analyzeInstruction, int maxCallStackDeep = 1000, bool cacheEnable = true) =>
        AnalyzeILInstructions(
            methodBase,
            analyzeInstruction,
            new Stack<MethodCallInfo>(),
            maxCallStackDeep,
            new HashSet<MethodBase>(),
            cacheEnable);


    internal static void AnalyzeILInstructions(
        this MethodBase methodBase,
        Action<ILInstruction> analyzeInstruction, 
        Stack<MethodCallInfo> callStack, 
        int maxCallStackDeep, 
        HashSet<MethodBase> cache, 
        bool cacheEnable)
    {
        if (callStack.Count >= maxCallStackDeep) return;
        if (cacheEnable)
        {
            if(cache.Contains(methodBase)) return;
            cache.Add(methodBase); 
        }
        callStack.Push(new MethodCallInfo(methodBase, null));
        var ilInstructions = methodBase.GetILInstructions();

        foreach (var ilInstruction in ilInstructions)
        {
            analyzeInstruction(ilInstruction);
            if (ilInstruction.Operand != null &&
                ilInstruction.Operand.GetType().IsSubclassOf(typeof(MethodBase)))
            {
                var nestedMethodBaseCall = (MethodBase)ilInstruction.Operand;
                nestedMethodBaseCall.AnalyzeILInstructions(
                    analyzeInstruction, callStack, maxCallStackDeep, cache, cacheEnable);
            }
        }

        callStack.Pop();
    }
}