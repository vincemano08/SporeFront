using System.Collections.Generic;
using System.Linq;

public class PriorityQueue<T> {
    private readonly SortedDictionary<float, Queue<T>> elements = new();

    public int Count { get; private set; }

    public void Enqueue(T item, float priority) {
        if (!elements.ContainsKey(priority)) elements[priority] = new Queue<T>();
        elements[priority].Enqueue(item);
        Count++;
    }

    public T Dequeue() {
        if (Count == 0) return default;

        var firstQueue = elements.First().Value;
        var item = firstQueue.Dequeue();
        if (firstQueue.Count == 0) elements.Remove(elements.First().Key);

        Count--;
        return item;
    }

    public bool Contains(T item) {
        foreach (var queue in elements.Values) {
            if (queue.Contains(item)) return true;
        }
        return false;
    }
}
