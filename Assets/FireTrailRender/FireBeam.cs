using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class FireBeam : MonoBehaviour
{
    private TrailRenderer trailRenderer;
    private SolarFlareSettings settings;
    private SolarFlareSystem manager;
    
    private float startTime;
    private Vector3 startPosition;
    private float flowPhase;
    private float noiseOffset;
    private bool isActive;
    
    private int beamIndex;
    private float heightOffset;
    private float speedMultiplier;
    
    public void Initialize(SolarFlareSettings settings, SolarFlareSystem manager, int index)
    {
        this.settings = settings;
        this.manager = manager;
        this.beamIndex = index;
        
        trailRenderer = GetComponent<TrailRenderer>();
        SetupTrailRenderer();
        
        noiseOffset = Random.Range(0f, 100f);
        flowPhase = Random.Range(0f, Mathf.PI * 2f);
        speedMultiplier = Random.Range(0.8f, 1.2f);
        heightOffset = Random.Range(0f, 5f);
    }
    
    void SetupTrailRenderer()
    {
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }
        
        trailRenderer.time = settings.trailTime;
        trailRenderer.startWidth = settings.startWidth;
        trailRenderer.endWidth = settings.endWidth;
        trailRenderer.minVertexDistance = 0.005f;
        trailRenderer.autodestruct = false;
        trailRenderer.numCornerVertices = 16;
        trailRenderer.numCapVertices = 16;
        trailRenderer.textureMode = LineTextureMode.Stretch;
        trailRenderer.alignment = LineAlignment.View;
        
        // 幅のカーブ設定
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 1f);
        widthCurve.AddKey(0.3f, 0.8f);
        widthCurve.AddKey(0.7f, 0.4f);
        widthCurve.AddKey(1f, 0f);
        trailRenderer.widthCurve = widthCurve;
        
        // カラーパレットから色を設定
        if (settings.colorPalette != null && settings.colorPalette.gradients != null && settings.colorPalette.gradients.Length > 0)
        {
            int gradientIndex = beamIndex % settings.colorPalette.gradients.Length;
            trailRenderer.colorGradient = settings.colorPalette.gradients[gradientIndex];
        }
        
        // マテリアル設定
        if (settings.useAdditiveBlending)
        {
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.SetFloat("_Mode", 3);
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            trailMaterial.SetInt("_ZWrite", 0);
            trailMaterial.DisableKeyword("_ALPHATEST_ON");
            trailMaterial.EnableKeyword("_ALPHABLEND_ON");
            trailMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            trailMaterial.renderQueue = 3000;
            trailRenderer.material = trailMaterial;
        }
    }
    
    public void StartFlareFlow(Vector3 position)
    {
        startTime = Time.time;
        startPosition = position;
        startPosition.y += heightOffset;
        transform.position = startPosition;
        
        isActive = true;
        trailRenderer.Clear();
        trailRenderer.emitting = true;
    }
    
    void Update()
    {
        if (!isActive) return;
        
        float elapsed = Time.time - startTime;
        
        // 上に到達したらリセット
        if (transform.position.y > settings.maxHeight)
        {
            // 連続的に再出現
            RestartFlow();
            return;
        }
        
        UpdateFlareMovement();
        UpdateFade(elapsed);
    }
    
    void UpdateFlareMovement()
    {
        float time = Time.time;
        
        // 波動の更新
        flowPhase += settings.waveFrequency * Time.deltaTime;
        
        // 複雑な波動パターン
        float primaryWave = Mathf.Sin(flowPhase) * settings.waveAmplitude;
        float secondaryWave = Mathf.Sin(flowPhase * 2.3f) * settings.waveAmplitude * 0.3f;
        float tertiaryWave = Mathf.Cos(flowPhase * 3.7f) * settings.waveAmplitude * 0.2f;
        
        // Perlinノイズによる不規則性
        float noise1 = Mathf.PerlinNoise(time * settings.noiseSpeed + noiseOffset, 0f) - 0.5f;
        float noise2 = Mathf.PerlinNoise(time * settings.noiseSpeed * 0.7f + noiseOffset * 2f, 100f) - 0.5f;
        float noise3 = Mathf.PerlinNoise(time * settings.noiseSpeed * 1.3f + noiseOffset * 3f, 200f) - 0.5f;
        
        float combinedNoise = (noise1 + noise2 * 0.5f + noise3 * 0.3f) * settings.irregularity;
        
        // X軸方向（横の動き）
        float xOffset = primaryWave + secondaryWave + tertiaryWave + combinedNoise;
        
        // 速度の変動
        float speedVariation = 1f + (noise1 * settings.speedVariation);
        float currentSpeed = settings.riseSpeed * speedVariation * speedMultiplier;
        
        Vector3 newPosition = transform.position;
        newPosition.y += currentSpeed * Time.deltaTime;  // 上昇
        newPosition.x = startPosition.x + xOffset;       // 横揺れ
        
        // Z軸の微細な動き（奥行き感）
        float zWave = Mathf.Sin(time * 4f + flowPhase) * 0.1f;
        newPosition.z = startPosition.z + zWave;
        
        transform.position = newPosition;
    }
    
    void UpdateFade(float elapsed)
    {
        // 高さに応じたフェード
        float heightProgress = (transform.position.y - settings.bottomY) / (settings.maxHeight - settings.bottomY);
        
        if (heightProgress > 0.7f)
        {
            float fadeProgress = (heightProgress - 0.7f) / 0.3f;
            Color color = trailRenderer.material.color;
            color.a = Mathf.Lerp(1f, 0f, fadeProgress);
            trailRenderer.material.color = color;
        }
    }
    
    void RestartFlow()
    {
        // ランダムな遅延を加えて再開始
        heightOffset = Random.Range(-2f, 2f);
        speedMultiplier = Random.Range(0.8f, 1.2f);
        flowPhase = Random.Range(0f, Mathf.PI * 2f);
        
        Vector3 newStart = startPosition;
        newStart.y = settings.bottomY + heightOffset;
        transform.position = newStart;
        
        startTime = Time.time;
        trailRenderer.Clear();
    }
    
    public void StopFlare()
    {
        isActive = false;
        trailRenderer.emitting = false;
    }
    
    void OnDestroy()
    {
        if (trailRenderer != null && trailRenderer.material != null)
        {
            Destroy(trailRenderer.material);
        }
    }
}