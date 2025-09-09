using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ObjectPool<T> where T : class
{
    private Queue<T> pool = new Queue<T>();
    private System.Func<T> createFunc;
    private int maxSize;
    
    public ObjectPool(System.Func<T> createFunc, int initialSize)
    {
        this.createFunc = createFunc;
        this.maxSize = initialSize * 2;
        
        for (int i = 0; i < initialSize; i++)
        {
            pool.Enqueue(createFunc());
        }
    }
    
    public T Get()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        return createFunc();
    }
    
    public void Return(T item)
    {
        if (pool.Count < maxSize)
        {
            pool.Enqueue(item);
        }
    }
}