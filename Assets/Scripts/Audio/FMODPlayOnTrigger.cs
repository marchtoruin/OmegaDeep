using UnityEngine;
using FMODUnity;

public class FMODPlayOnTrigger : MonoBehaviour
{
    public StudioEventEmitter emitter;

    private void OnTriggerEnter2D(Collider2D other)
    {
        emitter.Play();
    }
}
