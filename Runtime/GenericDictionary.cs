using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mactinite.ToolboxCommons
{
    [System.Serializable]
    public class GenericDictionary
    {
        private Dictionary<string, object> headers;

        public GenericDictionary()
        {
            headers = new Dictionary<string, object>();
        }

        public bool TryGetValue<T>(string dataID, out T data) where T : struct
        {
            data = default(T);
            if (headers.TryGetValue(dataID, out object headerValue))
            {
                data = (T)headerValue;
                return true;
            }
            return false;
        }

        public T? GetHeaderValue<T>(string header) where T : struct
        {
            object headerValue = null;
            if (headers.TryGetValue(header, out headerValue))
            {
                return (T)headerValue;
            }

            return null;
        }

        public object GetHeaderValue(string header)
        {
            object headerValue = null;
            if (headers.TryGetValue(header, out headerValue))
            {
                return headerValue;
            }

            return headerValue;
        }

        public void SetHeaderValue(string header, object value)
        {
            if (headers.ContainsValue(header))
            {
                headers[header] = value;
            }
            else
            {
                headers.Add(header, value);
            }
        }
    }
}
