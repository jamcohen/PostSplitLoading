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

using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PostSplitLoading
{
    public class LoadOptions : MonoBehaviour
    {
        public Toggle StreamingAssetsToggle;
        public Toggle LoadFromFileToggle;
        public Toggle CompressedBundleToggle;
        public Toggle UncompressedBundleToggle;

        public string StreamingAssetsBundlePath { get; private set; }
        public string PersistentDataBundlePath { get; private set; }

        private void Awake()
        {
            StreamingAssetsBundlePath = Path.Combine(Application.streamingAssetsPath, "Bundles/Bundles.zip");
            PersistentDataBundlePath = Path.Combine(Application.persistentDataPath, "Bundles/Bundles.zip");
        }

        public string GetLoadingLocation()
        {
            string path = "";
            if (StreamingAssetsToggle.isOn)
            {
                path = StreamingAssetsBundlePath.Replace("jar:file://", "");
            }
            else
            {
                path = PersistentDataBundlePath;
            }

            return path;
        }

        public bool GetLoadFromFile()
        {
            return LoadFromFileToggle.isOn;
        }

        public string GetAssetBundleName()
        {
            if (CompressedBundleToggle.isOn)
            {
                return "Bundles/examplebundle";
            }

            if (UncompressedBundleToggle)
            {
                return "Bundles/examplebundle-uncompressed";
            }

            return "Bundles/examplebundle-chunked";
        }
    }
}