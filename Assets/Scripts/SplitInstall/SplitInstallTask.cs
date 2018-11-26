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
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace PostSplitLoading.SplitInstall
{    
    public class SplitInstallTask
    {
        private float _downloadProgress;
        private bool _downloadIsDone;
        private SplitInstallSessionState _state;
        private const int INSTALLED = 5;

        public SplitInstallTask(SplitInstallStateUpdatedListener listener)
        {
            listener.OnStateUpdateEvent += UpdateProgress;
        }

        private void UpdateProgress(SplitInstallSessionState state)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("SplitInstall Error: " + state.ErrorCode);
            builder.AppendLine("SplitInstall bytes downloaded: " + state.BytesDownloaded);
            builder.AppendLine("SplitInstall total downloaded: " + state.TotalBytesToDownload);
            builder.AppendLine("SplitInstall status: " + state.Status);
            Debug.Log(builder.ToString());
            _downloadProgress = state.BytesDownloaded / (float) state.TotalBytesToDownload;
            _downloadIsDone = state.Status == INSTALLED;
            _state = state;
        }

        public SplitInstallSessionState GetState()
        {
            return _state;
        }
        
        public bool IsDone()
        {
            return _downloadIsDone;
        }

        public float GetProgress()
        {
            return _downloadProgress;
        }
    }
}