using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.ARFoundation;
using InputDevice = UnityEngine.XR.InputDevice;
using GestureClassification = UnityEngine.XR.MagicLeap.InputSubsystem.Extensions.MLGestureClassification;

using static UnityEngine.XR.MagicLeap.MLMarkerTracker;
using MarkerSettings = UnityEngine.XR.MagicLeap.MLMarkerTracker.TrackerSettings;

using StudyI;

public class StudyIManager : MonoBehaviour
{
    public Camera mainCamera;
    public Transform fixationPoint;
    //public GameObject PhysicalLayout;
    public GameObject VirtualLayout;
    public GameObject OcclusionLayout;
    public Transform[] Positions;
    public GameObject Position;
    public GameObject TestSphere;
    //public GameObject TargetSizes;
    //public GameObject Location;
    public GameObject Gesture;
    public GameObject Physicality;
    public TMPro.TextMeshProUGUI AlwaysOn;
    public GameObject Table;
    public GameObject Success;
    public GameObject AnchorPt;
    [SerializeField] LineRenderer PointerRayViz;
    [SerializeField] LayerMask physicalMask;
    [SerializeField] LayerMask virtualMask;
    [SerializeField] LayerMask timer;
    private Ray PointerRay;
    private Ray gazeRay;
    private RaycastHit hitData;
    private GameObject activeObject;
    private bool targetActive;
    private List<Bone> handBones = new List<Bone>();
    private Bone wristBone;
    static private int layer = 9;
    private HandRay handRay;
    private Vector3 rayOrigin;
    private Vector3 rayDirection;
    private Vector3 anchorPos;
    private Quaternion anchorRot;
    private Vector3 coarsePos;
    private Quaternion coarseRot;
    private Color numberColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);


    // Used to get ml inputs.
    private MagicLeapInputs mlInputs;

    private UnityEngine.InputSystem.XR.Eyes eyes;
    private MagicLeapInputs.EyesActions eyesActions;
    private InputDevice eyesDevice;

    private MagicLeapInputs.ControllerActions _controller;

    private MLAnchors.Request query;
    private Timer localizationInfoUpdateTimer;
    MLAnchors.LocalizationInfo localizationInfo;
    MLSpace.LocalizationResult spaceLocalization;
    MLSpace.SpaceInfo spaceInfo;
    private List<MLSpace.Space> Spaces;
    private string spaceFileName = "exported_space.bin";
    //GO FIX THIS WHEN CREATING ANCHORS
    private Dictionary<string, string> anchorMap = new Dictionary<string, string>();
    //private Dictionary<string, string> TargetShuffleMap = new Dictionary<string, string>();
    private Dictionary<int, int> TargetShuffleMap = new Dictionary<int, int>();
    public Dictionary<string, List<Transform>> targetMap = new Dictionary<string, List<Transform>>();
    private Dictionary<string, bool> targetAnchored = new Dictionary<string, bool>();
    public String[] targets;

    // Was EyeTracking permission granted by user
    private bool permissionGranted = false;
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    private DatabaseReference firebaseRef;
    private DatabaseReference firebaseUser;
    private DatabaseReference firebaseSession;

    private StudyObject obj;
    private Frame currentFrame;
    private Trial currentTrial;
    StudyDescription studyDesc;
    StudyOptions studyOps;
    private StudyMode[] modes;

    private string userId;
    private long timeStamp;
    private long prevLogTime;
    private long startTime;
    private string activeTarget;
    private int sceneNum;
    private int sessionNum;
    private int logSessionNum;
    private int frameNum;
    private int trialNum;
    private int logTrialNum;
    private int anchorNum;
    private Stage stage;
    private Handedness currentHand;
    private StudyMode mode;
    private Distances dist;
    private StudyI.Pointer currentPointer;
    private StudyI.Pointer activePointer;
    private bool studyInProgress;
    private bool targetSelected;
    private bool targetAcknowledged;
    private bool gazeValid;
    private bool moveOn;
    private bool countdownActive;
    private bool scanMarkers=false;
    private bool notScanned = true;
    private AnchorControl controlMode;
    private bool nextAnchor;
    private AnchorMode anchorMode;
    private string anchorName = "";
    private bool occluders = false;
    private bool showStats = false;
    private bool blockTrigger = false;
    private bool anchorPlane = false;
    private bool planeAnchored = false;
    private bool anchorFine = false;
    private float anchor_distance = Const.ANCHOR_DISTANCE;
    private string filePath;
    private float rotationConst = 0.1f;
    private float positionConst = 0.1f;
/*
    private string[][] physicality = new string[Const.NUM_CONDITIONS+Const.NUM_TUT][];
    private int[][] positions = new int[Const.NUM_CONDITIONS+Const.NUM_TUT][];
    private Dictionary<StudyMode, Dictionary<Distances, int>> SessionMap;*/

    private JsonSerializerSettings settings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    [SerializeField]
    private MagicLeap.Examples.MarkerVisual markerVisualPrefab;


    public MarkerType MarkerTypes = MarkerType.All;
    public ArucoDictionaryName ArucoDicitonary;
    public float ArucoMarkerSize = 0.1f;
    public int ArucoTrackingCamera = 0;
    public Profile TrackerProfile = Profile.Default;
    public FPSHint FPSHint;
    public ResolutionHint ResolutionHint;
    public CameraHint CameraHint;
    public FullAnalysisIntervalHint FullAnalysisIntervalHint;
    public CornerRefineMethod CornerRefineMethod;
    public bool UseEdgeRefinement;
    private bool removeMarkersUsingTimeStamps = false;
    private float markerTrackerTimeout = 0.5f;

    private List<KeyValuePair<string, MagicLeap.Examples.MarkerVisual>> markers = new();
    private System.Text.ASCIIEncoding asciiEncoder = new System.Text.ASCIIEncoding();
    public MarkerSettings markerSettings;
    private Transform xrOrigin;


    public void NewUser(bool first = false)
    {
        if (first)
        {
            Debug.Log("first!");
            //PhysicalLayout.SetActive(false);
            foreach (Transform t in VirtualLayout.transform)
            {
                targetMap[t.gameObject.name[1..]] = new List<Transform>();
                targetMap[t.gameObject.name[1..]].Add(t);
                Debug.Log(t.gameObject.name + "added!");
                t.gameObject.SetActive(false);
            }
            //VirtualLayout.SetActive(false);
            foreach (Transform t in OcclusionLayout.transform)
            {
                targetMap[t.gameObject.name[1..]].Add(t);
                foreach (Transform c in t)
                {
                    c.gameObject.SetActive(false);
                }
                Debug.Log(t.gameObject.name + "added!");
                t.gameObject.SetActive(false);
            }

            //foreach (Transform t in TargetSizes.transform)
            //{
            //    GameObject g = t.gameObject;
            //    targetMap[g.name[1..]] = new List<Transform>();
            //    targetMap[g.name[1..]].Add(g.transform);
            //}

            userId = "S_" + DateTime.Now.ToString("MMdd_HHmmss_tt");
            obj = new StudyObject(userId);
        }
        else
        {
            activeObject.GetComponent<Outline>().enabled = false;
            firebaseRef.Child(Const.FIREBASE_PATH).Child(userId).SetRawJsonValueAsync(JsonConvert.SerializeObject(obj, Formatting.Indented, settings));
            StreamWriter sw = new StreamWriter(Path.Combine(Application.persistentDataPath, userId + ".json"));
            sw.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented, settings));
            sw.Close();
            userId = "S_" + DateTime.Now.ToString("MMdd_HHmmss_tt");
            obj.ResetObject(userId, 0);
        }


        sessionNum = -1;
        firebaseRef.Child(Const.FIREBASE_PATH).Child("_current").SetValueAsync(userId);
        firebaseRef.Child(Const.FIREBASE_PATH).Child(userId).SetRawJsonValueAsync(JsonUtility.ToJson(obj));
        firebaseUser = firebaseRef.Child(Const.FIREBASE_PATH).Child(userId);
        currentHand = Handedness.right;
        AlwaysOn.text = "started new study\n\n Choose one of five modes: \nPhysical\nVirtual\nMixed\nIndividual Trials\nDifferent Target Sizes.";
        sessionNum = -1;
        logSessionNum = -1;
    }


    private void NewMode(int num)
    {

        Success.SetActive(false);
        showStats = false;
        ComputeStats(showStats);
        sessionNum = num;
        //sessionNum += 1;
        logSessionNum += 1;
        obj.log.session = sessionNum;
        obj.sessions[sessionNum] = new SessionRecording(userId);
        obj.sessions[sessionNum].stats = new Statistics(targets);
        obj.sessions[sessionNum].mode = mode.ToString();
        

        //Randomizes the order of targets if scene is fully virtual
        //REMOVE BEFORE FINAL BUILD
        /*        if (userTrials[sceneNum].mode == StudyMode.virt)
                {
                    var keys = new List<string>();
                    TargetShuffleMap = new Dictionary<string, string>();
                    foreach (TrialDesc t in userTrials[sceneNum].trials)
                    {
                        keys.Add(t.targetNum);
                    }
                    if (keys.Count > 0)
                    {
                        var vals = keys.Select(s => s).ToList();
                        keys.Shuffle();
                        for (int j = 0; j < keys.Count; j++)
                        {
                            TargetShuffleMap.Add(keys[j], vals[j]);
                        }
                    }
                }*/

        TargetShuffleMap = new Dictionary<int, int>();
        TrialDesc t;
        for (int j = 0; j < studyDesc.sessions[sessionNum].trials.Count; j++)
        {
            t = studyDesc.sessions[sessionNum].trials[j];
            TargetShuffleMap.Add(j, t.position);
            if(t.physicality == "P")
            {
                targetMap[t.targetNum][0].gameObject.SetActive(false);
                targetMap[t.targetNum][1].gameObject.SetActive(true);
            }
            else
            {
                targetMap[t.targetNum][0].gameObject.SetActive(true);
                targetMap[t.targetNum][1].gameObject.SetActive(false);
            }
        }

        startTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        obj.sessions[sessionNum].startTime = startTime;
        //obj.sessions[sessionNum].stats = new Statistics(targets);
        firebaseSession = firebaseUser.Child("sessions/" + logSessionNum.ToString());
        firebaseSession.Child("mode").SetValueAsync(mode.ToString());
        currentFrame = new Frame();
        currentTrial = new Trial();
        anchorMode = AnchorMode.none;
        prevLogTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        trialNum = -1;
        frameNum = 0;
        logTrialNum = -1;
        targetAcknowledged = false;
        targetActive = false;
        targetSelected = false;
        stage = Stage.select;
        studyInProgress = true;
        NextTrial();
    }

    private void NextTrial()
    {
        AlwaysOn.text = "next trial";
        string message = "next trial";
        firebaseUser.Child("message").SetValueAsync(message);
        //TODO 2/11: problem is here
        if (trialNum != -1)
        {
            firebaseSession.Child("trials").Child(logTrialNum.ToString()).SetRawJsonValueAsync(JsonUtility.ToJson(currentTrial));
            obj.sessions[sessionNum].trials.Add(currentTrial);
            firebaseUser.Child("message").SetValueAsync("saved obj " + currentTrial.status.ToString());
            if (currentTrial.status)
            {
                message += " " + currentTrial.targetNum + " ";
                firebaseUser.Child("message").SetValueAsync(message);
                obj.sessions[sessionNum].stats.trialCounts[currentTrial.targetNum] += 1;
                obj.sessions[sessionNum].stats.times[currentTrial.targetNum].Add(currentTrial.selectTime);
                obj.sessions[sessionNum].stats.error[currentTrial.targetNum].Add(currentTrial.selectAngleError);
                obj.sessions[sessionNum].stats.misclicks[currentTrial.targetNum] += 1;
                firebaseSession.Child("stats").SetRawJsonValueAsync(JsonConvert.SerializeObject(obj.sessions[sessionNum].stats, Formatting.Indented, settings));
            }
            message += "stats";
            firebaseUser.Child("message").SetValueAsync(message);
            //activeObject.GetComponent<Outline>().enabled = false;
            message += "saved previous";
            firebaseUser.Child("message").SetValueAsync(message);
        }
        AlwaysOn.text = "old trial num: " + trialNum;
        trialNum += 1;
        logTrialNum += 1;
        var temp = studyDesc.sessions[sessionNum];
        if (trialNum == studyDesc.sessions[sessionNum].trials.Count)
        {
           
                Success.SetActive(true);
                studyInProgress = false;
                firebaseUser.Child("message").SetValueAsync("select next mode");
                return;


        }
        AlwaysOn.text = "new trial num: " + trialNum;
        currentTrial.ResetTrial(studyDesc.sessions[sessionNum].trials[trialNum]);
        firebaseUser.Child("message").SetValueAsync("reset trial " + currentTrial.targetNum);
        Debug.Log("reset trial");
        Debug.Log(currentTrial.targetNum);
        AlwaysOn.text = "new trial" + currentTrial.targetNum;
        //activeObject = Targets[sceneNum][trialNum];
        message += "active object";
        firebaseUser.Child("message").SetValueAsync(message);
        stage = Stage.select;

        obj.log.targetActive = targetActive;
        obj.log.trialNum = trialNum + 1;
        obj.log.targetNum = currentTrial.targetNum;
        obj.log.acknowledged = false;
        obj.log.selected = false;
        countdownActive = false;
        moveOn = false;
        targetSelected = false;
    }

    private async Task WaitOneSecondAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    public async void WaitNextTrial(GameObject change, int time, bool switchMoveOn = false)
    {
        foreach (int i in Enumerable.Range(0, time))
        {
            await WaitOneSecondAsync();
        }
        change.GetComponent<Outline>().enabled = false;
        if (switchMoveOn)
        {
            moveOn = true;
        }

    }

    public void ComputeStats(bool show)
    {
        if (!show)
        {
            Physicality.SetActive(false);
            Gesture.SetActive(false);
            AlwaysOn.transform.gameObject.SetActive(true);
            

        }
        else
        {
            AlwaysOn.transform.gameObject.SetActive(false);
                string statStringBuilder = "";
                string physString = "Physical: no data yet";
                string virtString = "Virtual: no data yet";
                string mixedString = "Mixed: no data yet";
                if (sessionNum == -1)
                {
                    statStringBuilder = "<color=#B7B7B8><b>No Data Yet</b></color>";
                }
                else
                {
                    statStringBuilder = "<color=#B7B7B8><b>Statistics</b></color>";
                    statStringBuilder += "\n<color=#B7B7B8><b>By Mode</b></color>: \n";
                    physString = "Physical: no data yet\n";
                    virtString = "Virtual: no data yet\n";
                    mixedString = "Mixed: no data yet\n";
                    /*                    float[] times = new float[sessionNum + 1];
                                        int[] counts = new int[sessionNum + 1];
                                        string[] modes = new string[sessionNum + 1];*/
                    for (int i = 0; i < sessionNum + 1; i++)
                    {
                        if (obj.sessions[i].trials.Count == 0)
                        {
                            continue;
                            //statStringBuilder += obj.sessions[i].mode.ToString() + " :  no data\n";
                        }
                        else if (obj.sessions[i].mode == StudyMode.virt.ToString())
                        {
                            virtString = "Virtual: " + obj.sessions[i].trials[obj.sessions[i].trials.Count - 1].selectTime / 1000.0f + " s, " + obj.sessions[i].trials.Count.ToString() + " trials\n";
                        }
                        else if (obj.sessions[i].mode == StudyMode.phys.ToString())
                        {
                            physString = "Physical: " + obj.sessions[i].trials[obj.sessions[i].trials.Count - 1].selectTime / 1000.0f + " s, " + obj.sessions[i].trials.Count.ToString() + " trials\n";
                        }
                        else if (obj.sessions[i].mode == StudyMode.mixed.ToString())
                        {
                            mixedString = "Mixed: " + obj.sessions[i].trials[obj.sessions[i].trials.Count - 1].selectTime / 1000.0f + " s, " + obj.sessions[i].trials.Count.ToString() + " trials\n";
                        }
                        /*
                                                else
                                                {

                                                    *//*                            times[j] = obj.sessions[j].trials.Last().selectTime / 1000.0f;
                                                                                counts[j] = obj.sessions[j].trials.Count;
                                                                                modes[j] = obj.sessions[i].mode;*//*
                                                    statStringBuilder += obj.sessions[i].mode.ToString() + " : " + obj.sessions[j].trials[obj.sessions[j].trials.Count-1].selectTime / 1000.0f + " s, " + obj.sessions[j].trials.Count.ToString() + " trials\n";
                                                }*/

                    }
                    statStringBuilder += physString + virtString + mixedString;
                }
                //string[] temp = statStringBuilder.Split("\n");
                //[] newString = temp[(temp.Length>10?(temp.Length - 10):0)..temp.Length];
                //statStringBuilder = "<color=#B7B7B8><b>Statistics</b></color>\n<color=#B7B7B8><b>By Mode</b></color>: \n" + string.Join("\n", newString);

                Gesture.GetComponent<TMPro.TextMeshProUGUI>().text = statStringBuilder;
                Gesture.SetActive(true);
            
        }

    }

    private void MisClick()
    {
        obj.sessions[sessionNum].stats.misclicks[currentTrial.targetNum] += 1;
    }

    private void SetPointer()
    {


        rayOrigin = _controller.Position.ReadValue<Vector3>();
        rayDirection = _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward;
        PointerRayViz.gameObject.SetActive(true);
        PointerRayViz.SetPositions(new Vector3[2] { rayOrigin, rayOrigin + (rayDirection * 10000.0f) });
        PointerRay = new Ray(rayOrigin, rayDirection * 10000.0f);

        if (Physics.Raycast(ray: PointerRay, hitInfo: out hitData, maxDistance: 100.0f, layerMask: physicalMask | virtualMask))
        {
            if (!targetActive)
            {
                activeTarget = hitData.collider.gameObject.name[1..];
                if (activeTarget == currentTrial.targetNum)
                {
                    targetActive = true;
                    activeObject = hitData.collider.gameObject;
                }
            }

        }
        else if (targetActive)
        {
            targetActive = false;
        }

    }

    private void CheckSelector()
    {
        bool temp = _controller.Trigger.WasPressedThisFrame();
        if ( temp && !targetSelected && stage == Stage.select)
        {
            Debug.Log("trigger pressed");
            if (targetActive)
            {
                targetSelected = true;
                activeObject.GetComponent<Outline>().enabled = true;
                activeObject.GetComponent<Outline>().OutlineColor = Color.green;
                currentTrial.status = true;
                currentTrial.selectPoint = hitData.point;
                currentTrial.selectTime = timeStamp - startTime;


                Debug.Log(hitData.point - mainCamera.transform.position);
                Debug.Log(activeObject.transform.position - mainCamera.transform.position);
                currentTrial.selectAngleError = Vector3.Angle((hitData.point - mainCamera.transform.position), (activeObject.transform.position - mainCamera.transform.position));
                currentTrial.objectDistance = Vector3.Distance(mainCamera.transform.position, activeObject.transform.position);
                currentTrial.targetCenter = activeObject.transform.position;
                currentTrial.pointer = currentPointer.ToString();

                moveOn = true;
                WaitNextTrial(activeObject, 1);
                AlwaysOn.text = "switch move on";
            }
            else
            {
                MisClick();
            }


        }

    }

    private void changeColor()
    {
        //foreach (Transform t in VirtualLayout.transform)
        //{
        //    t.Find("Number").Find("Text (TMP)").GetComponent<TMPro.TextMeshProUGUI>().color = numberColor;
        //}
    }
    private async void WaitNextAnchor(int time)
    {
        foreach (int i in Enumerable.Range(0, time))
        {
            await WaitOneSecondAsync();
        }
        blockTrigger = false;
    }

    private void AnchorTargets()
    {
        if (nextAnchor)
        {
            while (anchorNum < targets.Length && targetAnchored[targets[anchorNum]] == true)
            {
                anchorNum++;
            }
            if (anchorNum >= targets.Length)
            {
                Debug.Log("done");
                Debug.Log(anchorNum);
                Debug.Log(targets.Length);
                firebaseUser.Child("message").SetValueAsync("anchoring done");
                AlwaysOn.text = "finished anchoring targets, press 'anch' to exit mode";
            }
            else
            {
                //firebaseUser.Child("log/targetNum").SetValueAsync(targets[anchorNum]);
                firebaseUser.Child("message").SetValueAsync("set target locations " + targets[anchorNum]);
            }
        }
        nextAnchor = false;
    }

    //private void UpdateTargetLocations()
    //{
    //    var mlResultStart = query.Start(new MLAnchors.Request.Params(mainCamera.transform.position, 0, 0, true));
    //    var mlResultGet = query.TryGetResult(out MLAnchors.Request.Result result);

    //    if (mlResultStart.IsOk && mlResultGet.IsOk)
    //    {
    //        foreach (var anchor in result.anchors)
    //        {
    //            string id = anchor.Id;
    //            if (anchorMap.ContainsKey(id))
    //            {
    //                if (TargetShuffleMap.ContainsKey(anchorMap[id]))
    //                {
    //                    foreach (Transform t in targetMap[TargetShuffleMap[anchorMap[id]]])
    //                    {
    //                        t.position = anchor.Pose.position;
    //                        t.rotation = anchor.Pose.rotation;
    //                    }
    //                }
    //                else
    //                {
    //                    foreach (Transform t in targetMap[anchorMap[id]])
    //                    {
    //                        t.position = anchor.Pose.position;
    //                        t.rotation = anchor.Pose.rotation;
    //                    }
    //                }

    //            }
    //            else if (id == planeId)
    //            {
    //                Table.transform.position = anchor.Pose.position;
    //                Table.transform.rotation = anchor.Pose.rotation;
    //            }
    //        }
    //    }
    //    if (anchorMode != AnchorMode.none && nextAnchor == false && blockTrigger == false && anchorNum < targets.Length)
    //    {

    //        if (anchorFine)
    //        {
    //            AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\nfine-tuning " + targets[anchorNum] + ", press 'w' to confirm.";
    //            foreach (Transform t in targetMap[targets[anchorNum]])
    //            {
    //                t.position = anchorPos;
    //                t.rotation = anchorRot;
    //            }
    //        }
    //        else
    //        {
    //            AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\ncoarse anchoring " + targets[anchorNum] + ", press trigger to fine-tune.";
    //            foreach (Transform t in targetMap[targets[anchorNum]])
    //            {
    //                t.position = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
    //                t.rotation = _controller.Rotation.ReadValue<Quaternion>();
    //            }
    //        }

    //    }
    //    if (anchorPlane && !planeAnchored)
    //    {
    //        if (anchorFine)
    //        {
    //            AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\nfine-tuning table, press 'w' to confirm.";
    //            Table.transform.position = anchorPos;
    //            Table.transform.rotation = anchorRot;

    //        }
    //        else
    //        {
    //            AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\ncoarse anchoring table, press trigger to fine-tune.";
    //            Table.transform.position = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
    //            Table.transform.rotation = _controller.Rotation.ReadValue<Quaternion>();

    //        }
    //    }

    //}

    private void UpdateTargetLocations()
    {
#if !UNITY_EDITOR
        var mlResultStart = query.Start(new MLAnchors.Request.Params(mainCamera.transform.position, 0, 0, true));
        var mlResultGet = query.TryGetResult(out MLAnchors.Request.Result result);

        if (mlResultStart.IsOk && mlResultGet.IsOk)
        {
            foreach (var anchor in result.anchors)
            {
                string id = anchor.Id;
                if (id == anchorName)
                {
                    Position.transform.position = anchor.Pose.position;
                    Position.transform.rotation = anchor.Pose.rotation;
                }
            }
        }
#endif
        for (int i = 0; i < Const.NUM_TARGETS; i++)
        {
            foreach(Transform t in targetMap[(i + 1).ToString("D2")])
            {
                t.position = Positions[TargetShuffleMap[i]].transform.position;
                t.rotation = Positions[TargetShuffleMap[i]].transform.rotation;
            }
        }
    }

    private void HandleOnBumper(InputAction.CallbackContext obj)
    {
        string message = "bumper pressed. ";
        firebaseUser.Child("message").SetValueAsync(message);
        switch (anchorMode)
        {

            case AnchorMode.coarse:
                anchorMode = AnchorMode.fine;
                break;
        }
    }

    private void HandleOnTrigger(InputAction.CallbackContext obj)
    {
        switch (anchorMode)
        {
            case AnchorMode.none:
                /*                if (anchorPlane)
                                {
                                    anchorPos = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
                                    anchorRot = _controller.Rotation.ReadValue<Quaternion>();
                                    anchorFine = true;

                                }
                                break;*/
                break;
            case AnchorMode.coarse:
                //anchorPos = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
                //anchorRot = _controller.Rotation.ReadValue<Quaternion>();
                if(scanMarkers)
                {
                    anchorPos = Position.transform.position;
                    anchorRot = Position.transform.rotation;
                    scanMarkers = false;
                }
                else if (!scanMarkers)
                {
                    scanMarkers = true;
                }
                break;
            case AnchorMode.fine:
                anchorPos = Position.transform.position;
                anchorRot = Position.transform.rotation;
                if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
                {
                    
                    query.Start(new MLAnchors.Request.Params(_controller.Position.ReadValue<Vector3>(), 0, 0, true));
                    query.TryGetResult(out MLAnchors.Request.Result result);
                    if (result.anchors.Length > 0)
                    {
                        foreach (MLAnchors.Anchor anchorDel in result.anchors)
                        {
                            anchorDel.Delete();
                        }
                    }
                    MLAnchors.Anchor.Create(new Pose(anchorPos, anchorRot), 0, out MLAnchors.Anchor anchor);
                    anchor.Publish();
                    anchorName = anchor.Id;
                    AlwaysOn.text = "anchor created";
                }
                break;
        }

    }

    public void LocalizeToSpace()
    {
        string message = "localizing | ";
        firebaseUser.Child("message").SetValueAsync(message);
        MLSpace.GetSpaceList(out MLSpace.Space[] list);
        Spaces = new List<MLSpace.Space>(list);
        foreach (MLSpace.Space space in Spaces)
        {
            message += space.SpaceName + " | ";
            firebaseUser.Child("message").SetValueAsync(message);
            if (space.SpaceName == "ONRDemo")
            {
                message += space.SpaceName + " | ";
                firebaseUser.Child("message").SetValueAsync(message);
                spaceInfo.SpaceId = space.SpaceId;
                MLSpace.RequestLocalization(ref spaceInfo);
                message += "requested localization | ";
                firebaseUser.Child("message").SetValueAsync(message);
                /*MLSpace.GetLocalizationResult(out spaceLocalization);
                firebaseUser.Child("message").SetValueAsync(spaceLocalization.LocalizationStatus);*/
            }

        }
    }

    private void Awake()
    {
        AlwaysOn.text = "Awake...";
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        //MLSpace.OnLocalizationEvent += OnLocalizationChanged;

        /*        modes = new StudyMode[] { StudyMode.mixed,      //tutorial
                                          StudyMode.phys, StudyMode.mixed, StudyMode.virt, 
                                          StudyMode.phys, StudyMode.mixed, StudyMode.virt, 
                                          StudyMode.phys, StudyMode.mixed, StudyMode.virt};*/


        
        string path = "";
#if UNITY_EDITOR
        path = Path.Combine("Assets", Const.RESOURCES_PATH, Const.OPTIONS_PATH);
#else
        path = Path.Combine(Application.persistentDataPath, Const.RESOURCES_PATH, Const.OPTIONS_PATH);
#endif

        using (StreamReader reader = new StreamReader(path))
        {
            string json = reader.ReadToEnd();
            Debug.Log(json);
            studyOps = JsonConvert.DeserializeObject<StudyOptions>(json);
        }
        Debug.Log(studyOps.options[0].sessions.Count);
        studyDesc = new StudyDescription(studyOps.options[0]);
        Debug.Log(studyDesc.sessions.Count);
        /*        targets = new string[Const.NUM_TARGETS];
                targetMap = new Dictionary<string, List<Transform>>();
                int i = 0;
                foreach (Transform T in VirtualLayout.transform)
                {
                    GameObject g = T.gameObject;
                    Debug.Log(g.name);

                    targetMap[g.name[1..]] = new List<Transform>();
                    targetMap[g.name[1..]].Add(g.transform);
                    targets[i] = g.name[1..];
                    i++;

                    if (i >= Const.NUM_TARGETS)
                    {
                        break;
                    }
                }
                i = 0;
                foreach (Transform t in OcclusionLayout.transform)
                {
                    GameObject g = t.gameObject;
                    targetMap[g.name[1..]].Add(g.transform);
                    i++;
                    if (i >= Const.NUM_TARGETS)
                    {
                        break;
                    }
                }

                //0, 12, 24, 36, 48, 60, 72, 1, 13, 25, 37, 49, 61, 73, 85, 2, 14, 26, 38, 50, 62, 74, 86, 3, 15, 27, 39, 51, 63, 75, 87
                int[] virtPos = { 26, 50, 87, 12, 62, 74, 27, 3, 60, 73, 49, 13, 1, 39, 85, 14, 72, 37, 38, 15, 61, 48, 2, 86, 51, 75, 63, 0, 24, 74, 87 };
                //4, 16, 28, 40, 52, 64, 76, 88, 5, 17, 29, 41, 53, 65, 77, 89, 6, 18, 30, 42, 54, 66, 78, 90, 7, 19, 31, 43, 55, 67, 79, 91
                int[] mixedPos = { 6, 65, 31, 7, 17, 79, 5, 90, 18, 52, 77, 30, 54, 89, 41, 64, 66, 53, 55, 76, 29, 4, 67, 78, 91, 19, 88, 43, 42, 28, 40, 54, 18 };
                //8, 20, 32, 44, 56, 68, 80, 92, 9, 21, 33, 45, 57, 69, 81, 93, 10, 22, 34, 46, 58, 70, 82, 94, 11, 23, 35, 47, 59, 71, 83, 95
                int[] physPos = { 82, 71, 33, 23, 94, 92, 8, 34, 81, 68, 35, 46, 10, 22, 59, 11, 47, 93, 45, 20, 95, 70, 69, 80, 57, 92, 58, 9, 32, 44, 56, 21 };


                System.Random r = new System.Random();

                //need to do this in python, so it's static here.
                //virtPos = virtPos.OrderBy(x => r.Next()).ToArray();
                //physPos = physPos.OrderBy(x => r.Next()).ToArray();
                //mixedPos = mixedPos.OrderBy(x => r.Next()).ToArray();
                userTrials = new Description[Const.NUM_CONDITIONS + Const.NUM_TUT];

                for (i = 0; i < Const.NUM_CONDITIONS + Const.NUM_TUT; i++)
                {
                    userTrials[i] = new Description();
                    userTrials[i].trials = new List<TrialDesc>();
                    userTrials[i].mode = modes[i];
                    //Targets[i] = new GameObject[Scenes[i].transform.childCount];
                    TargetShuffleMap = new Dictionary<string, string>();
                    for(int j=0; j < Const.NUM_TARGETS; j++)
                    {
                        if (modes[i] == StudyMode.virt)
                            userTrials[i].trials.Add(new TrialDesc("V", i + 1.ToString("D2"), "V", "V", virtPos[i]));
                        else if (modes[i] == StudyMode.phys)
                            userTrials[i].trials.Add(new TrialDesc("P", i + 1.ToString("D2"), "P", "P", physPos[i]));
                        else
                            userTrials[i].trials.Add(new TrialDesc(i%2==0?"P":"V", i + 1.ToString("D2"), i % 2 == 0 ? "P" : "V", i % 2 == 0 ? "P" : "V", mixedPos[i]));
                    }
        *//*          for (int j = 0; j < Scenes[i].transform.childCount; j++)
                    {
                        g = Scenes[i].transform.GetChild(j).gameObject;
                        Targets[i][j] = g;
                    }
                    for (int j = 0; j < Scenes[i].transform.childCount; j++)
                    {
                        g = Targets[i][j];
                        if (j == 0)
                        {
                            userTrials[i].trials.Add(new TrialDesc(g.name[..0], g.name[1..], "", Targets[i][j + 1].name[..0], g.transform.position));
                        }
                        else if (j == Scenes[i].transform.childCount - 1)
                        {
                            userTrials[i].trials.Add(new TrialDesc(g.name[..0], g.name[1..], Targets[i][j - 1].name[..0], "", g.transform.position));
                        }
                        else
                        {
                            userTrials[i].trials.Add(new TrialDesc(g.name[..0], g.name[1..], Targets[i][j - 1].name[..0], Targets[i][j + 1].name[..0], g.transform.position));
                        }
                        Debug.Log("Target " + j.ToString("D2"));
                        TargetShuffleMap.Add((j).ToString("D2"), (j).ToString("D2"));
                        targetMap[g.name[1..]] = new List<Transform>();
                        targetMap[g.name[1..]].Add(g.transform);
                        targetAnchored.Add(g.name[1..], false);
                        targets[j] = g.name[1..];
                        Debug.Log(targets[j]);
                    }
                    Scenes[i].SetActive(false);*//*
                }*/
        currentPointer = StudyI.Pointer.controller;
        activePointer = StudyI.Pointer.controller;
        sessionNum = -1;
        //ONLY FOR MARKER TEST, UNCOMMENT FOR ACTUAL;
        //Position.SetActive(false);
    }
    // Start is called before the first frame update
    void Start()
    {
        AlwaysOn.text = "Start...";
        EnableMarkerTrackerExample();
        xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>().transform;

        firebaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        firebaseRef.Child(Const.FIREBASE_PATH).Child("_current").SetValueAsync("init");
        firebaseRef.Child(Const.FIREBASE_PATH).Child("init").Child("_valid").SetValueAsync("null");
        firebaseUser = firebaseRef.Child(Const.FIREBASE_PATH).Child("init");
        Debug.Log("firebase done");

        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();

        _controller = new MagicLeapInputs.ControllerActions(mlInputs);
        _controller.Bumper.performed += HandleOnBumper;
        _controller.Trigger.performed += HandleOnTrigger;

        MLPermissions.RequestPermission(MLPermission.EyeTracking, permissionCallbacks);

        //SETUP TARGETS AND ANCHORING
        //localizationInfoUpdateTimer = new Timer(3);
        query = new MLAnchors.Request();

        //query.Start(new MLAnchors.Request.Params(_controller.Position.ReadValue<Vector3>(), 0, 0, true));
        //query.TryGetResult(out MLAnchors.Request.Result result);
        //firebaseUser.Child("message").SetValueAsync("deleting old anchors");
        //if (result.anchors.Length > 0)
        //{
        //    foreach (var anchor in result.anchors)
        //    {
        //        anchor.Delete();
        //    }
        //}
        //firebaseUser.Child("message").SetValueAsync("deleted old anchors");

        //LocalizeToSpace();

        targetActive = false;
        currentPointer = StudyI.Pointer.controller;
        activePointer = StudyI.Pointer.controller;
        fixationPoint.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        timeStamp = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();

        //READ KEYBOARD INPUT IF ANY
        Keyboard kboard = Keyboard.current;

        if (kboard.anyKey.wasPressedThisFrame)
        {
            char key = 'q';
            foreach (KeyControl k in kboard.allKeys)
            {
                if (k.wasPressedThisFrame)
                {
                    Debug.Log((int)k.keyCode + " " + k.path);
                    key = k.path[k.path.Length - 1];
                    break;
                }
            }
            if (key == 'r')
            {
                NewUser(!occluders);
                occluders = true;
            }
            else if (key == 'f' && studyInProgress)
            {
                currentTrial.status = false;
                NextTrial();
            }

            else if (key == 's')
            {
                /*anchorPlane = false;
                switch (anchorMode)
                {
                    case AnchorMode.none:
                        anchorMode = AnchorMode.single;
                        Success.SetActive(false);
                        showStats = false;
                        ComputeStats(showStats);
                        AlwaysOn.SetActive(true);
                        AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "anchoring targets, press 'anch' to exit mode";
                        anchor_distance = 0.5f;
                        Table.transform.Find("Table").gameObject.SetActive(true);
                        AnchorPt.SetActive(true);
                        anchorFine = false;
                        TargetShuffleMap = new Dictionary<string, string>();
                        foreach (string s in targets)
                        {
                            foreach (Transform t in targetMap[s])
                            {
                                t.gameObject.SetActive(true);
                            }
                        }
                        break;
                    case AnchorMode.single:
                        anchorMode = AnchorMode.none;
                        AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "exited target anchor mode\n\nPress 'reset' to start study";
                        Table.transform.Find("Table").gameObject.SetActive(false);
                        AnchorPt.SetActive(false);
                        break;
                        AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.0f, 0.0f, 0.0f);
                }
                Debug.Log("anchorplane");
                Debug.Log(anchorPlane);
                firebaseUser.Child("log/anchor").SetValueAsync(anchorMode.ToString());*/

            }
            else if (key == 't')
            {
                firebaseUser.Child("message").SetValueAsync("showing stats");
                showStats = !showStats;
                ComputeStats(showStats);
            }
            else if (key == 'a')
            {
                switch(anchorMode)
                {
                    case AnchorMode.none:
                        anchorMode = AnchorMode.coarse;
                        AlwaysOn.text = "anchoring targets, press trigger to scan for markers, 'd' to switch between position/rotation, and 'w' to confirm position";
                        scanMarkers = false;
                        Position.SetActive(true);
                        break;
                    case AnchorMode.coarse:
                        anchorMode = AnchorMode.fine;
                        break;
                    case AnchorMode.fine:
                        anchorMode = AnchorMode.none;
                        Position.SetActive(false);
                        break;
                }
            }
            else if (key == 'g')
            {

                if (numberColor.r < 1.0f)
                {
                    numberColor += new Color(0.1f, 0.1f, 0.1f, 0.0f);
                    changeColor();
                }

            }
            else if (key == 'h')
            {
                if (numberColor.r > 0.0f)
                {
                    numberColor -= new Color(0.1f, 0.1f, 0.1f, 0.0f);
                    changeColor();
                }

            }
            else if (key == 'q')
            {
                if(controlMode == AnchorControl.pos)
                {
                    positionConst -= 0.1f;
                    AlwaysOn.text = "position resolution: " + positionConst.ToString("F1");
                }
                else
                {
                    rotationConst -= 0.1f;
                    AlwaysOn.text = "rotation resolution: " + rotationConst.ToString("F1");
                }
                
            }
            else if (key == 'e')
            {
                if (controlMode == AnchorControl.pos)
                {
                    positionConst += 0.1f;
                    AlwaysOn.text = "position resolution: " + positionConst.ToString("F1");
                }
                else
                {
                    rotationConst += 0.1f;
                    AlwaysOn.text = "rotation resolution: " + rotationConst.ToString("F1");
                }
            }
            else if(key == 'p')
            {
                controlMode = AnchorControl.pos;
            }
            else if(key == 'o')
            {
                controlMode = AnchorControl.rot;
            }
            else if (anchorMode == AnchorMode.fine)
            {
                if (key == '1')
                {
                    if (controlMode == AnchorControl.pos)
                    {
                        anchorPos.x += positionConst;
                    }
                    else
                    {
                        anchorRot *= Quaternion.AngleAxis(rotationConst, Vector3.right);
                    }
                }
                else if (key == '2')
                {
                    if (controlMode == AnchorControl.pos)
                    {
                        anchorPos.x -= positionConst;
                    }
                    else
                    {
                        anchorRot *= Quaternion.AngleAxis(-rotationConst, Vector3.right);
                    }
                }
                else if (key == '3')
                {
                    if (controlMode == AnchorControl.pos)
                    {
                        anchorPos.y += positionConst;
                    }
                    else
                    {
                        anchorRot *= Quaternion.AngleAxis(rotationConst, Vector3.up);
                    }
                }
                else if (key == '4')
                {
                    if (controlMode == AnchorControl.pos)
                    {
                        anchorPos.y -= positionConst;
                    }
                    else
                    {
                        anchorRot *= Quaternion.AngleAxis(-rotationConst, Vector3.up);
                    }
                }
                else if (key == '5')
                {
                    if (controlMode == AnchorControl.pos)
                    {
                        anchorPos.z += positionConst;
                    }
                    else
                    {
                        anchorRot *= Quaternion.AngleAxis(rotationConst, Vector3.forward);
                    }
                }
                else if (key == '6')
                {
                    if (controlMode == AnchorControl.pos)
                    {
                        anchorPos.z -= positionConst;
                    }
                    else
                    {
                        anchorRot *= Quaternion.AngleAxis(-rotationConst, Vector3.forward);
                    }
                }
                Position.transform.position = anchorPos;
                Position.transform.rotation = anchorRot;
            }
            else if (key >= '0' && key <= '9')
            {
                int.TryParse(key.ToString(), out int val);
                NewMode(val);
            }
        }

        //ANCHOR TARGETS
        //----------------------------------------------------------------------------------------
        //if (localizationInfoUpdateTimer.LimitPassed)
        //{
        //    localizationInfoUpdateTimer.Reset();

        //    // SPATIAL_ANCHOR is a normal permission; we don't request it at runtime - must be included in AndroidManifest.xml
        //    if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
        //    {
        //        MLAnchors.GetLocalizationInfo(out localizationInfo);
        //        firebaseUser.Child("log/localization").SetValueAsync(localizationInfo.LocalizationStatus.ToString());
        //        MLSpace.GetLocalizationResult(out spaceLocalization);
        //        //Timer.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = spaceLocalization.LocalizationStatus.ToString();
        //    }
        //    else
        //    {
        //        //Timer.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "anchors failed";
        //    }

        //}

        //if (anchorMode != AnchorMode.none)
        //{
        //    AnchorTargets();
        //    AnchorPt.transform.position = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
        //}

        if(sessionNum != -1)
        {
            UpdateTargetLocations();
        }

        //-----------------------------------------------------------

        //MARKER SCANNING
        if (scanMarkers == true && notScanned == true)
        {
            AlwaysOn.text = "Starting scanning...";
            _ = MLMarkerTracker.StartScanningAsync();
            notScanned = false;
        }
        else if(scanMarkers == false)
        {
            _ = MLMarkerTracker.StopScanningAsync();
            notScanned = true;
        }
            

        //---------------------------------------------------------

        //SET UP INPUT DEVICES
        if (!permissionGranted)
        {
            return;
        }

        if (!eyesDevice.isValid)
        {
            this.eyesDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.TrackedDevice);
            return;
        }

        /*if (!leftGestureDevice.isValid || !rightGestureDevice.isValid)
        {
            List<InputDevice> foundDevices = new List<InputDevice>();
            InputDevices.GetDevices(foundDevices);

            foreach (InputDevice device in foundDevices)
            {
                if (device.name == GestureClassification.LeftGestureInputDeviceName)
                {
                    leftGestureDevice = device;
                    continue;
                }

                if (device.name == GestureClassification.RightGestureInputDeviceName)
                {
                    rightGestureDevice = device;
                    continue;
                }

                if (leftGestureDevice.isValid && rightGestureDevice.isValid)
                {
                    break;
                }
            }
            return;
        }*/


        //EYE TRACKING
        // Eye data provided by the engine for all XR devices.
        // Used here only to update the status text. The 
        // left/right eye centers are moved to their respective positions &
        // orientations using InputSystem's TrackedPoseDriver component.
        eyes = eyesActions.Data.ReadValue<UnityEngine.InputSystem.XR.Eyes>();

        // Eye data specific to Magic Leap
        InputSubsystem.Extensions.TryGetEyeTrackingState(eyesDevice, out var trackingState);

        if (trackingState.FixationConfidence < Const.FIXATION_THRESHOLD && gazeValid)
        {
            gazeValid = false;
        }
        else if (trackingState.FixationConfidence >= Const.FIXATION_THRESHOLD && !gazeValid)
        {
            gazeValid = true;
        }
        Vector3 worldPosition = mainCamera.transform.position;
        //Vector3 worldPosition = (eyes.leftEyePosition + eyes.rightEyePosition) / 2;
        Vector3 worldRotation = (eyes.fixationPoint - worldPosition).normalized;
        gazeRay = new Ray(worldPosition, worldRotation * 100.0f);
        //SELECTION CHECKS
        SetPointer();

        CheckSelector();

        if (occluders && (timeStamp - prevLogTime) > 1000)
        {
            obj.log.time = timeStamp - startTime;
            obj.log.studyInProgress = studyInProgress;
            obj.log.mode = mode.ToString();
            obj.log.stage = stage.ToString();
            obj.log.currentPointer = currentPointer.ToString();
            obj.log.handray = handRay.ToString();
            obj.log.anchor = anchorMode.ToString();
            obj.log.targetNum = currentTrial.targetNum;
            obj.log.gazeValid = gazeValid;
            obj.log.targetActive = targetActive;
            obj.log.targetSelected = targetSelected;
            obj.log.localization = localizationInfo.LocalizationStatus.ToString();
            firebaseUser.Child("log").SetRawJsonValueAsync(JsonUtility.ToJson(obj.log));
            prevLogTime = timeStamp;

        }

        //STUDY FLOW
        if (studyInProgress)
        {
            //WRITE FRAME DATA
            currentFrame.timestamp = timeStamp;
            currentFrame.hPos = mainCamera.transform.position;
            currentFrame.hRot = mainCamera.transform.rotation;
            currentFrame.fixationPt = eyes.fixationPoint;
            currentFrame.fixationConfidence = trackingState.FixationConfidence;
            currentFrame.gazeTimestamp = trackingState.Timestamp;

            currentFrame.leftEyePos = eyes.leftEyePosition;
            currentFrame.leftEyeRot = eyes.leftEyeRotation;
            currentFrame.rightEyePos = eyes.rightEyePosition;
            currentFrame.rightEyeRot = eyes.rightEyeRotation;

            currentFrame.controllerPos = _controller.Position.ReadValue<Vector3>();
            currentFrame.controllerRot = _controller.Rotation.ReadValue<Quaternion>();

            firebaseSession.Child("frames").Child(frameNum.ToString()).SetRawJsonValueAsync(JsonUtility.ToJson(currentFrame));
            frameNum++;

            if (targetSelected && stage == Stage.select && moveOn == true)
            {
                firebaseUser.Child("message").SetValueAsync("moving on");
                targetSelected = false;
                moveOn = false;
                Debug.Log("NEXT TRIAL");
                AlwaysOn.text = "moving on";
                NextTrial();

            }
        }
    }

    void OnPermissionDenied(string permission)
    {
        MLPluginLog.Error($"{permission} denied, example won't function.");
    }

    void OnPermissionGranted(string permission)
    {
        InputSubsystem.Extensions.MLEyes.StartTracking();
        eyesActions = new MagicLeapInputs.EyesActions(mlInputs);
        permissionGranted = true;
    }

    private void OnEnable()
    {
        MLMarkerTracker.OnMLMarkerTrackerResultsFoundArray += OnMLMarkerTrackerResultsFoundArray;
    }

    private void OnDisable()
    {
        MLMarkerTracker.OnMLMarkerTrackerResultsFoundArray -= OnMLMarkerTrackerResultsFoundArray;
    }

    private void EnableMarkerTrackerExample()
    {
        AlwaysOn.text = "Enabling marker tracking..." + scanMarkers.ToString();
        // Unity has it's own value for Enum called Everything and sets it to -1
        //MarkerTypes = (int)MarkerTypes == -1 ? MarkerType.All : MarkerTypes;
        //var customProfile = TrackerProfile == Profile.Custom ? TrackerSettings.CustomProfile.Create(FPSHint, ResolutionHint, CameraHint, FullAnalysisIntervalHint, CornerRefineMethod, UseEdgeRefinement) : default;
        //markerSettings = TrackerSettings.Create(scanMarkers, MarkerTypes, 0.1f, ArucoDicitonary, ArucoMarkerSize, TrackerProfile, customProfile);
        //SetSettingsAsync(markerSettings).GetAwaiter().GetResult();

        //Should the Marker Tracker enable as soon as the settings are set?
        //bool enableMarkerScanning = true;
        float qrCodeMarkerSize = 0.1f;
        float arucoMarkerSize = 0.1f;
        MLMarkerTracker.MarkerType type = MLMarkerTracker.MarkerType.Aruco_April;
        MLMarkerTracker.ArucoDictionaryName arucoDict = MLMarkerTracker.ArucoDictionaryName.DICT_4X4_100;
        MLMarkerTracker.Profile trackerProfile = MLMarkerTracker.Profile.Default;
        MarkerSettings settings = MLMarkerTracker.TrackerSettings.Create(true, type, 0.1f, arucoDict, arucoMarkerSize, trackerProfile);
        _ = MLMarkerTracker.SetSettingsAsync(settings);
        AlwaysOn.text = "Setting scan settings...";

    }

    private void OnMLMarkerTrackerResultsFoundArray(MarkerData[] dataArray)
    {
        if (!removeMarkersUsingTimeStamps)
        {
            RemoveNotVisibleTrackers(dataArray);
        }
        foreach (MarkerData data in dataArray)
        {
            ProcessSingleMarker(data);
        }
    }

    private void ProcessSingleMarker(MarkerData data)
    {
        AlwaysOn.text = "Processing marker";
        switch (data.Type)
        {
            case MarkerType.Aruco_April:
                {
                    string message = "";
                    //for specific markers
                    //string id = data.ArucoData.Id.ToString();
                    coarsePos = data.Pose.position;// + new Vector3(0, 0, 0.3048f);
                    coarseRot = data.Pose.rotation*Quaternion.Euler(-90, 180, 0);
                    Position.transform.position = coarsePos;
                    Position.transform.rotation = coarseRot;
                    notScanned = false;
                    //TestSphere.transform.position = Position.transform.position;
                }
                break;
            case MarkerType.EAN_13:
            case MarkerType.UPC_A:
            case MarkerType.QR:
                    break;
                
        }
    }

    private void UpdateVisibleTrackers()
    {
        if (removeMarkersUsingTimeStamps)
        {
            UpdateVisibleTrackersByTimeStamp();
        }
    }

    private void UpdateVisibleTrackersByTimeStamp()
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            MagicLeap.Examples.MarkerVisual marker = markers[i].Value;
            if (!(marker.Timestamp - Time.time > markerTrackerTimeout))
                continue;

            Destroy(marker.gameObject);
            markers.RemoveAt(i);
        }
    }

    private void RemoveNotVisibleTrackers(MarkerData[] dataArray)
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            MagicLeap.Examples.MarkerVisual marker = markers[i].Value;

            if (!dataArray.Any(x =>
            {
                if (x.Type != marker.Type)
                    return false;

                string id = default;
                switch (marker.Type)
                {
                    case MarkerType.Aruco_April:
                        id = x.ArucoData.Id.ToString();
                        break;
                    case MarkerType.EAN_13:
                    case MarkerType.UPC_A:
                    case MarkerType.QR:
                        break;
                }
                return id == markers[i].Key;
            }))
            {
                Destroy(marker.gameObject);
                markers.RemoveAt(i);
            }
        }
    }

}

