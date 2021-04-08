using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace mactinite.ToolboxCommons
{

    [System.Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    [System.Serializable]
    public class FloatUnityEvent : UnityEvent<float> { }

    [System.Serializable]
    public class StringUnityEvent : UnityEvent<string> { }
}