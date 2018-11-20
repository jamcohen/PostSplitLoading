// <copyright file="PlayServicesResolver.cs" company="Google Inc.">
// Copyright (C) 2015 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions andf
//    limitations under the License.
// </copyright>

using UnityEngine;

namespace PostSplitLoading.SplitInstall
{
    public class SplitInstallManager
    {
        private AndroidJavaObject _splitInstallManager;
        private SplitInstallStateUpdatedListener _listener;

        public int GetNumberOfInstalledModules()
        {
            var splitInstallManager = GetSplitInstallManager();
            var installedModuleNames =
                splitInstallManager.Call<AndroidJavaObject>("getInstalledModules"); // returns Set<string>
            int numberOfModules = installedModuleNames.Call<int>("size");
            return numberOfModules;
        }

        public SplitInstallStateUpdatedListener StartModuleInstall(params string[] moduleNames)
        {
            var request = BuildSplitInstallRequest(moduleNames);
            var splitInstallManager = GetSplitInstallManager();

            _listener = new SplitInstallStateUpdatedListener();
            splitInstallManager.Call("registerListener", _listener);
            splitInstallManager.Call<AndroidJavaObject>("startInstall",
                request); // TODO: Manage the task this method returns.

            return _listener;
        }

        // Returns a SplitInstallRequest.
        public AndroidJavaObject BuildSplitInstallRequest(params string[] moduleNames)
        {
            var splitInstallRequestClass =
                new AndroidJavaClass("com.google.android.play.core.splitinstall.SplitInstallRequest");
            var splitSplitInstallRequestBuilder = splitInstallRequestClass.CallStatic<AndroidJavaObject>("newBuilder");
            foreach (var moduleName in moduleNames)
            {
                splitSplitInstallRequestBuilder =
                    splitSplitInstallRequestBuilder.Call<AndroidJavaObject>("addModule", moduleName);
            }

            var splitInstallRequest = splitSplitInstallRequestBuilder.Call<AndroidJavaObject>("build");
            return splitInstallRequest;
        }

        private AndroidJavaObject GetSplitInstallManager()
        {
            if (_splitInstallManager != null)
            {
                return _splitInstallManager;
            }

            string factoryClassName = "com.google.android.play.core.splitinstall.SplitInstallManagerFactory";
            var activity = UnityPlayerHelper.GetCurrentActivity();
            using (var splitInstallManagerFactory = new AndroidJavaClass(factoryClassName))
            {
                _splitInstallManager = splitInstallManagerFactory.CallStatic<AndroidJavaObject>("create", activity);
                return _splitInstallManager;
            }
        }

        public static void UpdateAppInfo(AndroidJavaObject context)
        {
            using (var splitHelper =
                new AndroidJavaClass("com.google.android.play.core.splitinstall.SplitInstallHelper"))
            {
                splitHelper.CallStatic("updateAppInfo", context);
            }
        }
    }
}