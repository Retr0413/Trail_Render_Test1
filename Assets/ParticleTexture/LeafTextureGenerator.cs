using UnityEngine;

[System.Serializable]
public class LeafTextureGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    [SerializeField] private int textureSize = 256;
    [SerializeField] private Color leafBaseColor = new Color(0.4f, 0.6f, 0.2f);
    [SerializeField] private Color leafTipColor = new Color(0.8f, 0.5f, 0.1f);
    [SerializeField] private Color veinColor = new Color(0.3f, 0.4f, 0.15f);

    private Texture2D leafTexture;

    void Start()
    {
        GenerateLeafTexture();
    }

    public Texture2D GenerateLeafTexture()
    {
        leafTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        leafTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float u = (x - textureSize * 0.5f) / (textureSize * 0.5f);
                float v = (y - textureSize * 0.5f) / (textureSize * 0.5f);

                pixels[y * textureSize + x] = CalculateLeafPixel(u, v);
            }
        }

        leafTexture.SetPixels(pixels);
        leafTexture.Apply();

        return leafTexture;
    }

    private Color CalculateLeafPixel(float u, float v)
    {
        // Leaf shape (elliptical with pointed tip)
        float leafShape = GetLeafShape(u, v);

        if (leafShape <= 0)
        {
            return Color.clear;
        }

        // Add veins
        float veinPattern = GetVeinPattern(u, v);

        // Gradient from base to tip
        float gradient = Mathf.InverseLerp(-1f, 1f, v);
        Color baseGradient = Color.Lerp(leafBaseColor, leafTipColor, gradient);

        // Mix with vein color
        Color finalColor = Color.Lerp(baseGradient, veinColor, veinPattern * 0.3f);
        finalColor.a = leafShape;

        // Add slight noise for natural look
        float noise = Mathf.PerlinNoise(u * 10f, v * 10f) * 0.1f;
        finalColor = Color.Lerp(finalColor, leafTipColor, noise);
        finalColor.a = leafShape;

        return finalColor;
    }

    private float GetLeafShape(float u, float v)
    {
        // Create pointed leaf shape
        float width = 0.4f * (1f - Mathf.Abs(v) * 0.8f);
        width *= Mathf.Max(0, 1f - v * 0.3f); // Taper towards top

        if (Mathf.Abs(u) > width || v < -0.9f || v > 0.9f)
        {
            return 0;
        }

        // Smooth edges
        float edgeFade = 1f - Mathf.Pow(Mathf.Abs(u) / width, 3f);
        float tipFade = 1f - Mathf.Pow(Mathf.Max(0, v) / 0.9f, 2f);
        float baseFade = 1f - Mathf.Pow(Mathf.Max(0, -v) / 0.9f, 4f);

        return edgeFade * tipFade * baseFade;
    }

    private float GetVeinPattern(float u, float v)
    {
        // Central vein
        float centralVein = 1f - Mathf.Abs(u) * 20f;
        centralVein = Mathf.Max(0, centralVein);

        // Side veins
        float sideVein1 = 1f - Mathf.Abs(u - v * 0.2f) * 30f;
        float sideVein2 = 1f - Mathf.Abs(u + v * 0.2f) * 30f;
        float sideVein3 = 1f - Mathf.Abs(u - v * 0.1f - 0.1f) * 40f;
        float sideVein4 = 1f - Mathf.Abs(u + v * 0.1f + 0.1f) * 40f;

        sideVein1 = Mathf.Max(0, sideVein1);
        sideVein2 = Mathf.Max(0, sideVein2);
        sideVein3 = Mathf.Max(0, sideVein3);
        sideVein4 = Mathf.Max(0, sideVein4);

        return Mathf.Max(centralVein, sideVein1, sideVein2, sideVein3, sideVein4);
    }

    public void SaveTexture()
    {
        if (leafTexture != null)
        {
            byte[] bytes = leafTexture.EncodeToPNG();
            string path = Application.dataPath + "/Textures/LeafTexture.png";
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Textures");
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log("Leaf texture saved to: " + path);

            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
    }
}