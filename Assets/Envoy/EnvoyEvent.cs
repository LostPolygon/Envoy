using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LostPolygon.Envoy.Internal {
    /// <summary>
    /// The main Envoy event class.
    /// </summary>
    public sealed class EnvoyEvent : EnvoyEventBase {
        private readonly Queue<EventData> _deferredDispatchData = new Queue<EventData>();
        private event EnvoyEventHandler _event;

        /// <summary>
        /// Gets the multicast delegate event instance.
        /// </summary>
        public override EnvoyEventHandler Event {
            get {
                return _event;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvoyEvent"/> class.
        /// </summary>
        /// <param name="defaultDispatchType">
        /// The default event dispatch type.
        /// </param>
        public EnvoyEvent(EventDispatchType defaultDispatchType = EventDispatchType.Default)
            : base(defaultDispatchType) {
        }

        /// <summary>
        /// Dispatches the event invocation.
        /// </summary>
        /// <param name="arguments">
        /// The arguments passed to the listeners.
        /// </param>
        /// <param name="dispatchType">
        /// The event dispatch type.
        /// </param>
        public void Dispatch(EventData arguments, EventDispatchType dispatchType = EventDispatchType.Default) {
            if (dispatchType == EventDispatchType.Default) {
                dispatchType = _defaultDispatchType;
            }

            switch (dispatchType) {
                case EventDispatchType.Now:
                    if (_event == null)
                        return;

                    _event(arguments);
                    break;
                case EventDispatchType.NextFrame:
                    _deferredDispatchData.Enqueue(arguments);
                    break;
                default:
                    throw new InvalidEnumArgumentException("dispatchType", (int) dispatchType, typeof(EventDispatchType));
            }
        }

        /// <summary>
        /// Initiates dispatch of events that were dispatched with a deferred dispatch type.
        /// </summary>
        public override void DispatchDeferred() {
            int count = _deferredDispatchData.Count;
            if (count == 0)
                return;

            if (_event != null) {
                for (int i = 0; i < count; i++) {
                    _event(_deferredDispatchData.Dequeue());
                }
            }
        }

        /// <summary>
        /// Removes all listeners currently attached..
        /// </summary>
        /// <returns>
        /// The number of listeners that were removed.
        /// </returns>
        public override int RemoveAllListeners() {
            if (_event == null) {
                return 0;
            }

            Delegate[] handlers = _event.GetInvocationList();
            foreach (Delegate handler in handlers) {
                _event -= (EnvoyEventHandler) handler;
            }

            return handlers.Length;
        }

        /// <summary>
        /// Removes <paramref name="handler"/> to the event listeners.
        /// </summary>
        /// <param name="event">
        /// The event to remove the <paramref name="handler"/> to.
        /// </param>
        /// <param name="handler">
        /// The event listener delegate.
        /// </param>
        /// <returns>
        /// Event with removed event listener <paramref name="handler"/>.
        /// </returns>
        public static EnvoyEvent operator +(EnvoyEvent @event, EnvoyEventHandler handler) {
            @event._event += handler;
            return @event;
        }

        /// <summary>
        /// Adds <paramref name="handler"/> to the event listeners.
        /// </summary>
        /// <param name="event">
        /// The event to add the <paramref name="handler"/> to.
        /// </param>
        /// <param name="handler">
        /// The event listener delegate.
        /// </param>
        /// <returns>
        /// Event with added event listener <paramref name="handler"/>.
        /// </returns>
        public static EnvoyEvent operator -(EnvoyEvent @event, EnvoyEventHandler handler) {
            @event._event -= handler;
            return @event;
        }
    }

}