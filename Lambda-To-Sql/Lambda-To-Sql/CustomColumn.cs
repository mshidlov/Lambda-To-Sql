using System;
using System.Runtime.CompilerServices;
namespace Lambda_To_Sql
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class CustomColumn : Attribute
    {
        public CustomColumn([CallerLineNumber] int order = 0)
        {
            Order = order;
        }
        public int Order { get; private set; }
        public bool Primary { get; set; }
    }
}
