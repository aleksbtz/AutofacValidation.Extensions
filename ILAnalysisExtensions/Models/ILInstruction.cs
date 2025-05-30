using System.Reflection;
using System.Reflection.Emit;

namespace ILAnalysisExtensions.Models;

public class ILInstruction
{
    public OpCode Code { get; set; }
    public object? Operand { get; set; }
    public OperandBytes Bytes { get; set; }
    public int Offset { get; set; }

    public override string ToString() => $"{GetExpandedOffset(Offset)} : {Code} {GetOperandString()}";

    private string? GetOperandString() =>
        Operand == null
            ? Bytes.ToString()
            : Operand switch
            {
                FieldInfo fieldInfo =>
                    $"{ProcessSpecialTypes(fieldInfo.FieldType.ToString())} " +
                    $"{fieldInfo.ReflectedType}::{fieldInfo.Name}",
                MethodInfo methodInfo =>
                    (methodInfo.IsStatic ? "" : "instance ") +
                    $"{ProcessSpecialTypes(methodInfo.ReturnType.ToString())} " +
                    $"{methodInfo.ReflectedType}::" +
                    $"{methodInfo.Name}()",
                ConstructorInfo constructorInfo =>
                    (constructorInfo.IsStatic ? "" : "instance ") +
                    $"void {constructorInfo.ReflectedType?.ToString()}::{constructorInfo.Name}()",
                Type type => type.FullName,
                string str => (str == "\r\n") ? " \"\\r\\n\"" : $" \"{str}\"",
                _ => "not supported"
            };


    private static string GetExpandedOffset(long offset) => offset.ToString("D4");

    private static string ProcessSpecialTypes(string typeName) =>
        typeName switch
        {
            "System.string" => "string",
            "System.String" => "string",
            "String" => "string",
            "System.Int32" => "int",
            "Int" => "int",
            "Int32" => "int",
            _ => typeName
        };
}