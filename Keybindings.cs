using System;
using System.Collections.Generic;
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
        [SerializeField]
        private ButtonKeyCommand[] buttonKeys;
        [SerializeField]
        private ToggleKeyCommand[] toggleKeys;
        [SerializeField]
        private ActionKeyCommand[] actionKeys;
        [SerializeField]
        private HierarchyToggles[] objectEnableKeys;

		private List<KeyCommand> _keyCommands;

        public void Add(KeyCommand keyCommand)
        {
            _keyCommands.Add(keyCommand);
            _keyCommands.Sort();
        }

        private void Awake()
        {
			_keyCommands = new List<KeyCommand>();
			foreach (var item in buttonKeys)
			{
				_keyCommands.Add(item);
			}
			foreach (var item in toggleKeys)
			{
				_keyCommands.Add(item);
			}
			foreach (var item in actionKeys)
			{
				_keyCommands.Add(item);
			}
			foreach (var item in objectEnableKeys)
			{
				_keyCommands.Add(item);
			}
			_keyCommands.Sort();
        }

        void Update() 
		{
			KeyCommand.NewFrame();

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

			foreach (KeyCommand keyCommand in _keyCommands)
			{
				if (Input.GetKeyDown(keyCommand.key))
				{
					if (!keyCommand.ModifierQualified()) continue;
                    keyCommand.Invoke();
				}
			}
		}


		[Serializable]
		public class KeyCommand : IComparable<KeyCommand>
        {
			public KeyCode key;
			[Header("Modifier Keys")]
			public bool ctrl;
			public bool shift;
			public bool alt;
			public bool win;

			private static List<KeyCode> rejections = new List<KeyCode>();
			public bool ModifierQualified()
			{
				if (rejections.Contains(key)) return false;
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
			public static void NewFrame()
			{
				if (rejections == null)
				{
					rejections = new List<KeyCode>();
				}
				rejections.Clear();
			}
			public virtual void Invoke()
			{
				rejections.Add(key);
			}
			public int ModifierCount()
			{
				int count = 0;
				if (ctrl) count++;
				if (shift) count++;
				if (alt) count++;
				if (win) count++;
				return count;
			}

            public int CompareTo(KeyCommand other)
            {
				if (ModifierCount() == other.ModifierCount()) return 0;
				return (ModifierCount() > other.ModifierCount()) ? -1 : 1;
            }
        }

		[Serializable]
		public class ButtonKeyCommand : KeyCommand
		{
			[Header("Action")]
			public UnityEngine.UI.Button button;
            public override void Invoke()
            {
				base.Invoke();
				if (button != null && button.interactable)
				{
					button.onClick.Invoke();
				}
			}
        }
		[Serializable]
		public class ToggleKeyCommand : KeyCommand
		{
			[Header("Action")]
			public UnityEngine.UI.Toggle toggle;
            public override void Invoke()
            {
				base.Invoke();
				if (toggle != null && toggle.interactable)
				{
					toggle.isOn = !toggle.isOn;
				}

			}
		}
		[Serializable]
		public class ActionKeyCommand : KeyCommand
		{
			public bool onKeyUp = false;
			[Header("Action")]
			public UnityEvent action;
            public override void Invoke()
            {
				base.Invoke();
				action.Invoke();
			}
		}
		[Serializable]
		public class HierarchyToggles : KeyCommand
		{
			[Header("Action")]
			public GameObject gameObject;
            public override void Invoke()
            {
				base.Invoke();
				gameObject.ToggleActive();
			}
		}
    }
}