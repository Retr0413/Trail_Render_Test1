using UnityEngine;

public class StarTextureGenerator : MonoBehaviour
{
    [Header("テクスチャ設定")]
    [SerializeField] private int textureSize = 256;
    [SerializeField] private int starPoints = 5;
    [SerializeField] private float innerRadius = 0.3f;
    [SerializeField] private float outerRadius = 1f;
    [SerializeField] private bool addGlow = true;
    [SerializeField] private float glowIntensity = 0.5f;

    private Texture2D starTexture;

    public Texture2D GenerateStarTexture()
    {
        starTexture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, true);
        starTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[textureSize * textureSize];
        Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
        float maxRadius = textureSize * 0.5f * outerRadius;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 currentPos = new Vector2(x, y);
                Vector2 fromCenter = currentPos - center;
                float distance = fromCenter.magnitude;
                float angle = Mathf.Atan2(fromCenter.y, fromCenter.x);

                // 星形の計算
                float starValue = CalculateStarShape(distance / maxRadius, angle);

                // グロー効果の追加
                if (addGlow)
                {
                    float glowValue = CalculateGlow(distance / maxRadius);
                    starValue = Mathf.Max(starValue, glowValue * glowIntensity);
                }

                // 色とアルファ値の設定
                Color pixelColor = Color.white;
                pixelColor.a = starValue;

                int index = y * textureSize + x;
                pixels[index] = pixelColor;
            }
        }

        starTexture.SetPixels(pixels);
        starTexture.Apply();

        return starTexture;
    }

    float CalculateStarShape(float normalizedDistance, float angle)
    {
        if (normalizedDistance > 1f) return 0f;

        // 星の各頂点の角度を計算
        float anglePerPoint = (2f * Mathf.PI) / starPoints;
        float halfAnglePerPoint = anglePerPoint * 0.5f;

        // 現在の角度がどの頂点に最も近いか
        float adjustedAngle = angle + Mathf.PI;
        float pointIndex = adjustedAngle / anglePerPoint;
        float localAngle = (adjustedAngle % anglePerPoint);

        // 内側と外側の半径を角度に応じて補間
        float radiusRatio;
        if (localAngle < halfAnglePerPoint)
        {
            // 外側の頂点に向かう
            float t = localAngle / halfAnglePerPoint;
            radiusRatio = Mathf.Lerp(innerRadius, 1f, t);
        }
        else
        {
            // 内側の谷に向かう
            float t = (localAngle - halfAnglePerPoint) / halfAnglePerPoint;
            radiusRatio = Mathf.Lerp(1f, innerRadius, t);
        }

        // 距離に基づいてアルファ値を計算
        if (normalizedDistance <= radiusRatio)
        {
            // 星の内部
            float edgeFade = 1f - Mathf.Pow(normalizedDistance / radiusRatio, 2f);
            return edgeFade;
        }

        return 0f;
    }

    float CalculateGlow(float normalizedDistance)
    {
        if (normalizedDistance > 2f) return 0f;

        // ガウシアンブラー風のグロー
        float glow = Mathf.Exp(-normalizedDistance * normalizedDistance * 2f);
        return glow * 0.5f;
    }

    void Start()
    {
        // StarParticleSystemコンポーネントを探して、テクスチャを設定
        StarParticleSystem starSystem = GetComponent<StarParticleSystem>();
        if (starSystem != null)
        {
            Texture2D texture = GenerateStarTexture();

            // リフレクションで private フィールドにアクセス
            var field = typeof(StarParticleSystem).GetField("starTexture",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(starSystem, texture);
            }

            // パーティクルシステムを再起動
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }
        }
    }

    [ContextMenu("Generate Star Texture")]
    public void GenerateAndSaveTexture()
    {
        Texture2D texture = GenerateStarTexture();
        Debug.Log("Star texture generated. Assign it to StarParticleSystem's starTexture field.");
    }
}