using System;

namespace ORM.Infrastructure.Exceptions
{
    public class TypeNotConvertableException : Exception
    {
        public TypeNotConvertableException(string message) : base(message)
        {
        }

        public TypeNotConvertableException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}