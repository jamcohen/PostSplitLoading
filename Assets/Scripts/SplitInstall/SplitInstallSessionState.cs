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
//  See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace PostSplitLoading.SplitInstall
{
    public class SplitInstallSessionState
    {
        private AndroidJavaObject _sessionState;

        public long BytesDownloaded
        {
            get { return _sessionState.Call<long>("bytesDownloaded"); }
        }

        public int ErrorCode
        {
            get { return _sessionState.Call<int>("errorCode"); }
        }

        public List<string> ModuleNames
        {
            get
            {
                var javaList = _sessionState.Call<AndroidJavaObject>("bytesDownloaded");
                int count = javaList.Call<int>("size");

                var results = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    results.Add(javaList.Call<string>("get", i));
                }

                return results;
            }
        }

        // Returns a PendingIntent object.
        public AndroidJavaObject ResolutionIntent
        {
            get { return _sessionState.Call<AndroidJavaObject>("resolutionIntent"); }
        }


        public int SessionId
        {
            get { return _sessionState.Call<int>("sessionId"); }
        }


        public int Status
        {
            get { return _sessionState.Call<int>("status"); }
        }

        public long TotalBytesToDownload
        {
            get { return _sessionState.Call<long>("totalBytesToDownload"); }
        }

        public SplitInstallSessionState(AndroidJavaObject sessionState)
        {
            _sessionState = sessionState;
        }
    }
}