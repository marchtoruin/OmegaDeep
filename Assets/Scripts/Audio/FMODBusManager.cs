/* This script allows you to have a global area in Unity where FMOD Buses (volume, mute, solo) can be controlled in realtime.
 
 Note that this is meant to only be used as a tool to speed up audio workflow in the Unity Editor. 
 Ideally, one should probably remove this behaviour from a Game Object when done, but just in case 
 one gets added -- this script does not do anything outside of the Unity Editor.
 
 */

using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODBusManager : MonoBehaviour
{
    [System.Serializable]
    public class BusControl
    {
        [Tooltip("FMOD Bus Path, e.g., 'bus:/MusicBus'")]
        public string busPath;

        [Range(-80f, 10f)]
        [Tooltip("Volume slider for this bus (-80 dB to 10 dB)")]
        public float busVolume;

        [Tooltip("Mute this bus")]
        public bool mute;

        [Tooltip("Solo this bus (mutes all other buses)")]
        public bool solo;
    }

    [SerializeField]
    [Tooltip("List of FMOD buses to control")]
    private List<BusControl> busControls = new List<BusControl>();
    
// Do absolutely nothing if this isn't the Editor
#if UNITY_EDITOR
    private Dictionary<string, FMOD.Studio.Bus> buses = new Dictionary<string, FMOD.Studio.Bus>();

    void Start()
    {
        // Initialize buses
        foreach (var busControl in busControls)
        {
            if (!string.IsNullOrEmpty(busControl.busPath))
            {
                FMOD.Studio.Bus bus = FMODUnity.RuntimeManager.GetBus(busControl.busPath);
                buses[busControl.busPath] = bus;
            }
        }
    }

    void Update()
    {
        
        bool isSoloActive = false;

        // Check if any bus is in solo mode
        foreach (var busControl in busControls)
        {
            if (busControl.solo)
            {
                isSoloActive = true;
                break;
            }
        }

        foreach (var busControl in busControls)
        {
            if (buses.TryGetValue(busControl.busPath, out var bus))
            {
                // Convert dB to linear volume
                float volume = Mathf.Pow(10.0f, busControl.busVolume / 20f);
                bus.setVolume(volume);

                // Set Mute
                if (isSoloActive)
                {
                    // If solo is active, mute this bus unless it's soloed
                    bus.setMute(!busControl.solo);
                }
                else
                {
                    // No solo active, use the mute setting
                    bus.setMute(busControl.mute);
                }
            }
        }
    }
#endif
}
