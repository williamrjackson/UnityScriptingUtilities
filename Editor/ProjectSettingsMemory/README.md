# Project Settings Memory

A Unity 6 editor-only package that remembers preferred project settings across
projects.

Open **Tools > Project Settings Memory...**. The first time the tool is used on
a machine it initializes from the current project, which means a fresh project
uses the actual defaults for its Unity version and template. Clicking **Apply**:

1. writes the displayed values into the current project's settings;
2. stores the same preset in Unity's per-user `EditorPrefs`;
3. uses that preset when the window is opened in another project.

**Load Current Project** replaces the form with the current project's values.
**Forget Saved Defaults** deletes the cross-project preset; it does not revert
the current project.

API Compatibility applies to the active build target shown beside the field.
Switch build target before applying if you need to configure another target.

## Installation

Copy the `ProjectSettingsMemory` folder into a project under `Packages`, or add
it from a Git URL after changing the package name and author in `package.json`.

## Notes

- Remembered defaults are local to the current OS user and Unity editor
  installation preferences; they are not committed with a project.
- Applied project settings are stored by Unity in `ProjectSettings` and should
  be committed as usual.
- Skipping Domain Reload can substantially speed up Play Mode, but requires
  code that resets static state and event subscriptions correctly.
