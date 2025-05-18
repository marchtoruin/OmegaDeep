using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CompositeCollider2D))]
public class ShadowCaster2DTileMap : MonoBehaviour
{

    [Header("Shadow Caster Settings")]
    [SerializeField]
    private bool castsShadows = true;
    [SerializeField]
    private bool selfShadows = true;
    [SerializeField]
    private string[] targetSortingLayerNames = { "Default" };

    private CompositeCollider2D tilemapCollider;

    static readonly FieldInfo meshField = typeof(ShadowCaster2D).GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo applyToSortingLayersField = typeof(ShadowCaster2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
    
    static readonly MethodInfo generateShadowMeshMethod = typeof(ShadowCaster2D)
                                    .Assembly
                                    .GetType("UnityEngine.Rendering.Universal.ShadowUtility")
                                    .GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);
    public void Generate()
    {
        DestroyAllChildren();

        tilemapCollider = GetComponent<CompositeCollider2D>();

        List<int> layerIds = new List<int>();
        foreach(string layerName in targetSortingLayerNames)
        {
            int layerId = SortingLayer.NameToID(layerName);
            if (layerId != 0)
            {
                layerIds.Add(layerId);
            }
            else
            {
                Debug.LogWarning($"[ShadowCaster2DTileMap] Sorting Layer '{layerName}' not found. Skipping.", this);
            }
        }
        int[] applyToSortLayersArray = layerIds.ToArray();

        if (applyToSortingLayersField == null)
        {
            Debug.LogError("[ShadowCaster2DTileMap] Reflection failed: Could not find 'm_ApplyToSortingLayers' field. Target layers cannot be set.", this);
        }

        for (int i = 0; i < tilemapCollider.pathCount; i++)
        {
            Vector2[] pathVertices = new Vector2[tilemapCollider.GetPathPointCount(i)];
            tilemapCollider.GetPath(i, pathVertices);
            GameObject shadowCaster = new GameObject("GeneratedShadowCaster_" + i);
            shadowCaster.transform.parent = gameObject.transform;
            ShadowCaster2D shadowCasterComponent = shadowCaster.AddComponent<ShadowCaster2D>();
            
            shadowCasterComponent.castsShadows = this.castsShadows;
            shadowCasterComponent.selfShadows = this.selfShadows;

            Vector3[] testPath = new Vector3[pathVertices.Length];
            for (int j = 0; j < pathVertices.Length; j++)
            {
                testPath[j] = pathVertices[j];
            }

            shapePathField.SetValue(shadowCasterComponent, testPath);
            shapePathHashField.SetValue(shadowCasterComponent, Random.Range(int.MinValue, int.MaxValue));
            meshField.SetValue(shadowCasterComponent, new Mesh());
            generateShadowMeshMethod.Invoke(shadowCasterComponent, new object[] { meshField.GetValue(shadowCasterComponent), shapePathField.GetValue(shadowCasterComponent) });

            if (applyToSortingLayersField != null)
            {
                applyToSortingLayersField.SetValue(shadowCasterComponent, applyToSortLayersArray);
            }
        }
    }
    public void DestroyAllChildren()
    {

        var tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            DestroyImmediate(child.gameObject);
        }

    }

}