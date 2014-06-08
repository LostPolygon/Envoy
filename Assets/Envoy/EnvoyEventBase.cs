using System;

namespace LostPolygon.Envoy.Internal {
    /// <summary>
    /// The base event class.
    /// </summary>
    public abstract class EnvoyEventBase : IDisposable {
        /// <summary>
        /// Default dispatch type. Defaults to Now.
        /// </summary>
        protected EventDispatchType _defaultDispatchType;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvoyEventBase"/> class.
        /// </summary>
        /// <param name="defaultDispatchType">
        /// The default dispatch type.
        /// </param>
        protected EnvoyEventBase(EventDispatchType defaultDispatchType = EventDispatchType.Default) {
            _defaultDispatchType = defaultDispatchType == EventDispatchType.Default ?
                                   EventDispatchType.Now :
                                   defaultDispatchType;
        }

        /// <summary>
        /// Gets the multicast delegate event instance.
        /// </summary>
        public abstract EnvoyEventHandler Event { get; }

        /// <summary>
        /// Initiates dispatch of events that were dispatched with a deferred dispatch type.
        /// </summary>
        public abstract void DispatchDeferred();

        /// <summary>
        /// Removes all listeners currently attached..
        /// </summary>
        /// <returns>
        /// The number of listeners that were removed.
        /// </returns>
        public abstract int RemoveAllListeners();

        /// <summary>
        /// Disposes the event, removing all listeners currently attached.
        /// </summary>
        public void Dispose() {
            RemoveAllListeners();
        }
    }
}