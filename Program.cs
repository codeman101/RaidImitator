using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
//--------------------------------------------------------------------------------------------------
// Name:
// main
// 
// Arguments:
// None: Program doesn't use the C# args parameter explicitly
//
// 
//
// About: The function reads from a config text file to get the directory needs to read from and 
// the directory it needs to write to acting as a service making a replica of the directory tree it's reading
// from the given level down
// 
//
// Name: OnChanged
//
// Arguments:
// FileSystemEventArgs e is the only one that's used to get the path of the changed file or directory.
//
//
// About: Function triggers when a change is detected. If the change is due to a file the file is copied overwritting the
// old file. If the change is due to a directory the directory created in the destination location.
//
//
// Name: OnRenamed
//
// Arguments:
// FileSystemEventArgs e is the only one that's used to get the path of the old and new file or directory.
//
//
//
// About: Deletes old file or directory in destination location and copies new one to destination location. Deletes old directories recursively.
// If directory was renamed it calls a function called setdirs that copies the given directory recurvisely.
//
//
//  
// Name: OnDeleteed
//
// Arguments:
// FileSystemEventArgs e is the only one that's used to get the path of the deleted file or directory.
//
//
//
// About: Deletes file or directory in destination location. If directory then it's deleted recursively.
//
//  
// Name: OnCreated
//
// Arguments:
// FileSystemEventArgs e is the only one that's used to get the path of the deleted file or directory.
//
//
//
// About: Creates file or directiory in destination location.
//
// 
//--------------------------------------------------------------------------------------------------


namespace RaidImitator
{
    class Program
    {
        static string src;
        static string dest;
        static void Main(string[] args)
        {
            string configFile = "%USERPROFILE%\\Documents\\RaidImitatorConfig.txt";
            configFile = Environment.ExpandEnvironmentVariables(configFile);
            string[] srcDest = File.ReadAllLines(configFile);
            src = srcDest[0];
            dest = srcDest[1];

            FileSystemWatcher watcher = new FileSystemWatcher(); // watcher
            watcher.IncludeSubdirectories = true;
            watcher.InternalBufferSize = 4000;
            watcher.Path = src; // assigns path to be watched
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime 
                | NotifyFilters.FileName | NotifyFilters.DirectoryName; // fields to be monitored
            watcher.Changed += new FileSystemEventHandler(OnChanged); // OnChanged functon called if changed
            watcher.Created += new FileSystemEventHandler(OnCreated); // OnChanged functon called if created
            watcher.Deleted += new FileSystemEventHandler(OnDeleted); // OnDeleted function called if deleted
            watcher.Renamed += new RenamedEventHandler(OnRenamed); // OnRenamed function called if renamed

            // Begin watching.
            watcher.EnableRaisingEvents = true;
            Thread.Sleep(Timeout.Infinite); // keeps the program going
        }
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            string fileName = e.FullPath.Substring(src.Length); // extract part of the part that needs to be checked against the dest
            string pathCheck = dest + fileName;
            if (File.Exists(pathCheck) || Directory.Exists(pathCheck)) // if file is in dest
            {
                DateTime dtDest = File.GetLastWriteTime(pathCheck);
                DateTime dtSrc = File.GetLastWriteTime(e.FullPath);
                if (dtDest.Equals(dtSrc)) // no changes were made to the src file so no need to do anything
                    return;
                else
                {
                    FileAttributes attr = File.GetAttributes(e.FullPath);
                    if (!attr.HasFlag(FileAttributes.Directory))
                    {
                        File.Copy(e.FullPath, pathCheck, true); // copy file 'true' means copy even though they have same name
                    }
                    
                }
                    
            }
            else
            {
                FileAttributes attr = File.GetAttributes(e.FullPath);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    Directory.CreateDirectory(e.FullPath);
                }
                else
                {
                    File.Copy(e.FullPath, pathCheck); // file didn't even exist in dest
                }
            }
                
            
            

        }
        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            string oldFile = e.OldFullPath.Substring(src.Length);
            oldFile = dest + oldFile;
            string newFile = e.FullPath.Substring(src.Length);
            newFile = dest + newFile;
            FileAttributes attr = File.GetAttributes(oldFile);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                Directory.Delete(oldFile, true); // delete directory with old name
                Directory.CreateDirectory(newFile);
                string[] subdirs = Directory.GetDirectories(e.FullPath);
                if (subdirs.Length > 0)
                {
                    setDirs(newFile, e.FullPath); // function to copy all contents of current directory including sub-directories
                }
            }
            else
            {
                File.Delete(oldFile); // delete old copy of file
                File.Copy(e.FullPath, newFile); // make new copy
            }
        }
        private static void setDirs(string newFile, string subdirs)
        {
            try
            {
                foreach (string dir in Directory.GetDirectories(subdirs))
                {
                    string tempPath = dir.Substring(newFile.Length);
                    tempPath = newFile + "\\" + tempPath; // 
                    Directory.CreateDirectory(tempPath);
                    setDirs(newFile, dir); // recursive call to copy all subdirectories
                    
                }
                foreach (string file in Directory.GetFiles(subdirs))
                {
                    string tempPath = file.Substring(newFile.Length);
                    tempPath = newFile + "\\" + tempPath;
                    File.Copy(file, tempPath);
                }
            }catch (Exception erro)
            {
                // Expection to handle recursive call
            }
        }
        private static void OnDeleted(object source, FileSystemEventArgs e)
        {
            string fileName = e.FullPath.Substring(src.Length);
            fileName = dest + fileName;
            try
            {
                Directory.Delete(fileName, true);
            }catch(Exception erro)
            {
                File.Delete(fileName);
            }
            
        }
        private static void OnCreated(object source, FileSystemEventArgs e)
        {

            string fileName = e.FullPath.Substring(src.Length);
            fileName = dest + fileName;
            FileAttributes attr = File.GetAttributes(e.FullPath);
            if (attr.HasFlag(FileAttributes.Directory))
                Directory.CreateDirectory(fileName);
            else
            {
                var file = File.Create(fileName);
                file.Close(); // had to assign File.Create to a var because otherwise thread kept file open
            }
        }
    }
}
