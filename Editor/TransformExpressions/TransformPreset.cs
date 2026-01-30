using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    public abstract class TransformPreset : ScriptableObject
    {
        [Tooltip("Display name for this preset in the UI. If empty, uses the asset name.")]
        [SerializeField] private string displayName = "Preset";

        [Tooltip("Whether this preset is enabled and visible in the Transform Expressions window.")]
        [SerializeField] private bool enabledInWindow = true;

        // Per-asset UI state can live here too if you want it serialized.
        [Tooltip("Whether the preset's settings are expanded in the UI.")]
        [SerializeField] private bool isExpanded = true;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public bool EnabledInWindow => enabledInWindow;
        public bool IsExpanded { get => isExpanded; set => isExpanded = value; }

        /// <summary>Draw preset GUI. Return true if settings changed.</summary>
        public abstract bool DrawGUI(PresetContext ctx);

        /// <summary>Apply preset to targets. Window handles Undo.</summary>
        public abstract void Apply(PresetContext ctx, Transform[] targets);
    }
}
