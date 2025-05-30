namespace ILAnalysisExtensions.Models;

public class OperandBytes
{
    public byte[] Value { get; }

    public OperandBytes(byte[] value)
    {
        Value = value;
    }

    public override string ToString() => BitConverter.ToString(Value.Reverse().ToArray()).Replace("-", "");
}