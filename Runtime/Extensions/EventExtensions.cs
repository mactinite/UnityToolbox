using UnityEngine;
using UnityEngine.Events;

namespace mactinite.ToolboxCommons.Extensions
{
    public class EventExtensions
    {

    }
    
    [System.Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    [System.Serializable]
    public class FloatUnityEvent : UnityEvent<float> { }

    [System.Serializable]
    public class StringUnityEvent : UnityEvent<string> { }

    [System.Serializable]
    public class Vector2UnityEvent : UnityEvent<Vector2> { }

    [System.Serializable]
    public class Vector3UnityEvent : UnityEvent<Vector3> { }
}