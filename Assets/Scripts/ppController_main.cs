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
using System;
using System.Linq;
using Firebase.Database;
using Firebase;
using Firebase.Extensions;
//using UnityEngine.XR.OpenXR.Features.Interactions;

public class ppController_main: MonoBehaviour
{
    public GameObject virtualPrefab;

    public GameObject physicalPrefab;
    public float offsetToML2;
    public float ringSize = 1.0f;
    public int numPingPongBalls = 24;
    public float nearRingDepth = 0.74f;
    public float farRingDepth = 1.5f;

    public int numSelections=25;

    public int numBlocks=2;
    //public float defaultDistanceInFront = 1.0f;
    private bool isLastSelectionFinished = false;
    private bool isTaskStarted = false;
    private List<GameObject> pingPongBalls = new List<GameObject>();

    public Camera mainCamera;

    
    public TMP_Text targetText;

    // public Material greenMat;

    // public Material redMat;

    // public Material defaultMat;

    private GameObject parentObject;
    private int currentTargetLabel;
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


    private GameObject lastHighlightedTarget;
    
    public TMP_InputField participantIDInput; // Add a UI input field for participant ID
    private bool isParticipantIDEntered=false;

    private int participantID;


   private List<PP_Frame> frameDataList = new List<PP_Frame>(); // Collect all frame data

    // Task Data Variables
    private TaskData taskData = new TaskData();

    private TrialData currentTrialData;
    private float taskStartTime;
    private float lastSelectionFinishTime; 

    private float trialStartTime; //to record the time when countdown finished and target number shows up
    private float selectionFinishTime; //to record the time when click the controller trigger

    private int selectionCounter_inTrial=0;
    private float distanceInTotal_inTrial=0;

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
    private float accumulatedHeadMovement_Trial = 0f;
    private float accumulatedHeadRotation_Trial = 0f;
    private float accumulatedControllerMovement_Trial=0f;

    private float accumulatedControllerRotation_Trial=0f;


    private float accumulatedHeadMovement_Selection = 0f;
    private float accumulatedHeadRotation_Selection = 0f;
    private float accumulatedControllerMovement_Selection=0f;

    private float accumulatedControllerRotation_Selection=0f;

    private Vector3 calibrateOffset;

    private string taskStartTimeStamp;

    private bool isSetNextTargetFinished=false;

    float adjustSpeed = 0.001f; // Base adjustment speed
    float shiftMultiplier = 10f; // Multiplier when LeftShift is held

    public AudioClip correctSound;
    public AudioClip incorrectSound;
    private AudioSource audioSource;

    public GameObject overlayPlane;

    private bool isTriggerHandled = false;

    private long timeStamp;
    
    private DatabaseReference firebaseRef;
    private DatabaseReference firebaseUser;
    private DatabaseReference firebaseTaskData;
    private DatabaseReference firebaseFrameInCondition;
    private int frameNum;
    public TMP_Text showFirebase;

    private int lastDepthID=-1;



    private const int FramesPerChunk = 1000; // Number of frames per JSON file
    private int chunkCounter = 0; // Keeps track of the chunk number




    void Start()
    {
        mainCamera.GetComponent<Camera>().fieldOfView = 45;
        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();
        _controller = new MagicLeapInputs.ControllerActions(mlInputs);
        _controller.Bumper.performed += HandleOnBumper;
        _controller.Trigger.performed += HandleOnTrigger;
        _controller.Trigger.canceled += ResetTriggerHandled;


        
        

        // // Initialize Firebase
        // FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        // {
        //     if (task.IsFaulted)
        //     {
        //         Debug.LogError("Firebase initialization failed: " + task.Exception);
        //         showFirebase.text="Firebase initialization failed:"+task.Exception;
        //     }
        //     else if (task.IsCompleted)
        //     {
        //         FirebaseApp firebaseApp = FirebaseApp.DefaultInstance;

        //         // Set the DatabaseURL
        //         firebaseApp.Options.DatabaseUrl = new System.Uri("https://ping-pong-study-default-rtdb.firebaseio.com/");
        //         firebaseRef=FirebaseDatabase.DefaultInstance.RootReference;
        //         Debug.Log("firebase setup successfully");

        //         showFirebase.text="firebase setup successfully";
        //     }
        // });


        

        // DatabaseReference deleted1=firebaseRef.Child("Users").Child("User-1");
        // DatabaseReference deleted2=firebaseRef.Child("Users").Child("User-2");
        // deleted1.RemoveValueAsync();
        // deleted2.RemoveValueAsync();

      

        MLPermissions.RequestPermission(MLPermission.EyeTracking, permissionCallbacks);

        if(MLPermissions.CheckPermission(MLPermission.EyeTracking).IsOk){
            //eyetracking_Text.text="Eye Tracking permission denied";
            Debug.Log("eye tracking did not set up");
        }else{
            //eyetracking_Text.text="eye tracking successfully set up";
            Debug.Log("eye tracking successfully set up");
        }

        
        audioSource = GetComponent<AudioSource>();
        
        // Create a reference to the file to delete.
        

        
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
        
        timeStamp=System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        // Calculate the actual adjustment based on whether LeftShift is held
        float actualAdjustSpeed = Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift) ? adjustSpeed * shiftMultiplier : adjustSpeed;

        if(Input.GetKeyDown(KeyCode.Space)){
            if(isParticipantIDEntered){
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
                        circle.SetActive(false);
                        targetText.text="Calibration Done! Press 'Space' to start the condition";
                    
                }
            }
            
        }

        if(Input.GetKeyDown(KeyCode.W)){
            if(isParticipantIDEntered){
                if(!isSceneCalibrated){
                    circle.transform.position += new Vector3(0.0f, actualAdjustSpeed, 0.0f);
                }
            }
            
        }

        if(Input.GetKeyDown(KeyCode.A)){
            if(isParticipantIDEntered){
                if(!isSceneCalibrated){
                    circle.transform.position -= new Vector3(actualAdjustSpeed, 0.0f, 0.0f);
                }
            }
            
        }

        if(Input.GetKeyDown(KeyCode.S)){
            if(isParticipantIDEntered){
                if(!isSceneCalibrated){
                    circle.transform.position -= new Vector3(0.0f, actualAdjustSpeed, 0.0f);
                }
            }
            
        }

        if(Input.GetKeyDown(KeyCode.D)){
            if(isParticipantIDEntered){
                if(!isSceneCalibrated){
                    circle.transform.position += new Vector3(actualAdjustSpeed, 0.0f, 0.0f);
                }
            }
            
        }

        if(Input.GetKeyDown(KeyCode.Q)){
            if(isParticipantIDEntered){
                if(!isSceneCalibrated){
                    circle.transform.Rotate(0.0f, -1.0f, 0.0f);
                }
            }
            
        }

        if(Input.GetKeyDown(KeyCode.E)){
            if(isParticipantIDEntered){
                if(!isSceneCalibrated){
                    circle.transform.Rotate(0.0f, 1.0f, 0.0f);
                }
            }
            
        }

        if(Input.GetKeyDown(KeyCode.Z)){
            if(isParticipantIDEntered){
                if(!isSceneCalibrated){
                    circle.transform.position += Vector3.forward *actualAdjustSpeed;
                }
            }
            
        }

        if(Input.GetKeyDown(KeyCode.X)){
            if(isParticipantIDEntered){
                if(!isSceneCalibrated){
                    circle.transform.position += Vector3.back * actualAdjustSpeed;
                }
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

        if (Input.GetKeyDown(KeyCode.N)){
            if(isParticipantIDEntered&&isSceneCalibrated){
                if(currentBlockID==numBlocks){
                    SetNextCondition();
                }else{
                    SetNextBlock();
                }
            }
            
        }
        


        if(Input.GetKeyDown(KeyCode.P)){
            Application.Quit();
        }

        


        


        

        // Calculate movement distance
        Vector3 currentHeadPosition = mainCamera.transform.position;
        float movementDistance = Vector3.Distance(lastHeadPosition, currentHeadPosition);
        accumulatedHeadMovement_Trial += movementDistance;
        accumulatedHeadMovement_Selection+=movementDistance;
        

        // Calculate rotation difference as the norm of quaternion difference
        Quaternion currentHeadRotation = mainCamera.transform.rotation;
        float rotationAngle = Quaternion.Angle(lastHeadRotation, currentHeadRotation);
        accumulatedHeadRotation_Trial += rotationAngle;
        accumulatedHeadRotation_Selection+=rotationAngle;

        Vector3 currentControllerPosition=controller.transform.position;
        float controllerMovment=Vector3.Distance(lastControllerPosition, currentControllerPosition);
        accumulatedControllerMovement_Trial+=controllerMovment;
        accumulatedControllerMovement_Selection+=controllerMovment;

        quaternion currentControllerRotation=controller.transform.rotation;
        float controllerRotationAngle=Quaternion.Angle(lastControllerRotation, currentControllerRotation);
        accumulatedControllerRotation_Trial+=controllerRotationAngle;
        accumulatedControllerRotation_Selection+=controllerRotationAngle;

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
        
        
 

        if (isConditionOngoing)
        {
            PP_Frame currentFrame = new PP_Frame
            (
                frameID: frameNum, // Use the count as the unique ID
                timestamp: System.DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                conditionID:TaskConditionID,
                blockID:currentBlockID,
                targetCenterPos: circleCenterPos,
                headPos: mainCamera.transform.position,
                headRot: mainCamera.transform.rotation,

                fixationPt: eyes.fixationPoint,
                leftEyePos: eyes.leftEyePosition, // Replace with actual left eye position
                rightEyePos: eyes.rightEyePosition, // Replace with actual right eye position
                leftEyeRot: eyes.leftEyeRotation, // Replace with actual left eye rotation
                rightEyeRot: eyes.rightEyeRotation, // Replace with actual right eye rotation
                fixationConfidence: trackingState.FixationConfidence, // Replace with actual confidence
                gazeTimestamp: trackingState.Timestamp,

                //*****just for testing the frame data in Unity editor*****

                // fixationPt: Vector3.zero,
                // leftEyePos: Vector3.zero, // Replace with actual left eye position
                // rightEyePos: Vector3.zero, // Replace with actual right eye position
                // leftEyeRot: Quaternion.identity, // Replace with actual left eye rotation
                // rightEyeRot: Quaternion.identity, // Replace with actual right eye rotation
                // fixationConfidence: 1f, // Replace with actual confidence
                // gazeTimestamp: System.DateTimeOffset.Now.ToUnixTimeMilliseconds(),


                controllerPos: controller.transform.position,
                controllerRot: controller.transform.rotation
            );
            
            frameDataList.Add(currentFrame); // Add the frame to the list
            //firebaseFrameInCondition.Child("frames").Child(frameNum.ToString()).SetRawJsonValueAsync(JsonUtility.ToJson(currentFrame));
            frameNum++;

            // Save a chunk when frame data reaches the threshold
            // if (frameDataList.Count >= FramesPerChunk)
            // {
            //     SaveFrameData();
            // }
        }




        
        
    }

    public void startApplication(){
        if(isApplicationStarted==false){

            // Generate and store the timestamp at the start of the task
            taskStartTimeStamp = DateTime.Now.ToString("yyyy_MMdd_HH-mm-ss");
            //store the initial data of the mainCamera when is ready to start the application
            initialCamera=new GameObject("TempCamera");
            initialCamera.transform.position=mainCamera.transform.position;
            initialCamera.transform.rotation=mainCamera.transform.rotation;
            Initialization();
            isApplicationStarted=true;

            targetText.text="Press 'O' to start";
        }

        

        
        
    }

    public void RestartTask(){
        
        //isLastSelectionFinished=true;
        // randomTargets.Clear();
        // clearAllBalls(); // clear all children objects in the parent object



        // Assign the first condition to start with
        Initialization();
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
        if(isDepthCalibrated==false&&isConditionOngoing==false){

            if(circle!=null){
                Destroy(circle);
            }
            
            //targetText.text="Please calibrate the scene with researcher's help";
            if(taskOrder[TaskConditionID] == TaskCondition.Near_AllVirtual || taskOrder[TaskConditionID] == TaskCondition.Near_AllPhysical || taskOrder[TaskConditionID] == TaskCondition.Near_HalfVirtualHalfPhysical)
            {
                Quaternion circle_rot;
                calibrateOffset=new Vector3(0, 0, nearRingDepth);
                if (TaskConditionID==0 || lastDepthID!=TaskDepthID) {
                    circleCenterPos = mainCamera.transform.position + calibrateOffset;
                    circle_rot=Quaternion.identity;
                }else{
                    circle_rot=circle_transform.rotation;
                }
                
                
                circle = Instantiate(calibration_near, circleCenterPos, circle_rot);

                
            }else{
                Quaternion circle_rot;
                calibrateOffset=new Vector3(0, 0, farRingDepth);
                if (TaskConditionID==0 || lastDepthID!=TaskDepthID) {
                    circleCenterPos = mainCamera.transform.position + calibrateOffset;
                    circle_rot=Quaternion.identity;
                }else{
                    circle_rot=circle_transform.rotation;
                }

                circle = Instantiate(calibration_far, circleCenterPos, circle_rot);
                
            }

            targetText.text="Calibration";
            
            targetText.transform.rotation=Quaternion.identity;
            //targetText.transform.position=circle.transform.position+calibrateOffset;
            
        }
        
    }


    void Initialization(){
        Debug.Log("taskconditionid="+TaskConditionID);
        
        
        // Instantiate parent objects to hold each set of ping pong balls
        if(parentObject==null){
            parentObject = new GameObject("parentPingpongBalls");
        }
        
        InstantiateBalls(); // Create the ping pong balls
        Debug.Log("InstantiateBalls");
        PositionBalls(initialCamera); // Position them in their respective circles

        parentObject.SetActive(false);
        
        //firebaseFrameInCondition=firebaseUser.Child("Condition/" + TaskConditionID.ToString());
        
    



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

            for (int i = 0; i < numPingPongBalls; i++)
            {
                GameObject newBall = Instantiate(virtualPrefab, Vector3.zero, circle_transform.rotation, parentObject.transform);
                newBall.name = "PingPongBall_" + (i + 1);
                TMP_Text idTextFixed = newBall.GetComponentInChildren<TMP_Text>();
                if (idTextFixed != null) idTextFixed.text = numbers[i].ToString();
                pingPongBalls.Add(newBall);

                
            }
        }
        else{
            // Use predefined layout for other conditions
            List<SphereData> layout;
            if(currentBlockID==1){
                layout= layouts1[taskOrder[TaskConditionID]]; // if it is the first block, we go to use the layout1
                
            }else{
                layout= layouts2[taskOrder[TaskConditionID]]; // if it is the second block, we go to use the layout2
                
            }


            foreach (SphereData sphereData in layout)
            {
                GameObject newBall = Instantiate(
                    sphereData.IsPhysical ? physicalPrefab : virtualPrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    parentObject.transform
                );

                newBall.name = "PingPongBall_" + sphereData.Number;
                TMP_Text idTextFixed = newBall.GetComponentInChildren<TMP_Text>();
                if (idTextFixed != null) idTextFixed.text = sphereData.Number.ToString();
                pingPongBalls.Add(newBall);
            }
        }
        
        // Set the base scale based on the prefab's original scale
        if (virtualPrefab != null)
        {
            nearScale = virtualPrefab.transform.localScale;
        }
    }

    // void InstantiateBalls()
    // {
    //     pingPongBalls.Clear();
    //     if (taskOrder[TaskConditionID] == TaskCondition.Near_AllVirtual || taskOrder[TaskConditionID] == TaskCondition.Far_AllVirtual)
    //     {
    //         // Randomly generate virtual spheres for "All Virtual" conditions
    //         List<int> numbers = new List<int>();
    //         for (int i = 1; i <= numPingPongBalls; i++) numbers.Add(i);
    //         ShuffleList(numbers);

    //         // Generate physical spheres for "All Virtual" conditions, randomly assign the label to the sequential order of balls
    //         for (int i = 0; i < numPingPongBalls; i++)
    //         {
    //             GameObject newBall = Instantiate(virtualPrefab, Vector3.zero, Quaternion.identity, parentObject.transform);
    //             newBall.name = "PingPongBall_" + (i + 1);
    //             TMP_Text idTextFixed = newBall.GetComponentInChildren<TMP_Text>();
    //             if (idTextFixed != null) idTextFixed.text = numbers[i].ToString();
    //             pingPongBalls.Add(newBall);

                
    //         }
    //     }
    //     else if (taskOrder[TaskConditionID] == TaskCondition.Near_AllPhysical || taskOrder[TaskConditionID] == TaskCondition.Far_AllPhysical)
    //     {

    //         // Randomly generate virtual spheres for "All Virtual" conditions
    //         List<int> numbers = new List<int>();
    //         for (int i = 1; i <= numPingPongBalls; i++) numbers.Add(i);
    //         ShuffleList(numbers);

    //         // Generate physical spheres for "All Physical" conditions, randomly assign the label to the sequential order of balls
    //         for (int i = 0; i <= numPingPongBalls; i++)
    //         {
    //             GameObject newBall = Instantiate(physicalPrefab, Vector3.zero, Quaternion.identity, parentObject.transform);
    //             newBall.name = "PingPongBall_" + (i + 1);
    //             TMP_Text idTextFixed = newBall.GetComponentInChildren<TMP_Text>();
    //             if (idTextFixed != null) idTextFixed.text = numbers[i].ToString();
    //             pingPongBalls.Add(newBall);
    //         }
    //     }
    //     else
    //     {
    //         List<string> transitions = new List<string>();
    //         for (int i = 0; i < 6; i++)
    //         {
    //             transitions.Add("00");
    //             transitions.Add("01");
    //             transitions.Add("10");
    //             transitions.Add("11");
    //         }

    //         // Physicality list: 1 for Physical, 0 for Virtual
    //         List<int> physicalityList = new List<int>();;

    //         // Regenerate until the desired number of physical and virtual objects is met
    //         bool isValid = false;

    //         while (!isValid)
    //         {
                

    //             // Randomly assign the physicality of the first object
    //             physicalityList.Add(1); // 1 for Physical, 0 for Virtual
                

    //             // Copy transitions list to reset during each generation attempt
    //             List<string> transitionsCopy = new List<string>(transitions);

    //             // Generate the sequence of transitions
    //             for (int i = 1; i < numPingPongBalls; i++)
    //             {
    //                 int previousPhysicality = physicalityList[i - 1]; // Previous object's type
    //                 int currentPhysicality = UnityEngine.Random.Range(0, 2); // Randomly pick 0 or 1
    //                 string currentTransition = (previousPhysicality == 1 ? "1" : "0") + (currentPhysicality == 1 ? "1" : "0");

    //                 if (transitionsCopy.Contains(currentTransition))
    //                 {
    //                     // If valid, add the current type and remove the transition from the list
    //                     physicalityList.Add(currentPhysicality);
    //                     transitionsCopy.Remove(currentTransition);
    //                 }
    //                 else
    //                 {
    //                     // If invalid, assign the opposite physicality
    //                     currentPhysicality = 1 - currentPhysicality; // Flip 0 to 1 or 1 to 0
    //                     physicalityList.Add(currentPhysicality);

    //                     // Remove the valid transition for the flipped type
    //                     currentTransition = (previousPhysicality == 1 ? "1" : "0") + (currentPhysicality == 1 ? "1" : "0");
    //                     transitionsCopy.Remove(currentTransition);
    //                 }
    //             }

    //             // Validate the number of physical (1) and virtual (0) objects
    //             int physicalCount = physicalityList.Where(p => p == 1).Count();
    //             int virtualCount = physicalityList.Where(p => p == 0).Count();

    //             if (physicalCount == 12 && virtualCount == 13 )
    //             {
    //                 isValid = true; // The condition is satisfied
    //             }
    //         }

    //         // Create ping pong ball objects based on the physicality list
    //         for (int i = 0; i < numPingPongBalls; i++)
    //         {
    //             GameObject newBall = Instantiate(
    //                 physicalityList[i] == 1 ? physicalPrefab : virtualPrefab,
    //                 Vector3.zero, // Position will be handled in PositionBallSet
    //                 Quaternion.identity,
    //                 parentObject.transform
    //             );

    //             newBall.name = "PingPongBall_" + (i + 1);
    //             TMP_Text idTextFixed = newBall.GetComponentInChildren<TMP_Text>();
    //             if (idTextFixed != null) idTextFixed.text = (i + 1).ToString();

    //             pingPongBalls.Add(newBall);
    //         }

    //         // Shuffle the pingPongBalls list to randomize the label order
    //         ShuffleListForGameobject(pingPongBalls);


            
    //     }
        
    //     // Set the base scale based on the prefab's original scale
    //     if (virtualPrefab != null)
    //     {
    //         nearScale = virtualPrefab.transform.localScale;
    //     }
    // }

    // void ShuffleListForGameobject(List<GameObject> list)
    // {
    //     for (int i = 0; i < list.Count; i++)
    //     {
    //         int randomIndex = UnityEngine.Random.Range(i, list.Count);
    //         GameObject temp = list[i];
    //         list[i] = list[randomIndex];
    //         list[randomIndex] = temp;
    //     }
    // }

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
            float size=ballSet[i].transform.GetComponentInChildren<MeshRenderer>().bounds.size.x;
            
            
            
            
        }

        // Store the scale for reference
        farScale = CalculateScaleForDepth(new Vector2(angularSizeX, angularSizeY), ringDepth, ballScale);
        targetText.transform.position=centerPosition;
        float targetScale=0.0015f;
        targetText.transform.localScale=new Vector3(targetScale*farScale.x, targetScale*farScale.x, targetScale*farScale.x);


        controller.GetComponent<XRInteractorLineVisual>().lineWidth=0.005f*farScale.x;
        //targetText.transform.rotation=circle_transform.rotation;
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

        ConditionData currentCondition;

        // Check if the data of current condition already exists
        if (TaskConditionID < taskData.conditions.Count)
        {
            currentCondition = taskData.conditions[TaskConditionID];
        }
        else
        {

            // Create a new condition data if not present

            
            currentCondition = new ConditionData
            {
                conditionID = TaskConditionID,
                depth = TaskDepthID,
                physicality = TaskPhysicalityID,
                conditionStartTime = Time.time,
                conditionEndTime=0,
                conditionSpentTime=0
            };
            taskData.conditions.Add(currentCondition);
        }

        // Create a new block for the current condition
        BlockData currentBlock = new BlockData
        {
            blockID = currentBlockID,
            ballArrangements = new List<BallData>(),
            trials = new List<TrialData>(),
            blockStartTime=Time.time,
            blockEndTime=0,
            blockSpentTime=0

        };

        
        // Add the block data to the condition
        currentCondition.blockDataList.Add(currentBlock);
        

        
        // Record the ball arrangement directly within InitializeTask
        for (int i = 0; i < pingPongBalls.Count; i++)
        {
            BallData ballData = new BallData
            {
                index = int.Parse(pingPongBalls[i].GetComponentInChildren<TMP_Text>().text),  // Ball label
                positionIndex = i,  // Position index
                //objectPhysicality
                objectPhysicality = pingPongBalls[i].GetComponentInChildren<MeshRenderer>().sharedMaterial == physicalPrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial ? 1 : 0
                
                
                
            };
            currentBlock.ballArrangements.Add(ballData); // Add each ball's data to the current trial
        }

        

        

        


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
        // accumulatedHeadMovement = 0f;
        // accumulatedHeadRotation = 0f;

        // Store the initial head position and rotation
        // lastHeadPosition = mainCamera.transform.position;
        // lastHeadRotation = mainCamera.transform.rotation;
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
        overlayPlane.SetActive(true);
        targetText.transform.position=new Vector3(circleCenterPos.x, circleCenterPos.y, 0.5f);
        //Display the countdown sequence: 3, 2, 1, Go
        string[] countdownMessages = { "3", "2", "1", "Go" };
        
        foreach (string message in countdownMessages)
        {
            targetText.text = message;
            yield return new WaitForSeconds(1f); // Wait 1 second before the next message
        }

        

        
        parentObject.SetActive(true);
        isLastSelectionFinished=true;
        // Now show the next target
        overlayPlane.SetActive(false);

         targetText.transform.position=circleCenterPos;
        SetNextTarget();
    
        Debug.Log("call count down function");

        
    }


    

    void SetNextTarget()
    {
        
        if(isLastSelectionFinished==true){

            if(randomTargets.Count==numPingPongBalls){
                lastSelectionFinishTime=Time.time;
            }

            if (randomTargets.Count > numPingPongBalls-numSelections)
            {
                
                currentTargetLabel = randomTargets[0];  // Get the first target in the shuffled list

                for(int i=0; i<pingPongBalls.Count; i++){
                    if(int.Parse(pingPongBalls[i].GetComponentInChildren<TMP_Text>().text)==currentTargetLabel){
                        target=pingPongBalls[i];
                        break;
                    }

                }
                Debug.Log("target position="+ target.transform.position);

                randomTargets.RemoveAt(0);  // Remove the selected target from the list

                //targetText.text = "Select target: " + currentTargetLabel.ToString();  // Update the target text
                targetText.text="";

                trialStartTime=Time.time; //trial start time----to show the target 
                
                // Create a new trial data for this trial
                currentTrialData = new TrialData
                {
                    targetLabel = currentTargetLabel, // Will be updated when the target is set
                    depthSelection = TaskDepthID,
                    physicalitySelection = TaskPhysicalityID,
                    numSelections = 0,
                    totalDistanceEachTrial = 0,
                    timePerTrial = 0,
                    headmMovementEachTrial = 0,
                    headRotationEachTrial = 0,
                    controllerMovmentEachTrial = 0,
                    controllerRotationEachTrial = 0,
                };


                
                accumulatedControllerMovement_Trial=0f;
                accumulatedControllerRotation_Trial=0f;
                accumulatedHeadMovement_Trial=0f;
                accumulatedHeadRotation_Trial=0f;
                

                Debug.Log("ID: "+(numPingPongBalls-randomTargets.Count)+", Select target: " + currentTargetLabel.ToString());
                isSetNextTargetFinished=true;
                
            }
            else   // the end of current condition or current block
            {
                // Capture the end time of the current condition
                

                taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockEndTime = Time.time;
                taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockSpentTime = taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockEndTime-taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].blockStartTime;
                
                    
                
                
                
                if(currentBlockID==numBlocks){
                    
                    taskData.conditions[TaskConditionID].conditionEndTime = taskData.conditions[TaskConditionID].blockDataList[numBlocks-1].blockEndTime;
                    
                    taskData.conditions[TaskConditionID].conditionSpentTime = taskData.conditions[TaskConditionID].conditionEndTime-taskData.conditions[TaskConditionID].conditionStartTime;
                    targetText.text = "All targets selected! Press 'N' to go to next condition";  // Task complete
                    isConditionOngoing=false;
                    
                    SaveFrameData();
                    
                    Debug.Log("Task complete, current trial data saved, go to next condition");

                    //SetNextCondition(); // start the next trial when current trial is done
                }else{
                    //if it is the first block, then repeat the task again
                    targetText.text = "All targets in the first block are selected! Press 'N' to go to next block";  // Task complete
                    
                    
                    

                    
                    Debug.Log("Task complete, current block data of this condition is saved, go to next block");

                }
                randomTargets.Clear();
                
                SaveTaskData();
                
                clearAllBalls(); // clear all children objects in the parent object
                
                
            }
            isLastSelectionFinished=true;
        }
        
    }



    public void OnParticipantIDEntered()
    {
        Debug.Log(participantIDInput.text);
        participantID=int.Parse(participantIDInput.text);
        AssignTaskOrder(participantID);
        ApplyCondition(taskOrder[TaskConditionID]);
        isParticipantIDEntered = true;
        Debug.Log(participantID);
        Debug.Log("Participant ID: " + participantID + " - Task order assigned.");

        participantIDInput.GetComponent<UIFollowCamera>().enabled=false;
        targetText.GetComponent<UIFollowCamera>().enabled=false;
        targetText.transform.position=new Vector3(0, 0.06f, 1.0f);

        //firebaseUser=firebaseRef.Child("Users").Child("User-"+participantIDInput.text);
            
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
        if(TaskConditionID>0){
            lastDepthID=TaskDepthID;
        }

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

    void ResetTargetColor(GameObject obj){
        Outline outline = obj.GetComponentInChildren<Outline>();
        if (outline != null)
        {
            outline.enabled = false;  // Disable the outline
        }
        // Reset the material to the default or original material
        //obj.GetComponentInChildren<MeshRenderer>().material = defaultMat;

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
                if(isLastSelectionFinished==true&&isSetNextTargetFinished==true){

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
                if(isLastSelectionFinished==true&&isSetNextTargetFinished==true){
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

        
    }


    // Call this method when the user selects a coin
    public void SelectCoin(GameObject hitObject, Vector3 hitPoint)
    {
        isLastSelectionFinished=false;
        selectionFinishTime=Time.time;
        selectionCounter_inTrial++;
        int selectedLabel;
        
            if(hitObject!=null){
                TMP_Text index=hitObject.GetComponentInChildren<TMP_Text>();
                selectedLabel=int.Parse(index.text);
            }else{
                selectedLabel=-1; //if didn't select the target, record as -1
            }

            
            bool isCorrect = selectedLabel == currentTargetLabel;
            
            

            
            

            float timeOfSelection = selectionFinishTime-lastSelectionFinishTime;// to compute the time for this selection
            lastSelectionFinishTime=selectionFinishTime; // and then, update the lastSelectionFinish time
            
            Vector3 selectionPoint = hitPoint;

            Vector3 centerOfTarget = target.transform.position; //the supposed selection target's center position

            float distanceToTarget = Vector3.Distance(selectionPoint, centerOfTarget);
            distanceInTotal_inTrial+=distanceToTarget;
            
            

            // Record selection data
            SelectionData selection = new SelectionData
            {
                selectedLabel = selectedLabel,
                isCorrect = isCorrect,
                timePerSelection = timeOfSelection,
                distanceOfSelection = distanceToTarget,
                headmMovementEachSelection = accumulatedHeadMovement_Selection,
                headRotationEachSelection = accumulatedHeadRotation_Selection,
                controllerMovmentEachSelection = accumulatedControllerMovement_Selection,
                controllerRotationEachSelection = accumulatedControllerRotation_Selection,
            };
            
            currentTrialData.selections.Add(selection); //add this selection to the currentTrial data
            accumulatedHeadMovement_Selection=0;
            accumulatedHeadRotation_Selection=0;
            accumulatedControllerMovement_Selection=0;
            accumulatedControllerRotation_Selection=0;



            Debug.Log("Saved current selection data for this condition " + TaskConditionID);

            //Invoke("SetNextTarget", 1.5f);  // Add delay to show the result
            
            


            

            

            if(isCorrect){

                if(lastHighlightedTarget!=null&&randomTargets.Count>numPingPongBalls-numSelections){
                
                    ResetTargetColor(lastHighlightedTarget);
                }

                // If the correct coin is selected, highlight it as green
                PlayCorrectSound();
                HighlightTarget(hitObject,1);
                
                lastHighlightedTarget=hitObject;
                Debug.Log("Correct Selection");

                float trialFinishTime=Time.time;
                currentTrialData.timePerTrial=trialFinishTime-trialStartTime;
                currentTrialData.numSelections=selectionCounter_inTrial;//store the time of selectios that made by user in a trial
                currentTrialData.headmMovementEachTrial=accumulatedHeadMovement_Trial;
                currentTrialData.headRotationEachTrial=accumulatedHeadRotation_Trial;
                currentTrialData.controllerMovmentEachTrial=accumulatedControllerMovement_Trial;
                currentTrialData.controllerRotationEachTrial=accumulatedControllerRotation_Trial;
                currentTrialData.totalDistanceEachTrial=distanceInTotal_inTrial;

                selectionCounter_inTrial=0; //reset the counter
                distanceInTotal_inTrial=0; //reset the total distance


                
                taskData.conditions[TaskConditionID].blockDataList[currentBlockID - 1].trials.Add(currentTrialData);
                
                
                Debug.Log("call set next target");
                isLastSelectionFinished=true;
                isSetNextTargetFinished=false;
                SetNextTarget();
                // // Move to the next target or next condition
                // if(randomTargets.Count>numPingPongBalls-numSelections)
                // {//move to the next target
                //     Debug.Log("call set next target");
                //     isLastSelectionFinished=true;
                //     SetNextTarget();

                // }else{//move to next condition
                //     ShowCountDown();
                    
                // }

            }else{
                PlayIncorrectSound();
            }

            isLastSelectionFinished=true;

            
            
            
        
        
        
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
        if(currentBlockID==1&&currentBlockID<numBlocks){
            currentBlockID++;
            isBlockStarted = false; // Reset block started flag
            
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
        if(currentBlockID==numBlocks){

            
            isBlockStarted = false; // Reset block started flag
            
            isConditionOngoing=false;
            randomTargets.Clear();
            clearAllBalls(); // clear all children objects in the parent object
            

            isDepthCalibrated = false;
            isSceneCalibrated = false;

            isSetNextTargetFinished=false;
            
            // Capture the end time of the current trial
        
            
            

            currentBlockID=1;
            TaskConditionID++;

            if (TaskConditionID < 6) // If we haven't finished all conditions yet
            {
                    
                ApplyCondition(taskOrder[TaskConditionID]);
                calibrate();
                    
            }
            else
            {
                // All 6 conditions completed, save the entire task data
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
        //do something when finish all 6 conditions
        taskData.taskEndTime = Time.time;
        completionText.text="All Conditions finished, Task Completed!";

        Destroy(parentObject);

        SaveTaskData();
        
        //firebaseUser.Child("TaskData").SetRawJsonValueAsync(JsonUtility.ToJson(taskData));
        
        
        
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

    


    public void PlayCorrectSound()
    {
        audioSource.volume=0.5f;
        audioSource.PlayOneShot(correctSound);
    }

    public void PlayIncorrectSound()
    {
        audioSource.volume=1.0f;
        audioSource.PlayOneShot(incorrectSound);
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

        if (isTriggerHandled) return; // If already handled, exit

        isTriggerHandled = true; // Mark as handled

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
                    if(isLastSelectionFinished==true){
                        if (hit.collider.CompareTag("Target"))  // Check if the hit object has the tag "Target"
                        {
                            Debug.Log("Selected a Target");
                            GameObject selectedCoin = hit.collider.gameObject;
                            
                            SelectCoin(selectedCoin, hit.point);
        

                            
                        }else{
                                if(TaskDepthID==0){
                                    Vector3 hitPoint = CalculateIntersectionWithPlane(ray, circleCenterPos.z);
                                    SelectCoin(null, hitPoint);
                                    
                                    
                                }else{
                                    Vector3 hitPoint = CalculateIntersectionWithPlane(ray, circleCenterPos.z);
                                    SelectCoin(null, hitPoint);
                                    
                                    
                                }

                                

                                Debug.Log("send null data to the SelectiCoin");
                        }
                    }
                }else{
                        if(isLastSelectionFinished==true){
                            if(TaskDepthID==0){
                                Vector3 hitPoint = CalculateIntersectionWithPlane(ray, circleCenterPos.z);
                                SelectCoin(null, hitPoint);
                            
                            
                            }else{
                                Vector3 hitPoint = CalculateIntersectionWithPlane(ray, circleCenterPos.z);
                                SelectCoin(null, hitPoint);
                                
                                
                            }
                        }
                        
                }
            
                
                    
                    
        }
    }

    void ResetTriggerHandled(InputAction.CallbackContext obj)
    {
        isTriggerHandled = false; // Reset the flag when the trigger is released
    }

    private void IsTrackedOnPerformed(InputAction.CallbackContext obj)
    {
        Debug.Log("The Controller Is tracking");
    }

    void SaveTaskData()
    {
        // Use the stored timestamp for the file name
        string fileName = $"{participantID}_taskData_{taskStartTimeStamp}.json";

        // Define the full path for the file
        string path = Path.Combine(Application.persistentDataPath, fileName);

        TaskData combinedTaskData;

        if (File.Exists(path))
        {
            // Read existing data from the file
            string existingJson = File.ReadAllText(path);
            combinedTaskData = JsonConvert.DeserializeObject<TaskData>(existingJson);

            // Ensure we don't duplicate conditions and blocks
            foreach (var condition in taskData.conditions)
            {
                // Check if this condition already exists
                var existingCondition = combinedTaskData.conditions
                    .FirstOrDefault(c => c.conditionID == condition.conditionID);

                if (existingCondition == null)
                {
                    // If the condition doesn't exist, add it
                    combinedTaskData.conditions.Add(condition);
                }
                else
                {
                    // If the condition exists, merge its blocks
                    foreach (var block in condition.blockDataList)
                    {
                        // Check if this block already exists
                        if (!existingCondition.blockDataList.Exists(b => b.blockID == block.blockID))
                        {
                            existingCondition.blockDataList.Add(block);
                        }
                    }
                }
            }
        }
        else
        {
            // If the file doesn't exist, start with the current task data
            combinedTaskData = new TaskData
            {
                conditions = new List<ConditionData>(taskData.conditions),
                taskStartTime = taskData.taskStartTime,
                taskEndTime = taskData.taskEndTime
            };
        }

        // Update the task end time
        combinedTaskData.taskEndTime = taskData.taskEndTime;

        // Serialize the merged data to JSON
        string updatedJson = JsonConvert.SerializeObject(combinedTaskData, Formatting.Indented);

        // Write the updated JSON data to the file
        File.WriteAllText(path, updatedJson);

        Debug.Log($"Task data saved to: {path}");

    }


    void SaveFrameData()
    {
        // Generate the filename using the participant ID and task start timestamp
        string fileName = $"{participantID}_Condition_{TaskConditionID}_FrameData_{taskStartTimeStamp}.json";

        

        // Define the full path for the file
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // Configure settings to handle Unity types and avoid self-referencing loops
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // Serialize the frame data to JSON
        string json = JsonConvert.SerializeObject(frameDataList, Formatting.Indented, settings);

        // Write the JSON data to the file
        File.WriteAllText(path, json);

        Debug.Log($"Frame data saved to: {path}");

        // Clear the frameDataList for the next condition/task
        frameDataList.Clear();
    }

    // void SaveFrameData()
    // {
    //     while (frameDataList.Count > 0)
    //     {
    //         // Take a chunk of frame data
    //         List<PP_Frame> frameChunk = frameDataList.Take(FramesPerChunk).ToList();
    //         frameDataList.RemoveRange(0, frameChunk.Count);

    //         // Generate a unique filename for the chunk
    //         string fileName = $"{participantID}_frameData_{taskStartTimeStamp}_chunk{chunkCounter}.json";
    //         string path = Path.Combine(Application.persistentDataPath, fileName);

    //         // Serialize the chunk to JSON
    //         JsonSerializerSettings settings = new JsonSerializerSettings
    //         {
    //             ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    //         };

    //         string json = JsonConvert.SerializeObject(frameChunk, Formatting.Indented, settings);

    //         // Write the chunk to a file
    //         File.WriteAllText(path, json);
    //         Debug.Log($"Chunk {chunkCounter} saved to: {path}");

    //         chunkCounter++; // Increment chunk counter
    //     }
    // }


    
    

    

   

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

    // Predefined layouts for each condition----block 1
    private Dictionary<TaskCondition, List<SphereData>> layouts1 = new Dictionary<TaskCondition, List<SphereData>>()
    {
        { TaskCondition.Far_AllPhysical, new List<SphereData>
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
        { TaskCondition.Near_AllPhysical, new List<SphereData>
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
        { TaskCondition.Far_HalfVirtualHalfPhysical, new List<SphereData>
            {
                new SphereData(true, 22, 0), new SphereData(true, 18, 1), new SphereData(true, 19, 2), new SphereData(false, 8, 3),
                new SphereData(true, 10, 4), new SphereData(true, 23, 5), new SphereData(false, 11, 6), new SphereData(true, 16, 7),
                new SphereData(true, 4, 8), new SphereData(false, 2, 9), new SphereData(true, 24, 10), new SphereData(false, 9, 11),
                new SphereData(false, 7, 12), new SphereData(false, 14, 13), new SphereData(false, 1, 14), new SphereData(false, 3, 15),
                new SphereData(true, 12, 16), new SphereData(true, 21, 17), new SphereData(false, 20, 18), new SphereData(false, 25, 19),
                new SphereData(false, 15, 20), new SphereData(true, 13, 21), new SphereData(true, 5, 22), new SphereData(false, 6, 23), 
                new SphereData(false, 17, 24)
            }
        },
        { TaskCondition.Near_HalfVirtualHalfPhysical, new List<SphereData>
            {
                new SphereData(true, 1, 0), new SphereData(true, 11, 1), new SphereData(false, 9, 2), new SphereData(false, 10, 3),
                new SphereData(true, 22, 4), new SphereData(true, 17, 5), new SphereData(false, 2, 6), new SphereData(true, 12, 7),
                new SphereData(false, 3, 8), new SphereData(false, 23, 9), new SphereData(false, 4, 10), new SphereData(false, 6, 11),
                new SphereData(true, 19, 12), new SphereData(true, 25, 13), new SphereData(true, 24, 14), new SphereData(false, 16, 15),
                new SphereData(true, 18, 16), new SphereData(false, 5, 17), new SphereData(true, 21, 18), new SphereData(false, 14, 19),
                new SphereData(true, 8, 20), new SphereData(true, 13, 21), new SphereData(false, 7, 22), new SphereData(false, 20, 23),
                new SphereData(true, 15, 24)
            }
        }
    };


    private Dictionary<TaskCondition, List<SphereData>> layouts2 = new Dictionary<TaskCondition, List<SphereData>>()
    {
        { TaskCondition.Far_AllPhysical, new List<SphereData>
            {
                new SphereData(true, 23, 0), new SphereData(true, 17, 1), new SphereData(true, 4, 2), new SphereData(true, 8, 3),
                new SphereData(true, 14, 4), new SphereData(true,22, 5), new SphereData(true, 24, 6), new SphereData(true, 10, 7),
                new SphereData(true,16, 8), new SphereData(true, 18, 9), new SphereData(true, 19, 10), new SphereData(true, 2, 11),
                new SphereData(true, 3, 12), new SphereData(true, 20, 13), new SphereData(true, 1, 14), new SphereData(true, 15, 15),
                new SphereData(true, 21, 16), new SphereData(true, 12, 17), new SphereData(true, 25, 18), new SphereData(true, 9, 19),
                new SphereData(true, 6, 20), new SphereData(true, 11, 21), new SphereData(true, 7, 22), new SphereData(true, 5, 23),
                new SphereData(true, 13, 24)
            }
        },
        { TaskCondition.Near_AllPhysical, new List<SphereData>
            {
                new SphereData(true, 4, 0), new SphereData(true, 12, 1), new SphereData(true, 22, 2), new SphereData(true, 17, 3),
                new SphereData(true, 10, 4), new SphereData(true, 9, 5), new SphereData(true, 20, 6), new SphereData(true, 3, 7),
                new SphereData(true, 7, 8), new SphereData(true, 24, 9), new SphereData(true, 11, 10), new SphereData(true, 16, 11),
                new SphereData(true, 19, 12), new SphereData(true, 2, 13), new SphereData(true, 8, 14), new SphereData(true, 18, 15),
                new SphereData(true, 21, 16), new SphereData(true, 6, 17), new SphereData(true, 25, 18), new SphereData(true, 23, 19),
                new SphereData(true, 13, 20), new SphereData(true, 14, 21), new SphereData(true, 5, 22), new SphereData(true, 15, 23),
                new SphereData(true, 1, 24)
            }
        },
        { TaskCondition.Far_HalfVirtualHalfPhysical, new List<SphereData>
            {
                new SphereData(true, 25, 0), new SphereData(true, 21, 1), new SphereData(true, 12, 2), new SphereData(true, 8, 3),
                new SphereData(true, 22, 4), new SphereData(false, 7, 5), new SphereData(false, 14, 6), new SphereData(false, 15, 7),
                new SphereData(true, 5, 8), new SphereData(false, 13, 9), new SphereData(true, 23, 10), new SphereData(true, 3, 11),
                new SphereData(false, 1, 12), new SphereData(false, 18, 13), new SphereData(false, 4, 14), new SphereData(false, 17, 15),
                new SphereData(true, 19, 16), new SphereData(false, 9, 17), new SphereData(false, 10, 18), new SphereData(true, 24, 19),
                new SphereData(false, 20, 20), new SphereData(true, 11, 21), new SphereData(true, 6, 22), new SphereData(false, 2, 23), 
                new SphereData(true, 16, 24)
            }
        },
        { TaskCondition.Near_HalfVirtualHalfPhysical, new List<SphereData>
            {
                new SphereData(false, 25, 0), new SphereData(true, 3, 1), new SphereData(false, 22, 2), new SphereData(true, 4, 3),
                new SphereData(false, 17, 4), new SphereData(true, 20, 5), new SphereData(false, 13, 6), new SphereData(true, 12, 7),
                new SphereData(false, 14, 8), new SphereData(true, 11, 9), new SphereData(false, 10, 10), new SphereData(true, 8, 11),
                new SphereData(false, 21, 12), new SphereData(false, 24, 13), new SphereData(false, 6, 14), new SphereData(true, 5, 15),
                new SphereData(false, 9, 16), new SphereData(false, 1, 17), new SphereData(false, 18, 18), new SphereData(true, 16, 19),
                new SphereData(false, 15, 20), new SphereData(true, 23, 21), new SphereData(true, 7, 22), new SphereData(true, 19, 23),
                new SphereData(true, 2, 24)
            }
        }
    };
}





[System.Serializable]
public class TaskData
{
    public List<ConditionData> conditions = new List<ConditionData>(); // Store multiple trials in one task
    public float taskStartTime;
    public float taskEndTime;
    
}

[System.Serializable]
public class ConditionData
{
    public int conditionID;  // Number of the condition

    public int depth; // 0 for Near; 1 for Far
    public int physicality; // 0 for All Virtual; 1 for Half Physical and Half Virtual; 2 for All Physical

    public float conditionStartTime;
    public float conditionEndTime;

    public float conditionSpentTime;

    public List<BlockData> blockDataList=new List<BlockData>();
}


[System.Serializable]
public class BlockData{
    
    public int blockID;
    public List<TrialData> trials = new List<TrialData>();
    public List<BallData> ballArrangements = new List<BallData>();

    public float blockStartTime;

    public float blockEndTime;

    public float blockSpentTime;


}



[System.Serializable]
public class BallData
{
    public int index;  // The label of the ball---the number on the target object
    public int positionIndex;  // The position index of the ball, like the first ball will be at the position '0'

    public int objectPhysicality; // The physicality of the target-- 0 for virtual, 1 for physical
}

[System.Serializable]
public class TrialData
{
    public int targetLabel; // the index of target object

    public int depthSelection; // 0 for Near; 1 for Far

     public int physicalitySelection; // 0 for All Vitrual; 1 for Half Physical and Half Virtual; 2 for All Physical

    public int numSelections; // how many selections does user perform for this trial

    public List<SelectionData> selections=new List<SelectionData>();

    public float totalDistanceEachTrial; // the total distance of all selections between the hitpoint and the center of the current target
    
    public float timePerTrial; // the spent time of current trial
    

    public float headmMovementEachTrial; //total head movement of current trial

    public float headRotationEachTrial; //total head rotation of current trial


    public float controllerMovmentEachTrial; //total controller movement of current trial

    public float controllerRotationEachTrial; //total controller rotation of current trial
}

[System.Serializable]
public class SelectionData{
    public int selectedLabel; // the label of selected object

    public bool isCorrect; // if user correctly selected the target, the last selection of the trial should be correct, previous ones should be incorrect

    public float timePerSelection; //the spend time of this selection
    public float distanceOfSelection; //the distance of current selection between the hitpoint and the center of the current target


    public float headmMovementEachSelection; //head movement from last selection finished to current selection finish

    public float headRotationEachSelection; //head rotation from last selection finished to current selection finish


    public float controllerMovmentEachSelection; //controller movement from last selection finished to current selection finish

    public float controllerRotationEachSelection; //controller rotation from last selection finished to current selection finish




}


[System.Serializable]
    struct PP_Frame
    {   
        public int frameID;
        public long timestamp;

        public int conditionID;
        public int blockID;

        public float[] targetCenterPos; // Store Vector3 as a float array
        public float[] headPos;         // Store Vector3 as a float array
        public float[] headRot;         // Store Quaternion as a float array (x, y, z, w)

        public float[] fixationPt;
        public float[] leftEyePos;
        public float[] rightEyePos;
        public float[] leftEyeRot;
        public float[] rightEyeRot;

        public float fixationConfidence;
        public long gazeTimestamp;
        public float[] controllerPos;
        public float[] controllerRot;

        // Constructor to initialize the structure
        public PP_Frame(int frameID, long timestamp, int conditionID, int blockID, 
                        Vector3 targetCenterPos, Vector3 headPos, Quaternion headRot, 
                        Vector3 fixationPt, Vector3 leftEyePos, Vector3 rightEyePos, 
                        Quaternion leftEyeRot, Quaternion rightEyeRot, float fixationConfidence, 
                        long gazeTimestamp, Vector3 controllerPos, Quaternion controllerRot)
        {
            this.frameID = frameID;
            this.timestamp = timestamp;
            this.conditionID = conditionID;
            this.blockID = blockID;

            // Convert Unity types to arrays
            this.targetCenterPos = new float[] { targetCenterPos.x, targetCenterPos.y, targetCenterPos.z };
            this.headPos = new float[] { headPos.x, headPos.y, headPos.z };
            this.headRot = new float[] { headRot.x, headRot.y, headRot.z, headRot.w };

            this.fixationPt = new float[] { fixationPt.x, fixationPt.y, fixationPt.z };
            this.leftEyePos = new float[] { leftEyePos.x, leftEyePos.y, leftEyePos.z };
            this.rightEyePos = new float[] { rightEyePos.x, rightEyePos.y, rightEyePos.z };
            this.leftEyeRot = new float[] { leftEyeRot.x, leftEyeRot.y, leftEyeRot.z, leftEyeRot.w };
            this.rightEyeRot = new float[] { rightEyeRot.x, rightEyeRot.y, rightEyeRot.z, rightEyeRot.w };

            this.fixationConfidence = fixationConfidence;
            this.gazeTimestamp = gazeTimestamp;

            this.controllerPos = new float[] { controllerPos.x, controllerPos.y, controllerPos.z };
            this.controllerRot = new float[] { controllerRot.x, controllerRot.y, controllerRot.z, controllerRot.w };
        }

    }

[System.Serializable]
public struct Vector3Serializable
{
    public float x;
    public float y;
    public float z;

    public Vector3Serializable(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public static implicit operator Vector3Serializable(Vector3 vector)
    {
        return new Vector3Serializable(vector);
    }

    public static implicit operator Vector3(Vector3Serializable vector)
    {
        return new Vector3(vector.x, vector.y, vector.z);
    }
}

[System.Serializable]
public struct QuaternionSerializable
{
    public float x;
    public float y;
    public float z;
    public float w;

    public QuaternionSerializable(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }

    public static implicit operator QuaternionSerializable(Quaternion quaternion)
    {
        return new QuaternionSerializable(quaternion);
    }

    public static implicit operator Quaternion(QuaternionSerializable quaternion)
    {
        return new Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }
}
