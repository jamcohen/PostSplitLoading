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

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PostSplitLoading.SplitInstall
{
    public class SplitInstaller : MonoBehaviour
    {
        public Text DebugDisplay;
        
        public void ButtonDownloadSplit()
        {
            StartCoroutine(CoDownloadSplit());
        }

        private IEnumerator CoDownloadSplit()
        {
            var manager = new SplitInstallManager();
            var listener = manager.StartModuleInstall("feature");
            var task = new SplitInstallTask(listener);

            while (!task.IsDone())
            {
                DebugDisplay.text = string.Format("{0}% downloaded", task.GetProgress());
                yield return null;
            }
            
            
        }
    }
}
