using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using ICSharpCode.SharpZipLib.Zip;
using Ookii.Dialogs.Wpf;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model.Alarm;
using QA.Business.Model.RunTimeInfo;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using IComponent = QA.Business.Interfaces.IComponent;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.SpotCheckPages.Views
{
    public enum EN_UploadState
    {
        Idle,
        Zipping,
        Uploading
    }

    [Export("SpotCheckFTPUploadImgViewModel", typeof(ISpotPageViewModel))]
    public class SpotCheckFTPUploadImgViewModel : Screen, INotifyPropertyChanged, ISpotPageViewModel, IHandle<List<IComponent>>
    {
        #region Field
        private IEventAggregator _eventAggregator;
        private ParamManager _paramManager;
        private object obj = new object();
        private int count = 0;
        private DateTime lastAutoUploadDate = DateTime.Now.AddDays(-1).Date;
        private AlarmInfoModel serverNotLinkAlarm = null;
        #endregion

        #region Property
        public ushort OrderID { get; set; } = 8;
        public override string DisplayName { get; set; } = "图片上传";
        private string ImgPath { get => _paramManager.CameraParam.ImgPath; }
        private string FTP_IP { get => "ftp://" + _paramManager.CameraParam.FTP_ServerIP + ":" + _paramManager.CameraParam.FTP_ServerPort; }
        private string FTP_UserName { get => _paramManager.CameraParam.FTP_UserName; }
        private string FTP_Password { get => _paramManager.CameraParam.FTP_Password; }
        private string FTP_Line { get => _paramManager.CameraParam.FTP_Line; }
        private string FTP_Station { get => _paramManager.CameraParam.FTP_Station; }
        public ObservableCollection<RunTimeInfoModel> Logs { get; set; } = new ObservableCollection<RunTimeInfoModel>();

        private bool _autoUploadImg;
        public bool AutoUploadImg
        {
            get => _autoUploadImg;
            set
            {
                _autoUploadImg = value;
                NotifyOfPropertyChange(() => AutoUploadImg);
            }
        }

        private bool _isServerConnected = false;
        public bool IsServerConnected
        {
            get => _isServerConnected;
            set
            {
                _isServerConnected = value;
                NotifyOfPropertyChange(() => IsServerConnected);
            }
        }

        private string _uploadDir = "";
        public string UploadDir
        {
            get => _uploadDir;
            set
            {
                _uploadDir = value;
                NotifyOfPropertyChange(() => UploadDir);
            }
        }

        private EN_UploadState _uploadState = EN_UploadState.Idle;
        public EN_UploadState UploadState
        {
            get => _uploadState;
            set
            {
                _uploadState = value;
                NotifyOfPropertyChange(() => ButtonEnabled);
                NotifyOfPropertyChange(() => UploadState);
            }
        }

        public bool ButtonEnabled
        {
            get
            {
                return _uploadState == EN_UploadState.Idle;
            }
        }

        private double _zipProgress;
        public double ZipProgress
        {
            get => _zipProgress;
            set
            {
                _zipProgress = value;
                NotifyOfPropertyChange(() => ZipProgress);
            }
        }

        private double _uploadProgress;
        public double UploadProgress
        {
            get => _uploadProgress;
            set
            {
                _uploadProgress = value;
                NotifyOfPropertyChange(() => UploadProgress);
            }
        }
        #endregion

        #region Constructor
        public SpotCheckFTPUploadImgViewModel()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _paramManager = IoC.Get<ParamManager>();
        }
        #endregion

        #region Handle
        public void Handle(List<IComponent> message)
        {
            AutoUploadImg = _paramManager.CameraParam.FTP_AutoUploadImg;
            //每10s左右刷新一次状态
            if (count % 50 == 0)
            {
                count = 1;
                if (AutoUploadImg)
                {
                    CheckFTPLinkState();
                }
            }
            else
            {
                count++;
            }
            //到下一天后，自动上传图片
            if (DateTime.Now.Date != lastAutoUploadDate)
            {
                DateTime date = DateTime.Now.Date;
                lastAutoUploadDate = date;
                AutoZipAndUpload(false.ToString());
            }
            //判断是否关闭了自动上传图片
            if (!AutoUploadImg && serverNotLinkAlarm != null)
            {
                var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
                _baseBiz.RemoveRunAlarm(serverNotLinkAlarm);
                serverNotLinkAlarm = null;
            }
        }
        #endregion

        #region Method
        public async void AutoZipAndUpload(string manualStr)
        {
            bool manual = bool.Parse(manualStr);
            DateTime time;
            if (manual)
            {
                if (!AutoUploadImg)
                {
                    MessageBox.Warning("请打开系统设置-相机设置-启用图片打包上传至服务器！");
                    return;
                }
                if (serverNotLinkAlarm != null)
                {
                    MessageBox.Warning("服务器连接异常，无法上传！");
                    return;
                }
                if (string.IsNullOrEmpty(UploadDir) || !Directory.Exists(UploadDir))
                {
                    MessageBox.Warning("请选择要上传的文件夹！");
                    return;
                }
                int lastIndex = UploadDir.LastIndexOf('\\');
                if (UploadDir.Substring(0, lastIndex) != ImgPath)
                {
                    MessageBox.Error("必须选择存图文件夹内某个日期文件夹！");
                    return;
                }
                if (!DateStringToDateTime(UploadDir.Substring(lastIndex + 1), out time))
                {
                    MessageBox.Error("必须选择存图文件夹内某个日期文件夹！");
                    return;
                }
                //needUpload：本地有文件夹（已经在上面判断过了），且服务器没有zip
                bool needUpload = !FTPFileExists(time);
                if (!needUpload)
                {
                    if (MessageBox.Show($"已上传过{time.ToString("yyyyMMdd")}图片，是否重新上传？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                AddNLogInfo(EN_WARN_LEVEL.Info, $"手动上传开始");
                await Task.Run(() =>
                {
                    ForceZipAndUpload(time);
                    UploadState = EN_UploadState.Idle;
                });
                AddNLogInfo(EN_WARN_LEVEL.Info, $"手动上传结束");
            }
            else
            {
                await Task.Run(() =>
                {
                    //删除本地和服务器所有90天前的图片
                    AddNLogInfo(EN_WARN_LEVEL.Info, $"开始删除本地90天前的文件");
                    DateTime date = DateTime.Now.Date;
                    bool delete = false;
                    if (Directory.Exists(ImgPath))
                    {
                        string[] localDirs = Directory.GetDirectories(ImgPath);
                        foreach (var dir in localDirs)
                        {
                            if (!DateStringToDateTime(dir.Substring(dir.LastIndexOf('\\') + 1), out time))
                            {
                                continue;
                            }
                            if ((date - time).TotalDays > 90)
                            {
                                DeleteQuietly(dir);
                                delete = true;
                                AddNLogInfo(EN_WARN_LEVEL.Sucess, $"已删除本地文件夹{dir}");
                            }
                        }
                    }
                    if (!AutoUploadImg)
                    {
                        AddNLogInfo(EN_WARN_LEVEL.Info, "未打开系统设置-相机设置-启用图片打包上传至服务器，跳过上传！");
                        return;
                    }
                    if (serverNotLinkAlarm != null)
                    {
                        AddNLogInfo(EN_WARN_LEVEL.Info, "服务器连接异常，无法上传！");
                        return;
                    }
                    AddNLogInfo(EN_WARN_LEVEL.Info, $"开始删除服务器90天前的文件");
                    string FTP_Dir = $"{FTP_IP}/{FTP_Line}/{FTP_Station}";
                    if (FTPFileExists(FTP_Dir))
                    {
                        string[] FTPFileNames = FTPGetFileNames();
                        foreach (var fileName in FTPFileNames)
                        {
                            if (!fileName.EndsWith("zip"))
                            {
                                continue;
                            }
                            if (!DateStringToDateTime(fileName.Substring(0, fileName.LastIndexOf('.')), out time))
                            {
                                continue;
                            }
                            if ((date - time).TotalDays > 90)
                            {
                                string file = $"{FTP_IP}/{FTP_Line}/{FTP_Station}/{time.ToString("yyyyMMdd")}.zip";
                                FTPDeleteFile(file);
                                delete = true;
                                AddNLogInfo(EN_WARN_LEVEL.Sucess, $"已删除服务器文件{file}");
                            }
                        }
                    }
                    if (delete)
                    {
                        AddNLogInfo(EN_WARN_LEVEL.Sucess, $"已删除本地和服务器所有90天前的文件");
                    }
                    else
                    {
                        AddNLogInfo(EN_WARN_LEVEL.Info, $"本地和服务器均不存在90天前的文件");
                    }
                    Thread.Sleep(5);
                    if (!Directory.Exists(ImgPath))
                    {
                        AddNLogInfo(EN_WARN_LEVEL.Info, $"本地没有图片需要上传");
                        return;
                    }
                    //遍历上传90天图片
                    AddNLogInfo(EN_WARN_LEVEL.Info, $"自动上传开始");
                    time = DateTime.Now.Date;
                    for (int i = 0; i < 90; i++)
                    {
                        time = time.AddDays(-1);
                        //needUpload：本地有文件夹，且服务器没有zip
                        bool needUpload = Directory.Exists(ImgPath + "\\" + time.ToString("yyyyMMdd")) && !FTPFileExists(time);
                        if (needUpload)
                        {
                            ForceZipAndUpload(time);
                        }
                    }
                    UploadState = EN_UploadState.Idle;
                    AddNLogInfo(EN_WARN_LEVEL.Info, $"自动上传结束");
                });
            }
        }

        /// <summary>
        /// 将yyyyMMdd格式的日期字符串转为DateTime
        /// </summary>
        /// <param name="inputDateStr"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private bool DateStringToDateTime(string inputDateStr, out DateTime time)
        {
            time = DateTime.Now;
            //inputDateStr:"20230530"  shortDateStr:"2023/5/30"或"2023/05/30"都可以
            if (inputDateStr.Length != 8)
            {
                return false;
            }
            string shortDateStr = $"{inputDateStr.Substring(0, 4)}/{inputDateStr.Substring(4, 2)}/{inputDateStr.Substring(6)}";
            return DateTime.TryParse(shortDateStr, out time);
        }

        /// <summary>
        /// 智能删除本地文件/文件夹
        /// </summary>
        /// <param name="path"></param>
        private void DeleteQuietly(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    DeleteQuietly(file);
                }
                foreach (var dir in Directory.GetDirectories(path))
                {
                    DeleteQuietly(dir);
                }
                Directory.Delete(path);
            }
        }

        /// <summary>
        /// 强制打包上传，传进来的路径默认是可用的
        /// </summary>
        private void ForceZipAndUpload(DateTime time)
        {
            UploadState = EN_UploadState.Zipping;
            UploadDir = ImgPath + "\\" + time.ToString("yyyyMMdd");
            ZipProgress = 0;
            UploadProgress = 0;
            if (ZipDirectory(time))
            {
                UploadState = EN_UploadState.Uploading;
                UploadZipToServerThenDelete(time);
            }
        }

        public void ChooseDir()
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "请选择要上传的日期文件夹";
            dialog.UseDescriptionForTitle = true;
            dialog.SelectedPath = $"{ImgPath}\\{DateTime.Now.AddDays(-1).ToString("yyyyMMdd")}";
            bool? b = dialog.ShowDialog(null);
            if (b == true)
            {
                UploadDir = dialog.SelectedPath;
            }
        }

        public void OpenFTPDir()
        {
            string dir = $"{FTP_IP}/{FTP_Line}";
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }
        #endregion

        #region Log
        private void AddNLogInfo(EN_WARN_LEVEL level, string msg)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(Pl =>
                {
                    lock (obj)
                    {
                        var model = new RunTimeInfoModel()
                        {
                            RunLevel = level,
                            Datetime = DateTime.Now,
                            Msg = msg,
                            DetailMsg = "",
                        };
                        Logs.Add(model);
                        if (Logs.Count >= 1000)
                        {
                            Logs.RemoveAt(0);
                        }
                    }
                }, null);
            });
        }

        public void ClearLogs()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                    SynchronizationContext.Current.Post(Pl =>
                    {
                        lock (obj)
                        {
                            Logs.Clear();
                        }
                    }, null);
                });
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"添加运行日志异常:{ex.Message},{ex.StackTrace}");
            }
        }
        #endregion

        #region ZIP压缩
        /// <summary>
        /// 压缩指定的文件夹  
        /// </summary>
        public bool ZipDirectory(DateTime time)
        {
            try
            {
                if (!Directory.Exists(UploadDir))
                {
                    AddNLogInfo(EN_WARN_LEVEL.Error, $"文件夹{UploadDir}不存在");
                    return false;
                }
                List<string> fileList = GetUploadImgs(UploadDir);
                long totalLength = 0;
                foreach (var file in fileList)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    totalLength += fileInfo.Length;
                }
                if (totalLength == 0)
                {
                    AddNLogInfo(EN_WARN_LEVEL.Info, $"文件夹{UploadDir}内没有需要上传的图片");
                    return false;
                }
                string zipPath = UploadDir.Substring(0, UploadDir.LastIndexOf("\\") + 1) + time.ToString("yyyyMMdd") + ".zip";
                using (ZipOutputStream zipoutputstream = new ZipOutputStream(new FileStream(zipPath, FileMode.Create)))
                {
                    int bufferLength;//当前缓存区数据流的数据量
                    long zippedLength = 0;//已经压缩的数据量
                    foreach (string file in fileList)
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            ZipEntry entry = new ZipEntry(file.Substring(UploadDir.Length));
                            zipoutputstream.PutNextEntry(entry);
                            byte[] buffer = new byte[8192];
                            while ((bufferLength = fs.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                zipoutputstream.Write(buffer, 0, buffer.Length);
                                zippedLength += bufferLength;
                                //确保是百分比下两位小数，如12.34%
                                ZipProgress = double.Parse(((double)zippedLength / totalLength).ToString("F4"));
                            }
                        }
                    }
                }
                AddNLogInfo(EN_WARN_LEVEL.Info, $"已压缩图片至{zipPath}");
                return true;
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 获取指定日期文件夹内所有要上传的图片
        /// </summary>
        private static List<string> GetUploadImgs(string dir)
        {
            List<string> list = new List<string>();
            GetUploadImgs(dir, new[] { "FeederVision", "UpVision", "DownVision", "Pressurize" }, new[] { ".jpg" }, list);
            return list;
        }

        private static void GetUploadImgs(string path, string[] acceptDirName, string[] acceptFileSuffix, List<string> list)
        {
            for (int i = 0; i < acceptDirName.Length; i++)
            {
                acceptDirName[i] = acceptDirName[i].ToLower();
            }
            for (int i = 0; i < acceptFileSuffix.Length; i++)
            {
                acceptFileSuffix[i] = acceptFileSuffix[i].ToLower();
            }
            GetUploadImgs(path, acceptDirName, acceptFileSuffix, list, false);
        }

        private static void GetUploadImgs(string path, string[] acceptDirName, string[] acceptFileSuffix, List<string> list, bool contains)
        {
            if (File.Exists(path))
            {
                if (contains && !list.Contains(path))
                {
                    string extension = new FileInfo(path).Extension.ToLower();
                    foreach (var value in acceptFileSuffix)
                    {
                        if (value == extension)
                        {
                            list.Add(path);
                            break;
                        }
                    }
                }
            }
            else if (Directory.Exists(path))
            {
                if (!contains)
                {
                    string name = new DirectoryInfo(path).Name.ToLower();
                    foreach (var value in acceptDirName)
                    {
                        if (value == name)
                        {
                            contains = true;
                            break;
                        }
                    }
                }
                foreach (var file in Directory.GetFiles(path))
                {
                    GetUploadImgs(file, acceptDirName, acceptFileSuffix, list, contains);
                }
                foreach (var dir in Directory.GetDirectories(path))
                {
                    GetUploadImgs(dir, acceptDirName, acceptFileSuffix, list, contains);
                }
            }
        }
        #endregion

        #region FTP上传
        /// <summary>
        /// 检查FTP链接状态
        /// </summary>
        /// <returns></returns>
        private void CheckFTPLinkState()
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(FTP_IP));
                request.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Timeout = 3000;
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    IsServerConnected = true;
                    if (serverNotLinkAlarm != null)
                    {
                        var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
                        _baseBiz.RemoveRunAlarm(serverNotLinkAlarm);
                        serverNotLinkAlarm = null;
                    }
                }
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
                IsServerConnected = false;
                if (serverNotLinkAlarm == null)
                {
                    var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
                    string msg = "上传图片服务器连接异常，请检查连接状态！";
                    serverNotLinkAlarm = _baseBiz.AddRunAlarm(EN_WarnModules.Camera, msg, 0);
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, msg);
                }
            }
        }

        /// <summary>
        /// 上传本地文件到FTP服务器，然后删除本地文件
        /// </summary>
        private bool UploadZipToServerThenDelete(DateTime time)
        {
            string zipPath = UploadDir.Substring(0, UploadDir.LastIndexOf("\\") + 1) + time.ToString("yyyyMMdd") + ".zip";
            try
            {
                CheckFTPLinkState();
                if (!IsServerConnected)
                {
                    AddNLogInfo(EN_WARN_LEVEL.Error, $"未能连接服务器，取消上传！");
                    return false;
                }
                if (!File.Exists(zipPath))
                {
                    return false;
                }
                //例子：ftp://127.0.0.1:21/LE1_Line1/TP/Test.zip
                string FTP_Dir = $"{FTP_IP}/{FTP_Line}/{FTP_Station}";
                if (!FTPMakeDirs(FTP_Dir))
                {
                    return false;
                }
                FileInfo info = new FileInfo(zipPath);
                string FTP_FilePath = $"{FTP_Dir}/{info.Name}";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(FTP_FilePath));
                request.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Timeout = 30000;
                int buffLength = 8192;
                byte[] buff = new byte[buffLength];
                long totalLength = info.Length;//要上传的总字节长度
                long uploadedLength = 0;//已经上传完毕的字节长度
                int bufferLength = 0;//要上传的文件数据流在缓存区的字节长度
                using (FileStream fs = info.OpenRead())
                {
                    using (Stream stream = request.GetRequestStream())
                    {
                        while ((bufferLength = fs.Read(buff, 0, buffLength)) != 0)
                        {
                            stream.Write(buff, 0, bufferLength);
                            uploadedLength += bufferLength;
                            //确保是百分比下两位小数，如12.34%
                            UploadProgress = double.Parse(((double)uploadedLength / totalLength).ToString("F4"));
                        }
                    }
                }
                AddNLogInfo(EN_WARN_LEVEL.Sucess, $"已上传压缩包至{FTP_FilePath}");
                return true;
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
                return false;
            }
            finally
            {
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                    AddNLogInfo(EN_WARN_LEVEL.Info, $"已删除{zipPath}");
                }
            }
        }

        /// <summary>
        /// FTP创建多级目录文件夹-SXF
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        private bool FTPMakeDirs(string dirName)
        {
            string[] dirParts = dirName.Split('/');
            if (dirParts.Length <= 3)
            {
                return true;
            }
            string tempPath = $"ftp://{dirParts[2]}";
            for (int i = 3; i < dirParts.Length; i++)
            {
                tempPath += "/" + dirParts[i];
                if (!FTPMakeDir(tempPath))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 如果FTP服务器上存在指定目录则返回true，不存在则创建该目录。
        /// 该方法不可以创建多级目录。
        /// </summary>
        /// <param name="dir">文件目录</param>
        /// <returns></returns>
        private bool FTPMakeDir(string dir)
        {
            try
            {
                if (FTPFileExists(dir))
                {
                    return true;
                }
                string url = dir;
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(url));
                request.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
                return true;
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 判断ftp上的文件目录是否存在，绝对路径
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        private bool FTPFileExists(string fullPath)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(fullPath));
                request.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    return true;
                }
            }
            catch (Exception ignored)
            {
                return false;
            }
        }

        /// <summary>
        /// 判断ftp上的文件目录是否存在，相对路径，path格式为20220908
        /// </summary>
        /// <param name="path">日期，格式20220908</param>
        /// <returns></returns>
        private bool FTPFileExists(DateTime time)
        {
            string dir = $"{FTP_IP}/{FTP_Line}/{FTP_Station}/{time.ToString("yyyyMMdd")}.zip";
            return FTPFileExists(dir);
        }

        /// <summary>
        /// 从ftp服务器获取文件列表
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string[] FTPGetFileNames()
        {
            try
            {
                string FTP_Dir = $"{FTP_IP}/{FTP_Line}/{FTP_Station}";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(FTP_Dir));
                request.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream(), Encoding.Default))
                {
                    StringBuilder sb = new StringBuilder();
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        sb.Append(line).Append("\n");
                    }
                    if (sb.ToString() == "")
                    {
                        return new string[0];
                    }
                    else
                    {
                        //先去除最后的\n，再用\n分割
                        string s = sb.ToString();
                        return s.Substring(0, s.Length - 1).Split('\n');
                    }
                }
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 下载FTP服务器文件到本地
        /// </summary>
        /// <param name="ftpfilepath">ftp下载的地址</param>
        /// <param name="filePath">存放到本地的路径</param>
        /// <param name="fileName">保存的文件名称</param>
        /// <returns></returns>
        private bool FTPDownloadFile(string ftpfilepath, string filePath, string fileName)
        {
            try
            {
                filePath = filePath.Replace("我的电脑\\", "");
                string onlyFileName = Path.GetFileName(fileName);
                string newFileName = filePath + onlyFileName;
                if (File.Exists(newFileName))
                {
                    File.Delete(newFileName);
                }
                ftpfilepath = ftpfilepath.Replace("\\", "/");
                string url = FTP_IP + ftpfilepath;
                FtpWebRequest reqFtp = (FtpWebRequest)WebRequest.Create(new Uri(url));
                reqFtp.UseBinary = true;
                reqFtp.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                FtpWebResponse response = (FtpWebResponse)reqFtp.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                long cl = response.ContentLength;
                int bufferSize = 2048;
                int readCount;
                byte[] buffer = new byte[bufferSize];
                readCount = ftpStream.Read(buffer, 0, bufferSize);
                FileStream outputStream = new FileStream(newFileName, FileMode.Create);
                while (readCount > 0)
                {
                    outputStream.Write(buffer, 0, readCount);
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                }
                ftpStream.Close();
                outputStream.Close();
                response.Close();
                return true;
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 获得文件大小
        /// </summary>
        /// <param name="url">FTP文件的完全路径</param>
        /// <returns></returns>
        private long FTPGetFileSize(string url)
        {
            long fileSize = 0;
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(url));
                request.UseBinary = true;
                request.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                fileSize = response.ContentLength;

                response.Close();
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
            }
            return fileSize;
        }

        /// <summary>
        /// 从ftp服务器删除文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool FTPDeleteFile(string file)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(file));
                request.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
                return true;
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从ftp服务器删除空文件夹
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool FTPDeleteEmptyDir(string dir)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(dir));
                request.Credentials = new NetworkCredential(FTP_UserName, FTP_Password);
                request.Method = WebRequestMethods.Ftp.RemoveDirectory;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
                return true;
            }
            catch (Exception ex)
            {
                AddNLogInfo(EN_WARN_LEVEL.Error, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从ftp服务器删除文件/文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool FTPDeleteQuietly(string path)
        {
            return false;
        }
        #endregion
    }
}
