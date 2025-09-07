using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TrailInteractionSystem : MonoBehaviour
{
    [Header("相互作用設定")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private float attractionForce = 5f;
    [SerializeField] private float repulsionForce = 8f;
    [SerializeField] private float interactionThreshold = 1f;
    
    [Header("色の混合設定")]
    [SerializeField] private bool enableColorMixing = true;
    [SerializeField] private float colorMixRadius = 1.5f;
    [SerializeField] private float colorMixSpeed = 2f;
    
    [Header("分裂・融合設定")]
    [SerializeField] private bool enableSplitMerge = true;
    [SerializeField] private float mergeDistance = 0.5f;
    [SerializeField] private float splitVelocity = 5f;
    [SerializeField] private GameObject splitEffectPrefab;
    
    private TrailArtManager manager;
    private List<TrailInteractionData> interactionData = new List<TrailInteractionData>();
    
    public class TrailInteractionData
    {
        public TrailController controller;
        public Vector3 velocity;
        public Color currentColor;
        public List<TrailController> nearbyTrails;
        public float energy = 1f;
        
        public TrailInteractionData(TrailController ctrl)
        {
            controller = ctrl;
            velocity = Vector3.zero;
            nearbyTrails = new List<TrailController>();
        }
    }
    
    void Start()
    {
        manager = GetComponent<TrailArtManager>();
    }
    
    public void RegisterTrail(TrailController trail)
    {
        interactionData.Add(new TrailInteractionData(trail));
    }
    
    public void UnregisterTrail(TrailController trail)
    {
        interactionData.RemoveAll(data => data.controller == trail);
    }
    
    void FixedUpdate()
    {
        UpdateNearbyTrails();
        ApplyInteractionForces();
        
        if (enableColorMixing)
            ProcessColorMixing();
        
        if (enableSplitMerge)
            ProcessSplitMerge();
    }
    
    void UpdateNearbyTrails()
    {
        for (int i = 0; i < interactionData.Count; i++)
        {
            var data = interactionData[i];
            if (data.controller == null) continue;
            
            data.nearbyTrails.Clear();
            Vector3 pos1 = data.controller.transform.position;
            
            for (int j = 0; j < interactionData.Count; j++)
            {
                if (i == j) continue;
                
                var other = interactionData[j];
                if (other.controller == null) continue;
                
                Vector3 pos2 = other.controller.transform.position;
                float distance = Vector3.Distance(pos1, pos2);
                
                if (distance < interactionRadius)
                {
                    data.nearbyTrails.Add(other.controller);
                }
            }
        }
    }
    
    void ApplyInteractionForces()
    {
        foreach (var data in interactionData)
        {
            if (data.controller == null) continue;
            
            Vector3 totalForce = Vector3.zero;
            Vector3 myPos = data.controller.transform.position;
            
            foreach (var nearbyTrail in data.nearbyTrails)
            {
                Vector3 otherPos = nearbyTrail.transform.position;
                Vector3 direction = otherPos - myPos;
                float distance = direction.magnitude;
                
                if (distance < 0.01f) continue;
                
                direction.Normalize();
                
                // 距離に応じて引力と斥力を切り替え
                if (distance > interactionThreshold)
                {
                    // 引力
                    float attractMagnitude = attractionForce * (1f - distance / interactionRadius);
                    totalForce += direction * attractMagnitude * Time.fixedDeltaTime;
                }
                else
                {
                    // 斥力
                    float repulseMagnitude = repulsionForce * (1f - distance / interactionThreshold);
                    totalForce -= direction * repulseMagnitude * Time.fixedDeltaTime;
                }
            }
            
            // 力を速度に変換
            data.velocity += totalForce;
            data.velocity *= 0.95f; // 減衰
            
            // 位置を更新
            data.controller.AddInteractionForce(data.velocity);
        }
    }
    
    void ProcessColorMixing()
    {
        foreach (var data in interactionData)
        {
            if (data.controller == null) continue;
            
            Color mixedColor = data.controller.GetCurrentColor();
            float mixCount = 1f;
            
            foreach (var nearbyTrail in data.nearbyTrails)
            {
                float distance = Vector3.Distance(
                    data.controller.transform.position,
                    nearbyTrail.transform.position
                );
                
                if (distance < colorMixRadius)
                {
                    float influence = 1f - (distance / colorMixRadius);
                    Color otherColor = nearbyTrail.GetComponent<TrailRenderer>().material.color;
                    mixedColor += otherColor * influence;
                    mixCount += influence;
                }
            }
            
            mixedColor /= mixCount;
            data.currentColor = Color.Lerp(data.currentColor, mixedColor, colorMixSpeed * Time.deltaTime);
            data.controller.SetMixedColor(data.currentColor);
        }
    }
    
    void ProcessSplitMerge()
    {
        List<(TrailController, TrailController)> toMerge = new List<(TrailController, TrailController)>();
        
        for (int i = 0; i < interactionData.Count; i++)
        {
            var data1 = interactionData[i];
            if (data1.controller == null) continue;
            
            for (int j = i + 1; j < interactionData.Count; j++)
            {
                var data2 = interactionData[j];
                if (data2.controller == null) continue;
                
                float distance = Vector3.Distance(
                    data1.controller.transform.position,
                    data2.controller.transform.position
                );
                
                // 融合判定
                if (distance < mergeDistance && data1.energy > 0.5f && data2.energy > 0.5f)
                {
                    toMerge.Add((data1.controller, data2.controller));
                }
            }
            
            // 分裂判定（エネルギーが高い時）
            if (data1.energy > 1.5f && Random.value < 0.01f)
            {
                SplitTrail(data1.controller);
                data1.energy = 0.7f;
            }
        }
        
        // 融合処理
        foreach (var (trail1, trail2) in toMerge)
        {
            MergeTrails(trail1, trail2);
        }
    }
    
    void SplitTrail(TrailController original)
    {
        Vector3 pos = original.transform.position;
        
        // 分裂エフェクト
        if (splitEffectPrefab != null)
        {
            Instantiate(splitEffectPrefab, pos, Quaternion.identity);
        }
        
        // 新しいトレイルを2つ生成
        Vector3 offset1 = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized * 0.5f;
        Vector3 offset2 = -offset1;
        
        manager.SpawnTrailAtPosition(pos + offset1);
        manager.SpawnTrailAtPosition(pos + offset2);
    }
    
    void MergeTrails(TrailController trail1, TrailController trail2)
    {
        Vector3 midPoint = (trail1.transform.position + trail2.transform.position) / 2f;
        
        // 新しい強化されたトレイルを生成
        manager.SpawnEnhancedTrailAtPosition(midPoint, 1.5f);
        
        // 元のトレイルを削除
        manager.ReturnTrailToPool(trail1);
        manager.ReturnTrailToPool(trail2);
    }
}