using System;

namespace ORM.Core.Models.Exceptions
{
    public class ObjectMappingException : Exception
    {
        public ObjectMappingException(string message) : base(message)
        {
        }

        public ObjectMappingException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}