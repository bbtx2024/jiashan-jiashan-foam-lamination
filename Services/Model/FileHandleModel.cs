using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Model
{
    public class FileHandleModel
    {
        public static bool WriteFileToRemote(string carriersn, string productsn, bool needwork)
        {
            //判断文件路径是否存在
            if (!Directory.Exists("Z:\\"))
                return false;

            string filefullname = "Z:\\" + carriersn + ".txt";

            StreamWriter file = new StreamWriter(filefullname.ToString(), true);
            StringBuilder sb = new StringBuilder();
            if (needwork == true)
            {
                sb.Append($"{carriersn},{productsn},0");
            }
            else
                sb.Append($"{carriersn},{productsn},1");

            file.Write(sb.ToString());
            file.Close();
            return true;
        }
        public static bool ReadFileFromRemote(ref string carriersn, ref string productsn, ref bool needwork)
        {
            if (!Directory.Exists("Z:\\"))
                return false;
            string[] fileall = Directory.GetFiles("Z:\\");
            if (fileall.Length != 1)
                return false;
            if (!fileall[0].Contains(".txt"))
            {
                return false;
            }
            string name = fileall[0].Replace("Z:\\", "").Replace(".txt", "");

            StreamReader file = new StreamReader(fileall[0].ToString(), true);

            string content = file.ReadLine();

            if (file.ReadLine() != string.Empty)
            {
                //内容非法
                return false;
            }

            string[] parse = content.Split(',');

            if (parse.Length != 3)
            {
                return false;
            }

            if (name != parse[0])
            {
                return false;
            }
            carriersn = parse[0].ToString();
            productsn = parse[1].ToString();
            needwork = parse[2] == "0" ? true : false;

            return true;
        }
        public static bool CopyFileToRemote(string sourceFileName, string destFileName)
        {
            try
            {
                if (!File.Exists(sourceFileName))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"拷贝源文件不存在:{sourceFileName}");
                    return false;
                }
                File.Copy(sourceFileName, destFileName);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
                return false;
            }
            return true;
        }
        public static void DeleAllFile(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;
                string[] fileall = Directory.GetFiles(path);
                for (int i = 0; i < fileall.Length; i++)
                {
                    File.Delete(fileall[i]);
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
        }
        public static bool GetZipFile(string path, ref string filename)
        {
            filename = string.Empty;
            try
            {
                string[] fileall = Directory.GetFiles(path);

                if (fileall.Length != 1)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"Current file count = {fileall.Length.ToString()}");
                    return false;
                }
                else
                {
                    string[] sp = fileall[0].Split('\\');

                    for (int i = 0; i < sp.Length; i++)
                    {
                        if (sp[i].Contains(".zip"))
                        {
                            filename = sp[i].Replace(".zip", "");
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return false;
        }
        public static bool GetZipFiles(string path, int filecount, ref List<string> filename)
        {
            try
            {
                string[] fileall = Directory.GetFiles(path);
                DateTime dt = DateTime.Now;
                while (fileall.Length < filecount && DateTime.Now.Subtract(dt).TotalMilliseconds <= 3000)
                {
                    fileall = Directory.GetFiles(path);
                    //NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"Current file count = {fileall.Length.ToString()}");
                    Thread.Sleep(100);
                }
                if (fileall.Length != filecount || fileall.Length <= 0)
                {
                    HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.PDCA, 0, 0, $"获取文件数量不对,SetCount = {filecount.ToString()},GetCOunt = {fileall.Length.ToString()}");
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"Current file count = {fileall.Length.ToString()},setCount = {filecount.ToString()}");
                    return false;
                }
                else
                {
                    foreach (var singlefile in fileall)
                    {
                        string[] sp = singlefile.Split('\\');

                        if (sp.Last().Contains(".zip"))
                        {
                            filename.Add(sp.Last());
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return false;
        }
        public static bool GoerTek_GetZipFiles(string path, ref string[] filename)
        {
            try
            {
                string aiopath = path + "\\" + DateTime.Now.ToString("yyyyMMdd");
                if (!Directory.Exists(aiopath))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"AOI图片路径不存在！");
                    return false;
                }
                DateTime dt = DateTime.Now;
                string[] fileall = new string[256];

                fileall = Directory.GetFiles(path + "\\" + DateTime.Now.ToString("yyyyMMdd")/*,".zip",SearchOption.TopDirectoryOnly*/);

                while (fileall.Length <= 0 && DateTime.Now.Subtract(dt).TotalMilliseconds <= 3000)
                {
                    fileall = Directory.GetFiles(path + "\\" + DateTime.Now.ToString("yyyyMMdd")/*,".zip",SearchOption.TopDirectoryOnly*/);
                    Thread.Sleep(100);
                }

                if (fileall.Length > 3 && fileall.Length <= 0/*|| fileall.Length < 2*/)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"Current file count = {fileall.Length.ToString()}");
                    return false;
                }
                else
                {
                    filename = new string[fileall.Length];

                    string[] sp = new string[20];

                    for (int i = 0; i < fileall.Length; i++)
                    {
                        sp = fileall[i].Split('\\');

                        for (int j = 0; j < sp.Length; j++)
                        {
                            if (sp[j].Contains(".zip"))
                            {
                                filename[i] = sp[j].Replace(".zip", "");
                                //return true;
                            }
                            else
                            {
                                continue;
                            }
                        }

                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return false;
        }
        public static bool GoerTek_GetZipFiles(string path, int filecount, ref List<string> filename)
        {
            try
            {
                string aiopath = path + "\\" + DateTime.Now.ToString("yyyyMMdd");
                if (!Directory.Exists(aiopath))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"AOI图片路径不存在！");
                    return false;
                }
                DateTime dt = DateTime.Now;
                string[] fileall = Directory.GetFiles(path + "\\" + DateTime.Now.ToString("yyyyMMdd")/*,".zip",SearchOption.TopDirectoryOnly*/);

                while (fileall.Length < filecount && DateTime.Now.Subtract(dt).TotalMilliseconds <= 3000)
                {
                    fileall = Directory.GetFiles(path + "\\" + DateTime.Now.ToString("yyyyMMdd")/*,".zip",SearchOption.TopDirectoryOnly*/);
                    Thread.Sleep(100);
                }

                if (fileall.Length != filecount || fileall.Length <= 0)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"Current file count = {fileall.Length.ToString()},setCount = {filecount.ToString()}");
                    return false;
                }
                else
                {
                    foreach (var singlefile in fileall)
                    {
                        string[] sp = singlefile.Split('\\');

                        if (sp.Last().Contains(".zip"))
                        {
                            filename.Add(sp.Last());
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return false;
        }
        public static bool GoerTek_MoveZipFile(string oldPath, List<string> filenamelst, string newPath)//20210714
        {
            try
            {
                if (!Directory.Exists(newPath + "\\" + DateTime.Now.ToString("yyyyMMdd")))
                {
                    Directory.CreateDirectory(newPath + "\\" + DateTime.Now.ToString("yyyyMMdd"));
                }
                if (!Directory.Exists(oldPath))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"AOI图片路径不存在！");
                    return false;
                }
                else
                {
                    foreach (var filename in filenamelst)
                    {
                        string newfilename = Path.Combine(newPath + "\\" + DateTime.Now.ToString("yyyyMMdd"), filename);
                        FileInfo fileInfo = new FileInfo(oldPath + "\\" + filename);
                        fileInfo.MoveTo(newfilename);
                    }
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
                return false;
            }
            return true;
        }
        public static bool MoveZipFile(string oldPath, List<string> filenamelst, string newPath, string lineNo, string station, string stationID)
        {
            //公盘映射地址：\\b19-picstor01.luxshare.com.cn\share_abg_pic\abg_pic
            //Web地址:  http://10.33.20.148/UploadFileData/WebService.asmx     （正式）
            //Web地址:  http://10.103.5.102:8080/WebService.asmx     （测试）
            //上传目录格式：abg_pic\\2021-08-12\\14\\E2-3F-H27-OFF-01\\STATION269\\ITKS_E02-3FT-01_1_STATION269
            try
            {
                string path = @newPath + "\\" + DateTime.Now.ToString("yyyy-MM-dd");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = path + "\\" + DateTime.Now.ToString("HH");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = path + "\\" + lineNo;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = path + "\\" + station;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = path + "\\" + stationID;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                //if (!Directory.Exists(oldPath))
                //{
                //    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"AOI图片路径不存在！");
                //    return false;
                //}
                else
                {
                    foreach (var filename in filenamelst)
                    {
                        string newfilename = Path.Combine(path, filename);
                        FileInfo fileInfo = new FileInfo(oldPath + "\\" + filename);
                        fileInfo.MoveTo(newfilename);
                    }
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
                return false;
            }
            return true;
        }
        //因为相机拍照也产生照片，所以在焊接前删除拍照图，上传Recheck图片
        public static bool DelBeforeSolderPic(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"AOI图片路径不存在！");
                    return false;
                }
                DateTime dt = DateTime.Now;
                string[] fileall = new string[256];
                fileall = Directory.GetFiles(path);

                while (fileall.Length <= 0 && DateTime.Now.Subtract(dt).TotalMilliseconds <= 3000)
                {
                    fileall = Directory.GetFiles(path);
                    Thread.Sleep(100);
                }

                if (fileall.Length > 0)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"BeforeSolder ZIP count = {fileall.Length.ToString()}");
                    DeleAllFile(path);
                    return true;
                }
                else
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"Current file count = {fileall.Length.ToString()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return false;
        }
        public static bool MoveZipFile(string oldPath, string newPath)
        {
            try
            {
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                if (!Directory.Exists(oldPath))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"AOI图片路径不存在！");
                    return false;
                }
                else
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(oldPath);
                    FileInfo[] fileInfos = directoryInfo.GetFiles();

                    if (fileInfos.Length <= 0) return false;

                    for (int i = 0; i < fileInfos.Length; i++)
                    {
                        string path = Path.Combine(newPath, fileInfos[i].Name);
                        fileInfos[i].MoveTo(path);
                    }
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.ToString() + ex.StackTrace);
            }
            return true;
        }

        //public bool FTPUpLoadPic(List<string> filenamelst)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(Param.AOIPicPath) || string.IsNullOrEmpty(Param.Line_No)
        //                                                   || string.IsNullOrEmpty(Param.StationID) ||
        //                                                   string.IsNullOrEmpty(Param.MachineID))
        //        {
        //            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "AOI图片文件路径或Line_No或StationID或MachineID为空");
        //            HistoryWanning.GetSingleTon().AddWarningInfo(EN_WarnModules.FTP, 0, 0, "AOI图片文件路径或Line_No或StationID或MachineID为空");
        //            return false;
        //        }
        //        foreach (var filename in filenamelst)
        //        {
        //            ftp.Upload2(Path.Combine(Param.AOIPicPath, filename),
        //                DateTime.Now.ToString("yyyy-MM-dd"),
        //                Param.Line_No,
        //                Param.StationID,
        //                Param.MachineID);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message + ex.StackTrace);
        //        return false;
        //    }
        //    return true;
        //}
    }
}
