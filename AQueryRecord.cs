using System;
using System.Collections.Generic;
using System.Text;
using LPU = LINQPad.Util;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    /// <summary>
    /// A class used to represent Query records
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
            this.IdxKey = idxKey.ToAValue();
            this.Records = records;
        }

        public IEnumerable<ARecord> Records { get; }

        public AValue IdxKey { get; }
        
        virtual protected object ToDump()
        {
            return LPU.ToExpando(this, include: "IdxKey,Records", exclude: "key");            
        }
    }


}
