using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SnowflakeTextureGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    [SerializeField] private int textureSize = 256;
    [SerializeField] private Color snowflakeColor = Color.white;
    [SerializeField] private Color glowColor = new Color(0.8f, 0.9f, 1f, 0.5f);
    [SerializeField] private int branches = 6;
    [SerializeField] private float complexity = 3f;

    private Texture2D snowflakeTexture;

    void Start()
    {
        GenerateSnowflakeTexture();
    }

    public Texture2D GenerateSnowflakeTexture()
    {
        snowflakeTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        snowflakeTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float u = (x - textureSize * 0.5f) / (textureSize * 0.5f);
                float v = (y - textureSize * 0.5f) / (textureSize * 0.5f);

                pixels[y * textureSize + x] = CalculateSnowflakePixel(u, v);
            }
        }

        snowflakeTexture.SetPixels(pixels);
        snowflakeTexture.Apply();

        return snowflakeTexture;
    }

    private Color CalculateSnowflakePixel(float u, float v)
    {
        float radius = Mathf.Sqrt(u * u + v * v);
        float angle = Mathf.Atan2(v, u);

        if (radius > 0.9f)
        {
            return Color.clear;
        }

        float snowflakeValue = 0f;

        // Generate symmetrical branches
        for (int i = 0; i < branches; i++)
        {
            float branchAngle = (2f * Mathf.PI * i) / branches;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, branchAngle * Mathf.Rad2Deg));

            if (angleDiff < 30f)
            {
                float branchStrength = 1f - (angleDiff / 30f);
                snowflakeValue = Mathf.Max(snowflakeValue, GetBranchPattern(radius, angleDiff) * branchStrength);
            }
        }

        // Add center crystal
        float centerCrystal = GetCenterCrystal(radius);
        snowflakeValue = Mathf.Max(snowflakeValue, centerCrystal);

        if (snowflakeValue <= 0)
        {
            return Color.clear;
        }

        // Add glow effect
        float glowEffect = Mathf.Max(0, 1f - radius * 1.2f) * 0.3f;

        Color finalColor = Color.Lerp(glowColor, snowflakeColor, snowflakeValue);
        finalColor.a = Mathf.Min(1f, snowflakeValue + glowEffect);

        return finalColor;
    }

    private float GetBranchPattern(float radius, float angleDiff)
    {
        // Main branch
        float mainBranch = 1f - angleDiff / 5f;
        mainBranch *= 1f - Mathf.Pow(radius, 2f);
        mainBranch = Mathf.Max(0, mainBranch);

        // Sub-branches
        float subBranches = 0f;

        // First level sub-branches
        if (radius > 0.2f && radius < 0.5f)
        {
            float subAngle = angleDiff - 15f;
            if (Mathf.Abs(subAngle) < 3f)
            {
                subBranches = Mathf.Max(subBranches, 1f - Mathf.Abs(subAngle) / 3f);
            }

            subAngle = angleDiff + 15f;
            if (Mathf.Abs(subAngle) < 3f)
            {
                subBranches = Mathf.Max(subBranches, 1f - Mathf.Abs(subAngle) / 3f);
            }
        }

        // Second level sub-branches
        if (radius > 0.4f && radius < 0.7f)
        {
            float subAngle = angleDiff - 10f;
            if (Mathf.Abs(subAngle) < 2f)
            {
                subBranches = Mathf.Max(subBranches, (1f - Mathf.Abs(subAngle) / 2f) * 0.8f);
            }

            subAngle = angleDiff + 10f;
            if (Mathf.Abs(subAngle) < 2f)
            {
                subBranches = Mathf.Max(subBranches, (1f - Mathf.Abs(subAngle) / 2f) * 0.8f);
            }
        }

        // Decorative crystals
        float crystals = 0f;
        if (radius > 0.15f && radius < 0.25f && angleDiff < 2f)
        {
            crystals = 1f - (radius - 0.2f) * 20f;
            crystals = Mathf.Max(0, crystals);
        }

        if (radius > 0.35f && radius < 0.45f && angleDiff < 2f)
        {
            crystals = Mathf.Max(crystals, 1f - (radius - 0.4f) * 20f);
            crystals = Mathf.Max(0, crystals);
        }

        return Mathf.Max(mainBranch, subBranches * 0.7f, crystals);
    }

    private float GetCenterCrystal(float radius)
    {
        // Hexagonal center
        if (radius < 0.15f)
        {
            return 1f;
        }

        // Ring pattern
        float ring = Mathf.Abs(radius - 0.18f);
        if (ring < 0.02f)
        {
            return 1f - ring / 0.02f;
        }

        return 0f;
    }

    public void SaveTexture()
    {
        if (snowflakeTexture != null)
        {
            byte[] bytes = snowflakeTexture.EncodeToPNG();
            string path = Application.dataPath + "/Textures/SnowflakeTexture.png";
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Textures");
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log("Snowflake texture saved to: " + path);

            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
    }
}