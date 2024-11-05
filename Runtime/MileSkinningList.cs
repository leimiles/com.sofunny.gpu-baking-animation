using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MileSkinningList<T>
{
    public T[] buffer;
    public int size = 0;
    private int bufferIncrement = 0;
    public T this[int i]
    {
        get { return buffer[i]; }
        set { buffer[i] = value; }
    }

    public MileSkinningList(int bufferIncrement)
    {
        this.bufferIncrement = Mathf.Max(1, bufferIncrement);
    }

    public void Add(T item)
    {
        if (buffer == null || size == buffer.Length) AllocateMore();
        buffer[size++] = item;
    }

    public void RemoveAt(int index)
    {
        if (buffer != null && index > -1 && index < size)
        {
            --size;
            buffer[index] = default(T);
            for (int b = index; b < size; ++b) buffer[b] = buffer[b + 1];
            buffer[size] = default(T);
        }
    }

    void AllocateMore()
    {
        T[] newList = (buffer != null) ? new T[buffer.Length + bufferIncrement] : new T[bufferIncrement];
        if (buffer != null && size > 0) buffer.CopyTo(newList, 0);
        buffer = newList;
    }
    public void Clear() { size = 0; }

    public void Release() { size = 0; buffer = null; }
}
