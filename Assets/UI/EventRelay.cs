/// <summary>
/// Relays events from button, sub-relayers, key presses, etc
/// Can be used to reference buttons deeper in the scene tree without needing to click deep into the tree
/// Can also be used to add keybinds to buttons
/// </summary>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EventRelay : MonoBehaviour
{
    public List<Button> buttons;
    public List<EventRelay> subRelays;
    public List<KeyCode> keyBinds;
    public List<AudioClip> audioClips;

    public UnityEvent Event;

    readonly List<AudioSource> audioSources = new();

    void Start()
    {
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(SendEvent);
        }
        foreach (EventRelay buttonGroup in subRelays)
        {
            buttonGroup.Event.AddListener(SendEvent);
        }
    }

    void Update()
    {
        foreach (KeyCode keyBind in keyBinds)
        {
            if (Input.GetKeyUp(keyBind))
            {
                SendEvent();
            }
        }

        foreach (AudioSource audioSource in audioSources)
        {
            if (!audioSource.isPlaying) Destroy(audioSource);
        }
    }

    public void AddListener(UnityAction call) => Event.AddListener(call);
    public void RemoveListener(UnityAction call) => Event.RemoveListener(call);

    void SendEvent()
    {
        Event?.Invoke();
        foreach (var audioClip in audioClips)
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.loop = false;
            audioSource.Play();
        }
    }
}
