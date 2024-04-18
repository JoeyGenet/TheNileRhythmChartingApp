using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AlertDialog : MonoBehaviour
{    
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text messageText;
    [SerializeField] Button okButton;
    [SerializeField] Button cancelButton;

    public string Title
    {
        get => titleText.text;
        set => titleText.text = value;
    }

    public string Message
    {
        get => messageText.text;
        set => messageText.text = value;
    }

    public UnityEvent OnOkPressed;
    public UnityEvent OnCancelPressed;
    
    void Start()
    {
        okButton.onClick.AddListener(() =>
        {
            OnOkPressed?.Invoke();
            Destroy(gameObject);
        });
        if (cancelButton != null) cancelButton.onClick.AddListener(() =>
        {
            OnCancelPressed?.Invoke();
            Destroy(gameObject);
        });

        if (cancelButton != null) cancelButton.Select();
        else okButton.Select();
    }
}
