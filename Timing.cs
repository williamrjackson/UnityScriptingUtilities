using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

// /////////////////////////////////////////////////////////////////////////////////////////
//                              More Effective Coroutines
//                                        v3.06.2
// 
// This is an improved implementation of coroutines that boasts zero per-frame memory allocations,
// runs about twice as fast as Unity's built in coroutines and has a range of extra features.
// 
// This is the free version. MEC also has a pro version, which can be found here:
// https://www.assetstore.unity3d.com/en/#!/content/68480
// The pro version contains exactly the same core that the free version uses, but also
// contains additional features.
// 
// For manual, support, or upgrade guide visit http://trinary.tech/
//
// Created by Teal Rogers
// Trinary Software
// All rights preserved
// /////////////////////////////////////////////////////////////////////////////////////////

namespace MEC
{
    public class Timing : MonoBehaviour
    {
        /// <summary>
        /// The time between calls to SlowUpdate.
        /// </summary>
        [Tooltip("How quickly the SlowUpdate segment ticks.")]
        public float TimeBetweenSlowUpdateCalls = 1f / 7f;
        /// <summary>
        /// The amount that each coroutine should be seperated inside the Unity profiler. NOTE: When the profiler window
        /// is not open this value is ignored and all coroutines behave as if "None" is selected.
        /// </summary>
        [Tooltip("How much data should be sent to the profiler window when it's open.")]
        public DebugInfoType ProfilerDebugAmount;
        /// <summary>
        /// The number of coroutines that are being run in the Update segment.
        /// </summary>
        [Tooltip("A count of the number of Update coroutines that are currently running."), Space(12)]
        public int UpdateCoroutines;
        /// <summary>
        /// The number of coroutines that are being run in the FixedUpdate segment.
        /// </summary>
        [Tooltip("A count of the number of FixedUpdate coroutines that are currently running.")]
        public int FixedUpdateCoroutines;
        /// <summary>
        /// The number of coroutines that are being run in the LateUpdate segment.
        /// </summary>
        [Tooltip("A count of the number of LateUpdate coroutines that are currently running.")]
        public int LateUpdateCoroutines;
        /// <summary>
        /// The number of coroutines that are being run in the SlowUpdate segment.
        /// </summary>
        [Tooltip("A count of the number of SlowUpdate coroutines that are currently running.")]
        public int SlowUpdateCoroutines;
        /// <summary>
        /// The time in seconds that the current segment has been running.
        /// </summary>
        [System.NonSerialized]
        public float localTime;
        /// <summary>
        /// The time in seconds that the current segment has been running.
        /// </summary>
        public static float LocalTime { get { return Instance.localTime; } }
        /// <summary>
        /// The amount of time in fractional seconds that elapsed between this frame and the last frame.
        /// </summary>
        [System.NonSerialized]
        public float deltaTime;
        /// <summary>
        /// The amount of time in fractional seconds that elapsed between this frame and the last frame.
        /// </summary>
        public static float DeltaTime { get { return Instance.deltaTime; } }
        /// <summary>
        /// Used for advanced coroutine control.
        /// </summary>
        public static System.Func<IEnumerator<float>, CoroutineHandle, IEnumerator<float>> ReplacementFunction;
        /// <summary>
        /// This event fires just before each segment is run.
        /// </summary>
        public static event System.Action OnPreExecute;
        /// <summary>
        /// You can use "yield return Timing.WaitForOneFrame;" inside a coroutine function to go to the next frame. 
        /// </summary>
        public const float WaitForOneFrame = float.NegativeInfinity;
        /// <summary>
        /// The main thread that (almost) everything in unity runs in.
        /// </summary>
        public static System.Threading.Thread MainThread { get; private set; }
        /// <summary>
        /// The handle of the current coroutine that is running.
        /// </summary>
        public static CoroutineHandle CurrentCoroutine
        {
            get
            {
                for (int i = 1; i < 16; i++)
                    if (ActiveInstances[i] != null && ActiveInstances[i]._currentCoroutine.IsValid)
                        return ActiveInstances[i]._currentCoroutine;
                return default(CoroutineHandle);
            }
        }
        /// <summary>
        /// The handle of the current coroutine that is running.
        /// </summary>
        public CoroutineHandle currentCoroutine { get { return _currentCoroutine; } }
        private CoroutineHandle _currentCoroutine;

        private static object _tmpRef;
        //private static bool _tmpBool;
        //private static CoroutineHandle _tmpHandle;

        private int _currentUpdateFrame;
        private int _currentLateUpdateFrame;
        private int _currentSlowUpdateFrame;
        private int _nextUpdateProcessSlot;
        private int _nextLateUpdateProcessSlot;
        private int _nextFixedUpdateProcessSlot;
        private int _nextSlowUpdateProcessSlot;
        private int _lastUpdateProcessSlot;
        private int _lastLateUpdateProcessSlot;
        private int _lastFixedUpdateProcessSlot;
        private int _lastSlowUpdateProcessSlot;
        private float _lastUpdateTime;
        private float _lastLateUpdateTime;
        private float _lastFixedUpdateTime;
        private float _lastSlowUpdateTime;
        private float _lastSlowUpdateDeltaTime;
        private ushort _framesSinceUpdate;
        private ushort _expansions = 1;
        private byte _instanceID;

        private readonly Dictionary<CoroutineHandle, HashSet<CoroutineHandle>> _waitingTriggers = new Dictionary<CoroutineHandle, HashSet<CoroutineHandle>>();
        private readonly HashSet<CoroutineHandle> _allWaiting = new HashSet<CoroutineHandle>();
        private readonly Dictionary<CoroutineHandle, ProcessIndex> _handleToIndex = new Dictionary<CoroutineHandle, ProcessIndex>();
        private readonly Dictionary<ProcessIndex, CoroutineHandle> _indexToHandle = new Dictionary<ProcessIndex, CoroutineHandle>();
        private readonly Dictionary<CoroutineHandle, string> _processTags = new Dictionary<CoroutineHandle, string>();
        private readonly Dictionary<string, HashSet<CoroutineHandle>> _taggedProcesses = new Dictionary<string, HashSet<CoroutineHandle>>();

        private IEnumerator<float>[] UpdateProcesses = new IEnumerator<float>[InitialBufferSizeLarge];
        private IEnumerator<float>[] LateUpdateProcesses = new IEnumerator<float>[InitialBufferSizeSmall];
        private IEnumerator<float>[] FixedUpdateProcesses = new IEnumerator<float>[InitialBufferSizeMedium];
        private IEnumerator<float>[] SlowUpdateProcesses = new IEnumerator<float>[InitialBufferSizeMedium];

        private bool[] UpdatePaused = new bool[InitialBufferSizeLarge];
        private bool[] LateUpdatePaused = new bool[InitialBufferSizeSmall];
        private bool[] FixedUpdatePaused = new bool[InitialBufferSizeMedium];
        private bool[] SlowUpdatePaused = new bool[InitialBufferSizeMedium];
        private bool[] UpdateHeld = new bool[InitialBufferSizeLarge];
        private bool[] LateUpdateHeld = new bool[InitialBufferSizeSmall];
        private bool[] FixedUpdateHeld = new bool[InitialBufferSizeMedium];
        private bool[] SlowUpdateHeld = new bool[InitialBufferSizeMedium];

        private const ushort FramesUntilMaintenance = 64;
        private const int ProcessArrayChunkSize = 64;
        private const int InitialBufferSizeLarge = 256;
        private const int InitialBufferSizeMedium = 64;
        private const int InitialBufferSizeSmall = 8;

        private static Timing[] ActiveInstances = new Timing[16];
        private static Timing _instance;

        public static Timing Instance
        {
            get
            {
                if (_instance == null || !_instance.gameObject)
                {
                    GameObject instanceHome = GameObject.Find("Timing Controller");

                    if (instanceHome == null)
                    {
                        instanceHome = new GameObject { name = "Timing Controller" };

                        DontDestroyOnLoad(instanceHome);
                    }

                    _instance = instanceHome.GetComponent<Timing>() ?? instanceHome.AddComponent<Timing>();

                    _instance.InitializeInstanceID();
                }

                return _instance;
            }
            set { _instance = value; }
        }

        void Awake()
        {
            if(_instance == null)
                _instance = this;
            else
                deltaTime = _instance.deltaTime;

            if (MainThread == null)
                MainThread = System.Threading.Thread.CurrentThread;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void OnEnable()
        {
            InitializeInstanceID();
        }

        void OnDisable()
        {
            if (_instanceID < ActiveInstances.Length)
                ActiveInstances[_instanceID] = null;
        }

        private void InitializeInstanceID()
        {
            if (ActiveInstances[_instanceID] == null)
            {
                if (_instanceID == 0x00)
                    _instanceID++;

                for (; _instanceID <= 0x10; _instanceID++)
                {
                    if (_instanceID == 0x10)
                    {
                        GameObject.Destroy(gameObject);
                        throw new System.OverflowException("You are only allowed 15 different contexts for MEC to run inside at one time.");
                    }

                    if (ActiveInstances[_instanceID] == null)
                    {
                        ActiveInstances[_instanceID] = this;
                        break;
                    }
                }
            }
        }

        void Update()
        {
            if (OnPreExecute != null)
                OnPreExecute();

            if (_lastSlowUpdateTime + TimeBetweenSlowUpdateCalls < Time.realtimeSinceStartup && _nextSlowUpdateProcessSlot > 0)
            {
                ProcessIndex coindex = new ProcessIndex { seg = Segment.SlowUpdate };
                if (UpdateTimeValues(coindex.seg))
                    _lastSlowUpdateProcessSlot = _nextSlowUpdateProcessSlot;

                for (coindex.i = 0; coindex.i < _lastSlowUpdateProcessSlot; coindex.i++)
                {
                    try
                    {
                        if (!SlowUpdatePaused[coindex.i] && !SlowUpdateHeld[coindex.i] && SlowUpdateProcesses[coindex.i] != null && !(localTime < SlowUpdateProcesses[coindex.i].Current))
                        {
                            _currentCoroutine = _indexToHandle[coindex];

                            if (ProfilerDebugAmount != DebugInfoType.None && _indexToHandle.ContainsKey(coindex))
                            {
                                Profiler.BeginSample(ProfilerDebugAmount == DebugInfoType.SeperateTags ? ("Processing Coroutine (Slow Update)" +
                                        (_processTags.ContainsKey(_indexToHandle[coindex]) ? ", tag " + _processTags[_indexToHandle[coindex]] : ", no tag"))
                                        : "Processing Coroutine (Slow Update)");
                            }

                            if (!SlowUpdateProcesses[coindex.i].MoveNext())
                            {
                                if (_indexToHandle.ContainsKey(coindex))
                                    KillCoroutinesOnInstance(_indexToHandle[coindex]);
                            }
                            else if (SlowUpdateProcesses[coindex.i] != null && float.IsNaN(SlowUpdateProcesses[coindex.i].Current))
                            {
                                if (ReplacementFunction != null)
                                {
                                    SlowUpdateProcesses[coindex.i] = ReplacementFunction(SlowUpdateProcesses[coindex.i], _indexToHandle[coindex]);
                                    ReplacementFunction = null;
                                }
                                coindex.i--;
                            }

                            if (ProfilerDebugAmount != DebugInfoType.None)
                                Profiler.EndSample();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            if (_nextUpdateProcessSlot > 0)
            {
                ProcessIndex coindex = new ProcessIndex { seg = Segment.Update };
                if (UpdateTimeValues(coindex.seg))
                    _lastUpdateProcessSlot = _nextUpdateProcessSlot;

                for (coindex.i = 0; coindex.i < _lastUpdateProcessSlot; coindex.i++)
                {
                    try
                    {
                        if (!UpdatePaused[coindex.i] && !UpdateHeld[coindex.i] && UpdateProcesses[coindex.i] != null && !(localTime < UpdateProcesses[coindex.i].Current))
                        {
                            _currentCoroutine = _indexToHandle[coindex];

                            if (ProfilerDebugAmount != DebugInfoType.None && _indexToHandle.ContainsKey(coindex))
                            {
                                Profiler.BeginSample(ProfilerDebugAmount == DebugInfoType.SeperateTags ? ("Processing Coroutine" +
                                        (_processTags.ContainsKey(_indexToHandle[coindex]) ? ", tag " + _processTags[_indexToHandle[coindex]] : ", no tag"))
                                        : "Processing Coroutine");
                            }

                            if (!UpdateProcesses[coindex.i].MoveNext())
                            {
                                if (_indexToHandle.ContainsKey(coindex))
                                    KillCoroutinesOnInstance(_indexToHandle[coindex]);
                            }
                            else if (UpdateProcesses[coindex.i] != null && float.IsNaN(UpdateProcesses[coindex.i].Current))
                            {
                                if (ReplacementFunction != null)
                                {
                                    UpdateProcesses[coindex.i] = ReplacementFunction(UpdateProcesses[coindex.i], _indexToHandle[coindex]);
                                    ReplacementFunction = null;
                                }
                                coindex.i--;
                            }

                            if (ProfilerDebugAmount != DebugInfoType.None)
                                Profiler.EndSample();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            _currentCoroutine = default(CoroutineHandle);

            if(++_framesSinceUpdate > FramesUntilMaintenance)
            {
                _framesSinceUpdate = 0;

                if (ProfilerDebugAmount != DebugInfoType.None)
                    Profiler.BeginSample("Maintenance Task");

                RemoveUnused();

                if (ProfilerDebugAmount != DebugInfoType.None)
                    Profiler.EndSample();
            }
        }

        void FixedUpdate()
        {
            if (OnPreExecute != null)
                OnPreExecute();

            if (_nextFixedUpdateProcessSlot > 0)
            {
                ProcessIndex coindex = new ProcessIndex { seg = Segment.FixedUpdate };
                if (UpdateTimeValues(coindex.seg))
                    _lastFixedUpdateProcessSlot = _nextFixedUpdateProcessSlot;

                for (coindex.i = 0; coindex.i < _lastFixedUpdateProcessSlot; coindex.i++)
                {
                    try
                    {
                        if (!FixedUpdatePaused[coindex.i] && !FixedUpdateHeld[coindex.i] && FixedUpdateProcesses[coindex.i] != null && !(localTime < FixedUpdateProcesses[coindex.i].Current))
                        {
                            _currentCoroutine = _indexToHandle[coindex];


                            if (ProfilerDebugAmount != DebugInfoType.None && _indexToHandle.ContainsKey(coindex))
                            {
                                Profiler.BeginSample(ProfilerDebugAmount == DebugInfoType.SeperateTags ? ("Processing Coroutine" +
                                        (_processTags.ContainsKey(_indexToHandle[coindex]) ? ", tag " + _processTags[_indexToHandle[coindex]] : ", no tag"))
                                        : "Processing Coroutine");
                            }

                            if (!FixedUpdateProcesses[coindex.i].MoveNext())
                            {
                                if (_indexToHandle.ContainsKey(coindex))
                                    KillCoroutinesOnInstance(_indexToHandle[coindex]);
                            }
                            else if (FixedUpdateProcesses[coindex.i] != null && float.IsNaN(FixedUpdateProcesses[coindex.i].Current))
                            {
                                if (ReplacementFunction != null)
                                {
                                    FixedUpdateProcesses[coindex.i] = ReplacementFunction(FixedUpdateProcesses[coindex.i], _indexToHandle[coindex]);
                                    ReplacementFunction = null;
                                }
                                coindex.i--;
                            }

                            if (ProfilerDebugAmount != DebugInfoType.None)
                                Profiler.EndSample();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                _currentCoroutine = default(CoroutineHandle);
            }
        }

        void LateUpdate()
        {
            if (OnPreExecute != null)
                OnPreExecute();

            if (_nextLateUpdateProcessSlot > 0)
            {
                ProcessIndex coindex = new ProcessIndex { seg = Segment.LateUpdate };
                if (UpdateTimeValues(coindex.seg))
                    _lastLateUpdateProcessSlot = _nextLateUpdateProcessSlot;

                for (coindex.i = 0; coindex.i < _lastLateUpdateProcessSlot; coindex.i++)
                {
                    try
                    {
                        if (!LateUpdatePaused[coindex.i] && !LateUpdateHeld[coindex.i] && LateUpdateProcesses[coindex.i] != null && !(localTime < LateUpdateProcesses[coindex.i].Current))
                        {
                            _currentCoroutine = _indexToHandle[coindex];


                            if (ProfilerDebugAmount != DebugInfoType.None && _indexToHandle.ContainsKey(coindex))
                            {
                                Profiler.BeginSample(ProfilerDebugAmount == DebugInfoType.SeperateTags ? ("Processing Coroutine" +
                                        (_processTags.ContainsKey(_indexToHandle[coindex]) ? ", tag " + _processTags[_indexToHandle[coindex]] : ", no tag"))
                                        : "Processing Coroutine");
                            }

                            if (!LateUpdateProcesses[coindex.i].MoveNext())
                            {
                                if (_indexToHandle.ContainsKey(coindex))
                                    KillCoroutinesOnInstance(_indexToHandle[coindex]);
                            }
                            else if (LateUpdateProcesses[coindex.i] != null && float.IsNaN(LateUpdateProcesses[coindex.i].Current))
                            {
                                if (ReplacementFunction != null)
                                {
                                    LateUpdateProcesses[coindex.i] = ReplacementFunction(LateUpdateProcesses[coindex.i], _indexToHandle[coindex]);
                                    ReplacementFunction = null;
                                }
                                coindex.i--;
                            }

                            if (ProfilerDebugAmount != DebugInfoType.None)
                                Profiler.EndSample();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
                _currentCoroutine = default(CoroutineHandle);
            }
        }

        private void RemoveUnused()
        {
            var waitTrigsEnum = _waitingTriggers.GetEnumerator();
            while (waitTrigsEnum.MoveNext())
            {
                if (waitTrigsEnum.Current.Value.Count == 0)
                {
                    _waitingTriggers.Remove(waitTrigsEnum.Current.Key);
                    waitTrigsEnum = _waitingTriggers.GetEnumerator();
                    continue;
                }

                if (_handleToIndex.ContainsKey(waitTrigsEnum.Current.Key) && CoindexIsNull(_handleToIndex[waitTrigsEnum.Current.Key]))
                {
                    CloseWaitingProcess(waitTrigsEnum.Current.Key);
                    waitTrigsEnum = _waitingTriggers.GetEnumerator();
                }
            }

            ProcessIndex outer, inner;
            outer.seg = inner.seg = Segment.Update;

            for (outer.i = inner.i = 0; outer.i < _nextUpdateProcessSlot; outer.i++)
            {
                if (UpdateProcesses[outer.i] != null)
                {
                    if (outer.i != inner.i)
                    {
                        UpdateProcesses[inner.i] = UpdateProcesses[outer.i];
                        UpdatePaused[inner.i] = UpdatePaused[outer.i];
                        UpdateHeld[inner.i] = UpdateHeld[outer.i];

                        if (_indexToHandle.ContainsKey(inner))
                        {
                            RemoveTag(_indexToHandle[inner]);
                            _handleToIndex.Remove(_indexToHandle[inner]);
                            _indexToHandle.Remove(inner);
                        }

                        _handleToIndex[_indexToHandle[outer]] = inner;
                        _indexToHandle.Add(inner, _indexToHandle[outer]);
                        _indexToHandle.Remove(outer);
                    }
                    inner.i++;
                }
            }
            for (outer.i = inner.i; outer.i < _nextUpdateProcessSlot; outer.i++)
            {
                UpdateProcesses[outer.i] = null;
                UpdatePaused[outer.i] = false;
                UpdateHeld[outer.i] = false;

                if (_indexToHandle.ContainsKey(outer))
                {
                    RemoveTag(_indexToHandle[outer]);

                    _handleToIndex.Remove(_indexToHandle[outer]);
                    _indexToHandle.Remove(outer);
                }
            }

            _lastUpdateProcessSlot -= _nextUpdateProcessSlot - inner.i;
            UpdateCoroutines = _nextUpdateProcessSlot = inner.i;

            outer.seg = inner.seg = Segment.FixedUpdate;
            for (outer.i = inner.i = 0; outer.i < _nextFixedUpdateProcessSlot; outer.i++)
            {
                if (FixedUpdateProcesses[outer.i] != null)
                {
                    if (outer.i != inner.i)
                    {
                        FixedUpdateProcesses[inner.i] = FixedUpdateProcesses[outer.i];
                        FixedUpdatePaused[inner.i] = FixedUpdatePaused[outer.i];
                        FixedUpdateHeld[inner.i] = FixedUpdateHeld[outer.i];

                        if (_indexToHandle.ContainsKey(inner))
                        {
                            RemoveTag(_indexToHandle[inner]);
                            _handleToIndex.Remove(_indexToHandle[inner]);
                            _indexToHandle.Remove(inner);
                        }

                        _handleToIndex[_indexToHandle[outer]] = inner;
                        _indexToHandle.Add(inner, _indexToHandle[outer]);
                        _indexToHandle.Remove(outer);
                    }
                    inner.i++;
                }
            }
            for (outer.i = inner.i; outer.i < _nextFixedUpdateProcessSlot; outer.i++)
            {
                FixedUpdateProcesses[outer.i] = null;
                FixedUpdatePaused[outer.i] = false;
                FixedUpdateHeld[outer.i] = false;

                if (_indexToHandle.ContainsKey(outer))
                {
                    RemoveTag(_indexToHandle[outer]);

                    _handleToIndex.Remove(_indexToHandle[outer]);
                    _indexToHandle.Remove(outer);
                }
            }

            _lastFixedUpdateProcessSlot -= _nextFixedUpdateProcessSlot - inner.i;
            FixedUpdateCoroutines = _nextFixedUpdateProcessSlot = inner.i;

            outer.seg = inner.seg = Segment.LateUpdate;
            for (outer.i = inner.i = 0; outer.i < _nextLateUpdateProcessSlot; outer.i++)
            {
                if (LateUpdateProcesses[outer.i] != null)
                {
                    if (outer.i != inner.i)
                    {
                        LateUpdateProcesses[inner.i] = LateUpdateProcesses[outer.i];
                        LateUpdatePaused[inner.i] = LateUpdatePaused[outer.i];
                        LateUpdateHeld[inner.i] = LateUpdateHeld[outer.i];

                        if (_indexToHandle.ContainsKey(inner))
                        {
                            RemoveTag(_indexToHandle[inner]);
                            _handleToIndex.Remove(_indexToHandle[inner]);
                            _indexToHandle.Remove(inner);
                        }

                        _handleToIndex[_indexToHandle[outer]] = inner;
                        _indexToHandle.Add(inner, _indexToHandle[outer]);
                        _indexToHandle.Remove(outer);
                    }
                    inner.i++;
                }
            }
            for (outer.i = inner.i; outer.i < _nextLateUpdateProcessSlot; outer.i++)
            {
                LateUpdateProcesses[outer.i] = null;
                LateUpdatePaused[outer.i] = false;
                LateUpdateHeld[outer.i] = false;

                if (_indexToHandle.ContainsKey(outer))
                {
                    RemoveTag(_indexToHandle[outer]);

                    _handleToIndex.Remove(_indexToHandle[outer]);
                    _indexToHandle.Remove(outer);
                }
            }

            _lastLateUpdateProcessSlot -= _nextLateUpdateProcessSlot - inner.i;
            LateUpdateCoroutines = _nextLateUpdateProcessSlot = inner.i;

            outer.seg = inner.seg = Segment.SlowUpdate;
            for (outer.i = inner.i = 0; outer.i < _nextSlowUpdateProcessSlot; outer.i++)
            {
                if (SlowUpdateProcesses[outer.i] != null)
                {
                    if (outer.i != inner.i)
                    {
                        SlowUpdateProcesses[inner.i] = SlowUpdateProcesses[outer.i];
                        SlowUpdatePaused[inner.i] = SlowUpdatePaused[outer.i];
                        SlowUpdateHeld[inner.i] = SlowUpdateHeld[outer.i];

                        if (_indexToHandle.ContainsKey(inner))
                        {
                            RemoveTag(_indexToHandle[inner]);
                            _handleToIndex.Remove(_indexToHandle[inner]);
                            _indexToHandle.Remove(inner);
                        }

                        _handleToIndex[_indexToHandle[outer]] = inner;
                        _indexToHandle.Add(inner, _indexToHandle[outer]);
                        _indexToHandle.Remove(outer);
                    }
                    inner.i++;
                }
            }
            for (outer.i = inner.i; outer.i < _nextSlowUpdateProcessSlot; outer.i++)
            {
                SlowUpdateProcesses[outer.i] = null;
                SlowUpdatePaused[outer.i] = false;
                SlowUpdateHeld[outer.i] = false;

                if (_indexToHandle.ContainsKey(outer))
                {
                    RemoveTag(_indexToHandle[outer]);

                    _handleToIndex.Remove(_indexToHandle[outer]);
                    _indexToHandle.Remove(outer);
                }
            }

            _lastSlowUpdateProcessSlot -= _nextSlowUpdateProcessSlot - inner.i;
            SlowUpdateCoroutines = _nextSlowUpdateProcessSlot = inner.i;
        }

        /// <summary>
        /// Run a new coroutine in the Update segment.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public static CoroutineHandle RunCoroutine(IEnumerator<float> coroutine)
        {
            return coroutine == null ? new CoroutineHandle()
                : Instance.RunCoroutineInternal(coroutine, Segment.Update, null, new CoroutineHandle(Instance._instanceID), true);
        }

        /// <summary>
        /// Run a new coroutine in the Update segment.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="tag">An optional tag to attach to the coroutine which can later be used for Kill operations.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public static CoroutineHandle RunCoroutine(IEnumerator<float> coroutine, string tag)
        {
            return coroutine == null ? new CoroutineHandle()
                : Instance.RunCoroutineInternal(coroutine, Segment.Update, tag, new CoroutineHandle(Instance._instanceID), true);
        }

        /// <summary>
        /// Run a new coroutine.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="segment">The segment that the coroutine should run in.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public static CoroutineHandle RunCoroutine(IEnumerator<float> coroutine, Segment segment)
        {
            return coroutine == null ? new CoroutineHandle()
                : Instance.RunCoroutineInternal(coroutine, segment, null, new CoroutineHandle(Instance._instanceID), true);
        }

        /// <summary>
        /// Run a new coroutine.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="segment">The segment that the coroutine should run in.</param>
        /// <param name="tag">An optional tag to attach to the coroutine which can later be used for Kill operations.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public static CoroutineHandle RunCoroutine(IEnumerator<float> coroutine, Segment segment, string tag)
        {
            return coroutine == null ? new CoroutineHandle()
                : Instance.RunCoroutineInternal(coroutine, segment, tag, new CoroutineHandle(Instance._instanceID), true);
        }

        /// <summary>
        /// Run a new coroutine on this Timing instance in the Update segment.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public CoroutineHandle RunCoroutineOnInstance(IEnumerator<float> coroutine)
        {
            return coroutine == null ? new CoroutineHandle()
                 : RunCoroutineInternal(coroutine, Segment.Update, null, new CoroutineHandle(_instanceID), true);
        }

        /// <summary>
        /// Run a new coroutine on this Timing instance in the Update segment.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="tag">An optional tag to attach to the coroutine which can later be used for Kill operations.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public CoroutineHandle RunCoroutineOnInstance(IEnumerator<float> coroutine, string tag)
        {
            return coroutine == null ? new CoroutineHandle()
                 : RunCoroutineInternal(coroutine, Segment.Update, tag, new CoroutineHandle(_instanceID), true);
        }

        /// <summary>
        /// Run a new coroutine on this Timing instance.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="segment">The segment that the coroutine should run in.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public CoroutineHandle RunCoroutineOnInstance(IEnumerator<float> coroutine, Segment segment)
        {
            return coroutine == null ? new CoroutineHandle()
                 : RunCoroutineInternal(coroutine, segment, null, new CoroutineHandle(_instanceID), true);
        }

        /// <summary>
        /// Run a new coroutine on this Timing instance.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="segment">The segment that the coroutine should run in.</param>
        /// <param name="tag">An optional tag to attach to the coroutine which can later be used for Kill operations.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public CoroutineHandle RunCoroutineOnInstance(IEnumerator<float> coroutine, Segment segment, string tag)
        {
            return coroutine == null ? new CoroutineHandle()
                 : RunCoroutineInternal(coroutine, segment, tag, new CoroutineHandle(_instanceID), true);
        }


        private CoroutineHandle RunCoroutineInternal(IEnumerator<float> coroutine, Segment segment, string tag, CoroutineHandle handle, bool prewarm)
        {
            ProcessIndex slot = new ProcessIndex { seg = segment };

            if (_handleToIndex.ContainsKey(handle))
            {
                _indexToHandle.Remove(_handleToIndex[handle]);
                _handleToIndex.Remove(handle);
            }

            float currentLocalTime = localTime;
            float currentDeltaTime = deltaTime;
            CoroutineHandle cachedHandle = _currentCoroutine;
            _currentCoroutine = handle;

            switch (segment)
            {
                case Segment.Update:

                    if (_nextUpdateProcessSlot >= UpdateProcesses.Length)
                    {
                        IEnumerator<float>[] oldProcArray = UpdateProcesses;
                        bool[] oldPausedArray = UpdatePaused;
                        bool[] oldHeldArray = UpdateHeld;

                        UpdateProcesses = new IEnumerator<float>[UpdateProcesses.Length + (ProcessArrayChunkSize * _expansions++)];
                        UpdatePaused = new bool[UpdateProcesses.Length];
                        UpdateHeld = new bool[UpdateProcesses.Length];

                        for (int i = 0; i < oldProcArray.Length; i++)
                        {
                            UpdateProcesses[i] = oldProcArray[i];
                            UpdatePaused[i] = oldPausedArray[i];
                            UpdateHeld[i] = oldHeldArray[i];
                        }
                    }

                    if (UpdateTimeValues(slot.seg))
                        _lastUpdateProcessSlot = _nextUpdateProcessSlot;

                    slot.i = _nextUpdateProcessSlot++;
                    UpdateProcesses[slot.i] = coroutine;

                    if (null != tag)
                        AddTag(tag, handle);

                    _indexToHandle.Add(slot, handle);
                    _handleToIndex.Add(handle, slot);

                    while (prewarm)
                    {
                        if (!UpdateProcesses[slot.i].MoveNext())
                        {
                            if (_indexToHandle.ContainsKey(slot))
                                KillCoroutinesOnInstance(_indexToHandle[slot]);

                            prewarm = false;
                        }
                        else if (UpdateProcesses[slot.i] != null && float.IsNaN(UpdateProcesses[slot.i].Current))
                        {
                            if (ReplacementFunction != null)
                            {
                                UpdateProcesses[slot.i] = ReplacementFunction(UpdateProcesses[slot.i], _indexToHandle[slot]);
                                ReplacementFunction = null;
                            }
                            prewarm = !UpdatePaused[slot.i] || !UpdateHeld[slot.i];
                        }
                        else
                        {
                            prewarm = false;
                        }
                    }

                    break;

                case Segment.FixedUpdate:

                    if (_nextFixedUpdateProcessSlot >= FixedUpdateProcesses.Length)
                    {
                        IEnumerator<float>[] oldProcArray = FixedUpdateProcesses;
                        bool[] oldPausedArray = FixedUpdatePaused;
                        bool[] oldHeldArray = FixedUpdateHeld;

                        FixedUpdateProcesses = new IEnumerator<float>[FixedUpdateProcesses.Length + (ProcessArrayChunkSize * _expansions++)];
                        FixedUpdatePaused = new bool[FixedUpdateProcesses.Length];
                        FixedUpdateHeld = new bool[FixedUpdateProcesses.Length];

                        for (int i = 0; i < oldProcArray.Length; i++)
                        {
                            FixedUpdateProcesses[i] = oldProcArray[i];
                            FixedUpdatePaused[i] = oldPausedArray[i];
                            FixedUpdateHeld[i] = oldHeldArray[i];
                        }
                    }

                    if (UpdateTimeValues(slot.seg))
                        _lastFixedUpdateProcessSlot = _nextFixedUpdateProcessSlot;

                    slot.i = _nextFixedUpdateProcessSlot++;
                    FixedUpdateProcesses[slot.i] = coroutine;

                    if (null != tag)
                        AddTag(tag, handle);

                    _indexToHandle.Add(slot, handle);
                    _handleToIndex.Add(handle, slot);

                    while (prewarm)
                    {
                        if (!FixedUpdateProcesses[slot.i].MoveNext())
                        {
                            if (_indexToHandle.ContainsKey(slot))
                                KillCoroutinesOnInstance(_indexToHandle[slot]);

                            prewarm = false;
                        }
                        else if (FixedUpdateProcesses[slot.i] != null && float.IsNaN(FixedUpdateProcesses[slot.i].Current))
                        {
                            if (ReplacementFunction != null)
                            {
                                FixedUpdateProcesses[slot.i] = ReplacementFunction(FixedUpdateProcesses[slot.i], _indexToHandle[slot]);
                                ReplacementFunction = null;
                            }
                            prewarm = !FixedUpdatePaused[slot.i] || !FixedUpdateHeld[slot.i];
                        }
                        else
                        {
                            prewarm = false;
                        }
                    }

                    break;

                case Segment.LateUpdate:

                    if (_nextLateUpdateProcessSlot >= LateUpdateProcesses.Length)
                    {
                        IEnumerator<float>[] oldProcArray = LateUpdateProcesses;
                        bool[] oldPausedArray = LateUpdatePaused;
                        bool[] oldHeldArray = LateUpdateHeld;

                        LateUpdateProcesses = new IEnumerator<float>[LateUpdateProcesses.Length + (ProcessArrayChunkSize * _expansions++)];
                        LateUpdatePaused = new bool[LateUpdateProcesses.Length];
                        LateUpdateHeld = new bool[LateUpdateProcesses.Length];

                        for (int i = 0; i < oldProcArray.Length; i++)
                        {
                            LateUpdateProcesses[i] = oldProcArray[i];
                            LateUpdatePaused[i] = oldPausedArray[i];
                            LateUpdateHeld[i] = oldHeldArray[i];
                        }
                    }

                    if (UpdateTimeValues(slot.seg))
                        _lastLateUpdateProcessSlot = _nextLateUpdateProcessSlot;

                    slot.i = _nextLateUpdateProcessSlot++;
                    LateUpdateProcesses[slot.i] = coroutine;

                    if (tag != null)
                        AddTag(tag, handle);

                    _indexToHandle.Add(slot, handle);
                    _handleToIndex.Add(handle, slot);

                    while (prewarm)
                    {
                        if (!LateUpdateProcesses[slot.i].MoveNext())
                        {
                            if (_indexToHandle.ContainsKey(slot))
                                KillCoroutinesOnInstance(_indexToHandle[slot]);

                            prewarm = false;
                        }
                        else if (LateUpdateProcesses[slot.i] != null && float.IsNaN(LateUpdateProcesses[slot.i].Current))
                        {
                            if (ReplacementFunction != null)
                            {
                                LateUpdateProcesses[slot.i] = ReplacementFunction(LateUpdateProcesses[slot.i], _indexToHandle[slot]);
                                ReplacementFunction = null;
                            }
                            prewarm = !LateUpdatePaused[slot.i] || !LateUpdateHeld[slot.i];
                        }
                        else
                        {
                            prewarm = false;
                        }
                    }

                    break;

                case Segment.SlowUpdate:

                    if (_nextSlowUpdateProcessSlot >= SlowUpdateProcesses.Length)
                    {
                        IEnumerator<float>[] oldProcArray = SlowUpdateProcesses;
                        bool[] oldPausedArray = SlowUpdatePaused;
                        bool[] oldHeldArray = SlowUpdateHeld;

                        SlowUpdateProcesses = new IEnumerator<float>[SlowUpdateProcesses.Length + (ProcessArrayChunkSize * _expansions++)];
                        SlowUpdatePaused = new bool[SlowUpdateProcesses.Length];
                        SlowUpdateHeld = new bool[SlowUpdateProcesses.Length];

                        for (int i = 0; i < oldProcArray.Length; i++)
                        {
                            SlowUpdateProcesses[i] = oldProcArray[i];
                            SlowUpdatePaused[i] = oldPausedArray[i];
                            SlowUpdateHeld[i] = oldHeldArray[i];
                        }
                    }

                    if (UpdateTimeValues(slot.seg))
                        _lastSlowUpdateProcessSlot = _nextSlowUpdateProcessSlot;

                    slot.i = _nextSlowUpdateProcessSlot++;
                    SlowUpdateProcesses[slot.i] = coroutine;

                    if (tag != null)
                        AddTag(tag, handle);

                    _indexToHandle.Add(slot, handle);
                    _handleToIndex.Add(handle, slot);

                    while (prewarm)
                    {
                        if (!SlowUpdateProcesses[slot.i].MoveNext())
                        {
                            if (_indexToHandle.ContainsKey(slot))
                                KillCoroutinesOnInstance(_indexToHandle[slot]);

                            prewarm = false;
                        }
                        else if (SlowUpdateProcesses[slot.i] != null && float.IsNaN(SlowUpdateProcesses[slot.i].Current))
                        {
                            if (ReplacementFunction != null)
                            {
                                SlowUpdateProcesses[slot.i] = ReplacementFunction(SlowUpdateProcesses[slot.i], _indexToHandle[slot]);
                                ReplacementFunction = null;
                            }
                            prewarm = !SlowUpdatePaused[slot.i] || !SlowUpdateHeld[slot.i];
                        }
                        else
                        {
                            prewarm = false;
                        }
                    }

                    break;

                default:
                    handle = new CoroutineHandle();
                    break;
            }

            localTime = currentLocalTime;
            deltaTime = currentDeltaTime;
            _currentCoroutine = cachedHandle;

            return handle;
        }

        /// <summary>
        /// This will kill all coroutines running on the main MEC instance and reset the context.
        /// </summary>
        /// <returns>The number of coroutines that were killed.</returns>
        public static int KillCoroutines()
        {
            return _instance == null ? 0 : _instance.KillCoroutinesOnInstance();
        }

        /// <summary>
        /// This will kill all coroutines running on the current MEC instance and reset the context.
        /// </summary>
        /// <returns>The number of coroutines that were killed.</returns>
        public int KillCoroutinesOnInstance()
        {
            int retVal = _nextUpdateProcessSlot + _nextLateUpdateProcessSlot + _nextFixedUpdateProcessSlot + _nextSlowUpdateProcessSlot;

            UpdateProcesses = new IEnumerator<float>[InitialBufferSizeLarge];
            UpdatePaused = new bool[InitialBufferSizeLarge];
            UpdateHeld = new bool[InitialBufferSizeLarge];
            UpdateCoroutines = 0;
            _nextUpdateProcessSlot = 0;

            LateUpdateProcesses = new IEnumerator<float>[InitialBufferSizeSmall];
            LateUpdatePaused = new bool[InitialBufferSizeSmall];
            LateUpdateHeld = new bool[InitialBufferSizeSmall];
            LateUpdateCoroutines = 0;
            _nextLateUpdateProcessSlot = 0;

            FixedUpdateProcesses = new IEnumerator<float>[InitialBufferSizeMedium];
            FixedUpdatePaused = new bool[InitialBufferSizeMedium];
            FixedUpdateHeld = new bool[InitialBufferSizeMedium];
            FixedUpdateCoroutines = 0;
            _nextFixedUpdateProcessSlot = 0;

            SlowUpdateProcesses = new IEnumerator<float>[InitialBufferSizeMedium];
            SlowUpdatePaused = new bool[InitialBufferSizeMedium];
            SlowUpdateHeld = new bool[InitialBufferSizeMedium];
            SlowUpdateCoroutines = 0;
            _nextSlowUpdateProcessSlot = 0;

            _processTags.Clear();
            _taggedProcesses.Clear();
            _handleToIndex.Clear();
            _indexToHandle.Clear();
            _waitingTriggers.Clear();
            _expansions = (ushort)((_expansions / 2) + 1);

            return retVal;
        }

        /// <summary>
        /// Kills the instances of the coroutine handle if it exists.
        /// </summary>
        /// <param name="handle">The handle of the coroutine to kill.</param>
        /// <returns>The number of coroutines that were found and killed (0 or 1).</returns>
        public static int KillCoroutines(CoroutineHandle handle)
        {
            return ActiveInstances[handle.Key] != null ? GetInstance(handle.Key).KillCoroutinesOnInstance(handle) : 0;
        }

        /// <summary>
        /// Kills the instance of the coroutine handle on this Timing instance if it exists.
        /// </summary>
        /// <param name="handle">The handle of the coroutine to kill.</param>
        /// <returns>The number of coroutines that were found and killed (0 or 1).</returns>
        public int KillCoroutinesOnInstance(CoroutineHandle handle)
        {
            bool foundOne = false;

            if (_handleToIndex.ContainsKey(handle))
            {
                if (_waitingTriggers.ContainsKey(handle))
                    CloseWaitingProcess(handle);

                foundOne = CoindexExtract(_handleToIndex[handle]) != null;
                RemoveTag(handle);
            }

            return foundOne ? 1 : 0;
        }

        /// <summary>
        /// Kills all coroutines that have the given tag.
        /// </summary>
        /// <param name="tag">All coroutines with this tag will be killed.</param>
        /// <returns>The number of coroutines that were found and killed.</returns>
        public static int KillCoroutines(string tag)
        {
            return _instance == null ? 0 : _instance.KillCoroutinesOnInstance(tag);
        }

        /// <summary> 
        /// Kills all coroutines that have the given tag.
        /// </summary>
        /// <param name="tag">All coroutines with this tag will be killed.</param>
        /// <returns>The number of coroutines that were found and killed.</returns>
        public int KillCoroutinesOnInstance(string tag)
        {
            if (tag == null) return 0;
            int numberFound = 0;

            while (_taggedProcesses.ContainsKey(tag))
            {
                var matchEnum = _taggedProcesses[tag].GetEnumerator();
                matchEnum.MoveNext();

                if (Nullify(_handleToIndex[matchEnum.Current]))
                {
                    if (_waitingTriggers.ContainsKey(matchEnum.Current))
                        CloseWaitingProcess(matchEnum.Current);

                    numberFound++;
                }

                RemoveTag(matchEnum.Current);

                if (_handleToIndex.ContainsKey(matchEnum.Current))
                {
                    _indexToHandle.Remove(_handleToIndex[matchEnum.Current]);
                    _handleToIndex.Remove(matchEnum.Current);
                }
            }

            return numberFound;
        }

        /// <summary>
        /// This will pause all coroutines running on the current MEC instance until ResumeCoroutines is called.
        /// </summary>
        /// <returns>The number of coroutines that were paused.</returns>
        public static int PauseCoroutines()
        {
            return _instance == null ? 0 : _instance.PauseCoroutinesOnInstance();
        }

        /// <summary>
        /// This will pause all coroutines running on this MEC instance until ResumeCoroutinesOnInstance is called.
        /// </summary>
        /// <returns>The number of coroutines that were paused.</returns>
        public int PauseCoroutinesOnInstance()
        {
            int count = 0;
            int i;
            for (i = 0; i < _nextUpdateProcessSlot; i++)
            {
                if (!UpdatePaused[i] && UpdateProcesses[i] != null)
                {
                    count++;
                    UpdatePaused[i] = true;

                    if (UpdateProcesses[i].Current > GetSegmentTime(Segment.Update))
                        UpdateProcesses[i] = _InjectDelay(UpdateProcesses[i],
                            UpdateProcesses[i].Current - GetSegmentTime(Segment.Update));
                }
            }

            for (i = 0; i < _nextLateUpdateProcessSlot; i++)
            {
                if (!LateUpdatePaused[i] && LateUpdateProcesses[i] != null)
                {
                    count++;
                    LateUpdatePaused[i] = true;

                    if (LateUpdateProcesses[i].Current > GetSegmentTime(Segment.LateUpdate))
                        LateUpdateProcesses[i] = _InjectDelay(LateUpdateProcesses[i],
                            LateUpdateProcesses[i].Current - GetSegmentTime(Segment.LateUpdate));
                }
            }

            for (i = 0; i < _nextFixedUpdateProcessSlot; i++)
            {
                if (!FixedUpdatePaused[i] && FixedUpdateProcesses[i] != null)
                {
                    count++;
                    FixedUpdatePaused[i] = true;

                    if (FixedUpdateProcesses[i].Current > GetSegmentTime(Segment.FixedUpdate))
                        FixedUpdateProcesses[i] = _InjectDelay(FixedUpdateProcesses[i],
                            FixedUpdateProcesses[i].Current - GetSegmentTime(Segment.FixedUpdate));
                }
            }

            for (i = 0; i < _nextSlowUpdateProcessSlot; i++)
            {
                if (!SlowUpdatePaused[i] && SlowUpdateProcesses[i] != null)
                {
                    count++;
                    SlowUpdatePaused[i] = true;

                    if (SlowUpdateProcesses[i].Current > GetSegmentTime(Segment.SlowUpdate))
                        SlowUpdateProcesses[i] = _InjectDelay(SlowUpdateProcesses[i],
                            SlowUpdateProcesses[i].Current - GetSegmentTime(Segment.SlowUpdate));
                }
            }

            return count;
        }

        /// <summary>
        /// This will pause any matching coroutines until ResumeCoroutines is called.
        /// </summary>
        /// <param name="handle">The handle of the coroutine to pause.</param>
        /// <returns>The number of coroutines that were paused (0 or 1).</returns>
        public static int PauseCoroutines(CoroutineHandle handle)
        {
            return ActiveInstances[handle.Key] != null ? GetInstance(handle.Key).PauseCoroutinesOnInstance(handle) : 0;
        }

        /// <summary>
        /// This will pause any matching coroutines running on this MEC instance until ResumeCoroutinesOnInstance is called.
        /// </summary>
        /// <param name="handle">The handle of the coroutine to pause.</param>
        /// <returns>The number of coroutines that were paused (0 or 1).</returns>
        public int PauseCoroutinesOnInstance(CoroutineHandle handle)
        {
            return _handleToIndex.ContainsKey(handle) && !CoindexIsNull(_handleToIndex[handle]) && !SetPause(_handleToIndex[handle], true) ? 1 : 0;
        }

        /// <summary>
        /// This will pause any matching coroutines running on the current MEC instance until ResumeCoroutines is called.
        /// </summary>
        /// <param name="tag">Any coroutines with a matching tag will be paused.</param>
        /// <returns>The number of coroutines that were paused.</returns>
        public static int PauseCoroutines(string tag)
        {
            return _instance == null ? 0 : _instance.PauseCoroutinesOnInstance(tag);
        }

        /// <summary>
        /// This will pause any matching coroutines running on this MEC instance until ResumeCoroutinesOnInstance is called.
        /// </summary>
        /// <param name="tag">Any coroutines with a matching tag will be paused.</param>
        /// <returns>The number of coroutines that were paused.</returns>
        public int PauseCoroutinesOnInstance(string tag)
        {
            if (tag == null || !_taggedProcesses.ContainsKey(tag))
                return 0;

            int count = 0;
            var matchesEnum = _taggedProcesses[tag].GetEnumerator();

            while (matchesEnum.MoveNext())
                if (!CoindexIsNull(_handleToIndex[matchesEnum.Current]) && !SetPause(_handleToIndex[matchesEnum.Current], true))
                    count++;

            return count;
        }

        /// <summary>
        /// This resumes all coroutines on the current MEC instance if they are currently paused, otherwise it has
        /// no effect.
        /// </summary>
        /// <returns>The number of coroutines that were resumed.</returns>
        public static int ResumeCoroutines()
        {
            return _instance == null ? 0 : _instance.ResumeCoroutinesOnInstance();
        }

        /// <summary>
        /// This resumes all coroutines on this MEC instance if they are currently paused, otherwise it has no effect.
        /// </summary>
        /// <returns>The number of coroutines that were resumed.</returns>
        public int ResumeCoroutinesOnInstance()
        {
            int count = 0;
            ProcessIndex coindex;
            for (coindex.i = 0, coindex.seg = Segment.Update; coindex.i < _nextUpdateProcessSlot; coindex.i++)
            {
                if (UpdatePaused[coindex.i] && UpdateProcesses[coindex.i] != null)
                {
                    UpdatePaused[coindex.i] = false;
                    count++;
                }
            }

            for (coindex.i = 0, coindex.seg = Segment.LateUpdate; coindex.i < _nextLateUpdateProcessSlot; coindex.i++)
            {
                if (LateUpdatePaused[coindex.i] && LateUpdateProcesses[coindex.i] != null)
                {
                    LateUpdatePaused[coindex.i] = false;
                    count++;
                }
            }

            for (coindex.i = 0, coindex.seg = Segment.FixedUpdate; coindex.i < _nextFixedUpdateProcessSlot; coindex.i++)
            {
                if (FixedUpdatePaused[coindex.i] && FixedUpdateProcesses[coindex.i] != null)
                {
                    FixedUpdatePaused[coindex.i] = false;
                    count++;
                }
            }

            for (coindex.i = 0, coindex.seg = Segment.SlowUpdate; coindex.i < _nextSlowUpdateProcessSlot; coindex.i++)
            {
                if (SlowUpdatePaused[coindex.i] && SlowUpdateProcesses[coindex.i] != null)
                {
                    SlowUpdatePaused[coindex.i] = false;
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// This will resume any matching coroutines.
        /// </summary>
        /// <param name="handle">The handle of the coroutine to resume.</param>
        /// <returns>The number of coroutines that were resumed (0 or 1).</returns>
        public static int ResumeCoroutines(CoroutineHandle handle)
        {
            return ActiveInstances[handle.Key] != null ? GetInstance(handle.Key).ResumeCoroutinesOnInstance(handle) : 0;
        }

        /// <summary>
        /// This will resume any matching coroutines running on this MEC instance.
        /// </summary>
        /// <param name="handle">The handle of the coroutine to resume.</param>
        /// <returns>The number of coroutines that were resumed (0 or 1).</returns>
        public int ResumeCoroutinesOnInstance(CoroutineHandle handle)
        {
            return _handleToIndex.ContainsKey(handle) &&
                !CoindexIsNull(_handleToIndex[handle]) && SetPause(_handleToIndex[handle], false) ? 1 : 0;
        }

        /// <summary>
        /// This resumes any matching coroutines on the current MEC instance if they are currently paused, otherwise it has
        /// no effect.
        /// </summary>
        /// <param name="tag">Any coroutines previously paused with a matching tag will be resumend.</param>
        /// <returns>The number of coroutines that were resumed.</returns>
        public static int ResumeCoroutines(string tag)
        {
            return _instance == null ? 0 : _instance.ResumeCoroutinesOnInstance(tag);
        }

        /// <summary>
        /// This resumes any matching coroutines on this MEC instance if they are currently paused, otherwise it has no effect.
        /// </summary>
        /// <param name="tag">Any coroutines previously paused with a matching tag will be resumend.</param>
        /// <returns>The number of coroutines that were resumed.</returns>
        public int ResumeCoroutinesOnInstance(string tag)
        {
            if (tag == null || !_taggedProcesses.ContainsKey(tag))
                return 0;
            int count = 0;

            var indexesEnum = _taggedProcesses[tag].GetEnumerator();
            while (indexesEnum.MoveNext())
            {
                if (!CoindexIsNull(_handleToIndex[indexesEnum.Current]) && SetPause(_handleToIndex[indexesEnum.Current], false))
                {
                    count++;
                }
            }

            return count;
        }

        private bool UpdateTimeValues(Segment segment)
        {
            switch(segment)
            {
                case Segment.Update:
                    if (_currentUpdateFrame != Time.frameCount)
                    {
                        deltaTime = Time.deltaTime;
                        _lastUpdateTime += deltaTime;
                        localTime = _lastUpdateTime;
                        _currentUpdateFrame = Time.frameCount;
                        return true;
                    }
                    else
                    {
                        deltaTime = Time.deltaTime;
                        localTime = _lastUpdateTime;
                        return false;
                    }
                case Segment.LateUpdate:
                    if (_currentLateUpdateFrame != Time.frameCount)
                    {
                        deltaTime = Time.deltaTime;
                        _lastLateUpdateTime += deltaTime;
                        localTime = _lastLateUpdateTime;
                        _currentLateUpdateFrame = Time.frameCount;
                        return true;
                    }
                    else
                    {
                        deltaTime = Time.deltaTime;
                        localTime = _lastLateUpdateTime;
                        return false;
                    }
                case Segment.FixedUpdate:
                    deltaTime = Time.fixedDeltaTime;
                    localTime = Time.fixedTime;

                    if (_lastFixedUpdateTime + 0.0001f < Time.fixedTime)
                    {
                        _lastFixedUpdateTime = Time.fixedTime;
                        return true;
                    }

                    return false;
                case Segment.SlowUpdate:
                    if (_currentSlowUpdateFrame != Time.frameCount)
                    {
                        deltaTime = _lastSlowUpdateDeltaTime = Time.realtimeSinceStartup - _lastSlowUpdateTime;
                        localTime = _lastSlowUpdateTime = Time.realtimeSinceStartup;
                        _currentSlowUpdateFrame = Time.frameCount;
                        return true;
                    }
                    else
                    {
                        deltaTime = _lastSlowUpdateDeltaTime;
                        localTime = _lastSlowUpdateTime;
                        return false;
                    }
            }
            return true;
        }

        private float GetSegmentTime(Segment segment)
        {
            switch (segment)
            {
                case Segment.Update:
                    if (_currentUpdateFrame == Time.frameCount)
                        return _lastUpdateTime;
                    else 
                        return _lastUpdateTime + Time.deltaTime;
                case Segment.LateUpdate:
                    if (_currentUpdateFrame == Time.frameCount)
                        return _lastLateUpdateTime;
                    else
                        return _lastLateUpdateTime + Time.deltaTime;
                case Segment.FixedUpdate:
                    return Time.fixedTime;
                case Segment.SlowUpdate:
                    return Time.realtimeSinceStartup;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Retrieves the MEC manager that corresponds to the supplied instance id.
        /// </summary>
        /// <param name="ID">The instance ID.</param>
        /// <returns>The manager, or null if not found.</returns>
        public static Timing GetInstance(byte ID)
        {
            if (ID >= 0x10)
                return null;
            return ActiveInstances[ID];
        }

        private void AddTag(string tag, CoroutineHandle coindex)
        {
            _processTags.Add(coindex, tag);

            if (_taggedProcesses.ContainsKey(tag))
                _taggedProcesses[tag].Add(coindex);
            else
                _taggedProcesses.Add(tag, new HashSet<CoroutineHandle> { coindex });
        }

        private void RemoveTag(CoroutineHandle coindex)
        {
            if (_processTags.ContainsKey(coindex))
            {
                if (_taggedProcesses[_processTags[coindex]].Count > 1)
                    _taggedProcesses[_processTags[coindex]].Remove(coindex);
                else
                    _taggedProcesses.Remove(_processTags[coindex]);

                _processTags.Remove(coindex);
            }
        }

        /// <returns>Whether it was already null.</returns>
        private bool Nullify(ProcessIndex coindex)
        {
            bool retVal;

            switch (coindex.seg)
            {
                case Segment.Update:
                    retVal = UpdateProcesses[coindex.i] != null;
                    UpdateProcesses[coindex.i] = null;
                    return retVal;
                case Segment.FixedUpdate:
                    retVal = FixedUpdateProcesses[coindex.i] != null;
                    FixedUpdateProcesses[coindex.i] = null;
                    return retVal;
                case Segment.LateUpdate:
                    retVal = LateUpdateProcesses[coindex.i] != null;
                    LateUpdateProcesses[coindex.i] = null;
                    return retVal;
                case Segment.SlowUpdate:
                    retVal = SlowUpdateProcesses[coindex.i] != null;
                    SlowUpdateProcesses[coindex.i] = null;
                    return retVal;
                default:
                    return false;
            }
        }

        private IEnumerator<float> CoindexExtract(ProcessIndex coindex)
        {
            IEnumerator<float> retVal;

            switch (coindex.seg)
            {
                case Segment.Update:
                    retVal = UpdateProcesses[coindex.i];
                    UpdateProcesses[coindex.i] = null;
                    return retVal;
                case Segment.FixedUpdate:
                    retVal = FixedUpdateProcesses[coindex.i];
                    FixedUpdateProcesses[coindex.i] = null;
                    return retVal;
                case Segment.LateUpdate:
                    retVal = LateUpdateProcesses[coindex.i];
                    LateUpdateProcesses[coindex.i] = null;
                    return retVal;
                case Segment.SlowUpdate:
                    retVal = SlowUpdateProcesses[coindex.i];
                    SlowUpdateProcesses[coindex.i] = null;
                    return retVal;
                default:
                    return null;
            }
        }

        private IEnumerator<float> CoindexPeek(ProcessIndex coindex)
        {
            switch (coindex.seg)
            {
                case Segment.Update:
                    return UpdateProcesses[coindex.i];
                case Segment.FixedUpdate:
                    return FixedUpdateProcesses[coindex.i];
                case Segment.LateUpdate:
                    return LateUpdateProcesses[coindex.i];
                case Segment.SlowUpdate:
                    return SlowUpdateProcesses[coindex.i];
                default:
                    return null;
            }
        }

        private bool CoindexIsNull(ProcessIndex coindex)
        {
            switch (coindex.seg)
            {
                case Segment.Update:
                    return UpdateProcesses[coindex.i] == null;
                case Segment.FixedUpdate:
                    return FixedUpdateProcesses[coindex.i] == null;
                case Segment.LateUpdate:
                    return LateUpdateProcesses[coindex.i] == null;
                case Segment.SlowUpdate:
                    return SlowUpdateProcesses[coindex.i] == null;
                default:
                    return true;
            }
        }

        private bool SetPause(ProcessIndex coindex, bool newPausedState)
        {
            if (CoindexPeek(coindex) == null)
                return false;

            bool isPaused;
            
            switch (coindex.seg)
            {
                case Segment.Update:
                    isPaused = UpdatePaused[coindex.i];
                    UpdatePaused[coindex.i] = newPausedState;

                    if (newPausedState && UpdateProcesses[coindex.i].Current > GetSegmentTime(coindex.seg))
                        UpdateProcesses[coindex.i] = _InjectDelay(UpdateProcesses[coindex.i],
                            UpdateProcesses[coindex.i].Current - GetSegmentTime(coindex.seg));

                    return isPaused;
                case Segment.FixedUpdate:
                    isPaused = FixedUpdatePaused[coindex.i];
                    FixedUpdatePaused[coindex.i] = newPausedState;

                    if (newPausedState && FixedUpdateProcesses[coindex.i].Current > GetSegmentTime(coindex.seg))
                        FixedUpdateProcesses[coindex.i] = _InjectDelay(FixedUpdateProcesses[coindex.i],
                            FixedUpdateProcesses[coindex.i].Current - GetSegmentTime(coindex.seg));

                    return isPaused;
                case Segment.LateUpdate:
                    isPaused = LateUpdatePaused[coindex.i];
                    LateUpdatePaused[coindex.i] = newPausedState;

                    if (newPausedState && LateUpdateProcesses[coindex.i].Current > GetSegmentTime(coindex.seg))
                        LateUpdateProcesses[coindex.i] = _InjectDelay(LateUpdateProcesses[coindex.i],
                            LateUpdateProcesses[coindex.i].Current - GetSegmentTime(coindex.seg));

                    return isPaused;
                case Segment.SlowUpdate:
                    isPaused = SlowUpdatePaused[coindex.i];
                    SlowUpdatePaused[coindex.i] = newPausedState;

                    if (newPausedState && SlowUpdateProcesses[coindex.i].Current > GetSegmentTime(coindex.seg))
                        SlowUpdateProcesses[coindex.i] = _InjectDelay(SlowUpdateProcesses[coindex.i],
                            SlowUpdateProcesses[coindex.i].Current - GetSegmentTime(coindex.seg));

                    return isPaused;
                default:
                    return false;
            }
        }

        private bool SetHeld(ProcessIndex coindex, bool newHeldState)
        {
            if (CoindexPeek(coindex) == null)
                return false;

            bool isHeld;

            switch (coindex.seg)
            {
                case Segment.Update:
                    isHeld = UpdateHeld[coindex.i];
                    UpdateHeld[coindex.i] = newHeldState;

                    if (newHeldState && UpdateProcesses[coindex.i].Current > GetSegmentTime(coindex.seg))
                        UpdateProcesses[coindex.i] = _InjectDelay(UpdateProcesses[coindex.i],
                            UpdateProcesses[coindex.i].Current - GetSegmentTime(coindex.seg));

                    return isHeld;
                case Segment.FixedUpdate:
                    isHeld = FixedUpdateHeld[coindex.i];
                    FixedUpdateHeld[coindex.i] = newHeldState;

                    if (newHeldState && FixedUpdateProcesses[coindex.i].Current > GetSegmentTime(coindex.seg))
                        FixedUpdateProcesses[coindex.i] = _InjectDelay(FixedUpdateProcesses[coindex.i],
                            FixedUpdateProcesses[coindex.i].Current - GetSegmentTime(coindex.seg));

                    return isHeld;
                case Segment.LateUpdate:
                    isHeld = LateUpdateHeld[coindex.i];
                    LateUpdateHeld[coindex.i] = newHeldState;

                    if (newHeldState && LateUpdateProcesses[coindex.i].Current > GetSegmentTime(coindex.seg))
                        LateUpdateProcesses[coindex.i] = _InjectDelay(LateUpdateProcesses[coindex.i],
                            LateUpdateProcesses[coindex.i].Current - GetSegmentTime(coindex.seg));

                    return isHeld;
                case Segment.SlowUpdate:
                    isHeld = SlowUpdateHeld[coindex.i];
                    SlowUpdateHeld[coindex.i] = newHeldState;

                    if (newHeldState && SlowUpdateProcesses[coindex.i].Current > GetSegmentTime(coindex.seg))
                        SlowUpdateProcesses[coindex.i] = _InjectDelay(SlowUpdateProcesses[coindex.i],
                            SlowUpdateProcesses[coindex.i].Current - GetSegmentTime(coindex.seg));

                    return isHeld;
                default:
                    return false;
            }
        }

        private IEnumerator<float> _InjectDelay(IEnumerator<float> proc, float delayTime)
        {
            yield return WaitForSecondsOnInstance(delayTime);

            _tmpRef = proc;
            ReplacementFunction = ReturnTmpRefForRepFunc;
            yield return float.NaN;
        }

        private bool CoindexIsPaused(ProcessIndex coindex)
        {
            switch (coindex.seg)
            {
                case Segment.Update:
                    return UpdatePaused[coindex.i];
                case Segment.FixedUpdate:
                    return FixedUpdatePaused[coindex.i];
                case Segment.LateUpdate:
                    return LateUpdatePaused[coindex.i];
                case Segment.SlowUpdate:
                    return SlowUpdatePaused[coindex.i];
                default:
                    return false;
            }
        }

        private bool CoindexIsHeld(ProcessIndex coindex)
        {
            switch (coindex.seg)
            {
                case Segment.Update:
                    return UpdateHeld[coindex.i];
                case Segment.FixedUpdate:
                    return FixedUpdateHeld[coindex.i];
                case Segment.LateUpdate:
                    return LateUpdateHeld[coindex.i];
                case Segment.SlowUpdate:
                    return SlowUpdateHeld[coindex.i];
                default:
                    return false;
            }
        }

        private void CoindexReplace(ProcessIndex coindex, IEnumerator<float> replacement)
        {
            switch (coindex.seg)
            {
                case Segment.Update:
                    UpdateProcesses[coindex.i] = replacement;
                    return;
                case Segment.FixedUpdate:
                    FixedUpdateProcesses[coindex.i] = replacement;
                    return;
                case Segment.LateUpdate:
                    LateUpdateProcesses[coindex.i] = replacement;
                    return;
                case Segment.SlowUpdate:
                    SlowUpdateProcesses[coindex.i] = replacement;
                    return;
            }
        }

        /// <summary>
        /// Use "yield return Timing.WaitForSeconds(time);" to wait for the specified number of seconds.
        /// </summary>
        /// <param name="waitTime">Number of seconds to wait.</param>
        public static float WaitForSeconds(float waitTime)
        {
            if (float.IsNaN(waitTime)) waitTime = 0f;
            return LocalTime + waitTime;
        }

        /// <summary>
        /// Use "yield return timingInstance.WaitForSecondsOnInstance(time);" to wait for the specified number of seconds.
        /// </summary>
        /// <param name="waitTime">Number of seconds to wait.</param>
        public float WaitForSecondsOnInstance(float waitTime)
        {
            if (float.IsNaN(waitTime)) waitTime = 0f;
            return localTime + waitTime;
        }

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(otherCoroutine);" to pause the current 
        /// coroutine until otherCoroutine is done.
        /// </summary>
        /// <param name="otherCoroutine">The coroutine to pause for.</param>
        public static float WaitUntilDone(CoroutineHandle otherCoroutine)
        {
            return WaitUntilDone(otherCoroutine, true);
        }

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(otherCoroutine, false);" to pause the current 
        /// coroutine until otherCoroutine is done, supressing warnings.
        /// </summary>
        /// <param name="otherCoroutine">The coroutine to pause for.</param>
        /// <param name="warnOnIssue">Post a warning to the console if no hold action was actually performed.</param>
        public static float WaitUntilDone(CoroutineHandle otherCoroutine, bool warnOnIssue)
        {
            Timing inst = GetInstance(otherCoroutine.Key);

            if (inst != null && inst._handleToIndex.ContainsKey(otherCoroutine))
            {
                if (inst.CoindexIsNull(inst._handleToIndex[otherCoroutine]))
                    return 0f;

                if (!inst._waitingTriggers.ContainsKey(otherCoroutine))
                {
                    inst.CoindexReplace(inst._handleToIndex[otherCoroutine],
                        inst._StartWhenDone(otherCoroutine, inst.CoindexPeek(inst._handleToIndex[otherCoroutine])));
                    inst._waitingTriggers.Add(otherCoroutine, new HashSet<CoroutineHandle>());
                }

                if (inst._currentCoroutine == otherCoroutine)
                {
                    Assert.IsFalse(warnOnIssue, "A coroutine cannot wait for itself.");
                    return WaitForOneFrame;
                }
                if (!inst._currentCoroutine.IsValid)
                {
                    Assert.IsFalse(warnOnIssue, "The two coroutines are not running on the same MEC instance.");
                    return WaitForOneFrame;
                }

                inst._waitingTriggers[otherCoroutine].Add(inst._currentCoroutine);
                if (!inst._allWaiting.Contains(inst._currentCoroutine))
                    inst._allWaiting.Add(inst._currentCoroutine);
                inst.SetHeld(inst._handleToIndex[inst._currentCoroutine], true);
                inst.SwapToLast(otherCoroutine, inst._currentCoroutine);

                return float.NaN;
            }

            Assert.IsFalse(warnOnIssue, "WaitUntilDone cannot hold: The coroutine handle that was passed in is invalid.\n" + otherCoroutine);
            return WaitForOneFrame;
        }

        private IEnumerator<float> _StartWhenDone(CoroutineHandle handle, IEnumerator<float> proc)
        {
            if (!_waitingTriggers.ContainsKey(handle)) yield break;

            try
            {
                if (proc.Current > localTime)
                    yield return proc.Current;

                while (proc.MoveNext())
                    yield return proc.Current;
            }
            finally
            {
                CloseWaitingProcess(handle);
            }
        }

        private void SwapToLast(CoroutineHandle firstHandle, CoroutineHandle lastHandle)
        {
            if (firstHandle.Key != lastHandle.Key)
                return;

            ProcessIndex firstIndex = _handleToIndex[firstHandle];
            ProcessIndex lastIndex = _handleToIndex[lastHandle];

            if (firstIndex.seg != lastIndex.seg || firstIndex.i < lastIndex.i)
                return;

            IEnumerator<float> tempCoptr = CoindexPeek(firstIndex);
            CoindexReplace(firstIndex, CoindexPeek(lastIndex));
            CoindexReplace(lastIndex, tempCoptr);

            _indexToHandle[firstIndex] = lastHandle;
            _indexToHandle[lastIndex] = firstHandle;
            _handleToIndex[firstHandle] = lastIndex;
            _handleToIndex[lastHandle] = firstIndex;
            bool tmpB = SetPause(firstIndex, CoindexIsPaused(lastIndex));
            SetPause(lastIndex, tmpB);
            tmpB = SetHeld(firstIndex, CoindexIsHeld(lastIndex));
            SetHeld(lastIndex, tmpB);
        }

        private void CloseWaitingProcess(CoroutineHandle handle)
        {
            if (!_waitingTriggers.ContainsKey(handle)) return;

            var tasksEnum = _waitingTriggers[handle].GetEnumerator();
            _waitingTriggers.Remove(handle);

            while (tasksEnum.MoveNext())
            {
                if (_handleToIndex.ContainsKey(tasksEnum.Current) && !HandleIsInWaitingList(tasksEnum.Current))
                {
                    SetHeld(_handleToIndex[tasksEnum.Current], false);
                    _allWaiting.Remove(tasksEnum.Current);
                }
            }
        }

        private bool HandleIsInWaitingList(CoroutineHandle handle)
        {
            var triggersEnum = _waitingTriggers.GetEnumerator();
            while (triggersEnum.MoveNext())
                if (triggersEnum.Current.Value.Contains(handle))
                    return true;

            return false;
        }

        private static IEnumerator<float> ReturnTmpRefForRepFunc(IEnumerator<float> coptr, CoroutineHandle handle)
        {
            return _tmpRef as IEnumerator<float>;
        }

#if !UNITY_2018_3_OR_NEWER
        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(wwwObject);" to pause the current 
        /// coroutine until the wwwObject is done.
        /// </summary>
        /// <param name="wwwObject">The www object to pause for.</param>
        public static float WaitUntilDone(WWW wwwObject)
        {
            if (wwwObject == null || wwwObject.isDone) return 0f;

            _tmpRef = wwwObject;
            ReplacementFunction = WaitUntilDoneWwwHelper;
            return float.NaN;
        }


        private static IEnumerator<float> WaitUntilDoneWwwHelper(IEnumerator<float> coptr, CoroutineHandle handle)
        {
            return _StartWhenDone(_tmpRef as WWW, coptr);
        }

        private static IEnumerator<float> _StartWhenDone(WWW www, IEnumerator<float> pausedProc)
        {
            while (!www.isDone)
                yield return WaitForOneFrame;

            _tmpRef = pausedProc;
            ReplacementFunction = ReturnTmpRefForRepFunc;
            yield return float.NaN;
        }
#endif

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(operation);" to pause the current 
        /// coroutine until the operation is done.
        /// </summary>
        /// <param name="operation">The operation variable returned.</param>
        public static float WaitUntilDone(AsyncOperation operation)
        {
            if (operation == null || operation.isDone) return float.NaN;

            CoroutineHandle handle = CurrentCoroutine;
            Timing inst = GetInstance(CurrentCoroutine.Key);
            if (inst == null) return float.NaN;

            _tmpRef = _StartWhenDone(operation, inst.CoindexPeek(inst._handleToIndex[handle]));
            ReplacementFunction = ReturnTmpRefForRepFunc;
            return float.NaN;
        }

        private static IEnumerator<float> _StartWhenDone(AsyncOperation operation, IEnumerator<float> pausedProc)
        {
            while (!operation.isDone)
                yield return WaitForOneFrame;

            _tmpRef = pausedProc;
            ReplacementFunction = ReturnTmpRefForRepFunc;
            yield return float.NaN;
        }

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(operation);" to pause the current 
        /// coroutine until the operation is done.
        /// </summary>
        /// <param name="operation">The operation variable returned.</param>
        public static float WaitUntilDone(CustomYieldInstruction operation)
        {
            if (operation == null || !operation.keepWaiting) return float.NaN;

            CoroutineHandle handle = CurrentCoroutine;
            Timing inst = GetInstance(CurrentCoroutine.Key);
            if (inst == null) return float.NaN;

            _tmpRef = _StartWhenDone(operation, inst.CoindexPeek(inst._handleToIndex[handle]));
            ReplacementFunction = ReturnTmpRefForRepFunc;
            return float.NaN;
        }

        private static IEnumerator<float> _StartWhenDone(CustomYieldInstruction operation, IEnumerator<float> pausedProc)
        {
            while (operation.keepWaiting)
                yield return WaitForOneFrame;

            _tmpRef = pausedProc;
            ReplacementFunction = ReturnTmpRefForRepFunc;
            yield return float.NaN;
        }

        /// <summary>
        /// Keeps this coroutine from executing until UnlockCoroutine is called with a matching key.
        /// </summary>
        /// <param name="coroutine">The handle to the coroutine to be locked.</param>
        /// <param name="key">The key to use. A new key can be generated by calling "new CoroutineHandle(0)".</param>
        /// <returns>Whether the lock was successful.</returns>
        public bool LockCoroutine(CoroutineHandle coroutine, CoroutineHandle key)
        {
            if (coroutine.Key != _instanceID || key == new CoroutineHandle() || key.Key != 0)
                return false;

            if (!_waitingTriggers.ContainsKey(key))
                _waitingTriggers.Add(key, new HashSet<CoroutineHandle> { coroutine });
            else
                _waitingTriggers[key].Add(coroutine);

            SetHeld(_handleToIndex[coroutine], true);

            return true;
        }

        /// <summary>
        /// Unlocks a coroutine that has been locked, so long as the key matches.
        /// </summary>
        /// <param name="coroutine">The handle to the coroutine to be unlocked.</param>
        /// <param name="key">The key that the coroutine was previously locked with.</param>
        /// <returns>Whether the coroutine was successfully unlocked.</returns>
        public bool UnlockCoroutine(CoroutineHandle coroutine, CoroutineHandle key)
        {
            if (coroutine.Key != _instanceID || key == new CoroutineHandle() ||
                !_handleToIndex.ContainsKey(coroutine) || !_waitingTriggers.ContainsKey(key))
                return false;

            _waitingTriggers[key].Remove(coroutine);

            SetHeld(_handleToIndex[coroutine], HandleIsInWaitingList(coroutine));

            return true;
        }

        /// <summary>
        /// Calls the specified action after a specified number of seconds.
        /// </summary>
        /// <param name="delay">The number of seconds to wait before calling the action.</param>
        /// <param name="action">The action to call.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallDelayed(float delay, System.Action action)
        {
            return action == null ? new CoroutineHandle() : RunCoroutine(Instance._DelayedCall(delay, action, null));
        }

        /// <summary>
        /// Calls the specified action after a specified number of seconds.
        /// </summary>
        /// <param name="delay">The number of seconds to wait before calling the action.</param>
        /// <param name="action">The action to call.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallDelayedOnInstance(float delay, System.Action action)
        {
            return action == null ? new CoroutineHandle() : RunCoroutineOnInstance(_DelayedCall(delay, action, null));
        }

        /// <summary>
        /// Calls the specified action after a specified number of seconds.
        /// </summary>
        /// <param name="delay">The number of seconds to wait before calling the action.</param>
        /// <param name="action">The action to call.</param>
        /// <param name="cancelWith">A GameObject that will be checked to make sure it hasn't been destroyed before calling the action.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallDelayed(float delay, System.Action action, GameObject cancelWith)
        {
            return action == null ? new CoroutineHandle() : RunCoroutine(Instance._DelayedCall(delay, action, cancelWith));
        }

        /// <summary>
        /// Calls the specified action after a specified number of seconds.
        /// </summary>
        /// <param name="delay">The number of seconds to wait before calling the action.</param>
        /// <param name="action">The action to call.</param>
        /// <param name="cancelWith">A GameObject that will be checked to make sure it hasn't been destroyed before calling the action.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallDelayedOnInstance(float delay, System.Action action, GameObject cancelWith)
        {
            return action == null ? new CoroutineHandle() : RunCoroutineOnInstance(_DelayedCall(delay, action, cancelWith));
        }

        private IEnumerator<float> _DelayedCall(float delay, System.Action action, GameObject cancelWith)
        {
            yield return WaitForSecondsOnInstance(delay);

            if(ReferenceEquals(cancelWith, null) || cancelWith != null)
                action();
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallPeriodically(float timeframe, float period, System.Action action, System.Action onDone = null)
        {
            return action == null ? new CoroutineHandle() : RunCoroutine(Instance._CallContinuously(timeframe, period, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallPeriodicallyOnInstance(float timeframe, float period, System.Action action, System.Action onDone = null)
        {
            return action == null ? new CoroutineHandle() : RunCoroutineOnInstance(_CallContinuously(timeframe, period, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="segment">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallPeriodically(float timeframe, float period, System.Action action, Segment segment, System.Action onDone = null)
        {
            return action == null ? new CoroutineHandle() : RunCoroutine(Instance._CallContinuously(timeframe, period, action, onDone), segment);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="segment">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallPeriodicallyOnInstance(float timeframe, float period, System.Action action, Segment segment, System.Action onDone = null)
        {
            return action == null ? new CoroutineHandle() : RunCoroutineOnInstance(_CallContinuously(timeframe, period, action, onDone), segment);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallContinuously(float timeframe, System.Action action, System.Action onDone = null)
        {
            return action == null ? new CoroutineHandle() : RunCoroutine(Instance._CallContinuously(timeframe, 0f, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallContinuouslyOnInstance(float timeframe, System.Action action, System.Action onDone = null)
        {
            return action == null ? new CoroutineHandle() : RunCoroutineOnInstance(_CallContinuously(timeframe, 0f, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallContinuously(float timeframe, System.Action action, Segment timing, System.Action onDone = null)
        {
            return action == null ? new CoroutineHandle() : RunCoroutine(Instance._CallContinuously(timeframe, 0f, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallContinuouslyOnInstance(float timeframe, System.Action action, Segment timing, System.Action onDone = null)
        {
            return action == null ? new CoroutineHandle() : RunCoroutineOnInstance(_CallContinuously(timeframe, 0f, action, onDone), timing);
        }

        private IEnumerator<float> _CallContinuously(float timeframe, float period, System.Action action, System.Action onDone)
        {
            double startTime = localTime;
            while (localTime <= startTime + timeframe)
            {
                yield return WaitForSecondsOnInstance(period);

                action();
            }

            if (onDone != null)
                onDone();
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each period.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallPeriodically<T>
            (T reference, float timeframe, float period, System.Action<T> action, System.Action<T> onDone = null)
        {
            return action == null ? new CoroutineHandle() : 
                RunCoroutine(Instance._CallContinuously(reference, timeframe, period, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each period.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallPeriodicallyOnInstance<T>
            (T reference, float timeframe, float period, System.Action<T> action, System.Action<T> onDone = null)
        {
            return action == null ? new CoroutineHandle() : 
                RunCoroutineOnInstance(_CallContinuously(reference, timeframe, period, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each period.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallPeriodically<T>(T reference, float timeframe, float period, System.Action<T> action, 
            Segment timing, System.Action<T> onDone = null)
        {
            return action == null ? new CoroutineHandle() : 
                RunCoroutine(Instance._CallContinuously(reference, timeframe, period, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each period.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallPeriodicallyOnInstance<T>(T reference, float timeframe, float period, System.Action<T> action,
            Segment timing, System.Action<T> onDone = null)
        {
            return action == null ? new CoroutineHandle() : 
                RunCoroutineOnInstance(_CallContinuously(reference, timeframe, period, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each frame.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallContinuously<T>(T reference, float timeframe, System.Action<T> action, System.Action<T> onDone = null)
        {
            return action == null ? new CoroutineHandle() : 
                RunCoroutine(Instance._CallContinuously(reference, timeframe, 0f, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each frame.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallContinuouslyOnInstance<T>(T reference, float timeframe, System.Action<T> action, System.Action<T> onDone = null)
        {
            return action == null ? new CoroutineHandle() : 
                RunCoroutineOnInstance(_CallContinuously(reference, timeframe, 0f, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each frame.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public static CoroutineHandle CallContinuously<T>(T reference, float timeframe, System.Action<T> action, 
            Segment timing, System.Action<T> onDone = null)
        {
            return action == null ? new CoroutineHandle() : 
                RunCoroutine(Instance._CallContinuously(reference, timeframe, 0f, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each frame.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        /// <returns>The handle to the coroutine that is started by this function.</returns>
        public CoroutineHandle CallContinuouslyOnInstance<T>(T reference, float timeframe, System.Action<T> action,
            Segment timing, System.Action<T> onDone = null)
        {
            return action == null ? new CoroutineHandle() : 
                RunCoroutineOnInstance(_CallContinuously(reference, timeframe, 0f, action, onDone), timing);
        }

        private IEnumerator<float> _CallContinuously<T>(T reference, float timeframe, float period,
            System.Action<T> action, System.Action<T> onDone = null)
        {
            double startTime = localTime;
            while (localTime <= startTime + timeframe)
            {
                yield return WaitForSecondsOnInstance(period);

                action(reference);
            }

            if (onDone != null)
                onDone(reference);
        }

        private struct ProcessIndex : System.IEquatable<ProcessIndex>
        {
            public Segment seg;
            public int i;

            public bool Equals(ProcessIndex other)
            {
                return seg == other.seg && i == other.i;
            }

            public override bool Equals(object other)
            {
                if (other is ProcessIndex)
                    return Equals((ProcessIndex)other);
                return false;
            }

            public static bool operator ==(ProcessIndex a, ProcessIndex b)
            {
                return a.seg == b.seg && a.i == b.i;
            }

            public static bool operator !=(ProcessIndex a, ProcessIndex b)
            {
                return a.seg != b.seg || a.i != b.i;
            }

            public override int GetHashCode()
            {
                return (((int)seg - 2) * (int.MaxValue / 3)) + i;
            }
        }

        [System.Obsolete("Unity coroutine function, use RunCoroutine instead.", true)]
        public new Coroutine StartCoroutine(System.Collections.IEnumerator routine) { return null; }

        [System.Obsolete("Unity coroutine function, use RunCoroutine instead.", true)]
        public new Coroutine StartCoroutine(string methodName, object value) { return null; }

        [System.Obsolete("Unity coroutine function, use RunCoroutine instead.", true)]
        public new Coroutine StartCoroutine(string methodName) { return null; }

        [System.Obsolete("Unity coroutine function, use RunCoroutine instead.", true)]
        public new Coroutine StartCoroutine_Auto(System.Collections.IEnumerator routine) { return null; }

        [System.Obsolete("Unity coroutine function, use KillCoroutines instead.", true)]
        public new void StopCoroutine(string methodName) { }

        [System.Obsolete("Unity coroutine function, use KillCoroutines instead.", true)]
        public new void StopCoroutine(System.Collections.IEnumerator routine) { }

        [System.Obsolete("Unity coroutine function, use KillCoroutines instead.", true)]
        public new void StopCoroutine(Coroutine routine) { }

        [System.Obsolete("Unity coroutine function, use KillCoroutines instead.", true)]
        public new void StopAllCoroutines() { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void Destroy(Object obj) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void Destroy(Object obj, float f) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void DestroyObject(Object obj) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void DestroyObject(Object obj, float f) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void DestroyImmediate(Object obj) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void DestroyImmediate(Object obj, bool b) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void Instantiate(Object obj) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void Instantiate(Object original, Vector3 position, Quaternion rotation) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void Instantiate<T>(T original) where T : Object { }

        [System.Obsolete("Just.. no.", true)]
        public new static T FindObjectOfType<T>() where T : Object { return null; }

        [System.Obsolete("Just.. no.", true)]
        public new static Object FindObjectOfType(System.Type t) { return null; }

        [System.Obsolete("Just.. no.", true)]
        public new static T[] FindObjectsOfType<T>() where T : Object { return null; }

        [System.Obsolete("Just.. no.", true)]
        public new static Object[] FindObjectsOfType(System.Type t) { return null; }

        [System.Obsolete("Just.. no.", true)]
        public new static void print(object message) { }
    }

    /// <summary>
    /// The timing segment that a coroutine is running in or should be run in.
    /// </summary>
    public enum Segment
    {
        /// <summary>
        /// Sometimes returned as an error state
        /// </summary>
        Invalid = -1,
        /// <summary>
        /// This is the default timing segment
        /// </summary>
        Update,
        /// <summary>
        /// This is primarily used for physics calculations
        /// </summary>
        FixedUpdate,
        /// <summary>
        /// This is run immediately after update
        /// </summary>
        LateUpdate,
        /// <summary>
        /// This executes, by default, about as quickly as the eye can detect changes in a text field
        /// </summary>
        SlowUpdate
    }

    /// <summary>
    /// How much debug info should be sent to the Unity profiler. NOTE: Setting this to anything above none shows up in the profiler as a 
    /// decrease in performance and a memory alloc. Those effects do not translate onto device.
    /// </summary>
    public enum DebugInfoType
    {
        /// <summary>
        /// None coroutines will be separated in the Unity profiler
        /// </summary>
        None,
        /// <summary>
        /// The Unity profiler will identify each coroutine individually
        /// </summary>
        SeperateCoroutines,
        /// <summary>
        /// Coroutines will be separated and any tags or layers will be identified
        /// </summary>
        SeperateTags
    }

    /// <summary>
    /// A handle for a MEC coroutine.
    /// </summary>
    public struct CoroutineHandle : System.IEquatable<CoroutineHandle>
    {
        private const byte ReservedSpace = 0x0F;
        private readonly static int[] NextIndex = { ReservedSpace + 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private readonly int _id;

        public byte Key { get { return (byte)(_id & ReservedSpace); } }

        public CoroutineHandle(byte ind)
        {
            if (ind > ReservedSpace)
                ind -= ReservedSpace;

            _id = NextIndex[ind] + ind;
            NextIndex[ind] += ReservedSpace + 1;
        }

        public bool Equals(CoroutineHandle other)
        {
            return _id == other._id;
        }

        public override bool Equals(object other)
        {
            if (other is CoroutineHandle)
                return Equals((CoroutineHandle)other);
            return false;
        }

        public static bool operator==(CoroutineHandle a, CoroutineHandle b)
        {
            return a._id == b._id;
        }

        public static bool operator!=(CoroutineHandle a, CoroutineHandle b)
        {
            return a._id != b._id;
        }

        public override int GetHashCode()
        {
            return _id;
        }

        /// <summary>
        /// Is true if this handle may have been a valid handle at some point. (i.e. is not an uninitialized handle, error handle, or a key to a coroutine lock)
        /// </summary>
        public bool IsValid
        {
            get { return Key != 0; }
        }
    }
}

public static class MECExtensionMethods
{
    /// <summary>
    /// Cancels this coroutine when the supplied game object is destroyed or made inactive.
    /// </summary>
    /// <param name="coroutine">The coroutine handle to act upon.</param>
    /// <param name="gameObject">The GameObject to test.</param>
    /// <returns>The modified coroutine handle.</returns>
    public static IEnumerator<float> CancelWith(this IEnumerator<float> coroutine, GameObject gameObject)
    {
        while (MEC.Timing.MainThread != System.Threading.Thread.CurrentThread || (gameObject && gameObject.activeInHierarchy && coroutine.MoveNext()))
            yield return coroutine.Current;
    }

    /// <summary>
    /// Cancels this coroutine when the supplied game objects are destroyed or made inactive.
    /// </summary>
    /// <param name="coroutine">The coroutine handle to act upon.</param>
    /// <param name="gameObject1">The first GameObject to test.</param>
    /// <param name="gameObject2">The second GameObject to test</param>
    /// <returns>The modified coroutine handle.</returns>
    public static IEnumerator<float> CancelWith(this IEnumerator<float> coroutine, GameObject gameObject1, GameObject gameObject2)
    {
        while (MEC.Timing.MainThread != System.Threading.Thread.CurrentThread || (gameObject1 && gameObject1.activeInHierarchy && 
                gameObject2 && gameObject2.activeInHierarchy && coroutine.MoveNext()))
            yield return coroutine.Current;
    }

    /// <summary>
    /// Cancels this coroutine when the supplied game objects are destroyed or made inactive.
    /// </summary>
    /// <param name="coroutine">The coroutine handle to act upon.</param>
    /// <param name="gameObject1">The first GameObject to test.</param>
    /// <param name="gameObject2">The second GameObject to test</param>
    /// <param name="gameObject3">The third GameObject to test.</param>
    /// <returns>The modified coroutine handle.</returns>
    public static IEnumerator<float> CancelWith(this IEnumerator<float> coroutine,
        GameObject gameObject1, GameObject gameObject2, GameObject gameObject3)
    {
        while (MEC.Timing.MainThread != System.Threading.Thread.CurrentThread || (gameObject1 && gameObject1.activeInHierarchy && 
                gameObject2 && gameObject2.activeInHierarchy && gameObject3 && gameObject3.activeInHierarchy && coroutine.MoveNext()))
            yield return coroutine.Current;
    }
}
