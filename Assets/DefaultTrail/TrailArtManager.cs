using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class TrailSettings
{
    [Header("基本設定")]
    public float lifetime = 3f;
    public float fadeStartTime = 2f;
    public int maxTrailPoints = 50;
    
    [Header("サイズ設定")]
    public AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
    public float baseWidth = 0.5f;
    public float widthVariation = 0.2f;
    
    [Header("動き設定")]
    public float moveSpeed = 2f;
    public float noiseStrength = 1f;
    public float noiseFrequency = 1f;
    public float rotationSpeed = 30f;
    
    [Header("マテリアル設定")]
    public Material trailMaterial;
    public bool useAdditiveBlending = true;
}

public class TrailArtManager : MonoBehaviour
{
    [Header("トレイル設定")]
    [SerializeField] private TrailSettings trailSettings;
    [SerializeField] private int maxConcurrentTrails = 20;
    
    [Header("ビジュアル設定")]
    [SerializeField] private ColorPalette[] colorPalettes;
    [SerializeField] private int currentPaletteIndex = 0;
    
    [Header("インタラクション設定")]
    [SerializeField] private bool continuousSpawn = false;
    [SerializeField] private float spawnInterval = 0.1f;
    
    [Header("パーティクル設定")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private bool enableParticles = true;
    
    private List<TrailController> activeTrails = new List<TrailController>();
    private Camera mainCamera;
    private float lastSpawnTime;
    private ObjectPool<GameObject> trailPool;
    private ObjectPool<GameObject> particlePool;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        mainCamera = Camera.main;
        
        // オブジェクトプールの初期化
        trailPool = new ObjectPool<GameObject>(CreateTrailObject, maxConcurrentTrails);
        
        if (particlePrefab != null)
        {
            particlePool = new ObjectPool<GameObject>(CreateParticleObject, 50);
        }
        
        // デフォルトマテリアルの作成
        if (trailSettings.trailMaterial == null)
        {
            trailSettings.trailMaterial = CreateDefaultMaterial();
        }
        
        // デフォルトカラーパレットの設定
        if (colorPalettes == null || colorPalettes.Length == 0)
        {
            SetupDefaultPalettes();
        }
        
        StartCoroutine(TrailUpdateCoroutine());
    }
    
    Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        
        if (trailSettings.useAdditiveBlending)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        
        mat.EnableKeyword("_ALPHABLEND_ON");
        return mat;
    }
    
    GameObject CreateTrailObject()
    {
        GameObject trail = new GameObject("Trail");
        trail.layer = LayerMask.NameToLayer("TrailArt");
        
        TrailRenderer tr = trail.AddComponent<TrailRenderer>();
        tr.material = trailSettings.trailMaterial;
        tr.time = trailSettings.lifetime;
        tr.minVertexDistance = 0.1f;
        tr.emitting = true;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        
        TrailController controller = trail.AddComponent<TrailController>();
        controller.Initialize(trailSettings, this);
        
        trail.SetActive(false);
        return trail;
    }
    
    GameObject CreateParticleObject()
    {
        GameObject particle = Instantiate(particlePrefab);
        particle.SetActive(false);
        return particle;
    }
    
    void Update()
    {
        HandleInput();
        
        // 自動生成モード
        if (continuousSpawn && Time.time - lastSpawnTime > spawnInterval)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-8f, 8f),
                Random.Range(-4f, 4f),
                0
            );
            SpawnTrailAtPosition(randomPos);
            lastSpawnTime = Time.time;
        }
    }
    
    void HandleInput()
    {
        // マウス入力
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
            
            if (Input.GetMouseButtonDown(0) || Time.time - lastSpawnTime > spawnInterval)
            {
                SpawnTrailAtPosition(worldPos);
                lastSpawnTime = Time.time;
            }
        }
        
        // タッチ入力（モバイル対応）
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
                {
                    Vector3 touchPos = touch.position;
                    touchPos.z = 10f;
                    Vector3 worldPos = mainCamera.ScreenToWorldPoint(touchPos);
                    SpawnTrailAtPosition(worldPos);
                }
            }
        }
        
        // カラーパレット切り替え
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentPaletteIndex = (currentPaletteIndex + 1) % colorPalettes.Length;
        }
    }
    
    public void SpawnTrailAtPosition(Vector3 position)
    {
        // プールから取得
        GameObject trailObj = trailPool.Get();
        if (trailObj == null) return;
        
        trailObj.transform.position = position;
        trailObj.SetActive(true);
        
        TrailController controller = trailObj.GetComponent<TrailController>();
        TrailRenderer tr = trailObj.GetComponent<TrailRenderer>();
        
        // ビジュアル設定
        if (colorPalettes != null && colorPalettes.Length > 0)
        {
            int safeIndex = Mathf.Clamp(currentPaletteIndex, 0, colorPalettes.Length - 1);
            ColorPalette palette = colorPalettes[safeIndex];
            tr.colorGradient = palette.GetRandomGradient();
        }
        else
        {
            Debug.LogWarning("colorPalettesが空です");
        }
        tr.widthCurve = trailSettings.widthCurve;
        
        float width = trailSettings.baseWidth + Random.Range(-trailSettings.widthVariation, trailSettings.widthVariation);
        tr.startWidth = width;
        tr.endWidth = width * 0.1f;
        
        controller.StartTrail();
        activeTrails.Add(controller);
        
        // パーティクル生成
        if (enableParticles && particlePrefab != null)
        {
            SpawnParticlesAt(position);
        }
        
        // 古いトレイルの制限
        if (activeTrails.Count > maxConcurrentTrails)
        {
            ReturnTrailToPool(activeTrails[0]);
            activeTrails.RemoveAt(0);
        }
    }
    
    void SpawnParticlesAt(Vector3 position)
    {
        GameObject particle = particlePool.Get();
        if (particle != null)
        {
            particle.transform.position = position;
            particle.SetActive(true);
            StartCoroutine(ReturnParticleToPool(particle, 2f));
        }
    }
    
    IEnumerator ReturnParticleToPool(GameObject particle, float delay)
    {
        yield return new WaitForSeconds(delay);
        particle.SetActive(false);
        particlePool.Return(particle);
    }
    
    public void ReturnTrailToPool(TrailController controller)
    {
        controller.gameObject.SetActive(false);
        controller.GetComponent<TrailRenderer>().Clear();
        trailPool.Return(controller.gameObject);
        activeTrails.Remove(controller);
    }
    
    IEnumerator TrailUpdateCoroutine()
    {
        while (true)
        {
            // バッチ処理で効率化
            for (int i = activeTrails.Count - 1; i >= 0; i--)
            {
                if (activeTrails[i] == null || !activeTrails[i].gameObject.activeInHierarchy)
                {
                    activeTrails.RemoveAt(i);
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void SetupDefaultPalettes()
    {
        colorPalettes = new ColorPalette[3];
        
        // パレット1: 寒色系
        colorPalettes[0] = ScriptableObject.CreateInstance<ColorPalette>();
        colorPalettes[0].name = "Cool";
        colorPalettes[0].SetupCoolColors();
        
        // パレット2: 暖色系
        colorPalettes[1] = ScriptableObject.CreateInstance<ColorPalette>();
        colorPalettes[1].name = "Warm";
        colorPalettes[1].SetupWarmColors();
        
        // パレット3: ネオン系
        colorPalettes[2] = ScriptableObject.CreateInstance<ColorPalette>();
        colorPalettes[2].name = "Neon";
        colorPalettes[2].SetupNeonColors();
    }
}
