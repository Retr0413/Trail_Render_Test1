using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class RiverSettings
{
    [Header("川の基本設定")]
    [Tooltip("川の長さ（X軸の移動距離）")]
    public float riverLength = 40f;

    [Tooltip("川の開始X座標")]
    public float startX = -20f;

    [Tooltip("川の終了X座標")]
    public float endX = 20f;

    [Tooltip("TrailRendererの継続時間")]
    public float trailTime = 15f;

    [Header("Y座標設定")]
    [Tooltip("Y座標の最小値")]
    public float minY = -5f;

    [Tooltip("Y座標の最大値")]
    public float maxY = 5f;

    [Header("流れ設定")]
    [Tooltip("川の流れる速度")]
    public float flowSpeed = 3f;

    [Tooltip("速度の変動幅（0-1）")]
    public float speedVariation = 0.4f;

    [Header("波動設定")]
    [Tooltip("波の振幅（Z軸方向の揺れ幅）")]
    public float waveAmplitude = 2f;

    [Tooltip("波の周波数")]
    public float waveFrequency = 1.5f;

    [Header("不規則性設定")]
    [Tooltip("不規則な動きの強さ")]
    public float irregularity = 1.5f;

    [Tooltip("ノイズの速度")]
    public float noiseSpeed = 0.3f;

    [Header("サイズ設定")]
    [Tooltip("川の開始幅")]
    public float startWidth = 0.03f;

    [Tooltip("川の終了幅")]
    public float endWidth = 0.01f;

    [Header("マテリアル設定")]
    public Material riverMaterial;
    public bool useAdditiveBlending = true;
}

public class TrailArtManager : MonoBehaviour
{
    [Header("川の設定")]
    [SerializeField] private RiverSettings riverSettings;

    [Header("連続生成設定")]
    [Tooltip("川を自動生成するか")]
    [SerializeField] private bool autoGenerateRivers = true;

    [Tooltip("基本生成間隔（秒）")]
    [SerializeField] private float baseSpawnInterval = 0.01f;

    [Tooltip("同時に存在できる最大の川の数")]
    [SerializeField] private int maxConcurrentRivers = 200;

    [Tooltip("同時に流れる川の本数")]
    [SerializeField] private int simultaneousStreams = 15;

    [Header("連続性設定")]
    [Tooltip("オーバーラップ時間（秒）")]
    [SerializeField] private float overlapTime = 2f;

    [Tooltip("ストリーム間のオフセット")]
    [SerializeField] private float streamOffset = 0.3f;

    [Header("マウス撹乱設定")]
    [Tooltip("マウス撹乱を有効にする")]
    [SerializeField] private bool enableMouseDisturbance = true;

    [Tooltip("撹乱半径")]
    [SerializeField] private float disturbanceRadius = 3f;

    [Tooltip("撹乱の強さ")]
    [SerializeField] private float disturbanceStrength = 5f;

    [Tooltip("振動の周波数")]
    [SerializeField] private float vibrationFrequency = 15f;

    [Header("ビジュアル設定")]
    [SerializeField] private ColorPalette[] colorPalettes;
    [SerializeField] private int currentPaletteIndex = 0;
    [SerializeField] private bool randomizeColors = true;

    private List<TrailController> activeTrails = new List<TrailController>();
    private Camera mainCamera;
    private ObjectPool<GameObject> trailPool;

    // 各ストリームの最後の生成時間とY座標
    private List<float> lastSpawnTimes = new List<float>();
    private List<float> streamYPositions = new List<float>();

    // マウス位置（ワールド座標）
    private Vector3 mouseWorldPosition;
    private bool isMousePressed = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        mainCamera = Camera.main;

        trailPool = new ObjectPool<GameObject>(CreateRiverObject, maxConcurrentRivers);

        if (riverSettings.riverMaterial == null)
        {
            riverSettings.riverMaterial = CreateDefaultMaterial();
        }

        if (colorPalettes == null || colorPalettes.Length == 0)
        {
            SetupDefaultPalettes();
        }

        // 各ストリームを初期化（完全ランダムY座標）
        for (int i = 0; i < simultaneousStreams; i++)
        {
            lastSpawnTimes.Add(-100f);
            streamYPositions.Add(Random.Range(riverSettings.minY, riverSettings.maxY));
        }

        StartCoroutine(RiverUpdateCoroutine());
        StartCoroutine(ContinuousRiverSpawner());
    }

    Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        Material mat = new Material(shader);

        if (riverSettings.useAdditiveBlending)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }

        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
        return mat;
    }

    GameObject CreateRiverObject()
    {
        GameObject river = new GameObject("RiverTrail");

        TrailRenderer tr = river.AddComponent<TrailRenderer>();
        tr.material = riverSettings.riverMaterial;
        tr.time = riverSettings.trailTime;
        tr.minVertexDistance = 0.01f;
        tr.emitting = true;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.alignment = LineAlignment.TransformZ;
        tr.textureMode = LineTextureMode.Stretch;

        // より滑らかなカーブ
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 1f);
        widthCurve.AddKey(0.1f, 0.8f);
        widthCurve.AddKey(0.5f, 0.6f);
        widthCurve.AddKey(1f, 0.2f);
        tr.widthCurve = widthCurve;

        tr.startWidth = riverSettings.startWidth;
        tr.endWidth = riverSettings.endWidth;

        TrailController controller = river.AddComponent<TrailController>();
        controller.Initialize(riverSettings, this);

        river.SetActive(false);
        return river;
    }

    void Update()
    {
        // マウス位置の更新
        if (Input.GetMouseButton(0))
        {
            isMousePressed = true;
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f;
            mouseWorldPosition = mainCamera.ScreenToWorldPoint(mousePos);
            mouseWorldPosition.z = 0;
        }
        else
        {
            isMousePressed = false;
        }

        // 各アクティブな川にマウス位置を伝える
        if (isMousePressed && enableMouseDisturbance)
        {
            foreach (var trail in activeTrails)
            {
                if (trail != null)
                {
                    trail.SetDisturbance(mouseWorldPosition, disturbanceRadius, disturbanceStrength, vibrationFrequency);
                }
            }
        }
        else if (!isMousePressed)
        {
            foreach (var trail in activeTrails)
            {
                if (trail != null)
                {
                    trail.ClearDisturbance();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && colorPalettes.Length > 0)
        {
            currentPaletteIndex = (currentPaletteIndex + 1) % colorPalettes.Length;
            Debug.Log($"カラーパレット切り替え: {currentPaletteIndex}");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            randomizeColors = !randomizeColors;
            Debug.Log($"ランダムカラー: {randomizeColors}");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            autoGenerateRivers = !autoGenerateRivers;
            Debug.Log($"自動生成: {autoGenerateRivers}");
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            enableMouseDisturbance = !enableMouseDisturbance;
            Debug.Log($"マウス撹乱: {enableMouseDisturbance}");
        }
    }

    IEnumerator ContinuousRiverSpawner()
    {
        while (true)
        {
            if (autoGenerateRivers)
            {
                SpawnContinuousRivers();
            }
            yield return new WaitForSeconds(baseSpawnInterval);
        }
    }

    void SpawnContinuousRivers()
    {
        if (activeTrails.Count >= maxConcurrentRivers) return;

        for (int i = 0; i < simultaneousStreams; i++)
        {
            float timeSinceLastSpawn = Time.time - lastSpawnTimes[i];

            // 前のトレイルが終わる前に新しいのを生成（オーバーラップ）
            float spawnTiming = (riverSettings.riverLength / riverSettings.flowSpeed) - overlapTime;
            spawnTiming += Random.Range(-0.5f, 0.5f); // タイミングのランダム化

            if (timeSinceLastSpawn >= spawnTiming)
            {
                // 完全にランダムなY座標を生成
                float randomY = Random.Range(riverSettings.minY, riverSettings.maxY);

                // Z座標もランダム
                float randomZ = Random.Range(-2f, 2f);

                // X座標も少しランダム化
                float startXOffset = Random.Range(-1f, 1f);
                Vector3 startPosition = new Vector3(
                    riverSettings.startX + startXOffset,
                    randomY,
                    randomZ
                );

                SpawnRiverAtPosition(startPosition, randomY);
                lastSpawnTimes[i] = Time.time;

                // 次のストリームのY座標を新しくランダムに設定
                streamYPositions[i] = Random.Range(riverSettings.minY, riverSettings.maxY);
            }
        }
    }

    public void SpawnRiverAtPosition(Vector3 position, float yPos)
    {
        GameObject riverObj = trailPool.Get();
        if (riverObj == null) return;

        riverObj.SetActive(true);

        TrailController controller = riverObj.GetComponent<TrailController>();
        TrailRenderer tr = riverObj.GetComponent<TrailRenderer>();

        // カラー設定
        if (colorPalettes != null && colorPalettes.Length > 0)
        {
            Color baseColor;
            if (randomizeColors)
            {
                int randomPalette = Random.Range(0, colorPalettes.Length);
                ColorPalette palette = colorPalettes[randomPalette];
                if (palette != null)
                {
                    Gradient grad = palette.GetRandomGradient();
                    tr.colorGradient = grad;
                    baseColor = grad.colorKeys[0].color;
                }
                else
                {
                    baseColor = Color.cyan;
                }
            }
            else
            {
                int safeIndex = Mathf.Clamp(currentPaletteIndex, 0, colorPalettes.Length - 1);
                ColorPalette palette = colorPalettes[safeIndex];
                if (palette != null)
                {
                    Gradient grad = palette.GetRandomGradient();
                    tr.colorGradient = grad;
                    baseColor = grad.colorKeys[0].color;
                }
                else
                {
                    baseColor = Color.cyan;
                }
            }

            // 透明度調整
            Color matColor = baseColor;
            matColor.a = Random.Range(0.3f, 0.7f);
            tr.material.color = matColor;
        }

        // サイズのランダム化
        float sizeMultiplier = Random.Range(0.8f, 1.2f);
        tr.startWidth = riverSettings.startWidth * sizeMultiplier;
        tr.endWidth = riverSettings.endWidth * sizeMultiplier;

        controller.StartRiverFlow(position, yPos);
        activeTrails.Add(controller);
    }

    public void ReturnTrailToPool(TrailController controller)
    {
        controller.gameObject.SetActive(false);
        controller.GetComponent<TrailRenderer>().Clear();
        trailPool.Return(controller.gameObject);
        activeTrails.Remove(controller);
    }

    IEnumerator RiverUpdateCoroutine()
    {
        while (true)
        {
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
        colorPalettes = new ColorPalette[4];

        colorPalettes[0] = ScriptableObject.CreateInstance<ColorPalette>();
        colorPalettes[0].name = "Ocean";
        colorPalettes[0].SetupCoolColors();

        colorPalettes[1] = ScriptableObject.CreateInstance<ColorPalette>();
        colorPalettes[1].name = "River";
        colorPalettes[1].SetupWarmColors();

        colorPalettes[2] = ScriptableObject.CreateInstance<ColorPalette>();
        colorPalettes[2].name = "Aurora";
        colorPalettes[2].SetupNeonColors();

        colorPalettes[3] = ScriptableObject.CreateInstance<ColorPalette>();
        colorPalettes[3].name = "Crystal";
        colorPalettes[3].SetupCoolColors();
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 14;

        float yOffset = 10;
        GUI.Label(new Rect(10, yOffset, 400, 20), $"自動生成: {autoGenerateRivers} (Sキーで切替)", style);
        yOffset += 20;
        GUI.Label(new Rect(10, yOffset, 400, 20), $"アクティブな川: {activeTrails.Count}/{maxConcurrentRivers}", style);
        yOffset += 20;
        GUI.Label(new Rect(10, yOffset, 400, 20), $"同時ストリーム数: {simultaneousStreams}", style);
        yOffset += 20;
        GUI.Label(new Rect(10, yOffset, 400, 20), "スペースキー: カラーパレット切替", style);
        yOffset += 20;
        GUI.Label(new Rect(10, yOffset, 400, 20), "Rキー: ランダムカラー切替", style);
        yOffset += 20;
        GUI.Label(new Rect(10, yOffset, 400, 20), $"Dキー: マウス撹乱 {(enableMouseDisturbance ? "ON" : "OFF")}", style);
        yOffset += 20;
        GUI.Label(new Rect(10, yOffset, 400, 20), "マウスクリック: 川の流れを乱す", style);
    }
}