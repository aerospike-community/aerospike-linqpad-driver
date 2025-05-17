using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LPU = LINQPad.Util;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    /// <summary>
    /// A wrapper around Primary Key&apos;s <see cref="Object"/> value. 
    /// This is used as an aid so that casting is not required to perform comparison operations, etc.
    /// This object also performs implicit casting to standard .Net data types while using LINQ... 
    /// 
    /// If the Aerospike PK digest is returned, the value will be the digest (byte[]) otherwise it will be the PK value.
    /// </summary>
    /// <seealso cref="AValue"/>
    /// <seealso cref="AValue.ToValue(object)"/>
    /// <seealso cref="AValue.ToValue(Client.Bin)"/>
    /// <seealso cref="AValue.ToValue(Client.Value)"/>
    /// <seealso cref="APrimaryKey.ToValue(Client.Key)"/>
    /// <seealso cref="AValueHelper.ToAValue(Client.Bin)"/>
    /// <seealso cref="AValueHelper.ToAPrimaryKey(Client.Key)"/>
    /// <seealso cref="AValueHelper.ToAValue(Client.Value, string, string)"/>
    /// <seealso cref="AValueHelper.ToAValue(object, string, string)"/>
    /// <seealso cref="AValueHelper.ToAValue{T}(T?, string, string)"/>
    /// <seealso cref="AValueHelper.Cast{TResult}(IEnumerable{AValue})"/>
    /// <seealso cref="AValueHelper.OfType{TResult}(IEnumerable{AValue})"/>
    public class APrimaryKey : AValue
    {
        public APrimaryKey(Aerospike.Client.Key key)
            : base(key.userKey?.Object ?? key.digest, "PrimaryKey", "Value")
        {               
            this.AerospikeKey = key;
        }

        public APrimaryKey(APrimaryKey clone)
            : base(clone)
        {
            this.AerospikeKey = clone.AerospikeKey;
        }

        public Aerospike.Client.Key AerospikeKey { get; }

		public override bool Equals(byte[] byteArray)
                => byteArray is not null && byteArray.Length == 20
                        && this.AerospikeKey.digest.SequenceEqual(byteArray);

        public override bool Equals(string digestStr)
                => (digestStr is not null
                        && digestStr.Length == 42
                        && digestStr[0] == '0'
                        && char.ToLower(digestStr[1]) == 'x'
                        && Helpers.HasHexValues(digestStr[2..])
						&& this.Equals(Helpers.StringToByteArray(digestStr[2..])))
                    || base.Equals(digestStr);

		public override bool Equals(AValue value)
		{
            if(value is not null)
            {
                if(value is APrimaryKey pkValue)                
                    return this.CompareDigest(pkValue.AerospikeKey);
				
                if(value.Value is byte[] byteValue)
                    return this.Equals(byteValue);
            }
            return base.Equals(value);
		}

		public override bool Equals(Aerospike.Client.Value value)
		{
			if(value is not null && value.Object is byte[] bArray)
			    return this.Equals(bArray);

			return base.Equals(value);
		}

		/// <summary>
		/// If true, the PK has an actual value. If false, the digest is only provided.
		/// </summary>
		public bool HasKeyValue { get => this.AerospikeKey.userKey?.Object is not null; }

		/// <summary>
		/// Unique server hash value generated from set name and user key.
		/// </summary>
		public byte[] Digest => this.AerospikeKey.digest;

        public static APrimaryKey ToValue(Aerospike.Client.Key key) => new APrimaryKey(key);

        public override int GetHashCode() => this.DigestRequired()
                                                ? this.AerospikeKey.digest.GetHashCode()
                                                : base.GetHashCode();

        override public object ToDump()
        {
            return this.DigestRequired()
                    ? LPU.WithStyle($"Digest<0x{Helpers.ByteArrayToString(this.AerospikeKey.digest)}>",
                                        "color:DarkSlateGray")
                    : this.Value;
        }

        public override string ToString() => this.DigestRequired()
                                                ? Helpers.ByteArrayToString(this.AerospikeKey.digest)
                                                : base.ToString();

        protected override bool DigestRequired() => this.AerospikeKey.userKey?.Object is null;
        protected override bool CompareDigest(object value)
        {            
            if (value is null) return false;
            if(ReferenceEquals(this, value)) return true;

            if(value is Aerospike.Client.Key key)
            {
                if(key.userKey is null) return this.AerospikeKey.digest.SequenceEqual(key.digest);
                return this.CompareDigest(key.userKey);
            }
            else if (value is Aerospike.Client.Value avalue)
            {
                return this.AerospikeKey.digest.SequenceEqual(Aerospike.Client.Key.ComputeDigest(this.AerospikeKey.setName,
                                                                                                 avalue));
            }
            else if(value is APrimaryKey pkValue)
            {
                return this.CompareDigest(pkValue.AerospikeKey);
            }
            else if(value is AValue aValue)
            {
                return this.CompareDigest(aValue.Value);
            }

            var dbValue = Helpers.ConvertToAerospikeType(value);
            return this.AerospikeKey.digest.SequenceEqual(Aerospike.Client.Key.ComputeDigest(this.AerospikeKey.setName,
                                                                                                Aerospike.Client.Value.Get(dbValue)));
        }

        
    }
}
