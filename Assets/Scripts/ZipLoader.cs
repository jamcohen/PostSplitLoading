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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PostSplitLoading
{
    public class ZipLoader
    {
        private string _zipFilePath; // Path to the zip file.
        private BinaryReader _zipStream;
        private Dictionary<string, FileInfo> _fileInfoByPath;

        private const int StartOfCentralDirectory = 0x02014b50;
        private const int EndOfCentralDirectory = 0x06054b50;

        private struct FileInfo
        {
            public int Size;
            public int Offset;
        }

        public ZipLoader(string zipPath)
        {
            const long largestCentralDirectorySize = 64 * 1024;
            _zipStream = new BinaryReader(File.OpenRead(zipPath));
            _zipFilePath = zipPath;

            bool foundCentralDirectory = SeekToMatch(_zipStream, StartOfCentralDirectory);
            if (!foundCentralDirectory)
            {
                Debug.LogErrorFormat("Could not find central directory in zip file: {0}", zipPath);
                return;
            }

            _fileInfoByPath = new Dictionary<string, FileInfo>();
            ExtractFileLocations();
        }

        public AssetBundleCreateRequest LoadAssetBundleAsync(string fileName)
        {
            FileInfo fileInfo;
            if (!_fileInfoByPath.TryGetValue(fileName, out fileInfo))
            {
                Debug.LogErrorFormat("Cannot find AssetBundle {0} in zip file", fileName);
                return null;
            }

            Debug.LogFormat("Loading with offset: {0} and size: {1}", fileInfo.Offset, fileInfo.Size);
            return AssetBundle.LoadFromFileAsync(_zipFilePath, 0, (ulong) fileInfo.Offset);
        }

        public byte[] LoadFile(string fileName)
        {
            FileInfo fileInfo;
            if (!_fileInfoByPath.TryGetValue(fileName, out fileInfo))
            {
                Debug.LogErrorFormat("Cannot find AssetBundle {0} in zip file", fileName);
                return null;
            }

            Debug.LogFormat("Loading with offset: {0} and size: {1}", fileInfo.Offset, fileInfo.Size);
            _zipStream.BaseStream.Seek(fileInfo.Offset, SeekOrigin.Begin);
            return _zipStream.ReadBytes(fileInfo.Size);
        }

        private void ExtractFileLocations()
        {
            while (_zipStream.ReadInt32() != EndOfCentralDirectory)
            {
                ExtractFileLocation();
            }
        }

        /// <summary>
        /// Extracts the file location and path from the central directory.
        /// Caches it in _offsetsByFilePath.
        /// </summary>
        private void ExtractFileLocation()
        {
            var fileInfo = new FileInfo();

            _zipStream.BaseStream.Seek(16, SeekOrigin.Current); // Skip to compressed size.

            // Read compressed size.
            fileInfo.Size = _zipStream.ReadInt32();
            var uncompressedSize = _zipStream.ReadInt32();
            if (fileInfo.Size != uncompressedSize)
            {
                Debug.LogErrorFormat("File in zip {0} is not stored as uncompressed.", _zipFilePath);
                return;
            }

            var fileNameLength = _zipStream.ReadInt16();
            var extraFieldLength = _zipStream.ReadInt16();
            var commentFieldLength = _zipStream.ReadInt16();

            _zipStream.BaseStream.Seek(8, SeekOrigin.Current); // Skip to file offset.
            fileInfo.Offset = _zipStream.ReadInt32();

            // Add the size of the file's local header to get the starting location of the file.
            fileInfo.Offset += 34 +
                               fileNameLength +
                               extraFieldLength +
                               commentFieldLength;

            var fileName = Encoding.UTF8.GetString(_zipStream.ReadBytes(fileNameLength));

            _fileInfoByPath.Add(fileName, fileInfo);

            _zipStream.BaseStream.Seek(extraFieldLength + commentFieldLength, SeekOrigin.Current);
            Debug.LogFormat(
                "Found file:{0}, finished at: {1}, offsetToHeader: {2}, size: {3}",
                fileName, _zipStream.BaseStream.Position, fileInfo.Offset, fileInfo.Size);
        }

        private static bool SeekToMatch(BinaryReader zip, int search)
        {
            while (zip.BaseStream.Position < zip.BaseStream.Length)
            {
                if (zip.ReadInt32() == search)
                {
                    zip.BaseStream.Seek(-4, SeekOrigin.Current);
                    return true;
                }
            }

            return false;
        }
    }
}