using System;

namespace LostPolygon.Envoy.Internal {
    public abstract class EnvoyEventBase : IDisposable {
        protected EventDispatchType _defaultDispatchType;

        protected EnvoyEventBase(EventDispatchType defaultDispatchType = EventDispatchType.Default) {
            _defaultDispatchType = defaultDispatchType == EventDispatchType.Default ?
                                   EventDispatchType.Now :
                                   defaultDispatchType;
        }

        public abstract EnvoyEventHandler Event { get; }
        public abstract void DispatchDeferred();
        public abstract int RemoveAllListeners();

        public void Dispose() {
            RemoveAllListeners();
        }
    }
}