using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    public static class AValueHelper
    {
        /// <summary>
        /// This method returns only those underlying value of an <see cref="AValue"/> elements in <paramref name="source"/> that can be cast to type <typeparamref name="TResult"/>. 
        ///     To instead receive an exception if an element cannot be cast to type TResult, use <see cref="Cast{TResult}(IEnumerable{AValue})"/>.
        /// </summary>
        /// <typeparam name="TResult">The type each element matches</typeparam>
        /// <param name="source">A collection of <see cref="AValue"/> items</param>
        /// <returns>
        /// A new collection of underlying items that match TResult
        /// </returns>
        /// <exception cref="ArgumentNullException">if <paramref name="source"/> is null</exception>
        /// <see cref="AValue"/>
        /// <see cref="AValue.Value"/>
        /// <seealso cref="Cast{TResult}(IEnumerable{AValue})"/>
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
                if (item.UnderlyingType == typeof(TResult))
                {
                    yield return (TResult)item.Value;
                }
            }
        }

        /// <summary>
        /// This method will cast each underlying value of an <see cref="AValue"/> element to <typeparamref name="TResult"/>. 
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
    }
}
