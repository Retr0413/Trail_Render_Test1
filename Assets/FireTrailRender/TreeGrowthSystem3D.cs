using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeSettings3D
{
    [Header("木の基本設定")]
    [Tooltip("幹の高さ")]
    public float trunkHeight = 10f;

    [Tooltip("成長速度")]
    public float growthSpeed = 4f;

    [Tooltip("幹の太さ")]
    public float trunkWidth = 0.5f;

    [Header("主枝の設定")]
    [Tooltip("主枝の本数")]
    public int mainBranchCount = 20;

    [Tooltip("主枝の開始高さ（幹の何％から）")]
    public float mainBranchStartHeight = 0.3f;

    [Tooltip("主枝の分布範囲（幹の何％の範囲）")]
    public float mainBranchSpreadHeight = 0.6f;

    [Header("分岐設定")]
    [Tooltip("最大分岐世代数")]
    public int maxBranchGenerations = 5;

    [Tooltip("各世代での分岐数")]
    public int[] branchesPerGeneration = new int[] { 3, 3, 2, 2, 1 };

    [Tooltip("分岐確率（世代ごと）")]
    public float[] branchProbability = new float[] { 1f, 0.9f, 0.8f, 0.6f, 0.4f };

    [Header("枝のパラメータ減衰")]
    [Tooltip("枝の長さ減衰率")]
    public float branchLengthDecay = 0.65f;

    [Tooltip("枝の太さ減衰率")]
    public float branchWidthDecay = 0.55f;

    [Tooltip("初期枝の長さ")]
    public float initialBranchLength = 4f;

    [Header("角度設定")]
    [Tooltip("分岐角度の基本値")]
    public float baseAngle = 35f;

    [Tooltip("角度のランダム幅")]
    public float angleVariation = 20f;

    [Tooltip("世代ごとの角度増加")]
    public float angleIncreasePerGeneration = 5f;

    [Header("3D特有の設定")]
    [Tooltip("枝の3D広がり角度")]
    public float branchSpreadAngle = 360f;

    [Tooltip("枝の仰角範囲（度）")]
    public Vector2 elevationRange = new Vector2(10f, 60f);

    [Tooltip("らせん状成長の強度")]
    public float spiralStrength = 0.5f;

    [Tooltip("深度に応じた枝の色変化")]
    public bool useDepthBasedColor = true;

    [Header("分岐位置設定")]
    [Tooltip("枝上の分岐開始位置（0-1）")]
    public float branchStartPosition = 0.3f;

    [Tooltip("分岐位置のランダム性")]
    public float branchPositionVariation = 0.2f;

    [Header("TrailRenderer設定")]
    [Tooltip("トレイルの継続時間")]
    public float trailTime = 30f;

    [Tooltip("頂点間の最小距離")]
    public float minVertexDistance = 0.005f;

    [Header("ビジュアル設定")]
    public ColorPalette colorPalette;
    public bool useAdditiveBlending = false;

    [Header("アニメーション設定")]
    [Tooltip("色の変化速度")]
    public float colorChangeSpeed = 3f;

    [Tooltip("揺れの強さ")]
    public float swayStrength = 0.15f;

    [Tooltip("揺れの速度")]
    public float swaySpeed = 1f;

    [Header("パフォーマンス設定")]
    [Tooltip("同時成長枝数の制限")]
    public int maxConcurrentGrowth = 10;

    [Tooltip("成長遅延の最小時間")]
    public float minGrowthDelay = 0.02f;

    [Header("カメラ設定")]
    [Tooltip("カメラとの距離に応じてLODを調整")]
    public bool useLOD = true;

    [Tooltip("LOD距離閾値")]
    public float[] lodDistances = new float[] { 10f, 25f, 50f };
}

public class TreeGrowthSystem3D : MonoBehaviour
{
    [SerializeField] private TreeSettings3D settings;

    private List<TreeBranch3D> allBranches = new List<TreeBranch3D>();
    private Queue<BranchGrowthData3D> growthQueue = new Queue<BranchGrowthData3D>();
    private int currentlyGrowing = 0;
    private bool isGrowing = false;
    private Camera mainCamera;

    // 枝成長データ
    private class BranchGrowthData3D
    {
        public TreeBranch3D branch;
        public TreeBranch3D parent;
        public int generation;
        public float delay;

        public BranchGrowthData3D(TreeBranch3D b, TreeBranch3D p, int g, float d)
        {
            branch = b;
            parent = p;
            generation = g;
            delay = d;
        }
    }

    void Start()
    {
        if (settings == null)
        {
            settings = new TreeSettings3D();
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        // 配列の初期化
        if (settings.branchesPerGeneration == null || settings.branchesPerGeneration.Length < settings.maxBranchGenerations)
        {
            settings.branchesPerGeneration = new int[] { 3, 3, 2, 2, 1 };
        }
        if (settings.branchProbability == null || settings.branchProbability.Length < settings.maxBranchGenerations)
        {
            settings.branchProbability = new float[] { 1f, 0.9f, 0.8f, 0.6f, 0.4f };
        }

        StartGrowth();
    }

    public void StartGrowth()
    {
        if (isGrowing) return;

        Clear();
        isGrowing = true;
        currentlyGrowing = 0;

        StartCoroutine(GrowTree());
        StartCoroutine(ProcessGrowthQueue());
    }

    IEnumerator GrowTree()
    {
        // 幹の生成
        GameObject trunkObj = new GameObject("Trunk3D");
        trunkObj.transform.parent = transform;
        TreeBranch3D trunk = trunkObj.AddComponent<TreeBranch3D>();

        trunk.Initialize(
            settings,
            Vector3.zero,
            Vector3.up,
            settings.trunkHeight,
            settings.trunkWidth,
            0
        );

        allBranches.Add(trunk);
        trunk.StartGrowth();

        // 幹の成長を待つ
        yield return new WaitForSeconds(settings.trunkHeight / settings.growthSpeed * 0.7f);

        // 主枝の生成
        CreateMainBranches(trunk);

        // すべての成長が完了するまで待つ
        while (growthQueue.Count > 0 || currentlyGrowing > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        isGrowing = false;
    }

    void CreateMainBranches(TreeBranch3D trunk)
    {
        float startHeight = settings.trunkHeight * settings.mainBranchStartHeight;
        float spreadHeight = settings.trunkHeight * settings.mainBranchSpreadHeight;

        for (int i = 0; i < settings.mainBranchCount; i++)
        {
            // 主枝の配置を計算
            float heightRatio = (float)i / settings.mainBranchCount;
            float height = startHeight + (spreadHeight * heightRatio);
            height += Random.Range(-0.1f, 0.1f) * settings.trunkHeight * 0.1f;

            // 3Dらせん状に配置
            float baseAngle = (settings.branchSpreadAngle / settings.mainBranchCount * i);
            float spiralAngle = baseAngle + (heightRatio * settings.spiralStrength * 360f);
            spiralAngle += Random.Range(-20f, 20f);

            // 仰角の計算（高さに応じて変化）
            float elevation = Mathf.Lerp(settings.elevationRange.x, settings.elevationRange.y,
                                        heightRatio + Random.Range(-0.2f, 0.2f));
            elevation = Mathf.Clamp(elevation, 0f, 80f);

            float branchPointOnTrunk = height / settings.trunkHeight;
            branchPointOnTrunk = Mathf.Clamp01(branchPointOnTrunk);

            Vector3 startPos = trunk.GetBranchPoint(branchPointOnTrunk);

            // 3D方向ベクトルの計算
            Vector3 direction = CalculateDirection3D(spiralAngle, elevation);

            // 主枝を作成
            GameObject branchObj = new GameObject($"MainBranch3D_{i}");
            branchObj.transform.parent = transform;
            TreeBranch3D mainBranch = branchObj.AddComponent<TreeBranch3D>();

            float length = settings.initialBranchLength * Random.Range(0.8f, 1.2f);
            float width = settings.trunkWidth * 0.4f * Random.Range(0.9f, 1.1f);

            mainBranch.Initialize(
                settings,
                startPos,
                direction,
                length,
                width,
                1
            );

            allBranches.Add(mainBranch);

            // 成長キューに追加
            float delay = Random.Range(0f, 0.3f);
            growthQueue.Enqueue(new BranchGrowthData3D(mainBranch, trunk, 1, delay));

            // この主枝から分岐を生成
            ScheduleBranchSplits(mainBranch, 2);
        }
    }

    Vector3 CalculateDirection3D(float azimuth, float elevation)
    {
        // 球面座標から3D方向ベクトルへ変換
        float azimuthRad = azimuth * Mathf.Deg2Rad;
        float elevationRad = elevation * Mathf.Deg2Rad;

        return new Vector3(
            Mathf.Sin(azimuthRad) * Mathf.Cos(elevationRad),
            Mathf.Sin(elevationRad),
            Mathf.Cos(azimuthRad) * Mathf.Cos(elevationRad)
        ).normalized;
    }

    void ScheduleBranchSplits(TreeBranch3D parentBranch, int nextGeneration)
    {
        if (nextGeneration > settings.maxBranchGenerations) return;

        // 分岐確率チェック
        float probability = nextGeneration - 1 < settings.branchProbability.Length ?
            settings.branchProbability[nextGeneration - 1] : 0.3f;

        if (Random.Range(0f, 1f) > probability) return;

        // この世代での分岐数
        int branchCount = nextGeneration - 1 < settings.branchesPerGeneration.Length ?
            settings.branchesPerGeneration[nextGeneration - 1] : 1;

        // ランダムで分岐数を調整
        branchCount = Random.Range(Mathf.Max(1, branchCount - 1), branchCount + 2);

        // 分岐点を複数設定
        int splitPoints = Mathf.Max(1, 3 - nextGeneration / 2);

        for (int sp = 0; sp < splitPoints; sp++)
        {
            float basePosition = settings.branchStartPosition + (sp * 0.3f);
            float position = basePosition + Random.Range(-settings.branchPositionVariation, settings.branchPositionVariation);
            position = Mathf.Clamp(position, 0.2f, 0.95f);

            for (int i = 0; i < branchCount; i++)
            {
                CreateSubBranch(parentBranch, nextGeneration, position, i, branchCount);
            }
        }
    }

    void CreateSubBranch(TreeBranch3D parent, int generation, float positionOnParent, int index, int totalCount)
    {
        Vector3 startPos = parent.GetBranchPoint(positionOnParent);

        // 親の方向を取得
        Vector3 parentDir = parent.GetDirection();

        // 分岐角度の計算（3D空間で）
        float baseAngle = settings.baseAngle + (generation - 1) * settings.angleIncreasePerGeneration;
        float spread = baseAngle + Random.Range(-settings.angleVariation, settings.angleVariation);

        // 3D空間での分岐配置
        float azimuthStep = 360f / totalCount;
        float azimuth = azimuthStep * index + Random.Range(-30f, 30f);

        // 親の方向に対する相対的な座標系を構築
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, parentDir).normalized;
        if (right.magnitude < 0.01f)
        {
            right = Vector3.Cross(Vector3.forward, parentDir).normalized;
        }
        Vector3 forward = Vector3.Cross(parentDir, right).normalized;

        // 球面座標で分岐方向を計算
        float azimuthRad = azimuth * Mathf.Deg2Rad;
        float spreadRad = spread * Mathf.Deg2Rad;

        Vector3 localDirection = new Vector3(
            Mathf.Sin(azimuthRad) * Mathf.Sin(spreadRad),
            Mathf.Cos(spreadRad),
            Mathf.Cos(azimuthRad) * Mathf.Sin(spreadRad)
        );

        // ローカル座標系からワールド座標系へ変換
        Vector3 direction = (right * localDirection.x + parentDir * localDirection.y + forward * localDirection.z).normalized;

        // パラメータの計算
        float lengthDecay = Mathf.Pow(settings.branchLengthDecay, generation - 1);
        float widthDecay = Mathf.Pow(settings.branchWidthDecay, generation - 1);

        float length = settings.initialBranchLength * lengthDecay * Random.Range(0.7f, 1.3f);
        float width = settings.trunkWidth * 0.4f * widthDecay * Random.Range(0.8f, 1.2f);

        // 枝オブジェクトを作成
        GameObject branchObj = new GameObject($"Branch3D_G{generation}_{allBranches.Count}");
        branchObj.transform.parent = transform;
        TreeBranch3D newBranch = branchObj.AddComponent<TreeBranch3D>();

        newBranch.Initialize(
            settings,
            startPos,
            direction,
            length,
            width,
            generation
        );

        allBranches.Add(newBranch);

        // 成長キューに追加
        float delay = Random.Range(settings.minGrowthDelay, 0.2f);
        growthQueue.Enqueue(new BranchGrowthData3D(newBranch, parent, generation, delay));

        // 次の世代の分岐をスケジュール
        if (generation < settings.maxBranchGenerations)
        {
            ScheduleBranchSplits(newBranch, generation + 1);
        }
    }

    IEnumerator ProcessGrowthQueue()
    {
        while (isGrowing)
        {
            if (growthQueue.Count > 0 && currentlyGrowing < settings.maxConcurrentGrowth)
            {
                BranchGrowthData3D data = growthQueue.Dequeue();
                StartCoroutine(GrowBranchWithDelay(data));
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    IEnumerator GrowBranchWithDelay(BranchGrowthData3D data)
    {
        currentlyGrowing++;

        yield return new WaitForSeconds(data.delay);

        data.branch.StartGrowth();

        // 成長完了まで待つ
        float growthTime = data.branch.GetLength() / settings.growthSpeed;
        yield return new WaitForSeconds(growthTime);

        currentlyGrowing--;
    }

    void Update()
    {
        // 揺れアニメーション
        if (!isGrowing && allBranches.Count > 0)
        {
            float swayTime = Time.time * settings.swaySpeed;
            foreach (var branch in allBranches)
            {
                if (branch != null)
                {
                    branch.UpdateSway(swayTime, settings.swayStrength);
                }
            }
        }

        // LOD処理
        if (settings.useLOD && mainCamera != null && !isGrowing)
        {
            UpdateLOD();
        }
    }

    void UpdateLOD()
    {
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);

        foreach (var branch in allBranches)
        {
            if (branch != null)
            {
                TrailRenderer tr = branch.GetComponent<TrailRenderer>();
                if (tr != null)
                {
                    // 距離に応じて詳細度を調整
                    if (distance > settings.lodDistances[2])
                    {
                        // 最も遠い - 非表示または最低品質
                        tr.enabled = branch.GetComponent<TreeBranch3D>().GetWidth() > settings.trunkWidth * 0.2f;
                        tr.numCornerVertices = 3;
                    }
                    else if (distance > settings.lodDistances[1])
                    {
                        // 中距離 - 中品質
                        tr.enabled = true;
                        tr.numCornerVertices = 6;
                    }
                    else if (distance > settings.lodDistances[0])
                    {
                        // 近距離 - 高品質
                        tr.enabled = true;
                        tr.numCornerVertices = 9;
                    }
                    else
                    {
                        // 最も近い - 最高品質
                        tr.enabled = true;
                        tr.numCornerVertices = 12;
                    }
                }
            }
        }
    }

    public void Clear()
    {
        foreach (var branch in allBranches)
        {
            if (branch != null && branch.gameObject != null)
            {
                Destroy(branch.gameObject);
            }
        }
        allBranches.Clear();
        growthQueue.Clear();
        currentlyGrowing = 0;
    }

    void OnDestroy()
    {
        Clear();
    }

    [ContextMenu("Regrow Tree")]
    public void RegrowTree()
    {
        StartGrowth();
    }

    void OnDrawGizmosSelected()
    {
        if (settings == null) return;

        // 成長範囲の3D表示
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireCube(
            transform.position + Vector3.up * (settings.trunkHeight / 2),
            new Vector3(settings.trunkHeight * 2, settings.trunkHeight, settings.trunkHeight * 2)
        );

        // 主枝の開始範囲
        float startY = settings.trunkHeight * settings.mainBranchStartHeight;
        float endY = startY + settings.trunkHeight * settings.mainBranchSpreadHeight;

        Gizmos.color = new Color(1, 1, 0, 0.3f);

        // 円筒形の範囲を表示
        int segments = 20;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (360f / segments) * i * Mathf.Deg2Rad;
            float angle2 = (360f / segments) * (i + 1) * Mathf.Deg2Rad;

            Vector3 p1 = new Vector3(Mathf.Cos(angle1) * 2, startY, Mathf.Sin(angle1) * 2);
            Vector3 p2 = new Vector3(Mathf.Cos(angle2) * 2, startY, Mathf.Sin(angle2) * 2);
            Vector3 p3 = new Vector3(Mathf.Cos(angle1) * 2, endY, Mathf.Sin(angle1) * 2);
            Vector3 p4 = new Vector3(Mathf.Cos(angle2) * 2, endY, Mathf.Sin(angle2) * 2);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p1, p3);
        }
    }

    // カメラ角度に応じた深度可視化のためのヘルパー
    public float GetBranchDepth(TreeBranch3D branch)
    {
        if (mainCamera == null) return 0f;

        Vector3 toBranch = branch.transform.position - mainCamera.transform.position;
        return Vector3.Dot(toBranch, mainCamera.transform.forward);
    }
}