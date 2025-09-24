using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class StarParticleSystem : MonoBehaviour
{
    private ParticleSystem particleSystem;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;
    private ParticleSystem.SizeOverLifetimeModule sizeModule;
    private ParticleSystem.RotationOverLifetimeModule rotationModule;

    [Header("パーティクル設定")]
    [SerializeField] private int particleCount = 500;
    [SerializeField] private float minSize = 1f;  // サイズを大きく
    [SerializeField] private float maxSize = 3f;  // サイズを大きく
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float fadeOutDuration = 3f;

    [Header("無限ループ設定")]
    [SerializeField] private bool useInfiniteLoop = true;
    [SerializeField] private float minVisibleTime = 5f;
    [SerializeField] private float maxVisibleTime = 15f;
    [SerializeField] private float minHiddenTime = 3f;
    [SerializeField] private float maxHiddenTime = 8f;

    [Header("画面配置設定")]
    [SerializeField] private float screenDepth = 50f;
    [SerializeField] private float screenPadding = 1.2f;

    [Header("色設定")]
    [SerializeField] private Color minColor = new Color(1f, 0.2f, 0f, 1f);
    [SerializeField] private Color maxColor = new Color(1f, 0.6f, 0f, 1f);

    [Header("アニメーション設定")]
    [SerializeField] private float twinkleSpeed = 2f;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private bool enablePulsing = true;
    [SerializeField] private float pulseFrequency = 1f;
    [SerializeField] private float pulseAmplitude = 0.3f;

    [Header("星の形状設定")]
    [SerializeField] private Texture2D starTexture;
    [SerializeField] private Material starMaterial;

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
        GenerateSelectedTexture();

        SetupParticleSystem();
        ConfigureScreenFilling();

        // 遅延実行でテクスチャを再適用
        StartCoroutine(ApplyTextureDelayed());
    }

    void GenerateSelectedTexture()
    {
        if (useStarTexture && starTextureGenerator != null)
        {
            starTexture = starTextureGenerator.GenerateStarTexture();
            Debug.Log("[StarParticle] Using star texture from generator");
        }
        else if (useLeafTexture && leafTextureGenerator != null)
        {
            starTexture = leafTextureGenerator.GenerateLeafTexture();
            Debug.Log("[StarParticle] Using leaf texture from generator");
        }
        else if (useSnowflakeTexture && snowflakeTextureGenerator != null)
        {
            starTexture = snowflakeTextureGenerator.GenerateSnowflakeTexture();
            Debug.Log("[StarParticle] Using snowflake texture from generator");
        }
        else if (useStarTexture)
        {
            // StarTextureGeneratorがない場合は自動で追加して生成
            starTextureGenerator = gameObject.AddComponent<StarTextureGenerator>();
            starTexture = starTextureGenerator.GenerateStarTexture();
            Debug.Log("[StarParticle] Added StarTextureGenerator and generated texture");
        }
    }

    System.Collections.IEnumerator ApplyTextureDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        ForceApplyTexture();
    }

    void SetupParticleSystem()
    {
        particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            particleSystem = gameObject.AddComponent<ParticleSystem>();
        }

        mainCamera = Camera.main ?? FindObjectOfType<Camera>();

        mainModule = particleSystem.main;
        shapeModule = particleSystem.shape;
        emissionModule = particleSystem.emission;
        velocityModule = particleSystem.velocityOverLifetime;
        colorModule = particleSystem.colorOverLifetime;
        sizeModule = particleSystem.sizeOverLifetime;
        rotationModule = particleSystem.rotationOverLifetime;

        ConfigureMainModule();
        ConfigureShape();
        ConfigureEmission();
        ConfigureVelocity();
        ConfigureColor();
        ConfigureSize();
        ConfigureRotation();
        ConfigureRenderer();
    }

    void ConfigureMainModule()
    {
        if (useInfiniteLoop)
        {
            // 無限ループモード: 非常に長い寿命を設定
            mainModule.startLifetime = 10000f;
            mainModule.loop = false; // パーティクルシステム自体のループは無効
        }
        else
        {
            // 通常モード
            mainModule.startLifetime = new ParticleSystem.MinMaxCurve(minVisibleTime + minHiddenTime,
                                                                     maxVisibleTime + maxHiddenTime);
            mainModule.loop = true;
        }

        mainModule.startSpeed = 0f;
        mainModule.maxParticles = particleCount; // 正確に500個に制限
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        mainModule.playOnAwake = true;

        mainModule.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        mainModule.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        // パーティクルごとにランダムな色を設定
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(minColor, 0f);
        colorKeys[1] = new GradientColorKey(maxColor, 1f);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        colorGradient.SetKeys(colorKeys, alphaKeys);
        mainModule.startColor = new ParticleSystem.MinMaxGradient(colorGradient);

        mainModule.gravityModifier = 0f;
    }

    void ConfigureShape()
    {
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Box;

        Vector3 boxScale = CalculateScreenBounds();
        shapeModule.scale = boxScale;
    }

    Vector3 CalculateScreenBounds()
    {
        if (mainCamera == null) return new Vector3(20f, 20f, 10f);

        float distance = screenDepth;
        float height = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * mainCamera.aspect;

        return new Vector3(width * screenPadding, height * screenPadding, 10f);
    }

    void ConfigureEmission()
    {
        emissionModule.enabled = true;

        // 初期バーストで500個生成
        ParticleSystem.Burst initialBurst = new ParticleSystem.Burst(0f, particleCount);
        emissionModule.SetBursts(new ParticleSystem.Burst[] { initialBurst });

        // 無限ループモードでは追加生成しない
        emissionModule.rateOverTime = 0f;
    }

    void ConfigureVelocity()
    {
        velocityModule.enabled = false; // Velocityモジュールを無効化してエラーを回避

        // 代わりに星をわずかに動かす場合は、位置を直接操作する
        // velocityModuleのカーブモードの混在がエラーの原因なので無効化
    }

    void ConfigureColor()
    {
        // 無限ループモードでは手動で制御するため、ColorOverLifetimeは無効
        colorModule.enabled = !useInfiniteLoop;

        if (!useInfiniteLoop)
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[5];

            // きらめき効果のための色変化
            colorKeys[0] = new GradientColorKey(Color.white, 0f);
            colorKeys[1] = new GradientColorKey(Color.white * 1.2f, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.white, 1f);

            // 徐々にフェードイン・アウト
            float totalTime = maxVisibleTime + maxHiddenTime;
            float fadeInPoint = fadeInDuration / totalTime;
            float fadeOutPoint = (maxVisibleTime - fadeOutDuration) / totalTime;

            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, fadeInPoint * 0.5f);
            alphaKeys[2] = new GradientAlphaKey(1f, fadeInPoint);
            alphaKeys[3] = new GradientAlphaKey(1f, fadeOutPoint);
            alphaKeys[4] = new GradientAlphaKey(0f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            colorModule.color = new ParticleSystem.MinMaxGradient(gradient);
        }
    }

    Color GetRandomStarColor()
    {
        // 赤から濃いオレンジまでの色をランダムに生成
        float t = Random.Range(0f, 1f);

        // 赤 (1, 0, 0) から濃いオレンジ (1, 0.4, 0) までの補間
        float r = 1f;
        float g = Mathf.Lerp(0f, 0.4f, t);
        float b = 0f;

        return new Color(r, g, b, 1f);
    }

    void ConfigureSize()
    {
        sizeModule.enabled = enablePulsing;

        if (enablePulsing)
        {
            AnimationCurve sizeCurve = new AnimationCurve();

            int keyCount = 10;
            for (int i = 0; i <= keyCount; i++)
            {
                float time = i / (float)keyCount;
                float size = 1f + Mathf.Sin(time * Mathf.PI * 2f * pulseFrequency) * pulseAmplitude;
                sizeCurve.AddKey(time, size);
            }

            sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        }
    }

    void ConfigureRotation()
    {
        rotationModule.enabled = true;
        rotationModule.z = new ParticleSystem.MinMaxCurve(rotationSpeed * Mathf.Deg2Rad,
                                                          rotationSpeed * 2f * Mathf.Deg2Rad);
    }

    void ConfigureRenderer()
    {
        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

        // テクスチャが設定されている場合は、それを優先的に使用
        if (starTexture != null)
        {
            // テクスチャ情報をデバッグ出力
            Debug.Log($"[StarParticle] Using texture: {starTexture.name} ({starTexture.width}x{starTexture.height})");

            if (starMaterial == null)
            {
                // Mobile/Particles/Alpha Blendedが最も互換性が高い
                Shader shader = Shader.Find("Mobile/Particles/Alpha Blended");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                }

                starMaterial = new Material(shader);
                starMaterial.name = "StarParticleMaterial";
                Debug.Log($"[StarParticle] Created material with shader: {shader?.name ?? "null"}");
            }

            // テクスチャを複数のプロパティに設定（確実に適用）
            starMaterial.mainTexture = starTexture;
            starMaterial.SetTexture("_MainTex", starTexture);
            starMaterial.SetTexture("_BaseMap", starTexture); // URP用

            // ブレンディング設定（重要：正しいアルファブレンド）
            starMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            starMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            starMaterial.SetInt("_ZWrite", 0);
            starMaterial.renderQueue = 3000;

            // カラーとアルファを確実に有効化
            starMaterial.SetColor("_Color", Color.white);
            starMaterial.SetColor("_TintColor", Color.white);
            starMaterial.SetColor("_BaseColor", Color.white); // URP用
            starMaterial.EnableKeyword("_ALPHABLEND_ON");
            starMaterial.EnableKeyword("_ALPHATEST_ON");
        }
        else if (starMaterial == null)
        {
            // テクスチャがない場合は、デフォルトマテリアルを作成
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            starMaterial = new Material(shader);
            starMaterial.name = "StarParticleMaterial_Default";

            // Additive blendingで発光効果
            starMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            starMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            starMaterial.SetInt("_ZWrite", 0);
            starMaterial.renderQueue = 3000;
        }

        // レンダラー設定
        renderer.material = starMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // テクスチャシートアニメーション設定を完全に無効化
        var textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = false;

        // サイズ制限を解除（テクスチャが見えるように）
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0f;  // 最小サイズ制限を解除
        renderer.maxParticleSize = 1f;  // 最大サイズ制限を解除

        // メッシュをデフォルトに設定
        renderer.mesh = null; // nullにするとデフォルトのQuadが使用される
        renderer.enableGPUInstancing = false;

        // デバッグ情報
        Debug.Log($"[StarParticle] Renderer configured - Texture: {renderer.material?.mainTexture?.name ?? "null"}");
    }

    void ConfigureScreenFilling()
    {
        if (mainCamera != null)
        {
            transform.position = mainCamera.transform.position + mainCamera.transform.forward * screenDepth;
        }

        RandomizeAllParticles();
    }

    // 星の状態を管理するための配列
    private float[] starTimers;
    private int[] starStates; // 0:FadingIn, 1:Visible, 2:FadingOut, 3:Hidden
    private float[] starVisibleDurations;
    private float[] starHiddenDurations;
    private float[] starAlphas;

    void RandomizeAllParticles()
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleSystem.particleCount];
        int count = particleSystem.GetParticles(particles);

        // 状態配列の初期化
        if (useInfiniteLoop)
        {
            starTimers = new float[particleCount];
            starStates = new int[particleCount];
            starVisibleDurations = new float[particleCount];
            starHiddenDurations = new float[particleCount];
            starAlphas = new float[particleCount];
        }

        for (int i = 0; i < count; i++)
        {
            Color starColor = GetRandomStarColor();
            particles[i].startColor = starColor;
            particles[i].startSize = Random.Range(minSize, maxSize);
            particles[i].randomSeed = (uint)Random.Range(0, int.MaxValue);

            if (useInfiniteLoop)
            {
                particles[i].startLifetime = 10000f;
                particles[i].remainingLifetime = 10000f;

                // 初期状態をランダムに分散
                float randomStart = Random.Range(0f, 1f);
                if (randomStart < 0.7f) // 70%は最初から見える
                {
                    starStates[i] = 1; // Visible
                    starAlphas[i] = 1f;
                    starTimers[i] = Random.Range(0f, maxVisibleTime);
                }
                else if (randomStart < 0.85f) // 15%はフェードイン中
                {
                    starStates[i] = 0; // FadingIn
                    starAlphas[i] = Random.Range(0f, 0.5f);
                    starTimers[i] = Random.Range(0f, fadeInDuration);
                }
                else // 15%は非表示
                {
                    starStates[i] = 3; // Hidden
                    starAlphas[i] = 0f;
                    starTimers[i] = Random.Range(0f, maxHiddenTime);
                }

                starVisibleDurations[i] = Random.Range(minVisibleTime, maxVisibleTime);
                starHiddenDurations[i] = Random.Range(minHiddenTime, maxHiddenTime);
            }
            else
            {
                float randomLifetime = Random.Range(minVisibleTime, maxVisibleTime);
                particles[i].startLifetime = randomLifetime;
                particles[i].remainingLifetime = Random.Range(fadeInDuration, randomLifetime);
            }

            particles[i].angularVelocity = Random.Range(-rotationSpeed, rotationSpeed) * Mathf.Deg2Rad;
        }

        particleSystem.SetParticles(particles, count);
    }

    void Update()
    {
        if (mainCamera != null)
        {
            Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * screenDepth;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                RegenerateParticles();
            }

            // Tキーでテクスチャを強制適用
            if (Input.GetKeyDown(KeyCode.T))
            {
                ForceApplyTexture();
            }
        }

        UpdateTwinkling();
    }

    void UpdateTwinkling()
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleSystem.particleCount];
        int count = particleSystem.GetParticles(particles);

        if (useInfiniteLoop && starStates != null)
        {
            UpdateInfiniteLoopParticles(particles, count);
        }
        else if (enablePulsing)
        {
            UpdateNormalParticles(particles, count);
        }

        particleSystem.SetParticles(particles, count);
    }

    void UpdateInfiniteLoopParticles(ParticleSystem.Particle[] particles, int count)
    {
        int visibleCount = 0;

        for (int i = 0; i < count && i < starStates.Length; i++)
        {
            // タイマーを更新
            starTimers[i] -= Time.deltaTime;

            // 状態遷移
            switch (starStates[i])
            {
                case 0: // FadingIn
                    if (starTimers[i] <= 0f)
                    {
                        starStates[i] = 1; // Visible
                        starTimers[i] = starVisibleDurations[i];
                    }
                    starAlphas[i] = Mathf.MoveTowards(starAlphas[i], 1f, Time.deltaTime / fadeInDuration);
                    break;

                case 1: // Visible
                    if (starTimers[i] <= 0f)
                    {
                        starStates[i] = 2; // FadingOut
                        starTimers[i] = fadeOutDuration;
                    }
                    starAlphas[i] = 1f;
                    visibleCount++;
                    break;

                case 2: // FadingOut
                    if (starTimers[i] <= 0f)
                    {
                        starStates[i] = 3; // Hidden
                        starTimers[i] = starHiddenDurations[i];
                        // 新しいランダムな時間を設定
                        starVisibleDurations[i] = Random.Range(minVisibleTime, maxVisibleTime);
                        starHiddenDurations[i] = Random.Range(minHiddenTime, maxHiddenTime);
                    }
                    starAlphas[i] = Mathf.MoveTowards(starAlphas[i], 0f, Time.deltaTime / fadeOutDuration);
                    break;

                case 3: // Hidden
                    if (starTimers[i] <= 0f)
                    {
                        starStates[i] = 0; // FadingIn
                        starTimers[i] = fadeInDuration;
                    }
                    starAlphas[i] = 0f;
                    break;
            }

            // きらめき効果
            float twinkle = 1f;
            if (enablePulsing && starStates[i] == 1) // Visible状態のみ
            {
                twinkle = 1f + Mathf.Sin(Time.time * twinkleSpeed + i * 0.5f) * 0.2f;
            }

            // 色とアルファを適用
            Color starColor = particles[i].startColor;
            starColor.a = starAlphas[i] * twinkle;
            particles[i].startColor = starColor;
        }

        // デバッグ用
        if (visibleCount < particleCount * 0.8f)
        {
            Debug.Log($"Visible stars: {visibleCount} / {particleCount}");
        }
    }

    void UpdateNormalParticles(ParticleSystem.Particle[] particles, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float normalizedLifetime = 1f - (particles[i].remainingLifetime / particles[i].startLifetime);
            float fadeInPoint = fadeInDuration / particles[i].startLifetime;
            float fadeOutPoint = 1f - (fadeOutDuration / particles[i].startLifetime);

            // きらめき効果（フェード中でない星にのみ適用）
            if (normalizedLifetime > fadeInPoint && normalizedLifetime < fadeOutPoint)
            {
                float twinkle = Mathf.PerlinNoise(
                    particles[i].randomSeed * 0.001f,
                    Time.time * twinkleSpeed
                );

                Color currentColor = particles[i].GetCurrentColor(particleSystem);
                float baseAlpha = currentColor.a;
                currentColor.a = baseAlpha * Mathf.Lerp(0.7f, 1f, twinkle);
                particles[i].startColor = currentColor;
            }
        }
    }

    [ContextMenu("Regenerate Particles")]
    public void RegenerateParticles()
    {
        particleSystem.Clear();
        particleSystem.Play();
        RandomizeAllParticles();
    }

    [ContextMenu("Force Apply Star Texture")]
    public void ForceApplyTexture()
    {
        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

        if (starTexture == null)
        {
            Debug.LogError("[StarParticle] No star texture assigned! Please assign a texture in the Inspector.");
            return;
        }

        // 新しいマテリアルを強制的に作成
        Material newMaterial = new Material(Shader.Find("UI/Default"));
        newMaterial.mainTexture = starTexture;

        // レンダラーに直接適用
        renderer.sharedMaterial = newMaterial;

        Debug.Log($"[StarParticle] Force applied texture: {starTexture.name}");

        // パーティクルサイズを大きくして見やすくする
        var main = particleSystem.main;
        main.startSize = new ParticleSystem.MinMaxCurve(2f, 5f);

        // パーティクルを再生成
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
}