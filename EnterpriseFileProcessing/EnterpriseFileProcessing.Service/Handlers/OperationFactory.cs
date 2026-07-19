using System;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Service.Handlers
{


public static class OperationFactory
{
    public static IOperationHandler GetHandler(OperationType operationType)
    {
        switch (operationType)
        {
            case OperationType.Copy:
                return new CopyHandler();
            // Other operations (Move, Zip, Unzip, etc.) would be added here
            default:
                throw new NotSupportedException($"Operation {operationType} is not supported yet.");
        }
    }
}
}
