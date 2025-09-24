using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class SolarFlareSceneSetup : MonoBehaviour
{
    [Header("Scene Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private Color backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
    [SerializeField] private float cameraDistance = 30f;
    [SerializeField] private Vector3 cameraRotation = new Vector3(15f, -30f, 0f);
    
    [Header("Flare System")]
    [SerializeField] private GameObject solarFlareSystemPrefab;
    private SolarFlareSystem currentFlareSystem;
    
    [Header("Camera Effects")]
    [SerializeField] private bool addCameraEffects = true;
    [SerializeField] private float bloomIntensity = 1.5f;
    [SerializeField] private float vignetteIntensity = 0.3f;
    
    void Start()
    {
        if (autoSetupOnStart && Application.isPlaying)
        {
            SetupScene();
        }
    }
    
    public void SetupScene()
    {
        SetupCamera();
        SetupLighting();
        SetupFlareSystem();
    }
    
    void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
        }
        
        mainCamera.backgroundColor = backgroundColor;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.fieldOfView = 60f;
        
        // 2D表示用のカメラ設定
        mainCamera.transform.position = new Vector3(0, 0f, -30f);
        mainCamera.transform.rotation = Quaternion.identity;
        
        mainCamera.renderingPath = RenderingPath.Forward;
        mainCamera.allowHDR = true;
        mainCamera.allowMSAA = true;
    }
    
    void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.1f, 0.05f, 0.02f);
        RenderSettings.ambientEquatorColor = new Color(0.15f, 0.07f, 0.03f);
        RenderSettings.ambientGroundColor = new Color(0.05f, 0.02f, 0.01f);
        RenderSettings.ambientIntensity = 0.5f;
        
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.05f, 0.02f, 0.01f, 1f);
        RenderSettings.fogDensity = 0.01f;
        
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = 0.2f;
                light.color = new Color(1f, 0.5f, 0.3f);
            }
        }
        
        if (lights.Length == 0)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 0.2f;
            dirLight.color = new Color(1f, 0.5f, 0.3f);
            dirLight.transform.rotation = Quaternion.Euler(30f, -45f, 0f);
        }
    }
    
    void SetupFlareSystem()
    {
        currentFlareSystem = FindObjectOfType<SolarFlareSystem>();
        
        if (currentFlareSystem == null)
        {
            GameObject flareSystemObj = new GameObject("Solar Flare System");
            flareSystemObj.transform.position = Vector3.zero;
            
            currentFlareSystem = flareSystemObj.AddComponent<SolarFlareSystem>();
            
            FireFlareVisualizer visualizer = flareSystemObj.AddComponent<FireFlareVisualizer>();
            visualizer.SetGlowSettings(true, 2f, new Color(1f, 0.5f, 0.2f, 1f));
            visualizer.SetParticleSettings(true, 200, 3f, 2f);
        }
    }
    
    [ContextMenu("Regenerate Flares")]
    public void RegenerateFlares()
    {
        if (currentFlareSystem != null)
        {
            currentFlareSystem.RegenerateFlares();
        }
        else
        {
            SetupFlareSystem();
        }
    }
    
    [ContextMenu("Reset Scene")]
    public void ResetScene()
    {
        SetupScene();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SolarFlareSceneSetup))]
public class SolarFlareSceneSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SolarFlareSceneSetup setup = (SolarFlareSceneSetup)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Controls", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Setup Scene"))
        {
            setup.SetupScene();
        }
        
        if (GUILayout.Button("Regenerate Flares"))
        {
            setup.RegenerateFlares();
        }
        
        if (GUILayout.Button("Reset Everything"))
        {
            setup.ResetScene();
        }
    }
}
#endif