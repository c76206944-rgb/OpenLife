#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OpenLife.Editor
{
    /// <summary>Repeatable player builds from the Unity Editor menu.</summary>
    public static class OpenLifeBuild
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));

        [MenuItem("OpenLife/Build/Windows x86_64")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, Path.Combine(Root, "Windows", "OpenLife.exe"));
        }

        [MenuItem("OpenLife/Build/Android ARM64 APK")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            Build(BuildTarget.Android, Path.Combine(Root, "Android", "OpenLife.apk"));
        }

        [MenuItem("OpenLife/Build/WebGL for Vercel")]
        public static void BuildWebGL()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            Build(BuildTarget.WebGL, Path.Combine(Root, "WebGL"));
        }

        static void Build(BuildTarget target, string output)
        {
            var scenes = System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
            if (scenes.Length == 0)
            {
                Debug.LogError("OpenLife build stopped: add and enable the game scene(s) in File > Build Profiles / Build Settings.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(scenes, scene => scene.path),
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log("OpenLife build complete: " + output + " (" + report.summary.totalSize + " bytes)");
            else
                Debug.LogError("OpenLife build failed: " + report.summary.result + ". Check the Unity Editor Console for details.");
        }
    }
}
#endif
