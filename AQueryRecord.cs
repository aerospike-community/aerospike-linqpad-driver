using Aerospike.Database.LINQPadDriver.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using LPU = LINQPad.Util;

namespace Aerospike.Database.LINQPadDriver.Extensions
{

    public class AQueryRecord<T> : AQueryRecord
        where T : ARecord
    {
        public AQueryRecord(object idxKey, IEnumerable<T> records)
            : base(idxKey, records)
        {          
        }

        override protected object ToDump()
        {
            return LPU.ToExpando(this, include: "IdxKey,Records", exclude: "key");
        }
    }


    public class AQueryRecord
    {
        public AQueryRecord(object idxKey, IEnumerable<ARecord> records)
        {
            this.IdxKey = idxKey;
            this.Records = records;
        }

        public IEnumerable<ARecord> Records { get; }

        public object IdxKey { get; }
        
        virtual protected object ToDump()
        {
            return LPU.ToExpando(this, include: "IdxKey,Records", exclude: "key");            
        }
    }


}
