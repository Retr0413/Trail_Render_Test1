using UnityEngine;

public class Trail3DController : MonoBehaviour
{
    private Trail3DManager manager;
    private TrailRenderer trailRenderer;
    private float startTime;
    private Vector3 startPosition;
    private Vector3 noiseOffset;

    [Header("2D川の流れ設定")]
    private float lifetime = 8f;
    private float flowSpeed = 3f;
    private float waveAmplitude = 1f;
    private float waveFrequency = 2f;
    private float secondaryWaveAmplitude = 0.3f;
    private float secondaryWaveFrequency = 5f;
    private float noiseStrength = 0.5f;
    private float yPosition = 0f;
    private float flowPhase;
    private float secondaryPhase;
    
    [Header("花の設定")]
    private bool shouldSpawnFlowers = false;
    private float flowerSpawnInterval = 1.5f;
    private int flowersPerVine = 3;
    private ColorPalette flowerColorPalette;
    private float nextFlowerTime = 0f;
    private int flowersSpawned = 0; 
    
    public void Initialize(Trail3DManager mgr, float life, float yPos)
    {
        manager = mgr;
        trailRenderer = GetComponent<TrailRenderer>();
        lifetime = life;
        yPosition = yPos;

        noiseOffset = new Vector3(
            Random.Range(-100f, 100f),
            Random.Range(-100f, 100f),
            0
        );

        flowSpeed = Random.Range(2.5f, 4f);
        waveAmplitude = Random.Range(0.8f, 1.5f);
        waveFrequency = Random.Range(1.5f, 3f);
        secondaryWaveAmplitude = Random.Range(0.2f, 0.5f);
        secondaryWaveFrequency = Random.Range(4f, 7f);
        noiseStrength = Random.Range(0.3f, 0.8f);
    }
    
    public void StartTrail(Vector3 position)
    {
        startTime = Time.time;
        position.y = yPosition;
        position.x = -10f;
        startPosition = position;
        transform.position = position;
        trailRenderer.Clear();
        trailRenderer.emitting = true;

        flowPhase = Random.Range(0f, Mathf.PI * 2f);
        secondaryPhase = Random.Range(0f, Mathf.PI * 2f);

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
        
        Update2DRiverMovement();
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
    
    void Update2DRiverMovement()
    {
        float elapsed = Time.time - startTime;
        float time = Time.time;

        flowPhase += waveFrequency * Time.deltaTime;
        secondaryPhase += secondaryWaveFrequency * Time.deltaTime;

        float primaryWave = Mathf.Sin(flowPhase) * waveAmplitude;
        float secondaryWave = Mathf.Sin(secondaryPhase) * secondaryWaveAmplitude;

        float noise = Mathf.PerlinNoise(time * 0.5f + noiseOffset.x, noiseOffset.y) - 0.5f;
        float smoothNoise = Mathf.PerlinNoise(time * 0.3f + noiseOffset.x * 2f, noiseOffset.y * 2f) - 0.5f;

        float zOffset = primaryWave + secondaryWave + (noise * noiseStrength);

        float speedVariation = 1f + (smoothNoise * 0.3f);
        float currentFlowSpeed = flowSpeed * speedVariation;

        Vector3 newPosition = transform.position;
        newPosition.x += currentFlowSpeed * Time.deltaTime;
        newPosition.z = startPosition.z + zOffset;
        newPosition.y = yPosition + (Mathf.Sin(time * 3f) * 0.05f);

        transform.position = newPosition;

        if (newPosition.x > 15f)
        {
            manager.ReturnTrailToPool(gameObject);
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