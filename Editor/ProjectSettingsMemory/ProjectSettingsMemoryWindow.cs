using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Wrj
{
    internal sealed class ProjectSettingsMemoryWindow : EditorWindow
    {
        private const string PreferencesKey =
            "ProjectSettingsMemory.PreferredSettings.v1";

        [Serializable]
        private sealed class SettingsPreset
        {
            public EditorSettings.NamingScheme gameObjectNamingScheme;
            public int gameObjectNamingDigits;
            public bool assetNamingUsesSpace;
            public SerializationMode assetSerializationMode;
            public LineEndingsMode lineEndingsForNewScripts;
            public string rootNamespace;
            public ApiCompatibilityLevel apiCompatibilityLevel;
            public bool asyncShaderCompilation;
            public bool prefabAutoSave;
            public bool compactYamlMappings;
            public bool configureEnterPlayMode;
            public EnterPlayModeOptions enterPlayModeOptions;
        }

        private SettingsPreset _preset;
        private Vector2 _scrollPosition;
        private bool _hasUnsavedChanges;

        [MenuItem("Tools/Project Settings Memory...")]
        private static void Open()
        {
            var window = GetWindow<ProjectSettingsMemoryWindow>();
            window.titleContent = new GUIContent("Settings Memory");
            window.minSize = new Vector2(430f, 510f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadPreferredOrCurrent();
        }

        private void OnGUI()
        {
            if (_preset == null)
                LoadPreferredOrCurrent();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Project Settings Memory", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Apply writes these values to the current project and remembers " +
                "them for the next project. API Compatibility is applied to the " +
                "active build target only.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();

            DrawNamingSettings();
            DrawSerializationSettings();
            DrawScriptingSettings();
            DrawEditorWorkflowSettings();

            if (EditorGUI.EndChangeCheck())
                _hasUnsavedChanges = true;

            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        private void DrawNamingSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Naming", EditorStyles.boldLabel);
            _preset.gameObjectNamingScheme =
                (EditorSettings.NamingScheme)EditorGUILayout.EnumPopup(
                    new GUIContent("GameObject numbering", "Naming scheme used for duplicated GameObjects."),
                    _preset.gameObjectNamingScheme);
            _preset.gameObjectNamingDigits = EditorGUILayout.IntSlider(
                new GUIContent("Numbering digits", "Minimum digits used in duplicated GameObject numbers."),
                _preset.gameObjectNamingDigits,
                1,
                5);
            _preset.assetNamingUsesSpace = EditorGUILayout.Toggle(
                new GUIContent("Space before Asset number", "For example, Material 1 instead of Material1."),
                _preset.assetNamingUsesSpace);
        }

        private void DrawSerializationSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Assets and Serialization", EditorStyles.boldLabel);
            _preset.assetSerializationMode =
                (SerializationMode)EditorGUILayout.EnumPopup(
                    "Asset Serialization",
                    _preset.assetSerializationMode);
            _preset.compactYamlMappings = EditorGUILayout.Toggle(
                new GUIContent("Compact YAML mappings", "Writes references and similar YAML mappings on one line to reduce diff noise."),
                _preset.compactYamlMappings);
            _preset.lineEndingsForNewScripts =
                (LineEndingsMode)EditorGUILayout.EnumPopup(
                    "New script line endings",
                    _preset.lineEndingsForNewScripts);
        }

        private void DrawScriptingSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scripting", EditorStyles.boldLabel);
            _preset.rootNamespace = EditorGUILayout.TextField(
                new GUIContent("Root namespace", "Root namespace generated into C# project files."),
                _preset.rootNamespace ?? string.Empty);

            using (new EditorGUILayout.HorizontalScope())
            {
                _preset.apiCompatibilityLevel =
                    (ApiCompatibilityLevel)EditorGUILayout.EnumPopup(
                        new GUIContent("API Compatibility", ActiveNamedBuildTarget.TargetName),
                        _preset.apiCompatibilityLevel);
                GUILayout.Label(ActiveNamedBuildTarget.TargetName, EditorStyles.miniLabel, GUILayout.MaxWidth(95f));
            }
        }

        private void DrawEditorWorkflowSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Workflow", EditorStyles.boldLabel);
            _preset.asyncShaderCompilation = EditorGUILayout.Toggle(
                "Async shader compilation",
                _preset.asyncShaderCompilation);
            _preset.prefabAutoSave = EditorGUILayout.Toggle(
                "Prefab Mode Auto Save",
                _preset.prefabAutoSave);

            _preset.configureEnterPlayMode = EditorGUILayout.Toggle(
                new GUIContent("Configure Enter Play Mode", "When disabled, this wizard leaves the project's Enter Play Mode settings untouched."),
                _preset.configureEnterPlayMode);

            using (new EditorGUI.DisabledScope(!_preset.configureEnterPlayMode))
            using (new EditorGUI.IndentLevelScope())
            {
                _preset.enterPlayModeOptions =
                    (EnterPlayModeOptions)EditorGUILayout.EnumFlagsField(
                        "Options",
                        _preset.enterPlayModeOptions);
            }

            if (_preset.configureEnterPlayMode)
            {
                EditorGUILayout.HelpBox(
                    "Skipping Domain Reload requires your static state and event " +
                    "subscriptions to reset themselves explicitly.",
                    MessageType.Warning);
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Current Project"))
                {
                    _preset = CaptureCurrentProject();
                    _hasUnsavedChanges = true;
                }

                if (GUILayout.Button("Forget Saved Defaults"))
                {
                    if (EditorUtility.DisplayDialog(
                            "Forget Saved Defaults?",
                            "The next time this window opens it will initialize from that project's settings.",
                            "Forget",
                            "Cancel"))
                    {
                        EditorPrefs.DeleteKey(PreferencesKey);
                        _preset = CaptureCurrentProject();
                        _hasUnsavedChanges = false;
                    }
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!_hasUnsavedChanges && MatchesCurrentProject(_preset)))
                {
                    if (GUILayout.Button("Apply", GUILayout.Width(90f), GUILayout.Height(24f)))
                        ApplyAndRemember();
                }
            }
            EditorGUILayout.Space(6f);
        }

        private void LoadPreferredOrCurrent()
        {
            if (EditorPrefs.HasKey(PreferencesKey))
            {
                try
                {
                    _preset = JsonUtility.FromJson<SettingsPreset>(
                        EditorPrefs.GetString(PreferencesKey));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Could not read Project Settings Memory defaults: " + exception.Message);
                }
            }

            if (_preset == null)
                _preset = CaptureCurrentProject();

            _hasUnsavedChanges = false;
        }

        private static SettingsPreset CaptureCurrentProject()
        {
            return new SettingsPreset
            {
                gameObjectNamingScheme = EditorSettings.gameObjectNamingScheme,
                gameObjectNamingDigits = EditorSettings.gameObjectNamingDigits,
                assetNamingUsesSpace = EditorSettings.assetNamingUsesSpace,
                assetSerializationMode = EditorSettings.serializationMode,
                lineEndingsForNewScripts = EditorSettings.lineEndingsForNewScripts,
                rootNamespace = EditorSettings.projectGenerationRootNamespace,
                apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(ActiveNamedBuildTarget),
                asyncShaderCompilation = EditorSettings.asyncShaderCompilation,
                prefabAutoSave = EditorSettings.prefabModeAllowAutoSave,
                compactYamlMappings = EditorSettings.serializeInlineMappingsOnOneLine,
                configureEnterPlayMode = EditorSettings.enterPlayModeOptionsEnabled,
                enterPlayModeOptions = EditorSettings.enterPlayModeOptions
            };
        }

        private void ApplyAndRemember()
        {
            EditorSettings.gameObjectNamingScheme = _preset.gameObjectNamingScheme;
            EditorSettings.gameObjectNamingDigits = Mathf.Clamp(_preset.gameObjectNamingDigits, 1, 5);
            EditorSettings.assetNamingUsesSpace = _preset.assetNamingUsesSpace;
            EditorSettings.serializationMode = _preset.assetSerializationMode;
            EditorSettings.lineEndingsForNewScripts = _preset.lineEndingsForNewScripts;
            EditorSettings.projectGenerationRootNamespace = (_preset.rootNamespace ?? string.Empty).Trim();
            PlayerSettings.SetApiCompatibilityLevel(ActiveNamedBuildTarget, _preset.apiCompatibilityLevel);
            EditorSettings.asyncShaderCompilation = _preset.asyncShaderCompilation;
            EditorSettings.prefabModeAllowAutoSave = _preset.prefabAutoSave;
            EditorSettings.serializeInlineMappingsOnOneLine = _preset.compactYamlMappings;
            EditorSettings.enterPlayModeOptionsEnabled = _preset.configureEnterPlayMode;

            if (_preset.configureEnterPlayMode)
                EditorSettings.enterPlayModeOptions = _preset.enterPlayModeOptions;

            EditorPrefs.SetString(PreferencesKey, JsonUtility.ToJson(_preset));
            AssetDatabase.SaveAssets();
            _hasUnsavedChanges = false;
            ShowNotification(new GUIContent("Applied and remembered"));
        }

        private static bool MatchesCurrentProject(SettingsPreset preset)
        {
            if (preset == null)
                return false;

            return preset.gameObjectNamingScheme == EditorSettings.gameObjectNamingScheme
                && preset.gameObjectNamingDigits == EditorSettings.gameObjectNamingDigits
                && preset.assetNamingUsesSpace == EditorSettings.assetNamingUsesSpace
                && preset.assetSerializationMode == EditorSettings.serializationMode
                && preset.lineEndingsForNewScripts == EditorSettings.lineEndingsForNewScripts
                && string.Equals(preset.rootNamespace ?? string.Empty,
                    EditorSettings.projectGenerationRootNamespace ?? string.Empty,
                    StringComparison.Ordinal)
                && preset.apiCompatibilityLevel == PlayerSettings.GetApiCompatibilityLevel(ActiveNamedBuildTarget)
                && preset.asyncShaderCompilation == EditorSettings.asyncShaderCompilation
                && preset.prefabAutoSave == EditorSettings.prefabModeAllowAutoSave
                && preset.compactYamlMappings == EditorSettings.serializeInlineMappingsOnOneLine
                && preset.configureEnterPlayMode == EditorSettings.enterPlayModeOptionsEnabled
                && (!preset.configureEnterPlayMode
                    || preset.enterPlayModeOptions == EditorSettings.enterPlayModeOptions);
        }

        private static NamedBuildTarget ActiveNamedBuildTarget
        {
            get
            {
                var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                return NamedBuildTarget.FromBuildTargetGroup(group);
            }
        }
    }
}
