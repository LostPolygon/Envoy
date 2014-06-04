using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LostPolygon.Envoy.Internal {
    public sealed class EnvoyEvent : EnvoyEventBase {
        private readonly Queue<EventData> _deferredDispatchData = new Queue<EventData>();
        private event EnvoyEventHandler _event;

        public override EnvoyEventHandler Event {
            get {
                return _event;
            }
        }

        public EnvoyEvent(EventDispatchType defaultDispatchType = EventDispatchType.Default)
            : base(defaultDispatchType) {
        }

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

        public override void DispatchDeferred() {
            int count = _deferredDispatchData.Count;
            if (count == 0)
                return;

            if (_event != null) {
                for (int i = 0; i < count; i++) {
                    _event(_deferredDispatchData.Dequeue());
                }
            }

            _deferredDispatchData.Clear();
        }

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

        public static EnvoyEvent operator +(EnvoyEvent @event, EnvoyEventHandler handler) {
            @event._event += handler;
            return @event;
        }

        public static EnvoyEvent operator -(EnvoyEvent @event, EnvoyEventHandler handler) {
            @event._event -= handler;
            return @event;
        }
    }

}