using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EchoTrailSystem : MonoBehaviour
{
    [Header("エコー設定")]
    [SerializeField] private int echoCount = 3;
    [SerializeField] private float echoDelay = 0.1f;
    [SerializeField] private float echoFadeMultiplier = 0.7f;
    [SerializeField] private float echoScaleMultiplier = 0.9f;
    
    [Header("ゴーストトレイル設定")]
    [SerializeField] private bool enableGhostTrails = true;
    [SerializeField] private float ghostDelay = 0.5f;
    [SerializeField] private int maxGhostFrames = 60;
    
    [Header("時間の層設定")]
    [SerializeField] private bool enableTimeLayer = true;
    [SerializeField] private float replayDelay = 3f;
    [SerializeField] private float replaySpeed = 1f;
    [SerializeField] private int maxRecordedPaths = 5;
    
    private TrailArtManager manager;
    private List<RecordedPath> recordedPaths = new List<RecordedPath>();
    private List<EchoTrailData> activeEchoes = new List<EchoTrailData>();
    
    public class RecordedPath
    {
        public List<PathPoint> points = new List<PathPoint>();
        public float recordStartTime;
        public Color baseColor;
        public float width;
    }
    
    public class PathPoint
    {
        public Vector3 position;
        public float timestamp;
        public Quaternion rotation;
    }
    
    public class EchoTrailData
    {
        public TrailController mainTrail;
        public List<TrailController> echoes = new List<TrailController>();
        public Queue<PathPoint> pathHistory = new Queue<PathPoint>();
    }
    
    void Start()
    {
        manager = GetComponent<TrailArtManager>();
        StartCoroutine(TimeLayerReplayCoroutine());
    }
    
    public void CreateEchoesForTrail(TrailController mainTrail)
    {
        if (!enabled) return;
        
        EchoTrailData echoData = new EchoTrailData { mainTrail = mainTrail };
        
        StartCoroutine(CreateEchoesWithDelay(echoData));
        activeEchoes.Add(echoData);
        
        if (enableTimeLayer)
        {
            StartCoroutine(RecordTrailPath(mainTrail));
        }
    }
    
    IEnumerator CreateEchoesWithDelay(EchoTrailData echoData)
    {
        TrailRenderer mainRenderer = echoData.mainTrail.GetComponent<TrailRenderer>();
        
        for (int i = 0; i < echoCount; i++)
        {
            yield return new WaitForSeconds(echoDelay);
            
            if (echoData.mainTrail == null) yield break;
            
            // エコートレイルを生成
            GameObject echoObj = manager.GetTrailFromPool();
            if (echoObj == null) continue;
            
            TrailController echo = echoObj.GetComponent<TrailController>();
            TrailRenderer echoRenderer = echoObj.GetComponent<TrailRenderer>();
            
            // エコーの視覚設定
            float fadeMultiplier = Mathf.Pow(echoFadeMultiplier, i + 1);
            float scaleMultiplier = Mathf.Pow(echoScaleMultiplier, i + 1);
            
            // 色とサイズを調整
            Color echoColor = mainRenderer.material.color;
            echoColor.a *= fadeMultiplier;
            echoRenderer.material.color = echoColor;
            echoRenderer.colorGradient = CreateFadedGradient(mainRenderer.colorGradient, fadeMultiplier);
            echoRenderer.startWidth = mainRenderer.startWidth * scaleMultiplier;
            echoRenderer.endWidth = mainRenderer.endWidth * scaleMultiplier;
            
            echoData.echoes.Add(echo);
            
            // エコーの追従を開始
            StartCoroutine(FollowMainTrail(echo, echoData, i));
        }
    }
    
    IEnumerator FollowMainTrail(TrailController echo, EchoTrailData echoData, int echoIndex)
    {
        float delay = echoDelay * (echoIndex + 1);
        Queue<PathPoint> delayedPath = new Queue<PathPoint>();
        
        while (echoData.mainTrail != null && echo != null)
        {
            // 現在の位置を記録
            PathPoint currentPoint = new PathPoint
            {
                position = echoData.mainTrail.transform.position,
                rotation = echoData.mainTrail.transform.rotation,
                timestamp = Time.time
            };
            
            delayedPath.Enqueue(currentPoint);
            
            // 遅延時間分の古いポイントを適用
            while (delayedPath.Count > 0 && 
                   Time.time - delayedPath.Peek().timestamp > delay)
            {
                PathPoint point = delayedPath.Dequeue();
                echo.transform.position = point.position;
                echo.transform.rotation = point.rotation;
                
                // ゴースト効果の追加
                if (enableGhostTrails && echoIndex == 0)
                {
                    AddGhostEffect(point.position, echo);
                }
            }
            
            yield return null;
        }
        
        // クリーンアップ
        if (echo != null)
        {
            manager.ReturnTrailToPool(echo);
        }
    }
    
    void AddGhostEffect(Vector3 position, TrailController source)
    {
        // 一定間隔でゴースト残像を残す
        if (Random.value < 0.05f)
        {
            GameObject ghost = manager.GetTrailFromPool();
            if (ghost != null)
            {
                ghost.transform.position = position;
                TrailRenderer ghostRenderer = ghost.GetComponent<TrailRenderer>();
                
                // ゴーストの見た目を設定
                Color ghostColor = source.GetComponent<TrailRenderer>().material.color;
                ghostColor.a *= 0.3f;
                ghostRenderer.material.color = ghostColor;
                ghostRenderer.time = 0.5f;
                
                StartCoroutine(FadeOutGhost(ghost, ghostDelay));
            }
        }
    }
    
    IEnumerator FadeOutGhost(GameObject ghost, float duration)
    {
        TrailRenderer renderer = ghost.GetComponent<TrailRenderer>();
        Color startColor = renderer.material.color;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0, elapsed / duration);
            Color color = startColor;
            color.a = alpha;
            renderer.material.color = color;
            yield return null;
        }
        
        manager.ReturnTrailToPool(ghost.GetComponent<TrailController>());
    }
    
    IEnumerator RecordTrailPath(TrailController trail)
    {
        RecordedPath path = new RecordedPath
        {
            recordStartTime = Time.time,
            baseColor = trail.GetComponent<TrailRenderer>().material.color,
            width = trail.GetComponent<TrailRenderer>().startWidth
        };
        
        while (trail != null && trail.gameObject.activeInHierarchy)
        {
            path.points.Add(new PathPoint
            {
                position = trail.transform.position,
                rotation = trail.transform.rotation,
                timestamp = Time.time
            });
            
            yield return new WaitForSeconds(0.05f);
        }
        
        if (path.points.Count > 10)
        {
            recordedPaths.Add(path);
            
            if (recordedPaths.Count > maxRecordedPaths)
            {
                recordedPaths.RemoveAt(0);
            }
        }
    }
    
    IEnumerator TimeLayerReplayCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(replayDelay);
            
            if (enableTimeLayer && recordedPaths.Count > 0)
            {
                // ランダムに過去のパスを選択
                RecordedPath pathToReplay = recordedPaths[Random.Range(0, recordedPaths.Count)];
                StartCoroutine(ReplayPath(pathToReplay));
            }
        }
    }
    
    IEnumerator ReplayPath(RecordedPath path)
    {
        GameObject replayTrail = manager.GetTrailFromPool();
        if (replayTrail == null) yield break;
        
        TrailRenderer renderer = replayTrail.GetComponent<TrailRenderer>();
        
        // リプレイの見た目を設定（半透明で異なる色調）
        Color replayColor = path.baseColor;
        replayColor = Color.Lerp(replayColor, Color.white, 0.3f);
        replayColor.a = 0.5f;
        renderer.material.color = replayColor;
        renderer.startWidth = path.width * 1.2f;
        
        // パスを再生
        for (int i = 0; i < path.points.Count; i++)
        {
            if (replayTrail == null) break;
            
            replayTrail.transform.position = path.points[i].position;
            replayTrail.transform.rotation = path.points[i].rotation;
            
            yield return new WaitForSeconds(0.05f / replaySpeed);
        }
        
        // フェードアウト
        yield return new WaitForSeconds(1f);
        
        if (replayTrail != null)
        {
            manager.ReturnTrailToPool(replayTrail.GetComponent<TrailController>());
        }
    }
    
    Gradient CreateFadedGradient(Gradient original, float fadeMultiplier)
    {
        Gradient faded = new Gradient();
        GradientColorKey[] colorKeys = original.colorKeys;
        GradientAlphaKey[] alphaKeys = original.alphaKeys;
        
        for (int i = 0; i < alphaKeys.Length; i++)
        {
            alphaKeys[i].alpha *= fadeMultiplier;
        }
        
        faded.SetKeys(colorKeys, alphaKeys);
        return faded;
    }
}