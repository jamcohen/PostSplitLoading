// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using GooglePlayInstant;
using System.Runtime.InteropServices;
using System;
using UnityEngine.SceneManagement;

namespace PostSplitLoading.SplitInstall
{
    public class SplitInstaller : MonoBehaviour
    {
        public Text DebugOutput;

        public void ButtonDownloadSplit()
        {
            StartCoroutine(CoDownloadSplit());
        }

        public void ButtonLoadFromSplit()
        {
            StartCoroutine(LoadAssetBundleFromSplit());
        }

        private IEnumerator CoDownloadSplit()
        {
            var manager = new SplitInstallManager();
            var listener = manager.StartModuleInstall("feature");
            var task = new SplitInstallTask(listener);

            while (!task.IsDone())
            {
                DebugOutput.text = task.GetStatus();
                yield return null;
            }

            DebugOutput.text = task.GetStatus();
        }

        // When a split finishes installing the hash at the end of the /data/app/com.bundle.identifier path changes.
        // Unity isn't aware of this change so their streamingAssets path doesn't update.
        // Because of this we need to look for it ourselves.
        private IEnumerator LoadAssetBundleFromSplit()
        {
            string apkPath = "";
            try
            {
                using (var activity = UnityPlayerHelper.GetCurrentActivity())
                using (var newContext = activity.Call<AndroidJavaObject>("createPackageContext",
                    activity.Call<string>("getPackageName"), 0))
                using (var assetManager = newContext.Call<AndroidJavaObject>("getAssets"))
                using (var assetFileDescriptor = assetManager.Call<AndroidJavaObject>("openFd", "examplebundle"))
                using (var parcelFileDescriptor =
                    assetFileDescriptor.Call<AndroidJavaObject>("getParcelFileDescriptor"))
                using (var processClass = new AndroidJavaClass("android.os.Process"))
                {
                    if (GooglePlayInstantUtils.IsAtLeastO())
                    {
                        SplitInstallManager.UpdateAppInfo(activity);
                        SplitInstallManager.UpdateAppInfo(newContext);
                    }

                    var pid = processClass.CallStatic<int>("myPid");
                    var fd = parcelFileDescriptor.Call<int>("getFd");
                    Debug.LogFormat("fd={0}, pid={1}", fd, pid);

                    var fdSymPath = string.Format("/proc/{0}/fd/{1}", pid, fd);

                    IntPtr fdPath = Loader.ReadLink(fdSymPath);
                    apkPath = Marshal.PtrToStringAuto(fdPath);
                }
            }
            catch (AndroidJavaException e)
            {
                DisplayError(e.ToString());
                yield break;
            }

            var bundlePath = string.Format("jar:file:///{0}!/assets/examplebundle", apkPath);
            Debug.LogFormat("Fetching bundle at {0}", bundlePath);
            using (WWW loadFeature = new WWW(bundlePath))
            {
                yield return loadFeature;

                if (!string.IsNullOrEmpty(loadFeature.error))
                {
                    Debug.LogErrorFormat("Error loading AssetBundle from split: {0}", loadFeature.error);
                }

                if (loadFeature.assetBundle == null)
                {
                    Debug.LogFormat("File at path {0} could not be loaded as an AssetBundle", bundlePath);
                }

                SceneManager.LoadScene(loadFeature.assetBundle.GetAllScenePaths()[0]);
            }
        }

        private void DisplayError(string error)
        {
            Debug.LogError(error);
            DebugOutput.text = error;
        }

        private void DisplayLog(string log)
        {
            Debug.Log(log);
            DebugOutput.text = log;
        }
    }
}