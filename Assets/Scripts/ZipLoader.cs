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

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PostSplitLoading
{
    public class ZipLoader
    {
        private string _zipFilePath; // Path to the zip file.
        private Dictionary<string, FileInfo> _fileInfoByPath;

        private const int StartOfCentralDirectory = 0x02014b50;
        private const int EndOfCentralDirectory = 0x06054b50;
        private const int MaxCommentSize = (1 << 16) - 1;
        private const int EndOfCentralDirectoryRecordSize = 22;

        private struct FileInfo
        {
            public int Size;
            public int Offset;
        }

        public ZipLoader(string zipPath)
        {
            using (var zipStream = new BinaryReader(File.OpenRead(zipPath)))
            {
                _zipFilePath = zipPath;

                bool foundCentralDirectory = SeekToCentralDirectory(zipStream);
                if (!foundCentralDirectory)
                {
                    Debug.LogErrorFormat("Could not find central directory in zip file: {0}", zipPath);
                    return;
                }

                _fileInfoByPath = new Dictionary<string, FileInfo>();
                ExtractFileLocations(zipStream);
            }
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
            using (var zipStream = new BinaryReader(File.OpenRead(_zipFilePath)))
            {
                FileInfo fileInfo;
                if (!_fileInfoByPath.TryGetValue(fileName, out fileInfo))
                {
                    Debug.LogErrorFormat("Cannot find AssetBundle {0} in zip file", fileName);
                    return null;
                }

                Debug.LogFormat("Loading with offset: {0} and size: {1}", fileInfo.Offset, fileInfo.Size);
                zipStream.BaseStream.Seek(fileInfo.Offset, SeekOrigin.Begin);
                return zipStream.ReadBytes(fileInfo.Size);
            }
        }

        /// <summary>
        /// Extracts the file locations from the central directory.
        /// Assumes that zipStream is positioned at the start of the central directory.
        /// </summary>
        private void ExtractFileLocations(BinaryReader zipStream)
        {
            while (zipStream.ReadInt32() != EndOfCentralDirectory)
            {
                ExtractFileLocation(zipStream);
            }
        }

        /// <summary>
        /// Extracts the file location and path from the central directory.
        /// Caches it in _fileInfoByPath.
        /// Assumes that zipStream is positioned at the start of a central directory file header.
        /// Finishes with zipStream positioned at either the start of the next central directory file header,
        /// or the end of the central directory.
        /// </summary>
        private void ExtractFileLocation(BinaryReader zipStream)
        {
            var fileInfo = new FileInfo();

            zipStream.BaseStream.Seek(16, SeekOrigin.Current); // Skip to compressed size.

            // Read compressed size.
            fileInfo.Size = zipStream.ReadInt32();
            var uncompressedSize = zipStream.ReadInt32();
            if (fileInfo.Size != uncompressedSize)
            {
                Debug.LogErrorFormat("File in zip {0} is not stored as uncompressed.", _zipFilePath);
                return;
            }

            var fileNameLength = zipStream.ReadInt16();
            var extraFieldLength = zipStream.ReadInt16();
            var commentFieldLength = zipStream.ReadInt16();

            zipStream.BaseStream.Seek(8, SeekOrigin.Current); // Skip to file offset.
            fileInfo.Offset = zipStream.ReadInt32();

            // Add the size of the file's local header to get the starting location of the file.
            fileInfo.Offset += 34 +
                               fileNameLength +
                               extraFieldLength +
                               commentFieldLength;

            var fileName = Encoding.UTF8.GetString(zipStream.ReadBytes(fileNameLength));

            _fileInfoByPath.Add(fileName, fileInfo);

            zipStream.BaseStream.Seek(extraFieldLength + commentFieldLength, SeekOrigin.Current);
            Debug.LogFormat(
                "Found file:{0}, finished at: {1}, offsetToHeader: {2}, size: {3}",
                fileName, zipStream.BaseStream.Position, fileInfo.Offset, fileInfo.Size);
        }

        /// <summary>
        /// Extracts the central directory location and seeks zip to it.
        /// </summary>
        /// <returns>True if the central directory is found. False otherwise.</returns>
        private static bool SeekToCentralDirectory(BinaryReader zip)
        {
            // Find the end of central directory record, which contains the location of the central directory.
            // Start by assuming the central directory has no comment.
            zip.BaseStream.Seek(-EndOfCentralDirectoryRecordSize, SeekOrigin.End);
            if (SeekToMatch(zip, EndOfCentralDirectory))
            {
                if (SeekFromCdEndToCdStart(zip))
                {
                    return true;
                }
            }

            // If we haven't found it yet, assume there is a comment, and seek backwards by the maximum comment length.
            zip.BaseStream.Seek(-EndOfCentralDirectoryRecordSize - MaxCommentSize, SeekOrigin.End);
            if (SeekToMatch(zip, EndOfCentralDirectory))
            {
                if (SeekFromCdEndToCdStart(zip))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find the location of the central directory.
        /// Assumes that zip is pointing to the start of the "end of central directory record" (EOCD)
        /// </summary>
        /// <returns>
        /// True if the offset contained in the EOCD contains the "start of central directory" signature.
        /// False otherwise.
        /// </returns>
        private static bool SeekFromCdEndToCdStart(BinaryReader zip)
        {
            // Grab central directory offset which is 16 bytes from the start of the EOCD
            zip.BaseStream.Seek(16, SeekOrigin.Current);
            var offsetToCentralDirectory = zip.ReadInt32();
            zip.BaseStream.Seek(offsetToCentralDirectory, SeekOrigin.Begin);

            if (zip.ReadInt32() == StartOfCentralDirectory)
            {
                zip.BaseStream.Seek(-4, SeekOrigin.Current);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Scans the stream, starting from zip's current position until it finds "matchToken".
        /// </summary>
        /// <param name="matchToken">The int to search for.</param>
        /// <returns></returns>
        private static bool SeekToMatch(BinaryReader zip, int matchToken)
        {
            while (zip.BaseStream.Position < zip.BaseStream.Length)
            {
                if (zip.ReadInt32() == matchToken)
                {
                    zip.BaseStream.Seek(-4, SeekOrigin.Current);
                    return true;
                }
            }

            return false;
        }
    }
}