using System;

namespace LostPolygon.Envoy {
    /// <summary>
    /// An exception that is thrown when no suitable responder was found on EventManager.Request call.
    /// </summary>
    public class ResponderNotFoundException : Exception {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponderNotFoundException"/> class.
        /// </summary>
        /// <param name="argumentType">
        /// The type of argument passed to the responder.
        /// </param>
        /// <param name="returnType">
        /// The type of resource being requested.
        /// </param>
        public ResponderNotFoundException(Type argumentType, Type returnType) :
            base(
                string.Format(
                    "Can't find any responder with argument of type '{0}' and return type '{1}'",
                    argumentType == null ? "null" : argumentType.ToString(),
                    returnType == null ? "null" : returnType.ToString())) {
        }
    }
}