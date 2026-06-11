using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

namespace StudyI
{
    public enum AnchorMode { none, coarse, fine };
    public enum AnchorControl { pos, rot};
    public enum StudyMode { phys, virt, mixed};
    public enum Distances { near, med, far};
    public enum HandRay { index_finger, wrist_to_hand_center};
    public enum Pointer { gaze, controller, handray}
    public enum Handedness { left, right };
    public enum Stage { start, ack, select};

    

    public static class Const
    {
        public const int NUM_TUT = 1;
        public const int NUM_TARGETS = 32;
        public const int TOTAL_TARGETS = 32;
        public const int NUM_DIST = 3;
        public const int NUM_MODE = 3;
        public const int NUM_CONDITIONS = NUM_MODE * NUM_DIST;
        public const int NUM_SESSIONS = 50;
        public const float FIXATION_THRESHOLD = 0.75f;
        //public const float DWELL_THRESHOLD = 2.0f;
        public const float ANCHOR_DISTANCE = 1.5f;
        public const float NEAR_DIST = 0.5f;
        public const float MED_DIST = 1.5f;
        public const float FAR_DIST = 3.0f;
        public const int COUNTDOWN = 5;
        public const string FIREBASE_PATH = "test";
        public const string OPTIONS_PATH = "user_study_options.json";
        public const string RESOURCES_PATH = "Resources";
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
            keys = new string[targets.Length * Const.LAYOUT_CODES.Length];
            int i = 0;
            foreach (string t in targets)
            {

                        trialCounts[t] = 0;
                        misclicks[t] = 0;
                        times[t] = new List<long>();
                        error[t] = new List<float>();
                        keys[i] = t;
                        i++;
                    

                
            }
        }
    }

    [System.Serializable]
    struct TrialDesc
    {
        public string physicality;
        public string targetNum;
        public string previous;
        public string next;
        public int position;
        public TrialDesc(TrialDesc t)
        {
            physicality = t.physicality;
            targetNum = t.targetNum;
            previous = t.previous;
            next = t.next;
            position = t.position;
        }
        public TrialDesc(string p, string t, string prev, string next, int pos)
        {
            this.physicality = p;
            this.targetNum = t;
            this.previous = prev;
            this.next = next;
            this.position = pos;
        }
    }

    [System.Serializable]
    class StudyDescription
    {
        public string userID;
        public List<SessionDescription> sessions;
        public StudyDescription(UserOption options)
        {
            System.Random random = new System.Random();
            sessions = new List<SessionDescription>();
            foreach(SessionOptions option in options.sessions)
            {
                Debug.Log(option.sessionID.ToString());
                sessions.Add(new SessionDescription(option, random));
            }
        }
    }

    [System.Serializable]
    class SessionDescription
    {
        public int sessionID;
        public StudyMode mode;
        public Distances dist;
        public List<TrialDesc> trials;

        public SessionDescription(SessionOptions options, System.Random random)
        {
            sessionID = options.sessionID;
            mode = (StudyMode)options.mode;
            dist = (Distances)options.distance;
            trials = new List<TrialDesc>();
            int[] positions = new int[Const.NUM_TARGETS];
            if (mode == StudyMode.virt)
            {
                positions = options.positions.OrderBy(x => random.Next()).ToArray();
            }
            else if (mode == StudyMode.mixed)
            {
                positions = new int[options.positions.Count];
                var temp = new List<int>();
                for(int i = 0; i < options.positions.Count; i++)
                {
                    if(options.physical[i] == "V")
                    {
                        temp.Add(options.positions[i]);
                    }
                }
                temp.Shuffle();
                int tempCount = 0;
                for (int i = 0; i < options.positions.Count; i++)
                {
                    if (options.physical[i] == "P")
                        positions[i] = options.positions[i];
                    else
                        positions[i] = temp[tempCount++];
                }
                
            }
            else
            {
                positions = options.positions.ToArray();
            }

            for (int i = 0; i < positions.Length; i++)
            {
                trials.Add(new TrialDesc(options.physical[i], (i + 1).ToString("D2"), i == 0?"N":options.physical[i - 1], i == (options.positions.Count-1)?"N":options.physical[i + 1], positions[i]));
            }
        }
    }

    [System.Serializable]
    struct Trial
    {
        public string physicality;
        public string targetNum;
        public string pointer;
        public string previous;
        public string next;
        public int position;
        public long appearTime;
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
            this.previous = t.previous;
            this.next = t.next;
            this.position = t.position;
            this.appearTime = 0;
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
        public string distance;
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
        public string handray;
        public string stage;
        public string currentPointer;
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
        public StudyLog log;
        public SessionRecording[] sessions;

        public StudyObject(string userId)
        {
            this.userID = userId;
            log = new StudyLog();
            sessions = new SessionRecording[Const.NUM_SESSIONS];
        }
        public void ResetObject(string userId, int a)
        {
            this.userID = userId;
            sessions = new SessionRecording[Const.NUM_SESSIONS];
        }
    }
    [System.Serializable]
    public class SessionOptions
    {
        public int sessionID;
        public int distance;
        public int mode;
        public List<string> physical;
        public List<int> positions;

/*        public SessionOptions()
        {
            sessionID = 0;
            distance = 0;
            mode = 0;
            physical = new List<string>();
            physical.Add("P");
            physical.Add("P");
            positions = new List<int>();
            positions.Add(0);
            positions.Add(1);

        }*/
    }


    [System.Serializable]
    public class UserOption
    {
        public string userID;
        public List<SessionOptions> sessions;

/*        public UserOption()
        {
            userID = "12";
            sessions = new List<SessionOptions>();
            sessions.Add(new SessionOptions());
        }*/
    }

    [System.Serializable]
    public class StudyOptions
    {
        public List<UserOption> options;

/*        public StudyOptions()
        {
            options = new List<UserOption>();
            options.Add(new UserOption());
        }*/
    }


}


