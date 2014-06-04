using System;

namespace LostPolygon.Envoy {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DefaultDispatchTypeAttribute : Attribute {
        public DefaultDispatchTypeAttribute(EventDispatchType dispatchType) {
            DispatchType = dispatchType;
        }

        public EventDispatchType DispatchType { get; private set; }
    }
}