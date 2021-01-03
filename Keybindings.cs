using UnityEngine;
using UnityEngine.Events;

namespace Wrj
{		
	public class Keybindings : MonoBehaviour 
	{	
		public ActionKeyCommand[] actionKeys;
		public ButtonKeyCommand[] buttonKeys;
		public ToggleKeyCommand[] toggleKeys;
		public HierarchyToggles[] objectEnableKeys;

		void Update() {
			// If no keys are down, don't check for keys.
			// If there are any actionKeys enabled, we could be awaiting a KeyUp. 
			// So check anyway.
			if (!Input.anyKeyDown && actionKeys.Length == 0)
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
					actionKey.keyDownAction.Invoke();
				}
				else if (Input.GetKeyUp(actionKey.key))
				{
					actionKey.keyUpAction.Invoke();
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
			public KeyCode key;
			public UnityEngine.UI.Button button;
		}
		[System.Serializable]
		public class ToggleKeyCommand
		{
			public KeyCode key;
			public UnityEngine.UI.Toggle toggle;
		}
		[System.Serializable]
		public class ActionKeyCommand
		{
			public KeyCode key;
			public UnityEvent keyDownAction;
			public UnityEvent keyUpAction;
		}
		[System.Serializable]
		public class HierarchyToggles
		{
			public KeyCode key;
			public GameObject go;
		}
		
	}
}
