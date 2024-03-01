using UnityEngine;
using System.Linq;
using System.Collections.Generic;



public class MeshSplitter : MonoBehaviour
{
    public GameObject meshObj;
    public string materialPath = "Materials/defaultMaterial";

    // private List<string> materialNames = new List<string> {"defaultMaterial"};
    // private List<string> materialNames = new List<string> {"blue", "yellow"};
    private List<Material> mats = new List<Material>();

    private Material horizontalMat;
    private Material verticalMat;

    void Start()
    {
        // foreach (string matName in materialNames)
        // {
        //     mats.Add(Resources.Load("Materials/" + matName, typeof(Material)) as Material);
        // }
        horizontalMat = Resources.Load("Materials/blue", typeof(Material)) as Material;
        verticalMat = Resources.Load("Materials/yellow", typeof(Material)) as Material;
        // // Assuming the MeshFilter component is attached to the GameObject
        // MeshFilter meshFilter = meshObj.GetComponent<MeshFilter>();

        // if (meshFilter != null)
        // {
        //     Mesh originalMesh = meshFilter.mesh;

        //     // Create a new mesh for the decomposed parts
        //     GameObject rootObj = SplitMesh(originalMesh);
        //     rootObj.transform.localPosition = new Vector3(36.77f, -0.1f, 43.5f);
        //     rootObj.transform.localEulerAngles = new Vector3(-90f,0f,0f);
        //     rootObj.transform.localScale = Vector3.one * 0.01f;
        //     // // Do something with the decomposed meshes, for example, create new GameObjects
        //     // for (int i = 0; i < decomposedMeshes.Length; i++)
        //     // {
        //     //     GameObject newObject = new GameObject($"DecomposedPart_{i}");
        //     //     newObject.AddComponent<MeshFilter>().mesh = decomposedMeshes[i];
        //     //     newObject.AddComponent<MeshRenderer>();
        //     //     // You may want to set materials, shaders, etc., on the MeshRenderer
        //     // }
        // }
    }

    public GameObject SplitGameObject(GameObject go)
    {
        MeshFilter meshFilter = go.GetComponentsInChildren<MeshFilter>()[0];
        return SplitMesh(meshFilter.mesh);
    }

    public GameObject SplitMesh(Mesh originalMesh)
    {
        GameObject root = new GameObject("MeshDecomp");
        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector3[] normals = originalMesh.normals;
        // Debug.Log($"vertices : {vertices.Length}");
        // Debug.Log($"triangles : {triangles.Length}");
        // Debug.Log($"normals : {normals.Length}");
        // Create a new array to store the decomposed meshes
        List<Mesh> decomposedMeshes = new List<Mesh>();

        // Iterate through the triangles to get faces
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get the three vertices of the face
            Vector3 vertex1 = vertices[triangles[i]];
            Vector3 vertex2 = vertices[triangles[i + 1]];
            Vector3 vertex3 = vertices[triangles[i + 2]];

            Vector3 v1 = vertex2 - vertex1;
            Vector3 v2 = vertex3 - vertex1;
            float triArea = triangleArea(v1,v2); 
            if (triArea < 0.01f)
            {
                // triangle is empty
                continue;
            }
            if (triArea < 1.0f)
            {
                Debug.Log($"This is triangle area : {triArea}");
            }
            GameObject newObject = new GameObject($"DecomposedPart_{i}");
            newObject.transform.SetParent(root.transform);
            newObject.tag = "obstacle";
            MeshFilter mf = newObject.AddComponent<MeshFilter>(); //.mesh = decomposedMeshes[i];
            MeshRenderer renderer = newObject.AddComponent<MeshRenderer>();
            // Calculate the cross product (perpendicular to the plane)
            Vector3 normal = Vector3.Cross(v1, v2).normalized;
            MeshOrientation orient = newObject.AddComponent<MeshOrientation>();
            orient.normal = normal;
            // Debug.Log(Mathf.Abs(Vector3.Dot(normal,new Vector3(0f,0f,1f))));
            if (Mathf.Abs(Vector3.Dot(normal,new Vector3(0f,0f,1f))) >=0.9f)
            {
                // normal points roughly upwards, color it blue
                orient.originalMaterial = horizontalMat;
                renderer.material = horizontalMat;
            }
            else
            {
                // non horizontal obj, so even curved ones are colored yellow
                orient.originalMaterial = verticalMat;
                renderer.material = verticalMat;         
            }
            while ((normal*0.1f).magnitude > 0.1f)
            {
                normal = normal *0.1f;
            }
            // add "depth" to triangle to allow unitys physix engine to create a concave collider
            // https://www.reddit.com/r/Unity3D/comments/p9zar9/a_single_triangle_on_a_mesh_collider_is_not_convex/
            Vector3 extraVert = (vertex1+vertex2+vertex3)/3 + normal;

            // Create a new mesh for each decomposed part
            Mesh decomposedMesh = new Mesh();
            decomposedMesh.vertices = new Vector3[]{vertex1,vertex2,vertex3,extraVert};
            decomposedMesh.uv = originalMesh.uv;
            decomposedMesh.triangles = new int[]{0,1,2};

            // Assign the decomposed mesh to the array
            decomposedMeshes.Add(decomposedMesh);
            mf.mesh = decomposedMesh;
            MeshCollider mc = newObject.AddComponent<MeshCollider>(); 
            mc.convex=true;
        }
        return root;
    }

    public static float triangleArea(Vector3 side1, Vector3 side2)
    {
        // The area of the triangle is half the magnitude of the cross product of two sides
        return 0.5f * Vector3.Cross(side1, side2).magnitude;
    }
}