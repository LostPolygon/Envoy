using System;

namespace LostPolygon.Envoy.Internal {
    public abstract class EnvoyEventBase {
        protected EventDispatchType _defaultDispatchType;

        protected EnvoyEventBase(EventDispatchType defaultDispatchType = EventDispatchType.Default) {
            _defaultDispatchType = defaultDispatchType == EventDispatchType.Default ?
                                   EventDispatchType.Now :
                                   defaultDispatchType;
        }

        public abstract Delegate Event { get; }
        public abstract int RemoveAllListeners();
        public abstract void DispatchDeferred();

        public void Dispose() {
            RemoveAllListeners();
        }
    }
}