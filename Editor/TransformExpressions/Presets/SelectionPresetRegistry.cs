using UnityEngine;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Selection Presets/Selection Preset Registry",
        fileName = "SelectionPresetRegistry")]
    public sealed class SelectionPresetRegistry : ScriptableObject
    {
        public SelectionPreset multiRenamePreset;
        public SelectionPreset renumberPreset;
        public SelectionPreset reorderHierarchyToSelectionPreset;
    }
}
