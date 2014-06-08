namespace LostPolygon.Envoy {
    /// <summary>
    /// Specifies when the event will be actually dispatched.
    /// </summary>
    public enum EventDispatchType {
        /// <summary>
        /// Default value, same as Now.
        /// </summary>
        Default,

        /// <summary>
        /// Event will be dispatched immediately.
        /// </summary>
        Now,

        /// <summary>
        /// Event will be dispatched on the next MonoBehaviour.Update().
        /// </summary>
        NextFrame
    }
}