using UnityEngine;
using UnityEngine.Events;

namespace Wrj
{		
	public class Keybindings : MonoBehaviour {
		public ButtonKeyCommand[] buttonKeys;
		public ToggleKeyCommand[] toggleKeys;
		public ActionKeyCommand[] actionKeys;
		public HierarchyToggles[] objectEnableKeys;
		void Update() {
			if (!Input.anyKeyDown)
			{
				return;
			}

			if (Input.GetKeyDown(KeyCode.Escape)) {
				Application.Quit();
			}
			if (Input.GetKeyDown(KeyCode.F11)) {
				Screen.fullScreen = !Screen.fullScreen;
			}
			
			foreach (ButtonKeyCommand buttonKey in buttonKeys)
			{
				if (Input.GetKeyDown(buttonKey.key))
				{
					if (buttonKey.button != null && buttonKey.button.interactable)
					{
						buttonKey.button.onClick.Invoke();
					}		
				}
			}

			foreach (ToggleKeyCommand toggleKey in toggleKeys)
			{
				if (Input.GetKeyDown(toggleKey.key))
				{
					if (toggleKey.toggle != null && toggleKey.toggle.interactable)
					{
						toggleKey.toggle.isOn = !toggleKey.toggle.isOn;
					}
				}
			}

			foreach (ActionKeyCommand actionKey in actionKeys)
			{
				if (Input.GetKeyDown(actionKey.key))
				{
					actionKey.action.Invoke();
				}
			}

			foreach (HierarchyToggles hierarchyKey in objectEnableKeys)
			{
				if (Input.GetKeyDown(hierarchyKey.key))
				{
					hierarchyKey.go.ToggleActive();
				}
			}
		
		}
		[System.Serializable]
		public class ButtonKeyCommand
		{
			public UnityEngine.UI.Button button;
			public KeyCode key;
		}
		[System.Serializable]
		public class ToggleKeyCommand
		{
			public UnityEngine.UI.Toggle toggle;
			public KeyCode key;
		}
		[System.Serializable]
		public class ActionKeyCommand
		{
			public UnityEvent action;
			public KeyCode key;
		}
		[System.Serializable]

		public class HierarchyToggles
		{
			public GameObject go;
			public KeyCode key;
		}
		
	}
}
