using System;
using System.Collections.Generic;
using UnityEngine;
using LostPolygon.Envoy.Internal;

namespace LostPolygon.Envoy {
    public class EventManager : MonoBehaviour, IDisposable {
        private class EventInfo {
            public readonly Dictionary<Delegate, EnvoyEventHandler> DelegateLookup = new Dictionary<Delegate, EnvoyEventHandler>();
            public EnvoyEvent EnvoyEvent;

            public EventInfo(EnvoyEvent envoyEvent) {
                EnvoyEvent = envoyEvent;
            }
        }

        private readonly Dictionary<Type, EventInfo> _eventDictionaryArguments = new Dictionary<Type, EventInfo>();
        private readonly List<EnvoyEventBase> _events = new List<EnvoyEventBase>();

        #region No arguments

        public void AddListener<T>(Action handler) where T : EventData {
            AddListenerInternal<T>(handler, args => handler());
        }

        public void RemoveListener<T>(Action handler) where T : EventData {
            RemoveListenerInternal<T>(handler);
        }

        public void Dispatch<T>(EventDispatchType dispatchType = EventDispatchType.Default) where T : EventData {
            DispatchInternal<T>(null, dispatchType);
        }

        #endregion

        #region EventData type argument

        public void AddListener<T>(EnvoyEventHandler<T> handler) where T : EventData {
            AddListenerInternal<T>(handler, args => handler((T) args));
        }

        public void RemoveListener<T>(EnvoyEventHandler<T> handler) where T : EventData {
            RemoveListenerInternal<T>(handler);
        }

        public void Dispatch<T>(T eventData, EventDispatchType dispatchType = EventDispatchType.Default) where T : EventData {
            DispatchInternal(eventData, dispatchType);
        }

        #endregion

        #region Internal methods

        private void DispatchInternal<T>(T eventData, EventDispatchType dispatchType) where T : EventData {
            EventInfo eventInfo = GetEvent<T>();
            eventInfo.EnvoyEvent.Dispatch(eventData, dispatchType);
        }

        private void AddListenerInternal<T>(Delegate originalHandler, EnvoyEventHandler wrapperHandler) where T : EventData {
            EventInfo eventInfo = GetEvent<T>();
            if (eventInfo.DelegateLookup.ContainsKey(originalHandler)) {
                Debug.LogWarning(string.Format("An attempt to attach method {0} to an event multiple times was detected. Ignoring", typeof(T)));
                return;
            }

            eventInfo.DelegateLookup.Add(originalHandler, wrapperHandler);
            eventInfo.EnvoyEvent += wrapperHandler;
        }

        private void RemoveListenerInternal<T>(Delegate handler) where T : EventData {
            EventInfo eventInfo = GetEvent<T>();

            EnvoyEventHandler wrapperHandler;
            bool isFound = eventInfo.DelegateLookup.TryGetValue(handler, out wrapperHandler);
            if (isFound) {
                eventInfo.DelegateLookup.Remove(handler);
                eventInfo.EnvoyEvent -= wrapperHandler;
            }

            if (eventInfo.EnvoyEvent.Event == null) {
                _events.Remove(eventInfo.EnvoyEvent);
            }
        }

        #endregion

        #region Helper methods

        private EventInfo GetEvent<T>() {
            return GetEvent<T, EventInfo>(_eventDictionaryArguments, CreateEvent<T>, newEvent => _events.Add(newEvent.EnvoyEvent));
        }

        private static TEventWrapper GetEvent<TEvent, TEventWrapper>(Dictionary<Type, TEventWrapper> eventDictionary,
            Func<TEventWrapper> constructEvent,
            Action<TEventWrapper> onEventCreated = null)
            where TEventWrapper : class {
            Type delegateType = typeof(TEvent);
            TEventWrapper dataEvent;
            eventDictionary.TryGetValue(delegateType, out dataEvent);
            if (dataEvent == null) {
                dataEvent = constructEvent();
                if (onEventCreated != null) {
                    onEventCreated(dataEvent);
                }
                eventDictionary.Add(delegateType, dataEvent);
            }

            return dataEvent;
        }

        private static EventInfo CreateEvent<T>() {
            EnvoyEvent envoyEvent = new EnvoyEvent(GetEventDispatchType(typeof(T)));
            EventInfo eventInfo = new EventInfo(envoyEvent);

            return eventInfo;
        }

        private static EventDispatchType GetEventDispatchType(Type eventType) {
            DefaultDispatchTypeAttribute defaultDispatchTypeAttribute =
                (DefaultDispatchTypeAttribute) Attribute.GetCustomAttribute(eventType, typeof(DefaultDispatchTypeAttribute));

            return defaultDispatchTypeAttribute == null ? EventDispatchType.Default : defaultDispatchTypeAttribute.DispatchType;
        }

        #endregion

        #region MonoBehaviour

        private void Update() {
            DispatchDeferred();
        }

        private void OnDestroy() {
            RemoveAllListeners();
        }

        private void DispatchDeferred() {
            for (int i = 0, count = _events.Count; i < count; i++) {
                _events[i].DispatchDeferred();
            }
        }

        private void RemoveAllListeners() {
            for (int i = 0, count = _events.Count; i < count; i++) {
                int removedCount = _events[i].RemoveAllListeners();
                if (removedCount > 0) {
                    Debug.LogWarning("Some listeners haven't been removed manually. This could lead to memory leaks. Check for missing RemoveListener() calls");
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose() {
            RemoveAllListeners();
        }

        #endregion
    }
}