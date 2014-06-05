using System;
using System.Collections.Generic;
using System.Text;
using LostPolygon.Envoy.Internal;
using UnityEngine;

namespace LostPolygon.Envoy {
    public class EventManager : MonoBehaviour, IDisposable {
        private class EventInfo {
            public readonly Dictionary<Delegate, EnvoyEventHandler> DelegateLookup = new Dictionary<Delegate, EnvoyEventHandler>();
            public EnvoyEvent EnvoyEvent;

            public EventInfo(EnvoyEvent envoyEvent) {
                EnvoyEvent = envoyEvent;
            }
        }

        private struct ResponderTypeInfo {
            public readonly Type ArgumentType;
            public readonly Type ReturnType;

            public ResponderTypeInfo(Type returnType, Type argumentType) {
                ReturnType = returnType;
                ArgumentType = argumentType;
            }

            public bool Equals(ResponderTypeInfo other) {
                return ArgumentType == other.ArgumentType && ReturnType == other.ReturnType;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ResponderTypeInfo && Equals((ResponderTypeInfo) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return ((ArgumentType != null ? ArgumentType.GetHashCode() : 0) * 397) ^ (ReturnType != null ? ReturnType.GetHashCode() : 0);
                }
            }
        }

        private readonly Dictionary<Type, EventInfo> _eventsLookup = new Dictionary<Type, EventInfo>();
        private readonly Dictionary<ResponderTypeInfo, Delegate> _responders = new Dictionary<ResponderTypeInfo, Delegate>();
        private readonly List<EnvoyEventBase> _events = new List<EnvoyEventBase>();

        #region TArgument type responders

        public TReturn Request<TArgument, TReturn>(TArgument argument) {
            ResponderTypeInfo typeInfo = new ResponderTypeInfo(typeof(TReturn), typeof(TArgument));

            Delegate baseHandler;
            bool isFound = _responders.TryGetValue(typeInfo, out baseHandler);
            if (isFound) {
                Func<TArgument, TReturn> handler = (Func<TArgument, TReturn>) baseHandler;
                return handler(argument);
            }

            throw new ResponderNotFoundException(typeof(TReturn), typeof(TArgument));
        }

        public void AddResponder<TArgument, TReturn>(Func<TArgument, TReturn> handler) {
            AddResponderInternal(typeof(TReturn), typeof(TArgument), handler);
        }

        public void RemoveResponder<TArgument, TReturn>(Func<TArgument, TReturn> handler) {
            RemoveResponderInternal(typeof(TReturn), typeof(TArgument), handler);
        }

        #endregion

        #region No argument responder

        public TReturn Request<TReturn>() {
            ResponderTypeInfo typeInfo = new ResponderTypeInfo(typeof(TReturn), null);

            Delegate baseHandler;
            bool isFound = _responders.TryGetValue(typeInfo, out baseHandler);
            if (isFound) {
                Func<TReturn> handler = (Func<TReturn>) baseHandler;
                return handler();
            }

            throw new ResponderNotFoundException(typeof(TReturn), null);
        }

        public void AddResponder<TReturn>(Func<TReturn> handler) {
            AddResponderInternal(typeof(TReturn), null, handler);
        }

        public void RemoveResponder<TReturn>(Func<TReturn> handler) {
            RemoveResponderInternal(typeof(TReturn), null, handler);
        }

        #endregion

        #region Responders internal methods

        private void AddResponderInternal(Type typeReturn, Type typeArgument, Delegate handler) {
            ResponderTypeInfo typeInfo = new ResponderTypeInfo(typeReturn, typeArgument);
            if (_responders.ContainsKey(typeInfo)) {
                Debug.LogError(string.Format("An attempt to attach multiple responders with argument of type '{0}' and return type '{1}' was detected. " +
                                             "Only suitable responder can registered at the same time",
                    typeArgument, typeReturn));
            } else {
                _responders.Add(typeInfo, handler);
            }
        }

        private void RemoveResponderInternal(Type typeReturn, Type typeArgument, Delegate handler) {
            ResponderTypeInfo typeInfo = new ResponderTypeInfo(typeReturn, typeArgument);
            if (_responders.ContainsKey(typeInfo)) {
                _responders.Remove(typeInfo);
            }
        }

        #endregion

        #region No argument listener

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

        #region EventData type listener

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

        #region Listeners internal methods

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
            return GetEvent<T, EventInfo>(_eventsLookup, CreateEvent<T>, newEvent => _events.Add(newEvent.EnvoyEvent));
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
            Dispose();
        }

        private void DispatchDeferred() {
            for (int i = 0, count = _events.Count; i < count; i++) {
                _events[i].DispatchDeferred();
            }
        }

        private void RemoveAllListeners() {
            StringBuilder sb = null;
            for (int i = 0, count = _events.Count; i < count; i++) {
                EnvoyEventBase envoyEvent = _events[i];
                int removedCount = envoyEvent.RemoveAllListeners();
                if (removedCount > 0) {
                    if (sb == null)
                        sb = new StringBuilder("Some listeners haven't been removed manually. " +
                                               "This could lead to memory leaks. " +
                                               "Check for missing RemoveListener() calls. " +
                                               "List of event types:\n");

                    foreach (KeyValuePair<Type, EventInfo> eventInfo in _eventsLookup) {
                        if (eventInfo.Value.EnvoyEvent == envoyEvent) {
                            sb.AppendFormat("'{0}'\n", eventInfo.Key.FullName);
                        }
                    }
                }
            }

            _eventsLookup.Clear();

            if (sb != null)
                Debug.LogWarning(sb.ToString());
        }

        private void RemoveAllResponders() {
            int count = _responders.Count;
            if (count > 0) {
                StringBuilder sb = new StringBuilder("Some responders haven't been removed manually. This could lead to memory leaks. List of responders:\n");
                foreach (KeyValuePair<ResponderTypeInfo, Delegate> keyValuePair in _responders) {
                    sb.AppendFormat("Argument type: '{0}', return type: {1}\n", keyValuePair.Key.ArgumentType, keyValuePair.Key.ReturnType);
                }

                Debug.LogWarning(sb.ToString());
            }
            _responders.Clear();
        }

        #endregion

        #region IDisposable

        public void Dispose() {
            RemoveAllListeners();
            RemoveAllResponders();
        }

        #endregion
    }
}