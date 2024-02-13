using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshProcess;
using UnityEditor;

public class ToolConvexSplitter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public List<GameObject> split(GameObject tool, Material material)
    {
        List<GameObject> colliderObjs = new List<GameObject>();
        // AgentScript.body = this.GetComponent<Rigidbody>();
        MeshFilter mf =  tool.transform.GetComponentsInChildren<MeshFilter>()[0];
        tool = tool.transform.gameObject;
        VHACD decomposer = tool.transform.GetComponent<VHACD>();
        if (decomposer == null)
        {
            decomposer = tool.gameObject.AddComponent<VHACD>();
        }
        List<Mesh> colliderMeshes = decomposer.GenerateConvexMeshes(mf.sharedMesh);
        Debug.Log(colliderMeshes.Count);
        for (int i = 0; i < colliderMeshes.Count; i++)
        {
            GameObject obj = new GameObject($"convex mesh {i}");
            colliderObjs.Add(obj);
            MeshFilter mfTemp = obj.AddComponent<MeshFilter>();
            mfTemp.sharedMesh = colliderMeshes[i];
            // Debug.Log(colliderMeshes[i].vertices.Length);
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.material = material;
            renderer.enabled = false;

            MeshCollider current = obj.AddComponent<MeshCollider>();
            current.sharedMesh = colliderMeshes[i];
            current.convex = true;
            current.isTrigger=true;
            CollisionCheck colScript = obj.AddComponent<CollisionCheck>();
            
            // Debug.Log(colliderMeshes[i].vertices.Length);
            // Debug.Log(colliderMeshes[i].triangles.Length);
            // Debug.Log(colliderMeshes[i].normals.Length);
            // MeshUtility.Optimize(colliderMeshes[i]);
            if (!AssetDatabase.IsValidFolder($"Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder($"Assets/Resources/ToolData"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "ToolData");
            }
            if (!AssetDatabase.IsValidFolder($"Assets/Resources/ToolData/ToolConvexSplit"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/ToolData", "ToolConvexSplit");
            }
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo("./Assets/Resources/ToolData/ToolConvexSplit");
            string path = $"Assets/Resources/ToolData/ToolConvexSplit/convex_mesh_{(int) (dir.GetFiles("*.asset").Length+1)}.asset";
            Debug.Log(path);
            AssetDatabase.CreateAsset(colliderMeshes[i], path);
            AssetDatabase.SaveAssets();
        }
        Destroy(decomposer);
        return colliderObjs;
    }
}
