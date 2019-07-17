//
// FileWatcherInformation_V2.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.FSW;
using Roslyn.Utilities;

namespace MonoDevelop.Core
{
	internal sealed class SingleFileWatcher : IDisposable
	{
		public FilePath Path { get; }
		Action<FilePath> action;
		IdeWatcherInformation info;

		internal SingleFileWatcher (FilePath path, Action<FilePath> action, IdeWatcherInformation info)
		{
			Path = path;
			this.action = action;
			this.info = info;

			info.AddFileWatcher (this);
		}

		public void Notify (FilePath path) => action.Invoke (path);

		public void Dispose ()
		{
			if (info != null) {
				action = _ => {};
				info.RemoveFileWatcher (this);
				info = null;
			}
		}
	}

	public sealed class ProjectFilesWatcher : IDisposable
	{
		HashSet<FilePath> files = new HashSet<FilePath> ();
		Dictionary<FilePath, DirectoryGlobInfo> directories = new Dictionary<FilePath, DirectoryGlobInfo> ();
		IdeWatcherInformation information;

		internal ProjectFilesWatcher (IdeWatcherInformation information)
		{
			this.information = information;
		}

		public void AddFile (FilePath file)
		{
			files.Add (file);
		}

		public void RemoveFile (FilePath file)
		{
			files.Remove (file);
		}

		public void AddDirectory (FilePath directory, string globInclude = null, string globExclude = null)
		{
			directories.Add (directory, new DirectoryGlobInfo ());
		}

		public void RemoveDirectory (FilePath directory)
		{
			directories.Remove (directory);
		}

		public void Dispose ()
		{
			if (information == null)
				return;

			information = null;
			files = null;
			directories = null;
		}

		class DirectoryGlobInfo
		{
			public string[] Includes;
			public string[] Excludes;
		}
	}

	sealed class IdeWatcherInformation
	{
		readonly Dictionary<FilePath, List<SingleFileWatcher>> fileMap = new Dictionary<FilePath, List<SingleFileWatcher>> ();
		readonly ReaderWriterLockSlim fileLock = new ReaderWriterLockSlim (LockRecursionPolicy.NoRecursion);

		readonly PathTree directoryTree = new PathTree ();
		readonly ReaderWriterLockSlim directoryLock = new ReaderWriterLockSlim (LockRecursionPolicy.NoRecursion);

		public void AddFileWatcher (SingleFileWatcher watcher)
		{
			fileLock.EnterReadLock ();
			try {
				if (!fileMap.TryGetValue (watcher.Path, out var watchers)) {
					watchers = fileMap [watcher.Path] = new List<SingleFileWatcher> ();
				}

				watchers.Add (watcher);
			} finally {
				fileLock.ExitReadLock ();
			}

			// Update directory info, compute whether we need to start watchers.
		}

		public void RemoveFileWatcher (SingleFileWatcher watcher)
		{
			fileLock.EnterReadLock ();
			try {
				if (!fileMap.TryGetValue (watcher.Path, out var watchers) || !watchers.Remove (watcher))
					return;
			} finally {
				fileLock.ExitReadLock ();
			}

			// Update directory info, compute whether we need to stop watchers.
		}

		public void AddProjectWatcher (ProjectFilesWatcher watcher)
		{

		}
	}
}
