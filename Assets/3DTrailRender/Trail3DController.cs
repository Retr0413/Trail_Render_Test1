using UnityEngine;

public class Trail3DController : MonoBehaviour
{
    private Trail3DManager manager;
    private TrailRenderer trailRenderer;
    private float startTime;
    private Vector3 startPosition;
    private Vector3 noiseOffset;
    
    [Header("3D動作設定")]
    private float lifetime = 5f;
    private float upwardSpeed = 2f;
    
    [Header("川の流れ動作設定")]
    private float flowSpeed = 2f;
    private float waveAmplitude = 0.5f;
    private float waveFrequency = 1f;
    private float driftStrength = 0.3f;
    private Vector3 flowDirection;
    private Vector3 currentVelocity;
    private float flowPhase;
    
    [Header("花の設定")]
    private bool shouldSpawnFlowers = false;
    private float flowerSpawnInterval = 1.5f;
    private int flowersPerVine = 3;
    private ColorPalette flowerColorPalette;
    private float nextFlowerTime = 0f;
    private int flowersSpawned = 0; 
    
    public void Initialize(Trail3DManager mgr, float life, float upSpeed)
    {
        manager = mgr;
        trailRenderer = GetComponent<TrailRenderer>();
        lifetime = life;  
        upwardSpeed = upSpeed; 
        
        noiseOffset = new Vector3(
            Random.Range(-100f, 100f),
            Random.Range(-100f, 100f),
            Random.Range(-100f, 100f)
        );
        
        // 川の流れの初期方向を設定
        flowDirection = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(0.2f, 0.5f),
            Random.Range(-0.3f, 0.3f)
        ).normalized;
        
        // 流れのパラメータをランダム化
        flowSpeed = Random.Range(1.5f, 2.5f);
        waveAmplitude = Random.Range(0.3f, 0.7f);
        waveFrequency = Random.Range(0.8f, 1.5f);
        driftStrength = Random.Range(0.2f, 0.4f);
        currentVelocity = Vector3.zero;
    }
    
    public void StartTrail(Vector3 position)
    {
        startTime = Time.time;
        startPosition = position;
        trailRenderer.Clear();
        trailRenderer.emitting = true;
        
        // 流れの初期化
        flowPhase = Random.Range(0f, Mathf.PI * 2f);
        currentVelocity = flowDirection * flowSpeed * 0.5f;
        
        // 花のスポーン設定をリセット
        flowersSpawned = 0;
        nextFlowerTime = Time.time + flowerSpawnInterval;
    }
    
    void Update()
    {
        float elapsed = Time.time - startTime;
        
        if (elapsed > lifetime)
        {
            manager.ReturnTrailToPool(gameObject);
            return;
        }
        
        Update3DMovement();
        UpdateFade(elapsed);
        
        // 花を咲かせる処理
        if (shouldSpawnFlowers && flowersSpawned < flowersPerVine)
        {
            if (Time.time >= nextFlowerTime)
            {
                SpawnFlower();
                flowersSpawned++;
                nextFlowerTime = Time.time + flowerSpawnInterval;
            }
        }
    }
    
    void Update3DMovement()
    {
        float elapsed = Time.time - startTime;
        float time = Time.time;
        
        // 川の流れのような滑らかな波動
        flowPhase += waveFrequency * Time.deltaTime;
        
        // 横方向の波打ち（正弦波）
        float sineWave = Mathf.Sin(flowPhase) * waveAmplitude;
        float cosWave = Mathf.Cos(flowPhase * 0.7f) * waveAmplitude * 0.5f;
        
        // Perlinノイズで自然なうねりを追加
        float driftX = Mathf.PerlinNoise(time * 0.5f + noiseOffset.x, 0) - 0.5f;
        float driftY = Mathf.PerlinNoise(time * 0.4f + noiseOffset.y, 100) - 0.5f;
        float driftZ = Mathf.PerlinNoise(time * 0.6f + noiseOffset.z, 200) - 0.5f;
        
        Vector3 drift = new Vector3(driftX, driftY * 0.3f, driftZ) * driftStrength;
        
        // 流れの方向をゆっくり変化
        float dirX = Mathf.PerlinNoise(time * 0.2f + noiseOffset.x + 1000, 0) - 0.5f;
        float dirY = Mathf.PerlinNoise(time * 0.2f + noiseOffset.y + 1000, 0) - 0.5f;
        float dirZ = Mathf.PerlinNoise(time * 0.2f + noiseOffset.z + 1000, 0) - 0.5f;
        
        flowDirection = Vector3.Lerp(
            flowDirection,
            new Vector3(dirX * 0.5f, upwardSpeed * 0.3f + dirY * 0.2f, dirZ * 0.5f).normalized,
            Time.deltaTime * 0.3f
        );
        
        // 横方向の波を計算
        Vector3 waveOffset = new Vector3(
            sineWave,
            cosWave * 0.3f,
            sineWave * 0.5f
        );
        
        // 最終的な速度を計算
        Vector3 targetVelocity = (flowDirection * flowSpeed + waveOffset + drift);
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * 2f);
        
        // 位置の更新
        transform.position += currentVelocity * Time.deltaTime;
        
        // 穏やかな回転（流れに沿った動き）
        if (currentVelocity.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }
    }
    
    void UpdateFade(float elapsed)
    {
        float fadeStart = lifetime * 0.7f;
        if (elapsed > fadeStart)
        {
            float fadeProgress = (elapsed - fadeStart) / (lifetime - fadeStart);
            Color color = trailRenderer.material.color;
            color.a = Mathf.Lerp(color.a, 0, fadeProgress);
            trailRenderer.material.color = color;
        }
    }
    
    public void SetFlowerSettings(float interval, int count, ColorPalette palette)
    {
        shouldSpawnFlowers = true;
        flowerSpawnInterval = interval;
        flowersPerVine = count;
        flowerColorPalette = palette;
    }
    
    void SpawnFlower()
    {
        if (manager != null)
        {
            // 現在の位置に花を咲かせる
            Vector3 flowerPosition = transform.position;
            
            // 少しランダムなオフセットを加える
            flowerPosition += new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.3f, 0.3f)
            );
            
            manager.SpawnFlowerAt(flowerPosition);
        }
    }
}