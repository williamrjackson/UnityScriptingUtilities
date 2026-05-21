using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wrj
{
    // This component is meant for testing and demo purposes using an Arduino test button box running:
    // https://gist.github.com/williamrjackson/94dc3bafc02890cec27367c2b60f21c1
    public class ArduinoTestConfig : MonoBehaviour
    {
        public bool enableLogging = true;
        public bool sendConfigOnStart = true;
        public bool isTemporary = true;
        [SerializeField]
        private SerialButton button1 = new SerialButton("1", SerialButton.ButtonMode.Serial, "1", SerialButton.ArduinoKeyCode.A);
        [SerializeField]
        private SerialButton button1Mod = new SerialButton("1b", SerialButton.ButtonMode.Serial, "1", SerialButton.ArduinoKeyCode.ZERO);
        [SerializeField]
        private SerialButton button2 = new SerialButton("2", SerialButton.ButtonMode.Serial, "2", SerialButton.ArduinoKeyCode.B);
        [SerializeField]
        private SerialButton button2Mod = new SerialButton("2b", SerialButton.ButtonMode.Serial, "2", SerialButton.ArduinoKeyCode.ONE);
        [SerializeField]
        private SerialButton rotaryPress = new SerialButton("3", SerialButton.ButtonMode.Serial, "3", SerialButton.ArduinoKeyCode.C);
        public string rotaryMin = "0";
        public string rotaryMax = "359";

        private Coroutine _sendCoroutine;

        private static ArduinoTestConfig _instance;
        public static ArduinoTestConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ArduinoTestConfig>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ArduinoTestConfig");
                        _instance = go.AddComponent<ArduinoTestConfig>();
                        // default to not sending config on start, since this component
                        // was added automatically and likely won't be set up yet. 
                        // Config can be sent manually from the inspector or by calling 
                        // Instance.Start() from another script once setup is complete.
                        _instance.sendConfigOnStart = false; 
                    }
                }
                return _instance;
            }
        }
        private void Start()
        {
            if (sendConfigOnStart)
                SendConfig();
        }
        public void SendConfig()
        {
            if (_sendCoroutine != null)
            {
                StopCoroutine(_sendCoroutine);
            }
            _sendCoroutine = StartCoroutine(SendToDeviceRoutine());
        }
        private IEnumerator SendToDeviceRoutine()
        {
            if (!Application.isEditor)
            {
                // Only send config in editor for testing purposes. 
                // In a real build, config would likely be sent from a hardened button box.
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
            string baudConfig = $"baud:{(isTemporary ? "temp:" : "")}{ArduinoSerialCommunication.Instance.CurrentBaudRate}";
            ArduinoSerialCommunication.Instance.SendDataAsLine(baudConfig);
            yield return new WaitForSeconds(0.1f);
            string button1Config = button1.SendConfig(isTemporary);
            yield return new WaitForSeconds(0.1f);
            string button1ModConfig = button1Mod.SendConfig(isTemporary);
            yield return new WaitForSeconds(0.1f);
            string button2Config = button2.SendConfig(isTemporary);
            yield return new WaitForSeconds(0.1f);
            string button2ModConfig = button2Mod.SendConfig(isTemporary);
            yield return new WaitForSeconds(0.1f);
            string rotaryPressConfig = rotaryPress.SendConfig(isTemporary);
            yield return new WaitForSeconds(0.1f);
            string rotaryMinConfig = $"RMin:{(isTemporary ? "temp:" : "")}{rotaryMin}";
            ArduinoSerialCommunication.Instance.SendDataAsLine(rotaryMinConfig);
            yield return new WaitForSeconds(0.1f);
            string rotaryMaxConfig = $"RMax:{(isTemporary ? "temp:" : "")}{rotaryMax}";
            ArduinoSerialCommunication.Instance.SendDataAsLine(rotaryMaxConfig);
            if (enableLogging)
            {   
                Debug.Log("Serial Button 1 Config: " + button1Config);
                Debug.Log("Serial Button 1(Modified) Config: " + button1ModConfig);
                Debug.Log("Serial Button 2 Config: " + button2Config);
                Debug.Log("Serial Button 2(Modified) Config: " + button2ModConfig);
                Debug.Log("Serial Rotary Press Config: " + rotaryPressConfig);
                Debug.Log("Serial Rotary Min Config: " + rotaryMinConfig);
                Debug.Log("Serial Rotary Max Config: " + rotaryMaxConfig);
            }
        }
        public static void Echo(string data)
        {
            ArduinoSerialCommunication.Instance.SendDataAsLine($"echo:{data}");
            if (Instance.enableLogging)
                Debug.Log("Serial Echo: " + data);
        }
        [System.Serializable]
        private class SerialButton
        {
            [SerializeField, HideInInspector]
            private string id;
            [SerializeField]
            private ButtonMode mode;
            [SerializeField]
            private string command;
            [SerializeField]
            private ArduinoKeyCode[] keyCombo;

            public SerialButton(string id, ButtonMode mode, string command, ArduinoKeyCode[] keyCombo = null)
            {
                this.id = id;
                UpdateConfig(mode, command, keyCombo);
            }
            public SerialButton(string id, ButtonMode mode, string command, ArduinoKeyCode key = ArduinoKeyCode.A)
            {
                this.id = id;
                UpdateConfig(mode, command, key);
            }

            public void UpdateConfig(ButtonMode newMode, string newCommand, ArduinoKeyCode newKey)
            {
                mode = newMode;
                command = newCommand;
                keyCombo = new ArduinoKeyCode[] { newKey };
            }
            public void UpdateConfig(ButtonMode newMode, string newCommand, ArduinoKeyCode[] newKeyCombo)
            {
                mode = newMode;
                command = newCommand;
                keyCombo = newKeyCombo;
            }

            public string SendConfig(bool isTemporary)
            {
                string data = $"{id}:{(isTemporary ? "temp:" : "")}{((mode==ButtonMode.Keypress) ? "ascii:" : "")}{(mode == ButtonMode.Serial ? command : keyString)}";
                ArduinoSerialCommunication.Instance.SendDataAsLine(data);
                return data;
            }
            private string keyString
            {
                get
                {
                    if (keyCombo == null || keyCombo.Length == 0)
                        return "";
                    string result = "";
                    foreach (var k in keyCombo)
                    {
                        result += ((int)k).ToString() + "+";
                    }
                    return result.TrimEnd('+');
                }
            }
            public enum ButtonMode
            {
                Serial,
                Keypress,
            }
            public enum ArduinoKeyCode
            {
                SPACE = 32, EXCLAMATION = 33, DOUBLE_QUOTE = 34, HASH = 35, DOLLAR = 36, PERCENT = 37, AMPERSAND = 38, APOSTROPHE = 39,
                LEFT_PAREN = 40, RIGHT_PAREN = 41, ASTERISK = 42, PLUS = 43, COMMA = 44, HYPHEN_MINUS = 45, PERIOD = 46, SLASH = 47,

                ZERO = 48, ONE = 49, TWO = 50, THREE = 51, FOUR = 52, FIVE = 53, SIX = 54, SEVEN = 55, EIGHT = 56, NINE = 57,
                COLON = 58, SEMICOLON = 59, LESS_THAN = 60, EQUALS = 61, GREATER_THAN = 62, QUESTION = 63, AT = 64,

                LEFT_BRACKET = 91, BACKSLASH = 92, RIGHT_BRACKET = 93, CARET = 94, UNDERSCORE = 95, GRAVE = 96,
                A = 97, B = 98, C = 99, D = 100, E = 101, F = 102, G = 103, H = 104, I = 105, J = 106, K = 107, L = 108, M = 109,
                N = 110, O = 111, P = 112, Q = 113, R = 114, S = 115, T = 116, U = 117, V = 118, W = 119, X = 120, Y = 121, Z = 122,

                LEFT_BRACE = 123, VERTICAL_BAR = 124, RIGHT_BRACE = 125, TILDE = 126, DEL = 127,
                CTRL = 128, SHIFT = 129, ALT = 130, GUI = 131,

                RETURN = 176, ESC = 177, BACKSPACE = 178, TAB = 179,

                CAPS_LOCK = 193, F1 = 194, F2 = 195, F3 = 196, F4 = 197, F5 = 198, F6 = 199, F7 = 200, F8 = 201, F9 = 202,
                F10 = 203, F11 = 204, F12 = 205, PRINT_SCREEN = 206, SCROLL_LOCK = 207, PAUSE = 208,

                INSERT = 209, HOME = 210, PAGE_UP = 211, DELETE = 212, END = 213, PAGE_DOWN = 214,
                RIGHT_ARROW = 215, LEFT_ARROW = 216, DOWN_ARROW = 217, UP_ARROW = 218,
                NUM_LOCK = 219, KP_SLASH = 220, KP_ASTERISK = 221, KP_MINUS = 222, KP_PLUS = 223, KP_ENTER = 224,
                KP_1 = 225, KP_2 = 226, KP_3 = 227, KP_4 = 228, KP_5 = 229, KP_6 = 230, KP_7 = 231, KP_8 = 232, KP_9 = 233, KP_0 = 234, KP_DOT = 235,

                F13 = 240, F14 = 241, F15 = 242, F16 = 243, F17 = 244, F18 = 245, F19 = 246, F20 = 247, F21 = 248, F22 = 249, F23 = 250, F24 = 251
            }
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SerialButton))]
        private class SerialButtonDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                var mode = property.FindPropertyRelative("mode");
                var command = property.FindPropertyRelative("command");
                var keyCombo = property.FindPropertyRelative("keyCombo");

                position.height = EditorGUIUtility.singleLineHeight;
                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);

                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, mode);

                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    if ((SerialButton.ButtonMode)mode.enumValueIndex == SerialButton.ButtonMode.Serial)
                    {
                        if (command != null)
                            EditorGUI.PropertyField(position, command);
                    }
                    else
                    {
                        if (keyCombo != null)
                            EditorGUI.PropertyField(position, keyCombo, true);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUI.EndProperty();
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (!property.isExpanded)
                    return EditorGUIUtility.singleLineHeight;

                var mode = property.FindPropertyRelative("mode");

                float height = EditorGUIUtility.singleLineHeight;
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((SerialButton.ButtonMode)mode.enumValueIndex == SerialButton.ButtonMode.Keypress)
                {
                    var keyCombo = property.FindPropertyRelative("keyCombo");
                    height += (keyCombo != null ? EditorGUI.GetPropertyHeight(keyCombo, true) : EditorGUIUtility.singleLineHeight) + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                return height;
            }
        }
#endif
    }
}
