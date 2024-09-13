using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace ubco.ovilab.uxf.replayengine.editor
{
    public class UxfExtensionsDependencyCheckerWindow : EditorWindow
    {
        static ListRequest Request;

        [MenuItem("Window/Check Dependencies")]
        public static void ShowWindow()
        {
            Request = Client.List();
            EditorApplication.update += WaitForRequest;
        }

        private static void CheckIfPackageIsInstalled()
        {
            if(Request.Status != StatusCode.Success) Debug.LogWarning(Request.Error.message);
            foreach (PackageInfo package in Request.Result)
            {
                if (package.name.Contains("uxf.extensions")) return;
            }
            UxfExtensionsDependencyCheckerWindow window =
                GetWindow<UxfExtensionsDependencyCheckerWindow>("Dependency Checker");
            window.minSize = new Vector2(400, 80);
            window.maxSize = new Vector2(400, 80);
        }

        private static void WaitForRequest()
        {
            // there's probably a better way to do this
            if(!Request.IsCompleted) System.Threading.Thread.Sleep(50);
            else
            {
                EditorApplication.update -= WaitForRequest;
                CheckIfPackageIsInstalled();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Missing Dependency Package", EditorStyles.boldLabel);
            GUILayout.Label("UXF Extensions is not installed. Click the button below to install it.");

            if (!GUILayout.Button("Install Package")) return;
            InstallUxfExtensions();
            Close();
        }

        private void InstallUxfExtensions()
        {
            Client.Add("https://github.com/ovi-lab/UXF-extensions.git");
        }
    }

    [InitializeOnLoad]
    public static class DependencyChecker
    {
        static DependencyChecker()
        {
            UxfExtensionsDependencyCheckerWindow.ShowWindow();
        }
    }
}