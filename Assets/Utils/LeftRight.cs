using System;
using UnityEngine;

public enum LeftRight
{
	Left, Right
}

public static class LeftRightExtensions
{
    public static LeftRight Other(this LeftRight leftRight) => leftRight switch
    {
        LeftRight.Left => LeftRight.Right,
        LeftRight.Right => LeftRight.Left,
        _ => throw new Exception($"Unsupported LeftRight {leftRight}.")
    };
}

public interface IReadOnlyLeftRightDictionary<T>
{
    public T Left { get; }
    public T Right { get; }

    public T this[LeftRight hand] => Get(hand);

    public T Get(LeftRight hand) => hand switch
    {
        LeftRight.Left => Left,
        LeftRight.Right => Right,
        _ => throw new Exception($"Hand {hand} not supported.")
    };

    public LeftRightDictionary<T> ToDictionary() => new(Left, Right);
}

public readonly struct LeftRightGetter<T> : IReadOnlyLeftRightDictionary<T>
{
    readonly Func<LeftRight, T> getter;

    public T Left => getter(LeftRight.Left);
    public T Right => getter(LeftRight.Right);

    public LeftRightGetter(Func<LeftRight, T> getter)
    {
        this.getter = getter;
    }
}

[Serializable]
public class LeftRightDictionary<T> : IReadOnlyLeftRightDictionary<T>
{
    [SerializeField] T left;
    [SerializeField] T right;

    public T Left
    {
        get => left;
        set => left = value;
    }
    public T Right
    {
        get => right;
        set => right = value;
    }

    public T this[LeftRight hand]
    {
        get => ((IReadOnlyLeftRightDictionary<T>) this).Get(hand);
        set => Set(hand, value);
    }

    public void Set(LeftRight hand, T value)
    {
        switch (hand)
        {
            case LeftRight.Left:
                Left = value;
                break;
            case LeftRight.Right:
                Right = value;
                break;
            default:
                throw new Exception($"Hand {hand} not supported.");
        }
    }

    public LeftRightDictionary(T left, T right)
    {
        this.left = left;
        this.right = right;
    }

    public LeftRightDictionary() {}
}