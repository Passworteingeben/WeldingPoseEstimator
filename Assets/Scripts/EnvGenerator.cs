using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Legacy_BaseTrainer;

public class EnvGenerator : MonoBehaviour
{
    public GameObject base1;
    public GameObject base2;
    public GameObject target;
    private float length = 10f;
    private float height = 0.1f;
    private float width = 1f;
    private int numOfObs = 4;
    public List<GameObject> listOfObstacles = new List<GameObject>();

    public Vector3 obstacleEmptySpace = new Vector3(0,-15f,0);

    private Vector3 obstacleDefaultSize = new Vector3(0.5f,0.5f,0.5f);

    // for convex hull decomposition
    // https://github.com/Unity-Technologies/VHACD

    // Start is called before the first frame update
    void Start()
    {
        if (target == null)
        {
            target = setSphere(new Vector3(-0.25f,0.1f,0.1f), new Vector3(0,0,0), new Vector3(0.3f, 0.3f, 0.3f), name : "target");
            target.tag ="target";
        }
        Material newMat = Resources.Load("Materials/green", typeof(Material)) as Material;
        target.GetComponent<MeshRenderer>().material = newMat;
        if (base1 == null)
        {
            base1 = setCube(new Vector3(0,0,0), new Vector3(0,0,0), new Vector3(length, height,width), name:"floor");
            MeshOrientation orient = base1.AddComponent<MeshOrientation>();
            orient.originalMaterial = Resources.Load("Materials/blue", typeof(Material)) as Material;
            orient.resetColor();
        }
        if (base2 == null)
        {
            base2 = setCube(new Vector3(0,width,-height), new Vector3(0,0,0), new Vector3(length, width, height), name:"wall");
            MeshOrientation orient = base2.AddComponent<MeshOrientation>();
            orient.originalMaterial = Resources.Load("Materials/yellow", typeof(Material)) as Material; 
            orient.resetColor();
        }
        for (int i=0; i < numOfObs; i++)
        {
            setObstacle();
        }
        // Debug.Log(base1.transform.forward);
        // Debug.Log(base2.transform.forward);
    }

    // add obstacle object
    GameObject setObstacle()
    {
        GameObject obstacle = setCube(obstacleEmptySpace, Vector3.zero, obstacleDefaultSize, "obstacle"+ listOfObstacles.Count);
        this.listOfObstacles.Add(obstacle);
        MeshOrientation orient = obstacle.AddComponent<MeshOrientation>();
        return obstacle;
    }

    // create Cube
    GameObject setCube(Vector3 position, Vector3 rotation, Vector3 scale, string name="obstacle")
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.tag = "obstacle";
        cube.transform.SetParent(this.transform);
        cube.transform.localPosition = position + new Vector3(-scale[0]/2, -scale[1]/2, scale[2]/2);
        cube.transform.localEulerAngles = rotation;
        cube.transform.localScale = scale;
        return cube;
    }

    // create sphere
    GameObject setSphere(Vector3 position, Vector3 rotation, Vector3 scale, string name)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cube.name = name;
        cube.transform.SetParent(this.transform);
        cube.transform.localPosition = position;
        cube.transform.localEulerAngles = rotation;
        cube.transform.localScale = scale;
        return cube;
    }

    void resetObstacles()
    {
        // move all obstacles to empty space
        foreach (var obj in listOfObstacles)
        {
            obj.transform.localPosition = obstacleEmptySpace;
            obj.transform.localEulerAngles = Vector3.zero;
            obj.transform.localScale = obstacleDefaultSize;
        }
    }

    public List<ObstacleParameters> generateObstacleList()
    {
        List<ObstacleParameters> output = new List<ObstacleParameters>();
        foreach (var obj in this.listOfObstacles)
        {
            output.Add(new ObstacleParameters(obj));
        }
        return output;
    }

    public void setEnv(ProblemParameters envParams)
    {
        // set environment given parameters
        base1.transform.localPosition = envParams.base1Position;
        base1.transform.localEulerAngles = envParams.base1Rotation;
        base1.transform.localScale = envParams.base1Scale;
        base2.transform.localPosition = envParams.base2Position;
        base2.transform.localEulerAngles = envParams.base2Rotation;
        base2.transform.localScale = envParams.base2Scale;
        target.transform.localPosition = envParams.targetPosition;
        // reset obstacles to ensure that the area is clear
        // generate obstacles if necessary
        if (envParams.listOfObstacles.Count > this.listOfObstacles.Count)
        {
            setObstacle();
        }
        resetObstacles();
        // set obstacles
        for (int i=0; i < envParams.listOfObstacles.Count; i++)
        {
            GameObject obstacle = this.listOfObstacles[i];
            ObstacleParameters obsParam = envParams.listOfObstacles[i];
            obstacle.transform.localPosition = obsParam.position;
            obstacle.transform.localEulerAngles = obsParam.rotation;
            obstacle.transform.localScale = obsParam.scale;
        }
    }
    
    public ProblemParameters generateEnv(ProblemParameters envParams)
    {
        envParams.base1Position = base1.transform.localPosition;
        envParams.base1Rotation = base1.transform.localEulerAngles;
        envParams.base1Scale = base1.transform.localScale;
        envParams.base2Position = base2.transform.localPosition;
        envParams.base2Rotation = base2.transform.localEulerAngles;
        envParams.base2Scale = base2.transform.localScale;
        envParams.targetPosition = target.transform.localPosition;
        envParams.listOfObstacles = generateObstacleList();
        return envParams;
    }

    public GameObject addBaseHorizontalObstacle(float xPosition = -5f, float yPosition=1f, float minYScale = 0.1f, float maxYScale=0.3f,float minZScale = 0.5f, float maxZScale=2f)
    {
        GameObject hObs = listOfObstacles[0];
        hObs.transform.localPosition = new Vector3(xPosition,yPosition,-2f);
        hObs.transform.localScale = new Vector3(10f,Random.Range(minYScale, maxYScale),Random.Range(minZScale, maxZScale));
        return hObs;
    }

    public void getNewEnv(Vector3 inNormal1, Vector3 inNormal2, Vector3 spot)
    {
        Vector3 angleVector = (inNormal1.normalized + inNormal2.normalized).normalized;
        // this.transform.LookAt(angleVector);
        // this.transform.localEulerAngles = this.transform.localEulerAngles + new Vector3(45f,0f,0f);
        // this.transform.localEulerAngles = new Vector3(45f,0f,0f);
        base1.transform.localPosition = new Vector3(UnityEngine.Random.Range(-5f,5f), -0.05f, 0.5f);
        base2.transform.localPosition = new Vector3(UnityEngine.Random.Range(-5f,5f), 0.5f, -0.05f);
        resetObstacles();
        System.Random rng = new System.Random();
        int obsIdx = rng.Next(3);

        // only add at most 2 obstacles to guarantee solution
        // choose vertical or horizontal obs
        // if 0 then skip
        if (obsIdx==1)
        {
            // add vertical Obstacle
            listOfObstacles[0].transform.localPosition = new Vector3(UnityEngine.Random.Range(-7f,-3f), UnityEngine.Random.Range(1f,1.5f), UnityEngine.Random.Range(-1f,1f));
            listOfObstacles[0].transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            listOfObstacles[0].transform.localScale = new Vector3(10f, 0.1f, 1f);
            MeshOrientation orient = listOfObstacles[0].GetComponent<MeshOrientation>();
            orient.originalMaterial = Resources.Load("Materials/blue", typeof(Material)) as Material; 
            orient.resetColor();

        }

        if(obsIdx==2)
        {
            // add horizontal Obstacle
            listOfObstacles[1].transform.localPosition = new Vector3(UnityEngine.Random.Range(-7f,-3f), UnityEngine.Random.Range(-1f,1f), UnityEngine.Random.Range(1f,1.5f));
            listOfObstacles[1].transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            listOfObstacles[1].transform.localScale = new Vector3(10f, 1f, 0.1f);
            MeshOrientation orient = listOfObstacles[1].GetComponent<MeshOrientation>();
            orient.originalMaterial = Resources.Load("Materials/yellow", typeof(Material)) as Material; 
            orient.resetColor();

        }        

        // choose side obstacles
        rng = new System.Random();
        obsIdx = rng.Next(3);

        // if 0 then skip
        if (obsIdx==1)
        {
            // add left side Obstacle
            listOfObstacles[2].transform.localPosition = new Vector3(UnityEngine.Random.Range(0.1f,0.5f), UnityEngine.Random.Range(0.25f,0.5f), UnityEngine.Random.Range(-5f,-2f));
            listOfObstacles[2].transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            listOfObstacles[2].transform.localScale = new Vector3(0.1f, 1f, 10f);
            MeshOrientation orient = listOfObstacles[2].GetComponent<MeshOrientation>();
            orient.originalMaterial = Resources.Load("Materials/yellow", typeof(Material)) as Material; 
            orient.resetColor();

        }

        if(obsIdx==2)
        {
            // add right side Obstacle
            listOfObstacles[3].transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.1f,-0.5f), UnityEngine.Random.Range(0.25f,0.5f), UnityEngine.Random.Range(-5f,-2f));
            listOfObstacles[3].transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            listOfObstacles[3].transform.localScale = new Vector3(0.1f, 1f, 10f);
            MeshOrientation orient = listOfObstacles[3].GetComponent<MeshOrientation>();
            orient.originalMaterial = Resources.Load("Materials/yellow", typeof(Material)) as Material; 
            orient.resetColor();

        }    
    }
}
