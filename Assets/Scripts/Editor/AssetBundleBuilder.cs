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
using System.IO;
using UnityEngine;
using UnityEditor;

namespace PostSplitLoading.Editor
{
    public static class AssetBundleBuilder
    {
        private const string BundleName = "ExampleBundle";
        private const string ScenePath = "Assets/Scenes/AssetBundleScene.unity";

        [MenuItem("AssetBundleBuilder/Build Example")]
        public static void Build()
        {
            Build(BuildAssetBundleOptions.None, BundleName);
            Build(BuildAssetBundleOptions.UncompressedAssetBundle, BundleName + "-uncompressed");
            Build(BuildAssetBundleOptions.ChunkBasedCompression, BundleName + "-chunked");
        }

        public static void Build(BuildAssetBundleOptions options, string name)
        {
            var assetBundleBuild = new AssetBundleBuild();
            assetBundleBuild.assetBundleName = name;
            assetBundleBuild.assetNames = new[] {ScenePath};
            var assetBundleDirectory = Path.Combine(Application.streamingAssetsPath, "Bundles");
            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }

            var builtAssetBundleManifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                new[] {assetBundleBuild}, options, EditorUserBuildSettings.activeBuildTarget);

            // Returned AssetBundleManifest will be null if there was error in building assetbundle.
            if (builtAssetBundleManifest == null)
            {
                throw new Exception("Could not build example AssetBundle");
            }

            AssetDatabase.Refresh();
        }
    }
}