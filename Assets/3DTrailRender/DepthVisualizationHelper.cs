using UnityEngine;
using System.Collections.Generic;

public class DepthVisualizationHelper : MonoBehaviour
{
    [Header("深度グリッド設定")]
    [SerializeField] private int gridLayers = 5;
    [SerializeField] private float layerSpacing = 2f;
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private bool animateGrid = true;
    
    private List<GameObject> gridPlanes = new List<GameObject>();
    
    void Start()
    {
        CreateDepthGrid();
    }
    
    void CreateDepthGrid()
    {
        for (int i = 0; i < gridLayers; i++)
        {
            float z = (i - gridLayers / 2) * layerSpacing;
            GameObject plane = CreateGridPlane(z);
            gridPlanes.Add(plane);
        }
    }
    
    GameObject CreateGridPlane(float zPosition)
    {
        GameObject plane = new GameObject($"GridPlane_{zPosition}");
        plane.transform.position = new Vector3(0, 0, zPosition);
        
        // グリッドラインを作成
        LineRenderer line = plane.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = gridColor;
        line.endColor = gridColor;
        line.startWidth = 0.01f;
        line.endWidth = 0.01f;
        
        // グリッドパターンを生成
        List<Vector3> points = new List<Vector3>();
        float size = 10f;
        int divisions = 20;
        
        for (int i = 0; i <= divisions; i++)
        {
            float t = (float)i / divisions;
            float coord = Mathf.Lerp(-size, size, t);
            
            // 横線
            points.Add(new Vector3(-size, coord, zPosition));
            points.Add(new Vector3(size, coord, zPosition));
            
            // 縦線
            points.Add(new Vector3(coord, -size, zPosition));
            points.Add(new Vector3(coord, size, zPosition));
        }
        
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        
        return plane;
    }
    
    void Update()
    {
        if (animateGrid)
        {
            // グリッドをゆっくり脈動させる
            float pulse = Mathf.Sin(Time.time * 0.5f) * 0.1f + 0.9f;
            foreach (var plane in gridPlanes)
            {
                LineRenderer line = plane.GetComponent<LineRenderer>();
                Color c = gridColor;
                c.a = gridColor.a * pulse;
                line.startColor = c;
                line.endColor = c;
            }
        }
    }
}
