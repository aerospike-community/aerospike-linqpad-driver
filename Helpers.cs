using Aerospike.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Aerospike.Database.LINQPadDriver;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Aerospike.Database.LINQPadDriver.Extensions;
using LPEDC = LINQPad.Extensibility.DataContext;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Aerospike.Client
{
    /// <summary>
    /// Instructs the Aerospike LinqPad Driver to use this name for the bin instead of the field/property name.
    /// </summary>
    /// <seealso cref="ConstructorAttribute"/>
    /// <seealso cref="BinIgnoreAttribute"/>
    /// <seealso cref="PrimaryKeyAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class BinNameAttribute : Attribute
    {
        public BinNameAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

        public string PropertyName { get => this.Name; set => this.Name = value; }
    }

    /// <summary>
    /// Instructs the Serializer to use the specified constructor when deserializing that object.
    /// </summary>
    /// <seealso cref="BinNameAttribute"/>
    /// <seealso cref="BinIgnoreAttribute"/>
    /// <seealso cref="PrimaryKeyAttribute"/>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class ConstructorAttribute : Attribute
    {        
    }

    /// <summary>
    /// Instructs the Aerospike LinqPad Driver not to serialize/deserialize the field/property value.
    /// </summary>
    /// <seealso cref="ConstructorAttribute"/>
    /// <seealso cref="BinNameAttribute"/>
    /// <seealso cref="PrimaryKeyAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BinIgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// Instructs the Aerospike LinqPad Driver that this field/property is the Primary Key.
    /// </summary>
    /// <seealso cref="ConstructorAttribute"/>
    /// <seealso cref="BinNameAttribute"/>
    /// <seealso cref="BinIgnoreAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
    }

    public static class LPDHelpers
    {
		/// <summary>
		/// Copies <see cref="ARecord"/>&apos;s from the <paramref name="source"/> to <paramref name="targetSet"/>.
		/// Note that if the namespace and/or set is different, this instances&apos;s values are used, except 
		/// in the case where the primary key is a digest. In these cases, an <see cref="InvalidOperationException"/> is thrown but the PK can be changed with <paramref name="newPrimaryKeyValue"/>.
		/// </summary>
        /// <param name="source">
		/// The collection of records that will be copied
		/// </param>
		/// <param name="targetSet">
		/// The targeted Set where the records will be copied too.
		/// </param>
		/// <param name="newPrimaryKeyValue">
		/// Each record is passed as the first argument, and the return is the new primary key for that record.
		/// Note: it is possible to change any of the record&apos;s bin values via the <see cref="ARecord.SetValue(string, object, bool)"/> method,
        /// but this would change the original record.
		/// </param>
		/// <param name="writePolity">
		/// The Aerospike Write Policy.
		/// </param>
		/// <param name="parallelOptions">
		/// The <see cref="ParallelOptions"/> used to perform the copy operation.
		/// </param>
		/// <returns>
		/// Returns the Set passed in with <paramref name="targetSet"/>
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// If the record&apos;s primary key is a digest (not an actual value). This exception will be thrown,
		/// since a digest has the namespace and set of where this record was retrieved from. 
		/// </exception>
		/// <seealso cref="CopyRecords(IEnumerable{ARecord}, ANamespaceAccess, string, WritePolicy, ParallelOptions)"/>
		/// <seealso cref="CopyRecords(IEnumerable{ARecord}, SetRecords, WritePolicy, ParallelOptions)"/>
		/// <seealso cref="CopyRecords(IEnumerable{ARecord}, ANamespaceAccess, string, Func{ARecord, dynamic}, WritePolicy, ParallelOptions)"/>
		public static SetRecords CopyRecords([NotNull] this IEnumerable<ARecord> source,
                                                    [NotNull] SetRecords targetSet,
                                                    Func<ARecord, dynamic> newPrimaryKeyValue,
                                                    WritePolicy writePolity = null,
                                                    ParallelOptions parallelOptions = null)
		    => CopyRecords<ARecord>(source, targetSet, newPrimaryKeyValue, writePolity, parallelOptions);

		public static SetRecords CopyRecords<S>([NotNull] IEnumerable<S> source,
													[NotNull] SetRecords targetSet,
													Func<S, dynamic> newPrimaryKeyValue,
													WritePolicy writePolity = null,
													ParallelOptions parallelOptions = null)
            where S : ARecord
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(targetSet);

			parallelOptions ??= new ParallelOptions();

			Parallel.ForEach(source, parallelOptions, record =>
			{
				var newPK = newPrimaryKeyValue(record);
				targetSet.Put(newPK,
								record.ToDictionary(),
								writePolity,
								record.Aerospike.TTL);
			});

			return targetSet;
		}

        /// <summary>
        /// Copies <see cref="ARecord"/>&apos;s from the <paramref name="source"/> to <paramref name="targetSetName"/> in <paramref name="targetNamespace"/>.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used, except 
        /// in the case where the primary key is a digest. In these cases, an <see cref="InvalidOperationException"/> is thrown but the PK can be changed with <paramref name="newPrimaryKeyValue"/>
        /// </summary>
        /// <param name="source">
        /// The collection of records that will be copied
        /// </param>
        /// <param name="targetNamespace">
        /// The Namespace that will be the target of the record copy. 
        /// </param>
        /// <param name="targetSetName">
        /// The targeted Set where the records will be copied too.
        /// This can be a new set within the namespace.
        /// This can be null, in which case the records are copied to the &apos;null&apos; set.
        /// </param>
        /// <param name="newPrimaryKeyValue">
        /// Each record is passed as the first argument, and the return is the new primary key for that record.
        /// Note: it is possible to change any of the record&apos;s bin values via the <see cref="ARecord.SetValue(string, object, bool)"/> method,
        /// but this would change the original record.
        /// </param>
        /// <param name="writePolity">
        /// The Aerospike Write Policy.
        /// </param>
        /// <param name="parallelOptions">
        /// The <see cref="ParallelOptions"/> used to perform the copy operation.
        /// </param>
        /// <returns>
        /// Returns the Set instance that was the target set or null to indicated that a <see cref="ANamespaceAccess.RefreshExplorer"/> is required.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If the record&apos;s primary key is a digest (not an actual value). This exception will be thrown,
        /// since a digest has the namespace and set of where this record was retrieved from. 
        /// </exception>
        /// <seealso cref="CopyRecords(IEnumerable{ARecord}, SetRecords, WritePolicy, ParallelOptions)"/>
        /// <seealso cref="CopyRecords(IEnumerable{ARecord}, SetRecords, Func{ARecord, dynamic}, WritePolicy, ParallelOptions)"/>
        /// <seealso cref="CopyRecords(IEnumerable{ARecord}, ANamespaceAccess, string, WritePolicy, ParallelOptions)"/>
        public static SetRecords CopyRecords([NotNull] this IEnumerable<ARecord> source,
                                                [NotNull] ANamespaceAccess targetNamespace,
                                                string targetSetName,
                                                Func<ARecord, dynamic> newPrimaryKeyValue,
                                                WritePolicy writePolity = null,
                                                ParallelOptions parallelOptions = null)
            => CopyRecords<ARecord>(source, targetNamespace, targetSetName, newPrimaryKeyValue, writePolity, parallelOptions);

		public static SetRecords CopyRecords<T>([NotNull] IEnumerable<T> source,
												[NotNull] ANamespaceAccess targetNamespace,
												string targetSetName,
												Func<T, dynamic> newPrimaryKeyValue,
												WritePolicy writePolity = null,
												ParallelOptions parallelOptions = null)
			where T : ARecord
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(targetNamespace);

			parallelOptions ??= new ParallelOptions();

			Parallel.ForEach(source, parallelOptions, record =>
			{
				var newPK = newPrimaryKeyValue(record);
				targetNamespace.Put(targetSetName,
										newPK,
										record.ToDictionary(),
										writePolity,
										record.Aerospike.TTL);
			});

			return targetNamespace[targetSetName];
		}

        /// <summary>
        /// Copies <see cref="ARecord"/>&apos;s from the <paramref name="source"/> to <paramref name="targetSet"/>.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used, except 
        /// in the case where the primary key is a digest. In these cases, an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="source">
        /// The collection of records that will be copied
        /// </param>
        /// <param name="targetSet">
        /// The targeted Set where the records will be copied too.
        /// </param>
        /// <param name="writePolity">
        /// The Aerospike Write Policy.
        /// </param>
        /// <param name="parallelOptions">
        /// The <see cref="ParallelOptions"/> used to perform the copy operation.
        /// </param>
        /// <returns>
        /// Returns the Set passed in with <paramref name="targetSet"/>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If the record&apos;s primary key is a digest (not an actual value). This exception will be thrown,
        /// since a digest has the namespace and set of where this record was retrieved from. 
        /// </exception>
        /// <seealso cref="CopyRecords(IEnumerable{ARecord}, ANamespaceAccess, string, WritePolicy, ParallelOptions)"/>
        /// <seealso cref="CopyRecords(IEnumerable{ARecord}, SetRecords, Func{ARecord, dynamic}, WritePolicy, ParallelOptions)"/>
        public static SetRecords CopyRecords([NotNull] this IEnumerable<ARecord> source,
                                                [NotNull] SetRecords targetSet,
                                                WritePolicy writePolity = null,
                                                ParallelOptions parallelOptions = null)
            => CopyRecords<ARecord>(source, targetSet, writePolity, parallelOptions);

		public static SetRecords CopyRecords<S>([NotNull] IEnumerable<S> source,
												        [NotNull] SetRecords targetSet,
												        WritePolicy writePolity = null,
												        ParallelOptions parallelOptions = null)
            where S : ARecord
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(targetSet);

			parallelOptions ??= new ParallelOptions();

			Parallel.ForEach(source, parallelOptions, record =>
			{
				targetSet.Put(record, writePolicy: writePolity);
			});

			return targetSet;
		}

		/// <summary>
		/// Copies <see cref="ARecord"/>&apos;s from the <paramref name="source"/> to <paramref name="targetSetName"/> in <paramref name="targetNamespace"/>.
		/// Note that if the namespace and/or set is different, this instances&apos;s values are used, except 
		/// in the case where the primary key is a digest. In these cases, an <see cref="InvalidOperationException"/> is thrown.
		/// </summary>
		/// <param name="source">
		/// The collection of records that will be copied
		/// </param>
		/// <param name="targetNamespace">
		/// The Namespace that will be the target of the record copy. 
		/// </param>
		/// <param name="targetSetName">
		/// The targeted Set where the records will be copied too.
		/// This can be a new set within the namespace.
		/// This can be null, in which case the records are copied to the &apos;null&apos; set.
		/// </param>
		/// <param name="writePolity">
		/// The Aerospike Write Policy.
		/// </param>
		/// <param name="parallelOptions">
		/// The <see cref="ParallelOptions"/> used to perform the copy operation.
		/// </param>
		/// <returns>
		/// Returns the Set instance that was the target set or null to indicated that a <see cref="ANamespaceAccess.RefreshExplorer"/> is required.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// If the record&apos;s primary key is a digest (not an actual value). This exception will be thrown,
		/// since a digest has the namespace and set of where this record was retrieved from. 
		/// </exception>
		/// <seealso cref="CopyRecords(IEnumerable{ARecord}, SetRecords, WritePolicy, ParallelOptions)"/>
		/// <seealso cref="CopyRecords(IEnumerable{ARecord}, ANamespaceAccess, string, Func{ARecord, dynamic}, WritePolicy, ParallelOptions)"/>
		public static SetRecords CopyRecords([NotNull] this IEnumerable<ARecord> source,
                                                [NotNull] ANamespaceAccess targetNamespace,
                                                string targetSetName,
                                                WritePolicy writePolity = null,
                                                ParallelOptions parallelOptions = null)
		{
			ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(targetNamespace);

			parallelOptions ??= new ParallelOptions();

		    Parallel.ForEach(source, parallelOptions, record =>
            {
                targetNamespace.Put(record,
                                    setName: targetSetName,
                                    writePolicy: writePolity);                
            });

            return targetNamespace[targetSetName];
		}

		/// <summary>
		/// Returns a collection of <see cref="Aerospike.Client.Record"/> from an <see cref="Aerospike.Client.RecordSet"/>.
		/// </summary>
		/// <param name="recordSet">
		/// An <see cref="Aerospike.Client.RecordSet"/>
		/// </param>
		/// <returns>
		/// A collection of <see cref="Aerospike.Client.Record"/>
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// Thrown if <paramref name="recordSet"/> is null.
		/// </exception>
		public static IEnumerable<Record> AsEnumerable(this RecordSet recordSet)
        {
                while (recordSet.Next())
                {
                    yield return recordSet.Record;
                }
        }

        /// <summary>
        /// Casts a <see cref="Aerospike.Client.Bin"/>&apos;s value into <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Data type that the Value will try to be cast too.
        /// </typeparam>
        /// <param name="bin">
        /// The <see cref="Aerospike.Client.Bin"/>
        /// </param>
        /// <returns>
        /// The value casted to <typeparamref name="T"/> or an exception.
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// If the value cannot be casted.
        /// </exception>
        public static T Cast<T>(this Bin bin) => (T) Helpers.CastToNativeType(bin.name, typeof(T), bin.name, bin.value.Object);

        /// <summary>
        ///  Casts a <see cref="Aerospike.Client.Value"/>&apos;s value into <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Data type that the Value will try to be cast too.
        /// </typeparam>
        /// <param name="value">
        /// The <see cref="Aerospike.Client.Value"/>
        /// </param>
        /// <returns>
        /// The value casted to <typeparamref name="T"/> or an exception.
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// If the value cannot be casted.
        /// </exception>
        public static T Cast<T>(this Value value) => value is null ? default : (T) Helpers.CastToNativeType("Value", typeof(T), "Value", value.Object);
        
        public static JArray ToJArray(this IEnumerable<JsonDocument> documents) => new JArray(documents.Cast<JObject>());
        public static JsonDocument ToJsonDocument(this IDictionary<string,object> document) => new JsonDocument(document);

		/// <summary>
		/// Creates a <see cref="JArray"/> based on the collection of <see cref="ARecord"/>.
		/// </summary>
        /// <param name="records">
        /// A collection of <see cref="ARecord"/>
        /// </param>
		/// <param name="pkPropertyName">
		/// The property name used for the primary key. The default is &apos;_id&apos;.
		/// If the primary key value is not present, the digest is used. In these cases the property value will be a sub property where that name will be &apos;$oid&apos; and the value is a byte string.
		/// If this is null, no PK property is written. 
		/// </param>
		/// <param name="useDigest">
		/// If true, always use the PK digest as the primary key.
		/// If false, use the PK value is present, otherwise use the digest. 
		/// Default is false.
		/// </param>
		/// <returns>Json Array of the records in the collection.</returns>
		/// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
		/// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
		/// <seealso cref="ARecord.ToJson(string, bool)"/>
        /// <seealso cref="SetRecords.ToJson(Exp, string, bool)"/>
        /// <seealso cref="SetRecords.FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="SetRecords.FromJson(string, string, string, WritePolicy, TimeSpan?, bool)"/>
		public static JArray ToJson(this IEnumerable<ARecord> records, [AllowNull] string pkPropertyName = "_id", bool useDigest = false)
        {
			var jsonArray = new JArray();
            foreach(var record in records)
            {
                jsonArray.Add(record.ToJson(pkPropertyName, useDigest));
            }
            return jsonArray;
        }

		/// <summary>
		/// This will convert a list of <see cref="JObject"/> to a list of dictionary items.
		/// </summary>
		/// <param name="documentLst">A list of JSON documents/JObjects</param>
		/// <returns>
		/// a list of dictionary items.
		/// </returns>
		public static IEnumerable<IDictionary<string,object>> ToCDT(this IEnumerable<JObject> documentLst)
        {
            foreach(var document in documentLst)
            {
                yield return CDTConverter.ConvertToDictionary(document);
            }
        }

        /// <summary>
        /// This will convert a list of <see cref="JsonDocument"/> to a list of dictionary items.
        /// </summary>
        /// <param name="documentLst">A list of JSON documents/JObjects</param>
        /// <returns>
        /// a list of dictionary items.
        /// </returns>
        public static IEnumerable<IDictionary<string, object>> ToCDT(this IEnumerable<JsonDocument> documentLst)
        {
            foreach (var document in documentLst)
            {
                yield return document.ToDictionary();
            }
        }

        /// <summary>
        /// Converts a .Net object into an Aerospike <see cref="Value"/>, if possible.
        /// </summary>
        /// <param name="value">A .Net object.</param>
        /// <returns>
        /// An Aerospike <see cref="Value"/> instance.
        /// </returns>
        public static Client.Value ToAerospikeValue(this object value) => Client.Value.Get(Helpers.ConvertToAerospikeType(value));

        public static Client.Exp ToAerospikeExpression(this object value)
                    => Helpers.ConvertToAerospikeType(value) switch
                    {
                        byte[] castedObj => Exp.Val(castedObj),
                        IList castedObj => Exp.Val(castedObj),
                        IDictionary castedObj => Exp.Val(castedObj, MapOrder.UNORDERED),
                        string castedObj => Exp.Val(castedObj),
                        int castedObj => Exp.Val(castedObj),
                        long castedObj => Exp.Val(castedObj),
                        bool castedObj => Exp.Val(castedObj),
                        Enum castedObj => Exp.Val(Convert.ToInt32(castedObj)),
                        byte castedObj => Exp.Val(castedObj),
                        sbyte castedObj => Exp.Val(castedObj),
                        short castedObj => Exp.Val(castedObj),
                        uint castedObj => Exp.Val(castedObj),
                        ulong castedObj => Exp.Val(castedObj),
                        ushort castedObj => Exp.Val(castedObj),
                        null => Exp.Nil(),
                        _ => throw new ArgumentException($"Object type is not supported in Aerospike: {value.GetType()}"),
                    };

		/// <summary>
		/// Converts a value to an <see cref="Aerospike.Client.Key"/>.
        /// If value is a string of length 44 that begins with &apos;0x&apos;, it will be treated as a digest.
		/// </summary>
		/// <param name="value">The value to be converted to a Key.</param>
		/// <param name="nameSpace">The namespace associated with the key</param>
		/// <param name="setName">Name of the set associated with the key</param>
		/// <returns><see cref="Aerospike.Client.Key"/></returns>
		public static Client.Key ToAerospikeKey(this object value, string nameSpace, string setName = null)
                        => Helpers.DetermineAerospikeKey(value, nameSpace, setName);

        /// <summary>
        /// Converts a date/time to Unix Epoch nanosecond value
        /// </summary>
        /// <param name="dateTime">Date time to convert</param>
        /// <returns>
        /// Unix Epoch time in nanoseconds
        /// </returns>
        public static long ToUnixEpochNS(this DateTime dateTime) => Helpers.NanosFromEpoch(dateTime);

		/// <summary>
		/// Converts a date/time offset to Unix Epoch nanosecond value
		/// </summary>
		/// <param name="dateTimeOffset">Date time offset to convert</param>
		/// <returns>
		/// Unix Epoch time in nanoseconds
		/// </returns>
		public static long ToUnixEpochNS(this DateTimeOffset dateTimeOffset) => Helpers.NanosFromEpoch(dateTimeOffset.UtcDateTime);

        /// <summary>
        /// Converts a Unix Epoch nanoseconds value to a date time value.
        /// </summary>
        /// <param name="unixEpoch">Unix Epoch in nanoseconds</param>
        /// <returns>Date time value</returns>
        public static DateTime FromUnixEpochNS(this long unixEpoch) => Helpers.NanoEpochToDateTime(unixEpoch);
	}
}

namespace Aerospike.Database.LINQPadDriver
{
    public static partial class Helpers
    {

		public static (string, bool?) GetHostName(string ipAddress, bool tryPing = false, int timeoutms = 200)
		{
            if(string.IsNullOrEmpty(ipAddress)) return ("<Unknown>", null);

            if(ipAddress.ToLower() == "localhost"
				|| ipAddress.ToLower() == "local"
				|| ipAddress == "127.0.0.1"
				|| ipAddress == "::1")
				return ("localhost", true);

            bool? pinged = null;

            if(tryPing)
            {
                try
                {
                    using(Ping pinger = new Ping())
                    {
                        PingReply reply = pinger.Send(ipAddress, timeoutms);
                        pinged = reply.Status == IPStatus.Success;
                    }
                }
                catch(PingException)
                {
                }
            }

			try
			{
				IPHostEntry entry = Dns.GetHostEntry(ipAddress);
				if(entry != null)
				{
					return (entry.HostName, pinged);
				}
			}
			catch
			{
			}

			
			return (ipAddress, pinged);
		}

		public static bool IsPortOpen(string host, int port, int timeoutms = 200)
		{
			try
			{
                using(TcpClient tcpClient = new TcpClient())
                {
                    var cancel = new CancellationTokenSource();
                    var tstTask = tcpClient.ConnectAsync(host, port, cancel.Token);
                    var cnt = 0;
                    while(!tstTask.IsCompleted) 
                    {
                        if(cnt++ > timeoutms)
                        {
                            cancel.Cancel();
                            break;
                        }
                        Thread.Sleep(1);
                    }

                    tstTask.AsTask().Wait();
                    
                    return tstTask.IsCompletedSuccessfully; // Connection successful, port is likely open
                }
			}
			catch(Exception)
			{
				return false; // Connection failed, port is likely closed or unreachable
			}
		}


		public static bool IsPrivateAddress(string ipAddress)
        {
            if(string.IsNullOrEmpty(ipAddress)
                || ipAddress.ToLower() == "localhost"
                || ipAddress.ToLower() == "local"
                || ipAddress == "127.0.0.1"
                || ipAddress == "::1")
                return true;

            try
            {
                int[] ipParts = ipAddress.Split('.', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => int.Parse(s)).ToArray();
                // in private ip range
                if(ipParts[0] == 10 || ipParts[0] == 127 ||
                    (ipParts[0] == 192 && ipParts[1] == 168) ||
                    (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31)))
                {
                    return true;
                }
            }
            catch { return false; }

            // IP Address is probably public.
            // This doesn't catch some VPN ranges like OpenVPN and Hamachi.
            return false;
        }

        /// <summary>
        /// Checks to see if <paramref name="interfaceClass"/> is a subclass of <paramref name="classToCheck"/>.
        /// If the types are generic, the underlying types are ignored.
        /// </summary>
        /// <param name="interfaceClass"></param>
        /// <param name="classToCheck"></param>
        /// <returns></returns>
        public static bool IsSubclassOfInterface(Type interfaceClass, Type classToCheck)
        {
            if(interfaceClass is null || classToCheck is null) return false;
            if(ReferenceEquals(interfaceClass, classToCheck)) return true;

            if(classToCheck.IsGenericType)
            {
                if(!classToCheck.IsGenericTypeDefinition)
                    classToCheck = classToCheck.GetGenericTypeDefinition();
            }
            if(interfaceClass.IsGenericType)
            {
                if(!interfaceClass.IsGenericTypeDefinition)
                    interfaceClass = interfaceClass.GetGenericTypeDefinition();
            }

            if(ReferenceEquals(interfaceClass, classToCheck)) return true;

            return classToCheck.GetInterfaces().Any(ctc => IsSubclassOfInterface(interfaceClass, ctc));

        }

        public static string CheckName(string name, string contextType)
        {
            var changeList = new char[] { '.', ' ', '[', ']', '(', ')', '+', '-', '=', '^', '*', '/', '\\', ',', '<', '>', ';', ':', '@', '#', '!', '%', '&', '?', '~', '`', '$', '"', '\'' };
            StringBuilder newName = new StringBuilder();

            foreach(char c in name)
            {
                if(changeList.Contains(c))
                {
                    newName.Append('_');
                }
                else
                    newName.Append(c);
            }

            var newStr = newName.ToString();

            if(Char.IsDigit(newStr.FirstOrDefault()))
                newStr = contextType + newStr;

            if(LPEDC.DataContextDriver.IsCSharpKeyword(newStr))
            {
                return newStr + '_' + contextType;
            }

            return newStr;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach(byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex("(?<namespace>[^.]+)+",
                        RegexOptions.Compiled)]
        static private partial Regex NamespaceRegEx();
#else
        static readonly Regex namespaceRegEx = new Regex("(?<namespace>[^.]+)+",
                                                            RegexOptions.Compiled);
        static private Regex NamespaceRegEx() => namespaceRegEx;
#endif

        public static string GetRealTypeName(Type t, bool makeIntoNullable = false, bool includeNameSpace = false)
        {
            if(t == null) return null;
            string ns = null;

            if(includeNameSpace)
            {
                var parts = NamespaceRegEx().Matches(t.FullName);

                ns = string.Join(',', parts.SkipLast(1).Select(a => a.Value));
            }

            static string GetDeclaringClass(Type currentType)
            {
                if(currentType.IsGenericParameter)
                    return "{PARAM}";

                var declaringTpe = currentType.DeclaringType;

                return declaringTpe is null
                        ? currentType.Name
                        : GetRealTypeName(declaringTpe, false, false) + '.' + currentType.Name;
            }

            if(!t.IsGenericType)
            {
                string className = GetDeclaringClass(t);

                if(!string.IsNullOrWhiteSpace(ns))
                    className = ns + "." + className;

                return makeIntoNullable && t.IsValueType
                            ? $"Nullable<{className}>"
                            : className;
            }

            StringBuilder sb = new StringBuilder();
            var genericName = GetDeclaringClass(t);
            var paramCnt = genericName.Count(c => c == '{');
            var paramIdx = genericName.IndexOf('}');
            var genericFnd = genericName.Contains('`');
            bool appendComma = false;

            if(!string.IsNullOrEmpty(ns))
                sb.Append(ns);

            if(genericFnd)
            {
                sb.Append(genericName.AsSpan(0, genericName.IndexOf('`')));
                sb.Append('<');
                appendComma = false;
            }
            else
            {
                sb.Append(genericName);
                appendComma = true;
            }

            foreach(Type arg in t.GetGenericArguments())
            {
                var genericArg = GetRealTypeName(arg, makeIntoNullable, includeNameSpace);

                if(paramCnt-- > 0)
                {
                    sb.Replace("{PARAM}", genericArg, 0, paramIdx + 1);
                    paramIdx = genericName.IndexOf('}', paramIdx + 1);
                }
                else
                {
                    if(appendComma) sb.Append(',');
                    sb.Append(genericArg);
                    appendComma = true;
                }
            }

            if(genericFnd)
                sb.Append('>');

            return makeIntoNullable && t.IsValueType
                            ? $"Nullable<{sb}>"
                            : sb.ToString();
        }

        private static string GetBinNameFromProperty(PropertyInfo property)
        {
            if(Attribute.IsDefined(property, typeof(BinIgnoreAttribute)))
                return null;

            string binName;

            if(Attribute.IsDefined(property, typeof(BinNameAttribute)))
            {
                binName = ((BinNameAttribute) Attribute.GetCustomAttribute(property, typeof(BinNameAttribute), false))
                                ?.Name
                            ?? property.Name;
            }
            else
                binName = property.Name;

            return binName;
        }

        private static string GetBinNameFromField(FieldInfo field)
        {
            if(Attribute.IsDefined(field, typeof(BinIgnoreAttribute)))
                return null;

            string binName;

            if(Attribute.IsDefined(field, typeof(BinNameAttribute)))
            {
                binName = ((BinNameAttribute) Attribute.GetCustomAttribute(field, typeof(BinNameAttribute), false))
                                ?.Name
                            ?? field.Name;
            }
            else
                binName = field.Name;

            return binName;
        }


        private static ConstructorInfo GetConstructorInfo(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ConstructorInfo constructor = null;

            if(constructors.Length > 0)
            {
                if(constructors.Length == 1 && constructors.First().GetParameters().Length == 0)
                {
                    return null;
                }

                bool hasDefault = false;

                foreach(var item in constructors)
                {
                    if(item.CustomAttributes.Any(a => a.AttributeType == typeof(ConstructorAttribute)))
                    {
                        return item;
                    }
                    if(item.GetParameters().Length == 0)
                    {
                        hasDefault = true;
                    }
                    else
                    {
                        constructor = item;
                    }
                }

                if(hasDefault) return null;
            }

            return constructor;
        }

        public static T[] RemoveDups<T>(IEnumerable<T> items)
        {
            return items.GroupBy(g => g).Select(g => g.First()).ToArray();
        }

        public static bool SequenceEquals<T>(IEnumerable<T> items, object obj)
        {
            if(items is null) return obj is null;

            if(ReferenceEquals(items, obj)) return true;

            if(obj is IEnumerable<T> tItems) return items.SequenceEqual(tItems);
            if(obj is JArray jArray)
            {
                return SequenceEquals(items, CDTConverter.ConvertToList(jArray));
            }
            if(obj is JObject jObject)
            {
                if(IsSubclassOfInterface(typeof(KeyValuePair<,>), typeof(T)))
                    return SequenceEquals(items,
                                            CDTConverter.ConvertToDictionary(jObject));
                return false;
            }
            if(obj is JProperty jProp)
            {
                if(IsSubclassOfInterface(typeof(KeyValuePair<,>), typeof(T)))
                    return SequenceEquals(items,
                                            CDTConverter.ConvertToDictionary(jProp));
                return false;
            }

            if(obj is IEnumerable iobj)
            {
                return items.Cast<object>().SequenceEqual(iobj.Cast<object>());
            }

            return false;
        }

        public static bool IsAerospikeType(Type type)
        {
            return type.IsPrimitive
                        || type == typeof(string)
                        || type.IsSubclassOf(typeof(Client.Value));
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

#if NET7_0_OR_GREATER
        public static long NanosFromEpoch(DateTime dt) => (long) dt.ToUniversalTime().Subtract(UnixEpoch).TotalNanoseconds;
#else
        public static long NanosFromEpoch(DateTime dt) => (long) dt.ToUniversalTime().Subtract(UnixEpoch).TotalMilliseconds * 1000000;
#endif
        public static DateTime NanoEpochToDateTime(long nanoseconds) => UnixEpoch.AddTicks(nanoseconds / 100);

        internal const string defaultDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffff";
        internal const string defaultDateTimeOffsetFormat = "yyyy-MM-ddTHH:mm:ss.ffffzzz";
        internal const string defaultTimeSpanFormat = "c";

        /// <summary>
        /// Format used to serialize or deserialize a date to/from string 
        /// A null value will use the default format.
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings"/>
        /// </summary>
        public static string DateTimeFormat = defaultDateTimeFormat;
        /// <summary>
        /// Format used to serialize or deserialize a date offset to/from string 
        /// A null value will use the default format.
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings"/>
        /// </summary>
        public static string DateTimeOffsetFormat = defaultDateTimeOffsetFormat;
        /// <summary>
        /// Format used to serialize or deserialize a time to/from string 
        /// A null value will use the default format.
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings"/>
        /// </summary>
        public static string TimeSpanFormat = defaultTimeSpanFormat;

        /// <summary>
        /// A boolean, if true numeric values from the DB for targeted Date/Time data types are nanoseconds from Unix Epoch.
        /// If false, the numeric value represents .net ticks.
        /// <see cref="DateTime.DateTime(long)"/>
        /// <see cref="DateTimeOffset.DateTimeOffset(long, TimeSpan)"/>
        /// <see cref="Client.Exp.Val(DateTime)"/>
        /// <see cref="AllDateTimeUseUnixEpochNano"/>
        /// </summary>
        public static bool UseUnixEpochNanoForNumericDateTime = true;

        /// <summary>
        /// All Date/Time values are converted to nanoseconds from Unix Epoch Date/Time.
        /// </summary>
        /// <see cref="UseUnixEpochNanoForNumericDateTime"/>
        public static bool AllDateTimeUseUnixEpochNano = false;

        public static object ConvertToAerospikeType(object putObject)
        {
            if(putObject == null)
                return null;

            if(!IsAerospikeType(putObject.GetType()))
            {
                if(putObject is ARecord aRecord)
                {
                    putObject = ConvertToAerospikeType(aRecord.Aerospike.GetValues());
                }
                else if(putObject is AValue aValue)
                {
                    putObject = ConvertToAerospikeType(aValue.Value);
                }
                else if(putObject is Aerospike.Client.Value asValue)
                {
                    putObject = asValue.Object;
                }
                else if(putObject is Aerospike.Client.Bin asBin)
                {
                    putObject = asBin.value?.Object;
                }
                else if(putObject is Decimal decValue)
                {
                    putObject = (double) decValue;
                }
                else if(putObject is Enum enumValue)
                {
                    putObject = putObject.ToString();
                }
                else if(putObject is DateTime dateTimeValue)
                {
                    putObject = AllDateTimeUseUnixEpochNano
                                    ? (object) NanosFromEpoch(dateTimeValue)
                                    : dateTimeValue.ToString(DateTimeFormat);
                }
                else if(putObject is DateTimeOffset dateTimeOffsetValue)
                {
                    putObject = AllDateTimeUseUnixEpochNano
                                    ? (object) NanosFromEpoch(dateTimeOffsetValue.UtcDateTime)
                                    : dateTimeOffsetValue.ToString(DateTimeOffsetFormat);
                }
                else if(putObject is TimeSpan timeSpanValue)
                {
                    putObject = AllDateTimeUseUnixEpochNano
                                    ? (object) ((long) timeSpanValue.TotalMilliseconds * 1000000L)
                                    : timeSpanValue.ToString(TimeSpanFormat);
                }
                else if(putObject is Guid guidValue)
                {
                    putObject = guidValue.ToString();
                }
                else if(GeoJSONHelpers.IsGeoValue(putObject.GetType()))
                {
                    return GeoJSONHelpers.ConvertFromGeoJson(putObject);
                }
                else if(putObject is JObject jObject)
                {
                    return ConvertToAerospikeType(CDTConverter.ConvertToDictionary(jObject));
                }
                else if(putObject is JArray jArray)
                {
                    return ConvertToAerospikeType(CDTConverter.ConvertToList(jArray));
                }
                else if(putObject is JProperty jProp)
                {
                    return ConvertToAerospikeType(CDTConverter.ConvertToDictionary(jProp));
                }
                else if(putObject is JValue jValue)
                {
                    return ConvertToAerospikeType(jValue.Value);
                }
                else if(putObject is IDictionary dictValue)
                {
                    var genericTypes = dictValue.GetType().GetGenericArguments();

                    if(genericTypes.Length == 2
                            && genericTypes[0] == typeof(string))
                    {
                        if(!IsAerospikeType(genericTypes[1]))
                        {
                            var newDict = new Dictionary<string, object>();

                            foreach(DictionaryEntry kvp in dictValue)
                            {
                                newDict.Add((string) kvp.Key,
                                            ConvertToAerospikeType(kvp.Value));
                            }

                            putObject = newDict;
                        }
                    }
                    else if(genericTypes.Length == 0
                            || !IsAerospikeType(genericTypes[0])
                            || !IsAerospikeType(genericTypes[1]))
                    {
                        var kvps = dictValue.Keys
                                        .Cast<object>()
                                        .Select(key => ConvertToAerospikeType(key))
                                    .Zip(dictValue.Values
                                            .Cast<object>()
                                            .Select(value => ConvertToAerospikeType(value)),
                                            (k, v) => new KeyValuePair<object, object>(k, v));

                        putObject = new Dictionary<object, object>(kvps);
                    }
                }
                else if(putObject is byte[] byteArray)
                {
                    putObject = byteArray;
                }
                else if(putObject is IEnumerable enumerableValue)
                {
                    var genericTypes = enumerableValue.GetType().GetGenericArguments();

                    if(genericTypes.Length == 0 || !IsAerospikeType(genericTypes[0]))
                    {
                        var newList = new List<object>();

                        foreach(var item in enumerableValue)
                        {
                            newList.Add(ConvertToAerospikeType(item));
                        }
                        putObject = newList;
                    }
                }
                else
                {
                    putObject = TransForm(putObject, nestedItem: true);
                }
            }

            return putObject;
        }

        /// <summary>
        /// Transform object into a Dictionary that can be used with generating a document or bins in Aerospike.
        /// 
        /// <see cref="Aerospike.Client.BinNameAttribute"/> -- defines the bin name, otherwise the kvPair name is used
        /// <see cref="Aerospike.Client.BinIgnoreAttribute"/> -- will ignore this kvPair
        /// </summary>
        /// <param name="instance">Item being transformed</param>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the kvPair
        /// Second argument -- the name of the bin (can be different from kvPair if <see cref="Aerospike.Client.BinNameAttribute"/> is defined)
        /// Third argument -- the instance being transformed
        /// Fourth argument -- if true the instance is within another object.
        /// Returns the new transformed object or null to indicate that this kvPair should be skipped.
        /// </param>
        /// <param name="nestedItem">Indicates if item was nested inside another object.</param>
        /// <returns>
        /// The Dictionary used to pass to Aerospike's put command.
        /// </returns>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="CreateBinRecord(object, string, Bin[])"/>
        public static Dictionary<string, object> TransForm(object instance,
                                                                Func<string, string, object, bool, object> transform = null,
                                                                bool nestedItem = false)
        {
            var dictionary = new Dictionary<string, object>();

            foreach(var property in instance.GetType().GetProperties())
            {

                string binName = GetBinNameFromProperty(property);
                object putObject;

                if(string.IsNullOrEmpty(binName)) continue;

                if(transform == null)
                {
                    putObject = property.GetValue(instance);
                    dictionary.Add(binName, ConvertToAerospikeType(putObject));
                }
                else
                {
                    putObject = transform.Invoke(property.Name, binName, property.GetValue(instance), nestedItem);
                    if(putObject != null)
                        dictionary.Add(binName, putObject);
                }

            }

            foreach(var field in instance.GetType().GetFields())
            {
                string binName = GetBinNameFromField(field);
                object putObject;

                if(string.IsNullOrEmpty(binName)) continue;

                if(transform == null)
                {
                    putObject = field.GetValue(instance);
                    dictionary.Add(binName, ConvertToAerospikeType(putObject));
                }
                else
                {
                    putObject = transform.Invoke(field.Name, binName, field.GetValue(instance), nestedItem);
                    if(putObject != null)
                        dictionary.Add(binName, putObject);
                }
            }

            return dictionary;
        }

        public static Bin[] CreateBinRecord<V>(IEnumerable<(string binName, V value)> binItems,
                                                    string prefix = null,
                                                    params Bin[] additionalBins)
        {
            var bins = new List<Aerospike.Client.Bin>(additionalBins);

            if(IsAerospikeType(typeof(V)))
            {
                foreach(var kvPair in binItems)
                {
                    var binName = prefix == null ? kvPair.binName : $"{prefix}.{kvPair.binName}";
                    bins.Add(new Bin(binName, kvPair.value));
                }
            }
            else
            {
                foreach(var kvPair in binItems)
                {
                    var binName = prefix == null ? kvPair.binName : $"{prefix}.{kvPair.binName}";
                    bins.Add(new Bin(binName, ConvertToAerospikeType(kvPair.value)));
                }
            }

            return bins.ToArray();
        }


        public static Bin[] CreateBinRecord<K, V>(IDictionary<K, V> dict,
                                                    string prefix = null,
                                                    params Bin[] additionalBins)
        {
            var bins = new List<Aerospike.Client.Bin>(additionalBins);

            if(typeof(K) == typeof(string))
            {
                if(IsAerospikeType(typeof(V)))
                {
                    foreach(var kvPair in dict)
                    {
                        var binName = prefix == null ? kvPair.Key.ToString() : $"{prefix}.{kvPair.Key}";
                        bins.Add(new Bin(binName, kvPair.Value));
                    }
                }
                else
                {
                    foreach(var kvPair in dict)
                    {
                        var binName = prefix == null ? kvPair.Key.ToString() : $"{prefix}.{kvPair.Key}";
                        bins.Add(new Bin(binName, ConvertToAerospikeType(kvPair.Value)));
                    }
                }

                return bins.ToArray();
            }

            if(IsAerospikeType(typeof(K)) && IsAerospikeType(typeof(V)))
            {
                bins.Add(new Bin(prefix, dict));
            }
            else
            {
                bins.Add(new Bin(prefix, ConvertToAerospikeType(dict)));
            }

            return bins.ToArray();
        }

        public static Bin CreateBinRecord<T>(IEnumerable<T> collection,
                                                string binName)
        {
            if(IsAerospikeType(typeof(T)))
                return new Bin(binName, collection.ToList());

            var newLst = new List<object>();

            foreach(var value in collection)
            {
                newLst.Add(ConvertToAerospikeType(value));
            }

            return new Bin(binName, newLst);
        }

        public static Bin CreateBinRecord<K, V>(IEnumerable<KeyValuePair<K, V>> collection,
                                                string binName)
        {
            if(IsAerospikeType(typeof(K)) && IsAerospikeType(typeof(V)))
                return new Bin(binName, collection.ToDictionary(k => k.Key, v => v.Value));

            var newDict = new Dictionary<object, object>();

            foreach(var kvp in collection)
            {
                newDict.Add(ConvertToAerospikeType(kvp.Key), ConvertToAerospikeType(kvp.Value));
            }

            return new Bin(binName, newDict);
        }

        /// <summary>
        /// Creates an array of bins based on <paramref name="item"/> and <paramref name="additionalBins"/>
        /// </summary>
        /// <param name="item">
        /// If item is an IDictionary&lt;string, object&gt; 
        ///     each element is evaluated and a new bin created where the key is the bin name and value is the bin&apos;s value
        /// if item is an IList&lt;object&gt;
        ///     each element id evaluated and if the element is a list or dictionary, this method is recursively called.
        ///     The bins created are added to the collection. 
        ///     If the element is not a list or dictionary, a bin is created using the <paramref name="prefix"/> as the bin name and the element as the value.
        /// If item is neither of the above. A bin is created where <paramref name="prefix"/> is the name and item is the value.
        /// </param>
        /// <param name="prefix">
        /// Depending on the type of <paramref name="item"/> will determine how it is used.
        /// If <paramref name="item"/> is a dictionary, prefix is a prefix to the key as part of the bin name.
        /// If it is a list, the prefix is passed this method or used as the bin name...
        /// Otherwise it is used as the bin name.
        /// </param>
        /// <param name="additionalBins">
        /// Bins that will be part of the array of bins returned.
        /// </param>
        /// <returns>
        /// An array for bins based on <paramref name="item"/>.
        /// </returns>
        public static Bin[] CreateBinRecord(object item, string prefix = null, params Bin[] additionalBins)
        {
            var bins = new List<Aerospike.Client.Bin>(additionalBins);

            if(item is IDictionary<string, object> dict)
            {
                bins.AddRange(CreateBinRecord(dict, prefix));
            }
            else if(item is IEnumerable<(string, object)> binItems)
            {
                bins.AddRange(CreateBinRecord<object>(binItems, prefix));
            }
            else if(item is string strItem)
            {
                bins.Add(new Bin(prefix, strItem));
            }
            else if(item is IList<object> lst)
            {
                bins.Add(CreateBinRecord(lst, prefix));
            }
            else
            {
                bins.Add(new Bin(prefix, item));
            }

            return bins.ToArray();
        }

		static readonly char[] HexChars = new char[]{'a', 'A', 'b', 'B', 'c', 'C', 'd', 'D', 'e', 'E', 'f', 'F'};
        static public bool HasHexValues(string hexstring)
            => hexstring.All(i => char.IsDigit(i) || HexChars.Any(c => c == i));

        /// <summary>
        /// Compares two values to determine if they are equal
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>True if Equal</returns>
        public new static bool Equals(object a, object b)
        {
            if (a is null) return b is null;
            if (b is null) return false;
            if(ReferenceEquals(a, b)) return true;

            if (a is AValue aa)
                return aa.Equals(b);
            if (b is AValue ab)
                return ab.Equals(a);

            if (a is string sa)
            {
                if (b is string sb) return sa == sb;

                if(b is byte[] bBytes
                        && sa is not null
                        && sa.Length >= 4
                        && sa[0] == '0'
                        && char.ToLower(sa[1]) == 'x'
                        && HasHexValues(sa[2..]))
                    return SequenceEquals(StringToByteArray(sa[2..]), bBytes);

                return false;
            }
            if (b is string sbb)
            {
				if(a is byte[] aBytes
						&& sbb is not null
						&& sbb.Length >= 4
						&& sbb[0] == '0'
						&& char.ToLower(sbb[1]) == 'x'
						&& HasHexValues(sbb[2..]))
					return SequenceEquals(StringToByteArray(sbb[2..]), aBytes);

				return false;
            }
           
            if (a is IEnumerable<object> alist)
                return SequenceEquals(alist, b);
            if (b is IEnumerable<object> blist)
                return SequenceEquals(blist, a);

            if (a is IEnumerable aiobj)
                return SequenceEquals(aiobj.Cast<object>(), b);
            if (b is IEnumerable biobj)            
                return SequenceEquals(biobj.Cast<object>(), a);
            
            var aType = a.GetType();
            var bType = b.GetType();

            if (Helpers.IsSubclassOfInterface(typeof(Nullable<>), aType))
            {
                return Helpers.Equals(((dynamic)a).Value, b);
            }

            if (Helpers.IsSubclassOfInterface(typeof(Nullable<>), bType))
            {
                return Helpers.Equals(a, ((dynamic)b).Value);
            }

            if (aType == bType)
            {                
                return a.Equals(b);
            }

            if (aType.IsPrimitive
                && bType.IsPrimitive
                && Marshal.SizeOf(aType) <= Marshal.SizeOf(bType))
            {
                if (Helpers.IsSubclassOfInterface(typeof(IConvertible), aType))
                    return ((IConvertible)a).ToType(bType, null).Equals(b);

            }

            try
            {
                if (Helpers.IsSubclassOfInterface(typeof(IConvertible), bType))
                    return ((IConvertible)b).ToType(aType, null).Equals(a);
            }
            catch { }

            return false;
        }


        public static bool EqualsKVP(object a, object b, out object value)
        {
            if (a is null || b is null)
            {
                value = null;
                return false;
            }

            if (Helpers.IsSubclassOfInterface(typeof(KeyValuePair<,>), a.GetType()))
            {
                var fldKeyMethod = a.GetType().GetProperty("Key");
                var fldValueMethod = a.GetType().GetProperty("Value");

                var vKey = fldKeyMethod.GetValue(a);
                var vValue = fldValueMethod.GetValue(a);

                if (Helpers.IsSubclassOfInterface(typeof(KeyValuePair<,>), b.GetType()))
                {
                    var vKey1 = fldKeyMethod.GetValue(b);
                    var vValue1 = fldValueMethod.GetValue(b);

                    if(Helpers.Equals(vKey, vKey1) && Helpers.Equals(vValue, vValue1))
                    {
                        value = vValue;
                        return true;
                    }
                }
                else if(Helpers.Equals(vKey, b))
                {
                    value = vValue;
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Based on <paramref name="fldType"/>, creates a .Net Native (int, decimal, string, datetime, etc.) instance based on <paramref name="binValue"/>.
        /// </summary>
        /// <param name="fldName">
        /// Used mostly for detailed exception messages in case of errors.
        /// </param>
        /// <param name="fldType">
        /// Used to create an instance of this type.
        /// </param>
        /// <param name="binName">
        /// Used mostly for detailed exception messages in case of errors.
        /// </param>
        /// <param name="binValue">
        /// The bin value used to create <paramref name="fldType"/>. This value maybe returned, if the types match. 
        /// </param>
        /// <returns>
        /// An instance of <paramref name="fldType"/> or just <paramref name="binValue"/> depending if their types match.
        /// </returns>
        public static object CastToNativeType(string fldName, Type fldType, string binName, object binValue)
        {
            //Debugger.Launch();

            Exception CreateException<T>(T castValue, Exception innerException = null)
            {
                var castStr = castValue == null
                                ? "null"
                                : (castValue is string
                                    ? $"\"{castValue}\""
                                    : (IsSubclassOfInterface(typeof(IEnumerable), castValue.GetType()) 
                                        ? "<IEnumerable Object(s)>"
                                        : castValue.ToString()));
                
                if (binName == fldName)
                    return new ArgumentException($"Bin \"{binName}\" with Value {castStr} ({GetRealTypeName(binValue?.GetType()) ?? "<UnKnownType>"}) could not be cast to field type {GetRealTypeName(fldType)}",
                                                    innerException);

                return new ArgumentException($"Bin \"{binName}\" with Value {castStr} ({GetRealTypeName(binValue?.GetType()) ?? "<UnKnownType>"}) could not be cast to Field \"{fldName}\" of type {GetRealTypeName(fldType)}",
                                                innerException);
            }

            if (fldType == typeof(object) || fldType == binValue?.GetType())
            {
                return binValue;
            }            
            else
            {
                try
                {
                    if (fldType == typeof(JToken))
                    {
                        return binValue is null ? (JToken)null : JToken.FromObject(binValue);
                    }
                    else if (fldType == typeof(JValue))
                    {
                        return binValue is null ? (JValue)null :  JValue.FromObject(binValue);
                    }
                    else if (fldType == typeof(JArray))
                    {
                        return binValue is null ? (JArray)null : JArray.FromObject(binValue);
                    }
                    else if (fldType == typeof(JObject))
                    {
                        if (binValue is Value.GeoJSONValue geoJson)
                            return JObject.FromObject(geoJson.value);

                        return binValue is null ? (JObject)null : JObject.FromObject(binValue);
                    }
                    else if (fldType == typeof(JsonDocument))
                    {
                        return binValue is null ? (JsonDocument) null : new JsonDocument(JObject.FromObject(binValue));
                    }
                    
                    switch (binValue)
                    {
                        case byte byteValue:
                            {
                                if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    return binValue is null
                                            ? null
                                            : CastToNativeType(fldName, fldType.GetGenericArguments()[0], binName, binValue);
                                }

                                if (fldType == typeof(short))
                                {
                                    return (short)byteValue;
                                }
                                else if (fldType == typeof(int))
                                {
                                    return (int)byteValue;
                                }
                                else if (fldType == typeof(uint))
                                {
                                    return (uint)byteValue;
                                }
                                else if (fldType == typeof(ulong))
                                {
                                    return (ulong)byteValue;
                                }
                                else if (fldType == typeof(ushort))
                                {
                                    return (ushort)byteValue;
                                }
                                else if (fldType == typeof(decimal))
                                {
                                    return (decimal)byteValue;
                                }
                                else if (fldType == typeof(float))
                                {
                                    return (float)byteValue;
                                }
                                else if (fldType == typeof(double))
                                {
                                    return (double)byteValue;
                                }
                                else if (fldType == typeof(bool))
                                {
                                    return byteValue != 0;
                                }
                                else if (fldType == typeof(string))
                                {
                                    return byteValue.ToString();
                                }
                                else
                                {
                                    throw CreateException(byteValue);
                                }
                            }
                        case Int64 intValue:
                            {
                                if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    return binValue is null
                                            ? null
                                            : CastToNativeType(fldName, fldType.GetGenericArguments()[0], binName, binValue);
                                }

                                if (fldType == typeof(short))
                                {
                                    return (short)intValue;
                                }
                                else if (fldType == typeof(int))
                                {
                                    return (int)intValue;
                                }
                                else if (fldType == typeof(long))
                                {
                                    return (long)intValue;
                                }
                                else if (fldType == typeof(uint))
                                {
                                    return (uint)intValue;
                                }
                                else if (fldType == typeof(ulong))
                                {
                                    return (ulong)intValue;
                                }
                                else if (fldType == typeof(ushort))
                                {
                                    return (ushort)intValue;
                                }
                                else if (fldType == typeof(decimal))
                                {
                                    return (decimal)intValue;
                                }
                                else if (fldType == typeof(float))
                                {
                                    return (float)intValue;
                                }
                                else if (fldType == typeof(double))
                                {
                                    return (double)intValue;
                                }
                                else if (fldType == typeof(bool))
                                {
                                    return (long)intValue != 0;
                                }
                                else if (fldType == typeof(string))
                                {
                                    return intValue.ToString();
                                }
                                else if (fldType.IsEnum)
                                {
                                    return Enum.ToObject(fldType, intValue);
                                }
                                else if (fldType == typeof(DateTime))
                                {

                                    return UseUnixEpochNanoForNumericDateTime || AllDateTimeUseUnixEpochNano
                                            ? NanoEpochToDateTime(intValue)
                                            : new DateTime(intValue);
                                }
                                else if (fldType == typeof(DateTimeOffset))
                                {
                                    return UseUnixEpochNanoForNumericDateTime || AllDateTimeUseUnixEpochNano
                                            ? new DateTimeOffset(NanoEpochToDateTime(intValue), TimeSpan.Zero)
                                            : new DateTimeOffset(intValue, TimeSpan.Zero);
                                }
                                else if (fldType == typeof(TimeSpan))
                                {
                                    return UseUnixEpochNanoForNumericDateTime || AllDateTimeUseUnixEpochNano
                                            ? new TimeSpan(intValue / 100)
                                            : new TimeSpan(intValue);
                                }
                                else
                                {
                                    throw CreateException(intValue);
                                }
                            }
                        case double doubleValue:
                            {
                                if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    return binValue is null
                                            ? null
                                            : CastToNativeType(fldName, fldType.GetGenericArguments()[0], binName, binValue);
                                }

                                if (fldType == typeof(short))
                                {
                                    return (short)doubleValue;
                                }
                                else if (fldType == typeof(int))
                                {
                                    return (int)doubleValue;
                                }
                                else if (fldType == typeof(uint))
                                {
                                    return (uint)doubleValue;
                                }
                                else if (fldType == typeof(ulong))
                                {
                                    return (ulong)doubleValue;
                                }
                                else if (fldType == typeof(ushort))
                                {
                                    return (ushort)doubleValue;
                                }
                                else if (fldType == typeof(decimal))
                                {
                                    return (decimal)doubleValue;
                                }
                                else if (fldType == typeof(float))
                                {
                                    return (float)doubleValue;
                                }
                                else if (fldType == typeof(double))
                                {
                                    return (double)doubleValue;
                                }
                                else if (fldType == typeof(bool))
                                {
                                    return doubleValue != 0;
                                }
                                else if (fldType == typeof(string))
                                {
                                    return doubleValue.ToString();
                                }
                                else
                                {
                                    throw CreateException(doubleValue);
                                }
                            }
                        case string strValue:
                            {
                                if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    if (strValue == null) return null;

                                    return CastToNativeType(fldName,
                                                                fldType.GetGenericArguments()[0],
                                                                binName,
                                                                binValue);
                                }

                                if (string.IsNullOrEmpty(strValue))
                                {
                                    if (GeoJSONHelpers.IsGeoValue(fldType))
                                        return null;

                                    return strValue;
                                }
                                else if (fldType == typeof(DateTime))
                                {
                                    if (DateTime.TryParse(strValue, out DateTime dateTime))
                                        return dateTime;

                                    return DateTime.ParseExact(strValue, DateTimeFormat, CultureInfo.InvariantCulture);
                                }
                                else if (fldType == typeof(DateTimeOffset))
                                {
                                    if (DateTimeOffset.TryParse(strValue, out DateTimeOffset dateTime))
                                        return dateTime;

                                    return DateTimeOffset.ParseExact(strValue, DateTimeOffsetFormat, CultureInfo.InvariantCulture);
                                }
                                else if (fldType == typeof(TimeSpan))
                                {
                                    if (TimeSpan.TryParse(strValue, out TimeSpan timespan))
                                        return timespan;

                                    return TimeSpan.ParseExact(strValue, TimeSpanFormat, CultureInfo.InvariantCulture);
                                }
                                else if (fldType == typeof(bool))
                                {
                                    return bool.Parse(strValue);
                                }
                                else if (fldType == typeof(short))
                                {
                                    return short.Parse(strValue);
                                }
                                else if (fldType == typeof(int))
                                {
                                    return int.Parse(strValue);
                                }
                                else if (fldType == typeof(long))
                                {
                                    return long.Parse(strValue);
                                }
                                else if (fldType == typeof(uint))
                                {
                                    return uint.Parse(strValue);
                                }
                                else if (fldType == typeof(ulong))
                                {
                                    return ulong.Parse(strValue);
                                }
                                else if (fldType == typeof(ushort))
                                {
                                    return ushort.Parse(strValue);
                                }
                                else if (fldType == typeof(decimal))
                                {
                                    return decimal.Parse(strValue);
                                }
                                else if (fldType == typeof(float))
                                {
                                    return float.Parse(strValue);
                                }
                                else if (fldType == typeof(double))
                                {
                                    return double.Parse(strValue);
                                }
                                else if (fldType == typeof(Guid))
                                {
                                    return new Guid(strValue);
                                }
                                else if (fldType == typeof(JObject))
                                {
                                    return JObject.Parse(strValue);
                                }
                                else if (fldType == typeof(JToken))
                                {
                                    return JToken.Parse(strValue);
                                }
                                else if (fldType == typeof(JArray))
                                {
                                    return JArray.Parse(strValue);
                                }
                                else if (fldType.IsEnum)
                                {
                                    return Enum.Parse(fldType, strValue, true);
                                }
                                else if(fldType == typeof(Value.GeoJSONValue))
                                {
                                    return new Value.GeoJSONValue(strValue);
                                }
                                else if (GeoJSONHelpers.IsGeoValue(fldType))
                                {
                                    return GeoJSONHelpers.ConvertToGeoJson(strValue);
                                }
                                else
                                {
                                    throw CreateException(strValue);
                                }
                            }
                        case Value.GeoJSONValue geoObj:
                            {
                                return GeoJSONHelpers.ConvertToGeoJson(geoObj);
                            }
                        case DateTime dtValue:
                            {
                                if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    return binValue is null
                                            ? null
                                            : CastToNativeType(fldName,
                                                                fldType.GetGenericArguments()[0],
                                                                binName,
                                                                binValue);
                                }

                                if (fldType == typeof(DateTime))
                                {
                                    return dtValue;
                                }
                                else if (fldType == typeof(DateTimeOffset))
                                {
                                    return new DateTimeOffset(dtValue);
                                }
                                else if (fldType == typeof(string))
                                {
                                    return dtValue.ToString(DateTimeFormat);
                                }
                                else if (fldType == typeof(long))
                                {
                                    if (AllDateTimeUseUnixEpochNano)
                                        return NanosFromEpoch(dtValue);

                                    return dtValue.Ticks;
                                }
                                else if (fldType == typeof(JObject))
                                {
                                    return JObject.FromObject(dtValue);
                                }
                                else if (fldType == typeof(JToken))
                                {
                                    return JToken.FromObject(dtValue);
                                }
                                else
                                {
                                    throw CreateException(dtValue);
                                }
                            }
                        case DateTimeOffset dtoValue:
                            {
                                if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    return binValue is null
                                            ? null
                                            : CastToNativeType(fldName,
                                                                fldType.GetGenericArguments()[0],
                                                                binName,
                                                                binValue);
                                }

                                if (fldType == typeof(DateTime))
                                {
                                    return dtoValue.DateTime;
                                }
                                else if (fldType == typeof(DateTimeOffset))
                                {
                                    return dtoValue;
                                }
                                else if (fldType == typeof(string))
                                {
                                    return dtoValue.ToString(DateTimeFormat);
                                }
                                else if (fldType == typeof(long))
                                {
                                    if (AllDateTimeUseUnixEpochNano)
                                        return NanosFromEpoch(dtoValue.DateTime);

                                    return dtoValue.Ticks;
                                }
                                else if (fldType == typeof(JObject))
                                {
                                    return JObject.FromObject(dtoValue);
                                }
                                else if (fldType == typeof(JToken))
                                {
                                    return JToken.FromObject(dtoValue);
                                }
                                else
                                {
                                    throw CreateException(dtoValue);
                                }
                            }
                        case TimeSpan tsValue:
                            {
                                if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    return binValue is null
                                            ? null
                                            : CastToNativeType(fldName,
                                                                fldType.GetGenericArguments()[0],
                                                                binName,
                                                                binValue);
                                }

                                if (fldType == typeof(TimeSpan))
                                {
                                    return tsValue; 
                                }
                                else if (fldType == typeof(string))
                                {
                                    return tsValue.ToString(TimeSpanFormat);
                                }                                
                                else if (fldType == typeof(JObject))
                                {
                                    return JObject.FromObject(tsValue);
                                }
                                else if (fldType == typeof(JToken))
                                {
                                    return JToken.FromObject(tsValue);
                                }
                                else
                                {
                                    throw CreateException(tsValue);
                                }
                            }
                        case IList<AValue> lstAValue:
                            {
                                return CastToNativeType(fldName,
                                                        fldType,
                                                        binName,
                                                        lstAValue.Cast<object>().ToList());
                            }
                        case IList<object> lstValue:
                            {
                                if (fldType.IsArray)
                                {
                                    var itemType = fldType.GetElementType();
                                    var newArray = Array.CreateInstance(itemType, lstValue.Count);
                                    var idx = 0;

                                    foreach (var item in lstValue.Select(i => CastToNativeType(fldName, itemType, binName, i)).ToArray())
                                    {
                                        newArray.SetValue(item, idx++);
                                    }

                                    return newArray;
                                }

                                if (IsSubclassOfInterface(typeof(IList<>), fldType))
                                {
                                    var itemTypes = fldType.GetGenericArguments();
                                    var newList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemTypes[0]));

                                    foreach (var item in lstValue.Select(i => CastToNativeType(fldName, itemTypes[0], binName, i)).ToArray())
                                    {
                                        newList.Add(item);
                                    }

                                    return newList;
                                }

                                if (fldType != typeof(string) && IsSubclassOfInterface(typeof(IEnumerable<>), fldType))
                                {
                                    var itemTypes = fldType.GetGenericArguments();
                                    var newArray = Array.CreateInstance(itemTypes[0], lstValue.Count);
                                    var idx = 0;

                                    foreach (var item in lstValue.Select(i => CastToNativeType(fldName, itemTypes[0], binName, i)).ToArray())
                                    {
                                        newArray.SetValue(item, idx++);
                                    }

                                    return newArray;
                                }

                                throw CreateException(lstValue);
                            }
                        case IDictionary<AValue, AValue> dictAValue:
                            {
                                return CastToNativeType(fldName,
                                                        fldType,
                                                        binName,
                                                        dictAValue.ToDictionary(k => (object) k.Key, v => (object) v.Value));
                            }
                        case IDictionary<string, object> dictNameValue:
                            {
                                return CastToNativeType(fldName,
                                                        fldType,
                                                        binName,
                                                        dictNameValue.ToDictionary(k => (object)k.Key, v => v.Value));
                            }
                        case IDictionary<object, object> dictValue:
                            {
                                if (fldType == typeof(JsonDocument))
                                {
                                    return new JsonDocument(dictValue);
                                }
                                else if (fldType == typeof(Newtonsoft.Json.Linq.JObject))
                                {
                                    return Newtonsoft.Json.Linq.JObject.FromObject(dictValue);
                                }


                                if (IsSubclassOfInterface(typeof(IDictionary<,>), fldType))
                                {
                                    var itemTypes = fldType.GetGenericArguments();
                                    var newDict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(itemTypes[0], itemTypes[1]));
                                    var keyValues = dictValue.Keys.Select(i => CastToNativeType(fldName, itemTypes[0], binName, i)).ToArray();
                                    var values = dictValue.Values.Select(i => CastToNativeType(fldName, itemTypes[1], binName, i)).ToArray();

                                    for (var idx = 0; idx < dictValue.Count; idx++)
                                    {
                                        newDict.Add(keyValues[idx], values[idx]);
                                    }

                                    return newDict;
                                }
                                
                                return typeof(Helpers)
                                            .GetMethod("Transform")
                                            .MakeGenericMethod(fldType)
                                            .Invoke(null, new object[] { dictValue.ToDictionary(key => (string)key.Key, v => v.Value), null, null });
                            }
                        case AValue aValue:
                            {
                                if (fldType.IsGenericType)
                                {
                                    if (fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        if (aValue.Value is null) return null;

                                        return CastToNativeType(fldName,
                                                                    fldType.GetGenericArguments()[0],
                                                                    binName,
                                                                    binValue);
                                    }
                                    if(aValue.UnderlyingType.IsGenericType
                                            && aValue.UnderlyingType.GetGenericTypeDefinition() == fldType.GetGenericTypeDefinition())
                                    {
                                        return CastToNativeType(fldName,
                                                                    fldType,
                                                                    binName,
                                                                    aValue.Value);
                                    }
                                }

                                if (fldType == typeof(string))
                                {
                                    return (string)aValue;
                                }
                                else if (fldType == typeof(DateTime))
                                {
                                    return (DateTime)aValue;
                                }
                                else if (fldType == typeof(DateTimeOffset))
                                {
                                    return (DateTimeOffset)aValue;
                                }
                                else if (fldType == typeof(TimeSpan))
                                {
                                    return (TimeSpan)aValue;
                                }
                                else if (fldType == typeof(bool))
                                {
                                    return (bool)aValue;
                                }
                                else if (fldType == typeof(short))
                                {
                                    return (short)aValue;
                                }
                                else if (fldType == typeof(int))
                                {
                                    return (int)aValue;
                                }
                                else if (fldType == typeof(uint))
                                {
                                    return (uint)aValue;
                                }
                                else if (fldType == typeof(ulong))
                                {
                                    return (ulong)aValue;
                                }
                                else if (fldType == typeof(ushort))
                                {
                                    return (ushort)aValue;
                                }
                                else if (fldType == typeof(decimal))
                                {
                                    return (decimal)aValue;
                                }
                                else if (fldType == typeof(float))
                                {
                                    return (float)aValue;
                                }
                                else if (fldType == typeof(double))
                                {
                                    return (double)aValue;
                                }
                                else if (fldType == typeof(Guid))
                                {
                                    return (Guid)aValue;
                                }
                                else if (fldType == typeof(JObject))
                                {
                                    return (JObject)aValue;
                                }
                                else if (fldType == typeof(JsonDocument))
                                {
                                    return (JsonDocument)aValue;
                                }
                                else if (fldType.IsEnum)
                                {
                                    return (Enum)aValue;
                                }
                                else
                                {
                                    return aValue.Value;
                                }
                            }                        
                        case IConvertible iConvertible:
                            {
                                if (fldType.IsGenericType)
                                {
                                    if (fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        if (iConvertible is null) return null;

                                        return CastToNativeType(fldName,
                                                                    fldType.GetGenericArguments()[0],
                                                                    binName,
                                                                    binValue);
                                    }
                                }

                                if (fldType == typeof(DateTimeOffset))
                                {
                                    return new DateTimeOffset(iConvertible.ToDateTime(CultureInfo.CurrentCulture));
                                }
                                else if (fldType == typeof(TimeSpan))
                                {
                                    return iConvertible.ToDateTime(CultureInfo.CurrentCulture).TimeOfDay;
                                }
                                else if (fldType == typeof(Guid))
                                {
                                    return new Guid(iConvertible.ToString(CultureInfo.CurrentCulture));
                                }
                                else if (fldType == typeof(JObject))
                                {
                                    return new JObject(iConvertible);
                                }
                                else if (fldType == typeof(JsonDocument))
                                {
                                    return new JsonDocument(new JObject(iConvertible));
                                }
                                else if (fldType.IsEnum)
                                {
                                    if (iConvertible.GetTypeCode() == TypeCode.String)
                                        return Enum.Parse(fldType, iConvertible.ToString(CultureInfo.CurrentCulture));

                                    return Enum.ToObject(fldType, iConvertible.ToInt64(CultureInfo.CurrentCulture));
                                }
                                else
                                {
                                    try
                                    {
                                        return iConvertible.ToType(fldType, CultureInfo.CurrentCulture);
                                    }
                                    catch (Exception ex) { throw CreateException(binValue, ex); }
                                }
                            }
                        default:
                            {
                                if (binValue == null
                                        && (!fldType.IsValueType
                                                || (fldType.IsGenericType
                                                        && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))))
                                    return null;

                                if (binValue != null)
                                {
                                    var binValueType = binValue.GetType();

                                    if (binValueType.IsArray)
                                    {
                                        var binArray = (Array)binValue;

                                        if (fldType.IsArray)
                                        {
                                            var itemType = fldType.GetElementType();
                                            var newArray = Array.CreateInstance(itemType, binArray.Length);
                                            var idx = 0;

                                            foreach (var item in binArray)
                                            {
                                                newArray.SetValue(CastToNativeType(fldName, itemType, binName, item), idx++);
                                            }

                                            return newArray;
                                        }

                                        if (fldType.IsGenericType && IsSubclassOfInterface(typeof(IEnumerable<>), fldType))
                                        {
                                            var itemTypes = fldType.GetGenericArguments();
                                            var newList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemTypes[0]));

                                            foreach (var item in binArray)
                                            {
                                                newList.Add(CastToNativeType(fldName, itemTypes[0], binName, item));
                                            }

                                            return newList;
                                        }

                                        if (fldType == typeof(string) && binValueType.GetElementType() == typeof(byte))
                                        {
                                            return Encoding.Default.GetString((byte[])binValue);
                                        }
                                    }
                                }

                                throw CreateException(binValue);
                            }
                    }
                }
                catch(ArgumentException ex)
                {
                    if (Client.Log.DebugEnabled())
                    {
                        Client.Log.Error($"Helpers.CastToNativeType Exception {ex.GetType().Name} ({ex.Message})");
                        DynamicDriver.WriteToLog(ex, "Helpers.CastToNativeType");
                    }
                    throw; 
                }
                catch (Exception ex) 
                {
                    if (Client.Log.DebugEnabled())
                    {
                        Client.Log.Error($"Helpers.CastToNativeType Exception {ex.GetType().Name} ({ex.Message})");
                        DynamicDriver.WriteToLog(ex, "Helpers.CastToNativeType");
                    }
                    throw CreateException(binValue, ex);
                }
            }
        }

        /// <summary>
        /// Wrapper around <see cref="CastToNativeType(string, Type, string, object)"/> to trap exceptions.
        /// </summary>       
        public static object CastToNativeType(ARecord asRecord, string fldName, Type fldType, string binName, object binValue)
        {
            try
            {
                return CastToNativeType(fldName, fldType, binName, binValue);
            }
            catch (ArgumentException e)
            {
                if (asRecord is not null)
                {
                    if (asRecord.DumpType == ARecord.DumpTypes.Record) asRecord.DumpType = ARecord.DumpTypes.Dynamic;

                    asRecord.SetException(e);
                }
                return null;
            }
        }

        /// <summary>
        /// Wrapper around <see cref="CastToNativeType(string, Type, string, object)"/> to trap exceptions.
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        /// <return>
        /// The converted value or null if an exception occurred and <paramref name="ignoreException"/> is true.
        /// </return>
        public static object CastToNativeTypeInvalidCast(string fldName,
                                                            Type fldType,
                                                            string binName,
                                                            object binValue,
                                                            bool ignoreException = false)
        {
            try
            {
                return CastToNativeType(fldName, fldType, binName, binValue);
            }
            catch (ArgumentException e)
            {
                if (!ignoreException)
                    throw new InvalidCastException($"Cannot cast from {GetRealTypeName(binValue.GetType())} to {GetRealTypeName(fldType)} for field \"{fldName}\"",
                                                    e);               
            }
            return null;
        }

        /// <summary>
        /// Transform from Aerospike <paramref name="bins"/> into an .Net instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Creates an instance of Type T based on the Aerospike <paramref name="bins"/></typeparam>
        /// <param name="bins">The Aerospike Bins, where the Key is the bin name and value is the associated value</param>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the property/field
        /// Second argument -- the property/field type
        /// Third argument -- bin name
        /// Fourth argument -- bin value
        /// Returns the new transformed object or null to indicate that this transformation should be skipped.
        /// </param>
        /// <param name="primaryKey">Primary Key value used to update instance base on attribute</param>
        /// <returns>new instance of <typeparamref name="T"/></returns>
        /// <exception cref="MissingMethodException"></exception>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="Aerospike.Client.ConstructorAttribute"/>
        public static T Transform<T>(Dictionary<string, 
                                        object> bins, 
                                        Func<string, Type, string, object, object> transform = null,
                                        Client.Key primaryKey = null)
        {
            var publicProps = typeof(T).GetProperties();
            var publicFields = typeof(T).GetFields();
            var constructor = GetConstructorInfo(typeof(T));
            var pkProp = primaryKey == null
                            ? null 
                            : publicProps.FirstOrDefault(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(PrimaryKeyAttribute)));
            var pkFld = primaryKey == null
                            ? null 
                            : publicFields.FirstOrDefault(f => f.CustomAttributes.Any(a => a.AttributeType == typeof(PrimaryKeyAttribute)));

            object instance;
            
            if (constructor == null)
            {
                try
                {
                    instance = Activator.CreateInstance(typeof(T), true);
                }
                catch (System.Exception ex)
                {
                    throw new MissingMethodException($"Could not use the Default Constructor to create instance \"{typeof(T).Name}\". ConstructorAttribute is required.",
                                                        ex);
                }
            }
            else
            {
                var args = constructor.GetParameters();
                var values = new List<object>(args.Length);
                var fndArgs = new List<string>(args.Length);
                
                foreach (var arg in args)
                {
                    var fndKVP = bins.FirstOrDefault(kvp => kvp.Key.Equals(arg.Name, StringComparison.OrdinalIgnoreCase));

                    if (fndKVP.Key is null)
                    {
                        var fndProp = publicProps.FirstOrDefault(p => !p.CustomAttributes.Any(a => a.AttributeType == typeof(BinIgnoreAttribute))
                                                                       && p.Name.Equals(arg.Name, StringComparison.OrdinalIgnoreCase));

                        if (fndProp != null)
                            fndKVP = bins.FirstOrDefault(kvp => kvp.Key.Equals(GetBinNameFromProperty(fndProp), StringComparison.OrdinalIgnoreCase));
                    }
                    
                    if (fndKVP.Key != null)
                    {
                        values.Add(CastToNativeType(arg.Name, arg.ParameterType, fndKVP.Key, fndKVP.Value));
                        fndArgs.Add(fndKVP.Key);
                    }
                    else
                    {
                        if (pkProp != null && pkProp.Name.Equals(arg.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            values.Add(CastToNativeType(pkProp.Name,
                                                        pkProp.PropertyType,
                                                        "PrimaryKey",
                                                        primaryKey.userKey?.Object ?? primaryKey.digest));                            
                            pkProp = null;
                            pkFld = null;
                            continue;                            
                        }
                        else if (pkFld != null && pkFld.Name.Equals(arg.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            values.Add(CastToNativeType(pkFld.Name,
                                                        pkFld.FieldType,
                                                        "PrimaryKey",
                                                        primaryKey.userKey?.Object ?? primaryKey.digest));
                            pkProp = null;
                            pkFld = null;
                            continue;
                        }

                        if (arg.HasDefaultValue)
                        {
                            values.Add(arg.DefaultValue);
                        }
                        else
                        {
                            if (arg.ParameterType.IsValueType || arg.ParameterType.IsPrimitive)
                            {
                                values.Add(Activator.CreateInstance(arg.ParameterType));
                            }
                            else
                            {
                                values.Add(null);
                            }
                        }
                    }
                }

                try
                {
                    instance = Activator.CreateInstance(typeof(T), values.ToArray());
                }
                catch (System.Exception ex)
                {
                    throw new MissingMethodException($"Could not determine Constructor or Wrong Constructor Arguments defined. Trying to create instance \"{typeof(T).Name}\". Default Constructor or ConstructorAttribute required? Matched Params are: \"{string.Join(',', fndArgs)}\"",
                                                        ex);
                }

                bins = new Dictionary<string, object>(bins);

                foreach (var fndArg in fndArgs)
                {
                    bins.Remove(fndArg);
                }
            }

            object setValue;

            foreach (var bin in bins)
            {
                var checkBinName = Helpers.CheckName(bin.Key, "Bin");

                var fndProp = publicProps.FirstOrDefault(p => GetBinNameFromProperty(p) == bin.Key || p.Name == checkBinName);

                if (fndProp != null)
                {
                    if (fndProp.CanWrite)
                    {
                        if (transform == null)
                            setValue = CastToNativeType(fndProp.Name, fndProp.PropertyType, bin.Key, bin.Value);
                        else
                        {
                            setValue = transform(fndProp.Name, fndProp.PropertyType, bin.Key, bin.Value);

                            if (setValue == null) continue;
                        }

                        fndProp.SetValue(instance, setValue);
                    }
                }
                else
                {
                    var fndFld = publicFields.FirstOrDefault(f => GetBinNameFromField(f) == bin.Key || f.Name == checkBinName);
                    if (fndFld != null)
                    {
                        if (transform == null)
                            setValue = CastToNativeType(fndFld.Name, fndFld.FieldType, bin.Key, bin.Value);
                        else
                        {
                            setValue = transform(fndFld.Name, fndFld.FieldType, bin.Key, bin.Value);

                            if (setValue == null) continue;
                        }

                        fndFld.SetValue(instance, setValue);
                    }
                }
            }

            if(instance != null && primaryKey != null)
            {
                if (pkProp != null)
                {
                    if (pkProp.CanWrite)
                    {
                        pkProp.SetValue(instance, CastToNativeType(pkProp.Name,
                                                                    pkProp.PropertyType,
                                                                    "PrimaryKey",
                                                                    primaryKey.userKey?.Object ?? primaryKey.digest));
                    }
                    else
                    {
                        pkFld.SetValue(instance, CastToNativeType(pkFld.Name,
                                                                    pkFld.FieldType,
                                                                    "PrimaryKey",
                                                                    primaryKey.userKey?.Object ?? primaryKey.digest));
                    }
                }
            }

            return (T)instance;
        }
        
        public static Client.Key DetermineAerospikeKey(dynamic primaryKey, string nameSpace, string setName)
        {
            Client.Key key;

			if(setName == LPSet.NullSetName)
                setName = null;

            if(primaryKey is APrimaryKey aPrimaryKey)
                primaryKey = aPrimaryKey.AerospikeKey;
            else if(primaryKey is ARecord aRecord)
                primaryKey = aRecord.Aerospike.Key;

            if(primaryKey is Client.Key valueKey)
            {
                if(valueKey.userKey is null && setName != valueKey.setName)
                    throw new InvalidOperationException($"An Aerospike Key (\"{primaryKey}\") was provided with only a digest and the set names where different (Old Set: \"{valueKey.setName}\" New Set: \"{setName}\"). Because of this a Primary Key Value is required.");

                if(setName == valueKey.setName && nameSpace == valueKey.ns)
                    key = valueKey;
                else if(valueKey.userKey is null)
                    key = new Client.Key(nameSpace, valueKey.digest, valueKey.setName, valueKey.userKey);
                else
                    key = new Client.Key(nameSpace, setName, valueKey.userKey);
            }
            else if(primaryKey is AValue aValue)
                key = DetermineAerospikeKey(aValue.Value, nameSpace, setName);
            else if(primaryKey is Value value)
                key = new Client.Key(nameSpace, setName, value);
            else if(primaryKey is byte[] digest)
                key = new Client.Key(nameSpace, digest, setName, Value.AsNull);
            else if(primaryKey is string digestStr
                        && digestStr.Length == 42
                        && digestStr[0] == '0'
                        && char.ToLower(digestStr[1]) == 'x'
                        && HasHexValues(digestStr[2..]))
				key = new Client.Key(nameSpace,
                                        Helpers.StringToByteArray(digestStr[2..]),
                                        setName,
                                        Value.AsNull); 
            else if(primaryKey is null)
				key = new Client.Key(nameSpace, setName, Value.AsNull);
			else
                key = new Client.Key(nameSpace, setName, Value.Get(primaryKey));

            return key;
        }

        /// <summary>
        /// Dummy class
        /// </summary>
        sealed class Numeric { }

        public static int GetStableHashCode(this object obj)
        {
            if (obj == null) return 0;
            
            if (obj.GetType().IsPrimitive) return GetStableHashCode(obj.ToString(), typeof(Numeric));

            switch (obj) 
            {
                case string str:
                    return GetStableHashCode(str);
                case AValue av:
                    return GetStableHashCode(av);
                case Client.Key key:
                    return GetStableHashCode(key);
                case Client.Value value:
                    return GetStableHashCode(value);
                case DateTime dt:
                    return GetStableHashCode(dt);
                case DateTimeOffset dto:
                    return GetStableHashCode(dto);
                case TimeSpan ts:
                    return GetStableHashCode(ts);
                case Guid gs:
                    return GetStableHashCode(gs.ToString());
                case byte[] ba:
                    return GetStableHashCode(ba);
                case string[] sa: 
                    return GetStableHashCode(sa);
                case JObject jo:
                    return GetStableHashCode(jo.ToString());
                case JArray ja:
                    return GetStableHashCode(ja.ToString());
                case JToken jo:
                    return GetStableHashCode(jo.ToString());
                default:
                    break;
            }

            var objType = obj.GetType();

            if (objType.IsValueType)
            {
                if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var hasvalueProp = objType.GetProperty("HasValue")?.GetValue(obj, null);

                    if (hasvalueProp != null && (bool)hasvalueProp)
                    {
                        var valuePropValue = objType.GetProperty("Value").GetValue(obj, null);

                        return GetStableHashCode(valuePropValue);
                    }
                }

                //return GetStableHashCode(obj.ToString());
            }

            return obj.GetHashCode();
        }

        public static int GetStableHashCode(this DateTime dateTime)
        {
           return GetStableHashCode(dateTime.ToString(DateTimeFormat), typeof(DateTime));
        }

        public static int GetStableHashCode(this DateTimeOffset dateTimeOffset)
        {
            return GetStableHashCode(dateTimeOffset.ToString(DateTimeOffsetFormat), typeof(DateTime));
        }

        public static int GetStableHashCode(this TimeSpan timeSpan)
        {
            return GetStableHashCode(timeSpan.ToString(TimeSpanFormat), typeof(TimeSpan));
        }

        public static int GetStableHashCode(this AValue aValue)
        {
            if (aValue is null) return 0;
            return GetStableHashCode(aValue.Value);
        }

        public static int GetStableHashCode(this Client.Key key)
        {
            if (key is null) return 0;
            return  key.userKey is null
                        ? GetStableHashCode(key.digest)
                        : GetStableHashCode(key.userKey);
        }

        public static int GetStableHashCode(this Client.Value value)
        {
            if (value is null) return 0;
            return GetStableHashCode(value.Object);
        }

        public static int GetStableHashCode(this byte[] ba)
        {
            if (ba is null) return 0;
            return GetStableHashCode(ByteArrayToString(ba));
        }

        public static int GetStableHashCode(this string[] sa)
        {
            if (sa is null) return 0;
            return GetStableHashCode(string.Concat(sa));
        }

        public static int GetStableHashCode(this string str)
        {
            return GetStableHashCode(str, typeof(string));
        }

        public static int GetStableHashCode(string str, Type dataType)
        {
         
            str = string.Concat(dataType.Name, "|", str);

            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }


        public static bool IsInt(Type checkType) => checkType == typeof(long)
                                                        || checkType == typeof(ulong)
                                                        || checkType == typeof(int)
                                                        || checkType == typeof(uint)
                                                        || checkType == typeof(short)
                                                        || checkType == typeof(ushort);

        public static bool IsFloat(Type checkType) => checkType == typeof(double)
                                                        || checkType == typeof(float)
                                                        || checkType == typeof(decimal);

        public static bool IsNumeric(Type checkType) => checkType == typeof(long)
                                                        || checkType == typeof(double)
                                                        || checkType == typeof(ulong)
                                                        || checkType == typeof(float)
                                                        || checkType == typeof(decimal)
                                                        || checkType == typeof(int)
                                                        || checkType == typeof(uint)
                                                        || checkType == typeof(short)
                                                        || checkType == typeof(ushort)
                                                        || checkType == typeof(byte)
                                                        || checkType == typeof(sbyte);

        public static bool IsJson(Type checkType) => checkType == typeof(JObject)
                                                        || checkType == typeof(JToken)
                                                        || checkType == typeof(JArray)
                                                        || checkType == typeof(JsonDocument);

        public static bool IsJsonDoc(Type checkType) => checkType == typeof(JObject)
                                                        || checkType == typeof(JsonDocument);

        public static bool IsGeoJson(Type checkType) => GeoJSONHelpers.IsGeoValue(checkType);

        public static string ToLiteral(string input)
        {
            StringBuilder literal = new StringBuilder(input.Length + 2);
            literal.Append('"');
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        literal.Append(c);
                        // ASCII printable character
                        /*if (c >= 0x20 && c <= 0x7e)
                        {
                            literal.Append(c);
                            // As UTF16 escaped character
                        }
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((int)c).ToString("x4"));
                        }*/
                        break;
                }
            }
            literal.Append('"');
            return literal.ToString();
        }

        public static byte[] Encrypt(string value)
        {
            byte[] array = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(array);
            byte[] second = System.Security.Cryptography.ProtectedData.Protect(Encoding.UTF8.GetBytes(value), array, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return array.Concat(second).ToArray();
        }

        public static string Decrypt(byte[] encrypted)
        {
            byte[] optionalEntropy = encrypted.Take(32).ToArray();
            byte[] encryptedData = encrypted.Skip(32).ToArray();
            return Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, optionalEntropy, System.Security.Cryptography.DataProtectionScope.CurrentUser));
        }
    }
}
