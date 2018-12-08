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
using System.IO;
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

        private static AssetBundle _bundle;
        private string _bundlePath;

        public void ButtonLoadScene()
        {
            SceneManager.LoadScene(1);
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

        public void ButtonLoadSceneFromJar()
        {
            StartCoroutine(CoLoadScene(CoLoadSceneWithWWW()));
        }

        public void ButtonLoadSceneFromStreamingAssets()
        {
            StartCoroutine(CoLoadScene(CoLoadSceneFromStreamingAssets()));
            ;
        }

        public void ButtonLoadSceneFromZip()
        {
            StartCoroutine(CoLoadScene(CoLoadSceneFromZip()));
        }

        private IEnumerator CoLoadScene(IEnumerator LoadAssetBundle)
        {
            if (_bundle != null)
            {
                yield return UnloadAssetBundle();
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

            SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(scenePaths[0]));
        }

        private IEnumerator UnloadAssetBundle()
        {
            _bundle.Unload(true);
            _bundle = null;
            var unloading = Resources.UnloadUnusedAssets();
            yield return unloading;
        }

        private IEnumerator CoLoadSceneFromStreamingAssets()
        {
            yield return null;
            _bundlePath = Path.Combine(Application.streamingAssetsPath, "Bundles/Bundles.zip");
            string compressedFolderPath = _bundlePath.Replace("jar:file://", "");

            WalkPath(compressedFolderPath);
            var zipLoader = new ZipLoader(compressedFolderPath);
            /*var request = zipLoader.LoadAssetBundleAsync("Bundles/examplebundle");
            yield return request;
            _bundle = request.assetBundle;*/

            byte[] assetBundleBytes = zipLoader.LoadFile("Bundles/examplebundle");
            var request = AssetBundle.LoadFromMemoryAsync(assetBundleBytes);
            yield return request;
            _bundle = request.assetBundle;
        }

        private IEnumerator CoLoadSceneWithWWW()
        {
            string filePath = Application.persistentDataPath + PathInput.text;
            _bundlePath = "jar:file://" + filePath;

            WalkPath(_bundlePath);

            var www = new WWW(_bundlePath);
            yield return www;
            _bundle = www.assetBundle;
            if (!string.IsNullOrEmpty(www.error))
            {
                DisplayError(www.error);
            }
        }

        private IEnumerator CoLoadSceneFromZip()
        {
            string filePath = Application.persistentDataPath + PathInput.text;
            WalkPath(filePath);
            var zipLoader = new ZipLoader(filePath);
            var request = zipLoader.LoadAssetBundleAsync("Bundles/examplebundle");
            yield return request;
            _bundle = request.assetBundle;
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