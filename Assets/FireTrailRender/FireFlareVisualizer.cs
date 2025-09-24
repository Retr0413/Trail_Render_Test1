using UnityEngine;
using UnityEngine.Rendering;

public class FireFlareVisualizer : MonoBehaviour
{
    [Header("Post Processing Effects")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private Color glowTint = new Color(1f, 0.5f, 0.2f, 1f);
    
    [Header("Particle Effects")]
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private int particleCount = 100;
    [SerializeField] private float particleLifetime = 3f;
    [SerializeField] private float particleSpeed = 2f;
    [SerializeField] private float particleSize = 0.5f;
    
    private ParticleSystem particleSystem;
    
    void Start()
    {
        SetupParticleSystem();
    }
    
    void SetupParticleSystem()
    {
        if (!enableParticles) return;
        
        GameObject particleObj = new GameObject("FireParticles");
        particleObj.transform.parent = transform;
        particleObj.transform.localPosition = Vector3.zero;
        
        particleSystem = particleObj.AddComponent<ParticleSystem>();
        
        var main = particleSystem.main;
        main.loop = true;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.maxParticles = particleCount;
        
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.5f), 0.0f),
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.5f),
                new GradientColorKey(new Color(0.5f, 0.1f, 0.05f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.3f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = particleCount / particleLifetime;
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(1f, 3f);
        
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.SetFloat("_Mode", 3);
        renderer.material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        renderer.material.SetInt("_DstBlend", (int)BlendMode.One);
        renderer.material.SetInt("_ZWrite", 0);
        renderer.material.DisableKeyword("_ALPHATEST_ON");
        renderer.material.EnableKeyword("_ALPHABLEND_ON");
        renderer.material.renderQueue = 3000;
    }
    
    public void SetGlowSettings(bool enable, float intensity, Color tint)
    {
        enableGlow = enable;
        glowIntensity = intensity;
        glowTint = tint;
    }
    
    public void SetParticleSettings(bool enable, int count, float lifetime, float speed)
    {
        enableParticles = enable;
        particleCount = count;
        particleLifetime = lifetime;
        particleSpeed = speed;
        
        if (particleSystem != null)
        {
            var main = particleSystem.main;
            main.maxParticles = count;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            
            var emission = particleSystem.emission;
            emission.rateOverTime = count / lifetime;
        }
    }
}