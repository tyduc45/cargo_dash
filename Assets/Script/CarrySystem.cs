using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Display/manage the objects currently carried by the player (simulate a stack using List)
/// </summary>
public class CarryUI : MonoBehaviour
{
    [Header("Follow")]
    public Transform followTarget;
    public Vector3 offset = new Vector3(0, 2f, 0);

    [Header("Stack Settings")]
    public int capacity = 3;
    public Vector3 spacing = new Vector3(0.5f, 0, 0); // Relative position between multiple objects

    [SerializeField] private List<GameObject> _stack = new List<GameObject>(); // Stack string object references

    void Awake()
    {
        if (followTarget == null) followTarget = transform;
    }

    void LateUpdate()
    {
        // Update mount point of the entire stack
        if (followTarget != null)
        {
            Vector3 basePos = followTarget.position + offset;

            for (int i = 0; i < _stack.Count; i++)
            {
                GameObject cargo = _stack[i];
                if (cargo == null) continue;

                cargo.transform.position = basePos + spacing * i;
                cargo.transform.rotation = Quaternion.identity;
            }
        }
    }

    public int Count => _stack.Count;

    /// <summary>
    /// Push: add a cargo to the top of the stack
    /// </summary>
    public void Push(GameObject cargo)
    {
        if (cargo == null) return;
        if (_stack.Count >= capacity)
        {
            Debug.Log($"You cannot carry more than {capacity} objects");
            return;
        }

        // Disable physics to prevent cargo from moving unpredictably on the player
        var rb = cargo.GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;

        cargo.transform.SetParent(null); // Detach from any other parent
        _stack.Add(cargo);
    }

    /// <summary>
    /// Pop: remove the top cargo from the stack
    /// </summary>
    public GameObject Pop()
    {
        if (_stack.Count == 0) return null;

        var last = _stack[_stack.Count - 1];
        _stack.RemoveAt(_stack.Count - 1);

        if (last != null)
        {
            // Re-enable physics when dropping
            var rb = last.GetComponent<Rigidbody2D>();
            if (rb) rb.simulated = true;
        }

        return last;
    }

    /// <summary>
    /// Clear the stack
    /// </summary>
    public void Clear()
    {
        foreach (var cargo in _stack)
        {
            if (cargo != null)
            {
                var rb = cargo.GetComponent<Rigidbody2D>();
                if (rb) rb.simulated = true;
            }
        }
        _stack.Clear();
    }
}
