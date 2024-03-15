using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Windows;
using System.Threading;

namespace IjhCommonUtility
{
    /// <summary>
    /// C#からファイル操作するときの便利メソッド
    /// 例外が起きにくいよう対策、ファイルパスの指定も扱いやすく改良
    /// </summary>
    public class FileManager
    {
        #region ファイル操作

        /// <summary>
        /// 1ファイルを指定ディレクトリへ移動
        /// </summary>
        /// <param name="srcFilePath">移動するファイル</param>
        /// <param name="dstDir">移動先ディレクトリ</param>
        /// <param name="overwrite">上書きするか falseだと移動しない</param>
        public static void MoveSingleFile(string srcFilePath, string dstDir, bool overwrite = true)
        {
            string dstPath = Path.Combine(dstDir, Path.GetFileName(srcFilePath));
            if (File.Exists(dstPath))
            {
                if (overwrite) File.Delete(dstPath);
                else return;
            }
            File.Move(srcFilePath, dstPath);
        }
        /// <summary>
        /// 1ファイルを指定ディレクトリへコピー
        /// </summary>
        /// <param name="srcFilePath">コピーするファイル</param>
        /// <param name="dstDir">コピー先ディレクトリ</param>
        /// <param name="overwrite">上書きするか falseだとコピーしない</param>
        public static void CopySingleFile(string srcFilePath, string dstDir, bool overwrite = true)
        {
            string dstPath = Path.Combine(dstDir, Path.GetFileName(srcFilePath));
            if (File.Exists(dstPath))
            {
                if (overwrite) File.Delete(dstPath);
                else return;
            }
            File.Copy(srcFilePath, dstPath);
        }

        //ファイル削除
        public static void DeleteSingleFile(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        //.txtファイル作成
        public static void MakeTxtFile(string filePath, string txtData)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            //Shift-Jisでファイルを作成
            System.IO.StreamWriter sw = new System.IO.StreamWriter(filePath, true);
            sw.WriteLine(txtData);
            sw.Close();
        }

        /// <summary>
        /// ディレクトリを作成
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="overwrite"></param>
        public static void MakeDirectory(string Path, bool overwrite=true)
        {
            if (Directory.Exists(Path) && overwrite)
            {
                DeleteDirectory(Path); //重複の場合は削除して上書き
            }
            Directory.CreateDirectory(Path);
        }

        /// <summary>
        /// ファイルの名前変更
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="newName"></param>
        /// <param name="overwrite"></param>
        public static string Rename(string srcFilePath, string newName, bool overwrite=true)
        {
            string dstFullPath = Path.Combine(Path.GetDirectoryName(srcFilePath), newName);
            if (File.Exists(dstFullPath))
            {
                if (overwrite)
                {
                    Console.WriteLine($"overwrite:{dstFullPath}");
                    File.Delete(dstFullPath);  //重複の場合は上書き
                }
                else
                {
                    Console.WriteLine($"ファイル名重複のため変更なし:src:{srcFilePath}, dst:{dstFullPath}");
                }
            }
            Console.WriteLine("Rename " + srcFilePath + " To " + dstFullPath);
            File.Move(srcFilePath, dstFullPath);
            return dstFullPath;
        }

        //ファイルの検索
        //最初に引っかかったファイルを返す
        public static string FirstFileSearch(string Dir, string searchSentence="*")
        {
            DirectoryInfo di = new DirectoryInfo(Dir);
            foreach (var fi in di.GetFiles(searchSentence))
            {
                return fi.FullName;
            }
            return "";
        }

        /// <summary>
        /// srcフォルダ内を検索してdstにファイルを移動
        /// </summary>
        /// <param name="srcDir"></param>
        /// <param name="dstDir"></param>
        /// <param name="searchSentence"></param>
        public static void MoveWithSearch(string srcDir, string dstDir, string searchSentence = "*")
        {
            DirectoryInfo di = new DirectoryInfo(srcDir);

            Console.WriteLine("Move " + srcDir + " To " + dstDir + " search " + searchSentence);
            foreach (var fi in di.GetFiles(searchSentence))
            {
                try
                {
                    if (File.Exists(dstDir + fi.Name))
                    {
                        //Console.WriteLine("overwrite");
                        File.Delete(dstDir + fi.Name);  //重複の場合は上書き
                    }
                    //Console.WriteLine(fi.Name);
                    File.Move(Path.Combine(srcDir, fi.Name), Path.Combine(dstDir, fi.Name));
                }
                catch (Exception e)
                {
                    // ファイルを開くのに失敗したとき
                    Console.WriteLine(e.Message + " From:" + Path.Combine(srcDir, fi.Name) + " To:" + Path.Combine(dstDir, fi.Name) + "in MoveWithSearch");
                    throw;
                }
            }

        }

        /// <summary>
        /// srcフォルダ内を検索してdstにファイルをコピー
        /// </summary>
        /// <param name="srcDir"></param>
        /// <param name="dstDir"></param>
        /// <param name="searchSentence"></param>
        public static void CopyWithSearch(string srcDir, string dstDir, string searchSentence = "*")
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(srcDir);
                Console.WriteLine("Copy " + srcDir + " To " + dstDir);
                foreach (var fi in di.GetFiles(searchSentence))
                {
                    Console.WriteLine(fi.Name);
                    string temppath = System.IO.Path.Combine(dstDir, fi.Name);
                    File.Copy(Path.Combine(srcDir, fi.Name), Path.Combine(dstDir, fi.Name), true); //重複の場合は上書き
                }
            }
            catch (Exception e)
            {
                // ファイルを開くのに失敗したとき
                Console.WriteLine(e.Message + "CopyWithSearch");
            }
        }

        //フォルダコピー
        public static void CopyDirectory(string sourceDirPath, string destDirPath, bool copySubDirs=true)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirPath);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            var dstDir = Path.Combine(destDirPath, Path.GetFileName(sourceDirPath));
            // If the destination directory doesn't exist, create it.
            if (Directory.Exists(dstDir)) DeleteDirectory(dstDir);
            Directory.CreateDirectory(dstDir);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = System.IO.Path.Combine(dstDir, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = System.IO.Path.Combine(dstDir, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
        /// <summary>
        /// フォルダ移動　戻り値は移動先パス
        /// </summary>
        /// <param name="sourceDirPath"></param>
        /// <param name="destDirPath"></param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string MoveDirectory(string sourceDirPath, string destDirPath)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirPath);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            var dstDir = Path.Combine(destDirPath, Path.GetFileName(sourceDirPath));
            // If the destination directory doesn't exist, create it.
            if (Directory.Exists(dstDir)) DeleteDirectory(dstDir);
            Directory.CreateDirectory(dstDir);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = System.IO.Path.Combine(dstDir, file.Name);
                try
                {
                    file.MoveTo(temppath);
                }
                catch (Exception e)
                {
                    throw new ArgumentOutOfRangeException("FileMoveError!! " + temppath, e);
                }
            }

            // If copying subdirectories, copy them and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = System.IO.Path.Combine(dstDir, subdir.Name);
                MoveDirectory(subdir.FullName, temppath);
            }
            DeleteDirectory(sourceDirPath);
            return dstDir;
        }
        /// <summary>
        /// 指定したディレクトリとその中身を全て削除する
        /// </summary>
        public static void DeleteDirectory(string dstDir, bool deleteMyself = true)
        {
            try
            {
                System.IO.DirectoryInfo delDir = new DirectoryInfo(dstDir);
                FileSystemInfo[] fileInfos = delDir.GetFileSystemInfos("*", SearchOption.AllDirectories);
                //読み取り専用属性の解除
                foreach (var fi in fileInfos)
                {
                    if ((fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory) fi.Attributes = FileAttributes.Directory;
                    else fi.Attributes = FileAttributes.Normal;
                }
                //ディレクトリ削除
                if (deleteMyself) delDir.Delete(true);
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine("ファイル削除に失敗しました：" + dstDir);
                throw;
            }
        }
        public static void MoveSearchedDirectory(string srcDir, string dstDir, string searchSentense="*")
        {
            var di = new DirectoryInfo(srcDir);

            DirectoryInfo[] dInfos = di.GetDirectories(searchSentense, SearchOption.AllDirectories);
            if (dInfos.Length > 0) foreach (var dInfo in dInfos) MoveDirectory(dInfo.FullName, dstDir);

        }
        //Listに非同期書き込み
        public static void SyncWrite(List<string> list, string element, object syncObject)
        {
            lock (syncObject)
            {
                list.Add(element);
            }
            return;
        }
        public static void SyncWrite(List<string> list, List<string> element, object syncObject)
        {
            lock (syncObject)
            {
                list.AddRange(element);
            }
            return;
        }
        #endregion
        #region 情報取得
        /// <summary>
        /// フルパスからディレクトリ名を取得
        /// パスが拡張子なし or \で終わっているときも末尾ディレクトリ名が返る
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetDirectoryName(string filePath)
        {
            if (Path.HasExtension(filePath) == false
                || filePath.EndsWith("\\") == false)
            {
                return Path.GetFileName(filePath);
            }
            else
            {
                return Path.GetDirectoryName(filePath);
            }
        }
        /// <summary>
        /// ファイルの名前変更したパス取得(末尾に追加)
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="addName"></param>
        /// <param name="changeExt">拡張子を含めて変更するか</param>
        public static string GetRenamedPath_Add(string srcFilePath, string addName, bool changeExt = false)
        {
            string dstFullPath = changeExt ? Path.Combine(Path.GetDirectoryName(srcFilePath), Path.GetFileNameWithoutExtension(srcFilePath) + addName)
                                                         : Path.Combine(Path.GetDirectoryName(srcFilePath), Path.GetFileNameWithoutExtension(srcFilePath) + addName + Path.GetExtension(srcFilePath));
            return dstFullPath;
        }
        /// <summary>
        /// ファイルの名前変更したパス取得(新しいファイル名を付ける)
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="newName"></param>
        /// <param name="changeExt">拡張子を含めて変更するか</param>
        public static string GetRenamedPath_New(string srcFilePath, string newName, bool changeExt = false)
        {
            string dstFullPath = changeExt ? Path.Combine(Path.GetDirectoryName(srcFilePath), newName)
                                                         : Path.Combine(Path.GetDirectoryName(srcFilePath), newName + Path.GetExtension(srcFilePath));
            return dstFullPath;
        }
        /// <summary>
        /// ファイルパスのうち、直近ディレクトリ名を変更したパス取得(末尾に追加)
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="addDirName></param>
        public static string GetRenamedPathAnotherDir_Add(string srcFilePath, string addDirName)
        {
            string dstFullPath = Path.Combine(Path.GetDirectoryName(srcFilePath)+addDirName, Path.GetFileName(srcFilePath));
            return dstFullPath;
        }
        /// <summary>
        /// ファイルパスのうち、直近ディレクトリ名を変更したパス取得(新しいフォルダ名)
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="newDirName></param>
        public static string GetRenamedPathAnotherDir_New(string srcFilePath, string newDirName)
        {
            var rootDir = Path.GetDirectoryName(srcFilePath);
            string dstFullPath = Path.Combine(Path.GetDirectoryName(rootDir), newDirName, Path.GetFileName(srcFilePath));
            return dstFullPath;
        }
        /// <summary>
        /// ファイルの作成完了まで待機する(非同期)
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="timeout_sec"></param>
        /// <returns></returns>
        public static async Task<bool> WaitForMakeFileAsync(string filePath, int timeout_sec = 100)
        {
            int delay_msec = 500;
            int limit = timeout_sec * 1000 / delay_msec;
            for (int i = 0; i < limit; i++)
            {
                //File.Existsだと書き込み中かどうか分からないので、その後ファイルオープンを試す
                if (File.Exists(filePath))
                {
                    if (!IsFileLocked(filePath)) return true;
                }
                await Task.Delay(delay_msec);
            }
            return false;
        }
        /// <summary>
        /// ファイルの作成完了まで待機する
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="timeout_sec"></param>
        /// <returns></returns>
        public static bool WaitForMakeFile(string filePath, int timeout_sec = 100)
        {
            int delay_msec = 500;
            int limit = timeout_sec * 1000 / delay_msec;
            for (int i = 0; i < limit; i++)
            {
                //File.Existsだと書き込み中かどうか分からないので、その後ファイルオープンを試す
                if (File.Exists(filePath))
                {
                    if (!IsFileLocked(filePath)) return true;
                }
                Thread.Sleep(delay_msec);
            }
            return false;
        }
        /// <summary>
        /// 検索条件にあうファイルの作成完了まで待機する(非同期)
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="searchSentenece"></param>
        /// <param name="timeout_sec"></param>
        /// <returns></returns>
        public static async Task<(bool isSuccess, string path)> WaitForMakeFileAsync_Search(string dirPath, string searchSentenece = "*", int timeout_sec = 100)
        {
            int delay_msec = 500;
            int limit = timeout_sec * 1000 / delay_msec;
            for (int i = 0; i < limit; i++)
            {
                var files = Directory.GetFiles(dirPath, searchSentenece);
                if (files.Length == 0)
                {
                    await Task.Delay(delay_msec);
                    continue;
                }
                //File.Existsだと書き込み中かどうか分からないので、その後ファイルオープンを試す
                if (File.Exists(files[0]))
                {
                    if (!IsFileLocked(files[0])) return (true, files[0]);
                }
                await Task.Delay(delay_msec);
            }
            return (false, "");
        }
        /// <summary>
        /// 検索条件にあうファイルの作成完了まで待機する
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="searchSentenece"></param>
        /// <param name="timeout_sec"></param>
        /// <returns></returns>
        public static (bool isSuccess, string path) WaitForMakeFile_Search(string dirPath, string searchSentenece="*",  int timeout_sec = 100)
        {
            int delay_msec = 500;
            int limit = timeout_sec * 1000 / delay_msec;
            for (int i = 0; i < limit; i++)
            {
                var files = Directory.GetFiles(dirPath, searchSentenece);
                if (files.Length == 0)
                {
                    Thread.Sleep(delay_msec);
                    continue;
                }
                //File.Existsだと書き込み中かどうか分からないので、その後ファイルオープンを試す
                if (File.Exists(files[0]))
                {
                    if (!IsFileLocked(files[0])) return (true, files[0]);
                }
                Thread.Sleep(delay_msec);
            }
            return (false, "");
        }
        
        public static bool IsFileLocked(string path)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            }
            catch
            {
                return true;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
            return false;
        }
        #endregion
    }
}
