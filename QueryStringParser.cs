using UnityEngine.Events;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Wrj
{        
    public class QueryStringParser : MonoBehaviour
    {
        [SerializeField]
        private QueryStringCommand[] queryStrings;
        private static Dictionary<string, string> results;
        void Start()
        {
    #if UNITY_WEBGL
            string url = Application.absoluteURL;
            // Test string:
            // string url = "www.test.com/index.htm?EnableHints=true&WordList=Engagement,In,On,Oh&w=30&h=16&float=1.75";
            var splitUrl = url.Split(new[] { '?' }, 2);
            if (splitUrl.Length > 1)
            {
                var querySubstring = splitUrl[1];
                results = ParseQueryString(querySubstring);
                if (queryStrings == null) return;
                foreach (var qs in queryStrings)
                {
                    foreach (var item in results)
                    {
                        if (item.Key.ToLower() == qs.queryString.ToLower())
                        {
                            if (qs.action.GetPersistentEventCount() > 0)
                            {
                                qs.action.Invoke(item.Value);
                            }
                            if (qs.option.GetPersistentEventCount() > 0 && 
                                bool.TryParse(item.Value, out bool bResult))
                            {
                                qs.option.Invoke(bResult);
                            }
                            if (qs.integer.GetPersistentEventCount() > 0 && 
                                int.TryParse(item.Value, out int iResult))
                            {
                                qs.integer.Invoke(iResult);
                            }
                            if (qs.floatVal.GetPersistentEventCount() > 0 && 
                                float.TryParse(item.Value, out float fResult))
                            {
                                qs.floatVal.Invoke(fResult);
                            }
                        }
                    }
                }
            }
    #endif
        }
        public static string GetQueryStringValue(string key)
        {
            if (results == null || string.IsNullOrEmpty(key)) return null;
            if (results.TryGetValue(key, out string value)) return value;
            return null;
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query)) return dict;
            string[] pairs = query.Split('&');
            foreach (string pair in pairs)
            {
                if (string.IsNullOrEmpty(pair)) continue;
                string[] kv = pair.Split(new[] { '=' }, 2);
                string k = Uri.UnescapeDataString(kv[0]);
                string v = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
                if (!dict.ContainsKey(k))
                {
                    dict.Add(k, v);
                }
            }
            return dict;
        }
        
        [System.Serializable]
        public class QueryStringCommand
        {
            public string queryString;
            public UnityEvent<string> action;
            public UnityEvent<bool> option;
            public UnityEvent<int> integer;
            public UnityEvent<float> floatVal;
        }
    }
}