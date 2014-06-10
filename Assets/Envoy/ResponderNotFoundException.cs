using System;

namespace LostPolygon.Envoy {
    /// <summary>
    /// An exception that is thrown when no suitable responder was found on EventManager.Request call.
    /// </summary>
    public class ResponderNotFoundException : Exception {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponderNotFoundException"/> class.
        /// </summary>
        /// <param name="returnType">
        /// The type of resource being requested.
        /// </param>
        /// <param name="argumentType">
        /// The type of argument passed to the responder.
        /// </param>
        public ResponderNotFoundException(Type returnType, Type argumentType) :
            base(
                string.Format(
                    "Can't find any responder with return type '{0}' and argument of type '{1}'",
                    returnType == null ? "null" : returnType.ToString(),
                    argumentType == null ? "null" : argumentType.ToString())) {
        }
    }
}