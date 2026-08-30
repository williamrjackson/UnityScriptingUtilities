#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wrj
{
    /// <summary>
    /// Captures serialized state for existing components in all loaded scenes and
    /// reapplies it after Play mode exits.
    /// </summary>
    [InitializeOnLoad]
    public static class StopAndPersistChanges
    {
        const string SnapshotPath = "Library/StopAndPersistChanges.json";

        [Serializable]
        sealed class ComponentSnapshot
        {
            public string globalObjectId;
            public string scenePath;
            public string json;
        }

        [Serializable]
        sealed class GameObjectSnapshot
        {
            public string globalObjectId;
            public string scenePath;
            public string name;
            public string tag;
            public int layer;
            public bool active;
        }

        [Serializable]
        sealed class SnapshotCollection
        {
            public List<ComponentSnapshot> components = new List<ComponentSnapshot>();
            public List<GameObjectSnapshot> gameObjects = new List<GameObjectSnapshot>();
        }

        static StopAndPersistChanges()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Stop && Persist Scene Changes", priority = 2000)]
        static void Capture()
        {
            var snapshot = new SnapshotCollection();

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (Transform transform in transforms)
                    {
                        GameObject gameObject = transform.gameObject;
                        GlobalObjectId gameObjectId = GlobalObjectId
                            .GetGlobalObjectIdSlow(gameObject);
                        snapshot.gameObjects.Add(new GameObjectSnapshot
                        {
                            globalObjectId = gameObjectId.ToString(),
                            scenePath = scene.path,
                            name = gameObject.name,
                            tag = gameObject.tag,
                            layer = gameObject.layer,
                            active = gameObject.activeSelf
                        });
                    }

                    Component[] components = root.GetComponentsInChildren<Component>(true);
                    foreach (Component component in components)
                    {
                        if (component == null ||
                            (component.hideFlags & HideFlags.DontSave) != 0)
                            continue;

                        GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(component);
                        snapshot.components.Add(new ComponentSnapshot
                        {
                            globalObjectId = id.ToString(),
                            scenePath = scene.path,
                            json = EditorJsonUtility.ToJson(component)
                        });
                    }
                }
            }

            File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snapshot));
            Debug.Log($"Captured {snapshot.gameObjects.Count} GameObjects and " +
                $"{snapshot.components.Count} components. " +
                "They will be restored after Play mode stops.");
            // Stop Play mode
            EditorApplication.isPlaying = false;
        }

        [MenuItem("Tools/Stop && Persist Scene Changes", true)]
        static bool ValidateCapture()
        {
            return EditorApplication.isPlaying && !EditorApplication.isCompiling;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !File.Exists(SnapshotPath))
                return;

            string json = File.ReadAllText(SnapshotPath);
            File.Delete(SnapshotPath);
            SnapshotCollection snapshot = JsonUtility.FromJson<SnapshotCollection>(json);
            if (snapshot == null || snapshot.components == null)
                return;

            var targets = new List<Component>();
            var states = new List<ComponentSnapshot>();
            var gameObjectTargets = new List<GameObject>();
            var gameObjectStates = new List<GameObjectSnapshot>();
            var changedScenes = new HashSet<string>();

            foreach (GameObjectSnapshot saved in snapshot.gameObjects)
            {
                if (!GlobalObjectId.TryParse(saved.globalObjectId, out GlobalObjectId id))
                    continue;

                GameObject target = GlobalObjectId
                    .GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                if (target == null)
                    continue;

                gameObjectTargets.Add(target);
                gameObjectStates.Add(saved);
                if (!string.IsNullOrEmpty(saved.scenePath))
                    changedScenes.Add(saved.scenePath);
            }

            foreach (ComponentSnapshot saved in snapshot.components)
            {
                if (!GlobalObjectId.TryParse(saved.globalObjectId, out GlobalObjectId id))
                    continue;

                Component target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Component;
                if (target == null)
                    continue;

                targets.Add(target);
                states.Add(saved);
                if (!string.IsNullOrEmpty(saved.scenePath))
                    changedScenes.Add(saved.scenePath);
            }

            Undo.SetCurrentGroupName("Persist Play Mode Changes");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RecordObjects(gameObjectTargets.ToArray(), "Persist Play Mode Changes");
            Undo.RecordObjects(targets.ToArray(), "Persist Play Mode Changes");

            for (int i = 0; i < gameObjectTargets.Count; i++)
            {
                GameObject target = gameObjectTargets[i];
                GameObjectSnapshot saved = gameObjectStates[i];
                target.name = saved.name;
                target.tag = saved.tag;
                target.layer = saved.layer;
                target.SetActive(saved.active);
                EditorUtility.SetDirty(target);
            }

            for (int i = 0; i < targets.Count; i++)
            {
                EditorJsonUtility.FromJsonOverwrite(states[i].json, targets[i]);
                EditorUtility.SetDirty(targets[i]);
            }

            Undo.CollapseUndoOperations(undoGroup);
            foreach (string scenePath in changedScenes)
            {
                Scene scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"Restored Play mode changes on {gameObjectTargets.Count} " +
                $"GameObjects and {targets.Count} existing components. " +
                "Save the affected scenes to keep them.");
        }
    }
}
#endif