/* This software is in the public domain.  See the LICENSE file for details. */

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
      RecursivelyDeleteTempFilesAndFolders(new DirectoryInfo(Path.GetTempPath()));
    }

    private static void RecursivelyDeleteTempFilesAndFolders(DirectoryInfo di)
    {
      foreach (var fsi in di.EnumerateFileSystemInfos())
      {
        if (fsi is DirectoryInfo)
          RecursivelyDeleteTempFilesAndFolders(fsi as DirectoryInfo);
        
        if (CanDelete(fsi))
        {
          try
          {
            fsi.Delete();
          }
          catch
          {
            /* The file or folder can't be deleted.

               Assume some other process is using this item.
               Don't try to figure out why it can't be deleted or try to force
               the deletion.  It's safest to just skip it. */
          }
        }
      }
    }

    private static Boolean CanDelete(FileSystemInfo fsi)
    {
      return
        (fsi.CreationTime   < _bootDateTime) &&
        (fsi.LastAccessTime < _bootDateTime) &&
        (fsi.LastWriteTime  < _bootDateTime) &&
        ((fsi is DirectoryInfo) ? !(fsi as DirectoryInfo).EnumerateFileSystemInfos().Any() : true);
    }

    private static String GetWmiPropertyValueAsString(String queryString, String propertyName)
    {
      var query = new SelectQuery(queryString);
      var searcher = new ManagementObjectSearcher(query);
      foreach (ManagementObject mo in searcher.Get())
        return mo.Properties[propertyName].Value.ToString();

      return null;
    }

    private static DateTime GetWmiPropertyValueAsDateTime(String queryString, String propertyName)
    {
      var value = GetWmiPropertyValueAsString(queryString, propertyName);
      return (value == null) ? DateTime.MinValue : ManagementDateTimeConverter.ToDateTime(value);
    }

    public static DateTime GetBootDateTime()
    {
      return GetWmiPropertyValueAsDateTime("SELECT * FROM Win32_OperatingSystem WHERE Primary='true'", "LastBootUpTime");
    }
  }
}
