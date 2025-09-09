using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "TrailArt/ColorPalette")]
public class ColorPalette : ScriptableObject
{
    [Header("パレット情報")]
    public string paletteName = "Default";
    public Gradient[] gradients;
    
    [Header("3D深度用設定")]
    public bool useForDepth = false;
    public float depthRangeMin = -10f;
    public float depthRangeMax = 10f;
    
    public Gradient GetRandomGradient()
    {
        if (gradients == null || gradients.Length == 0)
            return new Gradient();
        
        return gradients[Random.Range(0, gradients.Length)];
    }
    
    public Gradient GetGradientByIndex(int index)
    {
        if (gradients == null || gradients.Length == 0)
            return new Gradient();
        
        index = Mathf.Clamp(index, 0, gradients.Length - 1);
        return gradients[index];
    }
    
    public void SetupCoolColors()
    {
        paletteName = "Cool";
        gradients = new Gradient[3];
        
        // 青から紫
        gradients[0] = CreateGradient(
            new Color(0, 0.7f, 1f),
            new Color(0.5f, 0, 1f),
            new Color(0.8f, 0, 0.8f)
        );
        
        // シアンから青
        gradients[1] = CreateGradient(
            new Color(0, 1f, 1f),
            new Color(0, 0.5f, 1f),
            new Color(0, 0, 0.8f)
        );
        
        // 緑から青
        gradients[2] = CreateGradient(
            new Color(0, 1f, 0.5f),
            new Color(0, 0.7f, 0.8f),
            new Color(0, 0.3f, 1f)
        );
    }
    
    public void SetupWarmColors()
    {
        paletteName = "Warm";
        gradients = new Gradient[3];
        
        // 赤からオレンジ
        gradients[0] = CreateGradient(
            new Color(1f, 0.3f, 0.1f),
            new Color(1f, 0.6f, 0.2f),
            new Color(1f, 0.9f, 0.3f)
        );
        
        // ピンクから黄色
        gradients[1] = CreateGradient(
            new Color(1f, 0.4f, 0.6f),
            new Color(1f, 0.7f, 0.3f),
            new Color(1f, 1f, 0)
        );
        
        // オレンジから赤
        gradients[2] = CreateGradient(
            new Color(1f, 0.6f, 0),
            new Color(1f, 0.3f, 0.1f),
            new Color(0.8f, 0, 0.2f)
        );
    }
    
    public void SetupNeonColors()
    {
        paletteName = "Neon";
        gradients = new Gradient[3];
        
        // ネオンピンク
        gradients[0] = CreateGradient(
            new Color(1f, 0, 1f),
            new Color(1f, 0.4f, 0.8f),
            new Color(0.8f, 0, 1f)
        );
        
        // ネオングリーン
        gradients[1] = CreateGradient(
            new Color(0, 1f, 0),
            new Color(0.5f, 1f, 0),
            new Color(1f, 1f, 0)
        );
        
        // ネオンブルー
        gradients[2] = CreateGradient(
            new Color(0, 1f, 1f),
            new Color(0, 0.5f, 1f),
            new Color(1f, 0, 1f)
        );
    }
    
    public void Setup3DDepthColors()
    {
        paletteName = "3D Depth";
        gradients = new Gradient[5];
        useForDepth = true;
        
        // 最前面: 赤系
        gradients[0] = CreateGradient(
            new Color(1f, 0.2f, 0.1f),
            new Color(1f, 0.4f, 0.2f),
            new Color(1f, 0.6f, 0.3f)
        );
        
        // 前面: オレンジ系
        gradients[1] = CreateGradient(
            new Color(1f, 0.5f, 0.1f),
            new Color(1f, 0.7f, 0.2f),
            new Color(1f, 0.9f, 0.3f)
        );
        
        // 中間: 緑系
        gradients[2] = CreateGradient(
            new Color(0.2f, 1f, 0.3f),
            new Color(0.4f, 0.9f, 0.5f),
            new Color(0.6f, 0.8f, 0.7f)
        );
        
        // 背面: 青系
        gradients[3] = CreateGradient(
            new Color(0.1f, 0.5f, 1f),
            new Color(0.2f, 0.3f, 0.9f),
            new Color(0.3f, 0.2f, 0.8f)
        );
        
        // 最背面: 紫系
        gradients[4] = CreateGradient(
            new Color(0.4f, 0.1f, 0.8f),
            new Color(0.3f, 0.05f, 0.6f),
            new Color(0.2f, 0.02f, 0.4f)
        );
    }
    
    Gradient CreateGradient(Color start, Color mid, Color end)
    {
        Gradient gradient = new Gradient();
        
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(start, 0f);
        colorKeys[1] = new GradientColorKey(mid, 0.5f);
        colorKeys[2] = new GradientColorKey(end, 1f);
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0] = new GradientAlphaKey(0.9f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.6f, 0.5f);
        alphaKeys[2] = new GradientAlphaKey(0f, 1f);
        
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }
}
