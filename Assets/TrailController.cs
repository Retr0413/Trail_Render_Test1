using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TrailController : MonoBehaviour
{
    private TrailSettings settings;
    private TrailArtManager manager;
    private TrailRenderer trailRenderer;
    private float startTime;
    private Vector3 noiseOffset;
    private bool isActive;
    private Vector3 interactionForce = Vector3.zero;
    private Color mixedColor;
    private bool hasMixedColor = false;
    
    public void Initialize(TrailSettings settings, TrailArtManager manager)
    {
        this.settings = settings;
        this.manager = manager;
        this.trailRenderer = GetComponent<TrailRenderer>();
        
        noiseOffset = new Vector3(
            Random.Range(-100f, 100f),
            Random.Range(-100f, 100f),
            Random.Range(-100f, 100f)
        );
    }
    
    public void StartTrail()
    {
        startTime = Time.time;
        isActive = true;
        trailRenderer.Clear();
        trailRenderer.emitting = true;
    }
    
    void Update()
    {
        if (!isActive) return;
        
        float elapsed = Time.time - startTime;
        
        // 寿命チェック
        if (elapsed > settings.lifetime)
        {
            manager.ReturnTrailToPool(this);
            return;
        }
        
        // 動きの計算
        UpdateMovement();
        
        // フェードアウト
        if (elapsed > settings.fadeStartTime)
        {
            float fadeProgress = (elapsed - settings.fadeStartTime) / (settings.lifetime - settings.fadeStartTime);
            UpdateFade(1f - fadeProgress);
        }
    }
    
    void UpdateMovement()
    {
        // Perlinノイズによる有機的な動き
        float noiseX = Mathf.PerlinNoise(Time.time * settings.noiseFrequency + noiseOffset.x, 0) - 0.5f;
        float noiseY = Mathf.PerlinNoise(Time.time * settings.noiseFrequency + noiseOffset.y, 100) - 0.5f;
        
        Vector3 movement = new Vector3(noiseX, noiseY, 0) * settings.noiseStrength;
        movement += Vector3.up * 0.3f; // 基本的な上昇
        
        transform.position += movement * settings.moveSpeed * Time.deltaTime;
        transform.Rotate(Vector3.forward, settings.rotationSpeed * Time.deltaTime);
    }
    
    void UpdateFade(float alpha)
    {
        Color color = trailRenderer.material.color;
        color.a = alpha;
        trailRenderer.material.color = color;
    }

    // 相互作用の力を追加
    public void AddInteractionForce(Vector3 force)
    {
        interactionForce += force;
    }
    
    // 現在の色を取得
    public Color GetCurrentColor()
    {
        return GetComponent<TrailRenderer>().material.color;
    }
    
    // 混合色を設定
    public void SetMixedColor(Color color)
    {
        mixedColor = color;
        hasMixedColor = true;
    }
    
    // Updateメソッドに追加する処理
    void ApplyInteractionEffects()
    {
        // 相互作用の力を適用
        if (interactionForce.magnitude > 0.01f)
        {
            transform.position += interactionForce * Time.deltaTime;
            interactionForce *= 0.9f; // 減衰
        }
        
        // 色の混合を適用
        if (hasMixedColor)
        {
            TrailRenderer renderer = GetComponent<TrailRenderer>();
            Color currentColor = renderer.material.color;
            renderer.material.color = Color.Lerp(currentColor, mixedColor, Time.deltaTime * 2f);
        }
    }
}