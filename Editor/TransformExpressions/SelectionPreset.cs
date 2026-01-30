using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    public abstract class SelectionPreset : ScriptableObject
    {
        [Tooltip("Display name for this preset in the UI. If empty, uses the asset name.")]
        [SerializeField] private string displayName = "Selection Preset";

        [Tooltip("Whether this preset is enabled and visible in the Transform Expressions window.")]
        [SerializeField] private bool enabledInWindow = true;

        [Tooltip("Whether the preset's settings are expanded in the UI.")]
        [SerializeField] private bool isExpanded = true;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public bool EnabledInWindow => enabledInWindow;
        public bool IsExpanded { get => isExpanded; set => isExpanded = value; }

        public abstract bool DrawGUI();
        public abstract void Apply(GameObject[] targets);
    }
}
