using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RhythmTracker : MonoBehaviour {
    public static RhythmTracker instance;
    [SerializeField]
    [Tooltip("Desired tempo in bpm")]
    private float m_InitialTempo = 120;
    [SerializeField]
    [Tooltip("Sends notification of beats by this much time in advance.")]
    private float m_AdvancedNotificationOffset = 0;
    [SerializeField]
    [Tooltip("Start Playing right away. Not actually on Awake, but on the first frame.")]
    private bool m_PlayOnAwake = true;
    [SerializeField]
    [Tooltip("AudioSource for the music you're playing along to. Optional")]
    private AudioSource m_PlaybackAudioSource = null;
    [SerializeField]
    [Tooltip("Useful for lead-ins and intros")]
    private float m_PrerollTime = 0;
    private AudioSource m_LoopAudioSource;
    private AudioSource m_OffsetAudioSource;
    private int m_SubDivisions = 64;
    private List<int> m_HitList;
    private int m_NextHitIndex = 0;
    private int m_NextAdvancedHitIndex = 0;
    private bool m_IsPaused;

    public enum TriggerTiming { Thirtyseconds, Sixteenths, Eighths, Quarters, Halves, Wholes };

    public delegate void OnBeatDelegate(int beatIndex);
    public OnBeatDelegate On32nd;
    public OnBeatDelegate On16th;
    public OnBeatDelegate On8th;
    public OnBeatDelegate OnQuarter;
    public OnBeatDelegate OnHalf;
    public OnBeatDelegate OnWhole;

    public OnBeatDelegate OnAdvanced32nd;
    public OnBeatDelegate OnAdvanced16th;
    public OnBeatDelegate OnAdvanced8th;
    public OnBeatDelegate OnAdvancedQuarter;
    public OnBeatDelegate OnAdvancedHalf;
    public OnBeatDelegate OnAdvancedWhole;

    void Awake()
    {
        // Singleton behavior
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this);
        }

        // Set the main audio loop
        m_LoopAudioSource = new GameObject().AddComponent<AudioSource>();
        m_LoopAudioSource.gameObject.name = "MainTracking";
        m_LoopAudioSource.transform.parent = transform;
        m_LoopAudioSource.loop = true;
        m_LoopAudioSource.playOnAwake = false;
        m_LoopAudioSource.volume = 0;
        AudioClip silentClip = AudioClip.Create("100bpm-Silent", 423360, 1, 44100, false);
        m_LoopAudioSource.clip = silentClip;

        // If we're providing forewarnings on an offset, create a new child object with a
        // similarly set up audio source. This one will start right away, while the other will
        // start on after the offset time has passed.
        if (m_AdvancedNotificationOffset > 0)
        {
            m_OffsetAudioSource = new GameObject().AddComponent<AudioSource>();
            m_OffsetAudioSource.gameObject.name = "OffsetTracking";
            m_OffsetAudioSource.transform.parent = transform;
            m_OffsetAudioSource.loop = true;
            m_OffsetAudioSource.playOnAwake = false;
            m_OffsetAudioSource.clip = m_LoopAudioSource.clip;
            m_OffsetAudioSource.volume = 0;
        }

        // Initialize list of target samples
        m_HitList = new List<int>();
        // Fill list with the the samples that represent target downbeats.
        for (int i = 0; i < m_SubDivisions; i++)
        {
            m_HitList.Add(m_LoopAudioSource.clip.samples / m_SubDivisions * i);
        }

        // Apply the tempo specified in the inspector.
        SetTempo(m_InitialTempo);

        // If PlayOnAwake is enabled, queue it up so Play() actually happens on the first frame. After everything's initialized and all subscriptions in 
        // Start() and Awake() functions have been made. Otherwise, first beats will be intermittently skipped, depending on the order of initialization.
        if (m_PlayOnAwake)
        {
            StartCoroutine(PlayDelayedByFrame());
        }

    }

	void Update ()
    {
        CheckForHit();
        CheckForAdvancedHit();
    }

    private void CheckForHit()
    {
        if (m_LoopAudioSource.timeSamples > m_HitList[m_NextHitIndex])
        {
            // Special check to not fire on the condition where the hit we're waiting for is at 0, but we're greater than 0 only because
            // we're at the end of the loop.
            if (m_NextHitIndex == 0 && m_LoopAudioSource.timeSamples > m_HitList[1])
                return;
            // Call notifier
            Hit(m_NextHitIndex);
            // Increment hit index (looping back to 0 once out of range)
            m_NextHitIndex = (m_NextHitIndex + 1) % m_HitList.Count;
        }
    }
    private void CheckForAdvancedHit()
    {
        if (m_AdvancedNotificationOffset > 0 && m_OffsetAudioSource.timeSamples > m_HitList[m_NextAdvancedHitIndex])
        {
            if (m_NextAdvancedHitIndex == 0 && m_OffsetAudioSource.timeSamples > m_HitList[1])
                return;
            // Call notifier
            AdvancedHit(m_NextAdvancedHitIndex);
            // Increment hit index (looping back to 0 once out of range)
            m_NextAdvancedHitIndex = (m_NextAdvancedHitIndex + 1) % m_HitList.Count;
        }
    }

    // Alert subscribers that it's note time
    private void Hit(int index)
    {
        if (On32nd != null)
            On32nd(index);
        if (index % 2 == 0 && On16th != null)
            On16th(index);
        if (index % 4 == 0 && On8th != null)
            On8th(index);
        if (index % 8 == 0 && OnQuarter != null)
            OnQuarter(index);
        if (index % 16 == 0 && OnHalf != null)
            OnHalf(index);
        if (index % 32 == 0 && OnWhole != null)
            OnWhole(index);
    }
    private void AdvancedHit(int index)
    {
        if (OnAdvanced32nd != null)
            OnAdvanced32nd(index);
        if (index % 2 == 0 && OnAdvanced16th != null)
            OnAdvanced16th(index);
        if (index % 4 == 0 && OnAdvanced8th != null)
            OnAdvanced8th(index);
        if (index % 8 == 0 && OnAdvancedQuarter != null)
            OnAdvancedQuarter(index);
        if (index % 16 == 0 && OnAdvancedHalf != null)
            OnAdvancedHalf(index);
        if (index % 32 == 0 && OnAdvancedWhole != null)
            OnAdvancedWhole(index);
    }

    public float GetOffset()
    {
        return m_AdvancedNotificationOffset;
    }

    public float GetPreRoll()
    {
        return m_PrerollTime;
    }

    public void SetPreRoll(float preRoll)
    {
        m_PrerollTime = preRoll;
    }

    public AudioSource GetPlaybackAudioSource()
    {
        return m_PlaybackAudioSource;
    }

    // Set and get tempo
    public float GetTempo()
    {
        return m_LoopAudioSource.pitch * 100;
    }

    public void SetTempo(float tempo)
    {
        m_LoopAudioSource.pitch = tempo / 100;
        if (m_OffsetAudioSource != null)
            m_OffsetAudioSource.pitch = tempo / 100;
        if (m_PlaybackAudioSource != null && m_PlaybackAudioSource.isPlaying)
            Debug.LogWarning("Warning: Changing the tempo of the RhythmSystem does NOT change the tempo of the playback audio clip.");
    }

    // Composite transport controls
    public void Play()
    {
        if (!m_IsPaused)
        {
            StartCoroutine(PlayDelayedByFrame());
        }
        else
        {
            UnPause();
        }
    }
    public void Stop()
    {
        m_IsPaused = false;
        if (m_OffsetAudioSource != null)
            m_OffsetAudioSource.Stop();
        if (m_PlaybackAudioSource != null)
            m_PlaybackAudioSource.Stop();
        if (m_LoopAudioSource != null)
            m_LoopAudioSource.Stop();
    }
    public void Pause()
    {
        StartCoroutine(PauseAsSoonAsPossible());
    }
    public void UnPause()
    {
        if (!m_IsPaused)
            return;

        m_IsPaused = false;
        if (m_OffsetAudioSource != null)
            m_OffsetAudioSource.UnPause();
        if (m_PlaybackAudioSource != null)
            m_PlaybackAudioSource.UnPause();
        if (m_LoopAudioSource != null)
            m_LoopAudioSource.UnPause();
    }

    public bool IsPaused()
    {
        return m_IsPaused;
    }

    // Wait til everything is fully initialized before playback begins... We don't want samples running before we have a chance to see them.
    private IEnumerator PlayDelayedByFrame()
    {
        yield return new WaitForEndOfFrame();
        StartPlayback();
    }

    private IEnumerator PauseAsSoonAsPossible()
    {
        m_IsPaused = true;
        if (m_AdvancedNotificationOffset > 0 && !m_LoopAudioSource.isPlaying)
        {
            // Wait until the advanced notification offset has elapsed.
            while (!m_LoopAudioSource.isPlaying)
            {
                // If playback is stopped (or something else happens to the state) while we're waiting, break.
                if (!m_IsPaused)
                    yield break;
                yield return new WaitForEndOfFrame();
            }
        }
        // Pause all relevant audio sources.
        if (m_OffsetAudioSource != null && m_OffsetAudioSource.isPlaying)
            m_OffsetAudioSource.Pause();
        if (m_PlaybackAudioSource != null && m_PlaybackAudioSource.isPlaying)
            m_PlaybackAudioSource.Pause();
        if (m_LoopAudioSource.isPlaying)
            m_LoopAudioSource.Pause();
    }

    private void StartPlayback()
    {
        m_IsPaused = false;
        m_NextHitIndex = 0;
        m_NextAdvancedHitIndex = 0;

        // If an offset is desired, start the pre-roll audiosource's playback and delay main playback by that offset.
        if (m_OffsetAudioSource != null)
        {
            m_OffsetAudioSource.PlayDelayed(m_PrerollTime);
        }
        if (m_PlaybackAudioSource != null)
        {
            m_PlaybackAudioSource.PlayDelayed(m_AdvancedNotificationOffset);
        }
        // Play main loop audiosource
        m_LoopAudioSource.PlayDelayed(m_AdvancedNotificationOffset + m_PrerollTime);
    }


    // Public Subscription methods for convenience
    public void Subscribe(OnBeatDelegate subscriber, TriggerTiming triggerTiming, bool advanced = false)
    {
        if (advanced)
        {
            switch (triggerTiming)
            {
                case TriggerTiming.Thirtyseconds:
                {
                    OnAdvanced32nd += subscriber;
                    return;
                }
                case TriggerTiming.Sixteenths:
                {
                    OnAdvanced16th += subscriber;
                    return;
                }
                case TriggerTiming.Eighths:
                {
                    OnAdvanced8th += subscriber;
                    return;
                }
                case TriggerTiming.Quarters:
                {
                    OnAdvancedQuarter += subscriber;
                    return;
                }
                case TriggerTiming.Halves:
                {
                    OnAdvancedHalf += subscriber;
                    return;
                }
                case TriggerTiming.Wholes:
                {
                    OnAdvancedWhole += subscriber;
                    return;
                }
            }
        }
        else
        {
            switch (triggerTiming)
            {
                case TriggerTiming.Thirtyseconds:
                {
                    On32nd += subscriber;
                    return;
                }
                case TriggerTiming.Sixteenths:
                {
                    On16th += subscriber;
                    return;
                }
                case TriggerTiming.Eighths:
                {
                    On8th += subscriber;
                    return;
                }
                case TriggerTiming.Quarters:
                {
                    OnQuarter += subscriber;
                    return;
                }
                case TriggerTiming.Halves:
                {
                    OnHalf += subscriber;
                    return;
                }
                case TriggerTiming.Wholes:
                {
                    OnWhole += subscriber;
                    return;
                }
            }
        }
    }
    public void Unsubscribe(OnBeatDelegate subscriber, TriggerTiming triggerTiming, bool advanced = false)
    {
        if (advanced)
        {
            switch (triggerTiming)
            {
                case TriggerTiming.Thirtyseconds:
                {
                    OnAdvanced32nd -= subscriber;
                    return;
                }
                case TriggerTiming.Sixteenths:
                {
                    OnAdvanced16th -= subscriber;
                    return;
                }
                case TriggerTiming.Eighths:
                {
                    OnAdvanced8th -= subscriber;
                    return;
                }
                case TriggerTiming.Quarters:
                {
                    OnAdvancedQuarter -= subscriber;
                    return;
                }
                case TriggerTiming.Halves:
                {
                    OnAdvancedHalf -= subscriber;
                    return;
                }
                case TriggerTiming.Wholes:
                {
                    OnAdvancedWhole -= subscriber;
                    return;
                }
            }
        }
        else
        {
            switch (triggerTiming)
            {
                case TriggerTiming.Thirtyseconds:
                {
                    On32nd -= subscriber;
                    return;
                }
                case TriggerTiming.Sixteenths:
                {
                    On16th -= subscriber;
                    return;
                }
                case TriggerTiming.Eighths:
                {
                    On8th -= subscriber;
                    return;
                }
                case TriggerTiming.Quarters:
                {
                    OnQuarter -= subscriber;
                    return;
                }
                case TriggerTiming.Halves:
                {
                    OnHalf -= subscriber;
                    return;
                }
                case TriggerTiming.Wholes:
                {
                    OnWhole -= subscriber;
                    return;
                }
            }
        }
    }
    // Unsubscribes method from unknown. 
    public void Unsubscribe(OnBeatDelegate subscriber)
    {
        OnAdvanced32nd -= subscriber;
        OnAdvanced16th -= subscriber;
        OnAdvanced8th -= subscriber;
        OnAdvancedQuarter -= subscriber;
        OnAdvancedHalf -= subscriber;
        OnAdvancedWhole -= subscriber;
        On32nd -= subscriber;
        On16th -= subscriber;
        On8th -= subscriber;
        OnQuarter -= subscriber;
        OnHalf -= subscriber;
        OnWhole -= subscriber;
    }
}
