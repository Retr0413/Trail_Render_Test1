using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "TrailArt/ColorPalette")]
public class ColorPalette : ScriptableObject
{
    public Gradient[] gradients;
    
    public Gradient GetRandomGradient()
    {
        if (gradients == null || gradients.Length == 0)
            return new Gradient();
        
        return gradients[Random.Range(0, gradients.Length)];
    }
    
    public void SetupCoolColors()
    {
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
        gradients = new Gradient[3];
        
        // 赤からオレンジ
        gradients[0] = CreateGradient(
            new Color(1f, 0, 0.2f),
            new Color(1f, 0.5f, 0),
            new Color(1f, 0.8f, 0)
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