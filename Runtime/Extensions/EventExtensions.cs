namespace toolbox.Extensions
{
    // The serializable UnityEvent<T> types (IntUnityEvent, FloatUnityEvent, ...) live in the root
    // `toolbox` namespace (Runtime/EventExtensions.cs). They used to be duplicated here, which made
    // any file importing both `toolbox` and `toolbox.Extensions` fail with CS0104 (ambiguous type).
    public class EventExtensions
    {
    }
}
