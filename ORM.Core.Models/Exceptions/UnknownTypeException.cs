using System;

namespace ORM.Core.Models.Exceptions
{
    public class UnknownTypeException : OrmException
    {
        public UnknownTypeException(string message) : base(message)
        {
        }

        public UnknownTypeException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}