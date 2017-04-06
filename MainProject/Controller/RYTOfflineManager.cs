//
//  RYTOfflineStorageManager
//
//  Created by wang.ping on 6/22/12.
//  Copyright 2011 RYTong. All rights reserved.
//
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using RYTong.TLSLib;
using RYTong.DataBaseLib;
using RYTong.MainProject.Model;
using RYTong.ControlLib;
using System.IO.IsolatedStorage;
using RYTong.ControlLib.HelperClass.SharpGIS;
using System.Net;
using RYTong.Controller.GB2312;
using System.Diagnostics;
using RYTong.LogLib;
using System.Windows.Media;
using System.Threading;
using SharpCompress.Archive;
using System.Threading.Tasks;

namespace RYTong.MainProject.Controller
{
    /// <summary>
    /// offline V0
    /// 1. 每次下载错误retry次数为DownloadMaxRetryTime
    /// 2. 下载zip包叫PLUG_IN，下载单独的文件叫Offline。每个根目录下会保存一个des文件，在读取文件的时候做sha1检查和是否加密处理。
    /// 3. 防止下载完成部分，而后面失败情况：每下载一个单独的文件后会重写写一下本地和给服务器对比的desc文件。
    /// 
    /// offline V1
    /// 1. 已下载的文件信息存放在 Local， 服务器需要下载的文件信息保存在 Server
    /// 2. 程序进入后初始化 Local 和　Server 信息， Server 用于文件下载，Local用于文件读取。
    /// 3. 
    /// </summary>
    public class RYTOfflineBase
    {
        #region Fileds
        public const string EMPJSBridge = "EMPJSBridge-1.0.0.js";
        public const string JSON_FROM_SERVER = DataBaseManager.JSON_FROM_SERVER;
        public const string Client_DESC = DataBaseManager.Client_DESC;
        public const string Client_OPT_DESC = DataBaseManager.Client_OPT_DESC;
        public static string OFFLINE_FOLDER_NAME = DataBaseManager.OFFLINE_FOLDER_NAME;
        public static string PLUG_IN_FOLDER_NAME = DataBaseManager.PLUG_IN_FOLDER_NAME;        
        public const string Option_DESC = DataBaseManager.Option_DESC;
        public const string Option_server_DESC = DataBaseManager.Option_server_DESC;
        public const int DownloadMaxRetryTime = 3;
        public const string APP_NAME = "appname";
        public const string INFO = "info";
        protected DateTime dtStart;
        protected object syncObj = new object();        
        #endregion
        #region Properties

        public bool HttpBusy { get; protected set; }

        public string JsonFromServer { get; protected set; }

        public V1OfflineJsonClass Server { get; protected set; }

        public V1OfflineJsonClass Local { get; protected set; }

        // 3.0
        public string AppName { get; protected set; }
        #endregion        
        public delegate void OfflineUpdate_DescCompletedHandler(RYTOfflineBase sender, int mustUpdate);
        public event OfflineUpdate_DescCompletedHandler Update_DescCompleted;
        public delegate void OfflineUpdateCompletedHandler(RYTOfflineBase sender, bool result, string json, string mustUpdate);
        public event OfflineUpdateCompletedHandler UpdateCompleted;        
        #region protected
        protected virtual void OnCompleted(bool result, string mustValue = "")
        {
            if (UpdateCompleted != null)
                UpdateCompleted(this, result, JsonFromServer, mustValue);
            HttpBusy = false;
        }
        /// <summary>
        /// 用于 描述文件下载解析完成后的回掉
        /// </summary>
        /// <param name="result">是否成功获取描述文件</param>
        protected void OnUpdateDescCompleted(bool result, int mustUpdate = -2)
        {
            if (Update_DescCompleted != null)
            {
                Update_DescCompleted(this, mustUpdate);
            }
            HttpBusy = false;
        }
        #endregion
        public virtual void InitOfflineManager()
        {
           
        }
        public virtual void V1EnsureDownloadFile(OfflineInfo info, string type, Action<byte[]> delayAction)
        {
        }
        #region ForPage  Version_0

        public virtual void ClientUpdateAction(bool justUpdate = true)
        {
        }

        public virtual void JustUpdateOfflineResource()
        {
        }

        public virtual void DownloadWithWebClient(OfflineDownloadInfo info, bool bSingleDownload = false, string name = "", string relatedPath = "", Action<bool> action = null)
        {
        }

        /// <summary>
        /// 根据OfflineResult.OptDownloadFileList返回对应json字符串
        /// </summary>
        /// <returns></returns>
        public virtual string GetServerDownloadOptJson()
        {
            return "{}";
        }

        /// <summary>
        /// 获取本地已下载的opt插件描述json文本
        /// </summary>
        /// <returns></returns>
        public virtual string GetClientDownloadOptJson()
        {
            return "{}";
        }

        /// <summary>
        /// 获取本地已下载的opt插件描述json文本(包含rev字段)
        /// </summary>
        /// <returns></returns>
        public virtual string GetClientDownloadOptDesc()
        {
            return "{}";
        }

        /// <summary>
        /// 获取服务器完整server.desc
        /// </summary>
        /// <returns></returns>
        public virtual string GetOfflineServerDesc()
        {
            return string.Empty;
        }

        /// <summary>
        /// 验证可选离线/插件资源是否可用(lua)
        /// </summary>
        /// <param name="zipFolderName"></param>
        /// <returns></returns>
        public virtual bool CheckOfflineFileAvailable(string fileName, string relatedPath)
        {
            return false;
        }

        /// <summary>
        /// 移除离线资源文件
        /// </summary>
        /// <param name="filePath">文件名/目录名</param>
        public virtual bool RemoveClientOfflineFile(string filePath)
        {
            return false;
        }

        #endregion        
        public virtual void Update_Hash(Action<int> action, Dictionary<string, string> parameter = null)
        {
        }
        public virtual void Update_Desc(string updateFlag = null, bool getDownloadList = false, Dictionary<string, string> parameter = null)
        {
        }
        /// <summary>
        /// Offline:update_resource 接口，下载必选资源
        /// </summary>
        public virtual void Update_Resource(Action<int, int> processCB, Action<List<object>> finishCB, Dictionary<string, string> parameter = null)
        {
        }
        /// <summary>
        /// 离线更新以及可选资源的下载 （程序会根据TCPPort情况判断下载方式）
        /// </summary>
        /// <param name="info">下载相关信息</param>
        /// <param name="name">下载文件名</param>
        /// <param name="type">下载类型（离线更新/可选资源下载）</param>
        /// <param name="delayAction">可选资源的回掉</param>
        public virtual void V1DownCommonOrOptionalFile(OfflineInfo info, string name, string type, Action<bool> delayAction, Dictionary<string, string> parameter = null)
        {
        }
        public virtual void DownloadOptionalFile(string fileName, Action<bool> callBack, Dictionary<string, string> parameter = null)
        {

        }
        /// <summary>
        /// 获取可选文件的 描述（只包含zip 名和 hash）
        /// </summary>
        /// <param name="path">client(本地已下载） / server（所有可选资源）</param>
        /// <returns></returns>
        public virtual Dictionary<string, string> GetOptNameRevDesc(string path)
        {
            return null;
        }
        public virtual bool V1CheckOfflineFileAvailable(string fileName)
        {
            return false;
        }
        /// <summary>
        /// 根据插件名 删除插件资源
        /// </summary>
        /// <param name="fileName">插件文件名</param>
        /// <returns>成功返回true</returns>
        public virtual bool V1RemoveOptionalFile(string fileName)
        {
            return false;
        }
        public virtual string GetCommentOfFile(string fileName)
        {
            return null;
        }        
        // 2.0 获取mustupdate
        public virtual int GetUpdateFlag(string json)
        { return 0; }  
        /// <summary>
        /// 根据文件名删除必选资源中的文件 （可以是插件zip包，可以为普通资源，也可以为插件包内资源）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>删除成功返回true</returns>
        public virtual bool RemoveOfflineFile(string fileName)
        {
            return false;
        }                       
        protected static string ReadLocalServerDesc(string fileName)
        {
            var bytes = DataBaseManager.ReadBytesFile(fileName);
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }
            var result = RYTSecurity.Instance.Decrypt(bytes);
            RYTLog.Log("本地读取client.desc\r\n" + result);

            return result;
        }        
        protected static void ReadAndWriteEMPJSToIS()
        {
            if (DataBaseManager.IsFileExistInISO(EMPJSBridge))
            {
                return;
            }

            string js = string.Empty;
            var streamInfo = Application.GetResourceStream(new Uri(@"Resources\EMPJSBridge-1.0.0.js", UriKind.RelativeOrAbsolute));
            if (streamInfo != null)
            {
                using (StreamReader reader = new StreamReader(streamInfo.Stream))
                {
                    js = reader.ReadToEnd();
                }
                streamInfo.Stream.Dispose();
            }

            if (!string.IsNullOrEmpty(js))
            {
                DataBaseManager.OpenOrCreateFileAndWriteData(EMPJSBridge, js);
            }
        }        
    }
    public class RYTOfflineZero : RYTOfflineBase
    {    
        public RYTOfflineZero() { InitOfflineManager(); }

        #region 

        List<OfflinePathRevInfo> SavedFilePathRevList;

        public OfflineJsonClass OfflineResult { get; private set; }
        
        #endregion
        protected override void OnCompleted(bool result, string mustValue = "")
        {
            mustValue = OfflineResult != null ? ((int)OfflineResult.MustUpdate).ToString() : string.Empty;
            base.OnCompleted(result, mustValue);
        }

        #region 初始化离线数据

        public override void InitOfflineManager()
        {
            if (DataBaseManager.IsFileExistInISO(Client_DESC))
                return;
            //- 导入本地资源至离线存储空间...
            var firstLocalServerDesc = DataBaseManager.ReadAppPackageDesc() as string;
            if (string.IsNullOrEmpty(firstLocalServerDesc))
                firstLocalServerDesc = DataBaseManager.ReadAppPackageFile("Offline/offline.json", "text") as string;
            if (string.IsNullOrEmpty(firstLocalServerDesc))
                firstLocalServerDesc = DataBaseManager.ReadAppPackageFile("Offline/emas-wp-320-480.desc", "text") as string;
            if (string.IsNullOrEmpty(firstLocalServerDesc))
                firstLocalServerDesc = DataBaseManager.ReadAppPackageFile("Offline/ebank-wp-320-480.desc", "text") as string;
            if (!string.IsNullOrEmpty(firstLocalServerDesc))
            {
                DataBaseManager.WriteFileWithEncrpyt(JSON_FROM_SERVER, firstLocalServerDesc);
                OfflineResult = DataBaseManager.ParserDescData(firstLocalServerDesc);
                if (OfflineResult != null)
                {
                    var allFileList = OfflineResult.PathRevList;
                    List<OfflinePathRevInfo> SavedFileRevList = new List<OfflinePathRevInfo>();
                    foreach (var file in allFileList)
                    {
                        if (!file.DownloadType)
                            continue;

                        if (file.Name.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (file.DESC == null)
                                continue;

                            var zipStream = DataBaseManager.ReadAppPackageFile("Offline/" + file.Name, "current") as Stream;
                            if (zipStream != null)
                            {
                                if (RYTMainHelper.CompareSHA1(zipStream, file.Rev))
                                {
                                    bool bFirstItem = true;
                                    string zipFullFolderName = string.Empty;
                                    List<OfflineFileInfo> fileList = new List<OfflineFileInfo>();
                                    Dictionary<string, string> savedZipFilesRevDict = new Dictionary<string, string>();
                                    RYTUnZipper unzipper = new RYTUnZipper(zipStream);
                                    foreach (var entry in unzipper.FileNamesInZip)
                                    {
                                        var fileStream = unzipper.GetFileStream(entry);
                                        if (fileStream != null && file.DESC.ContainsKey(entry))
                                        {
                                            string fileNameToAdd = DataBaseManager.PLUG_IN_FOLDER_NAME + "/" + entry;
                                            OfflineResult.FileList.Add(fileNameToAdd);
                                            var fileRevQuery = file.DESC[entry];

                                            if (bFirstItem)
                                            {
                                                bFirstItem = false;

                                                int indexSpit = entry.IndexOf("/");
                                                if (indexSpit != -1)
                                                {
                                                    zipFullFolderName = DataBaseManager.PLUG_IN_FOLDER_NAME + "/" + entry.Substring(0, entry.IndexOf("/"));
                                                    // 首先删除原有的zip包,如果有的话：
                                                    DataBaseManager.DeleteDirectory(zipFullFolderName);
                                                }
                                            }

                                            var fileByes = RYTMainHelper.StreamToBytes(fileStream);
                                            if (RYTMainHelper.CompareSHA1(fileByes, fileRevQuery))
                                            {
                                                OfflineFileInfo fInfo = new OfflineFileInfo();
                                                fInfo.Name = fileNameToAdd;
                                                fInfo.Rev = fileRevQuery;

                                                if (file.Encrypt || (file.EncryptFileList != null && file.EncryptFileList.Contains(entry)))
                                                {
                                                    fInfo.Encrypt = true;
                                                    var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                                                    DataBaseManager.SaveBytesFile(encryptData, entry, DataBaseManager.PLUG_IN_FOLDER_NAME);
                                                }
                                                else
                                                {
                                                    DataBaseManager.SaveBytesFile(fileByes, entry, DataBaseManager.PLUG_IN_FOLDER_NAME);
                                                }

                                                fileList.Add(fInfo);
                                                savedZipFilesRevDict.Add(entry, fileRevQuery);
                                            }
                                            else
                                            {
                                                RYTLog.Log(entry + " SHA1值校验出错！");
                                                OfflineResult.FileList.Remove(fileNameToAdd);
                                            }
                                            fileStream.Dispose();
                                        }
                                    }
                                    unzipper.Dispose();

                                    // 在每个Zip包的根目录记录下来文件列表,用来读取的时候做sha1检查和判断是否需要加密：
                                    var fileListJson = JsonConvert.SerializeObject(fileList);
                                    if (!string.IsNullOrEmpty(fileListJson))
                                    {
                                        var encryptData = RYTSecurity.Instance.Encrypt(fileListJson);
                                        var desFilePath = zipFullFolderName + "/" + DataBaseLib.DataBaseManager.FileDesInFolderName;
                                        DataBaseManager.WriteFile(desFilePath, encryptData);
                                    }

                                    // 保存Server.des文件所需要的zip信息对象：
                                    file.DESC = savedZipFilesRevDict;

                                    // 保存实际保存到本地的文件列表(Client.desc)，用来与服务器对比用
                                    SavedFileRevList.Add(file);
                                    DataBaseManager.SaveResourceDescFile(SavedFileRevList, Client_DESC);
                                }
                                else
                                {
                                    RYTLog.Log("ShA1值校验失败：" + file.Name);
                                }
                                zipStream.Dispose();
                            }
                            else
                            {
                                RYTLog.Log("找不到离线文件:" + file.Name);
                            }
                        }
                        else
                        {
                            var fileStream = DataBaseManager.ReadAppPackageFile("Offline/" + file.Name, "current") as Stream;
                            if (fileStream != null)
                            {
                                var fileByes = RYTMainHelper.StreamToBytes(fileStream);
                                if (RYTMainHelper.CompareSHA1(fileStream, file.Rev))
                                {
                                    OfflineResult.FileList.Add(file.Name);
                                    string fileName = OFFLINE_FOLDER_NAME + "/" + file.Name;

                                    if (file.Encrypt) // 加密
                                    {
                                        var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                                        DataBaseManager.SaveBytesFile(encryptData, fileName);
                                    }
                                    else
                                    {
                                        DataBaseManager.SaveBytesFile(fileByes, fileName);
                                    }

                                    var newWriteInfo = new OfflineFileInfo() { Name = fileName, Encrypt = file.Encrypt, Rev = file.Rev };

                                    // 每下完一个文件就保存一次filesdesc.dat文件，防止出错：
                                    SaveOfflineFolderFileDes(newWriteInfo);

                                    // 保存Server.des文件所需要的信息对象：
                                    SavedFileRevList.Add(file);

                                    // 保存实际保存到本地的文件列表，用来与服务器对比用
                                    DataBaseManager.SaveResourceDescFile(SavedFileRevList, Client_DESC);
                                }
                                else
                                {
                                    RYTLog.Log("ShA1值校验失败：" + file.Name);
                                }

                                fileStream.Dispose();
                            }
                            else
                            {
                                RYTLog.Log("找不到离线文件:" + file.Name);
                            }
                        }
                    }
                }
            }
        }

        #endregion
        
        #region ForPage

        public override void ClientUpdateAction(bool justUpdate = true)
        {
            if (!HttpBusy)
            {
                var url = string.Format("{0}ota/resource_update", ConfigManager.SERVER_URL_WITH_PORT);
                #region Body

                StringBuilder bodySB = new StringBuilder();
                bodySB.Append(string.Format("{0}&platform=wp&resolution={1}*{2}", ConfigManager.APP_VERSION, RYTong.ControlLib.Constant.ScreenWidth, RYTong.ControlLib.Constant.ScreenHeight));

                string clientdesc = ReadLocalServerDesc(Client_DESC);
                if (!string.IsNullOrEmpty(clientdesc))
                {
                    RYTLog.Log("本地clientdesc文件: \r\n" + clientdesc);
                    SavedFilePathRevList = DataBaseManager.ParserResourceDesc(clientdesc);
                    bodySB.Append("&desc=" + clientdesc);
                }

                if (SavedFilePathRevList == null)
                {
                    SavedFilePathRevList = new List<OfflinePathRevInfo>();
                }

                #endregion
                ClientUpdate(url, bodySB.ToString(), justUpdate);

                HttpBusy = true;
            }
        }

        public override void JustUpdateOfflineResource()
        {
            dtStart = DateTime.Now;
            DownloadResourceFiles();
        }

        public override void DownloadWithWebClient(OfflineDownloadInfo info, bool bSingleDownload = false, string name = "", string relatedPath = "", Action<bool> action = null)
        {
            if (bSingleDownload && OfflineResult == null)
            {
                OfflineResult = ReadAndParserServerJsonInLocal();
            }

            if (OfflineResult == null || OfflineResult.PathRevList == null || string.IsNullOrEmpty(OfflineResult.Host))
            {
                if (action != null)
                    action(false);

                return;
            }

            if (bSingleDownload && !string.IsNullOrEmpty(name))
            {
                info = OfflineResult.OptDownloadFileList.SingleOrDefault(c => c.FileName.Equals(name) && c.FilePath == relatedPath);
            }

            if (info == null)
            {
                if (action != null)
                    action(false);

                return;
            }

            string url = string.Format("{0}{1}/{2}?{3}", OfflineResult.Host, info.FilePath, info.FileName, Guid.NewGuid().ToString());

            WebClient dowloadClient = new WebClient();
            dowloadClient.OpenReadCompleted += (s, e) =>
            {
                bool bSuccess = true;
                if (e.Error != null)
                {
                    Debug.WriteLine("*下载失败:* " + url);
                    bSuccess = false;
                }
                else if (e.Result != null)
                {
                    var revQuery = OfflineResult.PathRevList.SingleOrDefault(c => c.Name.Equals(info.FileName));
                    if (revQuery != null)
                    {
                        #region ZIP包情况：存在PLUG_IN_FOLDER_NAME文件夹下
                        if (info.FileName.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) && revQuery.DESC != null)
                        {
                            if (RYTMainHelper.CompareSHA1(e.Result, revQuery.Rev))
                            {
                                List<OfflineFileInfo> fileList = new List<OfflineFileInfo>();
                                string folderName = PLUG_IN_FOLDER_NAME;
                                RYTUnZipper unzipper = new RYTUnZipper(e.Result);
                                string zipFullFolderName = folderName;// +"/" + info.FileName.Substring(0, info.FileName.Length - ".zip".Length);
                                //string zipFullFolderName = string.Empty;
                                bool bFirstItem = true;
                                // 此dict用来处理这种情况：zip包中有的des信息，而实际下载却没有的文件或者sha1值对比失败的文件
                                Dictionary<string, string> savedZipFilesRevDict = new Dictionary<string, string>();

                                foreach (var entry in unzipper.FileNamesInZip)
                                {
                                    var fileStream = unzipper.GetFileStream(entry);
                                    if (fileStream != null && revQuery.DESC.ContainsKey(entry))
                                    {
                                        string fileNameToAdd = folderName + "/" + entry;
                                        OfflineResult.FileList.Add(fileNameToAdd);
                                        var fileRevQuery = revQuery.DESC[entry];

                                        if (bFirstItem)
                                        {
                                            bFirstItem = false;

                                            //zipFullFolderName = folderName + "/" + (entry.IndexOf("/") == -1 ? entry : entry.Substring(0, entry.IndexOf("/")));
                                            int indexSpit = entry.IndexOf("/");
                                            if (indexSpit != -1)
                                            {
                                                zipFullFolderName = folderName + "/" + entry.Substring(0, entry.IndexOf("/"));
                                                // 首先删除原有的zip包,如果有的话：
                                                DataBaseManager.DeleteDirectory(zipFullFolderName);
                                            }
                                        }

                                        var fileByes = RYTMainHelper.StreamToBytes(fileStream);
                                        if (RYTMainHelper.CompareSHA1(fileByes, fileRevQuery))
                                        {
                                            info.Downloaded = true;
                                            OfflineFileInfo fInfo = new OfflineFileInfo();
                                            fInfo.Name = fileNameToAdd;
                                            fInfo.Rev = fileRevQuery;

                                            if (revQuery.Encrypt || (revQuery.EncryptFileList != null && revQuery.EncryptFileList.Contains(entry)))
                                            {
                                                fInfo.Encrypt = true;
                                                var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                                                DataBaseManager.SaveBytesFile(encryptData, entry, folderName);
                                            }
                                            else
                                            {
                                                DataBaseManager.SaveBytesFile(fileByes, entry, folderName);
                                            }

                                            fileList.Add(fInfo);
                                            savedZipFilesRevDict.Add(entry, fileRevQuery);
                                        }
                                        else
                                        {
                                            bSuccess = false;
                                            RYTLog.ShowMessage(entry + " SHA1值校验出错！");
                                            OfflineResult.FileList.Remove(fileNameToAdd);
                                        }
                                        fileStream.Dispose();
                                    }
                                }
                                unzipper.Dispose();

                                // 在每个Zip包的根目录记录下来文件列表,用来读取的时候做sha1检查和判断是否需要加密：
                                var fileListJson = JsonConvert.SerializeObject(fileList);
                                if (!string.IsNullOrEmpty(fileListJson))
                                {
                                    var encryptData = RYTSecurity.Instance.Encrypt(fileListJson);
                                    var desFilePath = zipFullFolderName + "/" + DataBaseLib.DataBaseManager.FileDesInFolderName;
                                    DataBaseManager.WriteFile(desFilePath, encryptData);
                                }

                                // 保存Server.des文件所需要的zip信息对象：
                                revQuery.DESC = savedZipFilesRevDict;

                                if (bSingleDownload)
                                {
                                    // lua接口更新可选插件资源
                                    List<OfflinePathRevInfo> onlyOPtZipList;
                                    var localOptDesc = ReadLocalServerDesc(Client_OPT_DESC);
                                    if (!string.IsNullOrEmpty(localOptDesc))
                                    {
                                        onlyOPtZipList = DataBaseManager.ParserResourceDesc(localOptDesc);
                                        onlyOPtZipList.RemoveAll(c => c.Name.Equals(revQuery.Name) && c.Path.Equals(revQuery.Path));
                                    }
                                    else
                                    {
                                        onlyOPtZipList = new List<OfflinePathRevInfo>();
                                    }
                                    onlyOPtZipList.Add(revQuery);
                                    DataBaseManager.SaveResourceDescFile(onlyOPtZipList, Client_OPT_DESC);
                                }
                                else
                                {
                                    // 保存实际保存到本地的文件列表(Server.desc)，用来与服务器对比用
                                    SavedFilePathRevList.RemoveAll(c => c.Name.Equals(revQuery.Name) && c.Path.Equals(revQuery.Path));
                                    SavedFilePathRevList.Add(revQuery);
                                    DataBaseManager.SaveResourceDescFile(SavedFilePathRevList, Client_DESC);
                                }
                            }
                            else
                            {
                                bSuccess = !bSingleDownload;
                                info.Downloaded = true;
                                RYTLog.ShowMessage(info.FileName + " SHA1值校验出错！");
                            }
                        }
                        #endregion

                        #region 普通文件情况：存在OFFLINE_FOLDER_NAME文件夹下
                        else if (!info.FileName.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(revQuery.Rev))
                        {
                            var fileByes = RYTMainHelper.StreamToBytes(e.Result);
                            string fileName = OFFLINE_FOLDER_NAME + "/" + info.FileName;

                            if (RYTMainHelper.CompareSHA1(e.Result, revQuery.Rev))
                            {
                                OfflineResult.FileList.Add(fileName);

                                if (revQuery.Encrypt) // 加密
                                {
                                    var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                                    DataBaseManager.SaveBytesFile(encryptData, fileName);
                                }
                                else
                                {
                                    DataBaseManager.SaveBytesFile(fileByes, fileName);
                                }
                                info.Downloaded = true;

                                var newWriteInfo = new OfflineFileInfo() { Name = fileName, Encrypt = revQuery.Encrypt, Rev = revQuery.Rev };

                                // 每下完一个文件就保存一次filesdesc.dat文件，防止出错：
                                SaveOfflineFolderFileDes(newWriteInfo);

                                SavedFilePathRevList.RemoveAll(c => c.Name.Equals(revQuery.Name) && c.Path.Equals(revQuery.Path));
                                // 保存Server.des文件所需要的信息对象：
                                SavedFilePathRevList.Add(revQuery);

                                // 保存实际保存到本地的文件列表，用来与服务器对比用
                                DataBaseManager.SaveResourceDescFile(SavedFilePathRevList, Client_DESC);
                            }
                            else
                            {
                                bSuccess = true;
                                info.Downloaded = true;
                                RYTLog.ShowMessage(info.FileName + " SHA1值校验出错！");
                            }
                        }
                        #endregion
                    }

                    e.Result.Dispose();
                }

                if (!bSingleDownload)
                {
                    DownloadResourceFileAction();
                }

                if (action != null)
                {
                    action(bSuccess);
                }
            };

            info.DownloadTimes++;
            dowloadClient.OpenReadAsync(new Uri(url, UriKind.RelativeOrAbsolute));
            RYTLog.Log("下载: " + url);
        }

        /// <summary>
        /// 根据OfflineResult.OptDownloadFileList返回对应json字符串
        /// </summary>
        /// <returns></returns>
        public override string GetServerDownloadOptJson()
        {
            if (OfflineResult == null)
                OfflineResult = ReadAndParserServerJsonInLocal();

            if (OfflineResult == null || OfflineResult.OptDownloadFileList == null || OfflineResult.OptDownloadFileList.Count == 0)
            {
                return "{}";
            }

            StringBuilder sb = new StringBuilder("{");
            foreach (var f in OfflineResult.OptDownloadFileList)
            {
                sb.Append(string.Format("\"{0}\":\"{1}\",", f.FileName, f.FilePath));
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// 获取本地已下载的opt插件描述json文本
        /// </summary>
        /// <returns></returns>
        public override string GetClientDownloadOptJson()
        {
            var client_opt_json = ReadLocalServerDesc(Client_OPT_DESC);
            if (!string.IsNullOrEmpty(client_opt_json))
            {
                var offFileList = DataBaseManager.ParserResourceDesc(client_opt_json);
                if (offFileList != null && offFileList.Count > 0)
                {
                    StringBuilder sb = new StringBuilder("{");
                    foreach (var f in offFileList)
                    {
                        sb.Append(string.Format("\"{0}\":\"{1}\",", f.Name, f.Path));
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("}");

                    return sb.ToString();
                }
            }

            return "{}";
        }

        /// <summary>
        /// 获取本地已下载的opt插件描述json文本(包含rev字段)
        /// </summary>
        /// <returns></returns>
        public override string GetClientDownloadOptDesc()
        {
            var client_opt_json = ReadLocalServerDesc(Client_OPT_DESC);
            if (!string.IsNullOrEmpty(client_opt_json))
            {
                return client_opt_json;
            }

            return "{}";
        }

        /// <summary>
        /// 获取服务器完整server.desc
        /// </summary>
        /// <returns></returns>
        public override string GetOfflineServerDesc()
        {
            return DataBaseManager.ReadFileWithDecrpyt(JSON_FROM_SERVER);
        }

        /// <summary>
        /// 验证可选离线/插件资源是否可用(lua)
        /// </summary>
        /// <param name="zipFolderName"></param>
        /// <returns></returns>
        public override bool CheckOfflineFileAvailable(string fileName, string relatedPath)
        {
            return DataBaseManager.CheckOfflineFileAvailable(fileName, relatedPath);
        }

        /// <summary>
        /// 移除离线资源文件
        /// </summary>
        /// <param name="filePath">文件名/目录名</param>
        public override bool RemoveClientOfflineFile(string filePath)
        {
            if (filePath.Contains('/'))
                return false;

            if (filePath.Contains('.'))
            {
                //暂不支持删除单个文件/内层目录
                return DataBaseManager.DeleteNormalOfflineFile(filePath);
            }
            else
            {
                //删除整个插件目录
                return DataBaseManager.DeletePlugInFolder(filePath);
            }
        }

        #endregion

        #region Client Update DESC

        /// <summary>
        /// 1.
        /// 在建立加密通道之后，客户端发送离线资源更新请求Client.update，
        /// 请求包含了客户端当前资源描述文件client.desc、
        /// 客户端平台类型platform（iphone、ipad、android）、
        /// 以及客户端屏幕分辨率width*height；
        /// 
        /// 2.
        /// 服务器将MustUpdate+download.desc+server.desc返回；
        /// 
        /// 3.	
        /// 客户端收到Server.update后，根据MustUpdate提示用户强制升级或者选择升级。
        /// 在决定升级后，根据下载描述文件download.desc中的下载地址（不经过加密通道）逐个下载列表中的资源文件到本地（同本地资源合并），
        /// 通过server.desc中资源的rev验证资源文件的正确性后，更新client.desc为server.desc；
        /// 如果用户选择不更新，则丢弃Server.update。
        /// </summary>
        private void ClientUpdate(string url, string bodySB, bool justUpdate)
        {
            //string url = "http://192.168.65.144:4002/ota/resource_update";            

            #region Body

            //StringBuilder bodySB = new StringBuilder();
            //bodySB.Append(string.Format("{0}&platform=wp&resolution={1}*{2}", ConfigManager.APP_VERSION, RYTong.ControlLib.Constant.ScreenWidth, RYTong.ControlLib.Constant.ScreenHeight));

            //string clientdesc = ReadLocalServerDesc(Client_DESC);
            //if (!string.IsNullOrEmpty(clientdesc))
            //{
            //    RYTLog.Log("本地clientdesc文件: \r\n" + clientdesc);
            //    SavedFilePathRevList = DataBaseManager.ParserResourceDesc(clientdesc);
            //    bodySB.Append("&desc=" + clientdesc);
            //}

            //if (SavedFilePathRevList == null)
            //{
            //    SavedFilePathRevList = new List<OfflinePathRevInfo>();
            //}

            #endregion

            HttpRequest request = new HttpRequest(url, bodySB.ToString());

            #region Http Failed

            request.OnFailed += (error, status) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (ConfigManager.bOfflineStartHint)
                        MessageBox.Show(error, ConfigManager.OfflineStorageError, MessageBoxButton.OK);

                    OnCompleted(false);
                });
            };

            #endregion

            #region Http Request Success

            request.OnSuccess += (result, temp, response, headers) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    RYTLog.Log("服务器返回:\r\n" + result);

                    if (string.IsNullOrEmpty(result))
                    {
                        return;
                    }

                    JsonFromServer = result;
                    // Save file:
                    DataBaseManager.WriteFileWithEncrpyt(JSON_FROM_SERVER, JsonFromServer);

                    OfflineResult = DataBaseManager.ParserDescData(result);

                    // 首先做删除操作：
                    if (OfflineResult.DeleteFileList != null && OfflineResult.DeleteFileList.Count > 0)
                    {
                        var pathList = new List<string>();
                        foreach (var file in OfflineResult.DeleteFileList)
                        {
                            string fileName = OFFLINE_FOLDER_NAME + "/" + file;
                            if (file.EndsWith(".zip"))
                            {
                                DataBaseManager.DeleteDirectory(PLUG_IN_FOLDER_NAME + "/" + file.Substring(0, file.Length - 4));
                            }
                            else
                            {
                                DataBaseManager.DeleteFile(fileName);
                            }
                            pathList.Add(fileName);
                        }

                        //更新文件夹中的FileDesc.dat文件列表
                        UpdateOfflineFolderFileDes(pathList);

                        // 更新client.desc
                        SavedFilePathRevList.RemoveAll(c => OfflineResult.DeleteFileList.Contains(c.Name));

                        // 保存实际保存到本地的文件列表(client.desc)，用来与服务器对比用
                        DataBaseManager.SaveResourceDescFile(SavedFilePathRevList, Client_DESC);
                    }

                    if (OfflineResult == null)
                    {
                        OnCompleted(false);
                        if (ConfigManager.bOfflineStartHint)
                            MessageBox.Show(ConfigManager.OfflineReturnedDataFormattedError, ConfigManager.OfflineDebuggingTips, MessageBoxButton.OK);
                        return;
                    }

                    if (!justUpdate)
                    {
                        OnCompleted(false);
                        return;
                    }

                    if (!string.IsNullOrEmpty(OfflineResult.ErrorMessage))
                    {
                        OnCompleted(false);
                        if (ConfigManager.bOfflineStartHint)
                            MessageBox.Show(OfflineResult.ErrorMessage, ConfigManager.Hint, MessageBoxButton.OK);
                        return;
                    }
                    else
                    {
                        bool toUpdate = false;
                        if (OfflineResult.MustUpdate == -1)
                        {
                            OnCompleted(false);
                            return;
                        }
                        else if (OfflineResult.MustUpdate == 0)
                        {
                            if (ConfigManager.bOfflineStartHint)
                            {
                                if (MessageBox.Show(ConfigManager.IsDownloadOfflineResource, ConfigManager.OfflineResourcesTips, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                {
                                    //if (OfflineResult.DownloadFileList != null && OfflineResult.DownloadFileList.Count > 0)
                                    toUpdate = true;
                                }
                            }
                            else
                            {
                                toUpdate = true;
                            }
                        }
                        else if (OfflineResult.MustUpdate == 1)
                        {
                            // 强制更新，不提示。
                            toUpdate = true;
                            if (ConfigManager.bOfflineStartHint)
                                MessageBox.Show(ConfigManager.IsDownloadOfflineResource, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
                        }

                        if (toUpdate)
                        {
                            JustUpdateOfflineResource();
                        }
                        else
                        {
                            OnCompleted(false);
                        }
                    }
                });
            };
            #endregion

            request.Run();
        }
        
        private void DownloadResourceFiles()
        {
            if (OfflineResult.DownloadFileList != null && OfflineResult.DownloadFileList.Any(f => f.Downloaded == false))
            {
                DownloadResourceFileAction(true);
            }
        }

        private void DownloadResourceFileAction(bool bFirstElement = false)
        {
            lock (syncObj)
            {
                OfflineDownloadInfo info = null;

                if (bFirstElement)
                {
                    info = OfflineResult.DownloadFileList[0];
                }
                else
                {
                    info = OfflineResult.DownloadFileList.FirstOrDefault(c => c.Downloaded == false && c.DownloadTimes < DownloadMaxRetryTime);

                    // 10、	下载完毕后存储server.desc文件。（改到每下载完一个存储）
                    if (info == null)
                    {
                        #region 新版添加deleted项，删除文件, 注销之前写的老代码
                        /*
                    var allLocalResourceFiles = DataBaseManager.GetAllFileNames_New(PLUG_IN_FOLDER_NAME);
                    if (allLocalResourceFiles != null && allLocalResourceFiles.Count > 0)
                    {
                        foreach (var f in allLocalResourceFiles)
                        {
                            if (!OfflineResult.FileList.Contains(f.FileFullPath))
                            {
                                DataBaseManager.DeleteFile(f.FileFullPath);
                            }
                        }
                    }
                     */
                        #endregion

                        //if (OfflineResult.DeleteFileList != null && OfflineResult.DeleteFileList.Count > 0)
                        //{
                        //    foreach (var file in OfflineResult.DeleteFileList)
                        //    {
                        //        string fileName = OFFLINE_FOLDER_NAME + "/" + file;
                        //        DataBaseManager.DeleteFile(fileName);
                        //    }

                        //    UpdateOfflineFolderFileDes(OfflineResult.DeleteFileList);
                        //}

                        var dtDura = DateTime.Now - dtStart;
                        Debug.WriteLine(string.Format("IO操作耗时：{0}", dtDura.ToString()));

                        ReadAndWriteEMPJSToIS();

                        if (OfflineResult.DownloadFileList.Any(c => c.DownloadTimes >= DownloadMaxRetryTime && c.Downloaded == false))
                        {
                            if (ConfigManager.bOfflineEndHint)
                                MessageBox.Show(ConfigManager.OfflineUpdatedWithUndownloadFiles, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
                            OnCompleted(false);
                        }
                        else
                        {
                            if (ConfigManager.bOfflineEndHint)
                                MessageBox.Show(ConfigManager.OfflineUpdated, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
                            OnCompleted(true);
                        }
                        return;
                    }
                }

                DownloadWithWebClient(info);
            }
        }

        #endregion
        
        #region Help Methods

        /// <summary>
        /// 读取并解析本地存储的连接服务器时返回的完整json信息对象
        /// </summary>
        /// <returns></returns>
        public OfflineJsonClass ReadAndParserServerJsonInLocal()
        {
            var json = GetOfflineServerDesc();
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return DataBaseManager.ParserDescData(json);
        }
        
        /// <summary>
        /// 保存（更新）离线存储文件夹的所有文件des文件(供读取文件时的防篡改功能)
        /// </summary>
        /// <param name="info"></param>
        private void SaveOfflineFolderFileDes(OfflineFileInfo info)
        {
            var fileName = OFFLINE_FOLDER_NAME + "/" + DataBaseManager.FileDesInFolderName;
            var list = DataBaseManager.ReadFileDesList(fileName);
            if (list == null)
            {
                list = new List<OfflineFileInfo>();
            }
            list.RemoveAll(c => c.Name.Equals(info.Name)); // 覆盖已存在的文件
            list.Add(info);
            list = list.Distinct(new OfflineFileInfoComparer()).ToList();

            var jsonStr = JsonConvert.SerializeObject(list);
            var encryptData = RYTSecurity.Instance.Encrypt(jsonStr);
            DataBaseManager.WriteFile(fileName, encryptData);
        }

        /// <summary>
        /// 删除离线存储文件夹的文件后更新des文件列表
        /// </summary>
        /// <param name="deletedFiles"></param>
        private void UpdateOfflineFolderFileDes(List<string> deletedFiles)
        {
            var fileName = OFFLINE_FOLDER_NAME + "/" + DataBaseManager.FileDesInFolderName;
            var list = DataBaseManager.ReadFileDesList(fileName);
            if (list == null)
            {
                return;
            }

            list.RemoveAll(c => deletedFiles.Contains(c.Name));
            if (list.Count == 0)
            {
                DataBaseManager.DeleteFile(fileName);
            }
            else
            {
                var jsonStr = JsonConvert.SerializeObject(list);
                var encryptData = RYTSecurity.Instance.Encrypt(jsonStr);
                DataBaseManager.WriteFile(fileName, encryptData);
            }
        }

        #endregion
    }
    /// <summary>
    /// offline V1
    /// 1. 已下载的文件信息存放在 Local， 服务器需要下载的文件信息保存在 Server
    /// 2. 程序进入后初始化 Local 和　Server 信息， Server 用于文件下载，Local用于文件读取。
    /// 3. 
    /// </summary>
    public class RYTOffline : RYTOfflineBase
    {
        public RYTOffline(string appName)
        {
            AppName = appName;
            InitOfflineManager();            
        }
        #region Fileds
        public const string H5_DESC = DataBaseManager.H5_DESC;
        public const string H5_FOLDER_NAME = DataBaseManager.H5_FOLDER_NAME;
        protected Dictionary<string, SocketClient> sockets = new Dictionary<string, SocketClient>();
        protected bool isTCPFailed = false;
        protected int failTimes = 0;  // 连续下载失败次数
        protected int downNum = 0;    // 已下载成功个数
        protected int mustDownloadFilesNum = 0;//必选资源总数(包括插件包内需单个下载的)    
        List<OfflineInfo> mustFileDownloadFaildList = new List<OfflineInfo>();
        List<OfflineInfo> H5FileDownloadFaildList = new List<OfflineInfo>();
        protected bool IsDownloadCompleted 
        { 
            get
            {
                if (Server.H5FileDic.Count > 0 && Server.FileDic.Count > 0)
                    return IsMustFileDownloadCompleted && isH5FileDownloadCompleted;
                else if (Server.H5FileDic.Count > 0 && Server.FileDic.Count == 0)
                    return isH5FileDownloadCompleted;
                else
                    return IsMustFileDownloadCompleted;

            }
        }
        protected bool IsMustFileDownloadCompleted = false;
        protected bool isH5FileDownloadCompleted = false;
        #endregion     
        public bool DownloadBusy { get; private set; }
        public JObject UpdateInfoJson { get; set; }
        public Dictionary<string, OfflineInfo> OldOptFileList { get; private set; }                      
        public Action<int, int> processAction;
        public Action<List<object>> finishAction;
        public Dictionary<string, object> allFailInfo;        
        protected override void OnCompleted(bool result, string mustValue = "")
        {
            List<object> downloadFailedList = new List<object>();
            foreach (var item in mustFileDownloadFaildList)
                downloadFailedList.Add(string.Format("{0}/{1}", item.Path, item.Name));
            foreach (var item in H5FileDownloadFaildList)
                downloadFailedList.Add(string.Format(@"h5/{0}", item.Name));
            if (finishAction != null)
                finishAction(downloadFailedList);
            if (downloadFailedList.Count == 0)
                DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, AppName, Server.Hash);
            base.OnCompleted(result);
            DownloadBusy = false;
            var dtDura = DateTime.Now - dtStart;
            Debug.WriteLine(string.Format("离线资源更新耗时：{0}", dtDura.ToString()));
        }        
        /// <summary>
        /// 确保更新文件下载（程序会根据TCPPort情况判断下载方式）
        /// </summary>
        /// <param name="info">下载信息</param>
        /// <param name="downloadType">下载类型 </param>
        /// <param name="delayAction">下载结束的回掉</param>
        /// <returns></returns>
        public override void V1EnsureDownloadFile(OfflineInfo info, string type, Action<byte[]> delayAction)
        {
            // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
            OfflineZipDLInfo zipInfo = null;
            string socketPath;
            byte[] fileByes = null;

            if (Server.FileDic.ContainsKey(info.Path))
                zipInfo = Server.FileDic[info.Path] as OfflineZipDLInfo;

            info.DownloadStatus = 1;

            #region 下载失败
            Action<string> failedAction = (errMsg) =>
            {
                Debug.WriteLine("*下载失败*: " + errMsg);
                info.DownloadTimes++;
                if (info.DownloadTimes < DownloadMaxRetryTime)
                {
                    V1EnsureDownloadFile(info, type, delayAction);
                    return;
                }
                info.DownloadStatus = 0;
                if (delayAction != null)
                {
                    delayAction(null);
                }
            };
            #endregion

            #region 下载成功
            Action<string, byte[]> successAction = (path, result) =>
            {
                fileByes = result;
                // 整个插件包时 不需要保存
                if (info.Name.EndsWith(".zip"))
                {
                    delayAction(fileByes);
                    return;
                }

                #region ZIP包情况：存在PLUG_IN_FOLDER_NAME文件夹下  // 如果是 zip 包中的文件  client.desc

                if (info.Path.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(info.Rev))
                {
                    string fileFullName = PLUG_IN_FOLDER_NAME + "/" + info.Name;
                    fileFullName = GetAppNamePath(fileFullName);
                    if (RYTMainHelper.CompareSHA1(result, info.Rev))
                    {
                        if (info.ParentInfo.Encrypt) // 加密
                        {
                            var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                            DataBaseManager.SaveBytesFile(encryptData, fileFullName);
                        }
                        else
                        {
                            DataBaseManager.SaveBytesFile(fileByes, fileFullName);
                        }

                        OfflineZipDLInfo zipFile;
                        if (Local.FileDic.ContainsKey(info.Path))
                        {
                            zipFile = Local.FileDic[info.Path] as OfflineZipDLInfo;
                        }
                        else
                        {
                            Dictionary<string, OfflineInfo> filesInZip = new Dictionary<string, OfflineInfo>();
                            zipFile = new OfflineZipDLInfo() { Name = info.Path, Encrypt = info.Encrypt, FilesInZip = filesInZip };
                            Local.FileDic[info.Path] = zipFile;
                        }
                        OfflineInfo fileInZip = new OfflineInfo() { Name = info.Name, Rev = info.Rev, Path = info.Path, Encrypt = info.Encrypt, ParentInfo = zipFile };
                        zipFile.FilesInZip[info.Name] = fileInZip;
                        Local.AllFilesInZipDic[info.Name] = fileInZip;

                        // 本地
                        DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, AppName);
                        info.DownloadStatus = 2;
                    }
                    else
                    {
                        RYTLog.ShowMessage(info.Name + " SHA1值校验出错！");
                    }
                }
                #endregion

                #region 普通文件情况：存在OFFLINE_FOLDER_NAME文件夹下    // 如果是 下载列表中的信息则 去做保存，修改下载状态等操作
                else if (!string.IsNullOrEmpty(info.Rev))
                {
                    if (RYTMainHelper.CompareSHA1(result, info.Rev))
                    {
                        string fileName = OFFLINE_FOLDER_NAME + "/" + info.Name;
                        fileName = GetAppNamePath(fileName);
                        if (info.Encrypt) // 加密
                        {
                            var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                            DataBaseManager.SaveBytesFile(encryptData, fileName);
                        }
                        else
                        {
                            DataBaseManager.SaveBytesFile(fileByes, fileName);
                        }
                        Local.FileDic[info.Name] = new OfflineInfo() { Name = info.Name, Rev = info.Rev, Encrypt = info.Encrypt };

                        DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, AppName);
                        info.DownloadStatus = 2;
                    }
                    else
                    {
                        RYTLog.ShowMessage(info.Name + " SHA1值校验出错！");
                    }
                }
                #endregion

                if (Server.FileDic.Values.All(c => c.DownloadStatus == 2))
                {
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, Server.Hash);
                }
                delayAction(fileByes);

                #region ImageShow

                //lock (PageDLFiles)
                //{
                //    if (name.EndsWith(".png"))  // 如果下载完成之后 都要查看 图片渲染列表中是否有需要 回调渲染的图片
                //    {
                //        // 显示图片
                //        if (bSuccess && ImgDelayActions.ContainsKey(name))
                //        {

                //            Dictionary<ImageBrush, Action<ImageBrush, byte[], string>> actions = RYTOfflineStorageManager.Instance.ImgDelayActions[name];
                //            if (actions != null && actions.Count > 0)
                //            {
                //                foreach (var acts in actions)
                //                {
                //                    acts.Value(acts.Key, fileByes, name);
                //                }
                //            }

                //            ImgDelayActions.Remove(name);
                //            if (PageDLFiles.ContainsKey(name))
                //            {
                //                PageDLFiles.Remove(name);
                //            }
                //        }

                //    }
                //    else if (downloadType.Equals(DownloadType.PageUpdate))
                //    {
                //        PageDLFiles.Remove(name);
                //    }

                //    if (imB == null)
                //    {
                //        V1DownloadResourceFileAction(false, delayAction);
                //    }
                //}

                #endregion
            };

            #endregion

            #region 离线2.0

            if (DataBaseManager.Offline_Version.CompareTo("2.0") > 0)
            {
                // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
                if (Server.TCPPort != 0)
                {
                    if (zipInfo != null)
                    {
                        socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", zipInfo.Path, info.Path.Replace(".zip", ""), info.Name);
                    }
                    else
                    {
                        socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", info.Path, info.Name);
                    }
                    if (!string.IsNullOrEmpty(AppName))
                        socketPath = socketPath.Insert(socketPath.Length - 1, string.Format(",\"from\":\"{0}\"", AppName));
                    string ip = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;
                    // ServerOfflineObj.ST == 0：TCP明文 1：TCP信道明文 2：TCP信道密文
                    SocketClient socketClient;
                    if (sockets.ContainsKey(type))
                    {
                        socketClient = sockets[type];
                    }
                    else
                    {
                        sockets[type] = socketClient = new SocketClient(ip, Server.TCPPort, ConfigManager.Offline_Version, Server.ST);
                    }
                    //{"path":"ebank/resources/wp/common/css/test.css"}
                    socketClient.Start(socketPath, successAction, failedAction);

                    RYTLog.Log("Socket下载: " + socketPath);
                }
            }

            #endregion

            #region 离线1.0

            else
            {
                if (Server.TCPPort != 0 && !isTCPFailed)
                {
                    if (zipInfo != null)
                    {
                        socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", zipInfo.Path, info.Path.Replace(".zip", ""), info.Name);
                    }
                    else
                    {
                        socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", info.Path, info.Name);
                    }
                    string ip = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;

                    SocketClient socketClient;
                    if (sockets.ContainsKey(type))
                    {
                        socketClient = sockets[type];
                    }
                    else
                    {
                        sockets[type] = socketClient = new SocketClient(ip, Server.TCPPort, ConfigManager.Offline_Version);
                    }
                    //{"path":"ebank/resources/wp/common/css/test.css"}
                    socketClient.Start(socketPath, successAction, failedAction);

                    RYTLog.Log("Socket下载: " + socketPath);

                }
            }

            #endregion

            info.DownloadTimes++;

        }       
        /// <summary>
        /// 初始化本地文件列表，当离线协议版本高于本地资源时，对本地资源进行升级。
        /// </summary>
        public override void InitOfflineManager()
        {
            #region 升级逻辑
            //   如果是第一次启动：本地没有离线文件时：保存预置资源中的描述文件，创建需要的文件夹
            //       本地离线版本为0时：如果预置资源为0：直接升级本离线资源，无视包中资源
            //                          如果预置资源为1：删除已下载必选资源，删除与预置资源冲突的本地可选资源（包括已下载的，以及可选列表中不存在的），升级本地描述与预置描述合并后保存
            //                                           
            //       本地        为1时：删除与预置资源冲突的已下载文件，合并描述后保存
            //  （升级离线资源之后，删除安装包中的 描述）
            //   不是第一次启动：本地为 0： 转换旧描述为新类型 ，删除不需要的文件
            //                   本地为 1： 将描述 转为 Local/Server  offlineobj 
            //  
            //  启动初始化时，必选资源哈希保存在 LocalObj中 可选哈希保存在 OfflineZipDLInfo 中，
            //  可能是为了防止与服务器同步下载列表中哈希混淆（后期如果没有问题可以改为保存在ServerObj中）
            #endregion
            string packageDesc = string.Empty;
            #region 如果当前离线资源版本为0，则将 离线资源 0 版本 升级到 1
            // 2.0预置资源 是经过加密的
            if (ConfigManager.Offline_Version.CompareTo("2.0") >= 0)
            {
                packageDesc = DataBaseManager.ReadAppPackageDescWithDecrypt() as string;
            }
            else
            {
                packageDesc = DataBaseManager.ReadAppPackageDesc() as string;
            }
            string clientDesc = string.Empty;
            string optSavedDesc = string.Empty;
            string resourceVersion = DataBaseManager.V1GetResourceOfflineVersion(AppName, out clientDesc, out optSavedDesc);
            // 第一次启动 (说明有预置资源）
            if (packageDesc != null)
            {
                string packageVersion = DataBaseManager.V1GetPackageOfflineVersion(packageDesc);
                DataBaseManager.Server = Server = new V1OfflineJsonClass();
                #region 无本地资源：保存预置资源中的描述文件，创建需要的文件夹
                if (resourceVersion == null)
                {
                    Local = DataBaseManager.V1PreDescToOfflineObj(packageDesc, ConfigManager.SERVER_URL_WITH_PORT);
                    DataBaseManager.WriteDesc(Option_server_DESC, Server.OptionDesc, AppName);
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, Local.Hash);
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.H5FileDic, H5_DESC, Local.H5Hash);
                    if (Server.OptDownloadFileDic.Values.All(c => c.DownloadStatus == 2))
                    {
                        Server.Hash = OfflineZipDLInfo.Hash;
                    }
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.OptSavedFileDic, Option_DESC, Server.Hash);
                    DataBaseManager.CreateDirectory(PLUG_IN_FOLDER_NAME);
                    DataBaseManager.CreateDirectory(OFFLINE_FOLDER_NAME);
                    DataBaseManager.CreateDirectory(H5_FOLDER_NAME);
                }
                #endregion
                #region 本地离线版本为0时：删除已下载必选资源，删除与本地冲突的可选资源，升级本地描述与预置描述合并后保存
                else if (resourceVersion.Equals("0"))
                {
                    V1OfflineJsonClass offlineObj = DataBaseManager.V1OldDescToNewOfflineObj(clientDesc, optSavedDesc);
                    // (这种情况应该不应该出现)包资源为 0： 直接升级本地离线资源，忽视包中资源
                    if (packageVersion.Equals("0"))
                    {
                        DataBaseManager.Local = Local = offlineObj;
                        DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, AppName);
                        DataBaseManager.V1SaveDownloadedFileDesc(Local.OptSavedFileDic, Option_DESC, AppName);
                        // 由于可选资源0没有哈希值，因此必定需要更新，可选资源描述；
                        // 删除旧描述文件
                        DataBaseManager.DeleteFile(JSON_FROM_SERVER);
                        DataBaseManager.DeleteFile(Client_OPT_DESC);
                        DataBaseManager.DeleteFile(Option_server_DESC);
                    }
                    // 包中资源为 1： 删除已下载必选资源，删除与本地冲突的可选资源，升级本地描述与预置描述合并后保存
                    else
                    {
                        foreach (var item in offlineObj.FileDic)
                        {
                            if (item.Key.EndsWith(".zip"))
                            {
                                string zipFullFolderName = DataBaseManager.PLUG_IN_FOLDER_NAME + "/" + item.Key.Replace(".zip", "");
                                DataBaseManager.DeleteDirectory(zipFullFolderName);
                            }
                            else
                            {
                                string commFullFolderName = DataBaseManager.OFFLINE_FOLDER_NAME + "/" + item.Key;
                                DataBaseManager.DeleteFile(commFullFolderName);
                            }
                        }
                        DataBaseManager.DeleteFile(Client_DESC);
                        DataBaseManager.DeleteFile(DataBaseManager.OFFLINE_FOLDER_NAME + "/" + DataBaseManager.FileDesInFolderName);

                        Local = DataBaseManager.V1PreDescToOfflineObj(packageDesc, ConfigManager.SERVER_URL_WITH_PORT);
                        foreach (var item in offlineObj.OptSavedFileDic)
                        {
                            if (Local.OptSavedFileDic.ContainsKey(item.Key) || !Server.OptDownloadFileDic.ContainsKey(item.Key))
                            {
                                string zipFullFolderName = DataBaseManager.PLUG_IN_FOLDER_NAME + "/" + item.Key.Replace(".zip", "");
                                DataBaseManager.DeleteDirectory(zipFullFolderName);
                            }
                            else
                            {
                                Local.OptSavedFileDic[item.Key] = item.Value;
                                foreach (var fileInZip in (item.Value as OfflineZipDLInfo).FilesInZip)
                                {
                                    Local.AllFilesInZipDic[fileInZip.Key] = fileInZip.Value;
                                }
                                Server.OptDownloadFileDic[item.Key].DownloadStatus = 2;
                            }
                        }
                        // 保存
                        DataBaseManager.WriteDesc(Option_server_DESC, Server.OptionDesc,AppName);
                        DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, Local.Hash);
                        if (Server.OptDownloadFileDic.Values.All(c => c.DownloadStatus == 2))
                        {
                            Server.Hash = OfflineZipDLInfo.Hash;
                        }
                        DataBaseManager.V1SaveDownloadedFileDesc(Local.OptSavedFileDic, Option_DESC, Server.Hash);
                    }
                }
                #endregion
                #region  本地离线版本为1/2/2.1时：删除与预置资源冲突的已下载文件，合并描述后保存
                else// if (resourceVersion.Equals("1.0") || resourceVersion.Equals("2.0"))
                {
                    Local = DataBaseManager.V1PreDescToOfflineObj(packageDesc, ConfigManager.SERVER_URL_WITH_PORT);
                    V1OfflineJsonClass preLocalObj = Local;
                    V1OfflineJsonClass preServerObj = Server;
                    V1OfflineJsonClass local = null;
                    V1OfflineJsonClass server = null;
                    DataBaseManager.V1LocalDescToOfflineObj(AppName, out local, out server);
                    DataBaseManager.Local = Local = local;
                    DataBaseManager.Server = Server = server;
                    OfflineZipDLInfo zipInfo;
                    foreach (var item in Local.FileDic)
                    {
                        if (preLocalObj.FileDic.ContainsKey(item.Key))
                        {
                            if (item.Key.EndsWith(".zip"))
                            {
                                string zipFullFolderName = DataBaseManager.PLUG_IN_FOLDER_NAME + "/" + item.Key.Replace(".zip", "");
                                DataBaseManager.DeleteDirectory(zipFullFolderName);
                            }
                            else
                            {
                                string commFullFolderName = DataBaseManager.OFFLINE_FOLDER_NAME + "/" + item.Key;
                                DataBaseManager.DeleteFile(commFullFolderName);
                            }
                        }
                        else
                        {
                            preLocalObj.FileDic[item.Key] = item.Value;
                            zipInfo = item.Value as OfflineZipDLInfo;
                            if (zipInfo != null)
                            {
                                foreach (var fileInZip in zipInfo.FilesInZip)
                                {
                                    preLocalObj.AllFilesInZipDic[fileInZip.Key] = fileInZip.Value;
                                }
                            }
                        }
                    }
                    foreach (var item in Local.OptSavedFileDic)
                    {
                        if (preLocalObj.OptSavedFileDic.ContainsKey(item.Key) || !preServerObj.OptDownloadFileDic.ContainsKey(item.Key))
                        {
                            string zipFullFolderName = DataBaseManager.PLUG_IN_FOLDER_NAME + "/" + item.Key.Replace(".zip", "");
                            DataBaseManager.DeleteDirectory(zipFullFolderName);
                        }
                        else
                        {
                            preLocalObj.OptSavedFileDic[item.Key] = item.Value;
                            foreach (var fileInZip in (item.Value as OfflineZipDLInfo).FilesInZip)
                            {
                                preLocalObj.AllFilesInZipDic[fileInZip.Key] = fileInZip.Value;
                            }
                            preServerObj.OptDownloadFileDic[item.Key].DownloadStatus = 2;
                        }
                    }
                    foreach (var item in Local.H5FileDic)
                    {
                        if (preLocalObj.H5FileDic.ContainsKey(item.Key))
                        {
                            string commFullFolderName = DataBaseManager.OFFLINE_FOLDER_NAME + "/" + item.Key;
                            DataBaseManager.DeleteFile(commFullFolderName);
                        }
                        else
                        {
                            preLocalObj.H5FileDic[item.Key] = item.Value;
                            zipInfo = item.Value as OfflineZipDLInfo;
                        }
                    }
                    DataBaseManager.Local = Local = preLocalObj;
                    DataBaseManager.Server = Server = preServerObj;
                    // 保存
                    DataBaseManager.WriteDesc(Option_server_DESC, Server.OptionDesc, AppName);
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.H5FileDic, H5_DESC, Local.H5Hash);
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, Local.Hash);
                    if (Server.OptDownloadFileDic.Values.All(c => c.DownloadStatus == 2))
                    {
                        Server.Hash = OfflineZipDLInfo.Hash;
                    }
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.OptSavedFileDic, Option_DESC, Server.Hash);
                }
                #endregion
                DataBaseManager.DeleteAppPackageDesc();
            }
            // 不是升级后的第一次启动：将描述 转为 Local/Server  offlineobj 
            else
            {
                // 直接升级本地离线资源
                if (!string.IsNullOrEmpty(resourceVersion) && resourceVersion.Equals("0"))
                {
                    V1OfflineJsonClass local = null;
                    V1OfflineJsonClass server = null;
                    DataBaseManager.V1LocalDescToOfflineObj(AppName, out local, out server);
                    DataBaseManager.Local = Local = local;
                    DataBaseManager.Server = Server = server;
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, AppName);
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.OptSavedFileDic, Option_DESC, AppName);
                    // 由于可选资源0没有哈希值，因此必定需要更新，可选资源描述；
                    // 删除旧描述文件
                    DataBaseManager.DeleteFile(JSON_FROM_SERVER);
                    DataBaseManager.DeleteFile(Client_OPT_DESC);
                    DataBaseManager.DeleteFile(Option_server_DESC);
                }
                else
                {
                    V1OfflineJsonClass local = null;
                    V1OfflineJsonClass server = null;
                    DataBaseManager.V1LocalDescToOfflineObj(AppName, out local, out server);
                    DataBaseManager.Local = Local = local;
                    DataBaseManager.Server = Server = server;
                }

            }
            Local.AppName = Server.AppName = AppName;
            #endregion
        }
        private int UpdateServerInfo(string jsonStr, V1OfflineJsonClass server)
        {
            JObject jo = JObject.Parse(jsonStr);
            var tNode = jo["t"];
            if (tNode != null && tNode.Type == JTokenType.Integer)
                server.TCPPort = tNode.Value<int>();
            var stNode = jo["st"];
            if (stNode != null && stNode.Type == JTokenType.Integer)
                server.ST = stNode.Value<int>();
            // 离线版本 1 - 2 需要更新 host
            var hostNode = jo["h"];
            if (hostNode != null)
                server.Host = hostNode.Value<string>();
            var mNode = jo["m"];
            if (mNode != null && mNode.Type == JTokenType.Integer)
            {
                int updateFlag = mNode.Value<int>();
                return updateFlag;
            }
            return -1;
        }

        /// <summary>
        /// 请求resource_hash接口，获取离线资源的更新检测结果。
        /// 通过上传hash 获取下载标志（0，1，2，3）
        /// </summary>
        /// <param name="action">回调：成功为0 -- 3 失败为-1</param>
        public override void Update_Hash(Action<int> action, Dictionary<string, string> parameter = null)
        {
            if (action == null)
                return;            
            string appName = "";
            string info = "";
            if (parameter != null)
            {
                if (parameter.ContainsKey(APP_NAME))
                    appName = parameter[APP_NAME];
                if (parameter.ContainsKey(INFO))
                    info = parameter[INFO];
            }
            string must_hash = "";
            string option_hash = "";
            byte[] hashData = DataBaseManager.GetIsolatedObjectValue<byte[]>(appName + Client_DESC);
            if (hashData != null)
                must_hash = RYTSecurity.Instance.Decrypt(hashData);
            hashData = DataBaseManager.GetIsolatedObjectValue<byte[]>(appName + Option_server_DESC);
            if (hashData != null)
                option_hash = RYTSecurity.Instance.Decrypt(hashData);
            if (string.IsNullOrEmpty(must_hash) && string.IsNullOrEmpty(option_hash))
            {
                action(3);
                return;
            }
            string hashUrl = string.Format("{0}ota/resource_hash", ConfigManager.SERVER_URL_WITH_PORT);
            StringBuilder body = new StringBuilder();
            body.Append(string.Format("hash={0}&option_hash={1}&platform=wp&resolution={2}*{3}", must_hash, option_hash, ControlLib.Constant.ScreenWidth, RYTong.ControlLib.Constant.ScreenHeight));
            if (ConfigManager.Offline_Version.CompareTo("2.0") >= 0) 
                body.Append("&version=" + ConfigManager.Offline_Version);
            if (appName != "")
                body.Append("&from=" + appName);
            if (info != "")
                body.Append("&info=" + info);                    
            HttpRequest request = new HttpRequest(hashUrl, body.ToString());
            request.OnFailed += (error, status) =>
            {
                action(-1);
            };
            request.OnSuccess += (result, temp, response, headers) =>
            {
                if (!string.IsNullOrEmpty(result))
                {
                    if (result.Length == 1)
                    {
                        string[] results = { "-1", "0", "1", "2", "3" };
                        if (!results.Contains(result))
                        {
                            action(-1);
                        }
                        else
                        {
                            action(Convert.ToInt32(result));
                        }
                    }
                    else
                        action(UpdateServerInfo(result, this.Server));
                }
            };
            request.Run();
        }
        
        /// <summary>
        /// 通过tls 获取的 离线更新标志直接去下载描述信息，或者先去获取更新标志
        /// </summary>
        /// <param name="updateFlag">更新标志( 0, 1, 2, 3 ）</param>
        /// <param name="getDownloadList">true 表示获取下载列表接口调用</param>
        public override void Update_Desc(string updateFlag = null, bool getDownloadList = false, Dictionary<string, string> parameter = null)
        {
            string appName = "";
            string info = "";
            if (parameter != null)
            {
                if (parameter.ContainsKey(APP_NAME))
                    appName = parameter[APP_NAME];
                if (parameter.ContainsKey(INFO))
                    info = parameter[INFO];
            }  
            if (!HttpBusy && !string.IsNullOrEmpty(updateFlag))
            {
                if (updateFlag.Equals("0"))
                {
                    this.Server.MustUpdate = -1;
                    OnUpdateDescCompleted(true);
                }
                else
                {
                    string updateUrl = string.Format("{0}ota/resource_update", ConfigManager.SERVER_URL_WITH_PORT);

                    //  1.desc为客户端描述文件
                    //  2.platform为客户端平台，如android、iphone。
                    //  3.resolution为客户端分辨率，如640960、320480
                    //  4.version为离线协议版本，初始版本为0，现（EMP5.2及以上）版本为1
                    //  5.hash_res为resource_hash接口的返回值.如果客户端没有本地资源上传3.

                    #region BodySB                                   
                    StringBuilder bodySB = new StringBuilder();
                    string clientDesc = DataBaseManager.InfoDicToUploadDesc(this.Local.FileDic, Client_DESC);
                    if (!string.IsNullOrEmpty(clientDesc) && ConfigManager.Offline_Version.CompareTo("2.0") > 0)
                    {
                        string h5Json = DataBaseManager.InfoDicToUploadDesc(this.Local.H5FileDic, H5_DESC);
                        if (!string.IsNullOrEmpty(h5Json))
                            clientDesc = clientDesc.Insert(clientDesc.Length - 1, h5Json.Substring(0, h5Json.Length - 1));
                    }
                    if (!string.IsNullOrEmpty(clientDesc))
                    {
                        RYTLog.Log("本地clientdesc文件: \r\n" + clientDesc);
                        bodySB.Append(string.Format("desc={0}&", clientDesc));
                    }
                    bodySB.Append(string.Format("platform=wp&resolution={0}*{1}&version={2}&hash_res={3}",
                        RYTong.ControlLib.Constant.ScreenWidth,
                        RYTong.ControlLib.Constant.ScreenHeight,
                        ConfigManager.Offline_Version,
                        updateFlag));
                    if (appName != "")
                        bodySB.Append("&from=" + appName);
                    if (info != "")
                        bodySB.Append("&info=" + info);                                       
                    #endregion
                    //string url = "http://192.168.65.144:4002/ota/resource_update";        
                    HttpRequest request = new HttpRequest(updateUrl, bodySB.ToString());
                    #region Http Failed
                    request.OnFailed += (error, status) =>
                    {
                        RYTLog.Log("请求resource_update离线更新接口失败");
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (ConfigManager.bOfflineStartHint)
                                MessageBox.Show(error, ConfigManager.OfflineStorageError, MessageBoxButton.OK);
                            OnUpdateDescCompleted(false);
                        });
                    };
                    #endregion
                    #region Http Request Success
                    request.OnSuccess += (result, temp, response, headers) =>
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            Debug.WriteLine("服务器返回:\r\n" + result);
                            if (string.IsNullOrEmpty(result))
                            {
                                OnUpdateDescCompleted(false);
                                return;
                            }          
                            try
                            {
                                UpdateInfoJson = JObject.Parse(result);                                
                            }
                            catch
                            {
                                RYTLog.Log("服务器返回的更新信息解析错误");
                                return;
                            }                            
                            int mustUpdate = -1;
                            if (UpdateInfoJson["m"] != null && UpdateInfoJson["m"].Type == JTokenType.Integer)
                                mustUpdate = UpdateInfoJson["m"].Value<int>();
                            this.Server.MustUpdate = mustUpdate;              
                            //更新可选资源描述并持久化可选资源hash
                            DataBaseManager.UpdateServerOptionalResourceDesc(UpdateInfoJson, this.Server, appName);
                            DataBaseManager.WriteFileWithEncrpyt(appName + "/" + JSON_FROM_SERVER, result);
                            if (mustUpdate == -1)
                                Update(UpdateInfoJson);
                            if (getDownloadList)
                            {
                                switch (mustUpdate)
                                {
                                    case 0:
                                        if (MessageBox.Show(ConfigManager.IsDownloadOfflineResource, ConfigManager.OfflineResourcesTips, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                                        {
                                            HttpBusy = false;
                                            return;
                                        }
                                        break;
                                    case 1:
                                        MessageBox.Show(ConfigManager.IsDownloadOfflineResource, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
                                        break;
                                    default:
                                        break;
                                }
                                Update(UpdateInfoJson);
                            }
                            JsonFromServer = result;
                            OnUpdateDescCompleted(true, mustUpdate);
                            return;
                        });
                    };
                    #endregion
                    request.Run();
                }
                HttpBusy = true;
            }
            else
            {
                OnUpdateDescCompleted(false);
            }
        }
        /// <summary>
        /// Offline:update_resource 接口，下载必选资源
        /// </summary>
        public override void Update_Resource(Action<int, int> processCallBack, Action<List<object>> finishCallBack, Dictionary<string, string> parameter = null)
        {
            processAction = processCallBack;
            finishAction = finishCallBack;
            if (DownloadBusy || string.IsNullOrEmpty(JsonFromServer))
                return;
            DownloadBusy = true;            
            Update(UpdateInfoJson);
            #region 删除操作：
            if (this.Server.DeleteFileDic != null && this.Server.DeleteFileDic.Count > 0)
            {
                foreach (var item in this.Server.DeleteFileDic)
                {
                    RemoveOfflineFile(item.Key);
                }
            }

            if (this.Server.H5_DeleteList != null && this.Server.H5_DeleteList.Count > 0)
            {
                foreach (var item in this.Server.H5_DeleteList)
                {
                    DataBaseManager.DeleteFile(string.Format(@"{0}/{1}", H5_FOLDER_NAME, item));
                    if (this.Local.H5FileDic.ContainsKey(item))
                    {
                        this.Local.H5FileDic.Remove(item);
                    }
                    DataBaseManager.V1SaveDownloadedFileDesc(this.Local.H5FileDic, H5_DESC, AppName);
                    this.Local.Hash = string.Empty;
                }
            }
            #endregion
            dtStart = DateTime.Now;
            if (this.Server.FileDic != null || this.Server.H5FileDic != null)
                DownloadResource(parameter);
            else
                DownloadBusy = false;
        }
        /// <summary>
        /// 离线更新以及可选资源的下载 （程序会根据TCPPort情况判断下载方式）
        /// </summary>
        /// <param name="info">下载相关信息</param>
        /// <param name="name">下载文件名</param>
        /// <param name="type">下载类型（离线更新/可选资源下载）</param>
        /// <param name="delayAction">可选资源的回掉</param>
        //public override void V1DownCommonOrOptionalFile(OfflineInfo info, string name, string type, Action<bool> delayAction, Dictionary<string, string> parameter = null)
        //{
        //    // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
        //    OfflineZipDLInfo zipInfo = null;
        //    string socketPath;
        //    // 一定是 zip 文件
        //    if (type.Equals(DLType.Option))
        //    {
        //        if (Server.OptDownloadFileDic.ContainsKey(name))
        //        {
        //            info = Server.OptDownloadFileDic[name];
        //        }
        //        else
        //        {
        //            RYTLog.Log("error : 下载描述中没有此文件：" + name);
        //            if (delayAction != null)
        //            {
        //                delayAction(false);
        //            }
        //            return;
        //        }
        //    }
        //    // 当压缩包内文件单个下载时
        //    zipInfo = info as OfflineZipDLInfo;
        //    if (zipInfo != null && !zipInfo.ZipDLAll)
        //    {
        //        for (int i = 0; i < DownloadMaxRetryTime; i++)
        //        {
        //            info = zipInfo.FilesInZip.Values.FirstOrDefault(c => c.DownloadStatus == 0 && c.DownloadTimes == i);
        //            if (info != null)
        //            {
        //                break;
        //            }
        //        }
        //    }

        //    info.DownloadStatus = 1;

        //    #region 下载失败
        //    /*  TCP连接错误，如果支持TCP转HTTP下载 则 用HTTP下载
        //        当连续下载失败 超过5个则取消下载。
        //     */
        //    Action<string> failedAction = (errMsg) =>
        //    {
        //        Debug.WriteLine("*下载失败:* " + errMsg);
        //        info.DownloadStatus = 0;

        //        if (type.Equals(DLType.Common))
        //        {
        //            failTimes++;
        //            if (errMsg.StartsWith(SocketClient.ErrFlag))
        //            {
        //                // 连接失败后如果配置 支持 则 转http下载
        //                if (ConfigManager.BeTcpToHttp)
        //                {
        //                    isTCPFailed = true;
        //                    V1DownloadResourceFileAction();
        //                    return;
        //                }
        //            }
        //            if (failTimes < 5)
        //            {
        //                if (zipInfo != null && zipInfo.DownloadStatus != 2)
        //                {
        //                    V1DownCommonOrOptionalFile(zipInfo, zipInfo.Name, type, delayAction);
        //                }
        //                else
        //                {
        //                    V1DownloadResourceFileAction();
        //                }
        //            }
        //            else
        //            {
        //                if (ConfigManager.bOfflineEndHint)
        //                    MessageBox.Show(ConfigManager.NETWORK_ERROR, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
        //                sockets.Remove(type);
        //                OnCompleted(false);
        //                DownloadBusy = false;
        //            }
        //        }
        //        else if (type.Equals(DLType.Option))
        //        {
        //            if (info.DownloadTimes < DownloadMaxRetryTime && errMsg.StartsWith(SocketClient.ErrFlag))
        //            {
        //                // socket 下载失败后如果配置 支持 则 转http下载
        //                if (ConfigManager.BeTcpToHttp)
        //                {
        //                    isTCPFailed = true;
        //                }
        //                if (zipInfo != null && zipInfo.DownloadStatus != 2)
        //                {
        //                    V1DownCommonOrOptionalFile(zipInfo, zipInfo.Name, type, delayAction);
        //                }
        //                else
        //                {
        //                    V1DownCommonOrOptionalFile(info, info.Name, type, delayAction);
        //                }
        //            }
        //            else
        //            {
        //                if (delayAction != null)
        //                {
        //                    //info.DownloadTimes = 1;
        //                    delayAction(false);
        //                }
        //                if (sockets.ContainsKey(type))
        //                {
        //                    sockets[type].SocketShutDowm();
        //                    sockets.Remove(type);
        //                }
        //            }
        //        }
        //    };

        //    #endregion

        //    #region 下载成功
        //    Action<string, byte[]> successAction = (path, result) =>
        //    {
        //        var stream = new MemoryStream(result);

        //        if (type.Equals(DLType.Common))
        //        {
        //            if (SaveStreamToISO(info, stream, Client_DESC))
        //            {
        //                failTimes = 0;
        //                OnDownLoadFile(++downNum);
        //                if (zipInfo != null && !zipInfo.ZipDLAll && zipInfo.DownloadStatus != 2)
        //                {
        //                    // 继续循环下载直到 当前压缩文件内部文件下载完成
        //                    V1DownCommonOrOptionalFile(zipInfo, zipInfo.Name, type, delayAction);
        //                }
        //                else
        //                    V1DownloadResourceFileAction();
        //            }
        //            else
        //            {
        //                failTimes++;
        //                if (failTimes < 5)
        //                {
        //                    if (zipInfo != null && zipInfo.DownloadStatus != 2)
        //                    {
        //                        V1DownCommonOrOptionalFile(zipInfo, zipInfo.Name, type, delayAction);
        //                    }
        //                    else
        //                        V1DownloadResourceFileAction();
        //                }
        //                else
        //                {
        //                    if (ConfigManager.bOfflineEndHint)
        //                        MessageBox.Show(ConfigManager.NETWORK_ERROR, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
        //                    sockets.Remove(type);
        //                    OnCompleted(false);
        //                    DownloadBusy = false;
        //                }
        //            }

        //        }
        //        else if (type.Equals(DLType.Option))
        //        {
        //            bool isSuccs = SaveStreamToISO(info, stream, Option_DESC);
        //            if (zipInfo != null && zipInfo.DownloadStatus != 2)
        //            {
        //                V1DownCommonOrOptionalFile(zipInfo, zipInfo.Name, type, delayAction);
        //            }
        //            else
        //            {
        //                if (delayAction != null)
        //                {
        //                    delayAction(isSuccs);
        //                }
        //                if (sockets.ContainsKey(type))
        //                {
        //                    sockets[type].SocketShutDowm();
        //                    sockets.Remove(type);
        //                }
        //            }

        //        }
        //        stream.Dispose();
        //    };

        //    #endregion

        //    #region 离线2.0

        //    if (DataBaseManager.Offline_Version.CompareTo("2.0") >= 0)
        //    {
        //        // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
        //        if (Server.TCPPort != 0 && !isTCPFailed)
        //        {
        //            if (zipInfo != null && !zipInfo.ZipDLAll)
        //            {
        //                socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", zipInfo.Path, info.Path.Replace(".zip", ""), info.Name);
        //            }
        //            else
        //            {
        //                socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", info.Path, info.Name);
        //            }
        //            if (parameter != null && parameter.ContainsKey(APP_NAME) && !string.IsNullOrEmpty(parameter[APP_NAME]))
        //                socketPath = socketPath.Insert(socketPath.Length - 1, string.Format(",\"from\":\"{0}\"", parameter[APP_NAME]));

        //            string url = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;
        //            // ServerOfflineObj.ST == 0：TCP明文 1：TCP信道明文 2：TCP信道密文
        //            SocketClient socketClient;
        //            if (sockets.ContainsKey(type))
        //            {
        //                socketClient = sockets[type];
        //            }
        //            else
        //            {
        //                sockets[type] = socketClient = new SocketClient(url, Server.TCPPort, ConfigManager.Offline_Version, Server.ST);
        //            }
        //            //{"path":"ebank/resources/wp/common/css/test.css"}
        //            socketClient.Start(socketPath, successAction, failedAction);

        //            RYTLog.Log("Socket下载: " + socketPath);
        //        }
        //        else
        //        {
        //            string postPath = string.Empty;
        //            if (zipInfo != null && !zipInfo.ZipDLAll)
        //            {
        //                postPath = string.Format("path={0}/{1}/{2}", zipInfo.Path, info.Path.Replace(".zip", ""), info.Name);
        //            }
        //            else
        //            {
        //                postPath = string.Format("path={0}/{1}", info.Path, info.Name);
        //            }
        //            if (parameter != null && parameter.ContainsKey(APP_NAME) && !string.IsNullOrEmpty(parameter[APP_NAME]))
        //                postPath += string.Format("&from={0}", parameter[APP_NAME]);

        //            // ServerOfflineObj.ST == 0：HTTP明文 1：HTTP信道明文 2：HTTP信道密文 3：HTTPS
        //            if (Server.ST == 0 || Server.ST == 3)
        //            {
        //                PostClient post = new PostClient(Encoding.UTF8.GetBytes(postPath));
        //                post.DownloadStringCompleted += (s, e) =>
        //                {
        //                    if (e.bytesResult != null)
        //                    {
        //                        successAction(info.Name, e.bytesResult);
        //                    }
        //                    else
        //                    {
        //                        failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
        //                    }
        //                };
        //                post.DownloadStringAsync(new Uri(Server.Host, UriKind.RelativeOrAbsolute));

        //            }
        //            else
        //            {
        //                HttpRequest req = new HttpRequest(Server.Host, postPath, Server.ST == 2);
        //                req.OnFailed += (error, status) =>
        //                {
        //                    failedAction((isTCPFailed ? "Tcp->Http下载失败：" : "HTTP下载失败：") + error);
        //                };
        //                req.OnSuccess += (result, temp, response, headers) =>
        //                {
        //                    successAction(info.Name, temp);
        //                };
        //                req.Run(Server.ST);
        //            }
        //            RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + postPath);
        //        }
        //    }

        //    #endregion

        //    #region 离线1.0

        //    else
        //    {
        //        if (Server.TCPPort != 0 && !isTCPFailed)
        //        {
        //            if (zipInfo != null && !zipInfo.ZipDLAll)
        //            {
        //                socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", zipInfo.Path, info.Path.Replace(".zip", ""), info.Name);
        //            }
        //            else
        //            {
        //                socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", info.Path, info.Name);
        //            }
        //            string ip = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;

        //            SocketClient socketClient;
        //            if (sockets.ContainsKey(type))
        //            {
        //                socketClient = sockets[type];
        //            }
        //            else
        //            {
        //                sockets[type] = socketClient = new SocketClient(ip, Server.TCPPort, ConfigManager.Offline_Version);
        //            }
        //            //{"path":"ebank/resources/wp/common/css/test.css"}
        //            socketClient.Start(socketPath, successAction, failedAction);

        //            RYTLog.Log("Socket下载: " + socketPath);

        //        }
        //        else
        //        {
        //            string url;
        //            if (zipInfo != null && !zipInfo.ZipDLAll)
        //            {
        //                url = string.Format("{0}{1}/{2}/{3}?{4}", Server.Host, zipInfo.Path, info.Path.Replace(".zip", ""), info.Name, Guid.NewGuid().ToString());
        //            }
        //            else
        //            {
        //                url = string.Format("{0}{1}/{2}?{3}", Server.Host, info.Path, info.Name, Guid.NewGuid().ToString());
        //            }

        //            WebClient downloadClient = new WebClient();
        //            downloadClient.OpenReadCompleted += (s, e) =>
        //            {
        //                if (e.Error == null && e.Result != null)
        //                {
        //                    byte[] result = RYTMainHelper.StreamToBytes(e.Result);
        //                    successAction(info.Name, result);
        //                }
        //                else
        //                {
        //                    failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
        //                }
        //            };
        //            downloadClient.OpenReadAsync(new Uri(url, UriKind.RelativeOrAbsolute));
        //            RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + url);
        //        }

        //    }

        //    #endregion

        //    info.DownloadTimes++;

        //}
        /// <summary>
        /// 获取可选文件的 描述（只包含zip 名和 hash）
        /// </summary>
        /// <param name="path">client(本地已下载） / server（所有可选资源）</param>
        /// <returns></returns>
        public override Dictionary<string, string> GetOptNameRevDesc(string path)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (path.Equals("client") && Local != null)
            {
                InfosToNameRevTable(Local.OptSavedFileDic, dic);
            }
            else if (path.Equals("server") && Server != null)
            {
                InfosToNameRevTable(Server.OptDownloadFileDic, dic);
            }
            return dic;
        }
        public override bool V1CheckOfflineFileAvailable(string fileName)
        {
            OfflineInfo info = DataBaseManager.V1FindInfoInOfflineObj(fileName, Local);
            var result = DataBaseManager.V1CheckOfflineFileAvailable(fileName, info, AppName);
            return result == null ? false : true;
        }
        /// <summary>
        /// 根据插件名 删除插件资源
        /// </summary>
        /// <param name="fileName">插件文件名</param>
        /// <returns>成功返回true</returns>
        public override bool V1RemoveOptionalFile(string fileName)
        {
            bool result = false;
            try
            {
                if (Local.OptSavedFileDic.ContainsKey(fileName))
                {
                    OfflineZipDLInfo zipInfo = Local.OptSavedFileDic[fileName] as OfflineZipDLInfo;
                    if (zipInfo != null)
                    {
                        Local.OptSavedFileDic.Remove(fileName);
                        foreach (var item in zipInfo.FilesInZip)
                        {
                            Local.AllFilesInZipDic.Remove(item.Key);
                        }
                        DataBaseManager.V1SaveDownloadedFileDesc(Local.OptSavedFileDic, Option_DESC, AppName);
                        result = true;
                        Debug.WriteLine("update == Delete : zipFile   Name = " + zipInfo.Name);
                        string filePath = PLUG_IN_FOLDER_NAME + "/" + fileName.Replace(".zip", "");
                        DataBaseManager.DeleteDirectory(GetAppNamePath(filePath), true);
                    }
                }
                else
                {
                    //Debug.WriteLine(string.Format("删除-找不到可选zip描述信息：{0}", fileName), "调试提示", MessageBoxButton.OK);
                    result = false;
                }
                return result;
            }
            catch (Exception err)
            {
                Debug.WriteLine("插件资源删除出现错误：" + err);
                return result;
            }
        }
        public override string GetCommentOfFile(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                OfflineInfo fileInfo;
                if (Local.OptSavedFileDic.ContainsKey(fileName))
                {
                    fileInfo = Local.OptSavedFileDic[fileName];
                    return string.IsNullOrEmpty(fileInfo.Comment) ? null : fileInfo.Comment;
                }
                if (Local.FileDic.ContainsKey(fileName))
                {
                    fileInfo = Local.FileDic[fileName];
                    return string.IsNullOrEmpty(fileInfo.Comment) ? null : fileInfo.Comment;

                }
            }
            return null;
        }        
        public override int GetUpdateFlag(string jsonStr)
        {
            return UpdateServerInfo(jsonStr, this.Server);
        }                      
        /// <summary>
        /// 根据文件名删除必选资源中的文件 （可以是插件zip包，可以为普通资源，也可以为插件包内资源）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>删除成功返回true</returns>
        public override bool RemoveOfflineFile(string fileName)
        {
            if (fileName.EndsWith(".zip"))
            {
                if (Local.FileDic.ContainsKey(fileName))
                {
                    OfflineZipDLInfo zipInfo = Local.FileDic[fileName] as OfflineZipDLInfo;
                    if (zipInfo != null)
                    {
                        Local.FileDic.Remove(fileName);
                        foreach (var item in zipInfo.FilesInZip)
                        {
                            Local.AllFilesInZipDic.Remove(item.Key);
                        }

                        DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, AppName);
                        DataBaseManager.DeleteDirectory(GetAppNamePath(PLUG_IN_FOLDER_NAME + "/" + fileName.Replace(".zip", "")), true);
                        Local.Hash = string.Empty;
                        return true;
                    }
                }
                Debug.WriteLine(string.Format("删除-找不到普通zip描述信息：{0}", fileName));
                return false;
            }
            // 必选 插件内 资源
            else if (fileName.IndexOf("/") != -1)
            {
                if (Local.AllFilesInZipDic.ContainsKey(fileName))
                {
                    OfflineInfo info = Local.AllFilesInZipDic[fileName];
                    Local.AllFilesInZipDic.Remove(fileName);
                    DataBaseManager.DeleteFile(fileName, GetAppNamePath(PLUG_IN_FOLDER_NAME));
                    if (Local.FileDic.ContainsKey(info.Path))
                    {
                        OfflineZipDLInfo zipInfo = Local.FileDic[info.Path] as OfflineZipDLInfo;
                        zipInfo.Rev = "";
                        zipInfo.FilesInZip.Remove(fileName);
                        DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, AppName);
                        Local.Hash = string.Empty;
                        Debug.WriteLine("Delete : Path = " + info.Path + ", Name = " + info.Name);
                        return true;
                    }
                }
                Debug.WriteLine(string.Format("删除-找不到插件描述信息：{0}", fileName), "调试提示", MessageBoxButton.OK);
                return false;
            }
            else
            {
                if (Local.FileDic.ContainsKey(fileName))
                {
                    OfflineInfo info = Local.FileDic[fileName];
                    if (info != null)
                    {
                        DataBaseManager.DeleteFile(fileName, GetAppNamePath(OFFLINE_FOLDER_NAME));
                        Local.FileDic.Remove(fileName);
                        DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, AppName);
                        Local.Hash = string.Empty;
                        return true;
                    }
                }
                Debug.WriteLine(string.Format("删除-找不到普通描述信息：{0}", fileName), "调试提示", MessageBoxButton.OK);
                return false;
            }
        }        
        #region private
        /// <summary>
        /// 根据保存地址将程序包中的离线资源复制到独立存储区 (由于1版本中不需要进行拷贝处理，因此被废弃，无用可删除）
        /// </summary>
        /// <param name="descSavePath"> Client_DESC/ Option_DESC </param>
        private void CopyPackageToISO(string descSavePath)
        {
            bool comAllSaved = true;
            var fileList = Server.FileDic;
            if (descSavePath.Equals(Option_DESC))
            {
                fileList = Server.OptSavedFileDic;
            }

            #region 删除已经下载 并且与 预置包资源冲突的插件内容， 不冲突的保存

            if (OldOptFileList != null && OldOptFileList.Count > 0)
            {
                Dictionary<string, OfflineInfo> delList = new Dictionary<string, OfflineInfo>();
                foreach (var item in OldOptFileList)
                {
                    bool haveOpt = fileList.Any(c => c.Key.Equals(item.Key, StringComparison.CurrentCultureIgnoreCase));
                    bool haveInOpt = false;

                    OfflineZipDLInfo zipInfo = item.Value as OfflineZipDLInfo;
                    if (!haveOpt)
                    {
                        foreach (var file in zipInfo.FilesInZip)
                        {
                            haveInOpt = Server.AllFilesInZipDic.Any(c => c.Key.Equals(item.Key, StringComparison.CurrentCultureIgnoreCase));
                            if (haveInOpt)
                            {
                                break;
                            }
                        }
                    }

                    if (haveOpt || haveInOpt)
                    {
                        string zipFullFolderName = DataBaseManager.PLUG_IN_FOLDER_NAME + "/" + item.Key.Replace(".zip", "");
                        // 首先删除原有的zip包,如果有的话：
                        DataBaseManager.DeleteDirectory(zipFullFolderName);
                        delList.Add(item.Key, item.Value);
                    }
                }
                foreach (var item in delList)
                {
                    OldOptFileList.Remove(item.Key);
                }
            }

            #endregion

            foreach (var file in fileList)
            {
                Stream stream;
                var dlFile = file.Value as OfflineZipDLInfo;
                if (dlFile != null)
                {
                    var zipFile = file.Value as OfflineZipDLInfo;
                    if (zipFile == null) continue;
                    stream = DataBaseManager.ReadAppPackageFile("Offline/" + file.Key, "current") as Stream;
                }
                else
                {
                    stream = DataBaseManager.ReadAppPackageFile("Offline/" + file.Key, "current") as Stream;
                }
                if (stream != null)
                {
                    bool result = SaveStreamToISO(file.Value, stream, descSavePath);
                    if (descSavePath.Equals(Client_DESC) && result == false)
                    {
                        comAllSaved = false;
                    }
                }
                else
                {
                    RYTLog.Log("找不到离线文件:" + file.Key);
                }
            }

            if (descSavePath.Equals(Client_DESC) && comAllSaved)
            {
                DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, Client_DESC, Server.Hash);
            }
        }
        /// <summary>
        /// 将文件流 保存到本地，并更新描述文件
        /// </summary>
        /// <param name="file"> OfflineInfo 信息 </param>
        /// <param name="stream">  </param>
        /// <param name="descSavePath"> Client_DESC/ Option_DESC </param>
        /// <returns>是否成功</returns>
        private bool SaveStreamToISO(OfflineInfo file, Stream stream, string descSavePath)
        {
            try
            {
                bool allSaved = true;   // 只要有出错的文件 就返回 失败

                #region zip

                if (file.ParentInfo == null && file.Name.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(file.Rev))
                {
                    //Debug.WriteLine("解压：" +  file.Key);
                    var zipFile = file as OfflineZipDLInfo;

                    if (RYTMainHelper.CompareSHA1(stream, zipFile.Rev))
                    {
                        Dictionary<string, OfflineInfo> filesInZip = new Dictionary<string, OfflineInfo>();
                        OfflineZipDLInfo savedFile = new OfflineZipDLInfo() { Name = file.Name, Rev = file.Rev, Encrypt = file.Encrypt, Comment = file.Comment, FilesInZip = filesInZip };
                        if (zipFile.FilesInZip != null && zipFile.FilesInZip.Count() != 0)
                        {
                            string zipFullFolderName = string.Empty;

                            #region 旧解压方法

                            //bool bFirstItem = true;
                            //RYTUnZipper unzipper = new RYTUnZipper(stream);
                            //foreach (var entry in unzipper.FileNamesInZip)
                            //{
                            //    var fileStream = unzipper.GetFileStream(entry);
                            //    if (fileStream != null && zipFile.FilesInZip.Any(c => c.Key.Equals(entry, StringComparison.CurrentCultureIgnoreCase)))
                            //    {
                            //        OfflineInfo fileInZip = zipFile.FilesInZip[entry];

                            //        if (bFirstItem)
                            //        {
                            //            bFirstItem = false;

                            //            int indexSpit = entry.IndexOf("/");
                            //            if (indexSpit != -1)
                            //            {
                            //                zipFullFolderName = DataBaseManager.PLUG_IN_FOLDER_NAME + "/" + entry.Substring(0, entry.IndexOf("/"));
                            //                // 首先删除原有的zip包,如果有的话：
                            //                DataBaseManager.DeleteDirectory(zipFullFolderName);
                            //            }
                            //        }

                            //        var fileByes = RYTMainHelper.StreamToBytes(fileStream);
                            //        if (RYTMainHelper.CompareSHA1(fileByes, fileInZip.Rev))
                            //        {
                            //            OfflineInfo newFileInZip = new OfflineInfo() { Name = fileInZip.Name, Path = zipFile.Name, Rev = fileInZip.Rev, Encrypt = fileInZip.Encrypt };
                            //            if (fileInZip.Encrypt)
                            //            {
                            //                var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                            //                DataBaseManager.SaveBytesFile(encryptData, entry, DataBaseManager.PLUG_IN_FOLDER_NAME);
                            //            }
                            //            else
                            //            {
                            //                DataBaseManager.SaveBytesFile(fileByes, entry, DataBaseManager.PLUG_IN_FOLDER_NAME);
                            //            }
                            //            filesInZip.Add(newFileInZip.Name, newFileInZip);
                            //        }
                            //        else
                            //        {
                            //            allSaved = false;
                            //            RYTLog.Log(entry + " SHA1值校验出错！");
                            //        }
                            //        fileStream.Dispose();
                            //    }
                            //    else
                            //    {
                            //        allSaved = false;
                            //        RYTLog.Log("描述列表 或者压缩文件中没找到文件：" + entry);
                            //    }
                            //}
                            //unzipper.Dispose();

                            #endregion

                            #region 新的解压方法

                            stream.Seek(0, SeekOrigin.Begin);
                            var archives = ArchiveFactory.Open(stream);
                            foreach (var entry in archives.Entries)
                            {
                                if (entry.IsDirectory)
                                {
                                    //DataBaseManager.CreateDirectory(entry.FilePath);
                                    continue;
                                }

                                using (Stream fileStream = new MemoryStream())
                                {
                                    entry.WriteTo(fileStream);
                                    fileStream.Seek(0, SeekOrigin.Begin);
                                    if (fileStream != null && zipFile.FilesInZip.ContainsKey(entry.FilePath))
                                    {
                                        OfflineInfo fileInZip = zipFile.FilesInZip[entry.FilePath];

                                        var fileByes = RYTMainHelper.StreamToBytes(fileStream);
                                        if (RYTMainHelper.CompareSHA1(fileByes, fileInZip.Rev))
                                        {
                                            OfflineInfo newFileInZip = new OfflineInfo() { Name = fileInZip.Name, Path = zipFile.Name, Rev = fileInZip.Rev, ParentInfo = savedFile };
                                            string folderName = GetAppNamePath( DataBaseManager.PLUG_IN_FOLDER_NAME);
                                            if (savedFile.Encrypt)
                                            {
                                                var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                                                DataBaseManager.SaveBytesFile(encryptData, entry.FilePath, folderName);
                                            }
                                            else
                                            {
                                                DataBaseManager.SaveBytesFile(fileByes, entry.FilePath, folderName);
                                            }
                                            filesInZip.Add(newFileInZip.Name, newFileInZip);
                                        }
                                        else
                                        {
                                            allSaved = false;
                                            RYTLog.Log(entry + " SHA1值校验出错！");
                                        }
                                        fileStream.Dispose();
                                    }
                                    else
                                    {
                                        RYTLog.Log("描述列表 或者压缩文件中没找到文件：" + entry.FilePath);
                                    }
                                }
                            }
                            archives.Dispose();

                            #endregion
                        }

                        #region 保存描述

                        if (!allSaved)
                        {
                            savedFile.Rev = "";
                        }

                        if (descSavePath.Equals(Client_DESC))
                        {
                            if (Local.FileDic.ContainsKey(savedFile.Name))
                            {
                                // 此处只应将更新过的重复文件删除。
                                OfflineZipDLInfo oldzipinfo = Local.FileDic[savedFile.Name] as OfflineZipDLInfo;
                                oldzipinfo.Rev = savedFile.Rev;
                                oldzipinfo.Comment = savedFile.Comment;
                                foreach (var item in savedFile.FilesInZip)
                                {
                                    oldzipinfo.FilesInZip[item.Key] = item.Value;
                                    Local.AllFilesInZipDic[item.Key] = item.Value;
                                }
                            }
                            else
                            {
                                Local.FileDic.Add(savedFile.Name, savedFile);
                                foreach (var item in filesInZip)
                                {
                                    Local.AllFilesInZipDic.Add(item.Key, item.Value);
                                }
                            }

                            DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, descSavePath, AppName);

                        }
                        else if (descSavePath.Equals(Option_DESC))
                        {
                            if (Local.OptSavedFileDic.ContainsKey(savedFile.Name))
                            {
                                OfflineZipDLInfo oldZipInfo = Local.OptSavedFileDic[savedFile.Name] as OfflineZipDLInfo;
                                oldZipInfo.Rev = savedFile.Rev;
                                oldZipInfo.Comment = savedFile.Comment;
                                foreach (var item in savedFile.FilesInZip)
                                {
                                    oldZipInfo.FilesInZip[item.Key] = item.Value;
                                    Local.AllFilesInZipDic[item.Key] = item.Value;
                                }
                            }
                            else
                            {
                                Local.OptSavedFileDic[savedFile.Name] = savedFile;
                                foreach (var item in filesInZip)
                                {
                                    Local.AllFilesInZipDic[item.Key] = item.Value;
                                }
                            }
                            DataBaseManager.V1SaveDownloadedFileDesc(Local.OptSavedFileDic, descSavePath, AppName);
                        }

                        // 修改下载状态 
                        if (allSaved)
                        {
                            file.DownloadStatus = 2;
                        }
                        OfflineZipDLInfo serverZipInfo = file as OfflineZipDLInfo;
                        if (serverZipInfo.FilesInZip != null)
                        {
                            foreach (var item in filesInZip)
                            {
                                if (serverZipInfo.FilesInZip.ContainsKey(item.Key))
                                    serverZipInfo.FilesInZip[item.Key].DownloadStatus = 2;
                            }
                        }

                        #endregion
                    }
                    else
                    {
                        RYTLog.Log("Zip file ShA1值校验失败：" + zipFile.Name);
                        allSaved = false;
                    }
                }

                #endregion

                #region zip内部单个文件

                else if (file.ParentInfo != null)
                {
                    var fileByes = RYTMainHelper.StreamToBytes(stream);
                    if (RYTMainHelper.CompareSHA1(stream, file.Rev))
                    {
                        OfflineZipDLInfo serverZipInfo = file.ParentInfo as OfflineZipDLInfo;
                        OfflineZipDLInfo localZipInfo = null;

                        if (!Local.FileDic.ContainsKey(serverZipInfo.Name))
                        {
                            Dictionary<string, OfflineInfo> filesDic = new Dictionary<string, OfflineInfo>();
                            localZipInfo = new OfflineZipDLInfo() { Name = serverZipInfo.Name, Comment = serverZipInfo.Comment, Encrypt = serverZipInfo.Encrypt, FilesInZip = filesDic };
                        }
                        else
                        {
                            if (descSavePath.Equals(Client_DESC))
                            {
                                localZipInfo = Local.FileDic[serverZipInfo.Name] as OfflineZipDLInfo;
                            }
                            else
                            {
                                localZipInfo = Local.OptSavedFileDic[serverZipInfo.Name] as OfflineZipDLInfo;
                            }
                        }

                        OfflineInfo savedFile = new OfflineInfo() { Name = file.Name, Path = file.Path, Rev = file.Rev, ParentInfo = localZipInfo };
                        string fileName = PLUG_IN_FOLDER_NAME + "/" + file.Name;
                        fileName = GetAppNamePath(fileName);
                        if (serverZipInfo.Encrypt) // 加密
                        {
                            var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                            DataBaseManager.SaveBytesFile(encryptData, fileName);
                        }
                        else
                        {
                            DataBaseManager.SaveBytesFile(fileByes, fileName);
                        }
                        localZipInfo.FilesInZip[savedFile.Name] = savedFile;
                        Local.AllFilesInZipDic[savedFile.Name] = savedFile;
                        file.DownloadStatus = 2;
                        if (serverZipInfo.FilesInZip.Values.All(c => c.DownloadStatus == 2))
                        {
                            localZipInfo.Rev = serverZipInfo.Rev;
                            serverZipInfo.DownloadStatus = 2;
                        }

                        #region 保存描述信息

                        if (descSavePath.Equals(Client_DESC))
                        {
                            Local.FileDic[localZipInfo.Name] = localZipInfo;
                            DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, descSavePath, AppName);
                        }
                        else
                        {
                            Local.OptSavedFileDic[localZipInfo.Name] = localZipInfo;
                            DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, descSavePath, AppName);
                        }
                        #endregion
                    }
                    else
                    {
                        allSaved = false;
                        RYTLog.Log("File In Zip ShA1值校验失败：" + file.Name);
                    }
                }

                #endregion

                #region 普通单个资源， 并未考虑为插件包内资源
                else
                {
                    var fileByes = RYTMainHelper.StreamToBytes(stream);
                    if (RYTMainHelper.CompareSHA1(stream, file.Rev))
                    {
                        OfflineInfo savedFile = new OfflineInfo() { Name = file.Name, Rev = file.Rev, Comment = file.Comment, Encrypt = file.Encrypt };
                        string fileName = OFFLINE_FOLDER_NAME + "/" + file.Name;
                        fileName = GetAppNamePath(fileName);
                        if (file.Encrypt) // 加密
                        {
                            var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(fileByes);
                            DataBaseManager.SaveBytesFile(encryptData, fileName);
                        }
                        else
                        {
                            DataBaseManager.SaveBytesFile(fileByes, fileName);
                        }
                        #region 保存描述信息
                        if (descSavePath.Equals(Client_DESC))
                        {
                            Local.FileDic[savedFile.Name] = savedFile;
                            file.DownloadStatus = 2;                           
                            DataBaseManager.V1SaveDownloadedFileDesc(Local.FileDic, descSavePath, AppName);
                        }
                        #endregion
                    }
                    else
                    {
                        allSaved = false;
                        RYTLog.Log("File ShA1值校验失败：" + file.Name);
                    }
                }
                #endregion

                stream.Dispose();
                return allSaved;
            }
            catch (Exception e)
            {
                Debug.WriteLine(string.Format("保存文件出错: {0} ", file.Name));
                return false;
            }

        }        
        /// <summary>
        /// 执行下载进程 的回掉
        /// </summary>
        /// <param name="downNum">已下载数</param>
        /// <param name="totalNum">需下载总数</param>
        private void OnDownLoadFile(int downNum)
        {            
            if (processAction != null)
            {
                int totalNum = mustDownloadFilesNum;
                if (ConfigManager.Offline_Version.CompareTo("2.0") > 0 && Server.H5FileDic != null)
                    totalNum += Server.H5FileDic.Count;
                processAction(downNum, totalNum);                
            }
        }
        public void UpdateServer(JObject UpdateInfoJson, V1OfflineJsonClass server)
        {
            try
            {
                JObject jo = UpdateInfoJson;               
                var hostNode = jo["h"];
                if (hostNode != null && hostNode.Type == JTokenType.String)
                    server.Host = hostNode.Value<string>();
                var tcpNode = jo["t"];
                if (tcpNode != null && tcpNode.Type == JTokenType.Integer)
                    server.TCPPort = tcpNode.Value<int>();
                #region OptionDownload  
                var optionNode = jo["o"];
                if (optionNode != null)
                {
                    if (optionNode.Children<JProperty>().Count() > 0)
                    {
                        server.OptDownloadFileDic = new Dictionary<string, OfflineInfo>();
                        foreach (var jp in optionNode.Children<JProperty>())
                        {
                            foreach (var jpFile in jp.Value.Children<JProperty>())
                            {
                                OfflineZipDLInfo zipInfo = DataBaseManager.ServerZipOrFileDescToObj(jp.Name, jpFile) as OfflineZipDLInfo;
                                if (zipInfo != null)
                                {
                                    OfflineZipDLInfo savedZipInfo = Local.OptSavedFileDic.Values.FirstOrDefault(c => c.Name.Equals(zipInfo.Name)) as OfflineZipDLInfo;
                                    if (savedZipInfo != null)//检查本地是否已下载次option资源文件
                                    {
                                        if (!string.IsNullOrEmpty(zipInfo.Rev))
                                            zipInfo.DownloadStatus = 2;
                                        if (savedZipInfo.FilesInZip != null)
                                        {
                                            foreach (var item in savedZipInfo.FilesInZip)
                                            {
                                                OfflineInfo fileInZip = zipInfo.FilesInZip.Values.FirstOrDefault(c => c.InfoEquals(item));
                                                if (fileInZip != null)
                                                    fileInZip.DownloadStatus = 2;
                                            }
                                        }
                                    }
                                    server.OptDownloadFileDic[zipInfo.Name] = zipInfo;
                                    if (zipInfo != null && zipInfo.FilesInZip != null)
                                    {
                                        foreach (var item in zipInfo.FilesInZip)
                                        {
                                            server.AllFilesInZipDic[item.Key] = item.Value;
                                        }
                                    }
                                }                                                               
                            }
                        }                        
                    }
                }
                #endregion          
                #region download
                var downloadNode = jo["l"];
                if (downloadNode != null)
                {
                    server.FileDic = new Dictionary<string, OfflineInfo>();
                    foreach (var jp in downloadNode.Values<JProperty>())
                    {
                        if (jp.Name.Equals("s", StringComparison.CurrentCultureIgnoreCase))
                        {
                            server.Hash = jp.Value.ToString();
                        }
                        else
                        {                            
                            foreach (var jpFile in jp.Value.Children<JProperty>())
                            {
                                OfflineInfo info = DataBaseManager.ServerZipOrFileDescToObj(jp.Name, jpFile);
                                server.FileDic[info.Name] = info;
                                OfflineZipDLInfo zipInfo = info as OfflineZipDLInfo;
                                if (zipInfo != null && zipInfo.FilesInZip != null)
                                {
                                    foreach (var item in zipInfo.FilesInZip)
                                    {
                                        server.AllFilesInZipDic[item.Key] = item.Value;
                                    }
                                }
                            }
                        }
                    }
                    server.ClientDesc = downloadNode.ToString();
                }
                #endregion
                #region H5数据结构

                ////其他字段不变
                ////新增下面两个字段
                //  "h5" :{ //H5资源更新格式
                //  "s" : "c4f3cffb7d68e63531c60e74611a2d2f569173e7", // 该值与必选资源的s字段值相同
                //  "h5" : { //本地描述的资源路径（p字段），目前H5资源的p字段值均为h5
                //             "path/file1" :{ // 普通资源 如: "demo/img/welcome.png"
                //             "r" : "c4f3cffb7d68e63531c60e74611a2d2f569173e7",
                //             "mime" : MimeType,  // 如: "text/plain"、"image/png"...
                //             "u" : 1  //1,确保更新, 0,非确保更新 默认为0,且如果为默认,则省略此字段
                //          },
                //         ...
                //    }
                //              },
                //  "h5_d":["path/file2","path/file3"] //须删除的H5资源数组

                #endregion
                #region H5 + H5_D
                var h5Node = jo["h5"];
                if (h5Node != null)
                {
                    server.H5Hash = h5Node.Value<string>("s");
                    var fileNode = h5Node["h5"];
                    if (fileNode != null)
                    {
                        Dictionary<string, OfflineInfo> infos = new Dictionary<string, OfflineInfo>();
                        OfflineInfo info;
                        foreach (var item in fileNode.Values<JProperty>())
                        {
                            info = new OfflineInfo();
                            info.Name = item.Name;
                            var itemNode = item.Value;
                            info.Rev = itemNode.Value<string>("r");
                            info.Mime = itemNode.Value<string>("mime");
                            string ed = itemNode.Value<string>("u");
                            if (!string.IsNullOrEmpty(ed) && ed.Equals("1"))
                                info.EnsureDownload = 1;
                            else
                                info.EnsureDownload = 0;
                            infos[info.Name] = info;
                        }
                        server.H5FileDic = infos;
                    }
                }
                var h5DelNode = jo["h5_d"];
                if (h5DelNode != null)
                {
                    List<string> delList = new List<string>();
                    foreach (var child in h5DelNode.Values<string>())
                    {
                        delList.Add(child);
                    }
                    server.H5_DeleteList = delList;
                }
                #endregion
                #region delete
                var deleteNode = jo["d"];
                if (deleteNode != null)
                {
                    server.DeleteFileDic = new Dictionary<string, OfflineInfo>();
                    foreach (var child in deleteNode.Children())
                    {
                        if (child.Type == JTokenType.String)
                        {
                            OfflineInfo delInfo = new OfflineInfo() { Name = (child as JValue).ToString() };
                            server.DeleteFileDic[delInfo.Name] = delInfo;
                        }
                        // 插件包
                        else if (child.Type == JTokenType.Object)
                        {
                            var token = child as JToken;
                            foreach (var item in token.Children())
                            {
                                JProperty jp = item as JProperty;
                                foreach (var jpp in jp.Value.Children())
                                {
                                    OfflineInfo delInfo = new OfflineInfo() { Name = jpp.Value<string>(), Path = jp.Name };
                                    server.DeleteFileDic[delInfo.Name] = delInfo;
                                }
                            }
                        }
                    }
                }
                #endregion             
            }
            catch (Exception err)
            {
                Debug.WriteLine("V1 服务器描述解析出错： " + err.Message);
            }
        }
        private void Update(JObject UpdateInfoJson)
        {
            UpdateServer(UpdateInfoJson, this.Server);
            this.Local.Host = this.Server.Host;
            this.Local.TCPPort = this.Server.TCPPort;
            if (this.Server == null)
            {
                OnUpdateDescCompleted(false);
                if (ConfigManager.bOfflineStartHint)
                    MessageBox.Show(ConfigManager.OfflineReturnedDataFormattedError, ConfigManager.OfflineDebuggingTips, MessageBoxButton.OK);
                return;
            }
            if (this.Server.FileDic.Count == 0 && this.Server.DeleteFileDic == null && !string.IsNullOrEmpty(this.Server.Hash))
            {
                DataBaseManager.V1SaveDownloadedFileDesc(this.Local.FileDic, Client_DESC, AppName, this.Server.Hash);
            }
        }                      
        private void DownloadResource(Dictionary<string, string> parameter = null)
        {
            Task.Run(() => DownloadMustFile(parameter));
            if (Server.H5FileDic.Count > 0)
                Task.Run(() => DownloadH5File(parameter));            
            #region test
            //else
            //{
            //    if (sockets.ContainsKey(DLType.Common) && sockets[DLType.Common] != null)
            //    {
            //        sockets[DLType.Common].SocketShutDowm();
            //        sockets.Remove(DLType.Common);
            //    }
            //}

            //var dtDura = DateTime.Now - dtStart;
            //Debug.WriteLine(string.Format("IO操作耗时：{0}", dtDura.ToString()));

            //ReadAndWriteEMPJSToIS();

            //if (commRes)
            //{
            //    DataBaseManager.V1SaveDownloadedFileDesc(Local.CommonFileDic, Client_DESC, AppName, Server.Hash);
            //    if (ConfigManager.bOfflineEndHint)
            //        MessageBox.Show(ConfigManager.OfflineUpdated, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
            //    OnCompleted(true);
            //}
            //else
            //{
            //    if (ConfigManager.bOfflineEndHint)
            //        MessageBox.Show(ConfigManager.OfflineUpdatedWithUndownloadFiles, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
            //    OnCompleted(false);
            //}
            //DownloadBusy = false;
            //return; 
            #endregion
        }
        public void DownloadH5File(Dictionary<string, string> parameter = null)
        {                        
            Queue<OfflineInfo> h5FilesQueue = new Queue<OfflineInfo>(this.Server.H5FileDic.Values);                               
            DownloadH5FileAction(h5FilesQueue, H5FileDownloadFaildList, parameter);
        }
        private void DownloadH5FileAction(Queue<OfflineInfo> queue, List<OfflineInfo> downloadFaildList, Dictionary<string, string> parameter = null)
        {
            OfflineInfo info = null;
            bool flag = true;
            while (queue.Count > 0 && flag)
            {
                info = queue.Dequeue();
                if (info.DownloadTimes >= 2)
                    downloadFaildList.Add(info);
                else
                    flag = false;
            }
            if (flag)//没有可下载的H5资源
            {
                isH5FileDownloadCompleted = true;
                if (IsDownloadCompleted)
                    OnCompleted(true);
                return;
            }
            info.DownloadStatus = 1;    
            string key = info.Name;           
            string fileName = string.Empty;
            string folderName = string.Empty;
            string url = this.Server.Host.Contains("/") ? this.Server.Host.Split('/')[2].Split(':')[0] : this.Server.Host;
            string path = string.Format(@"{0}/{1}", DataBaseManager.h5Key, key);
            // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
            string socketPath;

            #region 下载失败
            /*  TCP连接错误，如果支持TCP转HTTP下载 则 用HTTP下载
                当连续下载失败 超过5个则取消下载。
             */
            Action<string> failedAction = (errMsg) =>
            {
                Debug.WriteLine("*下载失败:* " + errMsg);
                info.DownloadStatus = 0;                
                queue.Enqueue(info);//下载失败少于2次的再次加入下载队列，等待下一次下载重试                 
                if (errMsg.StartsWith(SocketClient.ErrFlag))
                {
                     //连接失败后如果配置 支持 则 转http下载
                    if (ConfigManager.BeTcpToHttp)
                    {
                        isTCPFailed = true;
                    }
                }
                DownloadH5FileAction(queue, downloadFaildList, parameter); 
            };

            #endregion

            #region 下载成功

            Action<string, byte[]> successAction = (filepath, result) =>
            {
                if (!RYTSecurity.Instance.CompareSHA1(result, info.Rev))
                {
                    Debug.WriteLine("h5资源ShA1校验失败。");
                    info.DownloadStatus = 0;
                    queue.Enqueue(info);//下载失败少于2次的再次加入下载队列，等待下一次下载重试
                    DownloadH5FileAction(queue, downloadFaildList, parameter); 
                    return;
                }
                Local.H5FileDic[key] = new OfflineInfo() { Name = info.Name, Rev = info.Rev, Comment = info.Comment, Mime = info.Mime, Encrypt = info.Encrypt, EnsureDownload = info.EnsureDownload };
                try
                {
                    // 保存数据，更新json
                    if (info.Encrypt) // 加密
                    {
                        var encryptData = RYTSecurity.Instance.EncryptAndReturnBytes(result);
                        DataBaseManager.SaveBytesFile(encryptData, key, GetAppNamePath(H5_FOLDER_NAME), info.Encrypt);
                    }
                    else
                        DataBaseManager.SaveBytesFile(result, key, GetAppNamePath(H5_FOLDER_NAME), info.Encrypt);
                    DataBaseManager.V1SaveDownloadedFileDesc(Local.H5FileDic, H5_DESC, AppName);
                    info.DownloadStatus = 2;
                }
                catch (Exception)
                {
                    info.DownloadStatus = 0;
                    queue.Enqueue(info);
                    DownloadH5FileAction(queue, downloadFaildList, parameter);
                }
                // 下载个数 回掉
                OnDownLoadFile(++downNum);
                DownloadH5FileAction(queue, downloadFaildList, parameter);               
            };

            #endregion          
            if (DataBaseManager.Offline_Version.CompareTo("2.1") >= 0)
            {
                // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
                if (Server.TCPPort != 0 && !isTCPFailed)
                {
                    socketPath = string.Format("{{\"path\":\"{0}\"}}", path);
                    if (!string.IsNullOrEmpty(AppName))
                        socketPath = socketPath.Insert(socketPath.Length - 1, string.Format(",\"from\":\"{0}\"", AppName));

                    // ServerOfflineObj.ST == 0：TCP明文 1：TCP信道明文 2：TCP信道密文
                    SocketClient socketClient;
                    if (sockets.ContainsKey(DLType.H5))
                    {
                        socketClient = sockets[DLType.H5];
                    }
                    else
                    {
                        sockets[DLType.H5] = socketClient = new SocketClient(url, Server.TCPPort, ConfigManager.Offline_Version, Server.ST);
                    }                    
                    //{"path":"ebank/resources/wp/common/css/test.css"}
                    socketClient.Start(socketPath, successAction, failedAction);

                    RYTLog.Log("Socket下载: " + socketPath);
                }
                else
                {
                    string postPath = string.Empty;
                    postPath = string.Format("path={0}", path);

                    // ST == 0：HTTP明文 1：HTTP信道明文 2：HTTP信道密文 3：HTTPS
                    if (Server.ST == 0 || Server.ST == 3)
                    {
                        PostClient post = new PostClient(Encoding.UTF8.GetBytes(postPath));
                        post.DownloadStringCompleted += (s, e) =>
                        {
                            if (e.bytesResult != null)
                            {
                                successAction(path, e.bytesResult);
                            }
                            else
                            {
                                failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
                            }
                        };
                        post.DownloadStringAsync(new Uri(Server.Host, UriKind.RelativeOrAbsolute));
                    }
                    else
                    {
                        HttpRequest req = new HttpRequest(Server.Host, postPath, Server.ST == 2);
                        req.OnFailed += (error, status) =>
                        {
                            failedAction((isTCPFailed ? "Tcp->Http下载失败：" : "HTTP下载失败：") + error);
                        };
                        req.OnSuccess += (result, temp, response, headers) =>
                        {
                            successAction(path, temp);
                        };
                        req.Run(Server.ST);
                    }
                    RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + postPath);
                }
            }
            info.DownloadTimes++;
        }
        private void DownloadMustFile(Dictionary<string, string> parameter = null)
        {
            Queue<OfflineInfo> mustFilesQueue = new Queue<OfflineInfo>(this.Server.FileDic.Count);                       
            foreach(var info in this.Server.FileDic.Values)
            {
                OfflineZipDLInfo zipInfo = info as OfflineZipDLInfo;
                if (zipInfo != null && !zipInfo.ZipDLAll)
                {
                    foreach(var infoInZip in zipInfo.FilesInZip.Values)
                        mustFilesQueue.Enqueue(infoInZip);                   
                }
                else
                    mustFilesQueue.Enqueue(info);
            }
            mustDownloadFilesNum = mustFilesQueue.Count;
            DownloadMustFileAction(mustFilesQueue, mustFileDownloadFaildList, parameter);
            #region MyRegion
            //OfflineInfo offlineInfo = null;            
            //bool flag = true;
            //while (mustFilesQueue.Count > 0 && flag)
            //{
            //    offlineInfo = mustFilesQueue.Dequeue();
            //    if (offlineInfo.DownloadTimes >= 2)
            //        donwloadFailedFiles.Add(offlineInfo);
            //    else
            //        flag = false;

            //}
            //if (flag)//没有可下载的资源
            //{
            //    return;
            //}
            //offlineInfo.DownloadStatus = 1;

            //#region 下载失败
            ///*  TCP连接错误，如果支持TCP转HTTP下载 则 用HTTP下载
            //    当连续下载失败 超过5个则取消下载。
            // */
            //Action<string> failedAction = (errMsg) =>
            //{
            //    Debug.WriteLine("*下载失败:* " + errMsg);
            //    offlineInfo.DownloadStatus = 0;

            //    failTimes++;
            //    if (errMsg.StartsWith(SocketClient.ErrFlag))
            //    {
            //        // 连接失败后如果配置 支持 则 转http下载
            //        if (ConfigManager.BeTcpToHttp)
            //        {
            //            isTCPFailed = true;
            //            V1DownloadResourceFileAction();
            //            return;
            //        }
            //    }
            //    if (failTimes < 5)
            //    {
            //        //if (zipInfo != null && zipInfo.DownloadStatus != 2)
            //        //{
            //        //    V1DownCommonOrOptionalFile(zipInfo, zipInfo.Name, type, delayAction);
            //        //}
            //        //else
            //        //{
            //        //    V1DownloadResourceFileAction();
            //        //}
            //    }
            //    else
            //    {
            //        //if (ConfigManager.bOfflineEndHint)
            //        //    MessageBox.Show(ConfigManager.NETWORK_ERROR, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
            //        ////sockets.Remove(type);
            //        //OnCompleted(false);
            //        //DownloadBusy = false;
            //    }
            //};

            //#endregion

            //#region 下载成功
            //Action<string, byte[]> successAction = (path, result) =>
            //{
            //    var stream = new MemoryStream(result);

            //    if (SaveStreamToISO(offlineInfo, stream, Client_DESC))
            //    {
            //        failTimes = 0;
            //        OnDownLoadFile(++downNum);
            //        //if (zipInfo != null && !zipInfo.ZipDLAll && zipInfo.DownloadStatus != 2)
            //        //{
            //        //    // 继续循环下载直到 当前压缩文件内部文件下载完成
            //        //    V1DownCommonOrOptionalFile(zipInfo, zipInfo.Name, type, delayAction);
            //        //}
            //        //else
            //        //    V1DownloadResourceFileAction();
            //    }
            //    else
            //    {
            //        failTimes++;
            //        //if (failTimes < 5)
            //        //{
            //        //    if (zipInfo != null && zipInfo.DownloadStatus != 2)
            //        //    {
            //        //        V1DownCommonOrOptionalFile(zipInfo, zipInfo.Name, type, delayAction);
            //        //    }
            //        //    else
            //        //        V1DownloadResourceFileAction();
            //        //}
            //        //else
            //        //{
            //        //    if (ConfigManager.bOfflineEndHint)
            //        //        MessageBox.Show(ConfigManager.NETWORK_ERROR, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
            //        //    sockets.Remove(type);
            //        //    OnCompleted(false);
            //        //    DownloadBusy = false;
            //        //}
            //    }
            //    stream.Dispose();
            //};

            //#endregion

            //string socketPath;
            //#region 离线2.0

            //if (DataBaseManager.Offline_Version.CompareTo("2.0") >= 0)
            //{
            //    // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
            //    if (Server.TCPPort != 0 && !isTCPFailed)
            //    {
            //        if (offlineInfo.ParentInfo==null)
            //        {
            //            socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", offlineInfo.Path, offlineInfo.Name);
            //        }
            //        else
            //        {
            //            socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", offlineInfo.ParentInfo.Path, offlineInfo.Path.Replace(".zip", ""), offlineInfo.Name);

            //        }
            //        //if (zipInfo != null && !zipInfo.ZipDLAll)
            //        //{
            //        //    socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", zipInfo.Path, info.Path.Replace(".zip", ""), info.Name);
            //        //}
            //        //else
            //        //{
            //        //    socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", info.Path, info.Name);
            //        //}
            //        if (parameter != null && parameter.ContainsKey(APPNAMEKEY) && !string.IsNullOrEmpty(parameter[APPNAMEKEY]))
            //            socketPath = socketPath.Insert(socketPath.Length - 1, string.Format(",\"from\":\"{0}\"", parameter[APPNAMEKEY]));

            //        string url = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;
            //        // ServerOfflineObj.ST == 0：TCP明文 1：TCP信道明文 2：TCP信道密文
            //        SocketClient socketClient;
            //        if (sockets.ContainsKey(DLType.Common))
            //        {
            //            socketClient = sockets[DLType.Common];
            //        }
            //        else
            //        {
            //            sockets[DLType.Common] = socketClient = new SocketClient(url, Server.TCPPort, ConfigManager.Offline_Version, Server.ST);
            //        }
            //        //{"path":"ebank/resources/wp/common/css/test.css"}
            //        socketClient.Start(socketPath, successAction, failedAction);

            //        RYTLog.Log("Socket下载: " + socketPath);
            //    }
            //    else
            //    {
            //        string postPath = string.Empty;
            //        if(offlineInfo.ParentInfo==null)
            //        {
            //            postPath = string.Format("path={0}/{1}", offlineInfo.Path, offlineInfo.Name);
            //        }
            //        else
            //        {
            //            postPath = string.Format("path={0}/{1}/{2}", offlineInfo.ParentInfo.Path, offlineInfo.ParentInfo.Path.Replace(".zip", ""), offlineInfo.ParentInfo.Name);
            //        }
            //        //if (zipInfo != null && !zipInfo.ZipDLAll)
            //        //{
            //        //    postPath = string.Format("path={0}/{1}/{2}", zipInfo.Path, info.Path.Replace(".zip", ""), info.Name);
            //        //}
            //        //else
            //        //{
            //        //    postPath = string.Format("path={0}/{1}", info.Path, info.Name);
            //        //}
            //        if (parameter != null && parameter.ContainsKey(APPNAMEKEY) && !string.IsNullOrEmpty(parameter[APPNAMEKEY]))
            //            postPath += string.Format("&from={0}", parameter[APPNAMEKEY]);

            //        // ServerOfflineObj.ST == 0：HTTP明文 1：HTTP信道明文 2：HTTP信道密文 3：HTTPS
            //        if (Server.ST == 0 || Server.ST == 3)
            //        {
            //            PostClient post = new PostClient(Encoding.UTF8.GetBytes(postPath));
            //            post.DownloadStringCompleted += (s, e) =>
            //            {
            //                if (e.bytesResult != null)
            //                {
            //                    successAction(offlineInfo.Name, e.bytesResult);
            //                }
            //                else
            //                {
            //                    failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
            //                }
            //            };
            //            post.DownloadStringAsync(new Uri(Server.Host, UriKind.RelativeOrAbsolute));

            //        }
            //        else
            //        {
            //            HttpRequest req = new HttpRequest(Server.Host, postPath, Server.ST == 2);
            //            req.OnFailed += (error, status) =>
            //            {
            //                failedAction((isTCPFailed ? "Tcp->Http下载失败：" : "HTTP下载失败：") + error);
            //            };
            //            req.OnSuccess += (result, temp, response, headers) =>
            //            {
            //                successAction(offlineInfo.Name, temp);
            //            };
            //            req.Run(Server.ST);
            //        }
            //        RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + postPath);
            //    }
            //}

            //#endregion

            //#region 离线1.0

            //else
            //{
            //    if (Server.TCPPort != 0 && !isTCPFailed)
            //    {
            //        if (offlineInfo.ParentInfo == null)
            //        {
            //            socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", offlineInfo.Path, offlineInfo.Name);
            //        }
            //        else
            //        {
            //            socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", offlineInfo.ParentInfo.Path, offlineInfo.Path.Replace(".zip", ""), offlineInfo.Name);

            //        }
            //        //if (zipInfo != null && !zipInfo.ZipDLAll)
            //        //{
            //        //    socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", zipInfo.Path, info.Path.Replace(".zip", ""), info.Name);
            //        //}
            //        //else
            //        //{
            //        //    socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", info.Path, info.Name);
            //        //}
            //        string ip = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;

            //        SocketClient socketClient;
            //        if (sockets.ContainsKey(DLType.Common))
            //        {
            //            socketClient = sockets[DLType.Common];
            //        }
            //        else
            //        {
            //            sockets[DLType.Common] = socketClient = new SocketClient(ip, Server.TCPPort, ConfigManager.Offline_Version);
            //        }
            //        //{"path":"ebank/resources/wp/common/css/test.css"}
            //        socketClient.Start(socketPath, successAction, failedAction);

            //        RYTLog.Log("Socket下载: " + socketPath);

            //    }
            //    else
            //    {
            //        string url;
            //        if(offlineInfo.ParentInfo==null)
            //        {
            //            url = string.Format("{0}{1}/{2}?{3}", Server.Host, offlineInfo.Path, offlineInfo.Name, Guid.NewGuid().ToString());
            //        }
            //        else
            //        {
            //            url = string.Format("{0}{1}/{2}/{3}?{4}", Server.Host, offlineInfo.ParentInfo.Path, offlineInfo.Path.Replace(".zip", ""), offlineInfo.Name, Guid.NewGuid().ToString());
            //        }
            //        //if (zipInfo != null && !zipInfo.ZipDLAll)
            //        //{
            //        //    url = string.Format("{0}{1}/{2}/{3}?{4}", Server.Host, zipInfo.Path, info.Path.Replace(".zip", ""), info.Name, Guid.NewGuid().ToString());
            //        //}
            //        //else
            //        //{
            //        //    url = string.Format("{0}{1}/{2}?{3}", Server.Host, info.Path, info.Name, Guid.NewGuid().ToString());
            //        //}

            //        WebClient downloadClient = new WebClient();
            //        downloadClient.OpenReadCompleted += (s, e) =>
            //        {
            //            if (e.Error == null && e.Result != null)
            //            {
            //                byte[] result = RYTMainHelper.StreamToBytes(e.Result);
            //                successAction(offlineInfo.Name, result);
            //            }
            //            else
            //            {
            //                failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
            //            }
            //        };
            //        downloadClient.OpenReadAsync(new Uri(url, UriKind.RelativeOrAbsolute));
            //        RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + url);
            //    }

            //}

            //#endregion

            //offlineInfo.DownloadTimes++; 
            #endregion
        }
        private void DownloadMustFileAction(Queue<OfflineInfo> mustFilesQueue,List<OfflineInfo> downloadFaildList, Dictionary<string, string> parameter = null)
        {
            OfflineInfo offlineInfo = null;
            bool flag = true;            
            while (mustFilesQueue.Count > 0 && flag)
            {
                offlineInfo = mustFilesQueue.Dequeue();
                if (offlineInfo.DownloadTimes >= 2)
                    downloadFaildList.Add(offlineInfo);
                else
                    flag = false;
            }
            if (flag)//没有可下载的必选资源
            {
                IsMustFileDownloadCompleted = true;
                if (IsDownloadCompleted)
                    OnCompleted(true);                
                return;
            }
            offlineInfo.DownloadStatus = 1;

            #region 下载失败
            /*  TCP连接错误，如果支持TCP转HTTP下载 则 用HTTP下载
                当连续下载失败 超过5个则取消下载。
             */
            Action<string> failedAction = (errMsg) =>
            {
                Debug.WriteLine("*下载失败:* " + errMsg);
                offlineInfo.DownloadStatus = 0;
                mustFilesQueue.Enqueue(offlineInfo);//下载失败次数小于2次的再次加入下载队列，等待再次下载重试                
                failTimes++;
                if (errMsg.StartsWith(SocketClient.ErrFlag))
                {
                    // 连接失败后如果配置 支持 则 转http下载
                    if (ConfigManager.BeTcpToHttp)
                        isTCPFailed = true;   
                }
                if (failTimes < 5)
                    DownloadMustFileAction(mustFilesQueue, downloadFaildList, parameter);                    
                else
                {
                    if (ConfigManager.bOfflineEndHint)
                        MessageBox.Show(ConfigManager.NETWORK_ERROR, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
                    if (sockets.ContainsKey(DLType.Common))
                    {
                        if (sockets[DLType.Common] != null)
                            sockets[DLType.Common].SocketShutDowm();
                        sockets.Remove(DLType.Common);                                               
                    }
                    //设置下载失败列表并清空下载队列结束整个必选资源下载请求
                    downloadFaildList.AddRange(mustFilesQueue.ToList<OfflineInfo>());
                    mustFilesQueue.Clear();
                    DownloadMustFileAction(mustFilesQueue, downloadFaildList, parameter);                 
                }
            };
            #endregion

            #region 下载成功
            Action<string, byte[]> successAction = (path, result) =>
            {                
                var stream = new MemoryStream(result);
                if (SaveStreamToISO(offlineInfo, stream, Client_DESC))
                {
                    failTimes = 0;
                    OnDownLoadFile(++downNum);
                    DownloadMustFileAction(mustFilesQueue, downloadFaildList, parameter);                    
                }
                else
                {
                    failTimes++;
                    mustFilesQueue.Enqueue(offlineInfo);
                    if (failTimes < 5)
                        DownloadMustFileAction(mustFilesQueue, downloadFaildList, parameter);
                    else
                    {
                        if (ConfigManager.bOfflineEndHint)
                            MessageBox.Show(ConfigManager.OfflineStorageError, ConfigManager.OfflineResourcesTips, MessageBoxButton.OK);
                        if (sockets.ContainsKey(DLType.Common))
                        {
                            if (sockets[DLType.Common] != null)
                                sockets[DLType.Common].SocketShutDowm();
                            sockets.Remove(DLType.Common);
                        }                          
                        //设置下载失败列表并清空下载队列结束整个必选资源下载请求
                        downloadFaildList.AddRange(mustFilesQueue.ToList<OfflineInfo>());
                        mustFilesQueue.Clear();
                        DownloadMustFileAction(mustFilesQueue, downloadFaildList, parameter);
                    }
                }
                stream.Dispose();
            };

            #endregion

            string socketPath;
            #region 离线2.0

            if (DataBaseManager.Offline_Version.CompareTo("2.0") >= 0)
            {
                // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
                if (Server.TCPPort != 0 && !isTCPFailed)
                {
                    if (offlineInfo.ParentInfo == null)
                    {
                        socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", offlineInfo.Path, offlineInfo.Name);
                    }
                    else
                    {
                        socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", offlineInfo.ParentInfo.Path, offlineInfo.Path.Replace(".zip", ""), offlineInfo.Name);

                    }
                    
                    if (parameter != null && parameter.ContainsKey(APP_NAME) && !string.IsNullOrEmpty(parameter[APP_NAME]))
                        socketPath = socketPath.Insert(socketPath.Length - 1, string.Format(",\"from\":\"{0}\"", parameter[APP_NAME]));

                    string url = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;
                    // ServerOfflineObj.ST == 0：TCP明文 1：TCP信道明文 2：TCP信道密文
                    SocketClient socketClient;
                    if (sockets.ContainsKey(DLType.Common))
                    {
                        socketClient = sockets[DLType.Common];
                    }
                    else
                    {
                        sockets[DLType.Common] = socketClient = new SocketClient(url, Server.TCPPort, ConfigManager.Offline_Version, Server.ST);
                    }
                    //{"path":"ebank/resources/wp/common/css/test.css"}
                    socketClient.Start(socketPath, successAction, failedAction);

                    RYTLog.Log("Socket下载: " + socketPath);
                }
                else
                {
                    string postPath = string.Empty;
                    if (offlineInfo.ParentInfo == null)
                    {
                        postPath = string.Format("path={0}/{1}", offlineInfo.Path, offlineInfo.Name);
                    }
                    else
                    {
                        postPath = string.Format("path={0}/{1}/{2}", offlineInfo.ParentInfo.Path, offlineInfo.ParentInfo.Path.Replace(".zip", ""), offlineInfo.ParentInfo.Name);
                    }
                   
                    if (parameter != null && parameter.ContainsKey(APP_NAME) && !string.IsNullOrEmpty(parameter[APP_NAME]))
                        postPath += string.Format("&from={0}", parameter[APP_NAME]);

                    // ServerOfflineObj.ST == 0：HTTP明文 1：HTTP信道明文 2：HTTP信道密文 3：HTTPS
                    if (Server.ST == 0 || Server.ST == 3)
                    {
                        PostClient post = new PostClient(Encoding.UTF8.GetBytes(postPath));
                        post.DownloadStringCompleted += (s, e) =>
                        {
                            if (e.bytesResult != null)
                            {
                                successAction(offlineInfo.Name, e.bytesResult);
                            }
                            else
                            {
                                failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
                            }
                        };
                        post.DownloadStringAsync(new Uri(Server.Host, UriKind.RelativeOrAbsolute));

                    }
                    else
                    {
                        HttpRequest req = new HttpRequest(Server.Host, postPath, Server.ST == 2);
                        req.OnFailed += (error, status) =>
                        {
                            failedAction((isTCPFailed ? "Tcp->Http下载失败：" : "HTTP下载失败：") + error);
                        };
                        req.OnSuccess += (result, temp, response, headers) =>
                        {
                            successAction(offlineInfo.Name, temp);
                        };
                        req.Run(Server.ST);
                    }
                    RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + postPath);
                }
            }

            #endregion

            #region 离线1.0

            else
            {
                if (Server.TCPPort != 0 && !isTCPFailed)
                {
                    if (offlineInfo.ParentInfo == null)
                    {
                        socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", offlineInfo.Path, offlineInfo.Name);
                    }
                    else
                    {
                        socketPath = string.Format("{{\"path\":\"{0}/{1}/{2}\"}}", offlineInfo.ParentInfo.Path, offlineInfo.Path.Replace(".zip", ""), offlineInfo.Name);

                    }                    
                    string ip = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;

                    SocketClient socketClient;
                    if (sockets.ContainsKey(DLType.Common))
                    {
                        socketClient = sockets[DLType.Common];
                    }
                    else
                    {
                        sockets[DLType.Common] = socketClient = new SocketClient(ip, Server.TCPPort, ConfigManager.Offline_Version);
                    }
                    //{"path":"ebank/resources/wp/common/css/test.css"}
                    socketClient.Start(socketPath, successAction, failedAction);

                    RYTLog.Log("Socket下载: " + socketPath);

                }
                else
                {
                    string url;
                    if (offlineInfo.ParentInfo == null)
                    {
                        url = string.Format("{0}{1}/{2}?{3}", Server.Host, offlineInfo.Path, offlineInfo.Name, Guid.NewGuid().ToString());
                    }
                    else
                    {
                        url = string.Format("{0}{1}/{2}/{3}?{4}", Server.Host, offlineInfo.ParentInfo.Path, offlineInfo.Path.Replace(".zip", ""), offlineInfo.Name, Guid.NewGuid().ToString());
                    }                    
                    WebClient downloadClient = new WebClient();
                    downloadClient.OpenReadCompleted += (s, e) =>
                    {
                        if (e.Error == null && e.Result != null)
                        {
                            byte[] result = RYTMainHelper.StreamToBytes(e.Result);
                            successAction(offlineInfo.Name, result);
                        }
                        else
                        {
                            failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
                        }
                    };
                    downloadClient.OpenReadAsync(new Uri(url, UriKind.RelativeOrAbsolute));
                    RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + url);
                }

            }
            #endregion
            offlineInfo.DownloadTimes++;
        }
        public override void DownloadOptionalFile(string fileName, Action<bool> callBack, Dictionary<string, string> parameter = null)
        {
            OfflineInfo info = null;
            if (!Server.OptDownloadFileDic.ContainsKey(fileName))
            {
                RYTLog.Log("error : 可选资源描述中没有此文件：" + fileName);
                if (callBack != null)
                    callBack(false);
                return;                
            }
            else
            {
                info = Server.OptDownloadFileDic[fileName];
            }                
            info.DownloadStatus = 1;           
                                             
            #region 下载失败
            /*  TCP连接错误，如果支持TCP转HTTP下载 则 用HTTP下载
                当连续下载失败 超过5个则取消下载。
             */
            Action<string> failedAction = (errMsg) =>
            {
                Debug.WriteLine("*下载失败:* " + errMsg);
                info.DownloadStatus = 0;
                // socket 下载失败后如果配置 支持 则 转http下载
                if (ConfigManager.BeTcpToHttp)
                {
                    isTCPFailed = true;
                    DownloadOptionalFile(fileName, callBack, parameter);
                }  
                else
                {
                    callBack(false);
                    if (sockets.ContainsKey(DLType.Option))
                    {
                        sockets[DLType.Option].SocketShutDowm();
                        sockets.Remove(DLType.Option);
                    }
                }
            };

            #endregion

            #region 下载成功
            Action<string, byte[]> successAction = (path, result) =>
            {
                var stream = new MemoryStream(result);
                bool isSuccess = SaveStreamToISO(info, stream, Option_DESC);
                callBack(isSuccess);
                if (sockets.ContainsKey(DLType.Option))
                {
                    sockets[DLType.Option].SocketShutDowm();
                    sockets.Remove(DLType.Option);
                }                
                stream.Dispose();
            };

            #endregion
            string socketPath;
            #region 离线2.0

            if (DataBaseManager.Offline_Version.CompareTo("2.0") >= 0)
            {
                // http://192.168.64.128:4002/ebank/resources/wp/common/zip/payeeManage.zip
                if (Server.TCPPort != 0 && !isTCPFailed)
                {
                    socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", info.Path, info.Name);
                    if (parameter != null && parameter.ContainsKey(APP_NAME) && !string.IsNullOrEmpty(parameter[APP_NAME]))
                        socketPath = socketPath.Insert(socketPath.Length - 1, string.Format(",\"from\":\"{0}\"", parameter[APP_NAME]));

                    string url = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;
                    // ServerOfflineObj.ST == 0：TCP明文 1：TCP信道明文 2：TCP信道密文
                    SocketClient socketClient;
                    if (sockets.ContainsKey(DLType.Option))
                    {
                        socketClient = sockets[DLType.Option];
                    }
                    else
                    {
                        sockets[DLType.Option] = socketClient = new SocketClient(url, Server.TCPPort, ConfigManager.Offline_Version, Server.ST);
                    }
                    //{"path":"ebank/resources/wp/common/css/test.css"}
                    socketClient.Start(socketPath, successAction, failedAction);

                    RYTLog.Log("Socket下载: " + socketPath);
                }
                else
                {
                    string postPath = string.Empty;
                    postPath = string.Format("path={0}/{1}", info.Path, info.Name);
                    if (parameter != null && parameter.ContainsKey(APP_NAME) && !string.IsNullOrEmpty(parameter[APP_NAME]))
                        postPath += string.Format("&from={0}", parameter[APP_NAME]);

                    // ServerOfflineObj.ST == 0：HTTP明文 1：HTTP信道明文 2：HTTP信道密文 3：HTTPS
                    if (Server.ST == 0 || Server.ST == 3)
                    {
                        PostClient post = new PostClient(Encoding.UTF8.GetBytes(postPath));
                        post.DownloadStringCompleted += (s, e) =>
                        {
                            if (e.bytesResult != null)
                            {
                                successAction(info.Name, e.bytesResult);
                            }
                            else
                            {
                                failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
                            }
                        };
                        post.DownloadStringAsync(new Uri(Server.Host, UriKind.RelativeOrAbsolute));
                    }
                    else
                    {
                        HttpRequest req = new HttpRequest(Server.Host, postPath, Server.ST == 2);
                        req.OnFailed += (error, status) =>
                        {
                            failedAction((isTCPFailed ? "Tcp->Http下载失败：" : "HTTP下载失败：") + error);
                        };
                        req.OnSuccess += (result, temp, response, headers) =>
                        {
                            successAction(info.Name, temp);
                        };
                        req.Run(Server.ST);
                    }
                    RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + postPath);
                }
            }

            #endregion

            #region 离线1.0

            else
            {
                if (Server.TCPPort != 0 && !isTCPFailed)
                {
                    socketPath = string.Format("{{\"path\":\"{0}/{1}\"}}", info.Path, info.Name);
                    string ip = Server.Host.Contains("/") ? Server.Host.Split('/')[2].Split(':')[0] : Server.Host;

                    SocketClient socketClient;
                    if (sockets.ContainsKey(DLType.Option))
                    {
                        socketClient = sockets[DLType.Option];
                    }
                    else
                    {
                        sockets[DLType.Option] = socketClient = new SocketClient(ip, Server.TCPPort, ConfigManager.Offline_Version);
                    }
                    //{"path":"ebank/resources/wp/common/css/test.css"}
                    socketClient.Start(socketPath, successAction, failedAction);

                    RYTLog.Log("Socket下载: " + socketPath);

                }
                else
                {                   
                    string url = string.Format("{0}{1}/{2}?{3}", Server.Host, info.Path, info.Name, Guid.NewGuid().ToString());

                    WebClient downloadClient = new WebClient();
                    downloadClient.OpenReadCompleted += (s, e) =>
                    {
                        if (e.Error == null && e.Result != null)
                        {
                            byte[] result = RYTMainHelper.StreamToBytes(e.Result);
                            successAction(info.Name, result);
                        }
                        else
                        {
                            failedAction((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载：") + e.Error != null ? e.Error.Message : "失败");
                        }
                    };
                    downloadClient.OpenReadAsync(new Uri(url, UriKind.RelativeOrAbsolute));
                    RYTLog.Log((isTCPFailed ? "Tcp->Http下载：" : "HTTP下载： ") + url);
                }

            }
            #endregion
            info.DownloadTimes++;
        }
        private Dictionary<string, string> InfosToNameRevTable(Dictionary<string, OfflineInfo> infos, Dictionary<string, string> dic)
        {
            if (infos != null)
            {
                foreach (var item in infos)
                {
                    dic.Add(item.Key, item.Value.Rev);
                }
            }
            return dic;
        }        
        private string GetAppNamePath(string folderName)
        {
            if (!string.IsNullOrEmpty(AppName))
                folderName = AppName + "/" + folderName;
            return folderName;
        }
        #endregion        
    }
    public class DLType
    {
        public const string Option = "Option";
        public const string Common = "Common";
        public const string Page = "Page";
        public const string H5 = "H5";
    }
    public enum ResourceFileType
    {
        Text,
        Image,
        Media,
        Zip
    }
    public class RYTOfflineManager
    {
        const string DEFAULT_OFFLINE_KEY = "";
        public static string DefaultReadAppName = DEFAULT_OFFLINE_KEY;
        protected static Dictionary<string, RYTOfflineBase> AppOfflineDictionary = new Dictionary<string, RYTOfflineBase>();
        /// <summary>
        /// 获取应用程序默认的offline
        /// </summary>
        internal static RYTOfflineBase DefaultOffline
        {
            get
            {
                RYTOfflineBase offline = null;
                if (!AppOfflineDictionary.ContainsKey(DEFAULT_OFFLINE_KEY))
                {
                    if (ConfigManager.Offline_Version.CompareTo("1") >= 0)
                        offline = new RYTOffline(DEFAULT_OFFLINE_KEY);
                    else
                        offline = new RYTOfflineZero();
                    AppOfflineDictionary[DEFAULT_OFFLINE_KEY] = offline;
                }
                else
                {
                    if (AppOfflineDictionary[DEFAULT_OFFLINE_KEY] != null)
                    {
                        offline = AppOfflineDictionary[DEFAULT_OFFLINE_KEY];
                    }
                }
                return offline;
            }
        }
        /// <summary>
        /// 尝试获取一个Offine实例，如果没有则创建一个新实例
        /// </summary>
        /// <param name="appName">管理offline的字典的key</param>
        /// <returns></returns>
        internal static RYTOfflineBase GetOffline(string appName)
        {
            if (appName == null)
                appName = "";
            RYTOfflineBase offline = null;
            if (AppOfflineDictionary.ContainsKey(appName))
                offline = AppOfflineDictionary[appName];
            else
            {
                if (appName == DEFAULT_OFFLINE_KEY)
                    offline = DefaultOffline;
                else
                {
                    offline = new RYTOffline(appName);
                    AppOfflineDictionary[appName] = offline;
                }                    
            }
            return offline;
        }
        internal static void SetDefaultReadAppName(string appName)
        {
            DefaultReadAppName = appName;
        }
    }
}
