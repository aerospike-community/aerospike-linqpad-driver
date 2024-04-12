using Aerospike.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    public static class AValueHelper
    {
        public static AValue ToAValue(this Bin bin) => new AValue(bin);
        public static AValue ToAValue(this Value value, string bin = null, string fld = null)
                                    => new AValue(value, bin ?? "Value", fld ?? "Value");        
        public static APrimaryKey ToAPrimaryKey(this Key key) => new APrimaryKey(key);

        public static AValue ToAValue<T>(this Nullable<T> value, string bin = null, string fld = null)
                                where T : struct
        {
            if(value.HasValue)
                return ToAValue(value.Value, bin, fld);
            return AValue.Empty;
        }
                                
		public static AValue ToAValue(this object value, string bin = null, string fld = null)
                                => new AValue(value, bin ?? "Object", fld ??"Value");

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
        

    }
}
