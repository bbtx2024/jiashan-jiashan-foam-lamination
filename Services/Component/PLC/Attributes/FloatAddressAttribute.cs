using System;

namespace QA.Business.Component.PLC.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class FloatAddressAttribute : Attribute
    {
        public FloatAddressAttribute(bool isFloatAddress)
        {
            IsFloatAddress = isFloatAddress;
        }

        public bool IsFloatAddress { get; }
    }
}
