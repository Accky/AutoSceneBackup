using System.Linq;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Cucurbit.Editor
{
    [InitializeOnLoad]
    public class AutoSceneBackup
    {
        const string SAVE_FOLDER = "Library/AutoSceneBackup/";

        static double nextTime = 0;

        static AutoSceneBackup()
        {
            EditorPrefs.DeleteKey(autoSceneBackup);
            EditorPrefs.DeleteKey(autoSaveInterval);
            EditorPrefs.DeleteKey(autoSaveCountMax);
            EditorPrefs.DeleteKey(outputBackupLog);

            AssureDirectoryExists(SAVE_FOLDER);

            EditorApplication.update += () =>
            {
                if (nextTime < EditorApplication.timeSinceStartup && IsAutoSceneBackup) {
                    nextTime = EditorApplication.timeSinceStartup + Interval;
                    Backup();
                }
            };
        }

        static void AssureDirectoryExists(string folder)
        {
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }
        }

        static string MakeAutoSaveDateDirectory(string folder)
        {
            AssureDirectoryExists(folder);
            var date = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss/", CultureInfo.InvariantCulture);
            var path = Path.Combine(folder + date);
            AssureDirectoryExists(path);
            return path;
        }

        static void Backup()
        {
            if (EditorApplication.isPlaying) { return; }

            var path = MakeAutoSaveDateDirectory(SAVE_FOLDER);

            string output = "";
            for (var i = 0; i < EditorSceneManager.sceneCount; ++i) {
                var scene = EditorSceneManager.GetSceneAt(i);
                string sceneName = EditorSceneManager.GetSceneAt(i).path;
                string expoertPath = Path.Combine(path + Path.GetFileName(sceneName));
                output += "\n  " + Path.GetFileName(sceneName);

                if (string.IsNullOrEmpty(sceneName)) {
                    return;
                }

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), expoertPath, true);
            }

            if (IsOutputBackupLog) {
                Debug.Log("Auto Scene Backup is done:" + output);
            }
            CheckBackup();
        }


        public class FolderInfo
        {
            public string path;
            public System.DateTime time;

            public FolderInfo(string p, System.DateTime t)
            {
                path = p;
                time = t;
            }
        }

        static void CheckBackup()
        {
            AssureDirectoryExists(SAVE_FOLDER);

            var files = Directory.GetDirectories(SAVE_FOLDER, "*", SearchOption.AllDirectories)
                .Select(x => new FolderInfo(x, System.IO.File.GetCreationTime(x)))
                .ToList();

            files.Sort((a, b) => System.DateTime.Compare(b.time, a.time));

            for (int i = 0; i < files.Count; i++) {
                if (i >= BackupMax) {
                    Directory.Delete(files[i].path, true);
                }
            }
        }


        [PreferenceItem("Auto Scene Backup")]
        static void ExampleOnGUI()
        {
            IsAutoSceneBackup = EditorGUILayout.BeginToggleGroup("Auto Scene Back Up", IsAutoSceneBackup);
            Interval = EditorGUILayout.IntField("Interval(sec)", Interval);
            BackupMax = EditorGUILayout.IntField("Backup Counts Max", BackupMax);
            IsOutputBackupLog = EditorGUILayout.Toggle("Output Backup Log", IsOutputBackupLog);
            EditorGUILayout.EndToggleGroup();
        }

        //keys
        private static readonly string autoSceneBackup = "AutoSceneBackup";
        private static readonly string autoSaveInterval = "BackupSceneInterval";
        private static readonly string autoSaveCountMax = "BackupSceneCountMax";
        private static readonly string outputBackupLog = "OutputBackupLog";

        //configuration
        private static readonly int intervalMin = 60;
        private static readonly int intervalDefault = 300;
        private static readonly int backupCountMin = 1;
        private static readonly int backupCountDefault = 10;

        static bool IsAutoSceneBackup {
            get {
                string value = EditorUserSettings.GetConfigValue(autoSceneBackup);
                return !string.IsNullOrEmpty(value) && value.Equals("True");
            }
            set {
                EditorUserSettings.SetConfigValue(autoSceneBackup, value.ToString());
            }
        }

        static int Interval {
            get {
                string value = EditorUserSettings.GetConfigValue(autoSaveInterval);
                return string.IsNullOrEmpty(value) ? intervalDefault : int.Parse(value);
            }
            set {
                value = Mathf.Max(value, intervalMin);
                EditorUserSettings.SetConfigValue(autoSaveInterval, value.ToString());
            }
        }

        static int BackupMax {
            get {
                string value = EditorUserSettings.GetConfigValue(autoSaveCountMax);
                return string.IsNullOrEmpty(value) ? backupCountDefault : int.Parse(value);
            }
            set {
                value = Mathf.Max(value, backupCountMin);
                EditorUserSettings.SetConfigValue(autoSaveCountMax, value.ToString());
            }
        }


        static bool IsOutputBackupLog {
            get {
                string value = EditorUserSettings.GetConfigValue(outputBackupLog);
                return !string.IsNullOrEmpty(value) && value.Equals("True");
            }
            set {
                EditorUserSettings.SetConfigValue(outputBackupLog, value.ToString());
            }
        }
    }
}
