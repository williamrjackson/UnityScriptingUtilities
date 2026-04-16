using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR && RECORDER_AVAILABLE
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

namespace Wrj
{
    public class ImageSequenceRecorder
    {
    #if UNITY_EDITOR && RECORDER_AVAILABLE
        static RecorderController m_RecorderController;
        static RecorderController recorderController
        {
            get
            {
                if (m_RecorderController == null)
                {
                    var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                    m_RecorderController = new RecorderController(controllerSettings);
                }
                return m_RecorderController;
            }
        }
    #endif
        static public string OutputPath => Path.Combine(Application.dataPath, "..", "Recordings");
        static public void StartRecording(string recordingName, int framerate = 30)
        {
    #if UNITY_EDITOR && RECORDER_AVAILABLE
            if (m_RecorderController != null && m_RecorderController.IsRecording())
            {
                StopRecording();
            }

            var mediaOutputFolder = Path.GetFullPath(Path.Combine(OutputPath, recordingName));
            // Image Sequence
            var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            imageRecorder.name = recordingName;
            imageRecorder.Enabled = true;

            imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
            imageRecorder.CaptureAlpha = false;
            imageRecorder.FrameRate = framerate;
            imageRecorder.FrameRatePlayback = FrameRatePlayback.Constant;
            imageRecorder.CapFrameRate = true;

            imageRecorder.OutputFile = Path.Combine(mediaOutputFolder, recordingName) + "_" + DefaultWildcard.Frame;

            imageRecorder.imageInputSettings = new GameViewInputSettings
            {
                OutputWidth = Screen.width,
                OutputHeight = Screen.height
            };

            // Setup Recording
            recorderController.Settings.name = recordingName;
            recorderController.Settings.FrameRate = framerate;
            recorderController.Settings.FrameRatePlayback = FrameRatePlayback.Constant;
            recorderController.Settings.CapFrameRate = true;
            recorderController.Settings.AddRecorderSettings(imageRecorder);
            recorderController.Settings.SetRecordModeToManual();
            RecorderOptions.VerboseMode = false;
                        
            try 
            {
                recorderController.PrepareRecording();
                recorderController.StartRecording();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
    #else
            Debug.LogError("Recorder package not found. Install it via Package Manager.");
    #endif
        }

        static public void StopRecording()
        {
    #if UNITY_EDITOR && RECORDER_AVAILABLE
            if (m_RecorderController != null && m_RecorderController.IsRecording())
            {
                m_RecorderController.StopRecording();
                m_RecorderController = null;
            }
    #endif
        }

    // #if UNITY_EDITOR && RECORDER_AVAILABLE
        static private string _recordName = string.Empty;
        static private bool isRecording => !_recordName.Equals(string.Empty);
        const string RecordingSlashLookalikeMenuPath = "Tools/Start\u2215Stop Recording";
#if UNITY_EDITOR_WIN
        const int CapsLockVirtualKeyCode = 0x14;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern short GetKeyState(int virtualKeyCode);
#endif

        [MenuItem(RecordingSlashLookalikeMenuPath)]
        static public void ToggleRecording()
        {
            if (isRecording)
            {
#if UNITY_EDITOR_WIN
                if (IsCapsLockOn())
                {
                    var outputPath = Path.Combine(OutputPath, _recordName);
                    System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath(outputPath));
                }
#endif
                StopFrameRecording();
            }
            else
            {
                StartFrameRecording();
            }
        }

        [MenuItem(RecordingSlashLookalikeMenuPath, validate = true)]
        static public bool ValidateToggleRecording()
        {
            Menu.SetChecked(RecordingSlashLookalikeMenuPath, isRecording);
            return EditorApplication.isPlaying;
        }

        static private bool IsCapsLockOn()
        {
#if UNITY_EDITOR_WIN
            return (GetKeyState(CapsLockVirtualKeyCode) & 1) != 0;
#else
            var currentEvent = Event.current;
            if (currentEvent == null)
            {
                return false;
            }

            var modifiers = currentEvent.modifiers;
            return (modifiers & EventModifiers.CapsLock) != 0;
#endif
        }

        static public void StartFrameRecording()
        {
            _recordName = $"Recording_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            StartRecording(recordingName: _recordName);
            string message = $"Started Recording: {_recordName}";
#if UNITY_EDITOR_WIN
            message += " (Enable Caps Lock to open destination folder when recording stops)";
#endif
            Debug.Log(message);
        }

        static public void StopFrameRecording()
        {
            StopRecording();
            _recordName = string.Empty;
        }
    // #endif
    }
}
