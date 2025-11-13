using System.Collections.Generic;
using UnityEngine;

public class DisplayTutorial : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private KeyCode activationKey = KeyCode.Return;

    [Header("Game Objects to Control")]
    [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();

    private bool isActivated = false;

    void Update()
    {
        // change state every time player press the return key
        if (Input.GetKeyDown(activationKey))
        {
            if (!isActivated)
                ActivateObjects();
            else
                DeactivateObjects();
        }
    }

    private void ActivateObjects()
    {
        isActivated = true;
        Time.timeScale = 0f; // stop the game
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    private void DeactivateObjects()
    {
        isActivated = false;
        Time.timeScale = 1f; // recover the game
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}
