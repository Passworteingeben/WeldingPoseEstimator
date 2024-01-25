using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;
using UnityEditor;
using Math = System.Math;
using System;
using System.IO;


[Serializable]
public class ProblemParameters
{
    public Vector3 startPosition;
    public Vector3 startRotation;
    public Vector3 targetPosition;
    public Vector3 base1Position;
    public Vector3 base1Rotation;
    public Vector3 base1Scale;
    public Vector3 base2Position;
    public Vector3 base2Rotation;
    public Vector3 base2Scale;
    public List<ObstacleParameters> listOfObstacles = new List<ObstacleParameters>();

    public List<int[]> solutionSteps = new List<int[]>();

    public string serializedSolutionSteps;

    // public string serializedVectors;

    public ProblemParameters()
    {

    }

    public void serialize()
    {
        // to serialize solutionsteps convert to string
        this.serializedSolutionSteps = "";
        foreach (var obj in solutionSteps)
        {
            this.serializedSolutionSteps += string.Join(":", obj);
            this.serializedSolutionSteps += ";";
        }
        // List<string> vectorStrings = new List<string>();
        // vectorStrings.Add(this.startPosition.ToString("F6"));
        // vectorStrings.Add(this.startRotation.ToString("F6"));
        // vectorStrings.Add(this.targetPosition.ToString("F6"));
        // vectorStrings.Add(this.base1Position.ToString("F6"));
        // vectorStrings.Add(this.base1Rotation.ToString("F6"));
        // vectorStrings.Add(this.base1Scale.ToString("F6"));
        // vectorStrings.Add(this.base2Position.ToString("F6"));
        // vectorStrings.Add(this.base2Rotation.ToString("F6"));
        // vectorStrings.Add(this.base2Scale.ToString("F6"));
        // this.serializedVectors = String.Join(";", vectorStrings);
    }

    public void deserialize()
    {
        // to deserialize solutionsteps reconvert from string
        this.solutionSteps = new List<int[]>();
        string[] arrays = this.serializedSolutionSteps.Split(';');
        foreach (string arr in arrays)
        {
            if (arr.Length==0)
            {
                continue;
            }
            solutionSteps.Add(arr.Split(':').Select(stringDigit=>int.Parse(stringDigit)).ToArray());
        }
        // Debug.Log(this.serializedVectors);
        // List<string> vectorStrings = this.serializedVectors.Replace("(","").Replace(")","").Split(';').ToList();
        // this.startPosition = deserializeVector(vectorStrings[0]);
        // this.startRotation = rotationToHalf(deserializeVector(vectorStrings[1]));
        // this.targetPosition = deserializeVector(vectorStrings[2]);
        // this.base1Position = deserializeVector(vectorStrings[3]);
        // this.base1Rotation = rotationToHalf(deserializeVector(vectorStrings[4]));
        // this.base1Scale = deserializeVector(vectorStrings[5]);
        // this.base2Position = deserializeVector(vectorStrings[6]);
        // this.base2Rotation = rotationToHalf(deserializeVector(vectorStrings[7]));
        // this.base2Scale = deserializeVector(vectorStrings[8]);
    }

    // public Vector3 deserializeVector(string stringVec)
    // {
    //     Vector3 output = Vector3.zero;
    //     string[] vals = stringVec.Split(',');
    //     output.x = float.Parse(vals[0]);
    //     output.y = float.Parse(vals[1]);
    //     output.z = float.Parse(vals[2]);
    //     // Debug.Log($"Deserialize {vals[0]}");
    //     // Debug.Log($"Deserialize {output.x}");
    //     Debug.Log($"output {output}");
    //     return output;
    // }

    public static Vector3 rotationToHalf(Vector3 vec)
    {
        Vector3 tempV = Vector3.zero;
        tempV.x = vec.x <=180 ? vec.x : (vec.x-360)%360;
        tempV.y = vec.y <=180 ? vec.y : (vec.y-360)%360;
        tempV.z = vec.z <=180 ? vec.z : (vec.z-360)%360;
        return tempV;
    }
}

[Serializable]
public class ObstacleParameters
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public ObstacleParameters(GameObject obj)
    {
        this.position = obj.transform.localPosition;
        this.rotation = obj.transform.eulerAngles;
        this.scale = obj.transform.localScale;
    }
}

// // [Serializable]
// public class TrainerSolution
// {
//     public List<int[]> steps = new List<int[]>();
// }

class actionItem
{
    public string action;
    public Vector3 startRange;
    public Vector3 endRange;
    public actionItem(string action, Vector3 startRange, Vector3 endRange)
    {
        this.action = action;
        this.startRange = startRange;
        this.endRange = endRange;
    }

}


public class Legacy_BaseTrainer : MonoBehaviour
{
    public BaseControl baseControl;

    public EnvGenerator envGenerator;
    public Vector3 rangeStartPosition;
    public Vector3 rangeEndPosition;

    public Vector3 rangeStartRotation;
    public Vector3 rangeEndRotation;
    public GameObject targetObject;

    // can be set to angle bisector
    public Vector3 startPosition;
    public Vector3 startRotation;
    public Vector3 targetPosition;
    public Vector3 targetRotation;

    // public List<ProblemParameters> ListOfEnvironments = new List<ProblemParameters>();

    // public Rigidbody body;

    private List<int[]> currentSolutionSteps;
    private List<int[]> tempSteps = new List<int[]>(); 
    private float TRANSLATION_FACTOR =0.05f;
    private float ROTATION_FACTOR =0.25f;
    private bool targetReached = true;
    private bool collisionDetected = false;
    private float translationThreshold;
    private float rotationThreshold;
    private int[] actionArray = {0,0,0,0,0,0};

    private List<actionItem> actionParameters = new List<actionItem>();

    private actionItem currentAction;
    private int[] temparr;
    private float currentAngleDiff;
    private Vector3 currentAngle;
    
    public ProblemParameters currentEnv;

    public List<int[]> simSteps;
    public bool simulate = false;

    public int difficulty = 1;

    public int movementMode = 0; // 0 = do nothing in update, 1 = search for solution, 2 = simulation mode

    void Start()
    {
        baseControl = gameObject.GetComponent<BaseControl>();
        envGenerator = this.transform.parent.Find("EnvGenerator").GetComponent<EnvGenerator>();
        // ROTATION_FACTOR = baseControl.ROTATION_FACTOR;
        // TRANSLATION_FACTOR = baseControl.TRANSLATION_FACTOR;
        translationThreshold = 1*TRANSLATION_FACTOR; 
        rotationThreshold = 3*ROTATION_FACTOR; 
        // body = gameObject.GetComponent<Rigidbody>();

        // make sure that the ranges are correct
        // Vector3 temp = rangeStartPosition;
        // Vector3 temp2 = rangeEndPosition;
        // rangeStartPosition = new Vector3(Mathf.Min(temp[0],temp2[0]),
        //                                 Mathf.Min(temp[1],temp2[1]),
        //                                 Mathf.Min(temp[2],temp2[2]));
        // rangeStartPosition = new Vector3(Mathf.Max(temp[0],temp2[0]),
        //                                 Mathf.Max(temp[1],temp2[1]),
        //                                 Mathf.Max(temp[2],temp2[2]));
        // temp = rangeStartRotation;
        // temp2 = rangeEndRotation;
        // rangeStartRotation = new Vector3(Mathf.Min(temp[0],temp2[0]),
        //                                 Mathf.Min(temp[1],temp2[1]),
        //                                 Mathf.Min(temp[2],temp2[2]));
        // rangeStartRotation = new Vector3(Mathf.Max(temp[0],temp2[0]),
        //                                 Mathf.Max(temp[1],temp2[1]),
        //                                 Mathf.Max(temp[2],temp2[2]));
        // randomBaseRotation(rangeStartRotation, rangeEndRotation, 100);
        StartCoroutine(frameAfterStart());
        // StartCoroutine(runTest(1f));
        // StartCoroutine(runTest2(1f));
        // StartCoroutine(runTest3(1f));
        // StartCoroutine(runTest4(1f));
        // StartCoroutine(generateProblemSet(difficulty,90));
        // StartCoroutine(runSim(difficulty));
        StartCoroutine(generateProblemSetObstacles(difficulty,1));
    }

    IEnumerator frameAfterStart()
    {
        yield return new WaitForSeconds(0);
        // assign target gameobject the frame after start
        targetObject = envGenerator.transform.Find("target").gameObject;
        initSolution();
    }

    IEnumerator runTest(float waitTime)
    {
        // do some rotation and translations x times
        yield return new WaitForSeconds(0);
        Debug.Log("start random base rot");
        // get random starting pos
        actionParameters.Add(new actionItem("rotation", rangeStartRotation, rangeEndRotation));
        yield return new WaitForSeconds(0.1f);
        // move to random pos
        while(!targetReached)
        {
            yield return new WaitForSeconds(0.1f);
        }
        currentSolutionSteps.Clear();
        currentEnv.startPosition = startPosition;
        currentEnv.startRotation = startRotation;
        actionParameters.Add(new actionItem("translation", rangeStartPosition, rangeEndPosition));
        yield return new WaitForSeconds(0.1f);
        while(!targetReached)
        {
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(waitTime);
    }

    IEnumerator runTest2(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        Debug.Log("start random base rot");
        actionParameters.Add(new actionItem("rotation", rangeStartRotation, rangeEndRotation));
        yield return new WaitForSeconds(0.1f);
        while(!targetReached)
        {
            yield return new WaitForSeconds(0.1f);
        }
        currentSolutionSteps.Clear();
        currentEnv.startPosition = startPosition;
        currentEnv.startRotation = startRotation;
        actionParameters.Add(new actionItem("translation", rangeStartPosition/10, rangeEndPosition/10));
        // actionParameters.Add(new actionItem("translation", rangeStartPosition, rangeEndPosition));
        yield return new WaitForSeconds(0.1f);
        while(!targetReached)
        {
            yield return new WaitForSeconds(0.1f);
        }
        currentEnv.solutionSteps.AddRange(currentSolutionSteps);
    }
    IEnumerator runTest3(float waitTime)
    {
        int factor = 10;
        yield return new WaitForSeconds(waitTime);
        Debug.Log("start random base rot");
        actionParameters.Add(new actionItem("rotation", rangeStartRotation, rangeEndRotation));
        yield return new WaitForSeconds(0.1f);
        while(!targetReached)
        {
            yield return new WaitForSeconds(0.1f);
        }
        currentSolutionSteps.Clear();
        actionParameters.Add(new actionItem("translation", factor * rangeStartPosition/10, factor * rangeEndPosition/10));
        // actionParameters.Add(new actionItem("translation", rangeStartPosition, rangeEndPosition));
        yield return new WaitForSeconds(1f);
        while(!targetReached)
        {
            yield return new WaitForSeconds(0.1f);
        }
        currentEnv.startPosition = startPosition;
        currentEnv.startRotation = startRotation;
        currentEnv.solutionSteps.AddRange(currentSolutionSteps);
        currentEnv = envGenerator.generateEnv(currentEnv);
        Debug.Log($"Startposition {startPosition}");
        Debug.Log($"startRotation {startRotation}");
    }

    IEnumerator runTest4(float waitTime)
    {
        int factor = 5;
        yield return new WaitForSeconds(waitTime);
        Debug.Log("start random base rot");
        actionParameters.Add(new actionItem("rotation", rangeStartRotation, rangeEndRotation));
        yield return new WaitForSeconds(0.1f);
        while(!targetReached)
        {
            yield return new WaitForSeconds(0.1f);
        }
        currentSolutionSteps.Clear();
        actionParameters.Add(new actionItem("translation", factor * rangeStartPosition/10, factor * rangeEndPosition/10));
        // actionParameters.Add(new actionItem("translation", rangeStartPosition, rangeEndPosition));
        yield return new WaitForSeconds(1f);
        while(!targetReached)
        {
            yield return new WaitForSeconds(0.1f);
        }
        // simulate steps taken
        // this.transform.localPosition = currentEnv.startPosition;
        // this.transform.eulerAngles = currentEnv.startRotation;
        collisionDetected = false;
        targetReached = false;
        // runSteps(this.currentSolutionSteps);
    }

    IEnumerator generateProblemSet(int difficulty, int numToGenerate)
    {
        // difficulty goes from 1 - 10
        // wait for next frame
        yield return new WaitForSeconds(0.001f);
        // generate new problem
        for (int i = 0; i < numToGenerate; i++)
        {
            // get to default pos
            initSolution();
            // do some rotation
            Debug.Log("get random base rot");
            // get random starting pos
            actionParameters.Add(new actionItem("rotation", rangeStartRotation, rangeEndRotation));
            // wait for update to start rotation
            yield return new WaitForSeconds(0.1f);
            // wait for rotation to be complete
            while(!targetReached)
            {
                yield return new WaitForSeconds(0.1f);
            }
            // clear rotation and set this pos as startpos
            currentSolutionSteps.Clear();
            // move to random pos
            actionParameters.Add(new actionItem("translation", difficulty * rangeStartPosition/10, difficulty * rangeEndPosition/10));
            // wait for update to start translation
            yield return new WaitForSeconds(0.1f);
            // wait for translation to be complete
            while(!targetReached)
            {
                yield return new WaitForSeconds(0.1f);
            }
            currentEnv.solutionSteps.AddRange(currentSolutionSteps);
            currentEnv.startPosition = this.startPosition;
            currentEnv.startRotation = this.startRotation;
            // write solution down
            writeSolution(Path.Combine(Application.dataPath,"Solutions/ProblemSet"+difficulty.ToString()), "d_01_t_01_r_00");
        }
    }

    IEnumerator generateProblemSetObstacles(int difficulty, int numToGenerate)
    {
        // difficulty goes from 1 - 10
        // wait for next frame
        int padding = 5;
        yield return new WaitForSeconds(0.001f);
        // generate new problem
        for (int i = 0; i < numToGenerate; i++)
        {
            // get to default pos
            initSolution();
            // do some rotation
            Debug.Log("get random base rot");
            // get random starting pos
            movementMode = 1;
            actionParameters.Add(new actionItem("rotation", rangeStartRotation, rangeEndRotation));
            // wait for update to start rotation
            yield return new WaitForSeconds(0.1f);
            // wait for rotation to be complete
            while(!targetReached)
            {
                yield return new WaitForSeconds(0.1f);
            }
            // clear rotation and set this pos as startpos
            currentSolutionSteps.Clear();
            // move to random pos
            movementMode = 1;
            actionParameters.Add(new actionItem("translation", difficulty * rangeStartPosition/10, difficulty * rangeEndPosition/10));
            // wait for update to start translation
            yield return new WaitForSeconds(0.1f);
            // wait for translation to be complete
            while(!targetReached)
            {
                yield return new WaitForSeconds(0.1f);
            }
            currentEnv.solutionSteps.AddRange(currentSolutionSteps);
            currentEnv.startPosition = this.startPosition;
            currentEnv.startRotation = this.startRotation;
            GameObject hObs = envGenerator.addBaseHorizontalObstacle();
            Debug.Log(hObs.name);
            collisionDetected = false;
            float shiftFactor = 0.2f;
            while (shiftFactor > 0.05f)
            {
                while (!collisionDetected)
                {
                    Debug.Log("start next iter");
                    hObs.transform.localPosition += new Vector3(0,0,shiftFactor);
                    currentEnv.listOfObstacles[0] = new ObstacleParameters(hObs);
                    runSteps(currentEnv);
                    while(simulate)
                    {
                        yield return new WaitForSeconds(0.01f);
                    }
                    Debug.Log("target reached next iter");
                }
                Debug.Log(hObs.transform.localPosition);
                Debug.Log("collision detected move back");
                hObs.transform.localPosition -= new Vector3(0,0,shiftFactor);
                Debug.Log(hObs.transform.localPosition);

                shiftFactor = shiftFactor / 2;
                collisionDetected = false;
            }
            hObs.transform.localPosition -= new Vector3(0,0,0.05f * Random.Range(2,padding));
            // write solution down
            // writeSolution(Path.Combine(Application.dataPath,"Solutions/ProblemSetObstacles"+difficulty.ToString()), "d_01_t_01_r_00");

        }
    }

    IEnumerator runSim(int difficulty)
    {
        for (int i = 0; i < 10; i++)
        {
            string inputFilename = "./Assets/Solutions/ProblemSet" + difficulty.ToString() + "/d_01_t_01_r_00_" + i.ToString() + ".json"; 
            yield return new WaitForSeconds(0);
            readSolution(inputFilename);
            Debug.Log("start sim");
            runSteps(this.currentEnv);
            yield return new WaitForSeconds(3);
        }
    }    

    void Update()
    {
        if (movementMode == 1)
        {
            // search for solution mode
            if (collisionDetected)
            {
                Debug.Log("reset state");
                // reset
                resetState();
                // repeat old action
                doAction();
            }
            else
            {
                if (targetReached)
                {
                    if (tempSteps.Count>0)
                    {
                        Debug.Log("accept state");
                        // accept sol
                        acceptState();
                        movementMode = 0;
                    }
                    // get new action if exist
                    if (actionParameters.Count>0)
                    {
                        Debug.Log("get new action");
                        // pop next action
                        currentAction = actionParameters[0];
                        actionParameters.RemoveAt(0);
                        doAction();                   
                    }
                }
                else
                {
                    // Debug.Log($"This is target pos {targetPosition}, current pos {this.transform.localPosition}");
                    if ((targetPosition-this.transform.localPosition).magnitude > translationThreshold)
                    {
                        // Debug.Log("do translation");
                        // Debug.Log($"This is dist {this.transform.localPosition-targetPosition}");
                        // Debug.Log($"This is target pos {targetPosition}, current pos {this.transform.localPosition}");
                        actionArray = convertTranslation(this.transform.localPosition, targetPosition, this.gameObject.transform.forward, this.gameObject.transform.up, this.gameObject.transform.right);
                        // foreach (var obj in actionArray)
                        // {
                        //     Debug.Log(obj);
                        // }
                    }
                    else
                    {   
                        actionArray[0] = 0;
                        actionArray[1] = 0;
                        actionArray[2] = 0;
                    }
                    // // convert angle to 180 to -180
                    currentAngle = rotationToHalf(this.transform.eulerAngles);
                    currentAngleDiff= (targetRotation-currentAngle).magnitude;
                    // Debug.Log($"This is target rot {targetRotation}, current rot {currentAngle}, this is magn: {currentAngleDiff}");
                // Debug.Log($"This is target rot {targetRotation}, current rot {currentAngle}, this is magn: {currentAngleDiff}");
                    if ( currentAngleDiff > rotationThreshold)
                    {
                        temparr = convertRotation(currentAngle, targetRotation);
                        actionArray[3] = temparr[0];
                        actionArray[4] = temparr[1];
                        actionArray[5] = temparr[2];
                    }
                    else
                    {
                        actionArray[3] = 0;
                        actionArray[4] = 0;
                        actionArray[5] = 0;
                    }
                    // actionArray.Select( x => x != -1 ? x : 2).ToArray(); 
                    tempSteps.Add(actionArray);
                    baseControl.moveAgent(actionArray);

                    if ((targetPosition-this.transform.localPosition).magnitude < translationThreshold && (currentAngleDiff< rotationThreshold))
                    {
                        Debug.Log("targetReached");
                        // if target reached
                        targetReached=true;
                    }
                }
            }
        }
        if (movementMode == 2)
        {
            // simulation mode
            if (simulate)
            {
                if (simSteps.Count > 0 && !collisionDetected)
                {
                    // pop last sim step and move 
                    actionArray = simSteps[simSteps.Count-1];
                    simSteps.RemoveAt(simSteps.Count-1);
                    baseControl.moveAgent(actionArray);
                }
                else
                {
                    Debug.Log($"collision detection {collisionDetected}");
                    // stop simultation
                    simulate = false;
                    movementMode = 0;
                }
            }
        }
    }

    public void resetState()
    {
        // reset pos and rot
        this.targetPosition = startPosition;
        this.targetRotation = startRotation;
        this.transform.localPosition = startPosition;
        this.transform.eulerAngles = startRotation;
        // reset tempSteps
        tempSteps.Clear();
        // // start movement
        // targetReached = false;
        // collisionDetected=false;
    }

    public void acceptState()
    {
        // set new pos and rot
        startPosition = this.transform.localPosition;
        startRotation = rotationToHalf(this.transform.eulerAngles);
        // add new steps to solution
        // Debug.Log($"num of steps added {tempSteps.Count}");
        this.currentSolutionSteps.AddRange(tempSteps);
        tempSteps.Clear();
    }

    public void doAction()
    {
        Debug.Log(currentAction.action);
        if (currentAction.action =="rotation")
        {
            randomBaseRotation(currentAction.startRange, currentAction.endRange); 
        }
        if (currentAction.action =="translation")
        {
            randomBaseTranslation(currentAction.startRange, currentAction.endRange); 
        }
        // start movement
        targetReached = false;
        collisionDetected = false;
        movementMode = 1;
    }


    public void initSolution()
    {
        collisionDetected = false;

        GameObject floor = envGenerator.transform.Find("floor").gameObject;
        GameObject wall = envGenerator.transform.Find("wall").gameObject;
        currentSolutionSteps = new List<int[]>();
        tempSteps.Clear();
        // not relevant if rigidbody is kinematic
        // body.angularVelocity = Vector3.zero;
        // body.velocity = Vector3.zero;
        // this.gameObject.transform.localPosition = new Vector3(0,0.5f,1.25f);
        // Debug.Log(targetObject.transform.localPosition);
        // Debug.Log(targetObject.transform.position);
        this.transform.localPosition = targetObject.transform.localPosition+new Vector3(0,0.1f,0.1f);
        this.transform.localPosition = targetObject.transform.localPosition+new Vector3(0,0.1f,0.1f);
        this.transform.eulerAngles = new Vector3(-45f,0,0);
        startPosition = this.transform.localPosition;
        startRotation = rotationToHalf(this.transform.eulerAngles);
        this.targetPosition = startPosition;
        this.targetRotation = startRotation;
        currentEnv = new ProblemParameters();
        currentEnv = envGenerator.generateEnv(currentEnv);
        currentEnv.startPosition = startPosition;
        currentEnv.startRotation = startRotation;
        // currentEnv.targetPosition = startPosition;
        // currentEnv.targetRotation = startRotation;
    }
    
    public void writeSolution(string outputDir, string outputFilename)
    {
        // write down current Env to file
        // set env variables
        currentEnv = envGenerator.generateEnv(currentEnv);
        // serialize current env
        currentEnv.serialize();
        // get json data from env
        string jsonString = JsonUtility.ToJson(currentEnv);

        // write json to file
        Directory.CreateDirectory(outputDir);
        // Debug.Log("this is output Dir : " + outputDir);
        // Debug.Log($"num of dir: {Directory.GetFiles(outputDir).Where(fname => fname.EndsWith(".json")).ToList().Count}");
        outputFilename += "_" + Directory.GetFiles(outputDir).Where(fname => fname.EndsWith(".json")).ToList().Count.ToString() + ".json";
        string outputPath = Path.Combine(outputDir, outputFilename);
        File.WriteAllText(outputPath, jsonString);
        // Debug.Log(outputPath);
    }
    
    public void acceptSolution()
    {
        // TODO
    }

    public void readSolution(string inputFilename)
    {
        Debug.Log($"current pos : {this.transform.localPosition}");
        Debug.Log($"current rot : {this.transform.eulerAngles}");
        if (File.Exists(inputFilename))
        {
            // Read the entire contents of the file as a string
            string jsonString = File.ReadAllText(inputFilename);
            Debug.Log(jsonString);
            // ProblemParameters tempenv= JsonUtility.FromJson<ProblemParameters>(jsonString);
            // tempenv.deserialize();
            // Debug.Log(tempenv.serializedVectors);
            // Debug.Log($"env pos : {tempenv.startPosition}");
            this.currentEnv = JsonUtility.FromJson<ProblemParameters>(jsonString);
            this.currentEnv.deserialize();
            // Debug.Log(this.currentEnv.serializedVectors);
            // Debug.Log($"env pos : {this.currentEnv.startPosition}");
        }
        else
        {
            Debug.LogError("Filepath not found : " + inputFilename);
        }
    }

    public void runSteps(ProblemParameters env)
    {
        envGenerator.setEnv(env);
        this.transform.localPosition = env.startPosition;
        this.transform.eulerAngles = env.startRotation;
        Debug.Log($"current pos : {this.transform.localPosition}");
        Debug.Log($"current rot : {this.transform.eulerAngles}");
        List<int[]> reverseSteps = new List<int[]>();
        for (int i=0; i < env.solutionSteps.Count ; i++)
        {
            int[] actionArr = new int[6];
            // Debug.Log("input " + string.Join(" ", env.solutionSteps[i]));
            for (int j=0; j < env.solutionSteps[i].Length; j++)
            {
                actionArr[j] = - env.solutionSteps[i][j]; 
            }
            // Debug.Log("output " + string.Join(" ", actionArr));
            reverseSteps.Add(actionArr);
        }
        Debug.Log($"num of steps loaded {reverseSteps.Count}");
        simSteps = reverseSteps;
        collisionDetected = false;
        targetReached = false;
        simulate = true;
        movementMode = 2;
    }

    public int[] convertTranslation(Vector3 vec1, Vector3 vec2, Vector3 base1, Vector3 base2, Vector3 base3)
    {
        int[] actionArr = {0,0,0,0,0,0};
        Vector3 vec = vec2-vec1;
        float magn = vec.magnitude;
        base1 = base1*TRANSLATION_FACTOR;
        base2 = base2*TRANSLATION_FACTOR;
        base3 = base3*TRANSLATION_FACTOR;
        if ((vec - base1).magnitude < magn)
        {
            actionArr[0] = 1;
        }
        if ((vec + base1).magnitude < magn)
        {
            actionArr[0] = -1;
        }
        if ((vec - base2).magnitude < magn)
        {
            actionArr[1] = 1;
        }
        if ((vec + base2).magnitude < magn)
        {
            actionArr[1] = -1;
        }
        if ((vec - base3).magnitude < magn)
        {
            actionArr[2] = 1;
        }
        if ((vec + base3).magnitude < magn)
        {
            actionArr[2] = -1;
        }
        return actionArr;
    } 
    
    public static Vector3 rotationToHalf(Vector3 vec)
    {
        Vector3 tempV = Vector3.zero;
        tempV.x = vec.x <=180 ? vec.x : (vec.x-360)%360;
        tempV.y = vec.y <=180 ? vec.y : (vec.y-360)%360;
        tempV.z = vec.z <=180 ? vec.z : (vec.z-360)%360;
        return tempV;
    }
    public int[] convertRotation(Vector3 vec1, Vector3 vec2)
    {
        Vector3 vec = vec2-vec1;
        // Debug.Log($"This is target rot {vec2}, current rot {vec1}, this is magn: {vec.magnitude}");
        return new int[]{Math.Sign(vec.x),Math.Sign(vec.y),Math.Sign(vec.z)};
    } 

    public void randomBaseRotation(Vector3 rangeRotation1, Vector3 rangeRotation2, int numOfTries = 100)
    {
        // initSolution();
        // new steps to be added if pos is found
        // startPosition = this.transform.localPosition;
        // startRotation = this.transform.eulerAngles;
        
        // generate new angle to move to
        targetRotation.x = (Random.Range(rangeRotation1[0],rangeRotation2[0]));
        targetRotation.y = (Random.Range(rangeRotation1[1],rangeRotation2[1]));
        targetRotation.z = (Random.Range(rangeRotation1[2],rangeRotation2[2]));

        // Debug.Log($"This is target angle {targetRotation}");
        // Debug.Log($"This is euler angle {this.transform.eulerAngles}");
        // Debug.Log($"This is dist {this.transform.eulerAngles-targetRotation}");
    }

    IEnumerator takeStep()
    {
        // TODO is this needed?
        // wait single frame
        yield return 0;
        Debug.Log("start random base rot");
        randomBaseRotation(rangeStartRotation, rangeEndRotation);
        // targetObject = envGenerator.transform.Find("target").gameObject;
        // initSolution();
    }

    public void randomBaseTranslation(Vector3 rangeTranslation1, Vector3 rangeTranslation2)
    {
        // generate new translation
        targetPosition.x = Random.Range(rangeTranslation1[0],rangeTranslation2[0]);
        targetPosition.y = Random.Range(rangeTranslation1[1],rangeTranslation2[1]);
        targetPosition.z = Random.Range(rangeTranslation1[2],rangeTranslation2[2]);
        // this.gameObject.transform.localPosition = translationVector;
        // visualize target
        // GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        // cube.name = name;
        // cube.transform.SetParent(this.transform.parent);
        // cube.transform.localPosition = targetPosition;
        // cube.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
    }

    public void CollisionCheck(Collision collision)
    {
        // Debug.Log("obj hit: " + collision.gameObject.name);
        if (collision.gameObject.tag == "obstacle")
        {
            Debug.Log("obstacle hit: " +collision.gameObject.name);
            collisionDetected = true;            
        }
    }
}
