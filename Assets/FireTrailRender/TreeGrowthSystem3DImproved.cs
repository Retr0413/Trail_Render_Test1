using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeSettings3DImproved
{
    [Header("木の基本設定")]
    [Tooltip("幹の高さ")]
    public float trunkHeight = 15f;

    [Tooltip("成長速度")]
    public float growthSpeed = 6f;

    [Tooltip("幹の太さ")]
    public float trunkWidth = 1.2f;

    [Header("主枝の設定")]
    [Tooltip("主枝の本数")]
    public int mainBranchCount = 12;

    [Tooltip("主枝の開始高さ（幹の何％から）")]
    public float mainBranchStartHeight = 0.25f;

    [Tooltip("主枝の分布範囲（幹の何％の範囲）")]
    public float mainBranchSpreadHeight = 0.65f;

    [Header("分岐設定")]
    [Tooltip("最大分岐世代数")]
    public int maxBranchGenerations = 4;

    [Tooltip("各世代での分岐数")]
    public int[] branchesPerGeneration = new int[] { 2, 3, 2, 2 };

    [Tooltip("分岐確率（世代ごと）")]
    public float[] branchProbability = new float[] { 1f, 0.85f, 0.7f, 0.5f };

    [Header("枝のパラメータ")]
    [Tooltip("枝の長さ減衰率")]
    public float branchLengthDecay = 0.75f;

    [Tooltip("枝の太さ減衰率")]
    public float branchWidthDecay = 0.6f;

    [Tooltip("初期枝の長さ")]
    public float initialBranchLength = 8f;

    [Tooltip("枝の長さランダム範囲")]
    public Vector2 branchLengthRandomRange = new Vector2(0.6f, 1.4f);

    [Header("3D空間設定")]
    [Tooltip("枝の水平広がり（度）")]
    public float horizontalSpread = 85f;

    [Tooltip("枝の垂直広がり（度）")]
    public float verticalSpread = 65f;

    [Tooltip("枝の仰角範囲（度）")]
    public Vector2 elevationRange = new Vector2(15f, 75f);

    [Tooltip("らせん状成長の強度")]
    public float spiralStrength = 0.3f;

    [Tooltip("枝の外向き成長強度")]
    public float outwardGrowthBias = 1.2f;

    [Tooltip("重力の影響")]
    public float gravityInfluence = 0.15f;

    [Header("角度設定")]
    [Tooltip("分岐角度の基本値")]
    public float baseAngle = 45f;

    [Tooltip("角度のランダム幅")]
    public float angleVariation = 25f;

    [Tooltip("世代ごとの角度増加")]
    public float angleIncreasePerGeneration = 8f;

    [Header("分岐位置設定")]
    [Tooltip("枝上の分岐開始位置（0-1）")]
    public float branchStartPosition = 0.4f;

    [Tooltip("分岐位置のランダム性")]
    public float branchPositionVariation = 0.3f;

    [Tooltip("分岐点の間隔")]
    public float branchSpacing = 0.25f;

    [Header("TrailRenderer設定")]
    [Tooltip("トレイルの継続時間")]
    public float trailTime = 40f;

    [Tooltip("頂点間の最小距離")]
    public float minVertexDistance = 0.01f;

    [Header("ビジュアル設定")]
    public ColorPalette colorPalette;
    public bool useAdditiveBlending = false;
    public bool castShadows = true;

    [Header("アニメーション設定")]
    [Tooltip("色の変化速度")]
    public float colorChangeSpeed = 2f;

    [Tooltip("揺れの強さ")]
    public float swayStrength = 0.2f;

    [Tooltip("揺れの速度")]
    public float swaySpeed = 0.8f;

    [Header("パフォーマンス設定")]
    [Tooltip("同時成長枝数の制限")]
    public int maxConcurrentGrowth = 8;

    [Tooltip("成長遅延の最小時間")]
    public float minGrowthDelay = 0.03f;

    [Header("カメラ設定")]
    [Tooltip("カメラとの距離に応じてLODを調整")]
    public bool useLOD = true;

    [Tooltip("LOD距離閾値")]
    public float[] lodDistances = new float[] { 15f, 35f, 60f };
}

public class TreeGrowthSystem3DImproved : MonoBehaviour
{
    [SerializeField] private TreeSettings3DImproved settings;
    [SerializeField] private bool usePreset = true;

    public enum TreePreset
    {
        Oak,        // 樫の木 - 広がりのある大木
        Pine,       // 松の木 - 縦に長く、コンパクト
        Willow,     // 柳の木 - 下向きに垂れる枝
        Sakura,     // 桜の木 - 横に広がる
        Bonsai      // 盆栽 - コンパクトで芸術的
    }

    [SerializeField] private TreePreset treePreset = TreePreset.Oak;

    private List<TreeBranch3D> allBranches = new List<TreeBranch3D>();
    private Queue<BranchGrowthData3D> growthQueue = new Queue<BranchGrowthData3D>();
    private int currentlyGrowing = 0;
    private bool isGrowing = false;
    private Camera mainCamera;

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
            settings = new TreeSettings3DImproved();
        }

        if (usePreset)
        {
            ApplyPreset(treePreset);
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        InitializeArrays();
        StartGrowth();
    }

    void ApplyPreset(TreePreset preset)
    {
        switch (preset)
        {
            case TreePreset.Oak:
                // 樫の木 - 広がりのある大木
                settings.trunkHeight = 20f;
                settings.trunkWidth = 1.5f;
                settings.mainBranchCount = 15;
                settings.initialBranchLength = 10f;
                settings.horizontalSpread = 90f;
                settings.verticalSpread = 70f;
                settings.outwardGrowthBias = 1.5f;
                settings.baseAngle = 50f;
                settings.branchLengthDecay = 0.8f;
                settings.maxBranchGenerations = 5;
                settings.branchesPerGeneration = new int[] { 3, 3, 2, 2, 1 };
                break;

            case TreePreset.Pine:
                // 松の木 - 縦に長く、層状
                settings.trunkHeight = 25f;
                settings.trunkWidth = 1.0f;
                settings.mainBranchCount = 20;
                settings.initialBranchLength = 6f;
                settings.horizontalSpread = 60f;
                settings.verticalSpread = 40f;
                settings.outwardGrowthBias = 0.8f;
                settings.baseAngle = 30f;
                settings.branchLengthDecay = 0.7f;
                settings.gravityInfluence = 0.05f;
                break;

            case TreePreset.Willow:
                // 柳の木 - 下向きに垂れる枝
                settings.trunkHeight = 18f;
                settings.trunkWidth = 1.3f;
                settings.mainBranchCount = 12;
                settings.initialBranchLength = 12f;
                settings.horizontalSpread = 80f;
                settings.verticalSpread = 85f;
                settings.elevationRange = new Vector2(-30f, 45f);
                settings.gravityInfluence = 0.4f;
                settings.outwardGrowthBias = 1.2f;
                break;

            case TreePreset.Sakura:
                // 桜の木 - 横に広がる
                settings.trunkHeight = 15f;
                settings.trunkWidth = 1.4f;
                settings.mainBranchCount = 18;
                settings.initialBranchLength = 11f;
                settings.horizontalSpread = 100f;
                settings.verticalSpread = 60f;
                settings.outwardGrowthBias = 2.0f;
                settings.baseAngle = 60f;
                settings.branchLengthDecay = 0.85f;
                settings.mainBranchStartHeight = 0.2f;
                break;

            case TreePreset.Bonsai:
                // 盆栽 - コンパクトで芸術的
                settings.trunkHeight = 8f;
                settings.trunkWidth = 0.8f;
                settings.mainBranchCount = 8;
                settings.initialBranchLength = 4f;
                settings.horizontalSpread = 70f;
                settings.spiralStrength = 0.8f;
                settings.outwardGrowthBias = 1.0f;
                settings.maxBranchGenerations = 3;
                break;
        }
    }

    void InitializeArrays()
    {
        if (settings.branchesPerGeneration == null || settings.branchesPerGeneration.Length < settings.maxBranchGenerations)
        {
            settings.branchesPerGeneration = new int[] { 3, 3, 2, 2, 1 };
        }
        if (settings.branchProbability == null || settings.branchProbability.Length < settings.maxBranchGenerations)
        {
            settings.branchProbability = new float[] { 1f, 0.9f, 0.8f, 0.6f, 0.4f };
        }
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
        // 幹の生成（わずかに曲がりを持たせる）
        GameObject trunkObj = new GameObject("Trunk3D");
        trunkObj.transform.parent = transform;
        TreeBranch3D trunk = trunkObj.AddComponent<TreeBranch3D>();

        // 幹に微妙な傾きを加える
        Vector3 trunkDirection = Vector3.up + new Vector3(
            Random.Range(-0.05f, 0.05f),
            0,
            Random.Range(-0.05f, 0.05f)
        );

        trunk.Initialize(
            ConvertSettings(),
            Vector3.zero,
            trunkDirection.normalized,
            settings.trunkHeight,
            settings.trunkWidth,
            0
        );

        allBranches.Add(trunk);
        trunk.StartGrowth();

        // 幹の成長を待つ
        yield return new WaitForSeconds(settings.trunkHeight / settings.growthSpeed * 0.6f);

        // 主枝の生成
        CreateMainBranches(trunk);

        // すべての成長が完了するまで待つ
        while (growthQueue.Count > 0 || currentlyGrowing > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        isGrowing = false;
    }

    TreeSettings3D ConvertSettings()
    {
        TreeSettings3D converted = new TreeSettings3D
        {
            trunkHeight = settings.trunkHeight,
            growthSpeed = settings.growthSpeed,
            trunkWidth = settings.trunkWidth,
            trailTime = settings.trailTime,
            minVertexDistance = settings.minVertexDistance,
            colorPalette = settings.colorPalette,
            useAdditiveBlending = settings.useAdditiveBlending,
            colorChangeSpeed = settings.colorChangeSpeed
        };
        return converted;
    }

    void CreateMainBranches(TreeBranch3D trunk)
    {
        float startHeight = settings.trunkHeight * settings.mainBranchStartHeight;
        float spreadHeight = settings.trunkHeight * settings.mainBranchSpreadHeight;

        // 層状に主枝を配置
        int layerCount = 3 + settings.maxBranchGenerations;
        int branchesPerLayer = Mathf.CeilToInt(settings.mainBranchCount / (float)layerCount);

        for (int layer = 0; layer < layerCount; layer++)
        {
            float layerHeight = startHeight + (spreadHeight * layer / layerCount);
            int branchesInThisLayer = Mathf.Min(branchesPerLayer, settings.mainBranchCount - (layer * branchesPerLayer));

            for (int i = 0; i < branchesInThisLayer; i++)
            {
                float heightVariation = Random.Range(-0.05f, 0.05f) * settings.trunkHeight;
                float actualHeight = layerHeight + heightVariation;

                // 放射状に配置（各層で角度をずらす）
                float baseAngle = (360f / branchesInThisLayer) * i;
                float layerOffset = (layer % 2) * (180f / branchesInThisLayer); // 層ごとにオフセット
                float angle = baseAngle + layerOffset + Random.Range(-15f, 15f);

                // らせん効果を追加
                angle += layer * settings.spiralStrength * 30f;

                float branchPointOnTrunk = actualHeight / settings.trunkHeight;
                branchPointOnTrunk = Mathf.Clamp01(branchPointOnTrunk);

                Vector3 startPos = trunk.GetBranchPoint(branchPointOnTrunk);

                // 仰角を高さに応じて変化
                float heightRatio = branchPointOnTrunk;
                float elevation = Mathf.Lerp(settings.elevationRange.y, settings.elevationRange.x, heightRatio);
                elevation += Random.Range(-10f, 10f);

                // 外向きの成長を強調
                Vector3 outwardDirection = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad),
                    0,
                    Mathf.Cos(angle * Mathf.Deg2Rad)
                ).normalized;

                Vector3 upwardDirection = Vector3.up;

                // 仰角に基づいて方向を混合
                float elevationRad = elevation * Mathf.Deg2Rad;
                Vector3 direction = (outwardDirection * Mathf.Sin(elevationRad) * settings.outwardGrowthBias +
                                    upwardDirection * Mathf.Cos(elevationRad)).normalized;

                // 主枝を作成
                GameObject branchObj = new GameObject($"MainBranch_{layer}_{i}");
                branchObj.transform.parent = transform;
                TreeBranch3D mainBranch = branchObj.AddComponent<TreeBranch3D>();

                // 長さと太さを層に応じて調整
                float layerDecay = 1f - (layer * 0.1f);
                float length = settings.initialBranchLength *
                             Random.Range(settings.branchLengthRandomRange.x, settings.branchLengthRandomRange.y) *
                             layerDecay;
                float width = settings.trunkWidth * 0.35f * Random.Range(0.8f, 1.2f) * layerDecay;

                mainBranch.Initialize(
                    ConvertSettings(),
                    startPos,
                    direction,
                    length,
                    width,
                    1
                );

                allBranches.Add(mainBranch);

                // 成長キューに追加（層ごとに遅延）
                float delay = layer * 0.15f + Random.Range(0f, 0.2f);
                growthQueue.Enqueue(new BranchGrowthData3D(mainBranch, trunk, 1, delay));

                // この主枝から分岐を生成
                ScheduleBranchSplits(mainBranch, 2);
            }
        }
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

        // 分岐点を配置
        int splitPoints = Mathf.Max(1, 2);

        for (int sp = 0; sp < splitPoints; sp++)
        {
            float position = settings.branchStartPosition + (sp * settings.branchSpacing);
            position += Random.Range(-settings.branchPositionVariation * 0.5f, settings.branchPositionVariation * 0.5f);
            position = Mathf.Clamp(position, 0.3f, 0.9f);

            // 放射状に分岐を配置
            float angleStep = 360f / branchCount;
            float randomOffset = Random.Range(0f, angleStep);

            for (int i = 0; i < branchCount; i++)
            {
                float branchAngle = angleStep * i + randomOffset;
                CreateSubBranch(parentBranch, nextGeneration, position, branchAngle);
            }
        }
    }

    void CreateSubBranch(TreeBranch3D parent, int generation, float positionOnParent, float azimuthAngle)
    {
        Vector3 startPos = parent.GetBranchPoint(positionOnParent);
        Vector3 parentDir = parent.GetDirection();

        // 分岐角度の計算
        float spreadAngle = settings.baseAngle + (generation - 1) * settings.angleIncreasePerGeneration;
        spreadAngle += Random.Range(-settings.angleVariation, settings.angleVariation);

        // 外向きベクトルの計算
        Vector3 parentHorizontal = new Vector3(parentDir.x, 0, parentDir.z).normalized;
        if (parentHorizontal.magnitude < 0.1f)
        {
            parentHorizontal = Vector3.forward;
        }

        // 方位角から外向きベクトルを生成
        float azimuthRad = azimuthAngle * Mathf.Deg2Rad;
        Vector3 outward = new Vector3(
            Mathf.Sin(azimuthRad),
            0,
            Mathf.Cos(azimuthRad)
        );

        // 親の方向と外向きベクトルを組み合わせて最終方向を決定
        float spreadRad = spreadAngle * Mathf.Deg2Rad;
        Vector3 direction = (parentDir * Mathf.Cos(spreadRad) +
                           outward * Mathf.Sin(spreadRad) * settings.outwardGrowthBias).normalized;

        // 重力の影響を追加
        direction.y -= settings.gravityInfluence * generation * 0.1f;
        direction = direction.normalized;

        // パラメータの計算
        float lengthDecay = Mathf.Pow(settings.branchLengthDecay, generation - 1);
        float widthDecay = Mathf.Pow(settings.branchWidthDecay, generation - 1);

        float length = settings.initialBranchLength * lengthDecay *
                      Random.Range(settings.branchLengthRandomRange.x, settings.branchLengthRandomRange.y);
        float width = settings.trunkWidth * 0.3f * widthDecay * Random.Range(0.7f, 1.1f);

        // 枝オブジェクトを作成
        GameObject branchObj = new GameObject($"Branch_G{generation}_{allBranches.Count}");
        branchObj.transform.parent = transform;
        TreeBranch3D newBranch = branchObj.AddComponent<TreeBranch3D>();

        newBranch.Initialize(
            ConvertSettings(),
            startPos,
            direction,
            length,
            width,
            generation
        );

        allBranches.Add(newBranch);

        // 成長キューに追加
        float delay = Random.Range(settings.minGrowthDelay, 0.15f) * generation;
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
                    if (distance > settings.lodDistances[2])
                    {
                        tr.enabled = branch.GetComponent<TreeBranch3D>().GetWidth() > settings.trunkWidth * 0.15f;
                        tr.numCornerVertices = 3;
                    }
                    else if (distance > settings.lodDistances[1])
                    {
                        tr.enabled = true;
                        tr.numCornerVertices = 6;
                    }
                    else if (distance > settings.lodDistances[0])
                    {
                        tr.enabled = true;
                        tr.numCornerVertices = 9;
                    }
                    else
                    {
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

        // 広がりを可視化
        float maxSpread = settings.initialBranchLength * 2.5f;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * settings.trunkHeight * 0.6f, maxSpread);

        // 高さ範囲
        float startY = settings.trunkHeight * settings.mainBranchStartHeight;
        float endY = startY + settings.trunkHeight * settings.mainBranchSpreadHeight;

        Gizmos.color = new Color(1, 1, 0, 0.2f);

        // 円筒形の範囲を表示
        int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (360f / segments) * i * Mathf.Deg2Rad;
            float angle2 = (360f / segments) * (i + 1) * Mathf.Deg2Rad;

            float radius = maxSpread * 0.7f;
            Vector3 p1 = new Vector3(Mathf.Cos(angle1) * radius, startY, Mathf.Sin(angle1) * radius);
            Vector3 p2 = new Vector3(Mathf.Cos(angle2) * radius, startY, Mathf.Sin(angle2) * radius);
            Vector3 p3 = new Vector3(Mathf.Cos(angle1) * radius, endY, Mathf.Sin(angle1) * radius);
            Vector3 p4 = new Vector3(Mathf.Cos(angle2) * radius, endY, Mathf.Sin(angle2) * radius);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p1, p3);
        }
    }
}