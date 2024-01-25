using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;
using System.IO;
using System.Linq;
using static Legacy_BaseTrainer;
using System.Collections.Generic;


public class PoseEstimationLidarAgent : Agent
{
    public GameObject toolGameObject;
    public ProblemParameters currentEnv;

    public BaseControl baseControl;

    private GameObject[] straightStartPoints;
    private GameObject[] straightEndPoints;
    private GameObject cageEndPoint;
    private GameObject[] cageStartPoints;
    public GameObject[] envStartPoints;

    // Values should be between 0-1, and will then be shifted and adjusted between -1 and 1
    // private Vector3 FORCE_RIGHT = new Vector3(1.0f, 0.0f, 0.0f);
    // // private Vector3 FORCE_LEFT = new Vector3(-1.0f, 0.0f, 0.0f);
    // private Vector3 FORCE_UP = new Vector3(0.0f, 1.0f, 0.0f);
    // // private Vector3 FORCE_DOWN = new Vector3(0.0f, -1.0f, 0.0f);
    // private Vector3 FORCE_FORWARD = new Vector3(0.0f, 0.0f, 1.0f);
    // // private Vector3 FORCE_BACKWARD = new Vector3(0.0f, 0.0f, -1.0f);
    // private float FORCE_ROTATE_X = 1.0f*0.5f;
    // private float FORCE_ROTATE_Y = 1.0f*0.5f;
    // private float FORCE_ROTATE_Z = 1.0f*0.5f;

    public Rigidbody body;

    RaycastHit hit;

    float RAYCAST_DURATION = 0.01f;

    public bool SHOW_RAYCAST = true;

    private Vector3 startPosition;
    private Vector3 startRotation;

    // float TRANSLATION_SPEED;

    float PUNISHMENT_PER_STEP = 1/100;

    int DIFFICULTY_STEP = 300;      // num of completed episodes before increase of difficulty
    int difficulty = 0;

    EnvironmentParameters configParams;
    public int startDifficulty;

    // new Vars
    public Vector3 targetPosition;
    public Vector3 angleVector;
    public Vector3 normal1;
    public Vector3 normal2;


    public EnvGenerator envGenerator;
    private GameObject target;
    public GameObject weldingSpot;

    private float currentReward;

    private int steps = 0;

    public bool collisionState = false;
    private CollisionCheck[] colliders;
    private List<string> materialNames = new List<string> {"blue", "yellow"};
    private List<Material> mats = new List<Material>();

    public Material collisionMaterial;
    public Material defaultMaterial;

    private bool showColor = true;

    public override void Initialize()
    {
        configParams = Academy.Instance.EnvironmentParameters;
        envGenerator = this.transform.parent.Find("EnvGenerator").GetComponent<EnvGenerator>();
        target = envGenerator.target;

        startPosition = this.gameObject.transform.position;
        startRotation = this.gameObject.transform.eulerAngles;

        body = gameObject.GetComponent<Rigidbody>();
        baseControl = gameObject.GetComponent<BaseControl>();
        Transform tool_transform;
        if (toolGameObject == null)
        {
            tool_transform = this.gameObject.transform.Find("collision_TAND_GERAD_DD");
        }
        else
        {
            tool_transform = toolGameObject.transform;
        }

        foreach (Transform child_transform in tool_transform)
        {
            if (child_transform.name == "straight_ray_start_points")
            {
                straightStartPoints = new GameObject[child_transform.childCount];
                foreach (Transform grandchild_transform in child_transform)
                {
                    straightStartPoints[int.Parse(grandchild_transform.name)] = grandchild_transform.gameObject;
                    grandchild_transform.gameObject.GetComponent<Collider>().enabled=false;
                }
            }

            if (child_transform.name == "straight_ray_end_points")
            {
                straightEndPoints = new GameObject[child_transform.childCount];
                foreach (Transform grandchild_transform in child_transform)
                {
                    straightEndPoints[int.Parse(grandchild_transform.name)] = grandchild_transform.gameObject;
                    grandchild_transform.gameObject.GetComponent<Collider>().enabled=false;
                }
            }
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

        List<GameObject> tempEnvStartPoints = new List<GameObject>();

        foreach (var obj in envGenerator.gameObject.GetComponentsInChildren<Transform>())
        {
            if (obj.tag == "ray_point")
            {
                obj.gameObject.name=obj.parent.gameObject.name + "_" + obj.gameObject.name;
                tempEnvStartPoints.Add(obj.gameObject);
            }
        }
        tempEnvStartPoints.OrderBy(go=>go.name).ToList();
        envStartPoints = tempEnvStartPoints.ToArray();
}

    public override void OnEpisodeBegin()
    {
        // Debug.Log("Episode begin");
        steps = 0;
        // int difficulty = Mathf.RoundToInt(configParams.GetWithDefault("difficulty", 2)) + startDifficulty;
        // if (difficulty > 10)
        // {
        //     difficulty = 10;
        // }
        // Debug.Log($"current CompletedEpisodes: {this.CompletedEpisodes}; curr diff {difficulty}");
        // string problemPath = $"Assets/Solutions/ProblemSet{difficulty}";
        // System.Random rnd = new System.Random();
        // int randomProblem = rnd.Next(0, Directory.GetFiles(problemPath).Where(fname => fname.EndsWith(".json")).ToList().Count);
        // problemPath += $"/d_01_t_01_r_00_{randomProblem}.json";
        // // Debug.Log(problemPath);
        // readSolution(problemPath);
        // body.angularVelocity = Vector3.zero;
        // body.velocity = Vector3.zero;
        setAgent(new Vector3(0f,1f,0f), new Vector3(0f,0f,1f), target.transform.localPosition);
        envGenerator.getNewEnv(new Vector3(0f,1f,0f), new Vector3(0f,0f,1f), target.transform.localPosition);
        collisionState = CollisionCheck();
        Debug.Log($"Episode begin {this.angleVector}");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 normalizedAngles = this.transform.eulerAngles * 1/360;
        sensor.AddObservation(normalizedAngles);
        sensor.AddObservation(this.collisionState);
        calculateReward();
        sensor.AddObservation(currentReward);

        Vector3 start_point;
        Vector3 end_point;
        Vector3 dir;
        float dist;

        // get dist
        start_point = straightStartPoints[0].transform.position;
        end_point = straightEndPoints[0].transform.position;
        dir = end_point - start_point;
        dist = dir.magnitude;
    
        // // add straight raycast obs
        // for (int i = 0; i < straightStartPoints.Length; i++)
        // {
        //     start_point = straightStartPoints[i].transform.position;
        //     end_point = straightEndPoints[i].transform.position;
        //     dir = end_point - start_point;
        //     dir = Vector3.Normalize(dir);
        //     Physics.Raycast(start_point, dir, out hit, dist);
        //     // hit.distance = 1f;
        //     // normalize obs
        //     sensor.AddObservation(hit.distance/dist);

        //     if (SHOW_RAYCAST)
        //     {
        //         if (ray_is_hit(hit))
        //         {
        //             Debug.DrawRay(start_point, dir * hit.distance, Color.red, RAYCAST_DURATION);
        //         }
        //         else
        //         {
        //             Debug.DrawRay(start_point, dir * dist, Color.green, RAYCAST_DURATION);
        //         }
        //     }
        // }

        // get dist
        start_point = cageStartPoints[0].transform.position;
        end_point = cageEndPoint.transform.position;
        dir = end_point - start_point;
        dist = dir.magnitude;
        // int countd = 5;
        // add cage raycast obs
        for (int i = 0; i < cageStartPoints.Length; i++)
        {
            start_point = cageStartPoints[i].transform.position;
            dir = end_point - start_point;
            dir = Vector3.Normalize(dir);
            Physics.Raycast(start_point, dir, out hit, dist);
            sensor.AddObservation(hit.distance/dist);
            sensor.AddObservation(ray_is_hit(hit));
            // countd+=2;
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
        // Debug.Log(countd);
        // // add env raycast obs
        // for (int i = 0; i < envStartPoints.Length; i++)
        // {
        //     start_point = envStartPoints[i].transform.position;
        //     dir = end_point - start_point;
        //     dist = dir.magnitude;
        //     dir = Vector3.Normalize(dir);
        //     Physics.Raycast(start_point, dir, out hit, dist);
        //     sensor.AddObservation(hit.distance/dist);
        //     sensor.AddObservation(ray_is_hit(hit));
        //     if (SHOW_RAYCAST)
        //     {
        //         if (ray_is_hit(hit))
        //         {
        //             Debug.DrawRay(start_point, dir * hit.distance, Color.red, RAYCAST_DURATION);
        //         }
        //         else
        //         {
        //             Debug.DrawRay(start_point, dir * dist, Color.green, RAYCAST_DURATION);
        //         }
        //     }
        // }
    }

    public void setAgent(Vector3 inNormal1, Vector3 inNormal2, Vector3 spot)
    {
        this.normal1 = inNormal1.normalized;
        this.normal2 = inNormal2.normalized;
        // angleVector = (normal1 + normal2).normalized;
        this.transform.localPosition = spot;
        // this.transform.LookAt((normal1 + normal2));
        // angleVector = this.transform.eulerAngles;
        angleVector = new Vector3(-45f,0f,0f);
        this.transform.eulerAngles = angleVector;
    }



    public void readSolution(string inputFilename)
    {
        // Debug.Log($"current pos : {this.transform.localPosition}");
        // Debug.Log($"current rot : {this.transform.eulerAngles}");
        if (File.Exists(inputFilename))
        {
            // Read the entire contents of the file as a string
            string jsonString = File.ReadAllText(inputFilename);
            this.currentEnv = JsonUtility.FromJson<ProblemParameters>(jsonString);
            this.currentEnv.deserialize();
            // Debug.Log(this.currentEnv.serializedVectors);
            // Debug.Log($"env pos : {this.currentEnv.startPosition}");
            this.envGenerator.setEnv(this.currentEnv);
            this.transform.localPosition = this.currentEnv.startPosition;
            this.transform.eulerAngles = this.currentEnv.startRotation;
            this.target.transform.localPosition = this.currentEnv.targetPosition;
        }
        else
        {
            Debug.LogError("Filepath not found : " + inputFilename);
        }
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

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        collisionState = CollisionCheck();
        // Debug.Log(CollisionCheck());
        calculateReward();
        if (steps > 5)
        {
            // if ((actionBuffers.DiscreteActions[2]==1))
            if ((actionBuffers.DiscreteActions[2]==1) && (collisionState ==false))
            {
                if (currentReward < 0f)
                {
                    Debug.Log(currentReward);
                }
                // Debug.Log($"finished {currentReward}");
                Debug.Log($"finished {collisionState}");
                // Debug.Log(currentReward);
                SetReward(currentReward);
                EndEpisode();
            }
        }
        if ((steps > 701) || currentReward < -12f)
        {
            Debug.Log($"failed {currentReward}");
            SetReward(currentReward);
            EndEpisode();
            // if (collisionState==true)
            // {
            // }
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
        SetReward(-0.01f);
    }

    public void calculateReward()
    {
        collisionState = CollisionCheck();
        if (collisionState)
        {
            currentReward = -10f;
        }
        else
        {
            currentReward = 10f;
        }
        Vector3 angles = rotationToHalf(this.transform.eulerAngles);
        // Debug.Log(angles);
        if ((angles.x<=-90) || (angles.x>=0) || (angles.y>=90) || (angles.y<=-90))
        {
            // Debug.Log($"{angles.x} {angles.y}");
            currentReward -=15f;
        }
        if (steps >= 700)
        {
            currentReward -=15f;
        }
        // punish for bad angle
        // float badAngle = (this.angleVector - this.transform.eulerAngles).magnitude / 3600;
        // Debug.Log($"bad angle {badAngle}");
        // currentReward -= badAngle;
    }

    // direct control functions from Agent
    public override void Heuristic(in ActionBuffers actionBuffersOut)
    {
        var discreteActions = actionBuffersOut.DiscreteActions;
        // Debug.Log(collisionState);
        if (!collisionState)
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
        int count =0;
        foreach (CollisionCheck colCheck in colliders)
        {
            if (colCheck.collisionState)
            {
                // Debug.Log($"collision on {count}");
                // Debug.Log("collision found");
                SetReward(-0.02f);
                return true;
            }
            count+=1;
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
            collisionMaterial = Resources.Load("collisionMaterial", typeof(Material)) as Material;
        }
        if (defaultMaterial == null)
        {
            defaultMaterial = Resources.Load("defaultMaterial", typeof(Material)) as Material;
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


