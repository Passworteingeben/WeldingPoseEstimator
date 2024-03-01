using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using static Legacy_BaseTrainer;
using static xmlLoader;


public class PoseEstimationLidarAgent : Agent
{
    public xmlLoader xmlloader;
    public ProblemParameters currentEnv;

    public BaseControl baseControl;

    private GameObject cageEndPoint;
    private GameObject[] cageStartPoints;

    RaycastHit hit;
    float RAYCAST_DURATION = 0.1f;

    public bool SHOW_RAYCAST = true;


    private Vector3 startPosition;
    private Vector3 startRotation;

    // float TRANSLATION_SPEED;

    float PUNISHMENT_PER_STEP = 0.001f;

    EnvironmentParameters configParams;
    // new Vars
    public Vector3 targetPosition;
    public Vector3 angleVector;
    public Vector3 normal1;
    public Vector3 normal2;


    public GameObject envRoot;
    public EnvGenerator envGenerator;
    private GameObject target;

    private float currentReward;

    private int steps = 0;

    int minSteps = 3;
    public bool collisionState = false;

    // private List<CollisionCheck> colliders = new List<CollisionCheck>();
    private CollisionCheck[] colliders;
    private List<string> materialNames = new List<string> {"blue", "yellow"};
    private List<Material> mats = new List<Material>();

    public Material collisionMaterial;
    public Material defaultMaterial;

    private Transform parent;
    private Transform trainingArea;
    private bool showColor = true;

    public int toolType = 2; // 1 = MRW510_10GH or 2 = TAND_GERAD_DD

    private int docIndex=0;
    private int pointIndex=0;

    private envPara envParameters;
    public int numOfParallelInstances = 16;

    public override void Initialize()
    {
        parent = this.transform.parent;
        trainingArea = parent.transform.parent;
        addMaterials();
        registerColliders();
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.InferenceOnly)
        {
            // is training set min num of steps to 5
            minSteps = 5;
        }
        configParams = Academy.Instance.EnvironmentParameters;
        envGenerator = this.transform.parent.Find("EnvGenerator").GetComponent<EnvGenerator>();
        target = envGenerator.target;

        startPosition = this.gameObject.transform.localPosition;
        startRotation = this.gameObject.transform.localEulerAngles;

        baseControl = gameObject.GetComponent<BaseControl>();
        
        foreach (Transform child in this.gameObject.transform.parent.parent)
        {
            if (child.name == "Env")
            {
                envRoot = child.gameObject;        
            }
        }
        if (envRoot == null)
        {
            Debug.Log("env Root not assigned, assign it to Env");
        }

        Transform tool_transform = null;
        foreach (Transform child in transform)
        {
            if (child.gameObject.name.Contains("Lidar") && child.gameObject.activeSelf)
            {
                // get lidar Gameobject if its active
                tool_transform = child;
                break;
            }
        }
        if (tool_transform == null)
        {
            Debug.Log("no lidar Agent found, has Gameobject with prefix Lidar has to be a child of agent");
        }

        foreach (Transform child_transform in tool_transform)
        {
            if (child_transform.name == "cage_ray_start_points")
            {
                cageStartPoints = new GameObject[child_transform.childCount];
                foreach (Transform grandchild_transform in child_transform)
                {
                    cageStartPoints[int.Parse(grandchild_transform.name)] = grandchild_transform.gameObject;
                    grandchild_transform.gameObject.GetComponent<Collider>().enabled=false;
                }
            }

            if (child_transform.name == "cage_ray_end_points")
            {
                foreach (Transform grandchild_transform in child_transform)
                {
                    cageEndPoint = grandchild_transform.gameObject;
                    grandchild_transform.gameObject.GetComponent<Collider>().enabled=false;
                }
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        // Debug.Log("Episode begin");
        steps = 0;
        // envGenerator.getNewEnv(new Vector3(0f,1f,0f), new Vector3(0f,0f,1f), target.transform.localPosition);
        // seamEnv seamPoint = xmlloader.getNewPoint(toolType);
        seamEnv seamPoint = null;
        if (envParameters == null)
        {
            // envParameters = xmlloader.instanciateScene(envRoot, UnityEngine.Random.Range(0,16));
            // docIndex = 0;
            var id = this.parent.parent.gameObject.name.Split('_');
            int.TryParse(id.Last(), out docIndex);
            docIndex -=1;
            if (docIndex < 0)
            {
                docIndex = 0;
            }
            Debug.Log(docIndex);
            envParameters = xmlloader.instanciateScene(envRoot, docIndex);
            pointIndex = 0;
        }
        bool correctTool = false;
        while (!correctTool)
        {
            if (pointIndex < envParameters.envPoints.Count)
            {
                // new point available
                seamPoint = envParameters.envPoints[pointIndex];
            }
            else
            {
                // get new doc and new point
                // envParameters = xmlloader.instanciateScene(envRoot, UnityEngine.Random.Range(0,xmlloader.instMax));
                // docIndex+=1; 
                Debug.Log("finished doc: " + docIndex.ToString());
                docIndex += numOfParallelInstances;
                envParameters = xmlloader.instanciateScene(envRoot, docIndex);
                pointIndex = 0;
                seamPoint = envParameters.envPoints[pointIndex];
            }
            pointIndex +=1;
            // skip wrong tool
            if (seamPoint.toolType == toolType)
            {
                correctTool = true;
            }
            else
            {
                correctTool = false;
            }
        }
        // seamEnv seamPoint = null;
        if (seamPoint != null)
        {
            setAgent(seamPoint.normal1, seamPoint.normal2, seamPoint.pos);
        }
        else
        // {
        //     setAgent(new Vector3(0f,1f,0f), new Vector3(0f,0f,1f), target.transform.localPosition);
        // }

        collisionState = CollisionCheck();
        // Debug.Log($"Episode begin {this.angleVector}");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 normalizedAngles = this.transform.localEulerAngles * 1/360;
        sensor.AddObservation(normalizedAngles);
        sensor.AddObservation(this.collisionState);

        Vector3 start_point;
        Vector3 end_point;
        Vector3 dir;
        float dist;

        // get dist
        start_point = cageStartPoints[0].transform.position;
        end_point = cageEndPoint.transform.position;
        dir = end_point - start_point;
        dist = dir.magnitude;
        int countd = 5;
        // add cage raycast obs
        for (int i = 0; i < cageStartPoints.Length; i++)
        {
            start_point = cageStartPoints[i].transform.position;
            dir = end_point - start_point;
            dir = Vector3.Normalize(dir);
            Physics.Raycast(start_point, dir, out hit, dist);
            sensor.AddObservation(hit.distance/dist);
            sensor.AddObservation(ray_is_hit(hit));
            countd+=2;
            if (SHOW_RAYCAST)
            {
                if (ray_is_hit(hit))
                {
                    Debug.DrawRay(start_point, dir * hit.distance, Color.red, RAYCAST_DURATION);
                }
                else
                {
                    Debug.DrawRay(start_point, dir * dist, Color.green, RAYCAST_DURATION);
                }
            }
        }
        // Debug.Log($"number of obs {countd}");
    }


    public void setAgent(Vector3 inNormal1, Vector3 inNormal2, Vector3 spot)
    {
        // // inNormal1 = new Vector3(inNormal1.x, inNormal1.z, inNormal1.y);
        // // inNormal2 = new Vector3(inNormal2.x, inNormal2.z, inNormal2.y);
        // Debug.Log(inNormal1);
        // Debug.Log(inNormal2);
        this.normal1 = inNormal1.normalized;
        this.normal2 = inNormal2.normalized;
        Quaternion rot = new Quaternion();
        rot.SetLookRotation(normal1, normal2);
        // Debug.Log(parent.localEulerAngles);
        parent.localRotation = Quaternion.Euler(-90, 0,0) * rot;
        Vector3 angle = rotationToHalf(parent.localEulerAngles);
        if (Mathf.Abs(Mathf.Abs(angle.y)-90) < 45)
        {
            parent.localEulerAngles = angle + new Vector3(0f,-180f+angle.z,0f);
        }
        // parent.localRotation = Quaternion.Euler(-90, 0,0) * rot;
        // parent.localRotation = rot;
        // Vector3 addedRot = parent.localEulerAngles + new Vector3(-90f,0f,0f);
        // parent.localEulerAngles = addedRot;
        parent.localPosition = spot; // - trainingArea.localPosition - 0.006f* trainingArea.localPosition;
        this.angleVector = new Vector3(-45f,0f,0f);
        this.transform.localEulerAngles = new Vector3(-45f,0f,0f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Debug.Log(steps);
        collisionState = CollisionCheck();
        if (!collisionState && (steps < minSteps))
        {
            Debug.Log($"skip, {minSteps}");
            SetReward(5*PUNISHMENT_PER_STEP);
            EndEpisode();
        }
        else
        {
            if (steps == 4)
            {
                Debug.Log($"Episode begin {this.angleVector}");
            }
        }
        // Debug.Log(CollisionCheck());
        calculateReward();
        if (steps > minSteps)
        {
            // if ((actionBuffers.DiscreteActions[2]==1))
            if ((actionBuffers.DiscreteActions[2]==1) && (collisionState ==false))
            {
                // Debug.Log($"finished {currentReward}");
                Debug.Log($"finished success");
                // Debug.Log(currentReward);
                SetReward(currentReward);
                EndEpisode();
            }
        }
        if ((steps >= 400))
        {
            Debug.Log("failed");
            // Debug.Log($"failed {currentReward}");
            // SetReward(currentReward);
            SetReward(-1f);
            EndEpisode();
        }
        steps+=1;
        // Debug.Log(this.StepCount);
        int[] actionsArray = new int[]{0,
                                        0,
                                        0,
                                        actionBuffers.DiscreteActions[0],
                                        actionBuffers.DiscreteActions[1],
                                        0};
        baseControl.moveAgent(actionsArray);
        // reduce number of steps taken
        SetReward(-PUNISHMENT_PER_STEP);
    }

    bool ray_is_hit(RaycastHit hit)
    {
        if (hit.collider != null)
        {
            if (hit.collider.tag == "target")
            {
                // target is not ignored from raycast to show that it can be hit and in that case it is set to false this gives the target thickness
                return false;
            }
        }
        return hit.collider != null;
    }
    public void calculateReward()
    {
        collisionState = CollisionCheck();
        if (!collisionState)
        {
            currentReward = 1f;
        }
        else
        {
            currentReward = -1f;
        }
        Vector3 currentAngle = rotationToHalf(this.transform.localEulerAngles);
        // // Debug.Log(angles);
        // if ((angles.x<=-90) || (angles.x>=0) || (angles.y>=90) || (angles.y<=-90))
        // {
        //     // Debug.Log($"{angles.x} {angles.y}");
        //     currentReward -=15f;
        // }
        // punish for bad angle
        // float badAngle = (Mathf.Abs(this.angleVector.x - currentAngle.x) + Mathf.Abs(this.angleVector.y - currentAngle.y))/ 720;
        // Debug.Log($"bad angle {currentAngle} - {this.angleVector} : {badAngle}");
        currentReward -= (Mathf.Abs(this.angleVector.x - currentAngle.x) + Mathf.Abs(this.angleVector.y - currentAngle.y))/ 720;;
    }

    // direct control functions from Agent
    public override void Heuristic(in ActionBuffers actionBuffersOut)
    {
        var discreteActions = actionBuffersOut.DiscreteActions;
        // Debug.Log(collisionState);
        // if (!collisionState)
        if (Input.GetKey(KeyCode.Space))
        {
            discreteActions[2] = 1;
        }
        else
        {
            discreteActions[2] = 0;
            // otherwise dont set any actions
            if (Input.GetKey(KeyCode.DownArrow))
            {
                // Debug.Log("Q pressed");  
                discreteActions[0] = 1;
            }
            else if (Input.GetKey(KeyCode.UpArrow))
            {
                // Debug.Log("W pressed");
                discreteActions[0] = 2;
            }
            else
            {
                discreteActions[0] = 0;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                // Debug.Log("A pressed");
                discreteActions[1] = 2;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                // Debug.Log("E pressed");
                discreteActions[1] = 1;
            }
            else
            {
                discreteActions[1] = 0;
            }
        }

    }

    // Angle functions
    public Vector3 rotationToHalf(Vector3 vec)
    {
        Vector3 tempV = Vector3.zero;
        tempV.x = vec.x <=180 ? vec.x : (vec.x-360)%360;
        tempV.y = vec.y <=180 ? vec.y : (vec.y-360)%360;
        tempV.z = vec.z <=180 ? vec.z : (vec.z-360)%360;
        return tempV;
    }

    // Collision functions
    public bool CollisionCheck()
    {
        // int count =0;
        foreach (CollisionCheck colCheck in colliders)
        {
            if (colCheck.collisionState)
            {
                // Debug.Log($"collision on {count}");
                // Debug.Log("collision found");
                // SetReward(-0.02f);
                return true;
            }
            // count+=1;
        }
        return false;
    }

    public void registerColliders()
    {
        colliders = this.transform.GetComponentsInChildren<CollisionCheck>();
        foreach (CollisionCheck colCheck in colliders)
        {
            colCheck.mats = mats;
            colCheck.collisionMaterial = collisionMaterial;
            colCheck.defaultMaterial = defaultMaterial;
            colCheck.showColor =showColor; 
        }
    }

    public void addMaterials()
    {
        foreach (string matName in materialNames)
        {
            mats.Add(Resources.Load("Materials/" + matName, typeof(Material)) as Material);
            // Debug.Log(matName);
        }
        if (collisionMaterial == null)
        {
            collisionMaterial = Resources.Load("Materials/red", typeof(Material)) as Material;
        }
        if (defaultMaterial == null)
        {
            defaultMaterial = Resources.Load("Materials/defaultMaterial", typeof(Material)) as Material;
        }
    }

    public void resetColliders()
    {
        foreach (CollisionCheck colCheck in colliders)
        {
            colCheck.resetCollisionState();
            collisionState = false;
        }
    }
}


