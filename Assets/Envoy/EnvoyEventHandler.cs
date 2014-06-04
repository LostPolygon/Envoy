namespace LostPolygon.Envoy.Internal {
    public delegate void EnvoyEventHandler<in TEventData>(TEventData e) where TEventData : EventData;

    public delegate void EnvoyEventHandler(EventData e);
}
