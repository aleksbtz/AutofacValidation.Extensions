using System.Reflection;

namespace ILAnalysisExtensions.Models;

internal record MethodCallInfo(MethodBase MethodBase, object? Target);