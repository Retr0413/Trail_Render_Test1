using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Trail3DManager : MonoBehaviour
{
    [Header("2D川流れ設定")]
    [Tooltip("自動でトレイルを生成するかどうか")]
    [SerializeField] private bool enableAutoGeneration = true;
    [Tooltip("川の流れを生成するY座標のリスト")]
    [SerializeField] private float[] riverYPositions = { -3f, -1f, 1f, 3f };
    [Tooltip("Z座標の最小値")]
    [SerializeField] private float generationZMin = -5f;
    [Tooltip("Z座標の最大値")]
    [SerializeField] private float generationZMax = 5f;
    [Tooltip("川の生成間隔（秒）")]
    [SerializeField] private float riverSpawnInterval = 0.5f;
    private float nextGenerationTime = 0f;
    
    [Header("3D空間設定")]
    [Tooltip("深度の範囲")]
    [SerializeField] private float depthRange = 10f;
    [Tooltip("現在の深度")]
    [SerializeField] private float currentDepth = 0f;
    
    [Header("トレイル基本設定")]
    [Tooltip("トレイルのプレハブ")]
    [SerializeField] private GameObject trailPrefab;
    [Tooltip("最大トレイル数")]
    [SerializeField] private int maxTrails = 30;
    [Tooltip("トレイルの寿命（秒）")]
    [SerializeField] private float trailLifetime = 5f;
    [Tooltip("トレイルの長さ")]
    [SerializeField] private float trailLength = 3f;
    [Tooltip("トレイルのマテリアル")]
    [SerializeField] private Material trailMaterial;

    [Header("出現間隔設定")]
    [Tooltip("生成間隔（秒）")]
    [SerializeField] private float spawnInterval = 0.000001f;
    [Tooltip("連続生成モード")]
    [SerializeField] private bool continuousSpawn = true;

    [Header("発光設定")]
    [Tooltip("発光の強さ")]
    [SerializeField] private float emissionIntensity = 2.0f;
    [Tooltip("HDRカラーを使用")]
    [SerializeField] private bool useHDRColors = true;
    [Tooltip("光の増幅率")]
    [SerializeField] private float glowMultiplier = 2f;
    
    [Header("カラーパレット設定")]
    [Tooltip("使用可能なカラーパレット")]
    [SerializeField] private ColorPalette[] colorPalettes;
    [Tooltip("現在のパレット番号")]
    [SerializeField] private int currentPaletteIndex = 0;
    [Tooltip("深度ベースのパレットを使用")]
    [SerializeField] private bool useDepthBasedPalette = true;
    [Tooltip("深度用カラーパレット")]
    [SerializeField] private ColorPalette depthColorPalette;
    [Tooltip("つる用のカラーパレット")]
    [SerializeField] private ColorPalette vineColorPalette;
    [Tooltip("花用のカラーパレット")]
    [SerializeField] private ColorPalette flowerColorPalette;
    
    [Header("3Dビジュアル設定")]
    [Tooltip("深度フォグを有効化")]
    [SerializeField] private bool enableDepthFog = true;
    [Tooltip("フォグ開始距離")]
    [SerializeField] private float fogStartDistance = 5f;
    [Tooltip("フォグ終了距離")]
    [SerializeField] private float fogEndDistance = 15f;
    
    [Header("3D動き設定")]
    [Tooltip("3D動作を有効化")]
    [SerializeField] private bool enable3DMovement = true;
    [Tooltip("Z軸ノイズの強さ")]
    [SerializeField] private float zNoiseStrength = 2f;
    [Tooltip("螺旋の強さ")]
    [SerializeField] private float spiralStrength = 1f;
    [Tooltip("渦巻きを有効化")]
    [SerializeField] private bool enableVortex = false;
    [Tooltip("上昇速度")]
    [SerializeField] private float upwardSpeed = 2f;
    
    [Header("花の設定")]
    [Tooltip("花を生成する")]
    [SerializeField] private bool enableFlowers = true;
    [Tooltip("花が咲く間隔（秒）")]
    [SerializeField] private float flowerSpawnInterval = 1.5f;
    [Tooltip("1つのつるあたりの花の数")]
    [SerializeField] private int flowersPerVine = 3;
    [Tooltip("花のサイズ")]
    [SerializeField] private float flowerSize = 0.5f;
    [Tooltip("花の寿命（秒）")]
    [SerializeField] private float flowerLifetime = 3f;
    
    [Header("カメラ設定")]
    [Tooltip("メインカメラ")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("カメラの自動回転")]
    [SerializeField] private bool enableCameraOrbit = true;
    [Tooltip("回転速度")]
    [SerializeField] private float orbitSpeed = 10f;

    [Header("グリッド表示設定")]
    [Tooltip("グリッドを表示")]
    [SerializeField] private bool showGrid = true;
    [Tooltip("深度インジケーターを表示")]
    [SerializeField] private bool showDepthIndicator = true;
    [Tooltip("グリッドの色")]
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.1f); 

    private List<Trail3DController> activeTrails = new List<Trail3DController>();
    private float lastSpawnTime;  // ← 追加：最後の生成時刻を記録
    private Queue<GameObject> trailPool = new Queue<GameObject>();
    private Queue<GameObject> flowerPool = new Queue<GameObject>();  // 花用のオブジェクトプール
    private CameraOrbitController cameraController;
    private GameObject depthIndicator;
    private GameObject gridObject;
    private LineRenderer gridRenderer;
    
    void Start()
    {
        InitializeSystem();
        CreateDepthIndicators();
        SetupCamera();
        InitializeTrailPool();
        SetupDefaultPalettes();
    }
    
    void InitializeSystem()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (trailMaterial == null)
        {
            trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.SetFloat("_Mode", 3);
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            trailMaterial.EnableKeyword("_ALPHABLEND_ON");
            
            // ← 追加：HDRカラー対応
            if (useHDRColors)
            {
                trailMaterial.EnableKeyword("_EMISSION");
            }
        }
    }
    
    void SetupDefaultPalettes()
    {
        // デフォルトパレットは作成せず、Inspectorで設定されたものだけを使用
    }
    
    void CreateDepthIndicators()
    {
        gridObject = new GameObject("3D Grid");  // ← gridObjectに変更
        gridRenderer = gridObject.AddComponent<LineRenderer>();
        gridRenderer.material = new Material(Shader.Find("Sprites/Default"));
        gridRenderer.startColor = gridColor;  // ← 変数から取得
        gridRenderer.endColor = gridColor;    // ← 変数から取得
        gridRenderer.startWidth = 0.02f;
        gridRenderer.endWidth = 0.02f;
        
        CreateGridLines();
        gridObject.SetActive(showGrid);  // ← 初期表示状態を設定
        
        depthIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        depthIndicator.transform.localScale = Vector3.one * 0.3f;
        depthIndicator.GetComponent<Renderer>().material.color = new Color(1f, 1f, 0f, 0.5f);
        depthIndicator.GetComponent<Collider>().enabled = false;
        depthIndicator.SetActive(showDepthIndicator);  // ← 初期表示状態を設定
    }
    
    void CreateGridLines()
    {
        List<Vector3> points = new List<Vector3>();
        
        for (float z = -depthRange; z <= depthRange; z += 2f)
        {
            for (float y = -5f; y <= 5f; y += 1f)
            {
                points.Add(new Vector3(-8f, y, z));
                points.Add(new Vector3(8f, y, z));
            }
            
            for (float x = -8f; x <= 8f; x += 1f)
            {
                points.Add(new Vector3(x, -5f, z));
                points.Add(new Vector3(x, 5f, z));
            }
        }
        
        gridRenderer.positionCount = points.Count;
        gridRenderer.SetPositions(points.ToArray());
    }
    
    void SetupCamera()
    {
        cameraController = mainCamera.gameObject.AddComponent<CameraOrbitController>();
        cameraController.enabled = enableCameraOrbit;
        cameraController.orbitSpeed = orbitSpeed;
        
        mainCamera.transform.position = new Vector3(0, 2, -12);
        mainCamera.transform.rotation = Quaternion.Euler(10, 0, 0);
    }
    
    void InitializeTrailPool()
    {
        for (int i = 0; i < maxTrails; i++)
        {
            GameObject trail = CreateTrailObject();
            trail.SetActive(false);
            trailPool.Enqueue(trail);
        }
        
        // 花用のプールを初期化
        for (int i = 0; i < maxTrails * flowersPerVine; i++)
        {
            GameObject flower = CreateFlowerObject();
            flower.SetActive(false);
            flowerPool.Enqueue(flower);
        }
    }
    
    GameObject CreateTrailObject()
    {
        GameObject trail = new GameObject("Trail3D");
        
        TrailRenderer tr = trail.AddComponent<TrailRenderer>();
        tr.material = trailMaterial;
        tr.time = trailLength; 
        tr.minVertexDistance = 0.1f;
        tr.generateLightingData = true;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.alignment = LineAlignment.View;

        Trail3DController controller = trail.AddComponent<Trail3DController>();
        controller.Initialize(this, trailLifetime, 0f);
        
        return trail;
    }
    
    GameObject CreateFlowerObject()
    {
        GameObject flower = new GameObject("Flower3D");
        
        TrailRenderer tr = flower.AddComponent<TrailRenderer>();
        tr.material = trailMaterial;
        tr.time = flowerLifetime;
        tr.minVertexDistance = 0.05f;
        tr.generateLightingData = true;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.alignment = LineAlignment.View;
        
        // 花は放射状に広がるアニメーションカーブを設定
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 0.1f);
        widthCurve.AddKey(0.3f, flowerSize);
        widthCurve.AddKey(1f, 0f);
        tr.widthCurve = widthCurve;
        
        return flower;
    }
    
    void Update()
    {
        HandleInput();
        UpdateDepthIndicator();
        UpdateTrailVisuals();
        CleanupOldTrails();
        HandleAutoGeneration();
    }
    
    void HandleInput()
    {
        
        // カメラ回転（右クリック）
        if (Input.GetMouseButton(1))
        {
            float rotX = Input.GetAxis("Mouse X") * orbitSpeed;
            float rotY = Input.GetAxis("Mouse Y") * orbitSpeed;
            
            mainCamera.transform.RotateAround(Vector3.zero, Vector3.up, rotX);
            mainCamera.transform.RotateAround(Vector3.zero, mainCamera.transform.right, -rotY);
        }
        
        // パレット切り替え（スペースキー）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentPaletteIndex = (currentPaletteIndex + 1) % colorPalettes.Length;
            Debug.Log($"Switched to palette: {colorPalettes[currentPaletteIndex].paletteName}");
        }
        
        // 深度ベースパレットの切り替え（Dキー）
        if (Input.GetKeyDown(KeyCode.D))
        {
            useDepthBasedPalette = !useDepthBasedPalette;
            Debug.Log($"Depth-based palette: {(useDepthBasedPalette ? "ON" : "OFF")}");
        }
        
        // カメラリセット（Rキー）
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }

        // グリッド表示切り替え（Gキー）
        if (Input.GetKeyDown(KeyCode.G))
        {
            showGrid = !showGrid;
            if (gridObject != null)
            {
                gridObject.SetActive(showGrid);
            }
            Debug.Log($"Grid display: {(showGrid ? "ON" : "OFF")}");
        }

        // 深度インジケーター表示切り替え（Iキー）
        if (Input.GetKeyDown(KeyCode.I))
        {
            showDepthIndicator = !showDepthIndicator;
            if (depthIndicator != null)
            {
                depthIndicator.SetActive(showDepthIndicator);
            }
            Debug.Log($"Depth indicator: {(showDepthIndicator ? "ON" : "OFF")}");
        }


        // トレイルの長さ調整（+/-キー）← 新規追加
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            trailLength = Mathf.Min(trailLength + 0.5f, 10f);
            Debug.Log($"Trail length: {trailLength}");
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            trailLength = Mathf.Max(trailLength - 0.5f, 0.5f);
            Debug.Log($"Trail length: {trailLength}");
        }

        // 光の強さ調整（L/Shift+Lキー）← 新規追加
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                emissionIntensity = Mathf.Max(emissionIntensity - 0.5f, 0f);
            }
            else
            {
                emissionIntensity = Mathf.Min(emissionIntensity + 0.5f, 5f);
            }
            Debug.Log($"Emission intensity: {emissionIntensity}");
        }
    }
    
    
    
    void SpawnTrailAt3DPosition(Vector3 position)
    {
        GameObject trail = GetTrailFromPool();
        if (trail == null) return;
        
        trail.transform.position = position;
        trail.SetActive(true);
        
        Trail3DController controller = trail.GetComponent<Trail3DController>();
        TrailRenderer renderer = trail.GetComponent<TrailRenderer>();
        
        // 花を咲かせる設定をコントローラーに伝える
        if (enableFlowers)
        {
            controller.SetFlowerSettings(flowerSpawnInterval, flowersPerVine, flowerColorPalette);
        }
        
        // ColorPaletteから色を設定
        Gradient selectedGradient = new Gradient();
        
        // つる用のカラーパレットを優先使用
        if (vineColorPalette != null)
        {
            selectedGradient = vineColorPalette.GetRandomGradient();
        }
        else if (colorPalettes != null && colorPalettes.Length > 0)
        {
            if (useDepthBasedPalette && depthColorPalette != null)
            {
                // 深度に基づいてグラデーションを選択
                float depthNormalized = (position.z + depthRange) / (depthRange * 2f);
                int gradientIndex = Mathf.FloorToInt(depthNormalized * depthColorPalette.gradients.Length);
                selectedGradient = depthColorPalette.GetGradientByIndex(gradientIndex);
            }
            else if (colorPalettes[currentPaletteIndex] != null)
            {
                // 現在のパレットからランダムに選択
                ColorPalette currentPalette = colorPalettes[currentPaletteIndex];
                selectedGradient = currentPalette.GetRandomGradient();
            }
        }
        
        renderer.colorGradient = selectedGradient;
        
        // 深度によるサイズ変更（遠近感）
        float sizeFactor = 1f - (Mathf.Abs(position.z) / depthRange) * 0.5f;
        renderer.startWidth = 0.5f * sizeFactor;
        renderer.endWidth = 0.05f * sizeFactor;
        
        controller.StartTrail(position);
        activeTrails.Add(controller);

        // トレイルの長さを更新
        renderer.time = trailLength;  // ← 追加

        // HDRカラーと発光の適用（colorGradient設定部分を変更）
        if (useHDRColors)
        {
            Gradient enhancedGradient = EnhanceGradientWithHDR(selectedGradient);  // ← HDR強化
            renderer.colorGradient = enhancedGradient;
        }
        else
        {
            renderer.colorGradient = selectedGradient;
        }

        // 発光マテリアルの設定 ← 新規追加
        if (renderer.material != null)
        {
            Color baseColor = renderer.material.color;
            baseColor *= emissionIntensity * glowMultiplier;
            renderer.material.SetColor("_EmissionColor", baseColor);
        }

        // 新規メソッド追加（SpawnTrailAt3DPositionの外）
        Gradient EnhanceGradientWithHDR(Gradient original)
        {
            Gradient enhanced = new Gradient();
            GradientColorKey[] colorKeys = original.colorKeys;
            
            for (int i = 0; i < colorKeys.Length; i++)
            {
                Color color = colorKeys[i].color;
                // HDR効果で色を増幅
                color.r = Mathf.Pow(color.r, 0.7f) * glowMultiplier;
                color.g = Mathf.Pow(color.g, 0.7f) * glowMultiplier;
                color.b = Mathf.Pow(color.b, 0.7f) * glowMultiplier;
                colorKeys[i].color = color;
            }
            
            enhanced.SetKeys(colorKeys, original.alphaKeys);
            return enhanced;
        }
    }
    
    void UpdateTrailVisuals()
    {
        if (!enableDepthFog) return;
        
        foreach (var trail in activeTrails)
        {
            if (trail == null) continue;
            
            float distance = Vector3.Distance(mainCamera.transform.position, trail.transform.position);
            float fogFactor = Mathf.InverseLerp(fogStartDistance, fogEndDistance, distance);
            
            TrailRenderer renderer = trail.GetComponent<TrailRenderer>();
            Color color = renderer.material.color;
            color.a = Mathf.Lerp(1f, 0.2f, fogFactor);
            renderer.material.color = color;
        }
    }
    
   void UpdateDepthIndicator()
{
    // 深度インジケーターの更新（自動生成モード用に調整可能）
}
    
    GameObject GetTrailFromPool()
    {
        if (trailPool.Count > 0)
        {
            return trailPool.Dequeue();
        }
        return CreateTrailObject();
    }
    
    public void ReturnTrailToPool(GameObject trail)
    {
        trail.SetActive(false);
        trail.GetComponent<TrailRenderer>().Clear();
        trailPool.Enqueue(trail);
        
        Trail3DController controller = trail.GetComponent<Trail3DController>();
        activeTrails.Remove(controller);
    }
    
    public GameObject GetFlowerFromPool()
    {
        if (flowerPool.Count > 0)
        {
            return flowerPool.Dequeue();
        }
        return CreateFlowerObject();
    }
    
    public void ReturnFlowerToPool(GameObject flower)
    {
        flower.SetActive(false);
        flower.GetComponent<TrailRenderer>().Clear();
        flowerPool.Enqueue(flower);
    }
    
    public void SpawnFlowerAt(Vector3 position)
    {
        GameObject flower = GetFlowerFromPool();
        if (flower == null) return;
        
        flower.transform.position = position;
        flower.SetActive(true);
        
        TrailRenderer renderer = flower.GetComponent<TrailRenderer>();
        
        // 花用のカラーを設定
        if (flowerColorPalette != null)
        {
            renderer.colorGradient = flowerColorPalette.GetRandomGradient();
        }
        
        // 花びらのような動きをつける
        StartCoroutine(AnimateFlower(flower));
    }
    
    IEnumerator AnimateFlower(GameObject flower)
    {
        float startTime = Time.time;
        Vector3 originalPos = flower.transform.position;
        float bloomAngle = Random.Range(0f, 360f);
        
        while (Time.time - startTime < flowerLifetime)
        {
            float elapsed = Time.time - startTime;
            float bloomProgress = elapsed / flowerLifetime;
            
            // 花びらが放射状に広がる
            float radius = bloomProgress * flowerSize;
            flower.transform.position = originalPos + new Vector3(
                Mathf.Sin(bloomAngle + elapsed * 2f) * radius,
                Mathf.Cos(bloomAngle + elapsed * 2f) * radius * 0.5f,
                Mathf.Sin(elapsed * 3f) * radius * 0.3f
            );
            
            // 回転アニメーション
            flower.transform.Rotate(Vector3.up * 180f * Time.deltaTime);
            
            yield return null;
        }
        
        ReturnFlowerToPool(flower);
    }
    
    void CleanupOldTrails()
    {
        for (int i = activeTrails.Count - 1; i >= 0; i--)
        {
            if (activeTrails[i] == null || !activeTrails[i].gameObject.activeInHierarchy)
            {
                activeTrails.RemoveAt(i);
            }
        }
    }
    
    void ResetCamera()
    {
        mainCamera.transform.position = new Vector3(0, 2, -12);
        mainCamera.transform.rotation = Quaternion.Euler(10, 0, 0);
        currentDepth = 0;
    }
    
    void HandleAutoGeneration()
    {
        if (!enableAutoGeneration) return;

        if (Time.time >= nextGenerationTime)
        {
            float selectedY = riverYPositions[Random.Range(0, riverYPositions.Length)];
            float randomZ = Random.Range(generationZMin, generationZMax);
            Vector3 generationPosition = new Vector3(-10f, selectedY, randomZ);

            SpawnTrailAt2DPosition(generationPosition, selectedY);

            nextGenerationTime = Time.time + riverSpawnInterval;
        }
    }

    void SpawnTrailAt2DPosition(Vector3 position, float yPos)
    {
        GameObject trail = GetTrailFromPool();
        if (trail == null) return;

        trail.transform.position = position;
        trail.SetActive(true);

        Trail3DController controller = trail.GetComponent<Trail3DController>();
        TrailRenderer renderer = trail.GetComponent<TrailRenderer>();

        controller.Initialize(this, trailLifetime, yPos);

        if (enableFlowers)
        {
            controller.SetFlowerSettings(flowerSpawnInterval, flowersPerVine, flowerColorPalette);
        }

        Gradient selectedGradient = new Gradient();

        if (vineColorPalette != null)
        {
            selectedGradient = vineColorPalette.GetRandomGradient();
        }
        else if (colorPalettes != null && colorPalettes.Length > 0 && colorPalettes[currentPaletteIndex] != null)
        {
            selectedGradient = colorPalettes[currentPaletteIndex].GetRandomGradient();
        }

        renderer.colorGradient = selectedGradient;
        renderer.startWidth = 0.8f;
        renderer.endWidth = 0.3f;
        renderer.time = trailLength;

        controller.StartTrail(position);
        activeTrails.Add(controller);
    }
    
    // パレットを外部から設定するメソッド
    public void SetColorPalettes(ColorPalette[] palettes)
    {
        colorPalettes = palettes;
    }
    
    public void SetDepthColorPalette(ColorPalette palette)
    {
        depthColorPalette = palette;
        depthColorPalette.useForDepth = true;
    }
}