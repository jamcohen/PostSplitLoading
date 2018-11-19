﻿// Copyright 2018 Google LLC
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
using UnityEngine;
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

        private static AssetBundle _bundle;
        
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
            catch(DllNotFoundException e)
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

        public void LoadSceneFromStreamingAssets()
        {
            StartCoroutine(Co_LoadSceneFromStreamingAssets());
        }

        private IEnumerator Co_LoadSceneFromStreamingAssets()
        {
            if (_bundle != null)
            {
                _bundle.Unload(true);
                _bundle = null;
                var unloading = Resources.UnloadUnusedAssets();
                yield return unloading;
            }

            var bundlePath = Path.Combine(Application.streamingAssetsPath, "Bundles/examplebundle");
            _bundle = AssetBundle.LoadFromFile(bundlePath);

            if (_bundle == null)
            {
                DisplayError("Failed to load bundle with path: "+bundlePath);
                yield break;
            }
            
            var scenePaths = _bundle.GetAllScenePaths();
            if (scenePaths.Length == 0)
            {
                DisplayError("ExampleBundle does not contain a scene to load");
                DisplayError("Failed to load bundle with path: "+bundlePath);
            }
            
            SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(scenePaths[0]));
        }

        private void DisplayError(string error)
        {
            Debug.LogError(error);
            DebugOutput.text = error;
        }

        [DllImport("ReadLink")]
        private static extern void ReadLink(string path);
    }
}
