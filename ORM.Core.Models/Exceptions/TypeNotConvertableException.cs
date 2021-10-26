using System;

namespace ORM.Core.Models.Exceptions
{
    public class TypeNotConvertableException : OrmException
    {
        public TypeNotConvertableException(string message) : base(message)
        {
        }

        public TypeNotConvertableException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}