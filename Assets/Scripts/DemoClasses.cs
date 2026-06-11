using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Demo
{
    public enum Stage { start, appear, ack, select };
    public enum Pointer { gaze, handray, controller };
    public enum HandRay { index_finger, wrist_to_hand_center }
    public enum AnchorMode { none, single };
    public enum Handedness { left, right };
    public enum DemoMode { phys, virt, mixed, demo1, demo2 };
    public static class Const
    {
        public const int NUM_MODES = 1;
        public const int NUM_TUT = 2;
        public const int NUM_GESTURES = 3;
        public const int NUM_IVS = 3;
        public const int NUM_TARGETS = 8;
        public const int TOTAL_TARGETS = 32;
        public const int NUM_SCENES = 1;
        public const int NUM_REPEATS = 1;
        public const int NUM_SESSIONS = 50;
        public const int NUM_TRIALS = NUM_GESTURES * NUM_TARGETS * NUM_REPEATS;
        public const float FIXATION_THRESHOLD = 0.75f;
        public const float DWELL_THRESHOLD = 2.0f;
        public const float ANCHOR_DISTANCE = 1.5f;
        public const int COUNTDOWN = 5;
        public const string FIREBASE_PATH = "test";
        public const int PHYS_MASK = 1;
        public const int VIRT_MASK = 0;
        public static string[] LAYOUT_CODES = new string[] { "P", "V" };

        private static System.Random rng = new System.Random();

        public static void Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    [System.Serializable]
    struct Frame
    {
        public long timestamp;
        public Vector3 hPos;
        public Quaternion hRot;

        public Vector3 fixationPt;
        public Vector3 leftEyePos;
        public Vector3 rightEyePos;
        public Quaternion leftEyeRot;
        public Quaternion rightEyeRot;
        public float fixationConfidence;
        public long gazeTimestamp;

        public float leftPoseConfidence;
        public bool leftTracked;
        public float rightPoseConfidence;
        public bool rightTracked;
        public Vector3 controllerPos;
        public Quaternion controllerRot;

    }

    [System.Serializable]
    struct Statistics
    {
        public string[] keys;
        public Dictionary<string, int> trialCounts;
        public Dictionary<string, int> misclicks;
        public Dictionary<string, List<long>> times;
        public Dictionary<string, List<float>> error;

        public Statistics(string[] targets)
        {
            trialCounts = new Dictionary<string, int>();
            misclicks = new Dictionary<string, int>();
            times = new Dictionary<string, List<long>>();
            error = new Dictionary<string, List<float>>();
            keys = new string[targets.Length * Const.LAYOUT_CODES.Length * Const.NUM_GESTURES];
            int i = 0;
            foreach (string t in targets)
            {
                foreach (Demo.Pointer p in Enum.GetValues(typeof(Demo.Pointer)))
                {
                    foreach (string ph in Const.LAYOUT_CODES)
                    {
                        trialCounts[ph + t + "_" + p.ToString()] = 0;
                        misclicks[ph + t + "_" + p.ToString()] = 0;
                        times[ph + t + "_" + p.ToString()] = new List<long>();
                        error[ph + t + "_" + p.ToString()] = new List<float>();
                        keys[i] = ph + t + "_" + p.ToString();
                        i++;
                    }
                    
                }
            }
        }
    }

    [System.Serializable]
    struct TrialDesc
    {
        public string physicality;
        public string targetNum;
        public TrialDesc(TrialDesc t)
        {
            physicality = t.physicality;
            targetNum = t.targetNum;
        }
        public TrialDesc(string p, string t)
        {
            physicality = p;
            targetNum = t;
        }
    }


    [System.Serializable]
    struct Description
    {
        public int optionsFile;
        public List<TrialDesc> trials;
    }

    [System.Serializable]
    struct Trial
    {
        public string physicality;
        public string targetNum;
        public string pointer;
        //public string selector;
        public long appearTime;
        public long ackTime;
        public long selectTime;
        public float selectAngleError;
        public float objectDistance;
        public Vector3 targetCenter;
        public Vector3 selectPoint;
        public float distanceFromCenter;            //CHECK WHAT UNIT THIS NEEDS TO BE, ANGULAR DISTANCE
        public bool status;
        public int misclicks;

        public Trial(TrialDesc t)
        {
            this.physicality = t.physicality;
            this.targetNum = t.targetNum;
            this.pointer = Demo.Pointer.controller.ToString();
            //this.selector = Selector.button.ToString();
            this.appearTime = 0;
            this.ackTime = 0;
            this.selectTime = 0;
            this.selectAngleError = 0.0f;
            this.objectDistance = 0.0f;
            this.selectPoint = new Vector3(0.0f, 0.0f, 0.0f);
            this.targetCenter = new Vector3(0.0f, 0.0f, 0.0f);
            this.distanceFromCenter = 0.0f;
            this.status = false;
            this.misclicks = 0;
        }

        public void ResetTrial(TrialDesc t)
        {
            this.targetNum = t.targetNum;
            this.physicality = t.physicality;
            this.appearTime = 0;
            this.ackTime = 0;
            this.selectTime = 0;
            this.selectPoint = new Vector3(0.0f, 0.0f, 0.0f);
            this.targetCenter = new Vector3(0.0f, 0.0f, 0.0f);
            this.distanceFromCenter = 0.0f;
            this.status = false;

            return;
        }
    }

    [System.Serializable]
    class SessionRecording
    {
        public string userID;
        public long startTime;
        public string mode;
        public Statistics stats;
        public List<Frame> frames;
        public List<Trial> trials;

        public SessionRecording(string userId)
        {
            this.userID = userId;
            frames = new List<Frame>();
            trials = new List<Trial>();
        }
    }

    [System.Serializable]
    class StudyLog
    {
        public bool studyInProgress;
        public int session;
        public long time;
        public long countdown;
        public int trialNum;
        public string mode;
        public string targetNum;
        public string gesture;
        //public string leftPointer;
        //public string rightPointer;
        public string currentPointer;
        public string selector;
        public string handray;
        public string stage;
        public string anchor;
        public bool targetActive;
        public bool targetSelected;
        public bool acknowledged;
        public bool selected;
        public bool gazeValid;
        public string localization;
        public int misclicks;

        public StudyLog()
        {
            studyInProgress = false;
            session = -1;
            time = 0;
            countdown = 999;
            trialNum = -1;
            mode = "init";
            targetNum = "none";
            gesture = "none";
            //leftPointer = StudyI.Pointer.gaze.ToString();
            //rightPointer = StudyI.Pointer.gaze.ToString();
            currentPointer = Demo.Pointer.controller.ToString();
            stage = Stage.start.ToString();
            handray = HandRay.index_finger.ToString();
            anchor = AnchorMode.none.ToString();
            targetActive = false;
            targetSelected = false;
            acknowledged = false;
            selected = false;
            gazeValid = false;
            localization = "false";
        }
    }

    class StudyObject
    {
        public string _valid = "null";
        public string userID;
        public Handedness handedness;
        public StudyLog log;
        public SessionRecording[] sessions;

        public StudyObject(string userId)
        {
            this.userID = userId;
            this.handedness = Handedness.right;
            log = new StudyLog();
            sessions = new SessionRecording[Const.NUM_SESSIONS];
        }
        public void ResetObject(string userId, int a)
        {
            this.userID = userId;
            sessions = new SessionRecording[Const.NUM_SESSIONS];
        }
    }

}
