//
//  RYTMainHelper
//
//  Created by wang.ping on 2/16/12.
//  Copyright 2011 RYTong. All rights reserved.
//

using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.Phone.Shell;
using RYTong.ControlLib;
using Microsoft.Phone.Controls;
using System.Collections.Generic;
using RYTong.TLSLib;
using System.Text;
using RYTong.MainProject.View;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using RYTong.DataBaseLib;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Diagnostics;
using RYTong.LuaScript;

namespace RYTong.MainProject.Controller
{
    /// <summary>
    /// Common Helper in RYTMainHelper
    /// </summary>
    public static class RYTMainHelper
    {
        #region WP Navigation

        public static bool NavigationBack()
        {
            //PhoneApplicationFrame root = Application.Current.RootVisual as PhoneApplicationFrame;
            var currentPage = (Application.Current.RootVisual as PhoneApplicationFrame).Content as PhoneApplicationPage;
            if (currentPage != null && currentPage.NavigationService.CanGoBack)
            {
                currentPage.NavigationService.GoBack();
                return true;
            }

            return false;
        }

        #endregion

#if DEBUG
        public static TimeSpan dtRunLua;
#endif

        #region Page Navigate Helper

        /// <summary>
        /// NavigatePage By Html
        /// </summary>
        /// <param name="localFilePath">like /Resource/1.xml</param>
        /// <returns>RYTTemplatePage instance</returns>
        public static TemplatePage NavigatePageByLocalFile(Uri localFilePath, TemplatePage page = null, Action action = null)
        {
            string html = GetLocalFileContentString(localFilePath);
            if (!string.IsNullOrEmpty(html))
            {
                if (page != null)
                {
                    page = NavigatePageByContentHTML(html, page);
                }
                else
                {
                    page = NavigatePageByContentHTML(html);
                }
                if (action != null)
                {
                    action();
                }                
            }
            return page;
        }

        /// <summary>
        /// Navigate Page By HTMLContent (HTML Node With Content)
        /// </summary>
        /// <param name="html">html</param>
        /// <returns>RYTTemplatePage instance</returns>
        public static TemplatePage NavigatePageByContentHTML(string html, TemplatePage templatePage = null, bool bNavigation = true, TransitionType tType = TransitionType.none, bool bOneTimePage = false)
        {
            if (string.IsNullOrEmpty(html))
                return null;
            html = RYTMainHelper.FormatXML(html);
            html = html.Trim();
            if (html.Contains("<error"))
            {
                string errorMessage = AtomParser.ParserErrorText(html);
                MessageBox.Show(errorMessage, ConfigManager.Hint, MessageBoxButton.OK);
                return null;
            }
            if (templatePage == null)
            {
                templatePage = new TemplatePage();
                templatePage.bOneTimePage = bOneTimePage;
            }            
            try
            {
                
                if (LuaManager.RootLuaManager == null)
                {
                    LuaManager luaInstance = LuaManager.GetRootLuaManager();                    
                    LuaScriptExtend.DelegatesManager.SetExtendDelegates();                    
                    string localscript = DataBaseManager.ReadFileByType("RYTL.lua", "text") as string;
                    luaInstance.loadLuaString(localscript);
                    localscript = DataBaseManager.ReadFileByType("SLT2content.lua", "text") as string;
                    luaInstance.loadLuaString(localscript);                    
                }
                
                html = LuaManager.RootLuaManager.doSLTParaser(html);
                templatePage.LuaManager = LuaManager.RootLuaManager;
                templatePage.LuaManager.DetailV_ = templatePage;
                if (html.StartsWith("Error"))
                    LogLib.RYTLog.ShowMessage("SLT2 parser Error");
                templatePage.HTML_ = html;                
                List<string> scriptList = new List<string>();
                FrameworkElement fe = null;
                XElement rootXhtml = XElement.Parse(html);
                templatePage.RootXML_ = rootXhtml;
#if DEBUG
                DateTime dtStart = DateTime.Now;
#endif
                var controlList = AtomParser.ParseHTML_New(rootXhtml, scriptList,templatePage, out fe);
                templatePage.InitControls(fe, controlList);
                templatePage.RunLuaScript(scriptList);
#if DEBUG
                dtRunLua = DateTime.Now - dtStart - AtomParser.dtControl - AtomParser.dtLayout;
#endif
                templatePage.StopLoading();

                if (bNavigation)
                {
                    
                    AppData.PageTransation_.Navigate(templatePage, bNavigation, tType);
                }
            }
            catch (Exception e)
            {
                e.Source = html;
                RYTMainHelper.ExceptionHandle(e);
            }
            return templatePage;
        }

        public static void InitPageByContentHTML(TemplatePage templatePage, string html)
        {
            if (string.IsNullOrEmpty(html))
                return;
            try
            {
                html = html.Trim();
                List<string> scriptList = new List<string>();
                FrameworkElement fe = null;
                XElement rootXhtml = XElement.Parse(html);
                var controlList = AtomParser.ParseHTML_New(rootXhtml, scriptList, templatePage, out fe);
                templatePage.InitControls(fe, controlList);
                templatePage.RunLuaScript(scriptList);
            }
            catch { }
        }

        public static string GetLocalFileContentString(Uri localFilePath)
        {
            try
            {
                var fileInfo = Application.GetResourceStream(localFilePath);
                if (fileInfo != null)
                {
                    string content = string.Empty;
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(fileInfo.Stream))
                    {
                        content = reader.ReadToEnd();
                        fileInfo.Stream.Dispose();
                    }
                    return content;
                }
            }
            catch { }

            return string.Empty;
        }

        #endregion

        #region PhoneApplicationService

        public static void AddOrUpdatePhoneApplicationServiceKeyValue(string key, object value)
        {
            if (PhoneApplicationService.Current.State.ContainsKey(key))
            {
                PhoneApplicationService.Current.State[key] = value;
            }
            else
            {
                PhoneApplicationService.Current.State.Add(key, value);
            }
        }

        public static object GetPhoneApplicationServiceValue(string key)
        {
            if (PhoneApplicationService.Current.State.ContainsKey(key))
            {
                return PhoneApplicationService.Current.State[key];
            }

            return null;
        }

        public static T GetPhoneApplicationServiceValue<T>(string key) where T : class
        {
            object obj;
            if (PhoneApplicationService.Current.State.TryGetValue(key, out obj))
            {
                return obj as T;
            }

            return null;
        }

        #endregion

        #region Others

        public static string GenEnvelopeKey()
        {
            byte[] bytes = null;
            try
            {
                bytes = new byte[8];
                Random rd = new Random((int)DateTime.Now.Ticks);

                // the value of first byte is between 128 and 255
                int key = 129 + Math.Abs(rd.Next()) % 128;
                bytes[0] = (byte)key;

                int cnt = 7;
                do
                {
                    int tmp = Math.Abs(rd.Next()) % 255;
                    bytes[8 - cnt] = (byte)tmp;
                    cnt--;
                } while (cnt > 0);

                return Convert.ToBase64String(bytes).Substring(0, 8);
            }
            catch
            {
                ExceptionHandle(null, "创建密钥出错");
            }

            return null;
        }

        public static Grid FindTemplatePageLayoutGrid(RYTControl control)
        {
            var tempControl = control;
            while (tempControl.Parent_ != null)
            {
                tempControl = tempControl.Parent_;
            }

            var tempFE = tempControl.View_;
            while (tempFE.Parent != null)
            {
                if (tempFE is TemplatePage)
                {
                    break;
                }

                tempFE = tempFE.Parent as FrameworkElement;
            }

            if (tempFE != null && tempFE is TemplatePage)
            {
                return (tempFE as TemplatePage).LayoutRoot;
            }

            return null;
        }

        public static long ToUtcTimeLong(DateTime time)
        {
            var utcTime = time.ToUniversalTime();
            DateTime dtZone = new DateTime(1970, 1, 1, 0, 0, 0);

            return (long)utcTime.Subtract(dtZone).TotalSeconds;
        }

        /// <summary>
        /// XML 字符转义
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string FormatXML(string html)
        {
#if DEBUG
            //&(逻辑与) & amp;
            //< (小于) & lt;
            //> (大于) & gt;
            //"(双引号)  &quot;      
            //'(单引号)  &apos; 
            if (!html.Contains("&amp;") && !html.Contains("&lt;") && !html.Contains("&gt;"))
            {
                html = html.Replace("&", "&amp;");
            }
#endif
            return html;
        }

        #endregion

        #region Exceptions Handle Quit App Programatically

        public class QuitException : Exception { }

        public static void Quit()
        {
            //new Microsoft.Xna.Framework.Game().Exit();
            throw new QuitException();
            //Application.Current.Terminate();
        }

        /// <summary>
        /// 错误处理相关提示
        /// </summary>
        /// <param name="e">异常对象</param>
        /// <param name="notice">提示内容</param>
        /// <param name="title">提示标题</param>
        public static void ExceptionHandle(Exception e, string notice = "", string title = "")
        {
            if (!LogLib.RYTLog.IsLogOpen)
                return;

            // 普通错误提示
            if (e == null)
            {
                LogLib.RYTLog.ShowMessage(notice, title);
                return;
            }
            // 报文错误提示
            if (e is System.Xml.XmlException)
            {
                string detail = "";
                int line = 0;
                if (!string.IsNullOrEmpty(e.Source) && !e.Source.StartsWith("System.Xml"))
                {
                    string[] lineArray = e.Source.Split('\n');

                    line = (e as System.Xml.XmlException).LineNumber;
                    if (line - 1 < lineArray.Length && line - 1 >= 0)
                    {
                        detail = string.Concat("↘\n", lineArray[line - 1]);
                        if (line - 2 > 0)
                        {
                            detail = string.Concat(lineArray[line - 2], "\n", detail);
                        }
                        if (line < lineArray.Length)
                        {
                            detail = string.Concat(detail, "\n\n", lineArray[line]);
                        }
                    }
                }
                var message = string.Concat(e.Message, "\n\n", detail);
                LogLib.RYTLog.ShowMessage(message, LogLib.RYTLog.Const.XmlError);
                if (ConfigManager.isTrackEnable)
                {
                    //RYTTrackLib.RYTTrack.Instanse.LogError(LogLib.RYTLog.Const.XmlError, message, line);
                    LogTrackError(LogLib.RYTLog.Const.XmlError, message, line);
                }
            }
            // 退出App
            else if (!string.IsNullOrEmpty(e.Message) && e.Message.Equals("ryt:close()", StringComparison.CurrentCultureIgnoreCase))
            {
                RYTMainHelper.Quit();
            }
            // 其他提示
            else
            {
                var message = string.Concat(notice, "\n", e.Message, "\n", e.StackTrace ?? "");
                LogLib.RYTLog.ShowMessage(message, title);
                if (ConfigManager.isTrackEnable)
                {
                    //RYTTrackLib.RYTTrack.Instanse.LogError("unknown", message);
                    LogTrackError("unknown", message);
                }
            }
        }

        #endregion

        #region Page Navigate By Http Request (GET)

        public static void NavigatePageByHttpRequest(Action action, string xmlPostFix = "login.xml")
        {
            string url = ConfigManager.CombineUrl(xmlPostFix);

            HttpRequest request = new HttpRequest(url, null, false, false);
            request.OnFailed += (error, status) =>
                {
                    if (action != null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() => { action(); });
                    }

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            MessageBox.Show(ConfigManager.NETWORK_ERROR, ConfigManager.Hint, MessageBoxButton.OK);
                        });
                };

            request.OnSuccess += (result, bytes, response, headers) =>
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        RYTMainHelper.NavigatePageByContentHTML(result);

                        if (action != null)
                        {
                            action();
                        }
                    });
                };

            request.Run();
        }

        #endregion

        #region Set Secret Key & IV

        public static void CreateAndSetSecretKeyIV()
        {
            // Message = password + IMEI + deviceName + string1 + string2 + string3
            StringBuilder sb = new StringBuilder();
            sb.Append(CommonHelper.GetDeviceUniqueId());
            sb.Append(CommonHelper.GetDeviceName());
            sb.Append("11111111");
            sb.Append("22222222");
            sb.Append("33333333");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var md5 = HMac.DoMd5(bytes);
            var sha1 = HMac.DoSha1(bytes);

            var secret = new byte[md5.Length + sha1.Length];
            md5.CopyTo(secret, 0);
            sha1.CopyTo(secret, md5.Length);

            // key：从secret中顺序取前32字节
            // iv：从secret中倒序取后16字节
            var key = secret.SubArray(0, 32);
            var iv = secret.SubArray(secret.Length - 16, 16);

            AppData.Key = key;
            AppData.IV = iv;
        }

        #endregion

        #region SHA1

        /// <summary>
        /// Using UTF8 Encoding
        /// </summary>
        public static bool CompareSHA1(string value, byte[] bytes, string baseValue)
        {
            if (!string.IsNullOrEmpty(value))
            {
                bytes = Encoding.UTF8.GetBytes(value);
            }

            return CompareSHA1(bytes, baseValue);
        }

        public static bool CompareSHA1(Stream stream, string baseValue)
        {
            byte[] data = StreamToBytes(stream);
            return CompareSHA1(data, baseValue);

            //var computedHash = (new System.Security.Cryptography.SHA1Managed()).ComputeHash(stream);

            //StringBuilder EnText = new StringBuilder();
            //foreach (byte iByte in computedHash)
            //{
            //    EnText.AppendFormat("{0:x2}", iByte);
            //}
            //string computedStringValue = EnText.ToString();

            //if (computedStringValue.Equals(baseValue, StringComparison.CurrentCultureIgnoreCase))
            //{
            //    return true;
            //}

            //return false;
        }

        public static bool CompareSHA1(byte[] bytes, string baseValue)
        {
            return RYTSecurity.Instance.CompareSHA1(bytes, baseValue);
        }

        #endregion

        #region Stream & Bytes Converter

        public static byte[] StreamToBytes(System.IO.Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            return bytes;
        }

        public static Stream BytesToStream(byte[] bytes)
        {
            Stream stream = new MemoryStream(bytes);
            return stream;
        }

        #endregion

        #region 远程Lua脚本下载辅助类

        public class DownloadRomoteLuaString
        {
            #region Fields

            private List<string> luaScriptList_ = new List<string>();

            #endregion

            #region Event

            public delegate void DownloadLuaEventHandler();
            public event DownloadLuaEventHandler DownloadLuaCompleted;

            #endregion

            #region  method

            public DownloadRomoteLuaString(List<string> luaScript)
            {
                this.luaScriptList_ = luaScript;
            }

            public void run()
            {
                //如果不存在远程下载,则立即返回
                if (CheckTaskOverCompleted())
                    return;
            }

            private void DownLoadLua()
            {
                int index = 0;
                while (!luaScriptList_[index].StartsWith("/"))
                {
                    index++;
                }

                //每次只下载集合里面的第一个 带“/”的url
                if (luaScriptList_[index].StartsWith("/"))
                {
                    string url = ConfigManager.SERVER_URL_WITH_PORT + luaScriptList_[index].TrimStart('/');
                    var request = new HttpRequest(url, null, false, false);
                    RegistRequestEvent(request, index);
                    request.Run();
                }
            }

            private void RegistRequestEvent(HttpRequest request, int index)
            {
                request.OnSuccess += (string result, byte[] temp, int responseCode, System.Net.WebHeaderCollection headers) =>
                {
                    luaScriptList_[index] = result;
                    CheckTaskOverCompleted();
                };
                request.OnFailed += (string error, WebExceptionStatus status) =>
                {
                    luaScriptList_[index] = "error";
                    CheckTaskOverCompleted();
                };
            }

            private bool CheckTaskOverCompleted()
            {
                bool result = false;

                if (luaScriptList_.Any(c => c.StartsWith("/")))
                {
                    this.DownLoadLua();
                }
                else
                {
                    result = true;
                    if (DownloadLuaCompleted != null)
                    {
                        DownloadLuaCompleted();
                    }
                }

                return result;
            }

            #endregion
        }

        #endregion

        #region 开发模式 , 请求Ewp远程文件。

        public class DownloadRomoteEwpFile
        {
            public string filePath = string.Empty;
            string serverUrl = ConfigManager.SERVER_URL_WITH_PORT + "/test_s/get_page";
            public bool LOADED = false;

            public delegate void DownloadCompltedEventHandler(string content, byte[] temp, bool succeed);
            public event DownloadCompltedEventHandler DownloadComplted;

            public DownloadRomoteEwpFile(string path)
            {
                filePath = path;
            }

            // 请求test_s/get_page接口
            public void run()
            {
                var url = ConfigManager.AppendUrlHightVersion(serverUrl);
                string body = string.Empty;

                body = "name=" + "channels/" + filePath;
                body += string.Format("&platform=wp&resolution={0}*{1}", RYTong.ControlLib.Constant.ScreenWidth, RYTong.ControlLib.Constant.ScreenHeight);

                var request = new HttpRequest(url, body);
                request.OnSuccess += request_OnSuccess;
                request.OnFailed += request_OnFailed;
                request.Run();
                Debug.WriteLine("---开发模式:尝试下载:" + filePath + ",..........");
            }

            void request_OnFailed(string error, WebExceptionStatus status)
            {
                this.LOADED = true;
                Debug.WriteLine("---开发模式:下载失败:" + filePath);
                if (DownloadComplted != null)
                {
                    DownloadComplted(string.Empty, null, false);
                }
            }

            void request_OnSuccess(string result, byte[] temp, int responseCode, WebHeaderCollection reponseHeaders)
            {
                if (temp == null)
                {
                    return;
                }

                string data = Encoding.UTF8.GetString(temp, 0, temp.Length);
                string key = Convert.ToBase64String(AESCipher.ServerKey, 0, AESCipher.ServerKey.Length);
                string iv = Convert.ToBase64String(AESCipher.ServerIv, 0, AESCipher.ServerIv.Length);
                this.LOADED = true;
                Debug.WriteLine("---开发模式:下载成功:" + filePath + " !");
                if (DownloadComplted != null)
                {
                    string content = result;
                    if (!TLS.IsTlsVersionBigger(1, 4))
                        content = AESCipher.DoAesDecrypt(result);
                    DownloadComplted(content, temp, true);
                }
            }
        }

        public static class LoadEwpImgHost
        {
            public static List<DownloadRomoteEwpFile> fileLoadList = new List<DownloadRomoteEwpFile>();
            public static bool isLoading = false;

            public static void CheckLoad()
            {
                var firstLoad = fileLoadList.FirstOrDefault(i => i.LOADED == false);
                if (firstLoad != null && !isLoading)
                {
                    firstLoad.run();
                    isLoading = true;
                    firstLoad.DownloadComplted += delegate { isLoading = false; CheckLoad(); };
                }
            }

            public static void Load(BitmapImage bitmapImage, string imgPath)
            {
                if (!AppData.IsTlsConnection)
                    return;

                DownloadRomoteEwpFile loadImg = new DownloadRomoteEwpFile(imgPath);
                loadImg.DownloadComplted += (content, temp, succeed) =>
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (succeed)
                        {
                            try
                            {
                                var imageBytes = temp;
                                if (!TLS.IsTlsVersionBigger(1, 4))
                                    imageBytes = AESCipher.DoAesDecrypt(temp);
                                using (var ms = new MemoryStream(imageBytes))
                                {
                                    //尝试解析为图片，用于判断数据是否错误。
                                    bitmapImage.SetSource(ms);
                                }
                                DataBaseManager.SaveBytesFile(imageBytes, imgPath, "Temp");
                            }
                            catch
                            {
                                Debug.WriteLine("---开发模式:" + imgPath + "解析失败！");
                            }
                        }
                    });
                };
                fileLoadList.Add(loadImg);
                CheckLoad();
            }
        }

        /// <summary>
        /// 搜索所有需要下载的文件，包含CSS/LUA/Image文件。
        /// </summary>
        /// <param name="html"></param>
        /// <param name="delayAction"></param>
        public static void PreParseXmlForFileLoad(string html, Action<bool> delayAction)
        {
            string result = string.Empty;
            XElement xml = null;
            List<string> urlTaskList = new List<string>();
            try
            {
                xml = XElement.Parse(html);
                var linkList = xml.Descendants("link").ToList();
                var scriptList = xml.Descendants("script").ToList();
                List<Action> someActions = new List<Action>();

                foreach (var link in linkList)
                {
                    var refAttr = link.Attribute("ref");
                    if (refAttr != null)
                    {
                        var path = refAttr.Value;
                        if (!DataBaseManager.IsFileExist(path))
                        {
                            urlTaskList.Add(path);
                        }
                    }
                }

                foreach (var script in scriptList)
                {
                    var src = script.Attribute("src");
                    if (src != null)
                    {
                        var path = src.Value;
                        if (path == "RYTL.lua")
                        { continue; }
                        if (!DataBaseManager.IsFileExist(path))
                        {
                            urlTaskList.Add(path);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.Source = html;
                ExceptionHandle(e);
                return;
            }

            if (xml != null)
            {
                var styleList = xml.Descendants("style").ToList();
                var imgTags = xml.Descendants("img").ToList();
                var scripts = xml.Descendants("script").ToList();
                foreach (var img in imgTags)
                {
                    var src = img.Attribute("src");
                    if (src != null)
                    {
                        var imgPath = src.Value;
                        int index = imgPath.IndexOf("local:");
                        if (index != -1)
                        {
                            imgPath = imgPath.Substring(index + 6); // 6 is 'local:' length   
                        }
                        if (!DataBaseManager.IsFileExist(imgPath))
                        {
                            if (!urlTaskList.Any(u => u == imgPath))
                                urlTaskList.Add(imgPath);
                        }
                    }
                }

                /*
                foreach (var style in styleList)
                {
                    var styleContent = style.Value;
                    List<RYTCSSStyle> cssStyleList = new List<RYTCSSStyle>();
                    RYTControl.initAllStyleWithSource(styleContent, cssStyleList);
                    foreach (var css in cssStyleList)
                    {
                        var imgPath = css.bgImageUrl_;
                        if (!string.IsNullOrEmpty(imgPath) && !DataBaseManager.IsFileExist(imgPath))
                        {
                            if (!urlTaskList.Any(u => u == imgPath))
                                urlTaskList.Add(imgPath);
                        }
                    }
                }
                */
                // 正则表达式：不以.. 开头的"***.png字符串。
                Regex reg = new Regex(@"(?<!\.\.\s*)[\""].*?\.png");
                Action<string> loop = null;
                loop = (r) =>
                {
                    foreach (var mat in reg.Matches(r))
                    {
                        var imgPath = mat.ToString().TrimStart('\"').Trim();
                        if (imgPath.Contains("\""))
                        {
                            loop(imgPath);
                        }
                        else
                        {
                            int index = imgPath.IndexOf("local:");
                            if (index != -1)
                            {
                                imgPath = imgPath.Substring(index + 6);
                            }
                            index = imgPath.IndexOf("(");
                            if (index != -1)
                            {
                                imgPath = imgPath.Substring(index + 1);
                            }
                            if (!urlTaskList.Any(u => u == imgPath))
                                if (!DataBaseManager.IsFileExist(imgPath))
                                {
                                    urlTaskList.Add(imgPath);
                                }
                        }
                    }
                };

                foreach (var script in scripts)
                {
                    var scriptContent = script.Value;
                    loop(scriptContent);
                }

                if (urlTaskList.Count == 0)
                {
                    delayAction(false);
                }
                else
                {
                    System.Windows.Controls.Primitives.Popup popShow = new System.Windows.Controls.Primitives.Popup();
                    popShow.Width = 480; popShow.Height = Application.Current.Host.Content.ActualHeight;
                    StackPanel sp_ = new StackPanel { };
                    popShow.Child = sp_;
                    popShow.IsOpen = true;
                    sp_.Children.Add(new TextBlock { Text = "进入开发模式..." });

                    List<DownloadRomoteEwpFile> fileLoadList = new List<DownloadRomoteEwpFile>();
                    Action loadAction = delegate
                    {
                        var firstLoad = fileLoadList.FirstOrDefault(i => i.LOADED == false);
                        if (firstLoad != null)
                        {
                            firstLoad.run();
                            sp_.Children.Add(new TextBlock { Text = "正在下载:" + firstLoad.filePath + " ..." });
                        }
                        else
                        {
                            sp_.Children.Add(new TextBlock { Text = "下载结束." });
                            delayAction(false);
                            popShow.IsOpen = false;
                        }
                    };
                    foreach (var filePath in urlTaskList)
                    {
                        DownloadRomoteEwpFile fileLoad = new DownloadRomoteEwpFile(filePath);
                        fileLoadList.Add(fileLoad);
                        fileLoad.DownloadComplted += (content, temp, succeed) =>
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(delegate
                            {
                                if (succeed)
                                {
                                    try
                                    {
                                        var imageBytes = temp;
                                        if (!TLS.IsTlsVersionBigger(1, 4))
                                        {
                                            imageBytes = AESCipher.DoAesDecrypt(temp);
                                        }

                                        if (filePath.EndsWith(".png"))
                                            using (var ms = new MemoryStream(imageBytes))
                                            {
                                                //尝试解析为图片，用于判断数据是否错误。
                                                BitmapImage bi = new BitmapImage();
                                                bi.SetSource(ms);
                                            }
                                        DataBaseManager.SaveBytesFile(imageBytes, filePath, "Temp");
                                    }
                                    catch
                                    {
                                        Debug.WriteLine("---开发模式:" + filePath + "解析失败！");
                                    }
                                }
                                loadAction();
                            });
                        };
                    }
                    loadAction();
                }
            }
            else
            {
                delayAction(false);
            }
        }

        /// <summary>
        /// 搜索所有需要下载的文件，包含CSS/LUA/Image文件。
        /// </summary>
        /// <param name="html"></param>
        /// <param name="delayAction"></param>
        public static Dictionary<string, OfflineInfo> PreParseXmlForFileLoad(string html)
        {
            string result = string.Empty;
            XElement xml = null;
            Dictionary<string, OfflineInfo> urlTaskList = new Dictionary<string, OfflineInfo>();
            try
            {
                xml = XElement.Parse(html);
                var linkList = xml.Descendants("link").ToList();
                var scriptList = xml.Descendants("script").ToList();
                List<Action> someActions = new List<Action>();

                foreach (var link in linkList)
                {
                    var refAttr = link.Attribute("ref");
                    if (refAttr != null)
                    {
                        var path = refAttr.Value;
                        if (!DataBaseManager.IsFileExist(path))
                        {
                            urlTaskList[path] = new OfflineInfo();
                        }
                    }
                }

                foreach (var script in scriptList)
                {
                    var src = script.Attribute("src");
                    if (src != null)
                    {
                        var path = src.Value;
                        if (path == "RYTL.lua")
                        { continue; }
                        if (!DataBaseManager.IsFileExist(path))
                        {
                            urlTaskList[path] = new OfflineInfo();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.Source = html;
                ExceptionHandle(e);
                return urlTaskList;
            }

            if (xml != null)
            {
                var styleList = xml.Descendants("style").ToList();
                var imgTags = xml.Descendants("img").ToList();
                var scripts = xml.Descendants("script").ToList();
                foreach (var img in imgTags)
                {
                    var src = img.Attribute("src");
                    if (src != null)
                    {
                        var imgPath = src.Value;
                        int index = imgPath.IndexOf("local:");
                        if (index != -1)
                        {
                            imgPath = imgPath.Substring(index + 6); // 6 is 'local:' length   
                        }
                        if (!DataBaseManager.IsFileExist(imgPath))
                        {
                            if (!urlTaskList.Any(u => u.Key.Equals(imgPath)))
                                urlTaskList[imgPath] = new OfflineInfo();
                        }
                    }
                }
                #region css file old way
                /*
                foreach (var style in styleList)
                {
                    var styleContent = style.Value;
                    List<RYTCSSStyle> cssStyleList = new List<RYTCSSStyle>();
                    RYTControl.initAllStyleWithSource(styleContent, cssStyleList);
                    foreach (var css in cssStyleList)
                    {
                        var imgPath = css.bgImageUrl_;
                        if (!string.IsNullOrEmpty(imgPath) && !DataBaseManager.IsFileExist(imgPath))
                        {
                            if (!urlTaskList.Any(u => u == imgPath))
                                urlTaskList.Add(imgPath);
                        }
                    }
                }
                */
                #endregion

                // 正则表达式：不以.. 开头的"***.png字符串。
                Regex reg = new Regex(@"(?<!\.\.\s*)[\""].*?\.png");
                Action<string> loop = null;
                loop = (r) =>
                {
                    foreach (var mat in reg.Matches(r))
                    {
                        var imgPath = mat.ToString().TrimStart('\"').Trim();
                        if (imgPath.Contains("\""))
                        {
                            loop(imgPath);
                        }
                        else
                        {
                            int index = imgPath.IndexOf("local:");
                            if (index != -1)
                            {
                                imgPath = imgPath.Substring(index + 6);
                            }
                            index = imgPath.IndexOf("(");
                            if (index != -1)
                            {
                                imgPath = imgPath.Substring(index + 1);
                            }
                            if (!urlTaskList.Any(u => u.Key.Equals(imgPath)))
                                if (!DataBaseManager.IsFileExist(imgPath))
                                {
                                    urlTaskList[imgPath] = new OfflineInfo();
                                }
                        }
                    }
                };

                foreach (var script in scripts)
                {
                    var scriptContent = script.Value;
                    loop(scriptContent);
                }
                foreach (var style in styleList)
                {
                    var styleContent = style.Value;
                    loop(styleContent);
                }
            }
            return urlTaskList;
        }


        #endregion

        #region 统计信息发送/回掉接口

        public static bool bEncryChannel = false;

        #region InitTrack
        public static object TrackInstance { get; set; }
        public static Type TrackType { get; set; }
        public static void InitTrack(List<Action> appNavigatedToActionList, List<Action> appNavigatedFromActionList)
        {
            System.Reflection.Assembly trackLibAssembly = System.Reflection.Assembly.Load("RYTong.RYTTrackLib");
            TrackType = trackLibAssembly.GetType("RYTong.RYTTrackLib.RYTTrack");
            System.Reflection.PropertyInfo propertyInfo = TrackType.GetProperty("Instanse");
            TrackInstance = propertyInfo.GetValue(null, null);

            //System.AppDomain _Domain = System.AppDomain.CurrentDomain;          
            //System.Reflection.Assembly[] _AssemblyList = _Domain.GetAssemblies();
            //for (int i = 0; i != _AssemblyList.Length; i++)
            //{
            //    if (_AssemblyList[i].GetName().Name.Equals("RYTong.RYTTrackLib"))
            //    {
            //        System.Reflection.Assembly trackLib = _AssemblyList[i];
            //        //System.Reflection.Assembly aseembly = System.Reflection.Assembly.Load("RYTong.RYTTrackLib");
            //        TrackType = trackLib.GetType("RYTong.RYTTrackLib.RYTTrack");
            //        System.Reflection.PropertyInfo propertyInfo = TrackType.GetProperty("Instanse");
            //        TrackInstance = propertyInfo.GetValue(null, null);
            //        break;
            //    }
            //}

            if (TrackType == null)
            {
                LogLib.RYTLog.Log("Get Track Type error");
                return;
            }
            //set clientVersion
            System.Reflection.MethodInfo SetAppVersion_MethodInfo = TrackType.GetMethod("SetAppVersion");
            if (SetAppVersion_MethodInfo != null)
            {
                SetAppVersion_MethodInfo.Invoke(TrackInstance, new object[] { ConfigManager.CLIENT_VERSION });
            }

            //TODO: set send delegate-->RYTMainHelper.TrackRequestHandle
            System.Reflection.MethodInfo SetTrackHubAction_MethodInfo = TrackType.GetMethod("SetTrackHubAction");
            if (SetTrackHubAction_MethodInfo != null)
            {
                Action<string, Action<bool, string>> action = RYTMainHelper.TrackRequestHandle;
                SetTrackHubAction_MethodInfo.Invoke(TrackInstance, new object[] { action, true });
            }
            StartSession_Track(ConfigManager.Track_apiKey);
            // 切入App时执行StartSession
            appNavigatedToActionList.Add(delegate
            {
                StartSession_Track(ConfigManager.Track_apiKey);
            });

            // 切出App时立即执行EndSession
            System.Reflection.MethodInfo PauseSession_MethodInfo = TrackType.GetMethod("PauseSession");
            System.Reflection.MethodInfo EndSession_MethodInfo = TrackType.GetMethod("EndSession");
            appNavigatedFromActionList.Add(delegate
            {
                if (SetTrackHubAction_MethodInfo != null)
                {
                    PauseSession_MethodInfo.Invoke(TrackInstance, null);
                }

            });
            App.Current.UnhandledException += (s, e) =>
            {
                if (!e.ExceptionObject.GetType().Name.Equals("QuitException"))
                {
                    if (ConfigManager.isTrackEnable)
                    {
                        LogTrackError("ERROR", e.ExceptionObject.Message);
                    }

                }
                if (SetTrackHubAction_MethodInfo != null)
                {
                    EndSession_MethodInfo.Invoke(TrackInstance, new object[]{false,null});
                }

                App.Current.Terminate();
            };
        }
        public static void StartSession_Track(string apiKey)
        {
            if (TrackType == null || TrackInstance == null)
            {
                LogLib.RYTLog.Log("Get Track Type error");
                return;
            }
            System.Reflection.MethodInfo StartSession_MethodInfo = TrackType.GetMethod("StartSession");
            if (StartSession_MethodInfo != null)
            {
                StartSession_MethodInfo.Invoke(TrackInstance, new object[] { apiKey });
            }
        }
        public static void LogTrackError(string name, string message = "", int lineNumber = 0)
        {
            if (TrackType == null || TrackInstance == null)
            {
                LogLib.RYTLog.Log("Get Track Type error");
                return;
            }
            System.Reflection.MethodInfo LogError_MethodInfo = TrackType.GetMethod("LogError");
            if (LogError_MethodInfo != null)
            {
                LogError_MethodInfo.Invoke(TrackInstance, new object[] { name, message, lineNumber });
            }
        }

        #endregion
        public static void TrackRequestHandle(string body, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(body))
            {
                LogLib.RYTLog.Log("TrackRequestHandle called error");
                callback(false, null);
                return;
            }

            string serverUrl = string.Concat(ConfigManager.TrackServerUrl, bEncryChannel ? "stats/collect_s" : "stats/collect");
            //serverUrl = ConfigManager.AppendUrlTrackVersion(serverUrl);
            HttpRequest request = new HttpRequest(serverUrl, body, bEncryChannel, bEncryChannel);
            request.OnSuccess += (string result, byte[] temp, int responseCode, WebHeaderCollection reponseHeaders) =>
                {
                    if (bEncryChannel)
                        result = AESCipher.DoAesDecrypt(result);
                    callback(true, result);
                };
            request.OnFailed += (string error, WebExceptionStatus status) =>
                {
                    callback(false, error);
                    if (ConfigManager.isTrackEnable)
                    {
                        //RYTTrackLib.RYTTrack.Instanse.LogError(LogLib.RYTLog.Const.NetError, error);
                        LogTrackError(LogLib.RYTLog.Const.NetError, error);
                    }
                };
            request.OnCanceled += (string message) =>
                {
                    callback(false, message);
                    if (ConfigManager.isTrackEnable)
                    {
                        //RYTTrackLib.RYTTrack.Instanse.LogError(LogLib.RYTLog.Const.NetCancel, message);
                        LogTrackError(LogLib.RYTLog.Const.NetCancel, message);
                    }
                };
            if (bEncryChannel)
            {
                request.Run();
            }
            else
            {
                request.RunHttpRequest();
            }
        }

        #endregion

        #region 报文采集类

        public static class OutPutXml
        {
            public static void OutPut(XElement xe)
            {
                string html = xe.ToString();
                int newLen = html.Length;
                string title = string.Empty;

                //-采集规则:
                XElement x = (from item in xe.Descendants("label") where true select item).FirstOrDefault();
                if (x != null)
                {
                    title = x.Value;
                }
                try
                {
                    using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!file.DirectoryExists("xml"))
                        {
                            file.CreateDirectory("xml");
                        }
                        IsolatedStorageFileStream isf = file.OpenFile("xml/list.txt", FileMode.OpenOrCreate);
                        StreamReader sr = new StreamReader(isf);
                        bool bExsit = false;

                        if (isf.Length > 0)
                        {
                            string result = sr.ReadToEnd();
                            List<string> fList = result.Split('\n').ToList();
                            foreach (var f in fList)
                            {
                                string[] name = f.Split('|');
                                if (name.Count() >= 2)
                                {
                                    string n = name[0];
                                    int length;
                                    if (int.TryParse(name[1], out length))
                                    {
                                        if (newLen == length)
                                        {
                                            bExsit = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (bExsit)
                        {
                            return;
                        }
                        else
                        {
                            string fileName = title.Trim();

                            using (StreamWriter sw = new StreamWriter(isf))
                            {
                                sw.WriteLine(fileName + "|" + newLen + "|" + DateTime.Now.ToString());
                            }

                            IsolatedStorageFileStream fileStream = file.CreateFile("xml/" + fileName + "." + newLen + ".txt");
                            using (StreamWriter sw = new StreamWriter(fileStream))
                            {
                                sw.Write(html);
                            }
                        }

                        sr.Close();
                        isf.Dispose();
                    }
                }
                catch
                { }
            }
        }

        #endregion
    }

    #region Lua执行线程封装类

    public class LuaThread
    {
        public static List<LuaThread> LuaThreadList = new List<LuaThread>();

        private Thread _thread = null;
        private int _id = 0;

        public Thread Thread
        {
            get { return _thread; }
        }
        public int Id
        {
            get { return _id; }
        }

        public List<Action<object>> ActionList;

        public LuaThread(ThreadStart start)
        {
            _thread = new Thread(start) { Name = "sub" };
            _id = _thread.ManagedThreadId;
            ActionList = new List<Action<object>>();

            LuaThreadList.Add(this);
        }

        public void Run()
        {
            this.Thread.Start();
        }
    }

    #endregion
}



