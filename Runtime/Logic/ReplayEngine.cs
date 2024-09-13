using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Events;
using UXF;

namespace ubco.ovilab.uxf.replayengine
{
    public class ReplayEngine : MonoBehaviour
    {
        public UnityAction OnSetPose;

        [Header("Data Parameters")]
        [SerializeField] private string participantID;
        [SerializeField] private string session;
        [SerializeField] private string trialsFile;

        [Header("Replay Parameters")]
        [SerializeField] private int trialNumber;
        [Range(0, 1), SerializeField] private float playbackTime;
        [SerializeField] private ReplayType replayType;
        [SerializeField] private List<PositionRotationTracker> targetActiveTrackerReplayers;

        private string dataPath;
        private string sessionPath;
        private string fullPath;
        private float startTime;
        private float endTime;
        private bool isPlaying;
        private List<TrackerReplayer> activeReplayers = new List<TrackerReplayer>();
        private List<string[]> trialData = new List<string[]>();
        private int currentRowIndex = 1;
        private bool hasInit;

        public string ParticipantID => participantID;
        public string Session => session;
        public string TrialsFile => trialsFile;
        public List<PositionRotationTracker> TargetActiveTrackerReplayers => targetActiveTrackerReplayers;

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
            dataPath = path;
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

            startTime = float.Parse(row[Array.IndexOf(trialData[0], "start_time")]);
            endTime = float.Parse(row[Array.IndexOf(trialData[0], "end_time")]);

            foreach (TrackerReplayer replayer in activeReplayers)
            {
                string trackerName = replayer.GetTrackerName();
                //this is a hack and should be fixed later
                int targetIdx = Array.FindIndex(trialData[0], s => s.ToLower().Contains(trackerName.ToLower()));
                string targetPath = dataPath + "/../" + row[targetIdx];
                replayer.SetPathAndLoadData(targetPath);
            }

            return true;
        }

        /// <summary>
        /// Fetches all <see cref="PositionRotationTracker"/> attached GameObjects in the scene
        /// Uses the input list to validate and focus on a specific subset of trackers.
        /// If the input list is empty, focuses on all trackers.
        /// </summary>
        /// <param name="activeTrackerReplayers"></param>
        /// <returns>True if everything went alright</returns>
        public bool SetActiveTrackerReplayers(List<PositionRotationTracker> activeTrackerReplayers)
        {
            PositionRotationTracker[] allTrackers = FindObjectsOfType<PositionRotationTracker>();
            int addedCounter = 0;
            activeReplayers.Clear();
            foreach (PositionRotationTracker targetTracker in activeTrackerReplayers)
            {
                try
                {
                    if (allTrackers.Contains(targetTracker))
                    {
                        TrackerReplayer tr = targetTracker.gameObject.GetComponent<TrackerReplayer>();
                        if (tr == null) tr = targetTracker.gameObject.AddComponent<TrackerReplayer>();
                        activeReplayers.Add(tr);
                        addedCounter++;
                    }
                    else
                    {
                        Debug.LogError(
                            $"Object {targetTracker.gameObject.name} was not found with a Position Rotation Tracker, is everything okay?");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
            }
            Debug.Log($"Active: {addedCounter} of total: {allTrackers.Length} trackers in the scene");
            if(!hasInit)
            {
                Init();
                hasInit = true;
            }
            return true;
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
            playbackTime += Time.deltaTime / (endTime - startTime);
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
            SetFullPose(timeStamp, isNormalised);
        }

        private void SetFullPose(float timeStamp, bool isNormalised)
        {
            foreach (TrackerReplayer replayer in activeReplayers)
            {
                if (replayer != null) replayer.SetPose(timeStamp, isNormalised);
            }

            OnSetPose?.Invoke();
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

    public enum ReplayType
    {
        Update = 0,
        FixedUpdate = 1,
        LateUpdate = 2
    }
}