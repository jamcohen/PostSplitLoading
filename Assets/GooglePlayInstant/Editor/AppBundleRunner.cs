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
using System.Net;
using GooglePlayInstant.Editor.AndroidManifest;
using GooglePlayInstant.Editor.GooglePlayServices;
using GooglePlayInstant.Editor.QuickDeploy;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper to build an Android App Bundle file on Unity version 2018.2 and earlier.
    /// </summary>
    public static class AppBundleRunner
    {
        public static bool BuildAndRun()
        {
            var aabFilePath = EditorUtility.SaveFilePanel("Create Android App Bundle", null, null, "aab");
            if (string.IsNullOrEmpty(aabFilePath)) return false;
            AppBundleBuilder.Build(aabFilePath);
            return Run(aabFilePath);
        }
        
        /// <summary>
        /// Run an app bundle at the specified path, overwriting an existing file if one exists.
        /// </summary>
        /// <returns>True if the build succeeded, false if it failed or was cancelled.</returns>
        public static bool Run(string aabFilePath)
        {
            EditorUtility.ClearProgressBar();
            DisplayProgress("Extracting apks from bundle.", 0.1f);
            var aabFolder = new FileInfo(aabFilePath).Directory.FullName;
            var unpackedPath = Path.Combine(aabFolder, "unpacked");

            if (Directory.Exists(unpackedPath))
            {
                Directory.Delete(unpackedPath, true);
            }
            Directory.CreateDirectory(unpackedPath);
            
            var unpackedFile = Path.Combine(unpackedPath, "unpacked.apks");
            var buildError = Bundletool.BuildApksFromBundle(aabFilePath, unpackedFile);
            var zipError = ZipUtils.UnzipFile(unpackedPath, unpackedPath);
            EditorUtility.ClearProgressBar();
            if(buildError != null) DisplayBuildError("run failed", buildError);
            return buildError != null;
        }

        private static void DisplayProgress(string info, float progress)
        {
            Debug.LogFormat("{0}...", info);
            if (!WindowUtils.IsHeadlessMode())
            {
                EditorUtility.DisplayProgressBar("Running App Bundle", info, progress);
            }
        }

        private static void DisplayBuildError(string errorType, string errorMessage)
        {
            if (!WindowUtils.IsHeadlessMode())
            {
                EditorUtility.ClearProgressBar();
            }

            PlayInstantBuilder.DisplayBuildError(string.Format("{0} failed: {1}", errorType, errorMessage));
        }
    }
}