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

		public bool logKeys = false;

		private List<KeyCommand> _keyCommands;
        private List<ActionKeyCommand> _keyUps;

        public void Add(KeyCommand keyCommand)
        {
			if (keyCommand is ActionKeyCommand && ((ActionKeyCommand)keyCommand).onKeyUp)
			{
				_keyUps.Add((ActionKeyCommand)keyCommand);
			}
			else
			{
	            _keyCommands.Add(keyCommand);
			}
			Prioritize();
        }

        private void Awake()
        {
            _keyCommands = new List<KeyCommand>();
			_keyUps = new List<ActionKeyCommand>();
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
				if (item.onKeyUp)
				{
					_keyUps.Add(item);
				}
				else
				{
	                _keyCommands.Add(item);
				}
            }
            foreach (var item in objectEnableKeys)
            {
                _keyCommands.Add(item);
            }
			Prioritize();
        }

		private void Prioritize()
		{
            _keyCommands.Sort();
			_keyUps.Sort();
        }

        void Update() 
		{
			KeyCommand.NewFrame();

			foreach (ActionKeyCommand keyUp in _keyUps)
			{
				if (Input.GetKeyUp(keyUp.key))
				{
					if (!keyUp.ModifierQualified()) continue;
					if (logKeys)
					{
						Debug.Log(keyUp.ToString());
					}
                    keyUp.Invoke();
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
                    if (logKeys)
                    {
                        Debug.Log(keyCommand.ToString());
                    }
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

				bool shiftState =	Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
				bool ctrlState =	Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
				bool altState =		Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
				bool winState =		Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows)
								 || Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple);
				
                // Return true if all states match requirements
				return shiftState == shift &&
                       ctrlState == ctrl &&
                       altState == alt &&
                       winState == win;
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
            public override string ToString()
            {
				string id = string.Empty;
                if (ctrl) id += "CTRL+";
                if (shift) id += "SHIFT+";
                if (alt) id += "ALT+";
                if (win) id += "WIN+";
				id += Enum.GetName(typeof(KeyCode), key);
                return id;
            }
        }

		[Serializable]
		public class ButtonKeyCommand : KeyCommand
		{
			[Header("Action")]
			public UnityEngine.UI.Button button;
            public override void Invoke()
            {
                if (button == null) return;
                base.Invoke();
				if (button != null && button.interactable)
				{
					button.onClick.Invoke();
				}
			}
            public override string ToString()
            {
				return $"{base.ToString()}: [Button] {button.name}";
            }
        }
		[Serializable]
		public class ToggleKeyCommand : KeyCommand
		{
			[Header("Action")]
			public UnityEngine.UI.Toggle toggle;
            public override void Invoke()
            {
                if (toggle == null) return;
                base.Invoke();
				if (toggle != null && toggle.interactable)
				{
					toggle.isOn = !toggle.isOn;
				}
			}
            public override string ToString()
            {
                return $"{base.ToString()}: [Toggle] {toggle.name}";
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
				if (action == null) return;
				base.Invoke();
				action.Invoke();
			}
            public override string ToString()
            {
                string result = $"{base.ToString()}: [Events";
				if (onKeyUp) result += " (On Key Up)";
				result += "]";
                int eventCount = action.GetPersistentEventCount();
				if (eventCount < 1) return result;
				result += $"\n";

                for (int i = 0; i < eventCount; i++)
				{
					result += $"{action.GetPersistentTarget(i)}.{action.GetPersistentMethodName(i)}";
					if (i < eventCount - 1) result += "\n";
				}
                return result;
            }
        }
        [Serializable]
		public class HierarchyToggles : KeyCommand
		{
			[Header("Action")]
			public GameObject gameObject;
            public override void Invoke()
            {
                if (gameObject == null) return;
                base.Invoke();
				gameObject.ToggleActive();
			}
            public override string ToString()
            {
                return $"{base.ToString()}: [ToggleActive] {gameObject.name}";
            }
        }
    }
}