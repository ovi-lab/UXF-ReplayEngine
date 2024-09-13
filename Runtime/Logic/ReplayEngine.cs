using System;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine.Events;

namespace ubco.ovilab.uxf.replayengine
{
    public class ReplayEngine : MonoBehaviour
    {
        public UnityAction OnSetPose;
        public UnityAction OnFinishedPlaying;

        [SerializeField, Range(1, 2)] private float speedMult;
        [Range(0, 1), SerializeField] private float playbackTime;
        [SerializeField] private ReplayType replayType;
        [SerializeField] private Transform rigParent;
        [SerializeField] private int participantID;

        private float startTime;
        private float endTime;
        private bool isPlaying;
        private List<TrackerHandler> replayers = new();
        private List<string[]> trialData = new List<string[]>();
        private int currentRowIndex = 1;
        private string currWord;
        private string fullPath;
        private ReplayEngine replayEngine;
        private Dictionary<string, string> paths = new Dictionary<string, string>();

        public void LoadData(string path)
        {
            replayEngine = GetComponent<ReplayEngine>();
            fullPath = path +"/"+ participantID + "/S001/trial_results.csv";
            if (File.Exists(fullPath))
            {
                LoadMainTrialData();
                replayEngine.SetManualTrackers();
            }
            else
            {
                Debug.LogWarning("Wrong Path?");
                Debug.LogWarning(fullPath);
            }
        }

        public bool LoadNextTrial()
        {
            if (currentRowIndex < trialData.Count - 1)
            {
                currentRowIndex++;
                ChangeByIndex(currentRowIndex);
                return true;
            }
            else
            {
                Debug.LogWarning("No more trials!");
                return false;
            }
        }

        public bool LoadPrevTrial()
        {
            if (currentRowIndex > 1)
            {
                currentRowIndex--;
                ChangeByIndex(currentRowIndex);
                return true;
            }
            else
            {
                Debug.LogWarning("No prior trials!");
                return false;
            }
        }

        private void LoadMainTrialData()
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
            }

            Debug.Log("Loaded Data Successfully!");
        }

        private void Init()
        {
            LoadRowData(1);
        }

        private void LoadRowData(int rowIndex)
        {
            if (rowIndex < 1 || rowIndex >= trialData.Count)
            {
                Debug.LogError("Invalid row index");
                return;
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

            SetUpReplay(row);
        }

        private void SetUpReplay(string[] row)
        {
            float startTime = float.Parse(row[Array.IndexOf(trialData[0], "start_time")]);
            float endTime = float.Parse(row[Array.IndexOf(trialData[0], "end_time")]);

            Init(startTime, endTime, paths);
        }

        public void ChangeByIndex(int index)
        {
            LoadRowData(index);
        }


        public void SetManualTrackers()
        {
            if (rigParent != null)
            {
                AddTrackerRecursively(rigParent);
            }
            else
            {
                Debug.LogWarning("Missing rig parent!");
            }
        }

        private void AddTrackerRecursively(Transform parent)
        {
            foreach (Transform child in parent)
            {
                TrackerHandler component = child.gameObject.GetComponent<TrackerHandler>();
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
                OnFinishedPlaying?.Invoke();
            }

            SetFullPose();

        }

        private void OnValidate()
        {
            SetFullPose();
        }

        private void SetFullPose()
        {
            foreach (TrackerHandler replayer in replayers)
            {
                if (replayer != null) replayer.SetPose(playbackTime);
            }

            OnSetPose?.Invoke();
        }

        public void Init(float startTime, float endTime, Dictionary<string, string> paths)
        {
            this.startTime = startTime;
            this.endTime = endTime;
            playbackTime = 0;
            SetPaths(paths);
        }

        public void SetPaths(Dictionary<string, string> newPaths)
        {
            paths.Clear();

            foreach (KeyValuePair<string, string> entry in newPaths)
            {
                paths[entry.Key] = entry.Value;
            }

            foreach (KeyValuePair<string, string> entry in paths)
            {
                SetPathForGO(entry.Key, entry.Value);
            }
        }

        private void SetPathForGO(string key, string path)
        {
            string goName = key.Split(new string[] { "_movement_location_" }, System.StringSplitOptions.None)[0];
            goName = FixHeaderName(goName);
            GameObject go = GameObject.Find(goName);
            if (go == null)
            {
                Debug.LogWarning("GameObject not found: " + goName + ", skipping!");
                return;
            }

            TrackerHandler tracker = go.GetComponent<TrackerHandler>();
            if (tracker == null)
            {
                tracker = go.AddComponent<TrackerHandler>();
            }

            tracker.Init(path);
        }

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

        public void Play()
        {
            if (Application.isPlaying)
            {
                if (Mathf.Approximately(playbackTime, 1f)) playbackTime = 0;
                isPlaying = true;
            }
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void Restart()
        {
            playbackTime = 0;
            isPlaying = false;
            SetFullPose();
        }
    }

    public enum ReplayType
    {
        Update = 0,
        FixedUpdate = 1,
        LateUpdate = 2
    }
}