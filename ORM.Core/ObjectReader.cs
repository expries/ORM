using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using ORM.Core.Interfaces;

namespace ORM.Core
{
    public class ObjectReader<T> : IEnumerable<T>, IEnumerable
    {
        private IEnumerator<T>? _enumerator;
        
        public ObjectReader(IDataReader reader, ILazyLoader lazyLoader)
        {
            _enumerator = new RowReader<T>(reader, lazyLoader);
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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

        public static implicit operator T(ObjectReader<T> instance)
        {
            using var rowReader = instance.GetEnumerator();
            rowReader.MoveNext();
            return rowReader.Current;
        }
    }
}