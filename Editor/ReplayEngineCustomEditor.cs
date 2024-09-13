using ubco.ovilab.uxf.replayengine;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ReplayEngine))]
public class ReplayEngineCustomEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        ReplayEngine replayEngine = (ReplayEngine)target;

        if (GUILayout.Button("Select Path and Load Data"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", "", "");
            if(!string.IsNullOrEmpty(path))
                replayEngine.LoadData(path, replayEngine.ParticipantID, replayEngine.Session, replayEngine.TrialsFile);
            else
            {
                Debug.LogError("Something went wrong, try again!");
            }
        }

        if (GUILayout.Button("Set Replayer Components on Rig"))
        {
            replayEngine.SetTrackerReplayers();
        }

        if (GUILayout.Button("Next Trial"))
        {
            replayEngine.LoadNextTrial();
        }

        if (GUILayout.Button("Previous Trial"))
        {
            replayEngine.LoadPrevTrial();
        }

        if (GUILayout.Button("Restart"))
        {
            replayEngine.Restart();
        }

        if (GUILayout.Button("Play"))
        {
            replayEngine.Play();
        }

        if (GUILayout.Button("Pause"))
        {
            replayEngine.Pause();
        }

        DrawDefaultInspector();
    }
}
