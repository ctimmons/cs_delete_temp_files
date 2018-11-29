/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.IO;
using System.Linq;
using System.Management;

namespace DeleteTempFiles
{
  /* Conservatively delete all files and folders in the temp folder that were created before the last boot. */

  public class Program
  {
    private static readonly DateTime _bootDateTime = GetBootDateTime();

    public static void Main(String[] args)
    {
      var shouldLogErrors = args.Any(arg => arg.Trim().ToLower() == "/log");
      var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeleteTempFiles");
      var logFilename = Path.Combine(appDataFolder, $"{DateTime.Today.ToString("yyyy-MM-dd")} - Log.txt");

      if (shouldLogErrors)
      {
        Directory.CreateDirectory(appDataFolder);
        File.WriteAllText(logFilename, "");
      }

      Action<String, String, String> logErrorMessage =
        (type, name, message) => { if (shouldLogErrors) File.AppendAllText(logFilename, $"{type}: {name}: {message}\n"); };

      RecursivelyDeleteTempFilesAndFolders(new DirectoryInfo(Path.GetTempPath()), logErrorMessage);
    }

    private static void RecursivelyDeleteTempFilesAndFolders(DirectoryInfo di, Action<String, String, String> logErrorMessage)
    {
      try
      {
        foreach (var fsi in di.EnumerateFileSystemInfos())
        {
          if (fsi is DirectoryInfo)
            RecursivelyDeleteTempFilesAndFolders(fsi as DirectoryInfo, logErrorMessage);

          if (CanDelete(fsi))
          {
            try
            {
              fsi.Delete();
            }
            catch (Exception ex)
            {
              /* The file or folder can't be deleted.

                 Assume some other process is using this item.
                 Don't try to figure out why it can't be deleted or try to force
                 the deletion.  It's safest to just skip it. */
              logErrorMessage("File", fsi.FullName, ex.Message);
            }
          }
        }
      }
      catch (Exception ex)
      {
        /* The folder can't be enumerated. */
        logErrorMessage("Directory", di.FullName, ex.Message);
      }
    }

    private static Boolean CanDelete(FileSystemInfo fsi)
    {
      return
        (fsi.CreationTime   < _bootDateTime) &&
        (fsi.LastAccessTime < _bootDateTime) &&
        (fsi.LastWriteTime  < _bootDateTime) &&

        /* The LINQ Any() operator has "immediate execution" behavior.
        
           https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/classification-of-standard-query-operators-by-manner-of-execution#classification-table
        */

        ((fsi is DirectoryInfo) ? !(fsi as DirectoryInfo).EnumerateFileSystemInfos().Any() : true);
    }

    private static String GetWmiPropertyValueAsString(String queryString, String propertyName)
    {
      var query = new SelectQuery(queryString);
      using (var searcher = new ManagementObjectSearcher(query))
        using (var collection = searcher.Get())
          return collection.Cast<ManagementObject>().FirstOrDefault()?.Properties[propertyName].Value.ToString();
    }

    private static DateTime GetWmiPropertyValueAsDateTime(String queryString, String propertyName)
    {
      var value = GetWmiPropertyValueAsString(queryString, propertyName);
      return (value == null) ? DateTime.MinValue : ManagementDateTimeConverter.ToDateTime(value);
    }

    /* NB: WMI might return the wrong LastBootUpTime.

       Starting with version 8, Windows has a feature called "Fast Startup".
       When turned on (and that's the default), Windows will ONLY update LastBootUpTime on a system *restart*.
       In other words, LastBootUpTime will NOT be updated after sleeping,
       hibernating, or even a full system shutdown.

       The only way for LastBootUpTime to have a sane value is to turn off Fast Startup.
       (I've turned it off on all of my computers and haven't noticed a difference in boot times.)

       See the following for further details, and for steps about how to turn "Fast Startup" off.

         https://superuser.com/a/1096379
         https://lifehacker.com/shutting-down-windows-10-doesnt-actually-shut-down-wind-1825532376

      */

    public static DateTime GetBootDateTime()
    {
      return GetWmiPropertyValueAsDateTime("SELECT * FROM Win32_OperatingSystem WHERE Primary='true'", "LastBootUpTime");
    }
  }
}
