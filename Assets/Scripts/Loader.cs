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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PostSplitLoading
{
    public class Loader : MonoBehaviour
    {
        public Text LibraryLoadedStatus;
        public Button LoadLibraryButton;

        public RawImage ImageDisplay;
        public Text DebugOutput;
        public Text PathInput;

        private static Dictionary<string, AssetBundle> _bundleByName = new Dictionary<string, AssetBundle>();
        private string _bundlePath;
        private LoadOptions _options;

        private void Start()
        {
            _options = GetComponent<LoadOptions>();
            StartCoroutine(CoCopyBundleIntoPersistent());
        }

        public void ButtonLoadScene()
        {
            var bundleName = _options.GetAssetBundleName();
            AssetBundle bundle;
            if (!_bundleByName.TryGetValue(bundleName, out bundle))
            {
                DisplayError(string.Format("Cannot load scene because bundle: {0} is not loaded.", bundleName));
                return;
            }

            var scenePaths = bundle.GetAllScenePaths();
            if (scenePaths.Length == 0)
            {
                DisplayError(string.Format("Cannot load scene because bundle: {0} contains no scenes.", bundleName));
            }

            SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(scenePaths[0]));
        }

        public void ButtonLoadBundle()
        {
            StartCoroutine(CoLoadScene(CoLoadSceneFromZip()));
        }

        public void ButtonUnloadBundles()
        {
            StartCoroutine(CoUnloadAssetBundles());
        }

        /// <summary>
        /// Copies Bundle.zip from Application.StreamingAssets to Application.PersistentData,
        /// because streaming assets can't be accessed directly on Android.
        /// </summary>
        private IEnumerator CoCopyBundleIntoPersistent()
        {
            var streamingPath = _options.StreamingAssetsBundlePath;
#if !UNITY_ANDROID || UNITY_EDITOR
            streamingPath = "file:///" + streamingPath;
#endif

            using (WWW www = new WWW(streamingPath))
            {
                yield return www;
                if (!string.IsNullOrEmpty(www.error))
                {
                    DisplayError(string.Format("Failed to copy {0} into persistent data, {1}",
                        streamingPath, www.error));
                    yield break;
                }

                FileInfo fileInfo = new FileInfo(_options.PersistentDataBundlePath);
                DirectoryInfo bundleFolder = fileInfo.Directory;
                if (!bundleFolder.Exists)
                {
                    bundleFolder.Create();
                }

                File.WriteAllBytes(_options.PersistentDataBundlePath, www.bytes);
            }
        }

        private IEnumerator CoLoadScene(IEnumerator LoadAssetBundle)
        {
            if (_bundleByName.Count > 0)
            {
                yield return CoUnloadAssetBundles();
            }

            yield return LoadAssetBundle;

            var bundleName = _options.GetAssetBundleName();
            if (!bundleName.Contains(bundleName))
            {
                DisplayError("Failed to load bundle with path: " + _bundlePath);
                yield break;
            }
        }

        private IEnumerator CoUnloadAssetBundles()
        {
            foreach (var pair in _bundleByName)
            {
                yield return CoUnloadAssetBundle(pair.Value);
            }

            _bundleByName.Clear();
        }

        private IEnumerator CoUnloadAssetBundle(AssetBundle bundle)
        {
            bundle.Unload(true);
            var unloading = Resources.UnloadUnusedAssets();
            yield return unloading;
        }

        private IEnumerator CoLoadSceneFromZip()
        {
            _bundlePath = _options.GetLoadingLocation();
            WalkPath(_bundlePath);
            yield return CoLoadFromZip(_bundlePath, !_options.GetLoadFromFile());
        }

        private IEnumerator CoLoadFromZip(string path, bool loadFromMemory)
        {
            var zipLoader = new ZipLoader(path);
            var bundleName = _options.GetAssetBundleName();
            AssetBundle bundle;

            if (loadFromMemory)
            {
                byte[] assetBundleBytes = zipLoader.LoadFile(bundleName);
                var request = AssetBundle.LoadFromMemoryAsync(assetBundleBytes);
                yield return request;
                bundle = request.assetBundle;
            }
            else
            {
                var request = zipLoader.LoadAssetBundleAsync(bundleName);
                yield return request;
                bundle = request.assetBundle;
            }

            if (bundle != null)
            {
                _bundleByName.Add(bundleName, bundle);
            }
        }

        private void WalkPath(string path)
        {
            var log = new StringBuilder();
            var directories = path.Split('/');
            var currentDir = "/";
            for (int i = 1; i < directories.Length; i++)
            {
                currentDir = Path.Combine(currentDir, directories[i]);
                string foundFile = Directory.Exists(currentDir) || File.Exists(currentDir)
                    ? "Found file: " + currentDir
                    : "Cannot find file: " + currentDir;

                log.AppendLine(foundFile);
            }

            Debug.Log(log);
        }

        private void DisplayError(string error)
        {
            Debug.LogError(error);
            DebugOutput.text = error;
        }

        [DllImport("ReadLink")]
        public static extern IntPtr ReadLink(string path);
    }
}