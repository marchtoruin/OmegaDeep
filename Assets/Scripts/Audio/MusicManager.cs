using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MusicManager : MonoBehaviour
{
    [SerializeField]
    private EventReference musicEvent;

    private EventInstance musicInstance;

    private void Start()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicInstance.start();
        musicInstance.release(); // Optional: lets FMOD clean it up later
    }
}
