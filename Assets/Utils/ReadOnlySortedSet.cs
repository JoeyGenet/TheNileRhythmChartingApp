using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface IReadOnlySortedSet<T> : IReadOnlyCollection<T>
{
    T First { get; }
    T Last { get; }
    bool Contains(T item);
    IReadOnlySortedSet<T> GetViewBetween(T lowerValue, T upperValue);
}

public readonly struct ReadOnlySortedSet<T> : IReadOnlySortedSet<T>
{
    readonly SortedSet<T> set;

    public int Count => set.Count;
    public T First => set.Min;
    public T Last => set.Max;

    public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => set.GetEnumerator();

    public bool Contains(T item) => set.Contains(item);

    public IReadOnlySortedSet<T> GetViewBetween(T lowerValue, T upperValue)
        => (ReadOnlySortedSet<T>) set.GetViewBetween(lowerValue, upperValue);

    public ReadOnlySortedSet(SortedSet<T> set)
    {
        this.set = set;
    }

    public static implicit operator ReadOnlySortedSet<T>(SortedSet<T> set) => new(set);
}

public readonly struct ReadOnlySortedSet<T, B> : IReadOnlySortedSet<T>
where T : B
{
    readonly SortedSet<B> set;

    public int Count => set.Count;
    public T First => (T) set.Min;
    public T Last => (T) set.Max;

    public IEnumerator<T> GetEnumerator() => set.Cast<T>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => set.GetEnumerator();

    public bool Contains(T item) => set.Contains(item);

    public IReadOnlySortedSet<T> GetViewBetween(T lowerValue, T upperValue)
        => (ReadOnlySortedSet<T, B>) set.GetViewBetween(lowerValue, upperValue);

    public ReadOnlySortedSet(SortedSet<B> set)
    {
        this.set = set;
    }

    public static implicit operator ReadOnlySortedSet<T, B>(SortedSet<B> set) => new(set);
}
