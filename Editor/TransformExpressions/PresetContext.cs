using System;
using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    public sealed class PresetContext
    {
        public readonly Func<Transform[]> GetOrderedSelection;

        public PresetContext(Func<Transform[]> getOrderedSelection)
        {
            GetOrderedSelection = getOrderedSelection;
        }

        public void RecordUndo(Transform[] targets, string name)
        {
            if (targets == null || targets.Length == 0) return;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);
            Undo.RecordObjects(targets, name);
        }

        public Vector3 ComputeLocalCentroid(Transform[] targets)
        {
            if (targets == null || targets.Length == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var t in targets)
            {
                if (!t) continue;
                sum += t.localPosition;
                count++;
            }
            return count > 0 ? (sum / count) : Vector3.zero;
        }
    }
}
