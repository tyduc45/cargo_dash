using System;
using System.Collections.Generic;
using UnityEngine;


public class PauseManager : MonoBehaviour
{
    // Inspector: add specific instances you never want paused (GameManager, UI, SoundManager, etc.)
    public List<MonoBehaviour> exceptionInstances = new List<MonoBehaviour>();

    // Inspector: add type names (class names) to exclude from pausing, e.g. "GameManager", "UIManager", "SoundManager"
    public List<string> exceptionTypeNames = new List<string> { "GameManager", "UIManager", "SoundManager" };

    // Keeps original enabled state so ResumeAll restores correctly
    private readonly Dictionary<MonoBehaviour, bool> originalStates = new Dictionary<MonoBehaviour, bool>();

    // Pause everything except exceptions and their children
    public void PauseAll()
    {
        originalStates.Clear();

        // Use FindObjectsOfType<T>(true) to include disabled objects and avoid deprecated APIs
        var all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);

        // Collect root transforms to exclude (explicit instances + any instance matching exceptionTypeNames)
        var exceptionRoots = new HashSet<Transform>();
        foreach (var inst in exceptionInstances)
        {
            if (inst != null)
                exceptionRoots.Add(inst.transform);
        }

        foreach (var mb in all)
        {
            if (mb == null) continue;
            var tn = mb.GetType().Name;
            if (exceptionTypeNames.Contains(tn))
                exceptionRoots.Add(mb.transform);
        }

        foreach (var mb in all)
        {
            if (mb == null)
                continue;

            // never pause this PauseManager
            if (mb == this)
                continue;

            // skip explicitly referenced instances
            if (exceptionInstances.Contains(mb))
                continue;

            // skip if this MB is a child (or same GameObject) of any exception root
            if (IsChildOfAny(mb.transform, exceptionRoots))
                continue;

            // store original state if not already stored
            if (!originalStates.ContainsKey(mb))
                originalStates[mb] = mb.enabled;

            // disable behaviour (pauses Update/FixedUpdate/LateUpdate callbacks)
            mb.enabled = false;

            // coroutines started on a MonoBehaviour are not always stopped by disabling it,
            // stop them explicitly to fully "pause" logic running on that component
            try
            {
                mb.StopAllCoroutines();
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }

    // Restore previously saved enabled states
    public void ResumeAll()
    {
        foreach (var kv in originalStates)
        {
            var mb = kv.Key;
            if (mb == null)
                continue;

            try
            {
                mb.enabled = kv.Value;
            }
            catch (Exception)
            {
                // ignore components that may have been destroyed
            }
        }

        originalStates.Clear();
    }

    private bool IsChildOfAny(Transform t, HashSet<Transform> roots)
    {
        if (t == null) return false;
        var cur = t;
        while (cur != null)
        {
            if (roots.Contains(cur))
                return true;
            cur = cur.parent;
        }
        return false;
    }
}