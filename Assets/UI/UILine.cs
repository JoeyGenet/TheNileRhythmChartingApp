using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class UILine : MonoBehaviour
{
    RectTransform rect;
    Image image;

    public RectTransform Rect => rect;
    public Image Image => image;

    public Vector2 pos1 = Vector2.one / 2;
    public Vector2 pos2 = Vector2.one / 2;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        rect.anchorMin = rect.anchorMax = (pos1 + pos2) / 2;

        Vector2 parentSize = (rect.parent as RectTransform).rect.size;
        Vector2 vec = Vector2.Scale(pos1 - pos2, parentSize);

        if (vec != Vector2.zero)
            rect.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, vec));
        
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, vec.magnitude + rect.sizeDelta.x);

        image.pixelsPerUnitMultiplier = 100 / rect.sizeDelta.x;
    }
}
