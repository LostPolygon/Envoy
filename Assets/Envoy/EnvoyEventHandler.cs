namespace LostPolygon.Envoy.Internal {
    /// <summary>
    /// Envoy event handler delegate template.
    /// </summary>
    /// <param name="e">
    /// Arguments instance that will be passed to the listeners.
    /// </param>
    /// <typeparam name="TEventData">
    /// Any class that inherits EventData.
    /// </typeparam>
    public delegate void EnvoyEventHandler<in TEventData>(TEventData e) where TEventData : EventData;

    /// <summary>
    /// Envoy event handler delegate template.
    /// </summary>
    /// <param name="e">
    /// Arguments instance that will be passed to the listeners.
    /// </param>
    public delegate void EnvoyEventHandler(EventData e);
}
