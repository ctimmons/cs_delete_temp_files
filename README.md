# cs_delete_temp_files

#####C# app to delete old files and folders from the current user's temp folder.  

>!!! CAUTION !!!

>This app deletes files and folders.  If it deletes a file or folder that another process depends on, that process may crash or experience unexpected behavior.

As a software developer, I tend to accumulate a lot of flotsam in my temp folder, usually from testing new software I've written.  Just prior to writing this app, my temp folder had thousands of files taking up 3 GB of drive space.

The app assumes any file or folder that was created, accessed, or written to before the computer was booted up is an orphan, and can safely be deleted.  The app recursively walks the current user's temp folder depth first.  As the recursion unwinds, candidate files are deleted, as well as their parent folders (if the folder is empty).  If the file or folder can't be deleted, it is skipped.



