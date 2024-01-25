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

public class PoseEstimationVisAgent : Agent
{
    public xmlLoader xmlloader;
    public GameObject toolGameObject;
    public ProblemParameters currentEnv;

    public BaseControl baseControl;

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

    private float currentReward;

    private int steps = 0;

    int minSteps = 0;
    public bool collisionState = false;

    // private List<CollisionCheck> colliders = new List<CollisionCheck>();
    private CollisionCheck[] colliders;
    private List<string> materialNames = new List<string> {"blue", "yellow"};
    private List<Material> mats = new List<Material>();

    public Material collisionMaterial;
    public Material defaultMaterial;

    private bool showColor = true;

    public override void Initialize()
    {
        addMaterials();
        registerColliders();
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly)
        {
            // is training set min num of steps to 5
            minSteps = 5;
        }
        configParams = Academy.Instance.EnvironmentParameters;
        envGenerator = this.transform.parent.Find("EnvGenerator").GetComponent<EnvGenerator>();
        target = envGenerator.target;

        startPosition = this.gameObject.transform.position;
        startRotation = this.gameObject.transform.eulerAngles;

        baseControl = gameObject.GetComponent<BaseControl>();
    }

    public override void OnEpisodeBegin()
    {
        // Debug.Log("Episode begin");
        steps = 0;
        setAgent(new Vector3(0f,1f,0f), new Vector3(0f,0f,1f), target.transform.localPosition);
        // envGenerator.getNewEnv(new Vector3(0f,1f,0f), new Vector3(0f,0f,1f), target.transform.localPosition);

        seamEnv envPara = xmlloader.getNewPoint();
        this.gameObject.transform.localPosition = envPara.pos;
        this.gameObject.transform.rotation = Quaternion.LookRotation(envPara.normal1, envPara.normal2);

        collisionState = CollisionCheck();
        Debug.Log($"Episode begin {this.angleVector}");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 normalizedAngles = this.transform.eulerAngles * 1/360;
        sensor.AddObservation(normalizedAngles);
        sensor.AddObservation(this.collisionState);
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

    bool ray_is_hit(RaycastHit hit)
    {
        return hit.collider != null;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        collisionState = CollisionCheck();
        // Debug.Log(CollisionCheck());
        calculateReward();
        if (steps > minSteps)
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
        if ((steps > 401))
        {
            Debug.Log($"failed {currentReward}");
            SetReward(-10f);
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
        SetReward(-0.01f);
    }

    public void calculateReward()
    {
        collisionState = CollisionCheck();
        if (!collisionState)
        {
            currentReward = 1f;
        }
        Vector3 currentAngle = rotationToHalf(this.transform.eulerAngles);
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
            collisionMaterial = Resources.Load("Materials/collisionMaterial", typeof(Material)) as Material;
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


