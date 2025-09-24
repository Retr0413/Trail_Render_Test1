using System.Collections;
using UnityEngine;

[System.Serializable]
public class SolarFlareSettings
{
    [Header("フレアの基本設定")]
    [Tooltip("フレアの本数")]
    public int beamCount = 100;
    
    [Tooltip("横幅の広がり")]
    public float spreadWidth = 50f;
    
    [Tooltip("開始Y座標")]
    public float bottomY = -15f;
    
    [Tooltip("最大高さ")]
    public float maxHeight = 20f;
    
    [Tooltip("TrailRendererの継続時間")]
    public float trailTime = 3f;
    
    [Header("動き設定")]
    [Tooltip("上昇速度")]
    public float riseSpeed = 5f;
    
    [Tooltip("速度の変動幅（0-1）")]
    public float speedVariation = 0.3f;
    
    [Header("波動設定")]
    [Tooltip("波の振幅（横方向の揺れ幅）")]
    public float waveAmplitude = 2f;
    
    [Tooltip("波の周波数")]
    public float waveFrequency = 1.5f;
    
    [Header("不規則性設定")]
    [Tooltip("不規則な動きの強さ")]
    public float irregularity = 1f;
    
    [Tooltip("ノイズの速度")]
    public float noiseSpeed = 0.3f;
    
    [Header("サイズ設定")]
    [Tooltip("開始幅")]
    public float startWidth = 0.05f;
    
    [Tooltip("終了幅")]
    public float endWidth = 0.005f;
    
    [Header("ビジュアル設定")]
    public ColorPalette colorPalette;
    public bool useAdditiveBlending = true;
    
    [Header("生成設定")]
    [Tooltip("生成間隔（秒）")]
    public float spawnInterval = 0.05f;
    
    [Tooltip("同時に流れるビームの本数制限")]
    public int maxConcurrentBeams = 150;
}

public class SolarFlareSystem : MonoBehaviour
{
    [SerializeField] private SolarFlareSettings settings;
    
    private FireBeam[] fireBeams;
    private float timeOffset = 0f;
    
    void Start()
    {
        if (settings == null)
        {
            settings = new SolarFlareSettings();
        }
        
        InitializeSystem();
        CreateFireBeams();
        StartCoroutine(StartBeamsWithDelay());
    }
    
    void InitializeSystem()
    {
        if (settings.colorPalette == null)
        {
            Debug.LogWarning("No ColorPalette assigned. Please assign a ColorPalette to the SolarFlareSettings.");
        }
    }
    
    void CreateFireBeams()
    {
        fireBeams = new FireBeam[settings.beamCount];
        
        float horizontalStep = settings.spreadWidth / settings.beamCount;
        float startX = -settings.spreadWidth / 2f;
        
        for (int i = 0; i < settings.beamCount; i++)
        {
            GameObject beamObject = new GameObject($"FireBeam_{i}");
            beamObject.transform.parent = transform;
            
            FireBeam beam = beamObject.AddComponent<FireBeam>();
            beam.Initialize(settings, this, i);
            
            fireBeams[i] = beam;
            
            // 横位置の設定
            float xPosition = startX + (i * horizontalStep);
            xPosition += Random.Range(-horizontalStep * 0.3f, horizontalStep * 0.3f);
            
            Vector3 startPosition = new Vector3(xPosition, settings.bottomY, 0f);
            beamObject.transform.position = startPosition;
        }
    }
    
    IEnumerator StartBeamsWithDelay()
    {
        // ビームを時差で開始
        for (int i = 0; i < fireBeams.Length; i++)
        {
            if (fireBeams[i] != null)
            {
                float xPosition = -settings.spreadWidth / 2f + (i * settings.spreadWidth / settings.beamCount);
                Vector3 startPos = new Vector3(xPosition, settings.bottomY, 0f);
                
                fireBeams[i].StartFlareFlow(startPos);
                
                // ランダムな遅延を追加
                float delay = settings.spawnInterval * Random.Range(0.5f, 1.5f);
                yield return new WaitForSeconds(delay);
            }
        }
    }
    
    void Update()
    {
        timeOffset += Time.deltaTime;
    }
    
    public void UpdateColorPalette(ColorPalette newPalette)
    {
        settings.colorPalette = newPalette;
        
        // 既存のビームを再初期化
        if (fireBeams != null)
        {
            for (int i = 0; i < fireBeams.Length; i++)
            {
                if (fireBeams[i] != null)
                {
                    fireBeams[i].Initialize(settings, this, i);
                }
            }
        }
    }
    
    public void RegenerateFlares()
    {
        // 既存のビームを削除
        if (fireBeams != null)
        {
            foreach (var beam in fireBeams)
            {
                if (beam != null && beam.gameObject != null)
                {
                    Destroy(beam.gameObject);
                }
            }
        }
        
        // 新しく生成
        CreateFireBeams();
        StartCoroutine(StartBeamsWithDelay());
    }
    
    void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        
        Gizmos.color = Color.yellow;
        
        // 底辺のライン
        Vector3 leftPoint = new Vector3(-settings.spreadWidth / 2f, settings.bottomY, 0f);
        Vector3 rightPoint = new Vector3(settings.spreadWidth / 2f, settings.bottomY, 0f);
        Gizmos.DrawLine(leftPoint, rightPoint);
        
        // 最大高さのライン
        Vector3 topLeft = new Vector3(-settings.spreadWidth / 2f, settings.maxHeight, 0f);
        Vector3 topRight = new Vector3(settings.spreadWidth / 2f, settings.maxHeight, 0f);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawLine(topLeft, topRight);
        
        // 垂直ガイドライン
        for (int i = 0; i <= 4; i++)
        {
            float x = -settings.spreadWidth / 2f + (settings.spreadWidth / 4f) * i;
            Vector3 bottom = new Vector3(x, settings.bottomY, 0f);
            Vector3 top = new Vector3(x, settings.maxHeight, 0f);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawLine(bottom, top);
        }
    }
}