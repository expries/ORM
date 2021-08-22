using System;

namespace ORM.Infrastructure.Attributes
{
    public class MaxLengthAttribute : Attribute
    {
        public int Length { get; set; }
        
        public MaxLengthAttribute(int length)
        {
            Length = length;
        }
    }
}