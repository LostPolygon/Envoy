namespace LostPolygon.Envoy.Internal {
    public delegate void EnvoyEventHandler<in TGameEvent>(TGameEvent e) where TGameEvent : EventData;

    public delegate void EnvoyEventHandler(EventData e);
}
