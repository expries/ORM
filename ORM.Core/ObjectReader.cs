using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace ORM.Core
{
    public class ObjectReader<T> : IEnumerable<T>, IEnumerable
    {
        private IEnumerator<T>? _enumerator;
        
        public ObjectReader(IDataReader reader)
        {
            _enumerator = new RowReader<T>(reader);
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

        public static explicit operator T(ObjectReader<T> instance)
        {
            return instance.GetEnumerator().Current;
        }
    }
}