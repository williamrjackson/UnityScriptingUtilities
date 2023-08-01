using UnityEngine;
using UnityEngine.Events;

namespace Wrj
{
	public class Keybindings : MonoBehaviour
	{
        [SerializeField]
        private bool quitOnEsc = true;
        [SerializeField]
        private bool fullScreenOnF11 = true;
		public ButtonKeyCommand[] buttonKeys;
		public ToggleKeyCommand[] toggleKeys;
		public ActionKeyCommand[] actionKeys;
		public HierarchyToggles[] objectEnableKeys;

		void Update() 
		{
			if (!Input.anyKey) return;

			foreach (ActionKeyCommand actionKey in actionKeys)
			{
				if (actionKey.onKeyUp && Input.GetKeyUp(actionKey.key))
				{
					if (!actionKey.ModifierQualified()) continue;
					actionKey.Invoke();
				}
			}

			if (!Input.anyKeyDown) return;

			if (quitOnEsc && Input.GetKeyDown(KeyCode.Escape))
			{
#if UNITY_EDITOR
			    UnityEditor.EditorApplication.isPlaying = false;
#endif
                Application.Quit();
            }

			if (fullScreenOnF11 && Input.GetKeyDown(KeyCode.F11))
            {
				Screen.fullScreen = !Screen.fullScreen;
			}

			foreach (ActionKeyCommand actionKey in actionKeys)
			{
				if (Input.GetKeyDown(actionKey.key))
				{
					if (!actionKey.ModifierQualified()) continue;
					actionKey.Invoke();
				}
			}
			foreach (ButtonKeyCommand buttonKey in buttonKeys)
			{
				if (Input.GetKeyDown(buttonKey.key))
				{
					if (!buttonKey.ModifierQualified()) continue;
					buttonKey.Invoke();
				}
			}

			foreach (ToggleKeyCommand toggleKey in toggleKeys)
			{
				if (Input.GetKeyDown(toggleKey.key))
				{
					if (!toggleKey.ModifierQualified()) continue;
					toggleKey.Invoke();
				}
			}

			foreach (HierarchyToggles hierarchyKey in objectEnableKeys)
			{
				if (Input.GetKeyDown(hierarchyKey.key))
				{
					if (!hierarchyKey.ModifierQualified()) continue;
					hierarchyKey.Invoke();
				}
			}
		}


		[System.Serializable]
		public class KeyCommand
        {
			public KeyCode key;
			[Header("Modifier Keys")]
			public bool ctrl;
			public bool shift;
			public bool alt;
			public bool win;

			public bool ModifierQualified()
			{
                // If no modifiers are required, ignore all modifier states
                if (!ctrl && !shift && !alt && !win) return true;

				bool shiftState = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
				bool ctrlState = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
				bool altState = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
				bool winState = (Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows));
				
                // Return true if all states match requirements
				return (shiftState == shift &&
                        ctrlState == ctrl &&
                        altState == alt &&
                        winState == win);
			}

			public virtual void Invoke() { }

		}

		[System.Serializable]
		public class ButtonKeyCommand : KeyCommand
		{
			[Header("Action")]
			public UnityEngine.UI.Button button;
            public override void Invoke()
            {
				if (button != null && button.interactable)
				{
					button.onClick.Invoke();
				}
			}
        }
		[System.Serializable]
		public class ToggleKeyCommand : KeyCommand
		{
			[Header("Action")]
			public UnityEngine.UI.Toggle toggle;
            public override void Invoke()
            {
				if (toggle != null && toggle.interactable)
				{
					toggle.isOn = !toggle.isOn;
				}

			}
		}
		[System.Serializable]
		public class ActionKeyCommand : KeyCommand
		{
			public bool onKeyUp = false;
			[Header("Action")]
			public UnityEvent action;
            public override void Invoke()
            {
				action.Invoke();
			}
		}
		[System.Serializable]

		public class HierarchyToggles : KeyCommand
		{
			[Header("Action")]
			public GameObject gameObject;
            public override void Invoke()
            {
				gameObject.ToggleActive();
			}
		}
    }
}