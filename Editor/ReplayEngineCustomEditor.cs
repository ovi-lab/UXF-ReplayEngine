using UnityEditor;
using UnityEngine;



namespace ubco.ovilab.uxf.replayengine
{
    [CustomEditor(typeof(ReplayEngine))]
    public class ReplayEngineCustomEditor : Editor
    {

        private Texture2D playTrialIcon;
        private Texture2D restartTrialIcon;
        private Texture2D pauseTrialIcon;
        private Texture2D nextTrialIcon;
        private Texture2D previousTrialIcon;
        private GUIStyle buttonStyle;
        private void OnEnable()
        {
            playTrialIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/ubco.ovilab.uxf.replay-engine/Editor/Icons/play.png", typeof(Texture2D));
            restartTrialIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/ubco.ovilab.uxf.replay-engine/Editor/Icons/restart.png", typeof(Texture2D));
            pauseTrialIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/ubco.ovilab.uxf.replay-engine/Editor/Icons/pause.png", typeof(Texture2D));
            previousTrialIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/ubco.ovilab.uxf.replay-engine/Editor/Icons/previous.png", typeof(Texture2D));
            nextTrialIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/ubco.ovilab.uxf.replay-engine/Editor/Icons/next.png", typeof(Texture2D));
        }

        public override void OnInspectorGUI()
        {
            ReplayEngine replayEngine = (ReplayEngine)target;
            
            buttonStyle = new GUIStyle(GUI.skin.button)
            { 
                fixedWidth = 50,  // Button width
                fixedHeight = 50,  // Button height
            };

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

            if (GUILayout.Button("Set Active Tracker Replayers"))
            {
                replayEngine.SetActiveTrackerReplayers(replayEngine.TargetActiveTrackerReplayers);
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(previousTrialIcon), buttonStyle))
                {
                    replayEngine.LoadPrevTrial();
                }
                
                if (GUILayout.Button(new GUIContent(restartTrialIcon), buttonStyle))
                {
                    replayEngine.Restart();
                }

                if (GUILayout.Button(new GUIContent(playTrialIcon), buttonStyle))
                {
                    replayEngine.Play();
                }

                if (GUILayout.Button(new GUIContent(pauseTrialIcon), buttonStyle))
                {
                    replayEngine.Pause();
                }
                
                if (GUILayout.Button(new GUIContent(nextTrialIcon), buttonStyle))
                {
                    replayEngine.LoadNextTrial();
                }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawDefaultInspector();
        }
    }
}

