using System;
using System.Collections.Generic;

public class PriorityQueue
{
    private List<NodeWithPriority> _heap;
    private Dictionary<Node, int> _nodeLookup;

    public PriorityQueue()
    {
        _heap = new List<NodeWithPriority>();
        _nodeLookup = new Dictionary<Node, int>();
    }

    public void Enqueue(NodeWithPriority item)
    {
        if (_nodeLookup.ContainsKey(item.Node))
        {
            UpdatePriority(item);
            return;
        }

        _heap.Add(item);
        int index = _heap.Count - 1;
        _nodeLookup[item.Node] = index;
        HeapifyUp(index);
    }

    public NodeWithPriority Dequeue()
    {
        if (_heap.Count == 0) throw new InvalidOperationException("Queue is empty.");

        NodeWithPriority root = _heap[0];
        int lastIndex = _heap.Count - 1;

        _heap[0] = _heap[lastIndex];
        _nodeLookup[_heap[0].Node] = 0;

        _heap.RemoveAt(lastIndex);
        _nodeLookup.Remove(root.Node);

        HeapifyDown(0);
        return root;
    }

    public NodeWithPriority Peek()
    {
        if (_heap.Count == 0) throw new InvalidOperationException("Queue is empty.");
        return _heap[0];
    }

    public int Count => _heap.Count;

    public bool Contains(Node node)
    {
        return _nodeLookup.ContainsKey(node);
    }

    public void UpdatePriority(NodeWithPriority item)
    {
        if (!_nodeLookup.ContainsKey(item.Node)) return;

        int index = _nodeLookup[item.Node];
        _heap[index] = item;

        // Rebalance heap — priority might have increased or decreased
        HeapifyUp(index);
        HeapifyDown(index);
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (_heap[index].CompareTo(_heap[parentIndex]) >= 0) break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        int leftChild, rightChild, smallest;

        while (index < _heap.Count / 2)
        {
            leftChild = 2 * index + 1;
            rightChild = 2 * index + 2;
            smallest = index;

            if (leftChild < _heap.Count && _heap[leftChild].CompareTo(_heap[smallest]) < 0)
                smallest = leftChild;

            if (rightChild < _heap.Count && _heap[rightChild].CompareTo(_heap[smallest]) < 0)
                smallest = rightChild;

            if (smallest == index) break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int index1, int index2)
    {
        NodeWithPriority temp = _heap[index1];
        _heap[index1] = _heap[index2];
        _heap[index2] = temp;

        _nodeLookup[_heap[index1].Node] = index1;
        _nodeLookup[_heap[index2].Node] = index2;
    }
}
