using Autofac;
using Autofac.Core.Activators.Delegate;
using System.Reflection;
using AutofacValidationExtensions.Models;
using AutofacValidationExtensions.Models.Errors;
using ILAnalysisExtensions;
using ILAnalysisExtensions.Models;

namespace AutofacValidationExtensions.ActivatorsExtensions;

public static class DelegateActivatorExtensions
{
    private static bool IsAutofacResolveMethod(MethodBase methodBase) =>
        methodBase.DeclaringType == typeof(ResolutionExtensions) &&
        methodBase.Name == nameof(ResolutionExtensions.Resolve) &&
        methodBase.IsPublic &&
        methodBase.IsGenericMethod;

    public static (RequiredServicesSearchStatus, HashSet<Type>) GetRequiredTypes(this DelegateActivator activator)
    {
        var result = new HashSet<Type>();

        void AnalyzeFunc(ILInstruction iLInstruction)
        {
            if (iLInstruction.Operand != null &&
                iLInstruction.Operand.GetType().IsSubclassOf(typeof(MethodInfo)) &&
                IsAutofacResolveMethod((MethodInfo)iLInstruction.Operand))
            {
                var methodInfo = (MethodInfo)iLInstruction.Operand;
                result.Add(methodInfo.ReturnType);
            }
        }

        var activatonFuncObject = activator.GetType()
            .GetField("_activationFunction", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(activator);
        if (activatonFuncObject is not Delegate activatonFunc)
        {
            throw new DiValidationException("Cannot find activation function in DelegateActivator");
        }
        activatonFunc.AnalyzeILInstructions(AnalyzeFunc, 20);
        return (RequiredServicesSearchStatus.Success, result);

    }
}