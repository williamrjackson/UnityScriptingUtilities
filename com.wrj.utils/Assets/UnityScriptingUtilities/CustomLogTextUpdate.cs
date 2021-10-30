using System;
using UnityEngine;

namespace Wrj
{ 
    public class CustomLogTextUpdate : MonoBehaviour
    {
        private TMPro.TextMeshPro tmpro = null;
        private UnityEngine.UI.Text uiText = null;
        private TextMesh textMesh = null;

        void Awake()
        {
            tmpro = GetComponent<TMPro.TextMeshPro>();
            uiText = GetComponent<UnityEngine.UI.Text>();
            textMesh = GetComponent<TextMesh>();
            CustomLog.OnLogUpdate += LogUpdate;
        }

        private void LogUpdate(string msg)
        {
            if (tmpro != null)
            {
                tmpro.text = msg;
            }
            if (uiText != null)
            {
                uiText.text = msg;
            }
            if (textMesh != null)
            {
                textMesh.text = msg;
            }
        }
    }
}
