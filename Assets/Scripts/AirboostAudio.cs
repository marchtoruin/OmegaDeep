using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AirboostAudio : MonoBehaviour
{
    [Header("FMOD Events")]
    [SerializeField] private EventReference boostStartEvent;
    [SerializeField] private EventReference boostStopEvent;
    
    [Header("Settings")]
    [SerializeField] private KeyCode boostKey = KeyCode.Space;
    [SerializeField] private int eventPoolSize = 3; // Number of pre-created instances
    [SerializeField] private float instanceTTL = 3f; // Time to keep instances alive
    
    // FMOD resources
    private EventDescription boostStartEventDesc;
    private EventDescription boostStopEventDesc;
    
    // Instance pooling
    private List<EventInstance> boostStartPool = new List<EventInstance>();
    private List<float> boostStartPoolTimers = new List<float>(); 
    private List<EventInstance> boostStopPool = new List<EventInstance>();
    private List<float> boostStopPoolTimers = new List<float>();
    
    private bool isReady = false;
    private bool isBoostActive = false;
    private bool hasStartedPreload = false;

    private void Awake()
    {
        // Start preloading immediately
        StartCoroutine(PreloadWithPriority());
    }
    
    private IEnumerator PreloadWithPriority()
    {
        hasStartedPreload = true;
        Debug.Log("AirboostAudio: Beginning preload");
        
        // Ensure FMODUnity is fully initialized
        while (!RuntimeManager.IsInitialized)
        {
            yield return null;
        }
        
        // Wait one frame after FMOD initialization
        yield return null;
        
        // Preload event descriptions with highest priority
        PreloadEvents();
        
        // Wait another frame before creating instances
        yield return null;
        
        // Create instance pools for both events
        CreateEventPools();
        
        isReady = true;
        Debug.Log("AirboostAudio: Preload complete, ready to play");
    }

    private void PreloadEvents()
    {
        if (!boostStartEvent.IsNull)
        {
            // Get the event description and preload its sample data
            RuntimeManager.StudioSystem.getEvent(boostStartEvent.ToString(), out boostStartEventDesc);
            if (boostStartEventDesc.isValid())
            {
                boostStartEventDesc.loadSampleData();
                Debug.Log("AirboostAudio: Preloaded boost start event sample data");
            }
        }

        if (!boostStopEvent.IsNull)
        {
            // Get the event description and preload its sample data
            RuntimeManager.StudioSystem.getEvent(boostStopEvent.ToString(), out boostStopEventDesc);
            if (boostStopEventDesc.isValid())
            {
                boostStopEventDesc.loadSampleData();
                Debug.Log("AirboostAudio: Preloaded boost stop event sample data");
            }
        }
    }

    private void CreateEventPools()
    {
        // Create start event pool
        if (!boostStartEvent.IsNull)
        {
            for (int i = 0; i < eventPoolSize; i++)
            {
                EventInstance instance = RuntimeManager.CreateInstance(boostStartEvent);
                if (instance.isValid())
                {
                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                    boostStartPool.Add(instance);
                    boostStartPoolTimers.Add(0f);
                }
            }
            Debug.Log($"AirboostAudio: Created pool of {boostStartPool.Count} boost start instances");
        }
        
        // Create stop event pool
        if (!boostStopEvent.IsNull)
        {
            for (int i = 0; i < eventPoolSize; i++)
            {
                EventInstance instance = RuntimeManager.CreateInstance(boostStopEvent);
                if (instance.isValid())
                {
                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                    boostStopPool.Add(instance);
                    boostStopPoolTimers.Add(0f);
                }
            }
            Debug.Log($"AirboostAudio: Created pool of {boostStopPool.Count} boost stop instances");
        }
    }

    private void Update()
    {
        // If not initialized yet and preload hasn't started, start it now
        if (!isReady && !hasStartedPreload)
        {
            StartCoroutine(PreloadWithPriority());
            return;
        }
        
        // Don't process input until fully ready
        if (!isReady) return;
        
        // First, update our instance pool timers and clean up if needed
        ManageInstancePools();
        
        // Detect boost key press
        if (Input.GetKeyDown(boostKey))
        {
            PlayBoostStartSound();
            isBoostActive = true;
        }
        
        // Detect boost key release
        if (Input.GetKeyUp(boostKey) && isBoostActive)
        {
            PlayBoostStopSound();
            isBoostActive = false;
        }
    }
    
    private void ManageInstancePools()
    {
        // Update timers for start instances
        for (int i = 0; i < boostStartPoolTimers.Count; i++)
        {
            if (boostStartPoolTimers[i] > 0)
            {
                boostStartPoolTimers[i] -= Time.deltaTime;
                
                // If timer expired, release instance and create a new one
                if (boostStartPoolTimers[i] <= 0)
                {
                    if (i < boostStartPool.Count && boostStartPool[i].isValid())
                    {
                        FMOD.Studio.PLAYBACK_STATE state;
                        boostStartPool[i].getPlaybackState(out state);
                        
                        // Only release if not playing anymore
                        if (state != FMOD.Studio.PLAYBACK_STATE.PLAYING)
                        {
                            boostStartPool[i].release();
                            boostStartPool[i] = RuntimeManager.CreateInstance(boostStartEvent);
                            boostStartPool[i].set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                        }
                        else
                        {
                            // If still playing, reset timer to check again later
                            boostStartPoolTimers[i] = 0.5f;
                        }
                    }
                }
            }
        }
        
        // Update timers for stop instances
        for (int i = 0; i < boostStopPoolTimers.Count; i++)
        {
            if (boostStopPoolTimers[i] > 0)
            {
                boostStopPoolTimers[i] -= Time.deltaTime;
                
                // If timer expired, release instance and create a new one
                if (boostStopPoolTimers[i] <= 0)
                {
                    if (i < boostStopPool.Count && boostStopPool[i].isValid())
                    {
                        FMOD.Studio.PLAYBACK_STATE state;
                        boostStopPool[i].getPlaybackState(out state);
                        
                        // Only release if not playing anymore
                        if (state != FMOD.Studio.PLAYBACK_STATE.PLAYING)
                        {
                            boostStopPool[i].release();
                            boostStopPool[i] = RuntimeManager.CreateInstance(boostStopEvent);
                            boostStopPool[i].set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                        }
                        else
                        {
                            // If still playing, reset timer to check again later
                            boostStopPoolTimers[i] = 0.5f;
                        }
                    }
                }
            }
        }
    }

    private void PlayBoostStartSound()
    {
        if (!isReady || boostStartEvent.IsNull)
            return;
        
        // First try to use a pooled instance for lowest latency
        bool foundInstance = false;
        
        // Find an available instance in the pool
        for (int i = 0; i < boostStartPool.Count; i++)
        {
            FMOD.Studio.PLAYBACK_STATE state;
            boostStartPool[i].getPlaybackState(out state);
            
            if (state != FMOD.Studio.PLAYBACK_STATE.PLAYING)
            {
                // Update position before playing
                boostStartPool[i].set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                
                // Start the instance
                boostStartPool[i].start();
                boostStartPoolTimers[i] = instanceTTL;
                
                foundInstance = true;
                break;
            }
        }
        
        // If no instance was available, create a new one temporarily
        if (!foundInstance)
        {
            Debug.LogWarning("AirboostAudio: Pool exhausted, creating new temporary instance");
            RuntimeManager.PlayOneShot(boostStartEvent, transform.position);
        }
    }

    private void PlayBoostStopSound()
    {
        if (!isReady || boostStopEvent.IsNull)
            return;
            
        // First try to use a pooled instance for lowest latency
        bool foundInstance = false;
        
        // Find an available instance in the pool
        for (int i = 0; i < boostStopPool.Count; i++)
        {
            FMOD.Studio.PLAYBACK_STATE state;
            boostStopPool[i].getPlaybackState(out state);
            
            if (state != FMOD.Studio.PLAYBACK_STATE.PLAYING)
            {
                // Update position before playing
                boostStopPool[i].set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                
                // Start the instance
                boostStopPool[i].start();
                boostStopPoolTimers[i] = instanceTTL;
                
                foundInstance = true;
                break;
            }
        }
        
        // If no instance was available, create a new one temporarily
        if (!foundInstance)
        {
            Debug.LogWarning("AirboostAudio: Pool exhausted, creating new temporary instance");
            RuntimeManager.PlayOneShot(boostStopEvent, transform.position);
        }
    }

    private void OnDestroy()
    {
        // Clean up all pooled instances
        foreach (var instance in boostStartPool)
        {
            if (instance.isValid())
            {
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
            }
        }
        
        foreach (var instance in boostStopPool)
        {
            if (instance.isValid())
            {
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
            }
        }
        
        // Clear pools
        boostStartPool.Clear();
        boostStartPoolTimers.Clear();
        boostStopPool.Clear();
        boostStopPoolTimers.Clear();
        
        // Unload sample data
        if (boostStartEventDesc.isValid())
        {
            boostStartEventDesc.unloadSampleData();
        }
        
        if (boostStopEventDesc.isValid())
        {
            boostStopEventDesc.unloadSampleData();
        }
    }
}
