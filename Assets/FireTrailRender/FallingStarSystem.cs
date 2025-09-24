using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class FallingStarSystem : MonoBehaviour
{
    private ParticleSystem particleSystem;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;
    private ParticleSystem.SizeOverLifetimeModule sizeModule;
    private ParticleSystem.RotationOverLifetimeModule rotationModule;
    private ParticleSystem.ForceOverLifetimeModule forceModule;
    private ParticleSystem.NoiseModule noiseModule;

    [Header("落下する星の設定")]
    [SerializeField] private int maxParticleCount = 200;
    [SerializeField] private float spawnRate = 10f;
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 3f;
    [SerializeField] private float particleLifetime = 10f;

    [Header("落下設定")]
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float fallSpeedVariation = 2f;
    [SerializeField] private float swayStrength = 3f;
    [SerializeField] private float swayFrequency = 1f;

    [Header("サイズアニメーション")]
    [SerializeField] private bool enableSizePulsing = true;
    [SerializeField] private float sizeAnimationSpeed = 2f;
    [SerializeField] private float sizeAnimationStrength = 0.5f;

    [Header("スポーン範囲")]
    [SerializeField] private float spawnHeight = 30f;
    [SerializeField] private float spawnWidth = 40f;
    [SerializeField] private float spawnDepth = 10f;

    [Header("スポーン位置")]
    [SerializeField] private float spawnZ = -20f;

    [Header("色設定")]
    [SerializeField] private Color minStarColor = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private Color maxStarColor = new Color(1f, 0.4f, 0f, 1f);
    [SerializeField] private bool enableColorChange = true;

    [Header("回転設定")]
    [SerializeField] private float minRotationSpeed = -180f;
    [SerializeField] private float maxRotationSpeed = 180f;

    [Header("星のテクスチャ")]
    [SerializeField] private Texture2D starTexture;
    [SerializeField] private Material starMaterial;
    [SerializeField] private bool generateStarTexture = true;

    [Header("テクスチャジェネレーター選択")]
    [SerializeField] private bool useStarTexture = true;
    [SerializeField] private bool useLeafTexture = false;
    [SerializeField] private bool useSnowflakeTexture = false;

    private Camera mainCamera;
    private StarTextureGenerator starTextureGenerator;
    private LeafTextureGenerator leafTextureGenerator;
    private SnowflakeTextureGenerator snowflakeTextureGenerator;

    void Start()
    {
        // 同じGameObjectからテクスチャジェネレーターを取得
        starTextureGenerator = GetComponent<StarTextureGenerator>();
        leafTextureGenerator = GetComponent<LeafTextureGenerator>();
        snowflakeTextureGenerator = GetComponent<SnowflakeTextureGenerator>();

        // 選択されたテクスチャを生成
        if (generateStarTexture)
        {
            GenerateSelectedTexture();
        }

        SetupFallingStarSystem();
        ConfigureSpawnArea();
    }

    void GenerateSelectedTexture()
    {
        if (useStarTexture && starTextureGenerator != null)
        {
            starTexture = starTextureGenerator.GenerateStarTexture();
            Debug.Log("[FallingStar] Using star texture from generator");
        }
        else if (useLeafTexture && leafTextureGenerator != null)
        {
            starTexture = leafTextureGenerator.GenerateLeafTexture();
            Debug.Log("[FallingStar] Using leaf texture from generator");
        }
        else if (useSnowflakeTexture && snowflakeTextureGenerator != null)
        {
            starTexture = snowflakeTextureGenerator.GenerateSnowflakeTexture();
            Debug.Log("[FallingStar] Using snowflake texture from generator");
        }
        else if (useStarTexture)
        {
            // StarTextureGeneratorがない場合は自動で追加して生成
            starTextureGenerator = gameObject.AddComponent<StarTextureGenerator>();
            starTexture = starTextureGenerator.GenerateStarTexture();
            Debug.Log("[FallingStar] Added StarTextureGenerator and generated texture");
        }
    }

    void SetupFallingStarSystem()
    {
        particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            particleSystem = gameObject.AddComponent<ParticleSystem>();
        }

        mainCamera = Camera.main ?? FindObjectOfType<Camera>();

        // モジュールの取得
        mainModule = particleSystem.main;
        shapeModule = particleSystem.shape;
        emissionModule = particleSystem.emission;
        velocityModule = particleSystem.velocityOverLifetime;
        colorModule = particleSystem.colorOverLifetime;
        sizeModule = particleSystem.sizeOverLifetime;
        rotationModule = particleSystem.rotationOverLifetime;
        forceModule = particleSystem.forceOverLifetime;
        noiseModule = particleSystem.noise;

        ConfigureMainModule();
        ConfigureShape();
        ConfigureEmission();
        ConfigureVelocity();
        ConfigureFalling();
        ConfigureSizeAnimation();
        ConfigureColor();
        ConfigureRotation();
        ConfigureNoise();
        ConfigureRenderer();
    }

    void ConfigureMainModule()
    {
        mainModule.startLifetime = particleLifetime;
        mainModule.startSpeed = 0f;
        mainModule.maxParticles = maxParticleCount;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        mainModule.playOnAwake = true;
        mainModule.loop = true;

        // ランダムなサイズ
        mainModule.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);

        // ランダムな初期回転
        mainModule.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        // 色のグラデーション
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(minStarColor, 0f);
        colorKeys[1] = new GradientColorKey(maxStarColor, 1f);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        colorGradient.SetKeys(colorKeys, alphaKeys);
        mainModule.startColor = new ParticleSystem.MinMaxGradient(colorGradient);

        mainModule.gravityModifier = 0.3f; // 重力を少し追加
    }

    void ConfigureShape()
    {
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Box;
        shapeModule.scale = new Vector3(spawnWidth, 0.1f, spawnDepth);
        shapeModule.position = Vector3.zero; // ローカル位置は0
    }

    void ConfigureEmission()
    {
        emissionModule.enabled = true;
        emissionModule.rateOverTime = spawnRate;

        // バースト生成も追加（オプション）
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, 20);
        burst.repeatInterval = 5f;
        emissionModule.SetBursts(new ParticleSystem.Burst[] { burst });
    }

    void ConfigureVelocity()
    {
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.World; // World空間に変更

        // 落下速度（下方向） - より強く
        velocityModule.y = new ParticleSystem.MinMaxCurve(
            -(fallSpeed + fallSpeedVariation),
            -(fallSpeed - fallSpeedVariation)
        );

        // 横揺れ（落ち葉のような動き）
        AnimationCurve swayX = new AnimationCurve();
        swayX.AddKey(0f, 0f);
        swayX.AddKey(0.25f, 1f);
        swayX.AddKey(0.5f, 0f);
        swayX.AddKey(0.75f, -1f);
        swayX.AddKey(1f, 0f);

        AnimationCurve swayZ = new AnimationCurve();
        swayZ.AddKey(0f, 0f);
        swayZ.AddKey(0.33f, 0.5f);
        swayZ.AddKey(0.66f, -0.5f);
        swayZ.AddKey(1f, 0f);

        velocityModule.x = new ParticleSystem.MinMaxCurve(swayStrength, swayX);
        velocityModule.z = new ParticleSystem.MinMaxCurve(swayStrength * 0.5f, swayZ); // Z軸にも揺れを追加
    }

    void ConfigureFalling()
    {
        // Force Over Lifetimeで追加の落下効果
        forceModule.enabled = false; // 一旦無効化（VelocityModuleで制御）
    }

    void ConfigureSizeAnimation()
    {
        if (enableSizePulsing)
        {
            sizeModule.enabled = true;

            // サイズが脈動するアニメーション
            AnimationCurve sizeCurve = new AnimationCurve();
            int keyCount = 10;
            for (int i = 0; i <= keyCount; i++)
            {
                float t = i / (float)keyCount;
                float size = 1f + Mathf.Sin(t * Mathf.PI * 2f * sizeAnimationSpeed) * sizeAnimationStrength;
                sizeCurve.AddKey(t, size);
            }

            sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        }
        else
        {
            sizeModule.enabled = false;
        }
    }

    void ConfigureColor()
    {
        if (enableColorChange)
        {
            colorModule.enabled = true;

            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[5];

            // 色の変化（黄色→オレンジ→赤）
            colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 0.5f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0.25f);
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.5f);
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.4f, 0f), 0.75f);
            colorKeys[4] = new GradientColorKey(new Color(0.8f, 0.2f, 0f), 1f);

            // フェードイン・フェードアウト
            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 0.1f);
            alphaKeys[2] = new GradientAlphaKey(1f, 0.5f);
            alphaKeys[3] = new GradientAlphaKey(1f, 0.9f);
            alphaKeys[4] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            colorModule.color = new ParticleSystem.MinMaxGradient(gradient);
        }
        else
        {
            colorModule.enabled = false;
        }
    }

    void ConfigureRotation()
    {
        rotationModule.enabled = true;
        rotationModule.z = new ParticleSystem.MinMaxCurve(
            minRotationSpeed * Mathf.Deg2Rad,
            maxRotationSpeed * Mathf.Deg2Rad
        );
    }

    void ConfigureNoise()
    {
        // ノイズで自然な揺れを追加
        noiseModule.enabled = true;
        noiseModule.strength = new ParticleSystem.MinMaxCurve(swayStrength * 0.3f);
        noiseModule.frequency = swayFrequency;
        noiseModule.damping = true;
        noiseModule.octaveCount = 2;
        noiseModule.octaveMultiplier = 0.5f;
        noiseModule.octaveScale = 2f;
        noiseModule.quality = ParticleSystemNoiseQuality.High;
        noiseModule.scrollSpeed = new ParticleSystem.MinMaxCurve(0.5f);
        noiseModule.remapEnabled = false;
        noiseModule.positionAmount = new ParticleSystem.MinMaxCurve(1f);
        noiseModule.rotationAmount = new ParticleSystem.MinMaxCurve(0.2f);
        noiseModule.sizeAmount = new ParticleSystem.MinMaxCurve(0.1f);
    }

    void ConfigureRenderer()
    {
        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

        // テクスチャが設定されている場合
        if (starTexture != null)
        {
            Debug.Log($"[FallingStar] Using texture: {starTexture.name}");

            if (starMaterial == null)
            {
                // マテリアル作成
                Shader shader = Shader.Find("Mobile/Particles/Alpha Blended");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                }

                starMaterial = new Material(shader);
                starMaterial.name = "FallingStarMaterial";
            }

            // テクスチャ設定
            starMaterial.mainTexture = starTexture;
            starMaterial.SetTexture("_MainTex", starTexture);
            starMaterial.SetTexture("_BaseMap", starTexture);

            // ブレンディング設定
            starMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            starMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            starMaterial.SetInt("_ZWrite", 0);
            starMaterial.renderQueue = 3000;

            starMaterial.SetColor("_Color", Color.white);
            starMaterial.SetColor("_TintColor", Color.white);
            starMaterial.EnableKeyword("_ALPHABLEND_ON");
        }
        else if (starMaterial == null)
        {
            // デフォルトマテリアル
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            starMaterial = new Material(shader);
            starMaterial.name = "FallingStarMaterial_Default";
            starMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            starMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            starMaterial.SetInt("_ZWrite", 0);
            starMaterial.renderQueue = 3000;
        }

        renderer.material = starMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 1f;

        // テクスチャシートアニメーションを無効化
        var textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = false;

        renderer.mesh = null;
        renderer.enableGPUInstancing = false;

        Debug.Log($"[FallingStar] Renderer configured - Texture: {renderer.material?.mainTexture?.name ?? "null"}");
    }

    void ConfigureSpawnArea()
    {
        if (mainCamera != null)
        {
            // カメラの上方に配置しつつ、Zを固定
            Vector3 pos = mainCamera.transform.position;
            pos.y = spawnHeight;
            pos.z = spawnZ; // Zを固定
            transform.position = pos;
        }
    }

    void Update()
    {
        // カメラに追従（高さを維持）しつつ、Zを固定
        if (mainCamera != null)
        {
            Vector3 targetPosition = mainCamera.transform.position + Vector3.up * spawnHeight;
            targetPosition.y = spawnHeight; // Y位置を固定
            targetPosition.z = spawnZ;      // Zを固定（-20）
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f);
        }

        // デバッグ用キー操作
        if (Input.GetKeyDown(KeyCode.F))
        {
            ForceApplyTexture();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            particleSystem.Clear();
            particleSystem.Play();
        }

        // 数字キーでパラメータ調整
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            fallSpeed = Mathf.Max(1f, fallSpeed - 1f);
            ConfigureVelocity();
            Debug.Log($"Fall Speed: {fallSpeed}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            fallSpeed = Mathf.Min(20f, fallSpeed + 1f);
            ConfigureVelocity();
            Debug.Log($"Fall Speed: {fallSpeed}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            swayStrength = Mathf.Max(0f, swayStrength - 0.5f);
            ConfigureVelocity();
            ConfigureNoise();
            Debug.Log($"Sway Strength: {swayStrength}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            swayStrength = Mathf.Min(10f, swayStrength + 0.5f);
            ConfigureVelocity();
            ConfigureNoise();
            Debug.Log($"Sway Strength: {swayStrength}");
        }
    }

    [ContextMenu("Force Apply Star Texture")]
    public void ForceApplyTexture()
    {
        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

        if (starTexture == null)
        {
            Debug.LogError("[FallingStar] No star texture assigned!");
            return;
        }

        Material newMaterial = new Material(Shader.Find("UI/Default"));
        newMaterial.mainTexture = starTexture;
        renderer.sharedMaterial = newMaterial;

        Debug.Log($"[FallingStar] Force applied texture: {starTexture.name}");

        particleSystem.Clear();
        particleSystem.Play();
    }

    void OnDestroy()
    {
        if (starMaterial != null && Application.isPlaying)
        {
            Destroy(starMaterial);
        }
    }

    void OnDrawGizmosSelected()
    {
        // スポーンエリアの可視化
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Vector3 spawnCenter = transform.position + Vector3.up * spawnHeight;
        Gizmos.DrawWireCube(spawnCenter, new Vector3(spawnWidth, 0.1f, spawnDepth));

        // 落下範囲の表示
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
        Vector3 fallAreaCenter = transform.position;
        Gizmos.DrawWireCube(fallAreaCenter, new Vector3(spawnWidth, spawnHeight * 2, spawnDepth));
    }
}