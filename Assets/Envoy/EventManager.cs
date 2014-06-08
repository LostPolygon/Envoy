using System;
using System.Collections.Generic;
using System.Text;
using LostPolygon.Envoy.Internal;
using UnityEngine;

namespace LostPolygon.Envoy {
    /// <summary>
    /// The main facade class of Envoy. Incorporates a message bus and a service locator.
    /// </summary>
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
        }

        private readonly Dictionary<Type, EventInfo> _eventsLookup = new Dictionary<Type, EventInfo>();
        private readonly Dictionary<ResponderTypeInfo, Delegate> _responders = new Dictionary<ResponderTypeInfo, Delegate>();
        private readonly List<EnvoyEventBase> _currentEvents = new List<EnvoyEventBase>();

        #region TArgument type responders

        /// <summary>
        /// Adds the responder from the resource locator.
        /// </summary>
        /// <param name="handler">
        /// The responder delegate to add.
        /// </param>
        /// <typeparam name="TArgument">
        /// The type of argument that will be passed to the responder.
        /// </typeparam>
        /// <typeparam name="TReturn">
        /// The type of resource being requested.
        /// </typeparam>
        public void AddResponder<TArgument, TReturn>(Func<TArgument, TReturn> handler) {
            AddResponderInternal(typeof(TReturn), typeof(TArgument), handler);
        }

        /// <summary>
        /// Removes the responder from the resource locator.
        /// </summary>
        /// <param name="handler">
        /// The responder delegate to remove.
        /// </param>
        /// <typeparam name="TArgument">
        /// The type of argument that will be passed to the responder.
        /// </typeparam>
        /// <typeparam name="TReturn">
        /// The type of resource being requested.
        /// </typeparam>
        public void RemoveResponder<TArgument, TReturn>(Func<TArgument, TReturn> handler) {
            RemoveResponderInternal(typeof(TReturn), typeof(TArgument), handler);
        }

        /// <summary>
        /// Requests a resource of type <typeparamref name="TReturn"/> with argument of type <typeparamref name="TArgument"/>.
        /// </summary>
        /// <param name="argument">
        /// The argument that will be passed to the responder.
        /// </param>
        /// <typeparam name="TArgument">
        /// The type of argument that will be passed to the responder.
        /// </typeparam>
        /// <typeparam name="TReturn">
        /// The type of resource being requested.
        /// </typeparam>
        /// <returns>
        /// The instance of <see cref="TReturn"/>.
        /// </returns>
        /// <exception cref="ResponderNotFoundException">
        /// Thrown if not responders capable of returning <typeparamref name="TReturn"/> (given parameter of type <typeparamref name="TArgument"/>) was registered at the moment of call.
        /// </exception>
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

        #endregion

        #region No argument responder


        /// <summary>
        /// Adds the responder from the resource locator.
        /// </summary>
        /// <param name="handler">
        /// The responder delegate to add.
        /// </param>
        /// <typeparam name="TReturn">
        /// The type of resource being requested.
        /// </typeparam>
        public void AddResponder<TReturn>(Func<TReturn> handler) {
            AddResponderInternal(typeof(TReturn), null, handler);
        }

        /// <summary>
        /// Removes the responder from the resource locator.
        /// </summary>
        /// <param name="handler">
        /// The responder delegate to remove.
        /// </param>
        /// <typeparam name="TReturn">
        /// The type of resource being requested.
        /// </typeparam>
        public void RemoveResponder<TReturn>(Func<TReturn> handler) {
            RemoveResponderInternal(typeof(TReturn), null, handler);
        }

        /// <summary>
        /// Requests a resource of type <typeparamref name="TReturn"/>.
        /// </summary>
        /// <typeparam name="TReturn">
        /// The type of resource being requested.
        /// </typeparam>
        /// <returns>
        /// The instance of <see cref="TReturn"/>.
        /// </returns>
        /// <exception cref="ResponderNotFoundException">
        /// Thrown if not responders capable of returning <typeparamref name="TReturn"/> was registered at the moment of call.
        /// </exception>
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

        #endregion

        #region No argument listener

        /// <summary>
        /// Adds an event listener.
        /// </summary>
        /// <param name="handler">
        /// The event listener delegate.
        /// </param>
        /// <typeparam name="T">
        /// The type of event listener is listening for.
        /// </typeparam>
        public void AddListener<T>(Action handler) where T : EventData {
            AddListenerInternal<T>(handler, args => handler());
        }

        /// <summary>
        /// Removes an event listener.
        /// </summary>
        /// <param name="handler">
        /// The event listener delegate.
        /// </param>
        /// <typeparam name="T">
        /// The type of event listener is listening for.
        /// </typeparam>
        public void RemoveListener<T>(Action handler) where T : EventData {
            RemoveListenerInternal<T>(handler);
        }

        /// <summary>
        /// Dispatches the event invocation.
        /// </summary>
        /// <param name="dispatchType">
        /// The event dispatch type.
        /// </param>
        /// <typeparam name="T">
        /// The type of event to dispatch.
        /// </typeparam>
        public void Dispatch<T>(EventDispatchType dispatchType = EventDispatchType.Default) where T : EventData {
            DispatchInternal<T>(null, dispatchType);
        }

        #endregion

        #region EventData type listener

        /// <summary>
        /// Adds an event listener.
        /// </summary>
        /// <param name="handler">
        /// The event listener delegate.
        /// </param>
        /// <typeparam name="T">
        /// The type of event listener is listening for.
        /// </typeparam>
        public void AddListener<T>(EnvoyEventHandler<T> handler) where T : EventData {
            AddListenerInternal<T>(handler, args => handler((T) args));
        }

        /// <summary>
        /// Removes an event listener.
        /// </summary>
        /// <param name="handler">
        /// The event listener delegate.
        /// </param>
        /// <typeparam name="T">
        /// The type of event listener is listening for.
        /// </typeparam>
        public void RemoveListener<T>(EnvoyEventHandler<T> handler) where T : EventData {
            RemoveListenerInternal<T>(handler);
        }

        /// <summary>
        /// Dispatches the event invocation.
        /// </summary>
        /// <param name="eventData">
        /// The arguments passed to the listeners.
        /// </param>
        /// <param name="dispatchType">
        /// The event dispatch type.
        /// </param>
        /// <typeparam name="T">
        /// The type of event to dispatch.
        /// </typeparam>
        public void Dispatch<T>(T eventData, EventDispatchType dispatchType = EventDispatchType.Default) where T : EventData {
            DispatchInternal(eventData, dispatchType);
        }

        #endregion

        #region Responders internal methods

        private void AddResponderInternal(Type typeReturn, Type typeArgument, Delegate handler) {
            ResponderTypeInfo typeInfo = new ResponderTypeInfo(typeReturn, typeArgument);
            if (_responders.ContainsKey(typeInfo)) {
                Debug.LogError(
                    string.Format(
                        "An attempt to attach multiple responders with argument of type '{0}' and return type '{1}' was detected. " +
                        "Only suitable responder can registered at the same time",
                        typeArgument, 
                        typeReturn));
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
                _currentEvents.Remove(eventInfo.EnvoyEvent);
            }
        }

        #endregion

        #region Helper methods

        private EventInfo GetEvent<T>() {
            return GetEvent<T, EventInfo>(_eventsLookup, CreateEvent<T>, newEvent => _currentEvents.Add(newEvent.EnvoyEvent));
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
            for (int i = 0, count = _currentEvents.Count; i < count; i++) {
                _currentEvents[i].DispatchDeferred();
            }
        }

        private void RemoveAllListeners() {
            StringBuilder sb = null;
            for (int i = 0, count = _currentEvents.Count; i < count; i++) {
                EnvoyEventBase envoyEvent = _currentEvents[i];
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

        /// <summary>
        /// Disposes the instance, removing all attached listeners and responders.
        /// </summary>
        public void Dispose() {
            RemoveAllListeners();
            RemoveAllResponders();
        }

        #endregion
    }
}