using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UXF;

namespace ubco.ovilab.uxf.replayengine
{
    [DisallowMultipleComponent]
    public class TrackerReplayer : MonoBehaviour
    {
        private List<float> times = new List<float>();
        private List<Vector3> positions = new List<Vector3>();
        private List<Quaternion> rotations = new List<Quaternion>();
        private Dictionary<float, (Vector3, Quaternion)> normalizedData = new Dictionary<float, (Vector3, Quaternion)>();

        public string GetTrackerName() => GetComponent<PositionRotationTracker>().objectName;

        public void SetPathAndLoadData(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("File not found for " + gameObject.name + " @ " + path);
                return;
            }

            times.Clear();
            positions.Clear();
            rotations.Clear();
            normalizedData.Clear();

            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                float time = float.Parse(values[0], CultureInfo.InvariantCulture);
                float posX = float.Parse(values[1], CultureInfo.InvariantCulture);
                float posY = float.Parse(values[2], CultureInfo.InvariantCulture);
                float posZ = float.Parse(values[3], CultureInfo.InvariantCulture);
                float rotX = float.Parse(values[4], CultureInfo.InvariantCulture);
                float rotY = float.Parse(values[5], CultureInfo.InvariantCulture);
                float rotZ = float.Parse(values[6], CultureInfo.InvariantCulture);

                times.Add(time);
                positions.Add(new Vector3(posX, posY, posZ));
                rotations.Add(Quaternion.Euler(rotX, rotY, rotZ));
            }

            PreprocessData();
        }

        public void SetPose(float timeStamp, bool isNormalised)
        {
            float targetTimeStamp;
            if(isNormalised)
            {
                if (normalizedData.Count == 0)
                {
                    Debug.LogError("No data to replay. Please initialize with a valid path.");
                    return;
                }

                if (timeStamp is > 1f or < 0f)
                {
                    Debug.LogWarning("Time value not normalised! Clamping between 0 and 1");
                    timeStamp = Mathf.Clamp01(timeStamp);
                }

                targetTimeStamp = FindClosestTime(timeStamp);
            }
            else targetTimeStamp = timeStamp;
            Vector3 position = normalizedData[targetTimeStamp].Item1;
            Quaternion rotation = normalizedData[targetTimeStamp].Item2;

            transform.position = position;
            transform.rotation = rotation;
        }

        private void PreprocessData()
        {
            float startTime = times[0];
            float endTime = times[^1];
            float duration = endTime - startTime;

            for (int i = 0; i < times.Count; i++)
            {
                float normalizedTime = (times[i] - startTime) / duration;
                normalizedData[normalizedTime] = (positions[i], rotations[i]);
            }
        }

        private float FindClosestTime(float normalizedValue)
        {
            float closestTime = float.MaxValue;
            float smallestDifference = float.MaxValue;

            foreach (var time in normalizedData.Keys)
            {
                float difference = Mathf.Abs(time - normalizedValue);
                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    closestTime = time;
                }
            }

            return closestTime;
        }
    }
}