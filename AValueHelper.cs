using Aerospike.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    public static class AValueHelper
    {
        public static AValue ToAValue(this Bin bin) => new AValue(bin);
        public static AValue ToAValue(this Value value, string bin = null, string fld = null)
                                    => new AValue(value, bin ?? "Value", fld ?? "Value");        
        public static APrimaryKey ToAPrimaryKey(this Key key) => new APrimaryKey(key);

		/// <summary>
		/// Converts a value to a <see cref="APrimaryKey"/>
		/// If value is a string of length 44 that begins with &apos;0x&apos;, it will be treated as a digest.
		/// </summary>
		/// <param name="value">The value to be converted to a Key.</param>
		/// <param name="nameSpace">The namespace associated with the key</param>
		/// <param name="setName">Name of the set associated with the key</param>
		/// <returns><see cref="APrimaryKey"/></returns>
		public static APrimaryKey ToAPrimaryKey(this object value, string nameSpace, string setName = null)
                        => new APrimaryKey(LPDHelpers.ToAerospikeKey(value, nameSpace, setName));

        public static AValue ToAValue<T>(this Nullable<T> value, string bin = null, string fld = null)
                                where T : struct
        {
            if(value.HasValue)
                return ToAValue(value.Value, bin, fld);
            return AValue.Empty;
        }
                                
		public static AValue ToAValue(this object value, string bin = null, string fld = null)
                                => value is AValue aValue
                                        ? aValue
                                        : new AValue(value, bin ?? "Object", fld ??"Value");

		/// <summary>
		/// Converts an <see cref="AValue"/> to an Aerospike bin expression (<see cref="Exp.Bin(string, Exp.Type)"/>).
		/// </summary>
		/// <param name="value">The <see cref="AValue"/> to convert.</param>
		/// <param name="expType">
		/// Optional: explicitly specify the bin type. If not provided, the type is automatically inferred from the value.
		/// </param>
		/// <returns>
		/// An Aerospike bin expression that references the bin associated with this <see cref="AValue"/>.
		/// </returns>
		/// <remarks>
		/// This method creates an expression that references a bin in the Aerospike record by name.
		/// Use this when building filter expressions that need to reference record bins.
		/// </remarks>
		/// <example>
		/// <code>
		/// AValue statusValue = record["status"].ToAValue();
		/// Exp filterExp = Exp.Eq(statusValue.ToExpBin(), Exp.Val("active"));
		/// </code>
		/// </example>
		/// <seealso cref="ToExpVal(AValue, MapOrder)"/>
		public static Exp ToExpBin(this AValue value, Exp.Type? expType = null)
		{
			if(value is null || value.IsEmpty)
				return Exp.Bin(value?.BinName ?? "null", Exp.Type.NIL);

			var binName = value.BinName;

			// If type not specified, try to infer it
			if(!expType.HasValue)
            {
				expType = InferExpType(value.Value);
			}

			return Exp.Bin(binName, expType.Value);
		}

		/// <summary>
		/// Infers the Aerospike expression type from a value's runtime type.
		/// </summary>
		private static Exp.Type InferExpType(object value)
		{
			return value switch
			{
				null => Exp.Type.NIL,
				string => Exp.Type.STRING,
				long or int or short or byte or sbyte or ushort or uint or ulong => Exp.Type.INT,
				double or float or decimal => Exp.Type.FLOAT,
				bool => Exp.Type.BOOL,
				byte[] => Exp.Type.BLOB,
				IList => Exp.Type.LIST,
				IDictionary => Exp.Type.MAP,
				_ when value.GetType().Name.Contains("HyperLogLog") => Exp.Type.HLL,
				_ when GeoJSONHelpers.IsGeoValue(value.GetType()) => Exp.Type.GEO,
				_ => Exp.Type.NIL
			};
		}

		/// <summary>
		/// Converts an <see cref="AValue"/> to an Aerospike expression value (<see cref="Exp"/>).
		/// </summary>
		/// <param name="value">The <see cref="AValue"/> to convert.</param>
		/// <param name="mapOrder">The default map (Dictionary) server side ordering. Defaults to key order.</param>
		/// <returns>
		/// An Aerospike expression representing the value. Returns <see cref="Exp.Nil()"/> if <paramref name="value"/> is null or empty.
		/// </returns>
		/// <remarks>
		/// This method converts the underlying <see cref="AValue.Value"/> into an Aerospike expression
		/// using the appropriate <see cref="Exp"/> overload based on the value's runtime type.
		/// 
		/// Supported types include:
		/// - Primitive types: int, long, double, float, bool, string, byte[]
		/// - Collections: IList, IDictionary
		/// - Null values
		/// 
		/// The conversion uses <see cref="Helpers.ConvertToAerospikeType(object)"/> to handle type transformations
		/// such as converting decimals to doubles, date/time values to strings or longs (depending on configuration),
		/// and collections to Aerospike CDT types.
		/// 
		/// For example:
		/// 
		/// <code>
		/// AValue nameValue = record.Name.ToAValue();
		/// Exp nameExp = nameValue.ToExpVal();
		/// 
		/// // Use in an Aerospike expression
		/// Exp filterExp = Exp.Eq(Exp.Bin("status", Exp.Type.STRING), myValue.ToExpVal());
		/// </code>
		/// </remarks>
		/// <seealso cref="AValue"/>
		/// <seealso cref="Exp"/>
		/// <seealso cref="Helpers.ConvertToAerospikeType(object)"/>
		public static Exp ToExpVal(this AValue value, MapOrder mapOrder = MapOrder.KEY_ORDERED)
        {
			if(value is null || value.IsEmpty) return Exp.Nil();

			var convertedValue = Helpers.ConvertToAerospikeType(value.Value);

			return convertedValue switch
			{
				null => Exp.Nil(),
				string s => Exp.Val(s),
				long l => Exp.Val(l),
				int i => Exp.Val((long) i),
				short sh => Exp.Val((long) sh),
				byte b => Exp.Val((long) b),
				double d => Exp.Val(d),
				float f => Exp.Val((double) f),
				bool bo => Exp.Val(bo),
				byte[] ba => Exp.Val(ba),
                DateTime dt => Exp.Val(dt),
                IList list => Exp.Val(list),
                IDictionary dict => Exp.Val(dict, mapOrder),
				_ => Exp.Val(convertedValue.ToString())
			};
		}

		/// <summary>
		/// Converts a collection of <see cref="AValue"/>s into a dictionary where the key is the AValue&apos;s Bin Name and the value is the AValue.
		/// </summary>
		/// <param name="values">
		/// A collection of <see cref="AValue"/>s
		/// </param>
		/// <returns>
		/// A dictionary where the key is the AValue&apos;s Bin Name and the value is the AValue.
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// If <paramref name="values"/> is null.
		/// </exception>
		public static IDictionary<string,AValue> ToDictionary(this IEnumerable<AValue> values)
            => new Dictionary<string,AValue>(values.Select(x => new KeyValuePair<string,AValue>(x.BinName, x)));

        /// <summary>
        /// Converts an <see cref="ARecord"/> to a collection of AValues.
        /// </summary>
        /// <param name="record">
        /// An <see cref="ARecord"/>
        /// </param>
        /// <returns>
        /// Collection of AValues that represent <paramref name="record"/>.
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// If <paramref name="record"/> is null.
        /// </exception>
        public static IEnumerable<AValue> ToAValueList(this ARecord record)
            => record.Aerospike.Bins.Select(bin => new AValue(bin));

        /// <summary>
        /// Converts an <see cref="Aerospike.Client.Record"/> to a collection of AValues.
        /// </summary>
        /// <param name="record">
        /// An <see cref="Aerospike.Client.Record"/>
        /// </param>
        /// <returns>
        /// Collection of AValues that represent <paramref name="record"/>.
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// If <paramref name="record"/> is null.
        /// </exception>
        public static IEnumerable<AValue> ToAValueList(this Record record)
            => record.bins.Select(kvp => new AValue(kvp.Value, kvp.Key, kvp.Key));

        /// <summary>
        /// This method returns only those values that exactly of type <typeparamref name="TResult"/>. 
        /// To obtain values coerce into a type, use <see cref="Convert{TResult}(IEnumerable{AValue})"/>.
        /// </summary>
        /// <typeparam name="TResult">The type each element matches</typeparam>
        /// <param name="source">A collection of <see cref="AValue"/> items</param>
        /// <returns>
        /// A new collection of underlying items that match TResult
        /// </returns>
        /// <exception cref="ArgumentNullException">if <paramref name="source"/> is null</exception>
        /// <see cref="AValue"/>
        /// <see cref="AValue.Value"/>
        /// <see cref="AValue.Convert{T}"/>
        /// <seealso cref="Cast{TResult}(IEnumerable{AValue})"/>
        /// <seealso cref="Convert{TResult}(IEnumerable{AValue})"/>
        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable<AValue> source)
        {
            if (source is IEnumerable<TResult> result)
            {
                return result;
            }

            if (source is null) throw new ArgumentNullException(nameof(source));

            return OfTypeIterator<TResult>(source);
        }

        private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable<AValue> values)
        {
            foreach (var item in values)
            {
                if (item.IsEmpty) continue;
                if (item.UnderlyingType == typeof(TResult))
                {
                    yield return (TResult)item.Value;
                }
            }
        }

        /// <summary>
        /// This method will cast each underlying value of an <see cref="AValue"/> element to <typeparamref name="TResult"/>. 
        /// To obtain values coerce into a type, use <see cref="Convert{TResult}(IEnumerable{AValue})"/>.
        /// </summary>
        /// <typeparam name="TResult">The type each element matches</typeparam>
        /// <param name="source">A collection of <see cref="AValue"/> items</param>
        /// <returns>
        /// A new collection of underlying items that have been cast to TResult
        /// </returns>
        /// <exception cref="ArgumentNullException">if <paramref name="source"/> is null</exception>
        /// <exception cref="InvalidCastException">If an element cannot be cast to TResult</exception>
        /// <see cref="AValue"/>
        /// <see cref="AValue.Value"/>
        /// <see cref="AValue.Convert{T}"/>
        /// <seealso cref="OfType{TResult}(IEnumerable{AValue})"/>
        public static IEnumerable<TResult> Cast<TResult>(this IEnumerable<AValue> source)
        {
            if (source is IEnumerable<TResult> result)
            {
                return result;
            }

            if (source is null) throw new ArgumentNullException(nameof(source));

            return CastIterator<TResult>(source);
        }

        private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable<AValue> values)
        {
            foreach (var item in values)
            {
                yield return (TResult)item.Value;
            }
        }

        /// <summary>
        /// This will convert (coerce) values into <typeparamref name="TResult"/> if possible.
        /// This will only return converted values. All other values will be ignored.
        /// </summary>
        /// <typeparam name="TResult">Coerced Type</typeparam>
        /// <param name="source">
        /// A collection of <see cref="AValue"/> items
        /// </param>
        /// <returns>
        /// A collection of converted values, if possible. 
        /// </returns>
        /// <see cref="AValue"/>
        /// <see cref="AValue.Value"/>
        /// <see cref="AValue.Convert{T}"/>
        /// <seealso cref="Cast{TResult}(IEnumerable{AValue})"/>
        /// <seealso cref="OfType{TResult}(IEnumerable{AValue})"/>
        public static IEnumerable<TResult> Convert<TResult>(this IEnumerable<AValue> source)
        {
            if (source is IEnumerable<TResult> result)
            {
                return result;
            }

            if (source is null) return Enumerable.Empty<TResult>();

            return ConvertIterator<TResult>(source);
        }

        private static IEnumerable<TResult> ConvertIterator<TResult>(IEnumerable<AValue> values)
        {
            foreach (var item in values)
            {
                if (item is null || item.IsEmpty) continue;
                var convertValue = Helpers.CastToNativeType(null, item.FldName, typeof(TResult), item.BinName, item.Value);
                if (convertValue is not null)
                    yield return item.Convert<TResult>();
            }
        }

        /// <summary>
        /// Determines if <paramref name="matchValue"/> matches based on <paramref name="matchOptions"/>.        
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="matchValue"/></typeparam>
        /// <param name="source">A collection of <see cref="AValue"/> that will be searched for <paramref name="matchValue"/></param>
        /// <param name="matchValue">
        /// The value used to determined a match based on <paramref name="matchOptions"/>.
        /// If <paramref name="matchOptions"/> is <see cref="AValue.MatchOptions.Regex"/>, this param should be a RegEx string or a <see cref="System.Text.RegularExpressions.Regex"/> instance.
        /// If this param is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match.
        /// </param>
        /// <param name="matchOptions">
        /// Matching options based on <see cref="AValue.MatchOptions"/>.
        /// </param>
        /// <returns>
        /// True if a match occurred.
        /// </returns>
        /// <seealso cref="AValue.Contains{T}(T, AValue.MatchOptions)"/>
        public static bool Contains<T>(this IEnumerable<AValue> source,
                                        T matchValue,
                                        AValue.MatchOptions matchOptions = AValue.MatchOptions.Value | AValue.MatchOptions.Equals)
                            => source?.Any(i => i.Contains(matchValue, matchOptions)) ?? false;

        /// <summary>
        /// Returns the converted value based on <typeparamref name="R"/>, if possible, only if there is a match based on <paramref name="matchValue"/> and this AValue&apos;s <see cref="AValue.Value"/>.
        /// If the matched value cannot be converted to <typeparamref name="R"/>, this will return false.
        /// A match occurs when any of the following happens:
        ///     <see cref="AValue.Value"/> is a <see cref="IEnumerable{T}"/> or <see cref="Newtonsoft.Json.Linq.JArray"/> and one of the elements matches <paramref name="matchValue"/>
        ///     <see cref="AValue.Value"/> is a <see cref="IDictionary{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///     <see cref="AValue.Value"/> is a <see cref="KeyValuePair{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///         If <paramref name="matchValue"/> is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match
        ///     <see cref="AValue.Value"/> is a <see cref="string"/> and <paramref name="matchValue"/> is contained within
        ///     Otherwise <see cref="AValue.Value"/> <see cref="AValue.Equals(Object)"/> is applied against <paramref name="matchValue"/>
        /// </summary>
        /// <typeparam name="R">
        /// The type used to convert the matched value, if possible.
        /// If found value cannot be converted, it is ignored.
        /// </typeparam>
        /// <typeparam name="T"> <paramref name="source"/>&apos;s type</typeparam>
        /// <param name="source">A collection of <see cref="AValue"/> values</param>
        /// <param name="matchValue">
        /// The value used to determine if a match occurred.
        /// </param>
        /// <param name="resultValue">
        /// Returns the converted matched value, if possible. 
        /// If a match dose not occur or the value could not be converted, this will be the default value of type <typeparamref name="R"/>. 
        /// </param>
        /// <returns>
        /// True to indicate that a match occurred and the matched value could be converted.
        /// </returns>
        /// <seealso cref="AValue"/>
        /// <seealso cref="AValue.TryGetValue{R}(object, out R)"/>
        /// <seealso cref="TryGetValue{T, R}(IEnumerable{AValue}, T, R)"/>
        /// <seealso cref="TryGetValue{T}(IEnumerable{AValue}, T, bool)"/>
        /// <seealso cref="TryGetValue{T}(IEnumerable{AValue}, T, out AValue)"/>
        public static bool TryGetValue<T,R>(this IEnumerable<AValue> source, T matchValue, out R resultValue)
        {
            foreach(var item in source) 
            {
                if(item is not null && item.TryGetValue(matchValue, out resultValue))
                    return true;
            }
            resultValue = default;
            return false;
        }

        /// <summary>
        /// Returns the converted value based on <typeparamref name="R"/>, if possible, only if there is a match based on <paramref name="matchValue"/> and this AValue&apos;s <see cref="AValue.Value"/>.
        /// If the matched value cannot be converted to <typeparamref name="R"/>, this will return false.
        /// A match occurs when any of the following happens:
        ///     <see cref="AValue.Value"/> is a <see cref="IEnumerable{T}"/> or <see cref="JArray"/> and one of the elements matches <paramref name="matchValue"/>
        ///     <see cref="AValue.Value"/> is a <see cref="IDictionary{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///     <see cref="AValue.Value"/> is a <see cref="KeyValuePair{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///         If <paramref name="matchValue"/> is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match
        ///     <see cref="AValue.Value"/> is a <see cref="string"/> and <paramref name="matchValue"/> is contained within
        ///     Otherwise <see cref="AValue.Value"/> <see cref="AValue.Equals(Object)"/> is applied against <paramref name="matchValue"/>
        /// </summary>
        /// <typeparam name="R">The type used to convert the matched value</typeparam>
        /// <typeparam name="T"> <paramref name="matchValue"/>&apos;s type</typeparam>
        /// <param name="source">
        /// A collection of <see cref="AValue"/> values.
        /// </param>
        /// <param name="matchValue">
        /// The value used to determine if a match occurred.
        /// </param>
        /// <param name="defaultValue">
        /// The default value if a match dose not occur or the value could not be converted.
        /// </param>
        /// <returns>
        /// Returns the converted matched value, if possible. 
        /// If a match dose not occur or the value could not be converted, this will return <paramref name="defaultValue"/>. 
        /// </returns>
        /// <seealso cref="AValue"/>
        /// <seealso cref="AValue.TryGetValue{R}(object, R)"/>
        /// <seealso cref="TryGetValue{T, R}(IEnumerable{AValue}, T, out R)"/>
        /// <seealso cref="TryGetValue{T}(IEnumerable{AValue}, T, bool)"/>
        /// <seealso cref="TryGetValue{T}(IEnumerable{AValue}, T, out AValue)"/>
        public static R TryGetValue<T,R>(this IEnumerable<AValue> source, T matchValue, R defaultValue = default)
                            => TryGetValue(source, matchValue, out R matchedValue) ? matchedValue : defaultValue;
        
        /// <summary>
        /// Returns <see cref="AValue"/>, only if there is a match based on <paramref name="matchValue"/> and this AValue&apos;s <see cref="AValue.Value"/>.
        /// 
        /// A match occurs when any of the following happens:
        ///     <see cref="AValue.Value"/> is a <see cref="IEnumerable{T}"/> or <see cref="JArray"/> and one of the elements matches <paramref name="matchValue"/>
        ///     <see cref="AValue.Value"/> is a <see cref="IDictionary{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///     <see cref="AValue.Value"/> is a <see cref="KeyValuePair{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///         If <paramref name="matchValue"/> is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match
        ///     <see cref="AValue.Value"/> is a <see cref="string"/> and <paramref name="matchValue"/> is contained within
        ///     Otherwise <see cref="AValue.Value"/> <see cref="AValue.Equals(Object)"/> is applied against <paramref name="matchValue"/>
        /// </summary>
        /// <typeparam name="T"> <paramref name="matchValue"/>&apos;s type</typeparam>
        /// <param name="source">
        /// A collection of <see cref="AValue"/> values.
        /// </param>
        /// <param name="matchValue">
        /// The value used to determine if a match occurred.
        /// </param>
        /// <param name="resultValue">
        /// Returns the AValue which matched <paramref name="matchValue"/>.
        /// If a match dose not occur, this will be null. 
        /// </param>
        /// <returns>
        /// True to indicate the match was found.
        /// </returns>
        /// <seealso cref="AValue.TryGetValue(object, out AValue)"/>
        /// <seealso cref="TryGetValue{T, R}(IEnumerable{AValue}, T, out R)"/>
        /// <seealso cref="TryGetValue{T, R}(IEnumerable{AValue}, T, R)"/>
        /// <seealso cref="TryGetValue{T}(IEnumerable{AValue}, T, out AValue)"/>
        public static bool TryGetValue<T>(this IEnumerable<AValue> source, T matchValue, out AValue resultValue)
                            => TryGetValue<T,AValue>(source, matchValue, out resultValue);

        /// <summary>
        /// Returns <see cref="AValue"/>, only if there is a match based on <paramref name="matchValue"/> and this AValue&apos;s <see cref="AValue.Value"/>.
        /// 
        /// A match occurs when any of the following happens:
        ///     <see cref="AValue.Value"/> is a <see cref="IEnumerable{T}"/> or <see cref="JArray"/> and one of the elements matches <paramref name="matchValue"/>
        ///     <see cref="AValue.Value"/> is a <see cref="IDictionary{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///     <see cref="AValue.Value"/> is a <see cref="KeyValuePair{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///         If <paramref name="matchValue"/> is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match
        ///     <see cref="AValue.Value"/> is a <see cref="string"/> and <paramref name="matchValue"/> is contained within
        ///     Otherwise <see cref="AValue.Value"/> <see cref="AValue.Equals(Object)"/> is applied against <paramref name="matchValue"/>
        /// </summary>
        /// <typeparam name="T"> <paramref name="matchValue"/>&apos;s type</typeparam>
        /// <param name="source">
        /// A collection of <see cref="AValue"/> values.
        /// </param>
        /// <param name="matchValue">
        /// The value used to determine if a match occurred.
        /// </param>
        /// <param name="returnEmptyAValue">
        /// If true and if a match was not found, a null AValue will be returned.
        /// </param>
        /// <returns>
        /// Returns the matched <see cref="AValue"/>. If no match found either <see cref="AValue.Empty"/>, or null is returned.
        /// </returns>
        /// <seealso cref="AValue.TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue{T, R}(IEnumerable{AValue}, T, out R)"/>
        /// <seealso cref="TryGetValue{T, R}(IEnumerable{AValue}, T, R)"/>
        /// <seealso cref="TryGetValue{T}(IEnumerable{AValue}, T, out AValue)"/>
        public static AValue TryGetValue<T>(this IEnumerable<AValue> source, T matchValue, bool returnEmptyAValue = false)
                    => TryGetValue<T,AValue>(source, matchValue, out AValue matchedValue)
                            ? matchedValue
                            : (returnEmptyAValue ? AValue.Empty : null);

        /// <summary>
        /// Finds all matching values based on <paramref name="matchValue"/> and <paramref name="matchOptions"/>.
        /// For more information see <see cref="AValue.FindAll{T}(T, AValue.MatchOptions)"/>.
        /// </summary>
        /// <typeparam name="T"><paramref name="matchValue"/>&apos;s type</typeparam>
        /// <param name="source">A collection of <see cref="AValue"/></param>
        /// <param name="matchValue">The value used to determine if a match occurred.</param>
        /// <param name="matchOptions">
        /// Matching options based on <see cref="AValue.MatchOptions"/>.
        /// </param>
        /// <returns>A collection of matched <see cref="AValue"/></returns>
        /// <seealso cref="AValue.FindAll{T}(T, AValue.MatchOptions)"/>
        /// <seealso cref="AValue.Contains{T}(T, AValue.MatchOptions)"/>
        /// <seealso cref="AValue.TryGetValue(object, bool)"/>
        /// <seealso cref="Contains{T}(IEnumerable{AValue}, T, AValue.MatchOptions)"/>
        /// <seealso cref="TryGetValue{T}(IEnumerable{AValue}, T, bool)"/>
        public static IEnumerable<AValue> FindAll<T>(this IEnumerable<AValue> source, T matchValue, AValue.MatchOptions matchOptions = AValue.MatchOptions.Value | AValue.MatchOptions.Equals)
                        => source.SelectMany(a => a.FindAll(matchValue, matchOptions));


		/// <summary>
		/// Safely executes a function against an AValue after converting
		/// the underlying value to the specified input type, when the AValue is not null.
		/// </summary>
		/// <typeparam name="TValue">
		/// The type to convert the underlying <see cref="AValue"/> value to before
		/// executing the function.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// The function return type.
		/// </typeparam>
		/// <param name="value">
		/// The <see cref="AValue"/> instance to operate on. If null, returns <c>default</c> immediately.
		/// </param>
		/// <param name="method">
		/// The function to execute against the converted value.
		/// </param>
		/// <returns>
		/// The result returned by <paramref name="method"/> when <paramref name="value"/> is not null, 
		/// conversion succeeds, and the function executes successfully; otherwise, <c>default</c>.
		/// </returns>
		/// <remarks>
		/// This is a null-safe wrapper around <see cref="AValue.Apply{TValue, TResult}(Func{TValue, TResult})"/>.
		/// It provides a convenient way to execute type-specific functions against an <see cref="AValue"/>
		/// that may be null, without requiring explicit null checks.
		/// 
		/// If <paramref name="value"/> is null, this method returns <c>default</c> without attempting
		/// to execute <paramref name="method"/>.
		/// 
		/// If <paramref name="value"/> is not null, this method delegates to 
		/// <see cref="AValue.Apply{TValue, TResult}(Func{TValue, TResult})"/>, which safely converts
		/// the underlying value and executes the function.
		/// 
		/// For example:
		/// 
		/// <code>
		/// AValue value = testns.myset.First().BinA; // May be null since bin may not exists in the record.
		/// bool startsWithA = value.TryApply&lt;string, bool&gt;(s =&gt; s.StartsWith("A"));
		/// int length = value.TryApply&lt;string, int&gt;(s =&gt; s.Length);
		/// </code>
		///
		/// Find all customers where the Name starts with "B" (regardless the bin's underlying value type or even if the bin exists in the record):
		/// <code>
		/// test.customers.Where(dt =&gt; dt.Name.TryApply&lt;string,bool&gt;(v =>&gt;v.StartsWith("B"))).Dump();
		/// </code>
		///
		/// </remarks>
		/// <seealso cref="AValue.Apply{TValue, TResult}(Func{TValue, TResult})"/>
		/// <seealso cref="AValueHelper.CanConvert{T}"/>
		/// <seealso cref="AValue.Convert{T}"/>
		public static TResult TryApply<TValue, TResult>(this AValue value, Func<TValue, TResult> method)
			=> value is null ? default : value.Apply(method);


		/// <summary>
		/// Determines whether this <see cref="AValue"/> can be converted to the specified target type.
		/// </summary>
		/// <typeparam name="T">
		/// The target type to test for conversion.
		/// </typeparam>
		/// <param name="aValue">
		/// The <see cref="AValue"/> instance to operate on. If null, returns <c>false</c> immediately.
        /// </param>
		/// <returns>
		/// <c>true</c> if the underlying value can be converted to <typeparamref name="T"/>;
		/// otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// This method is a non-throwing companion to <see cref="AValue.Convert{T}"/>.
		/// It should be used when callers need to test conversion safety before calling
		/// <see cref="AValue.Convert{T}"/>.
		/// 
		/// If <typeparamref name="T"/> is <see cref="string"/>, native scalar values such as
		/// numeric types, <see cref="bool"/>, <see cref="DateTime"/>, <see cref="DateTimeOffset"/>,
		/// <see cref="TimeSpan"/>, <see cref="Guid"/>, and enum values are considered convertible
		/// because they can be represented as strings.
		/// </remarks>
		/// <seealso cref="AValue.Convert{T}"/>
		public static bool CanConvert<T>(this AValue aValue)
		{
            if(aValue is null) return false;
            if(aValue.UnderlyingType == typeof(T))  return true;

			return Helpers.CanCastToNativeType(typeof(T), aValue.Value);
		}

	}
}
