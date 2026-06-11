using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using InputDevice = UnityEngine.XR.InputDevice;
using HandGestures = UnityEngine.XR.MagicLeap.InputSubsystem.Extensions.DeviceFeatureUsages.HandGesture;
using GestureClassification = UnityEngine.XR.MagicLeap.InputSubsystem.Extensions.MLGestureClassification;
using Demo;

public class DemoManager : MonoBehaviour
{

    public Camera mainCamera;
    public Transform fixationPoint;
    public GameObject PhysicalLayout;
    public GameObject VirtualLayout;
    public GameObject OcclusionLayout;
    public GameObject TargetSizes;
    public GameObject Timer;
    public GameObject Gesture;
    public GameObject Physicality;
    public GameObject AlwaysOn;
    public GameObject Table;
    public GameObject Success;
    public GameObject AnchorPt;
    [SerializeField] LineRenderer leftPointerRayViz;
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
    private Color numberColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

    [SerializeField, Tooltip("Default: True. Set to false to not have a pre render Handtracking update. Not recommended for handtracking with visuals as this can affect smoothness.")]
    private bool preRenderHandUpdate = true;

    // Used to get ml inputs.
    private MagicLeapInputs mlInputs;

    private MagicLeapInputs.EyesActions eyesActions;
    private InputDevice eyesDevice;

    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;
    private InputDevice leftGestureDevice;
    private InputDevice rightGestureDevice;
    GestureClassification.KeyPoseType leftHandKeyPose;
    GestureClassification.KeyPoseType rightHandKeyPose;
    private UnityEngine.InputSystem.XR.Eyes eyes;

    private MagicLeapInputs.ControllerActions _controller;

    private MLAnchors.Request query;
    private Timer localizationInfoUpdateTimer;
    //GO FIX THIS WHEN CREATING ANCHORS
    private Dictionary<string, string> anchorMap = new Dictionary<string, string>();
    private Dictionary<string, string> TargetShuffleMap = new Dictionary<string, string>();
    private Dictionary<string, List<Transform>> targetMap = new Dictionary<string, List<Transform>>();
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
    private Description userTrials;

    private int optionsFile;
    private string userId;
    private long timeStamp;
    private long prevLogTime;
    private long startTime;
    private long dwellStart;
    private string keyboardInput;
    private string activeTarget;
    private int sessionNum;
    private int frameNum;
    private int interruptNum;
    private int trialNum;
    private int logTrialNum;
    private int anchorNum;
    private Stage stage;
    private Demo.Pointer currentPointer;
    private Demo.Pointer activePointer;
    private bool userLoaded;
    private bool getTextInput;
    private bool studyInProgress;
    private bool targetInView;
    private bool targetSelected;
    private bool targetAcknowledged;
    private bool gazeValid;
    private bool dwelling;
    private bool moveOn;
    private bool countdownActive;
    private bool nextAnchor;
    private AnchorMode anchorMode;
    private DemoMode mode;
    private Handedness currentHand;
    private bool occluders = false;
    private bool showStats = false;
    private bool blockTrigger = false;
    private bool anchorPlane = false;
    private bool planeAnchored = false;
    private bool anchorFine = false;
    private float anchor_distance = Const.ANCHOR_DISTANCE;
    MLAnchors.LocalizationInfo localizationInfo;
    MLSpace.LocalizationResult spaceLocalization;
    MLSpace.SpaceInfo spaceInfo;
    private List<MLSpace.Space> Spaces;
    private string spaceFileName = "exported_space.bin";
    private string filePath;
    private string planeId = "";
    private float rotationConst = 0.1f;


    private JsonSerializerSettings settings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public void NewUser(bool first = false)
    {
        if (first)
        {
            Debug.Log("first!");
            PhysicalLayout.SetActive(false);
            foreach (Transform t in VirtualLayout.transform)
            {
                targetMap[t.gameObject.name[1..]] = new List<Transform>();
                targetMap[t.gameObject.name[1..]].Add(t);
                t.Find("arrow").gameObject.SetActive(false);
            }
            //VirtualLayout.SetActive(false);
            foreach (Transform t in OcclusionLayout.transform)
            {
                targetMap[t.gameObject.name[1..]].Add(t);
                foreach (Transform c in t)
                {
                    c.gameObject.SetActive(false);
                }
            }

            foreach (Transform t in TargetSizes.transform)
            {
                GameObject g = t.gameObject;
                targetMap[g.name[1..]] = new List<Transform>();
                targetMap[g.name[1..]].Add(g.transform);
            }

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
            userId = "ONR_" + DateTime.Now.ToString("MMdd_HHmmss_tt");
            obj.ResetObject(userId, 0);
        }

        sessionNum = -1;
        firebaseRef.Child(Const.FIREBASE_PATH).Child("_current").SetValueAsync(userId);
        firebaseRef.Child(Const.FIREBASE_PATH).Child(userId).SetRawJsonValueAsync(JsonUtility.ToJson(obj));
        firebaseUser = firebaseRef.Child(Const.FIREBASE_PATH).Child(userId);

        currentPointer = Demo.Pointer.controller;
        activePointer = currentPointer;
        currentHand = Handedness.right;
        AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "started new study\n\n Choose one of five modes: \nPhysical\nVirtual\nMixed\nIndividual Trials\nDifferent Target Sizes.";

    }

    private void NewMode(DemoMode mode)
    {

        Success.SetActive(false);
        showStats = false;
        ComputeStats(showStats);
        sessionNum += 1;
        obj.log.session = sessionNum;
        obj.sessions[sessionNum] = new SessionRecording(userId);
        obj.sessions[sessionNum].stats = new Statistics(targets);
        obj.sessions[sessionNum].mode = mode.ToString();
        //WILL BE REPLACED BY INPUT FILE
        List<string> target_types = new List<string>();
        switch (mode)
        {
            case DemoMode.phys:
                target_types = (from number in Enumerable.Range(0, Const.NUM_TARGETS) select "physical").ToList();
                AlwaysOn.SetActive(true);
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "";
                break;
            case DemoMode.virt:
                target_types = (from number in Enumerable.Range(0, Const.NUM_TARGETS) select "virtual").ToList();
                AlwaysOn.SetActive(true);
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "";
                break;
            case DemoMode.mixed:
                AlwaysOn.SetActive(true);
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "";
                foreach (int j in Enumerable.Range(0, Const.NUM_TARGETS / 2))
                {
                    target_types.Add("physical");
                    target_types.Add("virtual");
                }
                break;
            case DemoMode.demo1:
                AlwaysOn.SetActive(false);
                foreach (int j in Enumerable.Range(0, Const.NUM_TARGETS / 2))
                {
                    target_types.Add("physical");
                    target_types.Add("virtual");
                }
                break;
            case DemoMode.demo2:
                AlwaysOn.SetActive(false);
                target_types = (from number in Enumerable.Range(0, TargetSizes.transform.childCount) select "virtual").ToList();
                break;
        }
        Debug.Log("targets");
        Debug.Log(target_types.Count);
        target_types.Shuffle();

        userTrials = new Description();
        userTrials.trials = new List<TrialDesc>();

        if (mode == DemoMode.demo2)
        {
            VirtualLayout.SetActive(false);
            OcclusionLayout.SetActive(false);
            TargetSizes.SetActive(true);
            int i = 1;
            foreach (Transform t in TargetSizes.transform)
            {
                GameObject g = t.gameObject;
                g.transform.Find("arrow").gameObject.SetActive(false);
                userTrials.trials.Add(new TrialDesc(target_types[i], g.name));
            }
        }
        else
        {
            VirtualLayout.SetActive(true);
            OcclusionLayout.SetActive(true);
            TargetSizes.SetActive(false);
            TargetShuffleMap = new Dictionary<string, string>();
            var keys = new List<string>();
            int i = 1;
            foreach (string type in target_types)
            {

                userTrials.trials.Add(new TrialDesc(type, type.Substring(0, 1).ToUpper() + (i).ToString("D2")));
                if (mode != DemoMode.demo1 && mode != DemoMode.demo2 && type == "virtual")
                {
                    keys.Add((i).ToString("D2"));
                }
                else
                {
                    TargetShuffleMap.Add((i).ToString("D2"), (i).ToString("D2"));
                }
                Debug.Log(userTrials.trials[i - 1].targetNum);
                //Debug.Log(type.Substring(0, 1).ToUpper() + (i).ToString("D2"));
                i++;
            }

            if (keys.Count > 0)
            {
                var vals = keys.Select(s => s).ToList();
                keys.Shuffle();
                foreach (var key in keys)
                {
                    Debug.Log(key);
                }
                foreach (var val in vals)
                {
                    Debug.Log(val);
                }
                for (int j = 0; j < keys.Count; j++)
                {
                    TargetShuffleMap.Add(keys[j], vals[j]);
                }
            }


            foreach (TrialDesc t in userTrials.trials)
            {

                Debug.Log(t.physicality);
                Debug.Log(t.targetNum);
                switch (t.physicality)
                {
                    case "physical":
                        targetMap[t.targetNum[1..]][Const.PHYS_MASK].gameObject.SetActive(true);
                        targetMap[t.targetNum[1..]][Const.VIRT_MASK].gameObject.SetActive(false);
                        Debug.Log(targetMap[t.targetNum[1..]][Const.VIRT_MASK].gameObject.name);
                        break;
                    case "virtual":
                        targetMap[t.targetNum[1..]][Const.VIRT_MASK].gameObject.SetActive(true);
                        targetMap[t.targetNum[1..]][Const.PHYS_MASK].gameObject.SetActive(false);
                        Debug.Log(targetMap[t.targetNum[1..]][Const.PHYS_MASK].gameObject.name);
                        break;
                }
                Debug.Log("\n");
            }
            Debug.Log("numtrials");
            Debug.Log(userTrials.trials.Count);
            foreach (int k in Enumerable.Range(userTrials.trials.Count + 1, Const.TOTAL_TARGETS - userTrials.trials.Count))
            {
                targetMap[k.ToString("D2")][Const.PHYS_MASK].gameObject.SetActive(false);
                targetMap[k.ToString("D2")][Const.VIRT_MASK].gameObject.SetActive(false);
            }
        }
        
        startTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        obj.sessions[sessionNum].startTime = startTime;
        //obj.sessions[sessionNum].stats = new Statistics(targets);
        firebaseSession = firebaseUser.Child("sessions/" + sessionNum.ToString());
        firebaseSession.Child("mode").SetValueAsync(mode.ToString());
       if(mode == DemoMode.demo1 || mode == DemoMode.demo2)
        {
            Timer.SetActive(true);
        }
        else
        {
            Timer.SetActive(false);
        }
        currentFrame = new Frame();
        currentTrial = new Trial();
        //currentPointer = Demo.Pointer.controller;
        anchorMode = AnchorMode.none;
        //mode = DemoMode.virt;
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
        string message = "next trial";
        firebaseUser.Child("message").SetValueAsync(message);
        if (trialNum != -1)
        {
            firebaseSession.Child("trials").Child(logTrialNum.ToString()).SetRawJsonValueAsync(JsonUtility.ToJson(currentTrial));
            obj.sessions[sessionNum].trials.Add(currentTrial);
            firebaseUser.Child("message").SetValueAsync("saved obj " + currentTrial.status.ToString());
            if (currentTrial.status)
            {
                message += " " + currentTrial.targetNum + "_" + currentTrial.pointer + " ";
                firebaseUser.Child("message").SetValueAsync(message);
                obj.sessions[sessionNum].stats.trialCounts[currentTrial.targetNum + "_" + currentTrial.pointer] += 1;
                obj.sessions[sessionNum].stats.times[currentTrial.targetNum + "_" + currentTrial.pointer].Add(currentTrial.selectTime);
                obj.sessions[sessionNum].stats.error[currentTrial.targetNum + "_" + currentTrial.pointer].Add(currentTrial.selectAngleError);
                obj.sessions[sessionNum].stats.misclicks[currentTrial.targetNum + "_" + currentTrial.pointer] += 1;
                firebaseSession.Child("stats").SetRawJsonValueAsync(JsonConvert.SerializeObject(obj.sessions[sessionNum].stats, Formatting.Indented, settings));
            }
            message += "stats";
            firebaseUser.Child("message").SetValueAsync(message);
            //activeObject.GetComponent<Outline>().enabled = false;
            message += "saved previous";
            firebaseUser.Child("message").SetValueAsync(message);
        }
        trialNum += 1;
        logTrialNum += 1;

        if (trialNum == userTrials.trials.Count)
        {
            if (mode == DemoMode.demo1)
            {
                trialNum = 0;
                userTrials.trials.Shuffle();
            }
            else if (mode == DemoMode.demo2)
            {
                trialNum = 0;
            }    
            else
            {
                Success.SetActive(true);
                studyInProgress = false;
                firebaseUser.Child("message").SetValueAsync("select next mode");
                return;
            }

        }
        currentTrial.ResetTrial(userTrials.trials[trialNum]);
        firebaseUser.Child("message").SetValueAsync("reset trial " + currentTrial.targetNum);
        Debug.Log("reset trial");
        Debug.Log(currentTrial.targetNum);
        if(mode != DemoMode.demo1 && mode != DemoMode.demo2)
        {
            AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = currentTrial.targetNum[1..];
        }
        foreach (Transform t in targetMap[currentTrial.targetNum[1..]])
        {
            if (t.gameObject.name == currentTrial.targetNum)
            {
                activeObject = t.gameObject;
            }
        }
        message += "active object";
        firebaseUser.Child("message").SetValueAsync(message);
        switch (mode)
        {
            case DemoMode.demo1:
            case DemoMode.demo2:
                stage = Stage.start;
                activeObject.transform.Find("arrow").gameObject.SetActive(true);
                message += "arrow";
                firebaseUser.Child("message").SetValueAsync(message);
                break;
            default:
                stage = Stage.select;
                break;
        }

        if(mode != DemoMode.demo1 && mode != DemoMode.demo2)
        {
            activePointer = currentPointer;
        }
        else
        {
            activePointer = Demo.Pointer.controller;
        }
        obj.log.targetActive = targetActive;
        obj.log.trialNum = trialNum + 1;
        obj.log.targetNum = currentTrial.targetNum;
        obj.log.acknowledged = false;
        obj.log.selected = false;
        countdownActive = false;
        moveOn = false;
        targetSelected = false;
        if(mode == DemoMode.demo1 || mode == DemoMode.demo2)
        {
            Timer.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "SPOT YOUR TARGET, POINT THE CONTROLLER HERE AND PRESS BUMPER.";
        }
    }

    public async void CheckTimerAck()
    {
        if (Physics.Raycast(ray: PointerRay, hitInfo: out hitData, maxDistance: 100.0f, layerMask: timer))
        {
            activeObject.GetComponent<Outline>().enabled = false;
            countdownActive = true;
            for (int i = Const.COUNTDOWN; i > 0; i--)
            {
                Timer.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = i.ToString();
                obj.log.countdown = (long)i;
                await WaitOneSecondAsync();
            }
            Timer.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "GO!";
            targetAcknowledged = true;
            await WaitOneSecondAsync();
            await WaitOneSecondAsync();
            await WaitOneSecondAsync();
            Timer.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "SELECT YOUR TARGET, THEN PRESS TRIGGER.";
            countdownActive = false;
            obj.log.countdown = 999;
        }
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
            if(mode != DemoMode.demo1 && mode != DemoMode.demo2)
            {
                AlwaysOn.SetActive(true);
            }
            
        }
        else
        {
            AlwaysOn.SetActive(false);
            if(mode == DemoMode.demo1 || mode == DemoMode.demo2)
            {
                Gesture.SetActive(true);
                //BY GESTURE
                float misclicksG = 0.0f, misclicksC = 0.0f, misclicksH = 0.0f, timesG = 0.0f, timesC = 0.0f, timesH = 0.0f, errorsG = 0.0f, errorsC = 0.0f, errorsH = 0.0f, trialsG = 0.0f, trialsC = 0.0f, trialsH = 0.0f;
                foreach (string key in obj.sessions[sessionNum].stats.keys)
                {
                    if (key.Contains("gaze"))
                    {
                        misclicksG += (float)obj.sessions[sessionNum].stats.misclicks[key];
                        timesG += (float)obj.sessions[sessionNum].stats.times[key].Sum() / 1000.0f;
                        errorsG += (float)obj.sessions[sessionNum].stats.error[key].Sum();
                        trialsG += (float)obj.sessions[sessionNum].stats.trialCounts[key];
                    }
                    else if (key.Contains("controller"))
                    {
                        misclicksC += (float)obj.sessions[sessionNum].stats.misclicks[key];
                        timesC += (float)obj.sessions[sessionNum].stats.times[key].Sum() / 1000.0f;
                        errorsC += (float)obj.sessions[sessionNum].stats.error[key].Sum();
                        trialsC += (float)obj.sessions[sessionNum].stats.trialCounts[key];
                    }
                    else if (key.Contains("handray"))
                    {
                        misclicksH += (float)obj.sessions[sessionNum].stats.misclicks[key];
                        timesH += (float)obj.sessions[sessionNum].stats.times[key].Sum() / 1000.0f;
                        errorsH += (float)obj.sessions[sessionNum].stats.error[key].Sum();
                        trialsH += (float)obj.sessions[sessionNum].stats.trialCounts[key];
                    }
                }

                string statStringBuilder = "<color=#B7B7B8><b>Statistics</b></color>";
                statStringBuilder += "\n\n<color=#B7B7B8><b>By Gesture</b></color>: (bumper for more)";
                if (trialsG > 0)
                {
                    statStringBuilder += "\n\n<color=#B7B7B8><b>Gaze</b></color>: ";
                    statStringBuilder += trialsG + " trials.\n";
                    statStringBuilder += misclicksG.ToString() + " misclick(s) per trial";
                    statStringBuilder += "\nAverage selection time: " + (timesG / trialsG).ToString();
                    statStringBuilder += "\nAverage selection error (in degrees): " + (errorsG / trialsG).ToString();
                }
                if (trialsH > 0)
                {
                    statStringBuilder += "\n\n<color=#B7B7B8><b>Handray</b></color>: ";
                    statStringBuilder += trialsH + " trials.\n";
                    statStringBuilder += misclicksH.ToString() + " misclick(s) per trial";
                    statStringBuilder += "\nAverage selection time: " + (timesH / trialsH).ToString();
                    statStringBuilder += "\nAverage selection error (in degrees): " + (errorsH / trialsH).ToString();
                }
                if (trialsC > 0)
                {
                    statStringBuilder += "\n\n<color=#B7B7B8><b>Controller</b></color>: ";
                    statStringBuilder += trialsC + " trials.\n";
                    statStringBuilder += misclicksC.ToString() + " misclick(s) per trial";
                    statStringBuilder += "\nAverage selection time: " + (timesC / trialsC).ToString();
                    statStringBuilder += "\nAverage selection error (in degrees): " + (errorsC / trialsC).ToString();
                }

                Gesture.GetComponent<TMPro.TextMeshProUGUI>().text = statStringBuilder;

                if(mode == DemoMode.demo1)
                {
                    statStringBuilder = "<color=#B7B7B8><b>Statistics</b></color>";

                    //BY PHYSICALITY
                    float misclicksP = 0.0f, misclicksV = 0.0f, timesP = 0.0f, timesV = 0.0f, errorsP = 0.0f, errorsV = 0.0f, trialsP = 0.0f, trialsV = 0.0f;

                    foreach (string key in obj.sessions[sessionNum].stats.keys)
                    {
                        if (key[0] == 'V')
                        {
                            misclicksV += (float)obj.sessions[sessionNum].stats.misclicks[key];
                            timesV += (float)obj.sessions[sessionNum].stats.times[key].Sum() / 1000.0f;
                            errorsV += (float)obj.sessions[sessionNum].stats.error[key].Sum();
                            trialsV += (float)obj.sessions[sessionNum].stats.trialCounts[key];
                        }
                        else if (key[0] == 'P')
                        {
                            misclicksP += (float)obj.sessions[sessionNum].stats.misclicks[key];
                            timesP += (float)obj.sessions[sessionNum].stats.times[key].Sum() / 1000.0f;
                            errorsP += (float)obj.sessions[sessionNum].stats.error[key].Sum();
                            trialsP += (float)obj.sessions[sessionNum].stats.trialCounts[key];
                        }
                    }
                    statStringBuilder += "\n\n<color=#B7B7B8><b>By Physicality</b></color>: (bumper to go back)";
                    if (trialsP > 0)
                    {
                        statStringBuilder += "\n\n<color=#B7B7B8><b>Physical</b></color>:";
                        statStringBuilder += trialsP + " trials.\n";
                        statStringBuilder += misclicksP.ToString() + " misclick(s) per trial";
                        statStringBuilder += "\nAverage selection time: " + (timesP / trialsP).ToString();
                        statStringBuilder += "\nAverage selection error (in degrees): " + (errorsP / trialsP).ToString();
                    }
                    if (trialsV > 0)
                    {
                        statStringBuilder += "\n\n<color=#B7B7B8><b>Virtual</b></color>: ";
                        statStringBuilder += trialsV + " trials.\n";
                        statStringBuilder += misclicksV.ToString() + " misclick(s) per trial";
                        statStringBuilder += "\nAverage selection time: " + (timesV / trialsV).ToString();
                        statStringBuilder += "\nAverage selection error (in degrees): " + (errorsV / trialsV).ToString();
                    }


                    Physicality.GetComponent<TMPro.TextMeshProUGUI>().text = statStringBuilder;
                }
                
            }

            else
            {
               
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
                        else if (obj.sessions[i].mode == DemoMode.virt.ToString())
                        {
                            virtString = "Virtual: " + obj.sessions[i].trials[obj.sessions[i].trials.Count - 1].selectTime / 1000.0f + " s, " + obj.sessions[i].trials.Count.ToString() + " trials\n"; 
                        }
                        else if (obj.sessions[i].mode == DemoMode.phys.ToString())
                        {
                            physString = "Physical: " + obj.sessions[i].trials[obj.sessions[i].trials.Count - 1].selectTime / 1000.0f + " s, " + obj.sessions[i].trials.Count.ToString() + " trials\n";
                        }
                        else if (obj.sessions[i].mode == DemoMode.mixed.ToString())
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

    }

    private void MisClick()
    {
        if(mode != DemoMode.demo2)
        {
            obj.sessions[sessionNum].stats.misclicks[currentTrial.targetNum + "_" + currentPointer.ToString()] += 1;
        }
        
    }

    private void SetPointer()
    {
        switch (activePointer)
        {
            case Demo.Pointer.gaze:
                fixationPoint.gameObject.SetActive(true);
                fixationPoint.SetPositionAndRotation(eyes.fixationPoint, Quaternion.LookRotation(eyes.fixationPoint - Camera.main.transform.position));
                leftPointerRayViz.gameObject.SetActive(false);
                //leftPointerRayViz.SetPositions(new Vector3[2] { worldPosition, worldPosition + (worldRotation * 10000.0f) });
                PointerRay = gazeRay;
                break;
            case Demo.Pointer.controller:
                fixationPoint.gameObject.SetActive(false);
                rayOrigin = _controller.Position.ReadValue<Vector3>();
                rayDirection = _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward;
                leftPointerRayViz.gameObject.SetActive(true);
                leftPointerRayViz.SetPositions(new Vector3[2] { rayOrigin, rayOrigin + (rayDirection * 10000.0f) });
                PointerRay = new Ray(rayOrigin, rayDirection * 10000.0f);
                break;
            case Demo.Pointer.handray:
                fixationPoint.gameObject.SetActive(false);
                switch (currentHand)
                {
                    case Handedness.left:
                    
                        if (leftHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.handData, out UnityEngine.XR.Hand lhand))
                        {
                            rayOrigin = new Vector3(0.0f, 0.0f, 0.0f);
                            rayDirection = new Vector3(0.0f, 0.0f, 0.0f);
                            switch (handRay)
                            {
                                case HandRay.index_finger:
                                    //index finger
                                    lhand.TryGetFingerBones(UnityEngine.XR.HandFinger.Index, this.handBones);
                                    this.handBones[0].TryGetPosition(out rayDirection);
                                    this.handBones[3].TryGetPosition(out rayOrigin);
                                    rayDirection = (rayDirection - rayOrigin).normalized;
                                    break;
                                case HandRay.wrist_to_hand_center:
                                    //hand center to wrist center
                                    leftHandDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.WristCenter, out this.wristBone);
                                    wristBone.TryGetPosition(out rayOrigin);
                                    leftHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out rayDirection);
                                    rayDirection = (rayDirection - rayOrigin).normalized;
                                    break;
                            }

                        }
                        break;
                    case Handedness.right:
                        if (rightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.handData, out UnityEngine.XR.Hand rhand))
                        {
                            rayOrigin = new Vector3(0.0f, 0.0f, 0.0f);
                            rayDirection = new Vector3(0.0f, 0.0f, 0.0f);
                            switch (handRay)
                            {
                                case HandRay.index_finger:
                                    //index finger
                                    rhand.TryGetFingerBones(UnityEngine.XR.HandFinger.Index, this.handBones);
                                    this.handBones[0].TryGetPosition(out rayDirection);
                                    this.handBones[3].TryGetPosition(out rayOrigin);
                                    rayDirection = (rayDirection - rayOrigin).normalized;
                                    break;
                                case HandRay.wrist_to_hand_center:
                                    //hand center to wrist center
                                    rightHandDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.WristCenter, out this.wristBone);
                                    wristBone.TryGetPosition(out rayOrigin);
                                    rightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out rayDirection);
                                    rayDirection = (rayDirection - rayOrigin).normalized;
                                    break;

                            }

                        }
                        break;
                }
                leftPointerRayViz.gameObject.SetActive(true);
                leftPointerRayViz.SetPositions(new Vector3[2] { rayOrigin, rayOrigin + rayDirection * 10000.0f });
                PointerRay = new Ray(rayOrigin, rayDirection * 10000.0f);
                break;
        }

        if (Physics.Raycast(ray: PointerRay, hitInfo: out hitData, maxDistance: 100.0f, layerMask: physicalMask | virtualMask))
        {
            if (!targetActive)
            {
                activeTarget = hitData.collider.gameObject.name;
                if (activeTarget == currentTrial.targetNum)
                {
                    targetActive = true;
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
        if (_controller.Trigger.WasPressedThisFrame() && !targetSelected && stage == Stage.select)
        {
            Debug.Log("trigger pressed");
            if (targetActive)
            {
                firebaseUser.Child("message").SetValueAsync("selected");
                Debug.Log("selected");
                targetSelected = true;
                activeObject.GetComponent<Outline>().enabled = true;
                activeObject.GetComponent<Outline>().OutlineColor = Color.green;
                currentTrial.status = true;
                currentTrial.selectPoint = hitData.point;
                switch (mode)
                {
                    case DemoMode.demo1:
                    case DemoMode.demo2:
                        currentTrial.selectTime = timeStamp - currentTrial.ackTime;
                        break;
                    default:
                        currentTrial.selectTime = timeStamp - startTime;
                        break;
                }
                
                Debug.Log(hitData.point - mainCamera.transform.position);
                Debug.Log(activeObject.transform.position - mainCamera.transform.position);
                currentTrial.selectAngleError = Vector3.Angle((hitData.point - mainCamera.transform.position), (activeObject.transform.position - mainCamera.transform.position));
                currentTrial.objectDistance = Vector3.Distance(mainCamera.transform.position, activeObject.transform.position);
                currentTrial.targetCenter = activeObject.transform.position;
                currentTrial.pointer = currentPointer.ToString();
                if (mode == DemoMode.demo1 || mode == DemoMode.demo2)
                {
                    WaitNextTrial(activeObject, 3, true);
                }
                else
                {
                    moveOn = true;
                    WaitNextTrial(activeObject, 1);
                }


            }
            else
            {
                MisClick();
            }


        }

    }

    private void changeColor()
    {
        foreach(Transform t in VirtualLayout.transform)
        {
            t.Find("Number").Find("Text (TMP)").GetComponent<TMPro.TextMeshProUGUI>().color = numberColor;
        }
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
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "finished anchoring targets, press 'anch' to exit mode";
            }
            else
            {
                //firebaseUser.Child("log/targetNum").SetValueAsync(targets[anchorNum]);
                firebaseUser.Child("message").SetValueAsync("set target locations " + targets[anchorNum]);
            }
        }
        nextAnchor = false;
    }

    private void UpdateTargetLocations()
    {
        var mlResultStart = query.Start(new MLAnchors.Request.Params(mainCamera.transform.position, 0, 0, true));
        var mlResultGet = query.TryGetResult(out MLAnchors.Request.Result result);

        if (mlResultStart.IsOk && mlResultGet.IsOk)
        {
            foreach (var anchor in result.anchors)
            {
                string id = anchor.Id;
                if (anchorMap.ContainsKey(id))
                {
                    if (TargetShuffleMap.ContainsKey(anchorMap[id]))
                    {
                        foreach (Transform t in targetMap[TargetShuffleMap[anchorMap[id]]])
                        {
                            t.position = anchor.Pose.position;
                            t.rotation = anchor.Pose.rotation;
                        }
                    }
                    else
                    {
                        foreach (Transform t in targetMap[anchorMap[id]])
                        {
                            t.position = anchor.Pose.position;
                            t.rotation = anchor.Pose.rotation;
                        }
                    }

                }
                else if (id == planeId)
                {
                    Table.transform.position = anchor.Pose.position;
                    Table.transform.rotation = anchor.Pose.rotation;
                }
            }
        }
        if (anchorMode != AnchorMode.none && nextAnchor == false && blockTrigger == false && anchorNum < targets.Length)
        {
            
            if(anchorFine)
            {
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\nfine-tuning " + targets[anchorNum] + ", press 'w' to confirm.";
                foreach (Transform t in targetMap[targets[anchorNum]])
                {
                    t.position = anchorPos;
                    t.rotation = anchorRot;
                }
            }
            else
            {
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\ncoarse anchoring " + targets[anchorNum] + ", press trigger to fine-tune.";
                foreach (Transform t in targetMap[targets[anchorNum]])
                {
                    t.position = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
                    t.rotation = _controller.Rotation.ReadValue<Quaternion>();
                }
            }
            
        }
        if(anchorPlane && !planeAnchored)
        {
            if (anchorFine)
            {
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\nfine-tuning table, press 'w' to confirm.";
                Table.transform.position = anchorPos;
                Table.transform.rotation = anchorRot;

            }
            else
            {
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\ncoarse anchoring table, press trigger to fine-tune.";
                Table.transform.position = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
                Table.transform.rotation = _controller.Rotation.ReadValue<Quaternion>();

            }
        }

    }

    private void HandleOnBumper(InputAction.CallbackContext obj)
    {
        string message = "bumper pressed. ";
        firebaseUser.Child("message").SetValueAsync(message);
        switch (anchorMode)
        {
            case AnchorMode.none:
                if(anchorPlane)
                {
                    if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
                    {
                        query.Start(new MLAnchors.Request.Params(_controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance, 0, 0, true));
                        query.TryGetResult(out MLAnchors.Request.Result result);
                        if (result.anchors.Length > 0)
                        {
                            foreach(MLAnchors.Anchor anchor in result.anchors)
                            {
                                if(anchor.Id == planeId)
                                {
                                    anchor.Delete();
                                    planeAnchored = false;
                                    planeId = "";
                                }
                            }
                        }
                    }
                }
                break;
            case AnchorMode.single:
                if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
                {
                    query.Start(new MLAnchors.Request.Params(_controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance, 0, 0, true));
                    query.TryGetResult(out MLAnchors.Request.Result result);
                    if (result.anchors.Length > 0)
                    {
                        var anchor = result.anchors[0];
                        string name = anchorMap[anchor.Id];
                        if (anchorMap.ContainsKey(anchor.Id))
                        {
                            targetAnchored[anchorMap[anchor.Id]] = false;
                            anchorMap.Remove(anchor.Id);
                        }
                        anchor.Delete();
                    }
                    anchorNum = 0;
                    nextAnchor = true;
                    anchorFine = false;
                }
                break;
        }
        if (showStats && mode == DemoMode.demo1)
        {
            Gesture.SetActive(!Gesture.activeSelf);
            Physicality.SetActive(!Physicality.activeSelf);
        }
    }

    private void HandleOnTrigger(InputAction.CallbackContext obj)
    {
        switch(anchorMode)
        {
            case AnchorMode.none:
                if (anchorPlane)
                {
                    anchorPos = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
                    anchorRot = _controller.Rotation.ReadValue<Quaternion>();
                    anchorFine = true;

                }
                break;
            case AnchorMode.single:
                anchorPos = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
                anchorRot = _controller.Rotation.ReadValue<Quaternion>();
                anchorFine = true;
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
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        //MLSpace.OnLocalizationEvent += OnLocalizationChanged;
    }
    // Start is called before the first frame update
    void Start()
    {

        

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

        // HAND_TRACKING is a normal permission, so we don't request it at runtime. It is auto-granted if included in the app manifest.
        // If it's missing from the manifest, the permission is not available.
        if (!MLPermissions.CheckPermission(MLPermission.HandTracking).IsOk)
        {
            Debug.LogError($"You must include the {MLPermission.HandTracking} permission in the AndroidManifest.xml to run this example.");
            enabled = false;
            return;
        }

        InputSubsystem.Extensions.MLHandTracking.StartTracking();
        InputSubsystem.Extensions.MLHandTracking.SetPreRenderHandUpdate(preRenderHandUpdate);

        // Check Hand Tracking Permissions before starting Gesture Classification
        if (MLPermissions.CheckPermission(MLPermission.HandTracking).IsOk)
        {
            GestureClassification.StartTracking();
            print("STARTED GESTURE CLASSIFICATION");
        }

        MLPermissions.RequestPermission(MLPermission.EyeTracking, permissionCallbacks);

        //SETUP TARGETS AND ANCHORING
        localizationInfoUpdateTimer = new Timer(3);
        query = new MLAnchors.Request();

        query.Start(new MLAnchors.Request.Params(_controller.Position.ReadValue<Vector3>(), 0, 0, true));
        query.TryGetResult(out MLAnchors.Request.Result result);
        firebaseUser.Child("message").SetValueAsync("deleting old anchors");
        if (result.anchors.Length > 0)
        {
            foreach(var anchor in result.anchors)
            {
                anchor.Delete();
            }    
        }
        firebaseUser.Child("message").SetValueAsync("deleted old anchors");

        LocalizeToSpace();
        

        anchorMap = new Dictionary<string, string>();
        targetMap = new Dictionary<string, List<Transform>>();
        anchorNum = 0;
        nextAnchor = false;
        targets = new string[Const.NUM_TARGETS + TargetSizes.transform.childCount];
/*        GameObject[] targetObj = GameObject.FindGameObjectsWithTag("Target");
        Array.Sort(targetObj, (x, y) => String.Compare(x.name, y.name));*/
        int i = 0;

        //foreach (GameObject g in targetObj)
        foreach(Transform T in VirtualLayout.transform)
        {
            GameObject g = T.gameObject;
            Debug.Log(g.name);

            if (targetMap.ContainsKey(g.name[1..]))
            {
                targetMap[g.name[1..]].Add(g.transform);
            }
            else
            {
                targetMap[g.name[1..]] = new List<Transform>();
                targetMap[g.name[1..]].Add(g.transform);
                targetAnchored.Add(g.name[1..], false);
                targets[i] = g.name[1..];
                i++;

            }
            if (i >= Const.NUM_TARGETS)
            {
                break;
            }
        }
        //firebaseUser.Child("message").SetValueAsync("virtual done");
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
        i = Const.NUM_TARGETS;
        foreach(Transform t in TargetSizes.transform)
        {
            GameObject g = t.gameObject;
            targetMap[g.name[1..]] = new List<Transform>();
            targetMap[g.name[1..]].Add(g.transform);
            targetAnchored.Add(g.name[1..], false);
            targets[i] = g.name[1..];
            i++;
        }
        Debug.Log(targets.Length);
        //firebaseUser.Child("message").SetValueAsync("occlusion done " + targets.Length.ToString());

        targetActive = false;
        currentPointer = Demo.Pointer.controller;
        activePointer = Demo.Pointer.controller;
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
            if (key == 'z')
            {
                mode = DemoMode.phys;
                NewMode(DemoMode.phys);
            }
            else if (key == 'x')
            {
                mode = DemoMode.virt;
                NewMode(DemoMode.virt);
            }
            else if (key == 'c')
            {
                mode = DemoMode.mixed;
                NewMode(DemoMode.mixed);
            }
            else if (key == 'v')
            {
                mode = DemoMode.demo1;
                NewMode(DemoMode.demo1);
            }
            else if(key == 'b')
            {
                mode = DemoMode.demo2;
                NewMode(DemoMode.demo2);
            }
            else if (key == 'l')
            {
                currentPointer = Demo.Pointer.controller;
                if ((mode != DemoMode.demo1 && mode != DemoMode.demo2) || stage == Stage.select)
                {
                    activePointer = currentPointer;
                }
            }
            else if (key == 'j')
            {
                currentPointer = Demo.Pointer.gaze;
                if ((mode != DemoMode.demo1 && mode != DemoMode.demo2) || stage == Stage.select)
                {
                    activePointer = currentPointer;
                }
            }
            else if (key == 'k')
            {
                currentPointer = Demo.Pointer.handray;
                if ((mode != DemoMode.demo1 && mode != DemoMode.demo2) || stage == Stage.select)
                {
                    activePointer = currentPointer;
                }
            }
            else if (key == 'f' && studyInProgress)
            {
                //FIX IN BUILD
                currentTrial.status = false;
                //currentTrial.pointer = currentPointer.ToString();
                activeObject.transform.Find("arrow").gameObject.SetActive(false);
                NextTrial();
            }
            else if (key == 's')
            {
                anchorPlane = false;
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
                        foreach(string s in targets)
                        { 
                            foreach(Transform t in targetMap[s])
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
                firebaseUser.Child("log/anchor").SetValueAsync(anchorMode.ToString());
            }
            else if (key == 'w')
            {
                switch (anchorMode)
                {
                    case AnchorMode.none:
                        if (anchorPlane)
                        {
                                if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
                                {
                                    MLAnchors.Anchor.Create(new Pose(anchorPos, anchorRot), 0, out MLAnchors.Anchor anchor);
                                    anchor.Publish();
                                    //TODO: MAP THIS TO PHYS, VIRT AND OCC TRANSFORMS FOR A TARGET NAME
                                    AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\n\n\nanchored plane.";
                                    planeId = anchor.Id;
                                    planeAnchored = true;
                                    anchorFine = false;
                                }
                            
                        }
                        break;
                    case AnchorMode.single:
                        if (blockTrigger == true)
                        {
                            break;
                        }
                        
                            if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
                            {
                                MLAnchors.Anchor.Create(new Pose(anchorPos, anchorRot), 0, out MLAnchors.Anchor anchor);
                                anchor.Publish();
                                //TODO: MAP THIS TO PHYS, VIRT AND OCC TRANSFORMS FOR A TARGET NAME
                                anchorMap.Add(anchor.Id, targets[anchorNum]);
                                //THE NAME SHOULD EXCLUDE P/V, ONLY HAVE SCENE NUM AND TARGETNUM
                                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "\n\n\n\nanchored " + targets[anchorNum] + ".";
                                targetAnchored[targets[anchorNum]] = true;
                                nextAnchor = true;
                                blockTrigger = true;
                                anchorFine = false;
                                WaitNextAnchor(1);
                            }
                        
                        break;
                }
            }
            else if (key == 'm')
            {
                switch (currentHand)
                {
                    case Handedness.left:
                        currentHand = Handedness.right;
                        break;
                    case Handedness.right:
                        currentHand = Handedness.left;
                        break;
                }
                firebaseUser.Child("log/handedness").SetValueAsync(currentHand.ToString());
            }
            else if(key == 'u')
            {
                anchor_distance += 0.1f;
            }
            else if(key == 'i')
            {
                anchor_distance -= 0.1f;
            }
            else if (key == 't')
            {
                firebaseUser.Child("message").SetValueAsync("showing stats");
                showStats = !showStats;
                ComputeStats(showStats);
            }
            else if (key == 'a')
            {
                anchorMode = AnchorMode.none;
                anchorPlane = !anchorPlane;
                if(anchorPlane)
                {
                    Success.SetActive(false);
                    AlwaysOn.SetActive(true);
                    showStats = false;
                    ComputeStats(showStats);
                    AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "anchoring table, press 'table' to exit mode";
                    Table.transform.Find("Table").gameObject.SetActive(true);
                    anchorFine = false;
                }
                else
                {
                    AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "exited 'table' mode";
                    Table.transform.Find("Table").gameObject.SetActive(false);
                }
            }
            else if(key == 'g')
            {

                if(numberColor.r < 1.0f)
                {
                    numberColor += new Color(0.1f, 0.1f, 0.1f, 0.0f);
                    changeColor();
                }
                    
            }
            else if(key == 'h')
            {
                if(numberColor.r > 0.0f)
                {
                    numberColor -= new Color(0.1f, 0.1f, 0.1f, 0.0f);
                    changeColor();
                }
                
            }
            else if(key == 'q')
            {
                rotationConst -= 0.1f;
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "rotation resolution: " + rotationConst.ToString("F1");
            }
            else if(key == 'e')
            {
                rotationConst += 0.1f;
                AlwaysOn.GetComponent<TMPro.TextMeshProUGUI>().text = "rotation resolution: " + rotationConst.ToString("F1");
            }
            else if (key == '1')
            {
                
                anchorPos -= new Vector3(0.002f, 0.0f, 0.0f);
            }
            else if (key == '2')
            {
                anchorPos += new Vector3(0.002f, 0.0f, 0.0f);
            }
            else if (key == '3')
            {
                anchorPos -= new Vector3(0.0f, 0.002f, 0.0f);
            }
            else if (key == '4')
            {
                anchorPos += new Vector3(0.0f, 0.002f, 0.0f);
            }
            else if (key == '5')
            {
                anchorPos -= new Vector3(0.0f, 0.0f, 0.002f);
            }
            else if (key == '6')
            {
                anchorPos += new Vector3(0.0f, 0.0f, 0.002f);
            }
            else if(key == '7')
            {
                anchorRot *= Quaternion.AngleAxis(-rotationConst, Vector3.right);
            }
            else if (key == '8')
            {
                anchorRot *= Quaternion.AngleAxis(rotationConst, Vector3.right);
            }
            else if (key == '9')
            {
                anchorRot *= Quaternion.AngleAxis(-rotationConst, Vector3.up);
            }
            else if (key == '0')
            {
                anchorRot *= Quaternion.AngleAxis(rotationConst, Vector3.up);
            }
            else if (key == 'o')
            {
                anchorRot *= Quaternion.AngleAxis(-rotationConst, Vector3.forward);
            }
            else if (key == 'p')
            {
                anchorRot *= Quaternion.AngleAxis(rotationConst, Vector3.forward);
            }

        }

        //ANCHOR TARGETS
        //----------------------------------------------------------------------------------------
        if (localizationInfoUpdateTimer.LimitPassed)
        {
            localizationInfoUpdateTimer.Reset();

            // SPATIAL_ANCHOR is a normal permission; we don't request it at runtime - must be included in AndroidManifest.xml
            if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
            {
                MLAnchors.GetLocalizationInfo(out localizationInfo);
                firebaseUser.Child("log/localization").SetValueAsync(localizationInfo.LocalizationStatus.ToString());
                MLSpace.GetLocalizationResult(out spaceLocalization);
                //Timer.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = spaceLocalization.LocalizationStatus.ToString();
            }
            else
            {
                //Timer.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "anchors failed";
            }

        }

        if (anchorMode != AnchorMode.none)
        {
            AnchorTargets();
            AnchorPt.transform.position = _controller.Position.ReadValue<Vector3>() + _controller.Rotation.ReadValue<Quaternion>() * Vector3.forward * anchor_distance;
        }
        UpdateTargetLocations();

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

        if (!leftGestureDevice.isValid || !rightGestureDevice.isValid)
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
        }

        if (!leftHandDevice.isValid || !rightHandDevice.isValid)
        {
            leftHandDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Left);
            rightHandDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right);
            return;
        }

        leftGestureDevice.TryGetFeatureValue(HandGestures.GesturesEnabled, out bool leftEnableCheck);
        //Debug.Log("Gestures Enabled: " + leftEnableCheck.ToString());

        //HAND TRACKING
        // Query the Input Devices for the detected postures

        leftHandDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.Confidence, out float leftConfidence);
        leftHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool leftIsTracked);

        rightHandDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.Confidence, out float rightConfidence);
        rightHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool rightIsTracked);

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

            currentFrame.leftPoseConfidence = leftConfidence;
            currentFrame.leftTracked = leftIsTracked;

            currentFrame.rightPoseConfidence = rightConfidence;
            currentFrame.rightTracked = rightIsTracked;

            currentFrame.controllerPos = _controller.Position.ReadValue<Vector3>();
            currentFrame.controllerRot = _controller.Rotation.ReadValue<Quaternion>();

            firebaseSession.Child("frames").Child(frameNum.ToString()).SetRawJsonValueAsync(JsonUtility.ToJson(currentFrame));
            frameNum++;

            if (mode == DemoMode.demo1 || mode == DemoMode.demo2)
            {
                if (_controller.Bumper.WasPressedThisFrame() && !showStats && stage == Stage.start)
                {
                    Debug.Log("bumper pressed");
                    stage = Stage.ack;
                    activeObject.transform.Find("arrow").gameObject.SetActive(false);
                }
                if (stage == Stage.ack && !countdownActive)
                {
                    CheckTimerAck();
                }
                if (targetAcknowledged == true && stage == Stage.ack)
                {
                    currentTrial.ackTime = timeStamp;
                    stage = Stage.select;
                    targetAcknowledged = false;
                    activePointer = currentPointer;
                    Debug.Log("currentPointer");
                    Debug.Log(activePointer.ToString());
                    firebaseUser.Child("message").SetValueAsync("acknowledged");
                }
            }

            if (targetSelected && stage == Stage.select && moveOn == true)
            {
                firebaseUser.Child("message").SetValueAsync("moving on");
                targetSelected = false;
                moveOn = false;
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
}
