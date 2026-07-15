using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteCursorRenderer : MonoBehaviour
{
    [SerializeField] protected Animator animator;

    protected virtual void OnEnable()
    {
        if (animator != null)
        {
            animator.SetBool("IsAnimationEnabled", UIManager.EnableAnimation);
        }
    }
}
