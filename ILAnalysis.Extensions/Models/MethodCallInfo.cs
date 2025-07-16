using System.Reflection;

namespace ILAnalysis.Extensions.Models;

internal record MethodCallInfo(MethodBase MethodBase, object? Target);