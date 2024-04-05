using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEditable
{
    public bool HasChanges { get; }

    public void SetChanged();
}


public static class IEditableExtensions
{
    public static bool RegisterChange<T>(this IEditable editable, ref T value, T newValue, bool notify = true, Action notifyAction = null)
    {
        if (newValue.Equals(value)) return false;
        value = newValue;
        if (notify)
        {
            editable.SetChanged();
            notifyAction?.Invoke();
        }
        return true;
    }
}