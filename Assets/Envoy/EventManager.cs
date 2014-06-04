using System;
using System.Collections.Generic;
using UnityEngine;
using LostPolygon.Envoy.Internal;

namespace LostPolygon.Envoy {
    public class EventManager : MonoBehaviour, IDisposable {
        private class EventInfo {
            public readonly Dictionary<Delegate, EnvoyEventHandler> DelegateLookup = new Dictionary<Delegate, EnvoyEventHandler>();
            public EnvoyEvent EnvoyEvent;
        }

        private readonly Dictionary<Type, EventInfo> _eventDictionaryArguments = new Dictionary<Type, EventInfo>();
        private readonly List<EnvoyEventBase> _eventList = new List<EnvoyEventBase>();

        #region No arguments

        public void AddListener<T>(Action value) where T : EventData {
            AddListenerInternal<T>(value, args => value());
        }

        public void RemoveListener<T>(Action value) where T : EventData {
            RemoveListenerInternal<T>(value);
        }

        public void Dispatch<T>(EventDispatchType dispatchType = EventDispatchType.Default) where T : EventData {
            DispatchInternal<T>(null, dispatchType);
        }

        #endregion

        #region EventData type argument

        public void AddListener<T>(EnvoyEventHandler<T> value) where T : EventData {
            AddListenerInternal<T>(value, args => value((T) args));
        }

        public void RemoveListener<T>(EnvoyEventHandler<T> value) where T : EventData {
            RemoveListenerInternal<T>(value);
        }

        public void Dispatch<T>(T arguments, EventDispatchType dispatchType = EventDispatchType.Default) where T : EventData {
            DispatchInternal(arguments, dispatchType);
        }

        #endregion

        #region Internal methods

        private void DispatchInternal<T>(T arguments, EventDispatchType dispatchType) where T : EventData {
            EventInfo eventInfo = GetEvent<T>();
            eventInfo.EnvoyEvent.Dispatch(arguments, dispatchType);
        }

        private void AddListenerInternal<T>(Delegate value, EnvoyEventHandler EnvoyEventHandler) where T : EventData {
            EventInfo eventInfo = GetEvent<T>();
            if (eventInfo.DelegateLookup.ContainsKey(value)) {
                Debug.LogWarning(string.Format("An attempt to attach method {0} to an event multiple times was detected. Ignoring", typeof(T)));
                return;
            }

            eventInfo.DelegateLookup.Add(value, EnvoyEventHandler);
            eventInfo.EnvoyEvent += EnvoyEventHandler;
        }

        private void RemoveListenerInternal<T>(Delegate value) where T : EventData {
            EventInfo eventInfo = GetEvent<T>();

            EnvoyEventHandler EnvoyEventHandler;
            bool isFound = eventInfo.DelegateLookup.TryGetValue(value, out EnvoyEventHandler);
            if (isFound) {
                eventInfo.DelegateLookup.Remove(value);
                eventInfo.EnvoyEvent -= EnvoyEventHandler;
            }

            if (eventInfo.EnvoyEvent.Event == null) {
                _eventList.Remove(eventInfo.EnvoyEvent);
            }
        }

        #endregion

        #region Helper methods

        private TEvent GetEvent<TEventSource, TEvent>(Dictionary<Type, TEvent> eventDictionary,
            Func<TEvent> constructEvent,
            Action<TEvent> onEventCreated = null)
            where TEvent : class {
            Type delegateType = typeof(TEventSource);
            TEvent dataEvent;
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

        private EventInfo GetEvent<T>() {
            return GetEvent<T, EventInfo>(_eventDictionaryArguments, CreateEvent<T>, newEvent => _eventList.Add(newEvent.EnvoyEvent));
        }

        private static EventInfo CreateEvent<T>() {
            EventInfo eventInfo = new EventInfo();
            eventInfo.EnvoyEvent = new EnvoyEvent(GetEventDispatchType(typeof(T)));
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
            int count = _eventList.Count;
            for (int i = 0; i < count; i++) {
                _eventList[i].DispatchDeferred();
            }
        }

        private void RemoveAllListeners() {
            int count = _eventList.Count;
            for (int i = 0; i < count; i++) {
                int removedCount = _eventList[i].RemoveAllListeners();
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