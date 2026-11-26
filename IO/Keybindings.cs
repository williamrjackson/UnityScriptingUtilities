using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Wrj
{
	public class Keybindings : MonoBehaviour
	{
        public enum KeyboardMapping
        {
            CurrentLayout,
            PhysicalPosition
        }

        [SerializeField]
        [Tooltip("Current Layout follows the OS keyboard layout for printable keys. Physical Position uses US keyboard positions. Applies to the new Input System.")]
        private KeyboardMapping keyboardMapping = KeyboardMapping.CurrentLayout;

        [SerializeField]
        private bool quitOnEsc = true;
        [SerializeField]
        private bool fullScreenOnF11 = true;
        [SerializeField]
        private ActionKeyCommand[] actionKeys;
        [SerializeField]
        private ButtonKeyCommand[] buttonKeys;
        [SerializeField]
        private ToggleKeyCommand[] toggleKeys;
        [SerializeField]
        private HierarchyToggles[] objectEnableKeys;

		public bool logKeys = false;

		private List<KeyCommand> _keyCommands;
        private List<ActionKeyCommand> _keyUps;

        public void Add(KeyCommand keyCommand)
        {
			if (_keyCommands == null) _keyCommands = new List<KeyCommand>();
			if (_keyUps == null) _keyUps = new List<ActionKeyCommand>();
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
			if (buttonKeys != null)
			{
				foreach (var item in buttonKeys)
				{
					_keyCommands.Add(item);
				}
			}
			if (toggleKeys != null)
			{
				foreach (var item in toggleKeys)
				{
					_keyCommands.Add(item);
				}
			}
			if (actionKeys != null)
			{
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
			}
			if (objectEnableKeys != null)
			{
				foreach (var item in objectEnableKeys)
				{
					_keyCommands.Add(item);
				}
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
				if (GetKeyUp(keyUp.key))
				{
					if (!keyUp.ModifierQualified()) continue;
					if (logKeys)
					{
						Debug.Log(keyUp.ToString());
					}
                    keyUp.Invoke();
				}
			}


			if (quitOnEsc && GetKeyDown(KeyCode.Escape))
			{
#if UNITY_EDITOR
			    UnityEditor.EditorApplication.isPlaying = false;
#endif
                Application.Quit();
            }

			if (fullScreenOnF11 && GetKeyDown(KeyCode.F11))
            {
				Screen.fullScreen = !Screen.fullScreen;
			}

			foreach (KeyCommand keyCommand in _keyCommands)
			{
				if (GetKeyDown(keyCommand.key))
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

        // Prefer the new backend when Active Input Handling is set to Both.
        private static bool GetKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            return GetButton(key)?.isPressed ?? false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }

        private bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            return GetButton(key, keyboardMapping)?.wasPressedThisFrame ?? false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        private bool GetKeyUp(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            return GetButton(key, keyboardMapping)?.wasReleasedThisFrame ?? false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyUp(key);
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static readonly string[] LayoutKeyLabels = CreateLayoutKeyLabels();

        private static string[] CreateLayoutKeyLabels()
        {
            var labels = new string[127];
            for (int i = 33; i < labels.Length; i++)
                labels[i] = ((char)i).ToString();
            return labels;
        }

        private static ButtonControl GetButton(KeyCode key, KeyboardMapping mapping = KeyboardMapping.PhysicalPosition)
        {
            var mouse = Mouse.current;
            switch (key)
            {
                case KeyCode.Mouse0: return mouse?.leftButton;
                case KeyCode.Mouse1: return mouse?.rightButton;
                case KeyCode.Mouse2: return mouse?.middleButton;
                case KeyCode.Mouse3: return mouse?.backButton;
                case KeyCode.Mouse4: return mouse?.forwardButton;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return null;
            // Resolve each query so OS layout switches take effect without a stale cache.
            // KeyCode's printable values are ASCII; special keys and keypad keys remain physical.
            int character = (int)key;
            if (mapping == KeyboardMapping.CurrentLayout && character > 32 && character < 127)
                return keyboard.FindKeyOnCurrentKeyboardLayout(LayoutKeyLabels[character]);

            var mapped = ToInputSystemKey(key);
            return mapped == Key.None ? null : keyboard[mapped];
        }

        // KeyCode values remain serialized so existing scenes and public API stay compatible.
        // Input System keys refer to physical positions on a US keyboard layout.
        private static Key ToInputSystemKey(KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z)
                return (Key)((int)Key.A + (int)key - (int)KeyCode.A);
            if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha9)
                return (Key)((int)Key.Digit1 + (int)key - (int)KeyCode.Alpha1);
            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
                return (Key)((int)Key.Numpad0 + (int)key - (int)KeyCode.Keypad0);
            if (key >= KeyCode.F1 && key <= KeyCode.F12)
                return (Key)((int)Key.F1 + (int)key - (int)KeyCode.F1);
            switch (key)
            {
                case KeyCode.Space: return Key.Space;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.BackQuote: return Key.Backquote;
                case KeyCode.Quote: return Key.Quote;
                case KeyCode.Semicolon: return Key.Semicolon;
                case KeyCode.Comma: return Key.Comma;
                case KeyCode.Period: return Key.Period;
                case KeyCode.Slash: return Key.Slash;
                case KeyCode.Backslash: return Key.Backslash;
                case KeyCode.LeftBracket: return Key.LeftBracket;
                case KeyCode.RightBracket: return Key.RightBracket;
                case KeyCode.Minus: return Key.Minus;
                case KeyCode.Equals: return Key.Equals;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.Backspace: return Key.Backspace;
                case KeyCode.PageDown: return Key.PageDown;
                case KeyCode.PageUp: return Key.PageUp;
                case KeyCode.Home: return Key.Home;
                case KeyCode.End: return Key.End;
                case KeyCode.Insert: return Key.Insert;
                case KeyCode.Delete: return Key.Delete;
                case KeyCode.CapsLock: return Key.CapsLock;
                case KeyCode.Pause: return Key.Pause;
                case KeyCode.F13: return Key.F13;
                case KeyCode.F14: return Key.F14;
                case KeyCode.F15: return Key.F15;
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftWindows: return Key.LeftMeta;
                case KeyCode.RightWindows: return Key.RightMeta;
                case KeyCode.LeftApple: return Key.LeftMeta;
                case KeyCode.RightApple: return Key.RightMeta;
                case KeyCode.Menu: return Key.ContextMenu;
                case KeyCode.Numlock: return Key.NumLock;
                case KeyCode.Print: return Key.PrintScreen;
                case KeyCode.ScrollLock: return Key.ScrollLock;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.KeypadDivide: return Key.NumpadDivide;
                case KeyCode.KeypadMultiply: return Key.NumpadMultiply;
                case KeyCode.KeypadPlus: return Key.NumpadPlus;
                case KeyCode.KeypadMinus: return Key.NumpadMinus;
                case KeyCode.KeypadPeriod: return Key.NumpadPeriod;
                case KeyCode.KeypadEquals: return Key.NumpadEquals;
                // Joystick buttons and keys with no physical equivalent are unsupported.
                default: return Key.None;
            }
        }
#endif

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

				bool shiftState =	GetKey(KeyCode.LeftShift) || GetKey(KeyCode.RightShift);
				bool ctrlState =	GetKey(KeyCode.LeftControl) || GetKey(KeyCode.RightControl);
				bool altState =		GetKey(KeyCode.LeftAlt) || GetKey(KeyCode.RightAlt);
				bool winState =		GetKey(KeyCode.LeftWindows) || GetKey(KeyCode.RightWindows)
								 || GetKey(KeyCode.LeftApple) || GetKey(KeyCode.RightApple);
				
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