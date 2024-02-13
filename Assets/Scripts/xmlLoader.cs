// using System.Collections;
// using System.Xml;
// using System.Xml.Serialization;
// using System.Xml.Linq;
// using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using xmlClass;
using System;

public class seamEnv
{
    public Vector3 normal1;
    public Vector3 normal2;
    public Vector3 pos;

    public int toolType;
    public seamEnv(pointClass point, int toolType)
    {
        normal1 = point.plane1;
        normal2 = point.plane2;
        pos = point.pos;
        pos.x = -pos.x;
        this.toolType = toolType;
    }
}

public class envPara
{
    public GameObject obstacleMesh;
    public List<seamEnv> envPoints = new List<seamEnv>();

    public envPara(GameObject go, List<seamEnv> points)
    {
        obstacleMesh = go;
        envPoints = points;
    }
}

public class xmlLoader : MonoBehaviour
{
    public GameObject torch_1;
    public GameObject torch_2;
    public string PATH_TORCH_1 = "ToolData/MRW510_10GH";
    public string PATH_TORCH_2 = "ToolData/TAND_GERAD_DD";
    public Material material;

    private string pathToXmls = "Assets/Resources/xmlData";
    private string[] instanceNames;
    public string path = "xmlData/201910204483_R1.xml";
    // public string path = "xmlData/202110184743_R3.xml";

    // public GameObject root;
    // public GameObject obstacleMesh;
    public xmlDoc xdoc;
    private int currentDoc=0;
    private int currentPoint=0;

    private Material defaultMaterial;

    public List<seamEnv> envPoints = new List<seamEnv>();

    public MeshSplitter ms;

    int envCount = 0;
    public int instMax;

    // Start is called before the first frame update
    void Start()
    {
        // load_mesh(torch_1, PATH_TORCH_1);
        // load_mesh(torch_2, PATH_TORCH_2);
        instanceNames = getInstances();
        instMax = instanceNames.Length;
        // Debug.Log(instanceNames[0]);
        // getNewDoc(instanceNames[0]);
        // instanciateScene(instanceNames[0]);
        // loadFull("xmlData/" + instanceNames[0]);
        // Debug.Log(instanceNames[12]);
        // loadFull("xmlData/" + instanceNames[12]);

        if (ms==null)
        {
            ms = this.gameObject.GetComponent<MeshSplitter>();
        }
    }

    

    GameObject load_mesh(string meshPath, string name)
    {
        /// <summary>
        /// Loads mesh and creates a gameobject with a given name
        /// The gameObject then has to be pulled into Assets to be created as an prefab in order to be persistent beyond this session
        /// </summary>
        GameObject obj = new GameObject(name);
        MeshFilter filter = obj.AddComponent<MeshFilter>();
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

        // set mesh
        Mesh meshObj = Resources.Load<GameObject>(meshPath).transform.GetChild(0).gameObject.GetComponent<MeshFilter>().sharedMesh;
        meshObj.name = name;
        filter.mesh = meshObj;
        // set material
        if (defaultMaterial == null)
        {
            defaultMaterial = Resources.Load("Materials/defaultMaterial", typeof(Material)) as Material;
        }

        renderer.material = defaultMaterial;

        return obj;
    }

    public envPara instanciateScene(GameObject envRoot, int docIndex)
    {
        if (docIndex >= instanceNames.Length)
        {
            docIndex = instanceNames.Length-1;
        }
        Debug.Log("instantiate Scene");
        GameObject root = new GameObject("root");
        root.transform.localScale = Vector3.one;
        root.transform.localEulerAngles = Vector3.zero;
        // destroy old one if available
        if (envRoot.transform.childCount > 0)
        {
            foreach (Transform child in envRoot.transform) 
            {
                Destroy(child.gameObject);
            }
        }
        envRoot.transform.localScale = Vector3.one;
        envRoot.transform.localEulerAngles = Vector3.zero;
        // Debug.Log(instanceNames[docIndex]);
        // instantiate obstacle mesh
        GameObject obstacleMesh = load_mesh("MeshInputData/"+instanceNames[docIndex], instanceNames[docIndex]);
        // obstacleMesh.transform.SetParent(envRoot.transform);
        GameObject decomposedMesh = ms.SplitGameObject(obstacleMesh);
        Destroy(obstacleMesh);
        decomposedMesh.transform.SetParent(envRoot.transform);
        decomposedMesh.transform.localPosition = Vector3.zero;
        // to make sure that the splitmesh is also scaled and rotated
        envRoot.transform.localScale = Vector3.one * 0.006f;
        envRoot.transform.localEulerAngles = new Vector3(-90f,0,0);
        List<GameObject> points = new List<GameObject>();
        List<seamEnv> envPoints = new List<seamEnv>();
        
        xmlDoc xdoc = getNewDoc(docIndex);
        // read all positions and add to scene
        foreach (var seam in xdoc.seamList)
        {
            for (int i = 0; i < seam.pointList.Count; i++)
            {
                // GameObject go = GameObject.Instantiate(torch_1, seam.pointList[0].pos, Quaternion.identity);
                // Vector3 rot = AnglesFromFrame(seam.frameList[0]);
                // go.transform.localEulerAngles = rot;
                Vector3 pos =seam.frameList[i].pos;
                pos.x = -pos.x;
                GameObject tool;
                int toolType = 0;
                if (seam.WkzName == "MRW510_10GH")
                {
                    // go = GameObject.Instantiate(torch_1, pos, ExtractRotation(seam.frameList[i]));
                    // go = GameObject.Instantiate(torch_1, pos, QuaternionFromFrame(seam.frameList[i]));
                    tool = torch_1;
                    toolType = 1;
                }
                else
                {
                    tool = torch_2;
                    toolType = 2;
                    // go = GameObject.Instantiate(torch_2, pos, ExtractRotation(seam.frameList[i]));
                }
                // go = GameObject.Instantiate(tool, pos, QuaternionFromFrame(seam.frameList[i]));
                GameObject go = GameObject.Instantiate(tool);
                if (root != null)
                {
                    go.transform.SetParent(root.transform);
                }
                Matrix4x4 tf = getMatrixFromFrame(seam.frameList[i]);
                go.transform.localRotation = tf.rotation;
                go.transform.localPosition = pos;
                // // go.transform.position = seam.frameList[0].pos;
                // // float z = go.transform.localEulerAngles.z <=180 ? go.transform.localEulerAngles.z : (go.transform.localEulerAngles.z-360)%360;
                // go.transform.localEulerAngles = new Vector3 (go.transform.localEulerAngles.x, -go.transform.localEulerAngles.y, go.transform.localEulerAngles.z);
                // // Debug.Log(seam.frameList[i].pos);
                envPoints.Add(new seamEnv(seam.pointList[i], toolType));
                points.Add(go);
                // go.SetActive(false);
            }
        }
        // transform scene to easier to use scale and rotation
        root.transform.localScale = Vector3.one * 0.006f;
        root.transform.localEulerAngles = new Vector3(-90f,0,0);

        // get points after transformation
        for (int i = 0; i < envPoints.Count; i++)
        {
            envPoints[i].pos = points[i].transform.position; 
            Destroy(points[i]);
        }
        Destroy(root);
        return new envPara(decomposedMesh, envPoints);
    }

    public string[] getInstances()
    {
        // get files from xml folder has endings in it
        return Directory.GetFiles(pathToXmls, "*.xml").Select(filename =>Path.GetFileNameWithoutExtension(filename)).ToArray();
    }

    public seamEnv getNewPoint(int toolType)
    {
        // if (currentPoint==0)
        // {
        //     currentPoint =60;
        // }
        GameObject envRoot = new GameObject("envRoot");

        if (currentPoint < envPoints.Count)
        {
            seamEnv point = envPoints[currentPoint];
            currentPoint++;
            if (point.toolType != toolType)
            {
                return getNewPoint(toolType);
            }
            return point;
        }
        else if(currentDoc < instanceNames.Length)
        {
            getNewDoc(currentDoc);
            instanciateScene(envRoot, currentDoc);
            currentDoc++;
            currentPoint =0;
            return getNewPoint(toolType);
        }
        else
        {
            return null;
        }
    }

    public xmlDoc getNewDoc(int docIndex)
    {
        // still a new doc available
        string path = "xmlData/" + instanceNames[docIndex];
        // remove ending before reading
        // Debug.Log(path);
        return new xmlDoc(path);
    }

    // from https://answers.unity.com/questions/11363/converting-matrix4x4-to-quaternion-vector3.html
    public static Quaternion QuaternionFromFrame(frameClass frame) {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        // Quaternion q = new Quaternion();
        // q.w = Mathf.Sqrt( Mathf.Max( 0, 1 + frame.XVek[0] + frame.YVek[1] + frame.ZVek[2] ) ) / 2; 
        // q.x = Mathf.Sqrt( Mathf.Max( 0, 1 + frame.XVek[0] - frame.YVek[1] - frame.ZVek[2] ) ) / 2; 
        // q.y = Mathf.Sqrt( Mathf.Max( 0, 1 - frame.XVek[0] + frame.YVek[1] - frame.ZVek[2] ) ) / 2; 
        // q.z = Mathf.Sqrt( Mathf.Max( 0, 1 - frame.XVek[0] - frame.YVek[1] + frame.ZVek[2] ) ) / 2; 
        // q.x *= Mathf.Sign( q.x * ( frame.ZVek[1] - frame.YVek[2] ) );
        // q.y *= Mathf.Sign( q.y * ( frame.XVek[2] - frame.ZVek[0] ) );
        // q.z *= Mathf.Sign( q.z * ( frame.YVek[0] - frame.XVek[1] ) );

        float[,] r = new float[3, 3]
        {
            { frame.XVek[0], frame.YVek[0], frame.ZVek[0] },
            { frame.XVek[1], frame.YVek[1], frame.ZVek[1] },
            { frame.XVek[2], frame.YVek[2], frame.ZVek[2] }
        };

        // Calculate the trace of the matrix
        float trace = r[0, 0] + r[1, 1] + r[2, 2];

        float w, x, y, z;

        if (trace > 0)
        {
            float s = 0.5f / Mathf.Sqrt(trace + 1.0f);
            w = 0.25f / s;
            x = (r[2, 1] - r[1, 2]) * s;
            y = (r[0, 2] - r[2, 0]) * s;
            z = (r[1, 0] - r[0, 1]) * s;
        }
        else
        {
            if (r[0, 0] > r[1, 1] && r[0, 0] > r[2, 2])
            {
                float s = 2.0f * Mathf.Sqrt(1.0f + r[0, 0] - r[1, 1] - r[2, 2]);
                w = (r[2, 1] - r[1, 2]) / s;
                x = 0.25f * s;
                y = (r[0, 1] + r[1, 0]) / s;
                z = (r[0, 2] + r[2, 0]) / s;
            }
            else if (r[1, 1] > r[2, 2])
            {
                float s = 2.0f * Mathf.Sqrt(1.0f+ r[1, 1] - r[0, 0] - r[2, 2]);
                w = (r[0, 2] - r[2, 0]) / s;
                x = (r[0, 1] + r[1, 0]) / s;
                y = 0.25f * s;
                z = (r[1, 2] + r[2, 1]) / s;
            }
            else
            {
                float s = 2.0f * Mathf.Sqrt(1.0f + r[2, 2] - r[0, 0] - r[1, 1]);
                w = (r[1, 0] - r[0, 1]) / s;
                x = (r[0, 2] + r[2, 0]) / s;
                y = (r[1, 2] + r[2, 1]) / s;
                z = 0.25f * s;
            }
        }

        Quaternion q = new Quaternion((float)x, (float)y, (float)z, (float)w);

        return q;
    }

    public static float toDeg(float rad)
    {
        return rad * (180/Mathf.PI);
    }

    public static Vector3 AnglesFromFrame(frameClass frame)
    {
        // https://www.eecs.qmul.ac.uk/~gslabaugh/publications/euler.pdf
        float x = toDeg(Mathf.Atan2(frame.YVek[0], frame.XVek[0]));
        float y = toDeg(Mathf.Atan2(frame.ZVek[0], Mathf.Sqrt(Mathf.Pow(frame.XVek[0], 2) + Mathf.Pow(frame.YVek[0], 2))));
        float z = toDeg(Mathf.Atan2(frame.ZVek[1], frame.ZVek[2]));
        return new Vector3(x,y,z);
    }

    public static Quaternion ExtractRotation(frameClass frame)
    {
        // https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
        Vector3 forward;
        forward.x = frame.XVek[2];
        forward.y = frame.YVek[2];
        forward.z = frame.ZVek[2];
        Debug.Log(forward);
        Vector3 upwards;
        upwards.x = frame.XVek[1];
        upwards.y = frame.YVek[1];
        upwards.z = frame.ZVek[1];
        Debug.Log(upwards);

        return Quaternion.LookRotation(forward, upwards);
    }

    // https://docs.unity3d.com/ScriptReference/Matrix4x4.html
    public static Matrix4x4 getMatrixFromFrame(frameClass frame)
    {
        Matrix4x4 tf = new Matrix4x4();
        tf.SetRow(0, new Vector4(frame.XVek[0], frame.YVek[0], frame.ZVek[0], frame.pos[0]));
        tf.SetRow(1, new Vector4(frame.XVek[1], frame.YVek[1], frame.ZVek[0], frame.pos[1]));
        tf.SetRow(2, new Vector4(frame.XVek[2], frame.YVek[2], frame.ZVek[0], frame.pos[2]));
        tf.SetRow(3, new Vector4(0,0,0,1));
        return tf;
    }

    // // LEGACY
    public void loadFull(string path)
    {
        GameObject root = new GameObject("root");

        xdoc = new xmlDoc(path);

        foreach (var seam in xdoc.seamList)
        {
            for (int i = 0; i < seam.pointList.Count; i++)
            {
                // GameObject go = GameObject.Instantiate(torch_1, seam.pointList[0].pos, Quaternion.identity);
                // Vector3 rot = AnglesFromFrame(seam.frameList[0]);
                // go.transform.localEulerAngles = rot;
                Vector3 pos =seam.frameList[i].pos;
                pos.x = -pos.x;
                GameObject go;
                if (seam.WkzName == "MRW510_10GH")
                {
                    // go = GameObject.Instantiate(torch_1, pos, ExtractRotation(seam.frameList[i]));
                    go = GameObject.Instantiate(torch_1, pos, QuaternionFromFrame(seam.frameList[i]));
                }
                else
                {
                    // go = GameObject.Instantiate(torch_2, pos, ExtractRotation(seam.frameList[i]));
                    go = GameObject.Instantiate(torch_2, pos, QuaternionFromFrame(seam.frameList[i]));
                }
                // go.transform.position = seam.frameList[0].pos;
                // float z = go.transform.localEulerAngles.z <=180 ? go.transform.localEulerAngles.z : (go.transform.localEulerAngles.z-360)%360;
                go.transform.localEulerAngles = new Vector3 (go.transform.localEulerAngles.x, -go.transform.localEulerAngles.y, go.transform.localEulerAngles.z);
                // Debug.Log(seam.frameList[i].pos);
                if (root != null)
                {
                    go.transform.SetParent(root.transform);
                }
            }
        }
        root.transform.localScale = Vector3.one * 0.006f;
        root.transform.localEulerAngles = new Vector3(-90f,0,0);
    }
}

 