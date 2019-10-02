using System;
using UnityEngine;

namespace Wrj
{ 
    public class CustomLogTextUpdate : MonoBehaviour
    {
        private TMPro.TextMeshProUGUI tmpro;
        private UnityEngine.UI.Text uiText;
        private TextMesh textMesh;

        void Awake()
        {
            tmpro = GetComponent<TMPro.TextMeshProUGUI>();
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
