#region Copyright (C) 2005-2006 Team MediaPortal

/* 
 *	Copyright (C) 2005-2006 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using MediaPortal.Services;
using SQLite.NET;
using MediaPortal.GUI.Library;
using MediaPortal.TagReader;
using MediaPortal.Database;
using MediaPortal.Music.Database;
using MediaPortal.Util;


namespace MediaPortal.MusicShareWatcher
{
  public class MusicShareWatcherHelper
  {
    #region Variables
    private bool bMonitoring;
    static public MusicDatabase musicDB = null;
    ArrayList m_Shares = new ArrayList();
    ArrayList m_Watchers = new ArrayList();
    static Thread m_CheckForVariousArtistsThread;

    // Lock order is _enterThread, _events.SyncRoot
    private object m_EnterThread = new object(); // Only one timer event is processed at any given moment
    private ArrayList m_Events = null;

    private System.Timers.Timer m_Timer = null;
    private int m_TimerInterval = 2000; // milliseconds
    #endregion

    #region Constructors/Destructors
    public MusicShareWatcherHelper()
    {
      // Create Log File
      Log.BackupLogFile(LogType.MusicShareWatcher);

      musicDB = new MusicDatabase();
      m_CheckForVariousArtistsThread = new Thread(new ThreadStart(CheckForVariousArtists));
      m_CheckForVariousArtistsThread.Name = "Check for Various Artists";
    }
    #endregion

    #region Main
    public void StartMonitor()
    {
      if (bMonitoring)
        WatchShares();
    }

    public void SetMonitoring(bool status)
    {
      if (status)
      {
        bMonitoring = true;
      }
      else
      {
        bMonitoring = false;
      }
    }

    public void ChangeMonitoring(bool status)
    {
      if (status)
      {
        bMonitoring = true;
        foreach (DelayedFileSystemWatcher watcher in m_Watchers)
        {
          watcher.EnableRaisingEvents = true;
        }
        m_Timer.Start();
        Log.Info(LogType.MusicShareWatcher, "Monitoring of shares enabled");
      }
      else
      {
        bMonitoring = false;
        foreach (DelayedFileSystemWatcher watcher in m_Watchers)
        {
          watcher.EnableRaisingEvents = false;
        }
        m_Timer.Stop();
        m_Events.Clear();
        Log.Info(LogType.MusicShareWatcher, "Monitoring of shares disabled");
      }
    }

    private void WatchShares()
    {
      Log.Info(LogType.MusicShareWatcher, "MusicShareWatcher starting up!");
      LoadShares();
      Log.Info(LogType.MusicShareWatcher, "Monitoring active for following shares:");
      Log.Info(LogType.MusicShareWatcher, "---------------------------------------");
      foreach (String sharename in m_Shares)
      {
        try
        {
          m_Events = ArrayList.Synchronized(new ArrayList(64));
          // Create the watchers. 
          //I need 2 type of watchers. 1 for files and the other for directories
          // Reason is that i don't know if the event occured on a file or directory.
          // For a Create / Change / Rename i could figure that out using FileInfo or DirectoryInfo,
          // but when something gets deleted, i don't know if it is a File or directory
          DelayedFileSystemWatcher watcherFile = new DelayedFileSystemWatcher();
          DelayedFileSystemWatcher watcherDirectory = new DelayedFileSystemWatcher();
          watcherFile.Path = sharename;
          watcherDirectory.Path = sharename;
          /* Watch for changes in LastWrite times, and the renaming of files or directories. */
          watcherFile.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Attributes;
          watcherDirectory.NotifyFilter = NotifyFilters.DirectoryName;
          // Monitor all Files and subdirectories.
          watcherFile.Filter = "*.*";
          watcherFile.IncludeSubdirectories = true;
          watcherDirectory.Filter = "*.*";
          watcherDirectory.IncludeSubdirectories = true;

          // Add event handlers.
          watcherFile.Changed += new FileSystemEventHandler(OnChanged);
          watcherFile.Created += new FileSystemEventHandler(OnCreated);
          watcherFile.Deleted += new FileSystemEventHandler(OnDeleted);
          watcherFile.Renamed += new RenamedEventHandler(OnRenamed);
          // For directories, i'm only interested in a Delete event
          watcherDirectory.Deleted += new FileSystemEventHandler(OnDirectoryDeleted);

          // Begin watching.
          watcherFile.EnableRaisingEvents = true;
          watcherDirectory.EnableRaisingEvents = true;
          m_Watchers.Add(watcherFile);
          m_Watchers.Add(watcherDirectory);

          // Start Timer for processing events
          m_Timer = new System.Timers.Timer(m_TimerInterval);
          m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(ProcessEvents);
          m_Timer.AutoReset = true;
          m_Timer.Enabled = watcherFile.EnableRaisingEvents;
          Log.Info(LogType.MusicShareWatcher, sharename);
        }
        catch (System.ArgumentException ex)
        {
          Log.Info(LogType.MusicShareWatcher, "Unable to turn on monitoring for: {0} Exception: {1}", sharename ,ex.Message);
        }
      }
      Log.Info(LogType.MusicShareWatcher, "---------------------------------------");
      Log.Info(LogType.MusicShareWatcher, "Note: Errors reported for CD/DVD drives can be ignored.");
    }
    #endregion Main

    #region EventHandlers
    // Event handler for Create of a file
    private void OnCreated(object source, FileSystemEventArgs e)
    {
      Log.Debug(LogType.MusicShareWatcher, "Add Song Fired: {0}", e.FullPath);
      m_Events.Add(new MusicShareWatcherEvent(MusicShareWatcherEvent.EventType.Create, e.FullPath));
    }

    /// <summary>
    /// Scan the Album cache for multiple artists
    /// Scanning is however delayed, in order to make sure that complete albums may get copied and 
    /// not checked several times.
    /// </summary>
    private static void CheckForVariousArtists()
    {
      Thread.Sleep(20000);
      musicDB.UpdateAlbumArtistsCounts(0, 0);
    }

    // Event handler for Change of a file
    private void OnChanged(object source, FileSystemEventArgs e)
    {
      FileInfo fi = new FileInfo(e.FullPath);
      // A Change event occured.
      // Was it on a file? Ignore change events on directories
      if (fi.Exists)
      {
        Log.Debug(LogType.MusicShareWatcher, "Change Song Fired: {0}", e.FullPath);
        m_Events.Add(new MusicShareWatcherEvent(MusicShareWatcherEvent.EventType.Change, e.FullPath));
      }
    }

    // Event handler handling the Delete of a file
    private void OnDeleted(object source, FileSystemEventArgs e)
    {
      Log.Debug(LogType.MusicShareWatcher, "Delete Song Fired: {0}", e.FullPath);
      m_Events.Add(new MusicShareWatcherEvent(MusicShareWatcherEvent.EventType.Delete, e.FullPath));
    }

    // Event handler handling the Delete of a directory
    private void OnDirectoryDeleted(object source, FileSystemEventArgs e)
    {
      Log.Debug(LogType.MusicShareWatcher, "Delete Directory Fired: {0}", e.FullPath);
      m_Events.Add(new MusicShareWatcherEvent(MusicShareWatcherEvent.EventType.DeleteDirectory, e.FullPath));
    }

    // Event handler handling the Rename of a file/directory
    private void OnRenamed(object source, RenamedEventArgs e)
    {
      Log.Debug(LogType.MusicShareWatcher, "Rename File/Directory Fired: {0}", e.FullPath);
      m_Events.Add(new MusicShareWatcherEvent(MusicShareWatcherEvent.EventType.Rename, e.FullPath, e.OldFullPath));
    }
    #endregion EventHandlers

    #region Private Methods
    void ProcessEvents(object sender, System.Timers.ElapsedEventArgs e)
    {
      // Allow only one Timer event to be executed.
      if (System.Threading.Monitor.TryEnter(m_EnterThread))
      {
        // Only one thread at a time is processing the events                
        try
        {
          // Lock the Collection, while processing the Events
          lock (m_Events.SyncRoot)
          {
            MusicShareWatcherEvent currentEvent = null;
            for (int i = 0; i < m_Events.Count; i++)
            {
              currentEvent = m_Events[i] as MusicShareWatcherEvent;
              switch (currentEvent.Type)
              {
                case MusicShareWatcherEvent.EventType.Create:
                  AddNewSong(currentEvent.FileName);
                  break;
                case MusicShareWatcherEvent.EventType.Change:
                  UpdateSong(currentEvent.FileName);
                  break;
                case MusicShareWatcherEvent.EventType.Delete:
                  musicDB.DeleteSong(currentEvent.FileName, true);
                  Log.Info(LogType.MusicShareWatcher, "Deleted Song: {0}", currentEvent.FileName);
                  break;
                case MusicShareWatcherEvent.EventType.DeleteDirectory:
                  musicDB.DeleteSongDirectory(currentEvent.FileName);
                  Log.Info(LogType.MusicShareWatcher, "Deleted Directory: {0}", currentEvent.FileName);
                  break;
                case MusicShareWatcherEvent.EventType.Rename:
                  if (musicDB.RenameSong(currentEvent.OldFileName, currentEvent.FileName))
                    Log.Info(LogType.MusicShareWatcher, "Song / Directory {0} renamed to {1]", currentEvent.OldFileName, currentEvent.FileName);
                  else
                    Log.Info(LogType.MusicShareWatcher, "Song / Directory rename failed: {0}", currentEvent.FileName);
                  break;
              }
              m_Events.RemoveAt(i);
              i--; // Don't skip next event
            }
          }
        }
        finally
        {
          System.Threading.Monitor.Exit(m_EnterThread);
        }
      }

    }

    // Method used by OnCreated to fill the song structure
    private static void AddNewSong(string strFileName)
    {
      // Has the song already be added? 
      // This happens when a full directory is copied into the share.
      if (musicDB.SongExists(strFileName))
        return;
      // For some reason the Create is fired already by windows while the file is still copied.
      // This happens especially on large songs copied via WLAN.
      // The result is that MP Readtag is throwing an IO Exception.
      // I'm trying to open the file here and in case of an exception i'll put it on a thread to be
      // processed 5 seconds later again.
      try
      {
        FileInfo file = new FileInfo(strFileName);
        Stream s = null;
        s = file.OpenRead();
        s.Close();
      }
      catch (System.IO.IOException)
      {
        // The file is not closed yet. Ignore the event, it will be processed by the Change event
        return;
      }
      MusicTag tag = new MusicTag();
      tag = TagReader.TagReader.ReadTag(strFileName);
      if (tag != null)
      {
        // We got a valid file, so let's add it
        Song song = new Song();
        song.Title = tag.Title;
        song.Genre = tag.Genre;
        song.FileName = strFileName;
        song.Artist = tag.Artist;
        song.Album = tag.Album;
        song.Year = tag.Year;
        song.Track = tag.Track;
        song.Duration = tag.Duration;
        musicDB.AddSong(song, true);
        Log.Info(LogType.MusicShareWatcher, "Added Song: {0}", strFileName);
        /// Whenever a new song is added it might happen that multiple artists are there for the same album. 
        /// Scan all albums in cache
        if (m_CheckForVariousArtistsThread.ThreadState == ThreadState.Stopped || m_CheckForVariousArtistsThread.ThreadState == ThreadState.Unstarted)
        {
          try
          {
            m_CheckForVariousArtistsThread = new Thread(new ThreadStart(CheckForVariousArtists));
            m_CheckForVariousArtistsThread.Name = "Check for Various Artists";
            m_CheckForVariousArtistsThread.Start();
          }
          catch (ThreadStateException)
          { }
        }
      }
    }

    private static void UpdateSong(string strFilename)
    {
      Song song = new Song();
      if (musicDB.GetSongByFileName(strFilename, ref song))
      {
        if (musicDB.UpdateSong(strFilename, song.songId))
          Log.Info(LogType.MusicShareWatcher, "Updated Song: {0}", strFilename);
      }
      else
      {
        // The song was not found in the database, add it
        Log.Info(LogType.MusicShareWatcher, "Added Song: {0}", strFilename);
        AddNewSong(strFilename);
      }
    }
    #endregion

    #region Common Methods

    // Retrieve the Music Shares that should be monitored
    int LoadShares()
    {
      MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));
      string strDefault = xmlreader.GetValueAsString("music", "default", String.Empty);
      for (int i = 0; i < 20; i++)
      {
        string strShareName = String.Format("sharename{0}", i);
        string strSharePath = String.Format("sharepath{0}", i);
        string shareType = String.Format("sharetype{0}", i);

        string ShareType = xmlreader.GetValueAsString("music", shareType, String.Empty);
        if (ShareType == "yes") continue;
        string SharePath = xmlreader.GetValueAsString("music", strSharePath, String.Empty);

        if (SharePath.Length > 0)
          m_Shares.Add(SharePath);
      }

      xmlreader = null;
      return 0;
    }
    #endregion
  }
}
