using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeSettings
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
}

public class TreeGrowthSystem : MonoBehaviour
{
    [SerializeField] private TreeSettings settings;
    
    private List<TreeBranch> allBranches = new List<TreeBranch>();
    private Queue<BranchGrowthData> growthQueue = new Queue<BranchGrowthData>();
    private int currentlyGrowing = 0;
    private bool isGrowing = false;
    
    // 枝成長データ
    private class BranchGrowthData
    {
        public TreeBranch branch;
        public TreeBranch parent;
        public int generation;
        public float delay;
        
        public BranchGrowthData(TreeBranch b, TreeBranch p, int g, float d)
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
            settings = new TreeSettings();
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
        GameObject trunkObj = new GameObject("Trunk");
        trunkObj.transform.parent = transform;
        TreeBranch trunk = trunkObj.AddComponent<TreeBranch>();
        
        trunk.Initialize(
            settings,
            Vector2.zero,
            Vector2.up,
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
    
    void CreateMainBranches(TreeBranch trunk)
    {
        float startHeight = settings.trunkHeight * settings.mainBranchStartHeight;
        float spreadHeight = settings.trunkHeight * settings.mainBranchSpreadHeight;
        
        for (int i = 0; i < settings.mainBranchCount; i++)
        {
            // 主枝の配置を計算
            float heightRatio = (float)i / settings.mainBranchCount;
            float height = startHeight + (spreadHeight * heightRatio);
            height += Random.Range(-0.1f, 0.1f) * settings.trunkHeight * 0.1f;
            
            // らせん状に配置
            float angle = (360f / settings.mainBranchCount * i) + Random.Range(-20f, 20f);
            angle += (heightRatio * 180f); // 高さに応じて回転
            
            float branchPointOnTrunk = height / settings.trunkHeight;
            branchPointOnTrunk = Mathf.Clamp01(branchPointOnTrunk);
            
            Vector2 startPos = trunk.GetBranchPoint(branchPointOnTrunk);
            
            // 枝の方向（上向き気味で横に広がる）
            float elevation = Random.Range(10f, 45f);
            Vector2 direction = new Vector2(
                Mathf.Sin(angle * Mathf.Deg2Rad) * Mathf.Cos(elevation * Mathf.Deg2Rad),
                Mathf.Cos(elevation * Mathf.Deg2Rad)
            ).normalized;
            
            // 主枝を作成
            GameObject branchObj = new GameObject($"MainBranch_{i}");
            branchObj.transform.parent = transform;
            TreeBranch mainBranch = branchObj.AddComponent<TreeBranch>();
            
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
            growthQueue.Enqueue(new BranchGrowthData(mainBranch, trunk, 1, delay));
            
            // この主枝から分岐を生成
            ScheduleBranchSplits(mainBranch, 2);
        }
    }
    
    void ScheduleBranchSplits(TreeBranch parentBranch, int nextGeneration)
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
    
    void CreateSubBranch(TreeBranch parent, int generation, float positionOnParent, int index, int totalCount)
    {
        Vector2 startPos = parent.GetBranchPoint(positionOnParent);
        
        // 分岐角度の計算
        float baseAngle = settings.baseAngle + (generation - 1) * settings.angleIncreasePerGeneration;
        float spread = baseAngle + Random.Range(-settings.angleVariation, settings.angleVariation);
        
        // 左右交互に配置
        if (totalCount > 1)
        {
            float angleStep = spread * 2f / (totalCount - 1);
            spread = -spread + (angleStep * index);
        }
        else
        {
            spread *= (Random.Range(0, 2) == 0 ? 1 : -1);
        }
        
        // 親の方向から相対的に計算
        Vector2 parentDir = parent.GetDirection();
        float parentAngle = Mathf.Atan2(parentDir.y, parentDir.x) * Mathf.Rad2Deg;
        float finalAngle = parentAngle + spread + 90f; // 90度は上向き補正
        
        Vector2 direction = new Vector2(
            Mathf.Cos(finalAngle * Mathf.Deg2Rad),
            Mathf.Sin(finalAngle * Mathf.Deg2Rad)
        ).normalized;
        
        // パラメータの計算
        float lengthDecay = Mathf.Pow(settings.branchLengthDecay, generation - 1);
        float widthDecay = Mathf.Pow(settings.branchWidthDecay, generation - 1);
        
        float length = settings.initialBranchLength * lengthDecay * Random.Range(0.7f, 1.3f);
        float width = settings.trunkWidth * 0.4f * widthDecay * Random.Range(0.8f, 1.2f);
        
        // 枝オブジェクトを作成
        GameObject branchObj = new GameObject($"Branch_G{generation}_{allBranches.Count}");
        branchObj.transform.parent = transform;
        TreeBranch newBranch = branchObj.AddComponent<TreeBranch>();
        
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
        growthQueue.Enqueue(new BranchGrowthData(newBranch, parent, generation, delay));
        
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
                BranchGrowthData data = growthQueue.Dequeue();
                StartCoroutine(GrowBranchWithDelay(data));
            }
            yield return new WaitForSeconds(0.01f);
        }
    }
    
    IEnumerator GrowBranchWithDelay(BranchGrowthData data)
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
        
        // 成長範囲の表示
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireCube(
            transform.position + Vector3.up * (settings.trunkHeight / 2), 
            new Vector3(settings.trunkHeight * 2, settings.trunkHeight, 0.1f)
        );
        
        // 主枝の開始範囲
        float startY = settings.trunkHeight * settings.mainBranchStartHeight;
        float endY = startY + settings.trunkHeight * settings.mainBranchSpreadHeight;
        
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawLine(
            new Vector3(-2, startY, 0),
            new Vector3(2, startY, 0)
        );
        Gizmos.DrawLine(
            new Vector3(-2, endY, 0),
            new Vector3(2, endY, 0)
        );
    }
}