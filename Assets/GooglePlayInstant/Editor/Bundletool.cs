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
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEngine;
using UnityEditor;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides methods for <a href="https://developer.android.com/studio/command-line/bundletool">bundletool</a>.
    /// </summary>
    public static class Bundletool
    {
        public const string BundletoolVersion = "0.6.1";

        /// <summary>
        /// BundleTool config optimized for Unity-based instant apps.
        /// Config definition: https://github.com/google/bundletool/blob/master/src/main/proto/config.proto
        /// SplitsConfig:
        ///  - Split on ABI so only one set of native libraries (armeabi-v7a, arm64-v8a, or x86) is sent to a device.
        ///  - Do not split on LANGUAGE since Unity games don't store localized strings in the typical Android manner.
        ///  - Do not split on SCREEN_DENSITY since Unity games don't have per-density resources other than app icons.
        /// UncompressNativeLibraries: Instant apps have smaller over-the-wire and on-disk size with this enabled.
        /// TODO: Consider fully uncompressed, i.e. ""compression"": { ""uncompressedGlob"": [""**/*""] }
        /// </summary>
        private const string BundleConfigJsonText = @"
{
  ""optimizations"": {
        ""splitsConfig"": {
            ""splitDimension"": [
                {
                    ""value"": ""ABI"",
                    ""negate"": true
                },
                {
                    ""value"": ""LANGUAGE"",
                    ""negate"": true
                },
                {
                    ""value"": ""SCREEN_DENSITY"",
                    ""negate"": true
                }
            ]
        },
        ""uncompressNativeLibraries"": { ""enabled"": false }
    }
}";

        /// <summary>
        /// Returns the path to the bundletool jar within the project's Library directory.
        /// </summary>
        public static string GetBundletoolJarPath()
        {
            var library = Directory.CreateDirectory("Library");
            return Path.Combine(library.FullName, string.Format("bundletool-all-{0}.jar", BundletoolVersion));
        }

        /// <summary>
        /// Returns true if the expected version of bundletool is already located in the expected location,
        /// and false if the file doesn't exist, in which case a dialog will be shown prompting to download it.
        /// </summary>
        public static bool CheckBundletool()
        {
            var bundletoolJarPath = GetBundletoolJarPath();
            if (File.Exists(bundletoolJarPath))
            {
                return true;
            }

            Debug.LogWarningFormat("Failed to locate bundletool: {0}", bundletoolJarPath);
            BundletoolDownloadWindow.ShowWindow();
            return false;
        }

        /// <summary>
        /// Builds an Android App Bundle at the specified location containing the specified base module.
        /// </summary>
        /// <returns>An error message if there was a problem running bundletool, or null if successful.</returns>
        public static string BuildBundle(string[] modules, string outputFile)
        {
            var bundleConfigJsonFile = Path.Combine(Path.GetTempPath(), "BundleConfig.json");
            File.WriteAllText(bundleConfigJsonFile, BundleConfigJsonText);

            // TODO: quote path on modules
            var arguments = string.Format(
                "-jar {0} build-bundle --config={1} --modules={2} --output={3}",
                CommandLine.QuotePath(GetBundletoolJarPath()),
                CommandLine.QuotePath(bundleConfigJsonFile),
                string.Join(",", modules),
                CommandLine.QuotePath(outputFile));
            var result = CommandLine.Run(JavaUtilities.JavaBinaryPath, arguments);
            return result.exitCode == 0 ? null : result.message;
        }

        /// <summary>
        /// Builds a .apks file containing all the various splits produced from the bundle.
        /// </summary>
        /// <param name="aabFile">Path to the app bundle file</param>
        /// <param name="outputFile">Path of the .apks file</param>
        /// <returns></returns>
        public static string BuildApksFromBundle(string aabFile, string outputFile)
        {
            var keystoreName = PlayerSettings.Android.keystoreName;
            var keystorePass = PlayerSettings.Android.keystorePass;
            var keyaliasName = PlayerSettings.Android.keyaliasName;
            var keyaliasPass = PlayerSettings.Android.keyaliasPass;

            var arguments = string.Format(
                "-jar {0} build-apks --ks={1} --key-pass=pass:{2} --ks-key-alias={3} --ks-pass=pass:{4} --bundle={5} --output={6}",
                CommandLine.QuotePath(GetBundletoolJarPath()),
                CommandLine.QuotePath(keystoreName),
                keystorePass,
                CommandLine.QuotePath(keyaliasName),
                keyaliasPass,
                CommandLine.QuotePath(aabFile),
                CommandLine.QuotePath(outputFile));
            var result = CommandLine.Run(JavaUtilities.JavaBinaryPath, arguments);
            return result.exitCode == 0 ? null : result.message;
        }
    }
}