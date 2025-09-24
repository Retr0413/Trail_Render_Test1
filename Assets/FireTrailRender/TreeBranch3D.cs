using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class TreeBranch3D : MonoBehaviour
{
    private TrailRenderer trailRenderer;
    private TreeSettings3D settings;

    private Vector3 startPosition;
    private Vector3 growthDirection;
    private float branchLength;
    private float branchWidth;
    private int generation;

    private bool isGrowing = false;
    private float growthProgress = 0f;
    private Vector3 currentEndPosition;

    // 色変化用
    private float colorChangeTimer = 0f;
    private int currentColorIndex = 0;

    // 揺れアニメーション用
    private float swayOffset;
    private Vector3 swayAxis;

    public float GetLength() => branchLength;
    public float GetWidth() => branchWidth;
    public Vector3 GetDirection() => growthDirection;

    public void Initialize(TreeSettings3D settings, Vector3 startPos, Vector3 direction, float length, float width, int gen)
    {
        this.settings = settings;
        this.startPosition = startPos;
        this.growthDirection = direction.normalized;
        this.branchLength = length;
        this.branchWidth = width;
        this.generation = gen;

        transform.position = startPosition;
        swayOffset = Random.Range(0f, Mathf.PI * 2f);
        swayAxis = Random.insideUnitSphere.normalized;

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
        trailRenderer.alignment = LineAlignment.TransformZ;

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
        Material trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (settings.useAdditiveBlending)
        {
            trailMaterial.SetFloat("_Surface", 1);
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        else
        {
            trailMaterial.SetFloat("_Surface", 1);
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        trailMaterial.SetInt("_ZWrite", 1);
        trailMaterial.EnableKeyword("_ALPHABLEND_ON");
        trailMaterial.renderQueue = 3000;

        // 影の設定
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        trailRenderer.receiveShadows = true;

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

            // 3Dでの枝の成長
            Vector3 endPosition = startPosition + growthDirection * branchLength;

            // 自然な3D曲がりを追加
            if (generation > 0)
            {
                // 重力の影響で少し下に曲がる
                float gravityEffect = Mathf.Sin(curvedProgress * Mathf.PI) * 0.1f * generation;
                endPosition.y -= gravityEffect * branchLength;

                // 螺旋状の成長パターン
                float spiralRadius = 0.1f * generation * Mathf.Sin(curvedProgress * Mathf.PI);
                float spiralAngle = curvedProgress * Mathf.PI * 2f + swayOffset;
                Vector3 perpendicular = Vector3.Cross(growthDirection, Vector3.up).normalized;
                Vector3 binormal = Vector3.Cross(growthDirection, perpendicular).normalized;

                endPosition += perpendicular * (Mathf.Cos(spiralAngle) * spiralRadius * branchLength);
                endPosition += binormal * (Mathf.Sin(spiralAngle) * spiralRadius * branchLength);

                // 風の影響をシミュレート
                Vector3 windEffect = new Vector3(
                    Mathf.PerlinNoise(Time.time * 0.5f + swayOffset, 0) - 0.5f,
                    0,
                    Mathf.PerlinNoise(0, Time.time * 0.5f + swayOffset) - 0.5f
                ) * 0.05f * generation;
                endPosition += windEffect * branchLength;
            }

            currentEndPosition = Vector3.Lerp(startPosition, endPosition, curvedProgress);

            // TrailRendererの位置を更新
            transform.position = currentEndPosition;

            // 成長に応じて太さを調整
            float widthMultiplier = 1f - (curvedProgress * 0.2f * (1f + generation * 0.1f));
            trailRenderer.startWidth = branchWidth * widthMultiplier;

            yield return new WaitForSeconds(0.01f);
        }

        isGrowing = false;
    }

    public Vector3 GetBranchPoint(float t)
    {
        // 枝上の任意の点を取得（0～1の範囲）
        t = Mathf.Clamp01(t);

        if (isGrowing)
        {
            return Vector3.Lerp(startPosition, currentEndPosition, t);
        }
        else
        {
            Vector3 endPosition = startPosition + growthDirection * branchLength;

            // 3D曲がりも考慮
            if (generation > 0)
            {
                float gravityEffect = Mathf.Sin(t * Mathf.PI) * 0.1f * generation;
                endPosition.y -= gravityEffect * branchLength;

                float spiralRadius = 0.1f * generation * Mathf.Sin(t * Mathf.PI);
                float spiralAngle = t * Mathf.PI * 2f + swayOffset;
                Vector3 perpendicular = Vector3.Cross(growthDirection, Vector3.up).normalized;
                Vector3 binormal = Vector3.Cross(growthDirection, perpendicular).normalized;

                endPosition += perpendicular * (Mathf.Cos(spiralAngle) * spiralRadius * branchLength);
                endPosition += binormal * (Mathf.Sin(spiralAngle) * spiralRadius * branchLength);
            }

            return Vector3.Lerp(startPosition, endPosition, t);
        }
    }

    public void UpdateSway(float time, float strength)
    {
        if (generation == 0) return; // 幹は揺れない

        // 3D空間での風による揺れ
        float swayAmount = Mathf.Sin(time + swayOffset) * strength;
        swayAmount += Mathf.Sin(time * 2.3f + swayOffset * 0.5f) * strength * 0.3f;
        swayAmount += Mathf.Sin(time * 3.7f + swayOffset * 0.3f) * strength * 0.15f;

        // 世代が進むほどよく揺れる
        float generationMultiplier = 1f + (generation * 0.5f);
        swayAmount *= generationMultiplier;

        // 3D揺れベクトル（複数軸で揺れる）
        Vector3 swayVector = new Vector3(
            Mathf.Sin(time * 1.1f + swayOffset) * swayAmount,
            Mathf.Sin(time * 0.7f + swayOffset * 1.2f) * swayAmount * 0.3f,
            Mathf.Cos(time * 0.9f + swayOffset * 0.8f) * swayAmount
        );

        // 枝の向きに対して垂直な揺れを追加
        Vector3 perpendicular = Vector3.Cross(growthDirection, Vector3.up).normalized;
        swayVector += perpendicular * Mathf.Sin(time * 1.5f + swayOffset) * swayAmount * 0.5f;

        if (!isGrowing)
        {
            transform.position = transform.position + swayVector * 0.1f;
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