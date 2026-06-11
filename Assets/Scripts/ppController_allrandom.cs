using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using UnityEngine.InputSystem.Controls;
using Unity.Mathematics;
using InputDevice = UnityEngine.XR.InputDevice;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Data.Common;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using Demo;

//using UnityEngine.XR.OpenXR.Features.Interactions;

public class ppController_allrandom: MonoBehaviour
{
    public GameObject virtualPrefab;

    public GameObject physicalPrefab;
    public float offsetToML2;
    public float ringSize = 1.0f;
    public int numPingPongBalls = 24;
    public float nearRingDepth = 0.74f;
    public float farRingDepth = 1.5f;

    public int numSelections=25;
    //public float defaultDistanceInFront = 1.0f;
    private bool isLastSelectionFinished = true;
    private bool isTaskStarted = false;
    private List<GameObject> pingPongBalls = new List<GameObject>();

    public Camera mainCamera;

    
    public TMP_Text targetText;

    public TMP_Text blockText;

    // public Material greenMat;

    // public Material redMat;

    // public Material defaultMat;

    private GameObject parentObject;
    private int currentTargetIndex;
    private List<int> randomTargets = new List<int>();
    private Vector3 nearScale;
    private Vector3 farScale;
    private GameObject initialCamera;
    private MagicLeapInputs mlInputs;
    private MagicLeapInputs.ControllerActions _controller;

    private MagicLeapInputs.EyesActions eyesActions;
    
    public GameObject eyetrackingVisualizer;

    private InputDevice eyeTrackingDevice;
    public bool EyeTrackingPermissionGranted { get; private set; }
    public Vector3 GazePosition { get; private set; }
    public Quaternion GazeRotation { get; private set; }
    private GameObject fixationSphere;

    private GameObject lastSelectedTarget;
    
    public TMP_InputField participantIDInput; // Add a UI input field for participant ID
    private bool isParticipantIDEntered=false;

    private int participantID;

  
    private bool isEyetrackingEnabled = true;

   
    // Task Data Variables
    private TaskData_allrandom taskData = new TaskData_allrandom();
    private float taskStartTime;
    private float lastSelectionTime; 

    private float showTargetTime; //to record the time when countdown finished and target number shows up
    private float selectionFinishTime; //to record the time when click the controller trigger

    private int TaskConditionID=0;

    private int currentBlockID=1;
    private int TaskPhysicalityID=0;
    private int TaskDepthID=0;
    
    private bool isSceneCalibrated=false;

    private bool isDepthCalibrated=false;



    private bool isApplicationStarted=false;

    private bool isConditionOngoing=false;

    private bool isBlockStarted = false;

   


 

    public TMP_Text completionText;

    //public TMP_Text eyetracking_Text;

    private UnityEngine.InputSystem.XR.Eyes eyes;
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();


    private GameObject circle;


    private Transform circle_transform;

    private Vector3 circleCenterPos;

    private GameObject target;

    public GameObject controller;

    public GameObject calibration_near;
    public GameObject calibration_far;

    private Coroutine countdownCoroutine;


    private Vector3 lastControllerPosition; 
    private Vector3 lastHeadPosition;
    private Quaternion lastHeadRotation;

    private quaternion lastControllerRotation;
    private float accumulatedHeadMovement = 0f;
    private float accumulatedHeadRotation = 0f;
    private float accumulatedControllerMovement=0f;

    private float accumulatedControllerRotation=0f;

    private Vector3 calibrateOffset;

    public GameObject viz;



    void Start()
    {
        mainCamera.GetComponent<Camera>().fieldOfView = 45;
        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();
        _controller = new MagicLeapInputs.ControllerActions(mlInputs);
        _controller.Bumper.performed += HandleOnBumper;
        _controller.Trigger.performed += HandleOnTrigger;

      

        MLPermissions.RequestPermission(MLPermission.EyeTracking, permissionCallbacks);

        if(MLPermissions.CheckPermission(MLPermission.EyeTracking).IsOk){
            //eyetracking_Text.text="Eye Tracking permission denied";
            Debug.Log("eye tracking did not set up");
        }else{
            //eyetracking_Text.text="eye tracking successfully set up";
            Debug.Log("eye tracking successfully set up");
        }

        fixationSphere = Instantiate(eyetrackingVisualizer, new Vector3(0, 0, 0.4f), Quaternion.identity);
        
        fixationSphere.GetComponent<Renderer>().material.color = Color.green;

        
        
        //eyetracking_Text.text="if eye gaze permission granted: "+EyeTrackingPermissionGranted;

        
    }
    private void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        
    }
    void Update()
    {
        //blockText.text="current condition="+TaskConditionID+", current block ID="+currentBlockID;
        if(Input.GetKeyDown(KeyCode.Space)){
            if(isSceneCalibrated==false&&isDepthCalibrated==true){
                isSceneCalibrated=true; //confirm all near and far calibrations are done
                if(isApplicationStarted&&isConditionOngoing==false){
                    RestartTask();

                }else{
                    targetText.text="Press 'F'";
                }
            }else if(isSceneCalibrated==false&&isDepthCalibrated==false){
                isDepthCalibrated=true;   //confirm far calibration is done
                circleCenterPos=circle.transform.position;
                circle_transform=circle.transform;
                Debug.Log("circle_right="+circle_transform.right+", circle_up="+circle_transform.up);
                
                targetText.text="Calibration Done! Press 'Space' to start the condition";
                
            }
        }

        if(Input.GetKeyDown(KeyCode.W)){
            if(!isSceneCalibrated){
                circle.transform.position += new Vector3(0.0f, 0.002f, 0.0f);
            }
        }

        if(Input.GetKeyDown(KeyCode.A)){
            if(!isSceneCalibrated){
                circle.transform.position -= new Vector3(0.002f, 0.0f, 0.0f);
            }
        }

        if(Input.GetKeyDown(KeyCode.S)){
            if(!isSceneCalibrated){
                circle.transform.position -= new Vector3(0.0f, 0.002f, 0.0f);
            }
        }

        if(Input.GetKeyDown(KeyCode.D)){
            if(!isSceneCalibrated){
                circle.transform.position += new Vector3(0.002f, 0.0f, 0.0f);
            }
        }

        if(Input.GetKeyDown(KeyCode.Q)){
            if(!isSceneCalibrated){
                circle.transform.Rotate(0.0f, -1.0f, 0.0f);
            }
        }

        if(Input.GetKeyDown(KeyCode.E)){
            if(!isSceneCalibrated){
                circle.transform.Rotate(0.0f, 1.0f, 0.0f);
            }
        }

        if(Input.GetKeyDown(KeyCode.F)){
            if(isSceneCalibrated){
                startApplication();

            }
            

        }

        if(Input.GetKeyDown(KeyCode.O)){
            if(isSceneCalibrated){
                StartEachConditionTask();
            }
            

        }

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse Click Detected");
            HandleMouseClick();
            
        }

        if(Input.GetKeyDown(KeyCode.N)){
            if(currentBlockID==2){
                SetNextCondition();
            }else{
                SetNextBlock();
            }
            
        }

        if(Input.GetKeyDown(KeyCode.M)){
            //go to last condition
            goLastCondition();
        }

        if(Input.GetKeyDown(KeyCode.P)){
            Application.Quit();
        }

        if(Input.GetKeyDown(KeyCode.R)){
            RestartApplication();
        }


        


        

        // Calculate movement distance
        Vector3 currentHeadPosition = mainCamera.transform.position;
        float movementDistance = Vector3.Distance(lastHeadPosition, currentHeadPosition);
        accumulatedHeadMovement += movementDistance;
        

        // Calculate rotation difference as the norm of quaternion difference
        Quaternion currentHeadRotation = mainCamera.transform.rotation;
        float rotationAngle = Quaternion.Angle(lastHeadRotation, currentHeadRotation);
        accumulatedHeadRotation += rotationAngle;

        Vector3 currentControllerPosition=controller.transform.position;
        float controllerMovment=Vector3.Distance(lastControllerPosition, currentControllerPosition);
        accumulatedControllerMovement+=controllerMovment;

        quaternion currentControllerRotation=controller.transform.rotation;
        float controllerRotationAngle=Quaternion.Angle(lastControllerRotation, currentControllerRotation);
        accumulatedControllerRotation+=controllerRotationAngle;

        // Update the last position and rotation
        lastHeadPosition = currentHeadPosition;
        lastHeadRotation = currentHeadRotation;
        lastControllerPosition = currentControllerPosition;
        lastControllerRotation=currentControllerRotation;
        

        

        // Eye tracking

        if (!EyeTrackingPermissionGranted)
        {
            return;
        }

        if (!eyeTrackingDevice.isValid)
        {
            this.eyeTrackingDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.TrackedDevice);
            return;
        }

        
        //eyetracking_Text.text="eyetrakcingdevice.isvalid is"+eyeTrackingDevice.isValid;

        //EYE TRACKING
        // Eye data provided by the engine for all XR devices.
        // Used here only to update the status text. The 
        // left/right eye centers are moved to their respective positions &
        // orientations using InputSystem's TrackedPoseDriver component.
        eyes = eyesActions.Data.ReadValue<UnityEngine.InputSystem.XR.Eyes>();

    

        // Eye data specific to Magic Leap
        InputSubsystem.Extensions.TryGetEyeTrackingState(eyeTrackingDevice, out var trackingState);
           
            Vector3 fixationPos = eyes.fixationPoint;
            Vector3 leftEyePos=eyes.leftEyePosition;
            Quaternion leftEyeRot=eyes.leftEyeRotation;
            Vector3 rightEyePos=eyes.rightEyePosition;
            Quaternion rightEyeRot=eyes.rightEyeRotation;

            //taskData.fixationPoints.Add(fixationPos);

            if (isEyetrackingEnabled)
            {
                fixationSphere.transform.position = fixationPos;
            }
        

        

        Debug.Log("Gaze Fixation Point"+eyes.fixationPoint+"confidence: "+trackingState.FixationConfidence);


        
        
    }

    public void startApplication(){
        if(isApplicationStarted==false){
            //store the initial data of the mainCamera when is ready to start the application
            initialCamera=new GameObject("TempCamera");
            initialCamera.transform.position=mainCamera.transform.position;
            initialCamera.transform.rotation=mainCamera.transform.rotation;
            InitializationBalls();
            isApplicationStarted=true;

            targetText.text="Press 'O' to start";
        }

        

        
        
    }

    public void RestartTask(){
        
        isLastSelectionFinished=true;
        // randomTargets.Clear();
        // clearAllBalls(); // clear all children objects in the parent object



        // Assign the first condition to start with
        InitializationBalls();
        InitializeTask();
    }
    public void Exit(){
        Application.Quit();

    }
    public void StartEachConditionTask(){
      
        if(!isTaskStarted){
            isTaskStarted=true;
            Debug.Log("start to call InitializeTask()");
            InitializeTask();
        }

        taskStartTime=Time.time;
        Debug.Log("Task Started");
        taskData.taskStartTime = taskStartTime;
        Debug.Log("start to record the taskStartTime");


        

        


    }

    

    void calibrate(){

        if(circle!=null){
                Destroy(circle);
            }
        if(isDepthCalibrated==false&&isConditionOngoing==false){
            
            //targetText.text="Please calibrate the scene with researcher's help";
            if(taskOrder[TaskConditionID] == TaskCondition.Near_AllVirtual || taskOrder[TaskConditionID] == TaskCondition.Near_AllPhysical || taskOrder[TaskConditionID] == TaskCondition.Near_HalfVirtualHalfPhysical)
            {
                calibrateOffset=new Vector3(0, 0, nearRingDepth);
                circle = Instantiate(calibration_near, Vector3.zero+calibrateOffset, Quaternion.identity);

                
            }else{
                calibrateOffset=new Vector3(0, 0, farRingDepth);
                circle = Instantiate(calibration_far, Vector3.zero+calibrateOffset, Quaternion.identity);
                
            }

            targetText.text="Calibration";
            
            targetText.transform.rotation=Quaternion.identity;
            //targetText.transform.position=circle.transform.position+calibrateOffset;
            
        }
        
    }


    void InitializationBalls(){

        ApplyCondition(taskOrder[TaskConditionID]);
        // Instantiate parent objects to hold each set of ping pong balls
        if(parentObject==null){
            parentObject = new GameObject("parentPingpongBalls");
        }
        
        InstantiateBalls(); // Create the ping pong balls
        Debug.Log("InstantiateBalls");
        PositionBalls(initialCamera); // Position them in their respective circles
        



    }

    void InstantiateBalls()
    {
        pingPongBalls.Clear();
        if (taskOrder[TaskConditionID] == TaskCondition.Near_AllVirtual || taskOrder[TaskConditionID] == TaskCondition.Far_AllVirtual)
        {
            // Randomly generate virtual spheres for "All Virtual" conditions
            List<int> numbers = new List<int>();
            for (int i = 1; i <= numPingPongBalls; i++) numbers.Add(i);
            ShuffleList(numbers);

            // Generate physical spheres for "All Virtual" conditions, randomly assign the label to the sequential order of balls
            for (int i = 0; i < numPingPongBalls; i++)
            {
                GameObject newBall = Instantiate(virtualPrefab, Vector3.zero, Quaternion.identity, parentObject.transform);
                newBall.name = "PingPongBall_" + (i + 1);
                TMP_Text idTextFixed = newBall.GetComponentInChildren<TMP_Text>();
                if (idTextFixed != null) idTextFixed.text = numbers[i].ToString();
                pingPongBalls.Add(newBall);

                
            }
        }
        else if (taskOrder[TaskConditionID] == TaskCondition.Near_AllPhysical || taskOrder[TaskConditionID] == TaskCondition.Far_AllPhysical)
        {

            // Randomly generate virtual spheres for "All Virtual" conditions
            List<int> numbers = new List<int>();
            for (int i = 1; i <= numPingPongBalls; i++) numbers.Add(i);
            ShuffleList(numbers);

            // Generate physical spheres for "All Physical" conditions, randomly assign the label to the sequential order of balls
            for (int i = 0; i <= numPingPongBalls; i++)
            {
                GameObject newBall = Instantiate(physicalPrefab, Vector3.zero, Quaternion.identity, parentObject.transform);
                newBall.name = "PingPongBall_" + (i + 1);
                TMP_Text idTextFixed = newBall.GetComponentInChildren<TMP_Text>();
                if (idTextFixed != null) idTextFixed.text = numbers[i].ToString();
                pingPongBalls.Add(newBall);
            }
        }
        else
        {
            List<string> transitions = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                transitions.Add("00");
                transitions.Add("01");
                transitions.Add("10");
                transitions.Add("11");
            }

            // Physicality list: 1 for Physical, 0 for Virtual
            List<int> physicalityList = new List<int>();

            // Randomly assign the physicality of the first object
            physicalityList.Add(UnityEngine.Random.Range(0, 2)); // 1 for Physical, 0 for Virtual

            // Generate the sequence of transitions
            for (int i = 1; i < numPingPongBalls; i++)
            {
                int previousPhysicality = physicalityList[i - 1]; // Previous object's type
                // Randomly pick a transition and remove it from the list
                int currentPhysicality = UnityEngine.Random.Range(0, 2);
                // Determine the resulting transition
                string currentTransition = (previousPhysicality == 1 ? "1" : "0") + (currentPhysicality == 1 ? "1" : "0");
                if (transitions.Contains(currentTransition))
                {
                    // If valid, add the current type and remove the transition from the list
                    physicalityList.Add(currentPhysicality);
                    transitions.Remove(currentTransition);
                }
                else
                {
                    // If invalid, assign the opposite physicality
                    currentPhysicality = 1 - currentPhysicality; // Flip 0 to 1 or 1 to 0
                    physicalityList.Add(currentPhysicality);

                    // Remove the valid transition for the flipped type
                    currentTransition = (previousPhysicality == 1 ? "1" : "0") + (currentPhysicality == 1 ? "1" : "0");
                    transitions.Remove(currentTransition);
                }
            }
            
            
            for (int i = 0; i < numPingPongBalls; i++)
            {
                GameObject newBall = Instantiate(
                    physicalityList[i] == 1 ? physicalPrefab : virtualPrefab,
                    Vector3.zero, // Position will be handled in PositionBallSet
                    Quaternion.identity,
                    parentObject.transform
                );

                newBall.name = "PingPongBall_" + (i + 1);
                TMP_Text idTextFixed = newBall.GetComponentInChildren<TMP_Text>();
                if (idTextFixed != null) idTextFixed.text = (i + 1).ToString();

                pingPongBalls.Add(newBall);
            }

            // Shuffle the pingPongBalls list to randomize the label order
            ShuffleListForGameobject(pingPongBalls);


            
        }
        
        // Set the base scale based on the prefab's original scale
        if (virtualPrefab != null)
        {
            nearScale = virtualPrefab.transform.localScale;
        }
    }


    

    void PositionBalls(GameObject currentCamera)
    {
        if (TaskDepthID == 0)
        {
            // Position the near depth set in a circle
            PositionBallSet(pingPongBalls, nearRingDepth, nearScale, ringSize, currentCamera, circleCenterPos);
        }
        else
        {
            // Position the far depth set in a circle
            PositionBallSet(pingPongBalls, farRingDepth, nearScale, ringSize, currentCamera, circleCenterPos);
        }

        Debug.Log("PositionBalls");

    }

    void PositionBallSet(List<GameObject> ballSet, float ringDepth, Vector3 ballScale, float ringRadius, GameObject cam, Vector3 centerPos)
    {
        float alpha = Mathf.PI * 2 / numPingPongBalls; // Calculate angular spacing dynamically based on the number of balls

        // Calculate the center position for the circle in front of the camera at the specified depth
        //Vector3 centerPosition = cam.transform.position + cam.transform.forward * ringDepth;
        Vector3 centerPosition=centerPos;

        // Calculate the vector from the camera to the circle center
        Vector3 toRingCenter = centerPosition - cam.transform.position;

        // Determine the right and up vectors for defining the plane perpendicular to 'toRingCenter'
        // Vector3 right = Vector3.Cross(toRingCenter.normalized, Vector3.up).normalized; // Right vector perpendicular to 'toRingCenter'
        // Vector3 up = Vector3.Cross(right, toRingCenter.normalized).normalized; // Up vector in the plane of the circle
        float angularSizeX = 2 * Mathf.Atan((ballScale.x / 2) / nearRingDepth) * Mathf.Rad2Deg;
        float angularSizeY = 2 * Mathf.Atan((ballScale.y / 2) / nearRingDepth) * Mathf.Rad2Deg;

        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;
        Vector3 cameraUp = cam.transform.up;
        // Calculate the scale adjustment to maintain the visual size based on effective distance
        // float effectiveDistanceToRingCenter = toRingCenter.magnitude; // The distance from the camera to the circle's center
        // float scaleAdjustment = effectiveDistanceToRingCenter / nearRingDepth; // Scale adjustment to keep visual size consistent
        float scaleAdjustment=ringDepth / nearRingDepth;
        Vector3 adjustedScale = ballScale * scaleAdjustment;
        float adjustedRingSize = ringRadius * scaleAdjustment;

        // Position the coins on a plane perpendicular to 'toRingCenter'
        for (int i = 0; i < ballSet.Count; i++)
        {
            float angle = alpha * i; 
            // Calculate position in the circle plane using 'right' and 'up' vectors
            //Vector3 circlePosition = cameraRight * Mathf.Cos(angle) * adjustedRingSize + cameraUp * Mathf.Sin(angle) * adjustedRingSize;
            //Vector3 circlePosition = Vector3.right * Mathf.Cos(angle) * adjustedRingSize + Vector3.up * Mathf.Sin(angle) * adjustedRingSize;
            Vector3 circlePosition = circle_transform.right * Mathf.Cos(angle) * adjustedRingSize + circle_transform.up * Mathf.Sin(angle) * adjustedRingSize;
            ballSet[i].transform.position = centerPosition + circlePosition; // Position relative to the circle's center
            ballSet[i].transform.localScale = CalculateScaleForDepth(new Vector2(angularSizeX, angularSizeY), ringDepth, ballScale); // Adjust the ball scale based on distance
            //ballSet[i].transform.rotation=cam.transform.rotation;
            var size=ballSet[i].transform.GetComponentInChildren<MeshRenderer>().bounds.size;
            
            
            
        }

        // Store the scale for reference
        farScale = CalculateScaleForDepth(new Vector2(angularSizeX, angularSizeY), ringDepth, ballScale);
        targetText.transform.position=centerPosition;
        float targetScale=0.0015f;
        targetText.transform.localScale=new Vector3(targetScale*farScale.x, targetScale*farScale.x, targetScale*farScale.x);


        controller.GetComponent<XRInteractorLineVisual>().lineWidth=0.005f*farScale.x;
        //targetText.transform.rotation=cam.transform.rotation;
        //targetText.gameObject.transform.localScale/=scaleAdjustment;
        Debug.Log("PositionBallSets");
        
    }

    Vector3 CalculateScaleForDepth(Vector2 angularSize, float depth, Vector3 referenceSize)
    {
        float sizeInMetersX = 2 * depth * Mathf.Tan(angularSize.x * Mathf.Deg2Rad / 2);
        float sizeInMetersY = 2 * depth * Mathf.Tan(angularSize.y * Mathf.Deg2Rad / 2);

        // Calculate the scale ratio for each axis based on the original size of the near disk
        float scaleRatioX = sizeInMetersX / referenceSize.x;
        float scaleRatioY = sizeInMetersY / referenceSize.y;

        // Return the new scale for the disk at the specified depth
        return new Vector3(scaleRatioX * referenceSize.x, scaleRatioY * referenceSize.y, referenceSize.z);
    }

    void clearAllBalls(){
        // Destroy all children of parentObject1
        DestroyChildren(parentObject);


    }

    private void DestroyChildren(GameObject parentObject)
    {
        // Loop through each child object and destroy it
        foreach (Transform child in parentObject.transform)
        {
            Destroy(child.gameObject);
        }
        
    }

    void InitializeTask()
    {
        isConditionOngoing=true;

        //************record the data************

        ConditionData_allrandom currentCondition;

        // Check if the data of current condition already exists
        if (TaskConditionID < taskData.conditions.Count)
        {
            currentCondition = taskData.conditions[TaskConditionID];
        }
        else
        {
            // Create a new condition data if not present
            currentCondition = new ConditionData_allrandom
            {
                conditionID = TaskConditionID,
                depth = TaskDepthID,
                physicality = TaskPhysicalityID,
                conditionStartTime = Time.time,
            };
            taskData.conditions.Add(currentCondition);
        }

        // Create a new block for the current condition
        BlockData_allrandom currentBlock = new BlockData_allrandom
        {
            blockID = currentBlockID,
            ballArrangements = new List<BallData_allrandom>(),
            selections = new List<SelectionData_allrandom>()
        };
        // Add the block data to the condition
        currentCondition.blockDataList.Add(currentBlock);

        
        // Record the ball arrangement directly within InitializeTask
        for (int i = 0; i < pingPongBalls.Count; i++)
        {
            BallData_allrandom ballData = new BallData_allrandom
            {
                index = int.Parse(pingPongBalls[i].GetComponentInChildren<TMP_Text>().text),  // Ball label
                positionIndex = i,  // Position index
                //objectPhysicality
                objectPhysicality = pingPongBalls[i].GetComponentInChildren<MeshRenderer>().sharedMaterial == physicalPrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial ? 1 : 0
                
                
                
            };
            currentBlock.ballArrangements.Add(ballData); // Add each ball's data to the current trial
        }

        
        

        Debug.Log("Ball arrangement for trial " + currentCondition.conditionID + " recorded.");





        //************generate the task************

        if(randomTargets.Count>0){
            randomTargets.Clear();
        }

        // Generate a randomized list of targets
        for (int i = 1; i < numPingPongBalls+1; i++)
        {
            randomTargets.Add(i);
        }
        //ShuffleList(randomTargets);  // Shuffle to randomize target order
        Debug.Log("Task Initialization Complete");
        ShowCountDown();

        // Reset trial-specific accumulation
        //accumulatedHeadMovement = 0f;
        //accumulatedHeadRotation = 0f;
        

        // Store the initial head position and rotation
        //lastHeadPosition = mainCamera.transform.position;
        //lastHeadRotation = mainCamera.transform.rotation;
        //SetNextTarget();  // Set the first target
        
    }



    void ShowCountDown()
    {
        // if(randomTargets.Count > 0){
        //     StartCoroutine(Countdown());
        //     Debug.Log("randomTargets.Cound="+randomTargets.Count);
        //     Debug.Log("call the count down");
        // }else{
        //     SetNextTarget();

        //     Debug.Log("randomTargets.Cound="+randomTargets.Count);
        //     Debug.Log("call the next target");
        // }

        countdownCoroutine=StartCoroutine(Countdown());
        
        

        
    }

    IEnumerator Countdown()
    {
        // Display the countdown sequence: 3, 2, 1, Go
        // string[] countdownMessages = { "3", "2", "1", "Go" };
        
        // foreach (string message in countdownMessages)
        // {
        //     targetText.text = message;
        //     yield return new WaitForSeconds(1f); // Wait 1 second before the next message
        // }

        yield return new WaitForSeconds(1f); // Wait 1 second before the next message

        


        // Now show the next target
        SetNextTarget();

        Debug.Log("call count down function");

        
    }
    

    void SetNextTarget()
    {
        
        if(isLastSelectionFinished==true){


            if(lastSelectedTarget!=null&&randomTargets.Count>numPingPongBalls-numSelections){
                
                ResetTargetColor(lastSelectedTarget);
            }

            


            
            

            if (randomTargets.Count > numPingPongBalls-numSelections)
            {
                
                currentTargetIndex = randomTargets[0];  // Get the first target in the shuffled list

                for(int i=0; i<pingPongBalls.Count; i++){
                    if(int.Parse(pingPongBalls[i].GetComponentInChildren<TMP_Text>().text)==currentTargetIndex){
                        target=pingPongBalls[i];
                        break;
                    }

                }
                Debug.Log("target position="+ target.transform.position);

                randomTargets.RemoveAt(0);  // Remove the selected target from the list
                targetText.text = "Select target: " + currentTargetIndex.ToString();  // Update the target text
                accumulatedControllerMovement=0f;
                accumulatedControllerRotation=0f;
                accumulatedHeadMovement=0f;
                accumulatedHeadRotation=0f;
                showTargetTime=Time.time;

                Debug.Log("ID: "+(numPingPongBalls-randomTargets.Count)+", Select target: " + currentTargetIndex.ToString());
                
            }
            else
            {

                randomTargets.Clear();
                clearAllBalls(); // clear all children objects in the parent object
                SaveTaskData();
                if(currentBlockID==2){
                   
                    
                    targetText.text = "All targets selected! Press 'N' to go to next Condition";  // Task complete
                    isConditionOngoing=false;
                    
                    

                    
                    Debug.Log("Task complete, current trial data saved, go to next condition");

                    //SetNextCondition(); // start the next trial when current trial is done
                }else{
                    //if it is the first block, then repeat the task again
                    targetText.text = "All targets in the first block are selected! Press 'N' to go to next block";  // Task complete
                    isConditionOngoing=false;
                    
                    

                    
                    Debug.Log("Task complete, current block data of this condition is saved, go to next block");

                }
                
                
                
            }
            isLastSelectionFinished=false;
        }
        
    }



    public void OnParticipantIDEntered()
    {
        Debug.Log(participantIDInput.text);
        participantID=int.Parse(participantIDInput.text);
        AssignTaskOrder(participantID);
        isParticipantIDEntered = true;
        Debug.Log(participantID);
        Debug.Log("Participant ID: " + participantID + " - Task order assigned.");

        participantIDInput.GetComponent<UIFollowCamera>().enabled=false;
        targetText.GetComponent<UIFollowCamera>().enabled=false;
        targetText.transform.position=new Vector3(0, 0.06f, 1);
            
        // Hide the input field after successful entry
        participantIDInput.gameObject.SetActive(false);
        calibrate();
    }

    // Define the 6 conditions
    public enum TaskCondition
    {
        Near_AllVirtual = 0,
        Near_HalfVirtualHalfPhysical = 1,
        Near_AllPhysical = 2,
        Far_AllVirtual = 3,
        Far_HalfVirtualHalfPhysical = 4,
        Far_AllPhysical = 5
    }

    // Task condition array for each participant
    public TaskCondition[] taskOrder;

    // Assign task order based on participant ID
    void AssignTaskOrder(int participantID)
    {
        taskOrder = new TaskCondition[6];

        // Calculate which cycle the participant falls into
        int groupID = (participantID - 1) % 6; // This ensures that every 6 participants start at a different condition

        // Example task order rotation based on the groupID
        switch (groupID)
        {
            case 0:
                taskOrder = new TaskCondition[] { TaskCondition.Near_AllVirtual, TaskCondition.Near_HalfVirtualHalfPhysical, TaskCondition.Near_AllPhysical, TaskCondition.Far_AllVirtual, TaskCondition.Far_HalfVirtualHalfPhysical, TaskCondition.Far_AllPhysical };
                break;
            case 1:
                taskOrder = new TaskCondition[] { TaskCondition.Near_HalfVirtualHalfPhysical, TaskCondition.Near_AllPhysical, TaskCondition.Far_AllVirtual, TaskCondition.Far_HalfVirtualHalfPhysical, TaskCondition.Far_AllPhysical, TaskCondition.Near_AllVirtual };
                break;
            case 2:
                taskOrder = new TaskCondition[] { TaskCondition.Near_AllPhysical, TaskCondition.Far_AllVirtual, TaskCondition.Far_HalfVirtualHalfPhysical, TaskCondition.Far_AllPhysical, TaskCondition.Near_AllVirtual, TaskCondition.Near_HalfVirtualHalfPhysical };
                break;
            case 3:
                taskOrder = new TaskCondition[] { TaskCondition.Far_AllVirtual, TaskCondition.Far_HalfVirtualHalfPhysical, TaskCondition.Far_AllPhysical, TaskCondition.Near_AllVirtual, TaskCondition.Near_HalfVirtualHalfPhysical, TaskCondition.Near_AllPhysical };
                break;
            case 4:
                taskOrder = new TaskCondition[] { TaskCondition.Far_HalfVirtualHalfPhysical, TaskCondition.Far_AllPhysical, TaskCondition.Near_AllVirtual, TaskCondition.Near_HalfVirtualHalfPhysical, TaskCondition.Near_AllPhysical, TaskCondition.Far_AllVirtual };
                break;
            case 5:
                taskOrder = new TaskCondition[] { TaskCondition.Far_AllPhysical, TaskCondition.Near_AllVirtual, TaskCondition.Near_HalfVirtualHalfPhysical, TaskCondition.Near_AllPhysical, TaskCondition.Far_AllVirtual, TaskCondition.Far_HalfVirtualHalfPhysical };
                break;
        }

    }


    void ApplyCondition(TaskCondition condition)
    {
        switch (condition)
        {
            case TaskCondition.Near_AllVirtual:
                TaskDepthID = 0; // Near
                TaskPhysicalityID = 0; // All Virtual
                break;
            case TaskCondition.Near_HalfVirtualHalfPhysical:
                TaskDepthID = 0; // Near
                TaskPhysicalityID = 1; // Half Virtual, Half Physical
                break;
            case TaskCondition.Near_AllPhysical:
                TaskDepthID = 0; // Near
                TaskPhysicalityID = 2; // All Physical
                break;
            case TaskCondition.Far_AllVirtual:
                TaskDepthID = 1; // Far
                TaskPhysicalityID = 0; // All Virtual
                break;
            case TaskCondition.Far_HalfVirtualHalfPhysical:
                TaskDepthID = 1; // Far
                TaskPhysicalityID = 1; // Half Virtual, Half Physical
                break;
            case TaskCondition.Far_AllPhysical:
                TaskDepthID = 1; // Far
                TaskPhysicalityID = 2; // All Physical
                break;
        }

        // Initialize the task for this condition
        //RestartTask();
    }

    void ResetTargetColor(GameObject lastSelectedTarget){
        Outline outline = lastSelectedTarget.GetComponentInChildren<Outline>();
        if (outline != null)
        {
            outline.enabled = false;  // Disable the outline
        }
        // Reset the material to the default or original material
        //lastSelectedTarget.GetComponentInChildren<MeshRenderer>().material = defaultMat;

    }

    void HighlightTarget(GameObject target, int correctOrnot)
    {

        // Assuming the coin has an Outline component or similar that we can enable and set color for
        Outline outline = target.GetComponentInChildren<Outline>();
        if (outline == null)
        {
            outline = target.transform.Find("Sphere").gameObject.AddComponent<Outline>();  // Add Outline component if it doesn't exist
        }
        if(correctOrnot==0){
            outline.OutlineColor = Color.red;  // Set the color to red for incorrect selection
            //target.GetComponentInChildren<MeshRenderer>().material=redMat;
        }else{
            outline.OutlineColor = Color.green;  // Set the color to green for correct selection
            //target.GetComponentInChildren<MeshRenderer>().material=greenMat;
        }
        
        outline.OutlineWidth = 5.0f;  // Set a width for the outline
        outline.enabled = true;  // Enable the outline
    }

    void HandleMouseClick(){

        if(isConditionOngoing){
            // Create a ray from the camera through the mouse position
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if(isLastSelectionFinished==false){

                    if (hit.collider.CompareTag("Target"))  // Check if the hit object has the tag "Target"
                    {
                        Debug.Log("Selected a Target");
                        GameObject selectedCoin = hit.collider.gameObject;
                        

                        SelectCoin(selectedCoin, hit.point);
                        
                        
                    }else{
                            if(TaskDepthID==0){
                                Vector3 hitPoint = CalculateIntersectionWithPlane(ray, nearRingDepth);
                                SelectCoin(null, hitPoint);
                                
                            }else{
                                Vector3 hitPoint = CalculateIntersectionWithPlane(ray, farRingDepth);
                                SelectCoin(null, hitPoint);
                                
                            }
                            

                            Debug.Log("send null data to the SelectiCoin");
                    }
                }
            }else{
                
                if(TaskDepthID==0){
                    Vector3 hitPoint = CalculateIntersectionWithPlane(ray, nearRingDepth);
                    SelectCoin(null, hitPoint);
                    
                }else{
                    Vector3 hitPoint = CalculateIntersectionWithPlane(ray, farRingDepth);
                    SelectCoin(null, hitPoint);
                    
                }

                Debug.Log("send null data to the SelectiCoin");
                
            }
        }

        
    }


    // Call this method when the user selects a coin
    public void SelectCoin(GameObject hitObject, Vector3 hitPoint)
    {
        selectionFinishTime=Time.time;

        if (!isBlockStarted)
        {
            // Record the block start time when select the first target
            taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockStartTime = Time.time;
            isBlockStarted = true;
            Debug.Log($"Block {currentBlockID} started at {Time.time}");
        }


        int selectedIndex;
        if(isLastSelectionFinished==false){
            if(hitObject!=null){
                TMP_Text index=hitObject.GetComponentInChildren<TMP_Text>();
                selectedIndex=int.Parse(index.text);
                if ( selectedIndex == currentTargetIndex)
                {
                    // If the correct coin is selected, highlight it as green
                    HighlightTarget(hitObject,1);
                    Debug.Log("Correct Selection");
                    

                    // Move to the next target
                    
                }else{
                    HighlightTarget(hitObject,0);
                    Debug.Log("Incorrect Selection");
                }
            }else{
                selectedIndex=-1; //if didn't select the target, record as -1
            }
            

            

            lastSelectedTarget=hitObject;
            

            float timeBetweenSelections = selectionFinishTime-showTargetTime;// to record the time spent on searching and selection
            
            
            Vector3 selectionPoint = hitPoint;

            Vector3 centerOfTarget = target.transform.position; //the supposed selection target's center position

            float accuracy = Vector3.Distance(selectionPoint, centerOfTarget);
            bool isCorrect = selectedIndex == currentTargetIndex;

            // Create new selection entry
            SelectionData_allrandom selection = new SelectionData_allrandom {
                targetIndex = currentTargetIndex,
                selectedIndex = selectedIndex,
                isCorrect = isCorrect,
                timePerSelections = timeBetweenSelections,
                accuracy = accuracy,
                depthSelection = TaskDepthID,  // Record depth
                physicalitySelection = TaskPhysicalityID,  // Record physicality
                controllerMovment =accumulatedControllerMovement,
                controllerRotation=accumulatedControllerRotation,
                headmMovement=accumulatedHeadMovement,
                headRotation=accumulatedHeadRotation
            };
            
            taskData.conditions[TaskConditionID].blockDataList[currentBlockID-1].selections.Add(selection);  // Add selection data to the current trial
            Debug.Log("Saved current selection data for trial " + TaskConditionID);

            //Invoke("SetNextTarget", 1.5f);  // Add delay to show the result
            
            


            isLastSelectionFinished=true;

            if(randomTargets.Count>numPingPongBalls-numSelections){
                ShowCountDown();

            }else{
                SetNextTarget();
                
            }
            
            
        }
        
        
    }


    Vector3 CalculateIntersectionWithPlane(Ray ray, float planeDepth)
    {
        // Ensure the ray's direction is not parallel to the plane (direction.z cannot be 0)
        if (Mathf.Abs(ray.direction.z) > 0.0001f)
        {
            // Solve for t where the ray intersects the plane at the specified depth
            float t = (planeDepth - ray.origin.z) / ray.direction.z;

            // Calculate and return the intersection point
            return ray.origin + t * ray.direction;
        }

        // If the ray is parallel to the plane (direction.z == 0), return a special value indicating no intersection
        return Vector3.positiveInfinity; // Indicates no intersection
    }



    void SetNextBlock(){
        if(currentBlockID==1){
            taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockEndTime = Time.time;
            taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockSpentTime = taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockEndTime-taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockStartTime;;
            isBlockStarted = false; // Reset block started flag
            currentBlockID++;
            randomTargets.Clear();
            clearAllBalls(); // clear all children objects in the parent object
            if(countdownCoroutine!=null){
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            RestartTask();


        }


    }


    void SetNextCondition()
    {
        //if it is the second block, then go to next trial
        if(currentBlockID==2){

            taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockEndTime = Time.time;
            taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockSpentTime = taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockEndTime-taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockStartTime;;
            isBlockStarted = false; // Reset block started flag
            
            isConditionOngoing=false;
            randomTargets.Clear();
            clearAllBalls(); // clear all children objects in the parent object
            

            isDepthCalibrated = false;
            isSceneCalibrated = false;
            
            // Capture the end time of the current trial
            taskData.conditions[TaskConditionID].conditionEndTime = Time.time;
            taskData.conditions[TaskConditionID].conditionSpentTime = taskData.conditions[TaskConditionID].conditionEndTime -taskData.conditions[TaskConditionID].conditionStartTime ;
            

            currentBlockID=1;
            TaskConditionID++;

            if (TaskConditionID < 6) // If we haven't finished all trials yet
            {
                    
                    
                calibrate();
                    
            }
            else
            {
                // All 6 trials completed, save the entire task data
                TaskFinished();
                Debug.Log("Call the TaskFinished func");
            }
        }
        

        
    }

    void goLastCondition(){

        if(countdownCoroutine!=null){
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

        if(TaskConditionID>0){
            TaskConditionID--;
            currentBlockID=1;

            if (TaskConditionID < 6) // If we haven't finished all trials yet
            {
                RestartTask();
            }
        }
        
    }


    void TaskFinished(){
        //do something when finish all 6 trails
        completionText.text="All trails finished, Task Completed!";

        Destroy(parentObject);
        
        
    }

    void RestartApplication(){
        isApplicationStarted=false;
        isTaskStarted=false;
        TaskConditionID=0;
        isLastSelectionFinished=true;
        completionText.text="";

    }

    // Helper method to shuffle a list (Fisher-Yates shuffle)
    void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void ShuffleListForGameobject(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            GameObject temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    

    // Method to modify the depth of the adjustable ring

    public void ResetView()
    {
        if (mainCamera.transform != null)
        {
            // Calculate the new center position in front of the camera for concentric circles
            //Vector3 newCenterPosition = mainCamera.transform.position + mainCamera.transform.forward * defaultDistanceInFront;

            // Set the parent objects' positions to this new center position
            //parentObjectFixed.transform.position = newCenterPosition;
            //parentObjectAdjustable.transform.position = newCenterPosition;

            // Reposition the balls based on the new positions
            
            PositionBalls(mainCamera.gameObject); // Reposition both sets of balls
        }
    }

    // Method to modify the size of the circles

    private void HandleOnBumper(InputAction.CallbackContext obj)
    {
        bool bumperDown = obj.ReadValueAsButton();
        Debug.Log("was released this frame: " + obj.action.WasReleasedThisFrame());
        ResetView();

    }

    private void HandleOnTrigger(InputAction.CallbackContext obj)
    {
        if(isConditionOngoing){
            Debug.Log("click the trigger");
            RaycastHit hit;
            // Vector3 controllerPos=mlInputs.Controller.Position.ReadValue<Vector3>();
            // Vector3 controllerDir=mlInputs.Controller.Rotation.ReadValue<Quaternion>()*Vector3.forward;
            Vector3 controllerPos=controller.transform.position;
            Vector3 controllerDir=controller.transform.forward;
            
            Ray ray = new Ray(controllerPos, controllerDir);
            
                
                if (Physics.Raycast(controllerPos,controllerDir, out hit))
                {
                    if(isLastSelectionFinished==false){
                        if (hit.collider.CompareTag("Target"))  // Check if the hit object has the tag "Target"
                        {
                            Debug.Log("Selected a Target");
                            GameObject selectedCoin = hit.collider.gameObject;
                            // if(TaskDepthID==0){
                            //         Vector3 hitPoint = CalculateIntersectionWithPlane(ray, nearRingDepth);
                            //         SelectCoin(selectedCoin, hitPoint);
                                     
                                    
                            //     }else{
                            //         Vector3 hitPoint = CalculateIntersectionWithPlane(ray, farRingDepth);
                            //         SelectCoin(selectedCoin, hitPoint);
                            //         viz.transform.position=hitPoint;
                                    
                            //     }
                            
                            SelectCoin(selectedCoin, hit.point);
                            viz.transform.position=hit.point;

                            
                        }else{
                                if(TaskDepthID==0){
                                    Vector3 hitPoint = CalculateIntersectionWithPlane(ray, nearRingDepth);
                                    SelectCoin(null, hitPoint);
                                    viz.transform.position=hitPoint;
                                    
                                }else{
                                    Vector3 hitPoint = CalculateIntersectionWithPlane(ray, farRingDepth);
                                    SelectCoin(null, hitPoint);
                                    viz.transform.position=hitPoint;
                                    
                                }

                                

                                Debug.Log("send null data to the SelectiCoin");
                        }
                    }
                }else{
                    
                        if(TaskDepthID==0){
                            Vector3 hitPoint = CalculateIntersectionWithPlane(ray, nearRingDepth);
                            SelectCoin(null, hitPoint);
                            viz.transform.position=hitPoint;
                            
                        }else{
                            Vector3 hitPoint = CalculateIntersectionWithPlane(ray, farRingDepth);
                            SelectCoin(null, hitPoint);
                            viz.transform.position=hitPoint;
                            
                        }
                }
            
                
                    
                    
        }
    }

    private void IsTrackedOnPerformed(InputAction.CallbackContext obj)
    {
        Debug.Log("The Controller Is tracking");
    }

    void SaveTaskData()
    {
        taskData.taskEndTime = Time.time; // Record the end time for the entire task

        // Capture the end time of the current trial
        taskData.conditions[TaskConditionID].conditionEndTime = Time.time;

        string jsonData = JsonConvert.SerializeObject(taskData, Formatting.Indented);
        

        // string jsonData = JsonConvert.SerializeObject(taskData, Formatting.Indented, new JsonSerializerSettings
        // {
        // ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        // });
        string path = Application.persistentDataPath + "/"+participantID+"_taskData.json"; // Save all trials to one file
        File.WriteAllText(path, jsonData);

        Debug.Log("All task data saved to: " + path);
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.Log($"Eye tracking permission denied.");
    }

    void OnPermissionGranted(string permission)
    {
        InputSubsystem.Extensions.MLEyes.StartTracking();
        eyesActions = new MagicLeapInputs.EyesActions(mlInputs);
        EyeTrackingPermissionGranted = true;
    }


    public class SphereData
    {
        public bool IsPhysical;
        public int Number;
        public int Position;

        public SphereData(bool isPhysical, int number, int position)
        {
            IsPhysical = isPhysical;
            Number = number;
            Position = position;
        }
    }

    // Predefined layouts for each condition
    private Dictionary<TaskCondition, List<SphereData>> layouts = new Dictionary<TaskCondition, List<SphereData>>()
    {
        { TaskCondition.Far_AllPhysical, new List<SphereData> //keep
            {
                new SphereData(true, 1, 0), new SphereData(true, 14, 1), new SphereData(true, 10, 2), new SphereData(true, 2, 3),
                new SphereData(true, 13, 4), new SphereData(true, 7, 5), new SphereData(true, 12, 6), new SphereData(true, 19, 7),
                new SphereData(true, 3, 8), new SphereData(true, 25, 9), new SphereData(true, 21, 10), new SphereData(true, 11, 11),
                new SphereData(true, 18, 12), new SphereData(true, 4, 13), new SphereData(true, 8, 14), new SphereData(true, 23, 15),
                new SphereData(true, 20, 16), new SphereData(true, 9, 17), new SphereData(true, 6, 18), new SphereData(true, 15, 19),
                new SphereData(true, 5, 20), new SphereData(true, 17, 21), new SphereData(true, 22, 22), new SphereData(true, 24, 23),
                new SphereData(true, 16, 24)
            }
        },
        { TaskCondition.Near_AllPhysical, new List<SphereData> //keep
            {
                new SphereData(true, 18, 0), new SphereData(true, 17, 1), new SphereData(true, 21, 2), new SphereData(true, 11, 3),
                new SphereData(true, 5, 4), new SphereData(true, 14, 5), new SphereData(true, 19, 6), new SphereData(true, 12, 7),
                new SphereData(true, 8, 8), new SphereData(true, 1, 9), new SphereData(true, 24, 10), new SphereData(true, 16, 11),
                new SphereData(true, 20, 12), new SphereData(true, 15, 13), new SphereData(true, 22, 14), new SphereData(true, 3, 15),
                new SphereData(true, 4, 16), new SphereData(true, 23, 17), new SphereData(true, 10, 18), new SphereData(true, 25, 19),
                new SphereData(true, 13, 20), new SphereData(true, 7, 21), new SphereData(true, 6, 22), new SphereData(true, 2, 23),
                new SphereData(true, 9, 24)
            }
        },
        { TaskCondition.Far_HalfVirtualHalfPhysical, new List<SphereData> //ensure there are 6 times of all four transitions
            {
                new SphereData(false, 13, 0), new SphereData(false, 25, 1), new SphereData(false, 17, 2), new SphereData(false, 23, 3),
                new SphereData(false, 1, 4), new SphereData(false, 5, 5), new SphereData(true, 14, 6), new SphereData(true, 16, 7),
                new SphereData(true, 10, 8), new SphereData(false, 19, 9), new SphereData(true, 8, 10), new SphereData(false, 3, 11),
                new SphereData(false, 21, 12), new SphereData(false, 9, 13), new SphereData(true, 24, 14), new SphereData(true, 22, 15),
                new SphereData(true, 2, 16), new SphereData(false, 11, 17), new SphereData(false, 7, 18), new SphereData(true, 18, 19),
                new SphereData(true, 12, 20), new SphereData(true, 4, 21), new SphereData(true, 20, 22), new SphereData(true, 6, 23), 
                new SphereData(false, 15, 24)
            }
        },
        { TaskCondition.Near_HalfVirtualHalfPhysical, new List<SphereData> //ensure there are 6 times of all four transitions
            {
                new SphereData(false, 22, 0), new SphereData(false, 10, 1), new SphereData(true, 21, 2), new SphereData(false, 16, 3),
                new SphereData(true, 1, 4), new SphereData(false, 12, 5), new SphereData(true, 13, 6), new SphereData(true, 15, 7),
                new SphereData(false, 24, 8), new SphereData(true, 25, 9), new SphereData(true, 3, 10), new SphereData(false, 6, 11),
                new SphereData(false, 2, 12), new SphereData(true, 7, 13), new SphereData(false, 20, 14), new SphereData(true, 19, 15),
                new SphereData(true, 5, 16), new SphereData(true, 11, 17), new SphereData(true, 9, 18), new SphereData(false, 4, 19),
                new SphereData(true, 17, 20), new SphereData(false, 14, 21), new SphereData(false, 18, 22), new SphereData(false, 8, 23),
                new SphereData(true, 23, 24)
            }
        }
    };
}





[System.Serializable]
public class TaskData_allrandom
{
    public List<ConditionData_allrandom> conditions = new List<ConditionData_allrandom>(); // Store multiple trials in one task
    public float taskStartTime;
    public float taskEndTime;
    
}

[System.Serializable]
public class ConditionData_allrandom
{
    public int conditionID;  // Number of the condition

    public int depth; // 0 for Near; 1 for Far
    public int physicality; // 0 for All Virtual; 1 for Half Physical and Half Virtual; 2 for All Physical

    public float conditionStartTime;
    public float conditionEndTime;

    public float conditionSpentTime;

    public List<BlockData_allrandom> blockDataList=new List<BlockData_allrandom>();

    
}

[System.Serializable]
public class BlockData_allrandom{
    
    public int blockID;
    public List<SelectionData_allrandom> selections = new List<SelectionData_allrandom>();
    public List<BallData_allrandom> ballArrangements = new List<BallData_allrandom>();

    public List<Dictionary<Vector3,Vector3>> fixationPoints;


    public float blockStartTime;

    public float blockEndTime;

    public float blockSpentTime;


}



[System.Serializable]
public class BallData_allrandom
{
    public int index;  // The label of the ball---the number on the target object
    public int positionIndex;  // The position index of the ball, like the first ball will be at the position '0'

    public int objectPhysicality; // The physicality of the target-- 0 for virtual, 1 for physical
}

[System.Serializable]
public class SelectionData_allrandom
{
    public int targetIndex; // the index of target object

    public int depthSelection; // 0 for Near; 1 for Far

    public int physicalitySelection; // 0 for All Vitrual; 1 for Half Physical and Half Virtual; 2 for All Physical
    public int selectedIndex; // the index of selected target object
    public bool isCorrect; // if user correctly selected the target
    public float timePerSelections; // the spent time of each selection
    public float accuracy; // the distance between the hitpoint and the center of the object

    public float headmMovement;

    public float headRotation;


    public float controllerMovment;

    public float controllerRotation;
}
