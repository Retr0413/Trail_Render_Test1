using UnityEngine;

public class TrailController : MonoBehaviour
{
    private RiverSettings settings;
    private TrailArtManager manager;
    private TrailRenderer trailRenderer;
    private float startTime;
    private Vector3 startPosition;
    private float flowPhase;
    private float noiseOffset;
    private bool isActive;

    // マウス撹乱用
    private Vector3 disturbanceCenter;
    private float disturbanceRadius = 3f;
    private float disturbanceStrength = 5f;
    private float vibrationFrequency = 15f;
    private bool isDisturbed = false;
    private float disturbanceTime = 0f;

    public void Initialize(RiverSettings settings, TrailArtManager manager)
    {
        this.settings = settings;
        this.manager = manager;
        this.trailRenderer = GetComponent<TrailRenderer>();

        noiseOffset = Random.Range(0f, 100f);
        flowPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    public void StartRiverFlow(Vector3 position, float yPos)
    {
        startTime = Time.time;
        position.y = yPos;
        position.x = settings.startX;
        startPosition = position;
        transform.position = position;

        isActive = true;
        trailRenderer.Clear();
        trailRenderer.emitting = true;
    }

    void Update()
    {
        if (!isActive) return;

        float elapsed = Time.time - startTime;

        if (transform.position.x > settings.endX || elapsed > settings.riverLength / settings.flowSpeed)
        {
            manager.ReturnTrailToPool(this);
            isActive = false;
            return;
        }

        UpdateRiverMovement();
        UpdateFade(elapsed);
    }

    void UpdateRiverMovement()
    {
        float time = Time.time;

        flowPhase += settings.waveFrequency * Time.deltaTime;

        float primaryWave = Mathf.Sin(flowPhase) * settings.waveAmplitude;
        float secondaryWave = Mathf.Sin(flowPhase * 2.3f) * settings.waveAmplitude * 0.3f;
        float tertiaryWave = Mathf.Cos(flowPhase * 3.7f) * settings.waveAmplitude * 0.2f;

        float noise1 = Mathf.PerlinNoise(time * settings.noiseSpeed + noiseOffset, 0f) - 0.5f;
        float noise2 = Mathf.PerlinNoise(time * settings.noiseSpeed * 0.7f + noiseOffset * 2f, 100f) - 0.5f;
        float noise3 = Mathf.PerlinNoise(time * settings.noiseSpeed * 1.3f + noiseOffset * 3f, 200f) - 0.5f;

        float combinedNoise = (noise1 + noise2 * 0.5f + noise3 * 0.3f) * settings.irregularity;

        float zOffset = primaryWave + secondaryWave + tertiaryWave + combinedNoise;

        float speedVariation = 1f + (noise1 * settings.speedVariation);
        float currentSpeed = settings.flowSpeed * speedVariation;

        Vector3 newPosition = transform.position;
        newPosition.x += currentSpeed * Time.deltaTime;
        newPosition.z = startPosition.z + zOffset;

        float yWave = Mathf.Sin(time * 4f + flowPhase) * 0.05f;
        newPosition.y = startPosition.y + yWave;

        // マウス撹乱処理
        if (isDisturbed)
        {
            Vector3 toDisturbance = newPosition - disturbanceCenter;
            float distance = toDisturbance.magnitude;

            if (distance < disturbanceRadius)
            {
                disturbanceTime += Time.deltaTime;

                // 距離に応じた撹乱の強さ
                float disturbForce = 1f - (distance / disturbanceRadius);
                disturbForce = Mathf.Pow(disturbForce, 1.5f);

                // 激しい振動を生成
                float vibration1 = Mathf.Sin(disturbanceTime * vibrationFrequency) * disturbForce;
                float vibration2 = Mathf.Cos(disturbanceTime * vibrationFrequency * 1.7f) * disturbForce * 0.7f;
                float vibration3 = Mathf.Sin(disturbanceTime * vibrationFrequency * 2.3f) * disturbForce * 0.5f;

                // ランダムな撹乱
                float randomDisturb = Random.Range(-1f, 1f) * disturbForce;

                // Z軸（横方向）に大きく振れる
                float lateralDisturbance = (vibration1 + vibration2 + vibration3 + randomDisturb) * disturbanceStrength;
                newPosition.z += lateralDisturbance * Time.deltaTime;

                // Y軸にも少し振動
                float verticalDisturbance = Mathf.Sin(disturbanceTime * vibrationFrequency * 3f) * disturbForce * disturbanceStrength * 0.3f;
                newPosition.y += verticalDisturbance * Time.deltaTime;

                // 流れの速度も乱す
                float speedDisturbance = Random.Range(0.5f, 1.5f) * disturbForce;
                newPosition.x += (speedDisturbance - 1f) * settings.flowSpeed * Time.deltaTime * 0.5f;

                // カオスな螺旋運動
                float spiral = disturbanceTime * 5f;
                newPosition.z += Mathf.Sin(spiral) * disturbForce * 0.3f;
                newPosition.y += Mathf.Cos(spiral) * disturbForce * 0.2f;
            }
        }

        transform.position = newPosition;
    }

    public void SetDisturbance(Vector3 center, float radius, float strength, float frequency)
    {
        disturbanceCenter = center;
        disturbanceRadius = radius;
        disturbanceStrength = strength;
        vibrationFrequency = frequency;
        isDisturbed = true;
    }

    public void ClearDisturbance()
    {
        isDisturbed = false;
        disturbanceTime = 0f;
    }

    void UpdateFade(float elapsed)
    {
        float fadeStart = (settings.riverLength / settings.flowSpeed) * 0.7f;

        if (elapsed > fadeStart)
        {
            float maxTime = settings.riverLength / settings.flowSpeed;
            float fadeProgress = (elapsed - fadeStart) / (maxTime - fadeStart);

            Color color = trailRenderer.material.color;
            color.a = Mathf.Lerp(1f, 0f, fadeProgress);
            trailRenderer.material.color = color;
        }
    }

    public void StopRiver()
    {
        isActive = false;
        trailRenderer.emitting = false;
    }
}