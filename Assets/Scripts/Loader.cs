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

        public Toggle LoadMemoryCheckbox;

        private static AssetBundle _bundle;
        private static HashSet<AssetBundle> _bundles;
        private string _bundlePath;
        private LoadOptions _options;

        private void Start()
        {
            _options = GetComponent<LoadOptions>();
        }

        public void ButtonLoadScene()
        {
            SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(_bundle.GetAllScenePaths()[0]));
            //SceneManager.LoadScene(1);
        }

        public void ButtonLoadNativeLib()
        {
            try
            {
                // Pass empty string to library function to load the library.
                ReadLink("");
            }
            catch (DllNotFoundException e)
            {
                DisplayError(e.ToString());
                return;
            }

            LoadLibraryButton.enabled = false;
            LibraryLoadedStatus.text = "Library Loaded";
        }

        public void ButtonLoadResources()
        {
            var image = Resources.Load<Texture2D>("ExampleImage");
            ImageDisplay.texture = image;

            if (image == null)
            {
                DisplayError("Failed to load image from resources");
            }
        }

        public void ButtonLoadSceneFromStreamingAssets()
        {
            StartCoroutine(CoLoadScene(CoLoadSceneFromStreamingAssets()));
        }

        public void ButtonLoadSceneFromZip()
        {
            StartCoroutine(CoLoadScene(CoLoadSceneFromPersistent()));
        }

        private IEnumerator CoLoadScene(IEnumerator LoadAssetBundle)
        {
            if (_bundles.Count > 0)
            {
                _bundle = null;
                foreach (var bundle in _bundles)
                {
                    yield return UnloadAssetBundle(bundle);
                }

                _bundles.Clear();
            }

            yield return LoadAssetBundle;

            if (_bundle == null)
            {
                DisplayError("Failed to load bundle with path: " + _bundlePath);
                yield break;
            }

            var scenePaths = _bundle.GetAllScenePaths();
            if (scenePaths.Length == 0)
            {
                DisplayError("ExampleBundle does not contain a scene to load");
                DisplayError("Failed to load bundle with path: " + _bundlePath);
            }

            //SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(scenePaths[0]));
        }

        private IEnumerator UnloadAssetBundle(AssetBundle bundle)
        {
            bundle.Unload(true);
            var unloading = Resources.UnloadUnusedAssets();
            yield return unloading;
        }

        private IEnumerator CoLoadSceneFromStreamingAssets()
        {
            _bundlePath = _options.GetLoadingLocation();

            WalkPath(_bundlePath);
            yield return CoLoadFromZip(_bundlePath, !_options.GetLoadFromFile());
        }

        private IEnumerator CoLoadSceneFromPersistent()
        {
            string filePath = _options.GetLoadingLocation();
            WalkPath(filePath);
            yield return CoLoadFromZip(filePath, !_options.GetLoadFromFile());
        }

        private IEnumerator CoLoadFromZip(string path, bool loadFromMemory)
        {
            var zipLoader = new ZipLoader(path);

            if (loadFromMemory)
            {
                byte[] assetBundleBytes = zipLoader.LoadFile(_options.GetAssetBundleName());
                var request = AssetBundle.LoadFromMemoryAsync(assetBundleBytes);
                yield return request;
                _bundle = request.assetBundle;
            }
            else
            {
                var request = zipLoader.LoadAssetBundleAsync(_options.GetAssetBundleName());
                yield return request;
                _bundle = request.assetBundle;
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