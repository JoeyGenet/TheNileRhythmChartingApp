/// <summary>
/// Sends out an event when multiple keys are pressed at a time. This can be used
/// to create complex shortcuts for actions in the
/// </summary>

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MultikeyShortcutEventRelay : MonoBehaviour
{
    public List<KeyCode> holdKeys1;
    public List<KeyCode> holdKeys2;
    public List<KeyCode> pressKeys;
    public List<KeyCode> excludeKeys;

    public UnityEvent Event;

    void Update()
    {
        // one key from this list must be held to activate the shortcut
        bool isHoldKey1Held = holdKeys1.Count == 0;
        foreach (KeyCode keyBind in holdKeys1)
        {
            if (Input.GetKey(keyBind) && !UIManager.IsSelectingInputField)
            {
                isHoldKey1Held = true;
            }
        }
        // one key from this list must be held to activate the shortcut
        bool isHoldKey2Held = holdKeys2.Count == 0;
        foreach (KeyCode keyBind in holdKeys2)
        {
            if (Input.GetKey(keyBind) && !UIManager.IsSelectingInputField)
            {
                isHoldKey2Held = true;
            }
        }
        // one key from this list must be held to activate the shortcut
        bool isPressKeyPressed = pressKeys.Count == 0;
        foreach (KeyCode keyBind in pressKeys)
        {
            if (Input.GetKeyUp(keyBind) && !UIManager.IsSelectingInputField)
            {
                isPressKeyPressed = true;
            }
        }
        // if any keys from this list are held, the shortcut may not be performed
        bool isexcludedKeyHeld = false;
        foreach (KeyCode keyBind in excludeKeys)
        {
            if (Input.GetKey(keyBind) && !UIManager.IsSelectingInputField)
            {
                isexcludedKeyHeld = true;
            }
        }

        if (isHoldKey1Held && isHoldKey2Held && isPressKeyPressed && !isexcludedKeyHeld)
        {
            SendEvent();
        }
    }

    public void AddListener(UnityAction call) => Event.AddListener(call);
    public void RemoveListener(UnityAction call) => Event.RemoveListener(call);

    void SendEvent() => SendEvent(true);
    public void SendEvent(bool notify = true)
    {
        if (notify)
        {
            Event?.Invoke();
        }
    }
}
