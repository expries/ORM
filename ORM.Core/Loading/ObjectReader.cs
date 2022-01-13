using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using ORM.Core.Interfaces;

namespace ORM.Core.Loading
{
    /// <summary>
    /// Maps the result rows of a data reader to objects of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectReader<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Row reader that turns a result row into an object of type T
        /// </summary>
        private IEnumerator<T>? _enumerator;

        public ObjectReader(IDataReader reader, ILazyLoader lazyLoader)
        {
            _enumerator = new RowReader<T>(reader, lazyLoader);
        }
        
        /// <summary>
        /// Returns an enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator of T.
        /// Only returns a value once as database results can only be loaded once.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerator<T> GetEnumerator()
        {
            if (_enumerator is null)
            {
                throw new ArgumentException("Results for SQL statements were already enumerated.");
            }

            var result = _enumerator;
            _enumerator = null;
            return result;
        }

        /// <summary>
        /// The object reader can be casted to an object of type T by only reading and mapping the first result row.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static implicit operator T(ObjectReader<T> instance)
        {
            using var rowReader = instance.GetEnumerator();
            rowReader.MoveNext();
            return rowReader.Current;
        }
    }
}