using System;

namespace LostPolygon.Envoy {
    public class ResponderNotFoundException : Exception {
        public ResponderNotFoundException(Type argumentType, Type returnType) :
            base(string.Format("Can't find any responder with argument of type '{0}' and return type '{1}'",
                argumentType == null ? "null" : argumentType.ToString(),
                returnType == null ? "null" : returnType.ToString())) {
        }
    }
}