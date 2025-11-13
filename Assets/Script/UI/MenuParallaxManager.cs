using UnityEngine;
using System;

public class MenuParallaxManager : MonoBehaviour
{
    [SerializeField] private MenuBackground[] backgrounds;
    [SerializeField] private float globalSpeed = 1f;
    [SerializeField] private bool autoMove = true;
    
    public static MenuParallaxManager instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Store initial positions as anchors
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i].sprite)
                backgrounds[i].anchor = backgrounds[i].sprite.position;
        }
    }

    private void Update()
    {
        if (!autoMove) return;

        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (!backgrounds[i].sprite) continue;

            // Move each background layer at different speeds
            float layerSpeed = backgrounds[i].speed * backgrounds[i].intensity * globalSpeed;
            Vector3 movement = Vector3.right * layerSpeed * Time.deltaTime;
            backgrounds[i].sprite.Translate(movement);

            // Wrap around when object goes too far right
            if (backgrounds[i].sprite.position.x > backgrounds[i].wrapDistance)
            {
                Vector3 resetPos = backgrounds[i].sprite.position;
                resetPos.x = backgrounds[i].resetPosition;
                backgrounds[i].sprite.position = resetPos;
            }
        }
    }

    public void SetGlobalSpeed(float speed)
    {
        globalSpeed = speed;
    }

    public void PauseMovement(bool pause)
    {
        autoMove = !pause;
    }

    public void ResetBackgrounds()
    {
        Array.Resize(ref backgrounds, transform.childCount);
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (!backgrounds[i].sprite)
                backgrounds[i].intensity = GetDefaultIntensity(i);

            backgrounds[i].sprite = transform.GetChild(i);
            backgrounds[i].name = backgrounds[i].sprite.name;
            backgrounds[i].anchor = backgrounds[i].sprite.position;
        }
    }

    public void ResetIntensities()
    {
        for (int i = 0; i < backgrounds.Length; ++i)
            backgrounds[i].intensity = (float)(i + 1) / (backgrounds.Length + 1);
    }

    private float GetDefaultIntensity(int index)
    {
        return (float)(index + 1) / (backgrounds.Length + 1);
    }
}

[Serializable]
public struct MenuBackground
{
    [HideInInspector] public string name;
    [Range(0, 2)] public float intensity;
    [Range(0, 5)] public float speed;
    public Transform sprite;
    public Vector2 anchor;
    [Header("Wrapping")]
    public float wrapDistance;
    public float resetPosition;
}