using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class TreeBranch : MonoBehaviour
{
    private TrailRenderer trailRenderer;
    private TreeSettings settings;
    
    private Vector2 startPosition;
    private Vector2 growthDirection;
    private float branchLength;
    private float branchWidth;
    private int generation;
    
    private bool isGrowing = false;
    private float growthProgress = 0f;
    private Vector2 currentEndPosition;
    
    // 色変化用
    private float colorChangeTimer = 0f;
    private int currentColorIndex = 0;
    
    // 揺れアニメーション用
    private float swayOffset;
    
    public float GetLength() => branchLength;
    public float GetWidth() => branchWidth;
    public Vector2 GetDirection() => growthDirection;
    
    public void Initialize(TreeSettings settings, Vector2 startPos, Vector2 direction, float length, float width, int gen)
    {
        this.settings = settings;
        this.startPosition = startPos;
        this.growthDirection = direction.normalized;
        this.branchLength = length;
        this.branchWidth = width;
        this.generation = gen;
        
        transform.position = startPosition;
        swayOffset = Random.Range(0f, Mathf.PI * 2f);
        
        SetupTrailRenderer();
    }
    
    void SetupTrailRenderer()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }
        
        // トレイル基本設定
        trailRenderer.time = settings.trailTime;
        trailRenderer.startWidth = branchWidth;
        trailRenderer.endWidth = branchWidth * 0.1f;
        trailRenderer.minVertexDistance = settings.minVertexDistance;
        trailRenderer.autodestruct = false;
        trailRenderer.emitting = false;
        
        // 滑らかさの設定
        trailRenderer.numCornerVertices = 12;
        trailRenderer.numCapVertices = 12;
        trailRenderer.textureMode = LineTextureMode.Stretch;
        trailRenderer.alignment = LineAlignment.View;
        
        // 幅のカーブ（自然な先細り）
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 1f);
        widthCurve.AddKey(0.2f, 0.95f);
        widthCurve.AddKey(0.5f, 0.8f);
        widthCurve.AddKey(0.8f, 0.5f);
        widthCurve.AddKey(1f, 0.2f);
        trailRenderer.widthCurve = widthCurve;
        
        // 初期カラー設定
        UpdateColor();
        
        // マテリアル設定
        Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
        if (settings.useAdditiveBlending)
        {
            trailMaterial.SetFloat("_Mode", 3);
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        else
        {
            trailMaterial.SetFloat("_Mode", 3);
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        trailMaterial.SetInt("_ZWrite", 0);
        trailMaterial.DisableKeyword("_ALPHATEST_ON");
        trailMaterial.EnableKeyword("_ALPHABLEND_ON");
        trailMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        trailMaterial.renderQueue = 3000;
        
        trailRenderer.material = trailMaterial;
    }
    
    void UpdateColor()
    {
        if (settings.colorPalette != null && settings.colorPalette.gradients != null && settings.colorPalette.gradients.Length > 0)
        {
            // ランダムにグラデーションを選択
            currentColorIndex = Random.Range(0, settings.colorPalette.gradients.Length);
            trailRenderer.colorGradient = settings.colorPalette.gradients[currentColorIndex];
        }
        else
        {
            // デフォルトカラー（木の色）
            Gradient gradient = new Gradient();
            Color baseColor = Color.Lerp(
                new Color(0.4f, 0.25f, 0.1f),  // 茶色
                new Color(0.2f, 0.5f, 0.2f),   // 緑
                generation / 4f
            );
            
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(baseColor * 1.2f, 0f),
                    new GradientColorKey(baseColor, 0.5f),
                    new GradientColorKey(baseColor * 0.7f, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0.3f, 1f)
                }
            );
            trailRenderer.colorGradient = gradient;
        }
    }
    
    public void StartGrowth()
    {
        if (isGrowing) return;
        
        isGrowing = true;
        growthProgress = 0f;
        colorChangeTimer = 0f;
        trailRenderer.Clear();
        trailRenderer.emitting = true;
        
        StartCoroutine(GrowBranch());
    }
    
    IEnumerator GrowBranch()
    {
        float growthDuration = branchLength / settings.growthSpeed;
        
        while (growthProgress < 1f)
        {
            growthProgress += Time.deltaTime / growthDuration;
            growthProgress = Mathf.Clamp01(growthProgress);
            
            // 色の変化
            colorChangeTimer += Time.deltaTime;
            if (colorChangeTimer >= 1f / settings.colorChangeSpeed)
            {
                UpdateColor();
                colorChangeTimer = 0f;
            }
            
            // 成長カーブ
            float curvedProgress = Mathf.SmoothStep(0, 1, growthProgress);
            
            // 2Dでの枝の成長
            Vector2 endPosition = startPosition + growthDirection * branchLength;
            
            // 自然な曲がりを追加（Y字型の分岐を表現）
            if (generation > 0)
            {
                // 重力の影響で少し下に曲がる
                float gravityEffect = Mathf.Sin(curvedProgress * Mathf.PI) * 0.1f * generation;
                endPosition.y -= gravityEffect * branchLength;
                
                // 横方向にも少し曲げる
                float sideBend = Mathf.Sin(curvedProgress * Mathf.PI * 1.5f + swayOffset) * 0.2f;
                endPosition.x += sideBend * branchLength * 0.3f;
            }
            
            currentEndPosition = Vector2.Lerp(startPosition, endPosition, curvedProgress);
            
            // TrailRendererの位置を更新
            transform.position = currentEndPosition;
            
            // 成長に応じて太さを調整
            float widthMultiplier = 1f - (curvedProgress * 0.2f * (1f + generation * 0.1f));
            trailRenderer.startWidth = branchWidth * widthMultiplier;
            
            yield return new WaitForSeconds(0.01f);
        }
        
        isGrowing = false;
    }
    
    public Vector2 GetBranchPoint(float t)
    {
        // 枝上の任意の点を取得（0～1の範囲）
        t = Mathf.Clamp01(t);
        
        if (isGrowing)
        {
            return Vector2.Lerp(startPosition, currentEndPosition, t);
        }
        else
        {
            Vector2 endPosition = startPosition + growthDirection * branchLength;
            
            // 曲がりも考慮
            if (generation > 0)
            {
                float gravityEffect = Mathf.Sin(t * Mathf.PI) * 0.1f * generation;
                endPosition.y -= gravityEffect * branchLength;
                
                float sideBend = Mathf.Sin(t * Mathf.PI * 1.5f + swayOffset) * 0.2f;
                endPosition.x += sideBend * branchLength * 0.3f;
            }
            
            return Vector2.Lerp(startPosition, endPosition, t);
        }
    }
    
    public void UpdateSway(float time, float strength)
    {
        if (generation == 0) return; // 幹は揺れない
        
        // 風による2D揺れ
        float swayAmount = Mathf.Sin(time + swayOffset) * strength;
        swayAmount += Mathf.Sin(time * 2.3f + swayOffset * 0.5f) * strength * 0.3f;
        
        // 世代が進むほどよく揺れる
        float generationMultiplier = 1f + (generation * 0.5f);
        swayAmount *= generationMultiplier;
        
        // X軸方向のみの揺れ（2D）
        Vector2 swayVector = new Vector2(swayAmount, 0);
        
        if (!isGrowing)
        {
            transform.position = (Vector2)transform.position + swayVector * 0.1f;
        }
    }
    
    void OnDestroy()
    {
        if (trailRenderer != null && trailRenderer.material != null)
        {
            Destroy(trailRenderer.material);
        }
    }
}