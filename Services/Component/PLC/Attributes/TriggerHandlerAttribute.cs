using System;

namespace QA.Business.Component.PLC.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class TriggerHandlerAttribute : Attribute
    {
        private bool _isTriggerSign;
        public TriggerHandlerAttribute(bool isTriggerSign)
        {
            _isTriggerSign = isTriggerSign;
        }

        public bool IsTriggerSign
        {
            get { return _isTriggerSign; }
        }
    }
}
