using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR && RECORDER_AVAILABLE
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
        static public void StartRecording(string recordingName, int framerate = 30)
        {
    #if UNITY_EDITOR && RECORDER_AVAILABLE
            if (m_RecorderController != null && m_RecorderController.IsRecording())
            {
                Debug.Log($"Stopping {m_RecorderController.Settings.name}.");
                StopRecording();
            }

            var mediaOutputFolder = Path.Combine(Application.dataPath, "..", "Recordings");
            mediaOutputFolder = Path.GetFullPath(Path.Combine(mediaOutputFolder, recordingName));
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
            
            Debug.Log($"Starting Recording: {mediaOutputFolder}");
            
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
    }
}
