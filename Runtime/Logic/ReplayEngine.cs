using System;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.Events;

namespace ubco.ovilab.uxf.replayengine
{
    public class ReplayEngine : MonoBehaviour
    {
        public UnityAction OnSetPose;

        [Header("Data Parameters")]
        [SerializeField] private string participantID;
        [SerializeField] private string session;
        [SerializeField] private string trialsFile;

        [Header("Replay Paramters")]
        [SerializeField, Range(1, 2)] private float speedMult;
        [Range(0, 1), SerializeField] private float playbackTime;
        [SerializeField] private ReplayType replayType;
        [SerializeField] private List<ReplayTrackerTarget> targets;

        private string sessionPath;
        private string fullPath;

        private float startTime;
        private float endTime;
        private bool isPlaying;
        private List<TrackerReplayer> replayers = new();
        private List<string[]> trialData = new List<string[]>();
        private int currentRowIndex = 1;
        private Dictionary<string, string> paths = new Dictionary<string, string>();

        public string ParticipantID => participantID;
        public string Session => session;
        public string TrialsFile => trialsFile;


        /// <summary>
        /// Loads the trial data of a given participant, session and trial.
        /// Required path of UXF data folder with the participant ids.
        /// If any of the optional parameters are not provided, will default to the first available folder.
        /// If any step fails here, will return false.
        /// </summary>
        /// <param name="path">Path of data folder containing the different participants data</param>
        /// <param name="participantID">target participant id. Ignore if you want to load the first participant</param>
        /// <param name="session">target session id. Ignore if you want to load the first session</param>
        /// <param name="trialsFile">target trials file. You can add ".csv" at the end if you want, it'll be handled either which way. Leave empty to load first available csv in folder.</param>
        /// <returns>True if all the data loading was successful else false.</returns>
        public bool LoadData(string path, string participantID = "", string session = "", string trialsFile = "")
        {
            if (!ValidatePaths(path, participantID, session, trialsFile)) return false;

            fullPath = sessionPath + "/" + this.trialsFile;
            if (File.Exists(fullPath))
            {
                return LoadMainTrialData();
            }

            Debug.LogWarning("Wrong Path?");
            Debug.LogWarning(fullPath);
            return false;
        }

        private bool ValidatePaths(string path, string participantID, string session, string trialsFile)
        {
            string[] participantIDs;
            string[] sessions;
            string[] trialsFiles;
            // fetching participant
            try
            {
                string[] directories = Directory.GetDirectories(path);
                participantIDs = new string[directories.Length];
                for (int i = 0; i < directories.Length; i++)
                {
                    participantIDs[i] = new DirectoryInfo(directories[i]).Name;
                }
            }
            catch
            {
                Debug.LogError($"Invalid path provided!\n{path}");
                return false;
            }
            if (participantIDs.Length <= 0)
            {
                Debug.Log("No Participant IDs found!");
                return false;
            }
            if (string.IsNullOrEmpty(participantID))
            {
                this.participantID = participantIDs[0];
                Debug.Log($"No participant provided, using first available participant: {this.participantID}");
            }
            else
            {
                if (participantIDs.Contains(participantID)) this.participantID = participantID;
                else
                {
                    Debug.LogError($"Participant ID Not Found!\n Target: {participantID}; Available: {String.Join(", ", participantIDs)}");
                    return false;
                }
            }
            // fetching session
            string participantPath = path + "/" + this.participantID;
            try
            {
                string[] directories = Directory.GetDirectories(participantPath);
                sessions = new string[directories.Length];
                for (int i = 0; i < directories.Length; i++)
                {
                    sessions[i] = new DirectoryInfo(directories[i]).Name;
                }
            }
            catch
            {
                Debug.LogError($"Invalid path provided!\n{participantPath}");
                return false;
            }
            if (sessions.Length <= 0)
            {
                Debug.LogError("No sessions found");
                return false;
            }
            if (string.IsNullOrEmpty(session))
            {
                this.session = sessions[0];
                Debug.Log($"No session provided, using first available session: {this.session}");
            }
            else
            {
                if (sessions.Contains(session)) this.session = session;
                else
                {
                    Debug.LogError($"Session Not Found!\nTarget:{session}; Available:{String.Join(",",sessions)}");
                    return false;
                }
            }
            //fetching trial
            sessionPath = participantPath + "/" + this.session;
            try
            {
                string[] directories = Directory.GetFiles(sessionPath, "*.csv");
                trialsFiles = new string[directories.Length];
                for (int i = 0; i < directories.Length; i++)
                {
                    trialsFiles[i] = new DirectoryInfo(directories[i]).Name;
                }
            }
            catch
            {
                Debug.LogError($"Invalid path provided!\n{sessionPath}");
                return false;
            }
            if (trialsFiles.Length <= 0)
            {
                Debug.LogError("No Trial Files Found!");
                return false;
            }
            if (string.IsNullOrEmpty(trialsFile))
            {
                this.trialsFile = trialsFiles[0];
                Debug.Log($"No trials file provided, using first available trials file: {this.trialsFile}");
            }
            else
            {
                if (!trialsFile.EndsWith(".csv")) trialsFile += ".csv";
                if (trialsFiles.Contains(trialsFile)) this.trialsFile = trialsFile;
                else
                {
                    Debug.LogError($"Trial File Not Found!\nTarget:{trialsFile}; Available:{String.Join(",", trialsFiles)}");
                    return false;
                }
            }
            return true;
        }

        private bool LoadMainTrialData()
        {
            trialData = new List<string[]>();

            string[] lines = File.ReadAllLines(fullPath);
            foreach (string line in lines)
            {
                trialData.Add(line.Split(','));
            }

            if (trialData.Count > 1)
            {
                Init();
                Debug.Log("Loaded Data Successfully!");
                return true;
            }

            Debug.LogWarning("Trial File is empty. Is everything alright?");
            return false;
        }

        private void Init()
        {
            LoadRowData(1);
        }

        private bool LoadRowData(int rowIndex)
        {
            if (rowIndex < 1 || rowIndex >= trialData.Count)
            {
                Debug.LogError("Invalid row index");
                return false;
            }

            currentRowIndex = rowIndex;
            string[] row = trialData[rowIndex];

            paths = new Dictionary<string, string>();
            for (int i = Array.IndexOf(trialData[0], "canceled_trial") + 1; i < row.Length; i++)
            {
                string header = trialData[0][i];
                string relativePath = row[i];
                string fullPath = Path.Combine("/.." + "/Data", relativePath);

                paths[header] = fullPath;
            }

            return SetUpReplay(row);
        }

        private bool SetUpReplay(string[] row)
        {
            float startTime = float.Parse(row[Array.IndexOf(trialData[0], "start_time")]);
            float endTime = float.Parse(row[Array.IndexOf(trialData[0], "end_time")]);
            playbackTime = 0;
            return SetPaths(paths);
        }

        /// <summary>
        /// Iterates over all target transforms, and sets the <see cref="TrackerReplayer"/> component on the objects.
        /// If Set All Children As Targets is true, will recursively add the component to all children of the target object.
        /// </summary>
        public void SetTrackerReplayers()
        {
            foreach (ReplayTrackerTarget target in targets)
            {
                TrackerReplayer trackerReplayer = target.TargetTransform.gameObject.GetComponent<TrackerReplayer>();
                if (trackerReplayer == null) trackerReplayer = target.TargetTransform.gameObject.AddComponent<TrackerReplayer>();
                replayers.Add(trackerReplayer);
                if (target.SetAllChildrenAsTargets)
                {
                    AddTrackerRecursively(target.TargetTransform);
                }
            }

            Debug.Log("Added all tracker components!");
        }

        private void AddTrackerRecursively(Transform parent)
        {
            foreach (Transform child in parent)
            {
                TrackerReplayer component = child.gameObject.GetComponent<TrackerReplayer>();
                if(component==null) component = child.gameObject.AddComponent<TrackerReplayer>();
                replayers.Add(component);
                AddTrackerRecursively(child);
            }
        }

        private void FixedUpdate()
        {
            if (isPlaying && replayType == ReplayType.FixedUpdate)
            {
                Run();
            }
        }

        private void Update()
        {
            if (isPlaying && replayType == ReplayType.Update)
            {
                Run();
            }
        }

        private void LateUpdate()
        {
            if (isPlaying && replayType == ReplayType.LateUpdate)
            {
                Run();
            }
        }

        private void Run()
        {
            playbackTime += speedMult * Time.deltaTime / (endTime - startTime);
            if (playbackTime >= 1)
            {
                playbackTime = 1;
                isPlaying = false;
            }

            SetFullPose(playbackTime, true);

        }

        private void OnValidate()
        {
            if(Application.isPlaying)
            {
                SetFullPose(playbackTime, true);
            }
        }

        /// <summary>
        /// Sets all transforms with <see cref="TrackerReplayer"/> to the target trial with the target timestamp.
        /// </summary>
        /// <param name="timeStamp">target time stamp, either the same value from the tracker files, or a normalised value between 0 and 1 which ranges from the first to last timestamp in the tracker files.</param>
        /// <param name="isNormalised">is value between 0 and 1. If not, this expects the raw timestamp from the UXF trackers csv</param>
        /// <param name="trialIdx">target trial index. If ignored, will assume currently loaded trial.</param>
        public void SetTrialAndFullPose(float timeStamp, bool isNormalised, int trialIdx = -1)
        {
            if (trialIdx >= 0)
            {
                LoadRowData(trialIdx);
            }
        }

        private void SetFullPose(float timeStamp, bool isNormalised)
        {
            foreach (TrackerReplayer replayer in replayers)
            {
                if (replayer != null) replayer.SetPose(timeStamp, isNormalised);
            }

            OnSetPose?.Invoke();
        }

        private bool SetPaths(Dictionary<string, string> newPaths)
        {
            paths.Clear();

            foreach (KeyValuePair<string, string> entry in newPaths)
            {
                paths[entry.Key] = entry.Value;
            }

            foreach (KeyValuePair<string, string> entry in paths)
            {
                // SetPathForGO(entry.Key, entry.Value);
            }

            return false;
        }

        // private void SetPathForGO(string key, string path)
        // {
        //     string goName = key.Split(new string[] { "_movement_location_" }, System.StringSplitOptions.None)[0];
        //     goName = FixHeaderName(goName);
        //     GameObject go = GameObject.Find(goName);
        //     if (go == null)
        //     {
        //         Debug.LogWarning("GameObject not found: " + goName + ", skipping!");
        //         return;
        //     }
        //
        //     TrackerReplayer tracker = go.GetComponent<TrackerReplayer>();
        //     if (tracker == null)
        //     {
        //         tracker = go.AddComponent<TrackerReplayer>();
        //     }
        //
        //     tracker.Init(path);
        // }

        private static string FixHeaderName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            StringBuilder result = new StringBuilder();
            bool capitalizeNext = true;

            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    capitalizeNext = true;
                    result.Append(c);
                }
                else if (c == '_')
                {
                    capitalizeNext = false;
                    result.Append(c);
                }
                else
                {
                    if (capitalizeNext)
                    {
                        result.Append(char.ToUpper(c, CultureInfo.InvariantCulture));
                        capitalizeNext = false;
                    }
                    else
                    {
                        result.Append(c);
                    }
                }
            }

            return result.ToString();
        }

        public bool LoadNextTrial()
        {
            if (currentRowIndex < trialData.Count - 1)
            {
                currentRowIndex++;
                return LoadRowData(currentRowIndex);
            }
            Debug.LogWarning("No more trials!");
            return false;
        }

        public bool LoadPrevTrial()
        {
            if (currentRowIndex > 1)
            {
                currentRowIndex--;
                return LoadRowData(currentRowIndex);
            }
            Debug.LogWarning("No prior trials!");
            return false;
        }

        public void Play()
        {
            if (Application.isPlaying)
            {
                isPlaying = true;
            }
        }

        public void Pause()
        {
            if (Application.isPlaying)
            {
                isPlaying = false;
            }
        }

        public void Restart()
        {
            if(Application.isPlaying)
            {
                playbackTime = 0;
                isPlaying = false;
                SetFullPose(playbackTime, true);
            }
        }
    }

    [Serializable]
    public class ReplayTrackerTarget
    {
        public Transform TargetTransform;
        public bool SetAllChildrenAsTargets;
    }

    public enum ReplayType
    {
        Update = 0,
        FixedUpdate = 1,
        LateUpdate = 2
    }
}