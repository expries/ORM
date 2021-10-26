using System;

namespace ORM.Core.Models.Exceptions
{
    public class OrmException : Exception
    {
        public OrmException(string message) : base(message)
        {
        }

        public OrmException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}