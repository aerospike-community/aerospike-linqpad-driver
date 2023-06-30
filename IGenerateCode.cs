using System;
using System.Collections.Generic;
using System.Text;

namespace Aerospike.Database.LINQPadDriver
{
    public interface IGenerateCode
    {
        (string classCode, string definePropCode, string createInstanceCode)
            CodeCache { get; }

        bool CodeNeedsUpdating { get; }

        (string classCode, string definePropCode, string createInstanceCode)
            CodeGeneration(bool useAValues, bool forceGeneration = false);
    }
}
