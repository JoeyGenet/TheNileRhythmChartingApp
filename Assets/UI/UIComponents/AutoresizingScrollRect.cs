using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect), typeof(LayoutElement))]
public class AutoresizingScrollRect : MonoBehaviour
{
    public int MinHeight = 50;
    public int MaxHeight = 200;

    ScrollRect scrollRect;
    LayoutElement layoutElement;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        layoutElement = GetComponent<LayoutElement>();
    }

    // Update is called once per frame
    void Update()
    {
        layoutElement.preferredHeight = Mathf.Clamp(scrollRect.content.rect.height, MinHeight, MaxHeight);
    }
}
