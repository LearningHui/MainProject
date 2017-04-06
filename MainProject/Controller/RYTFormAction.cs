using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using RYTong.MainProject.View;
using RYTong.MainProject.Model;
using RYTong.ControlLib;
using RYTong.DataBaseLib;
using RYTong.TLSLib;
using RYTong.ClassScriptParser;
using RYTong.ToolsLib;
using RYTong.LogLib;
using System.Threading.Tasks;

namespace RYTong.MainProject.Controller
{
    public class RYTFormAction
    {
        string url;
        string body;
        string methodType;

        RYTPageTransationBase pageTran = AppData.PageTransation_;
        TemplatePage newPage;
        TemplatePage currentPage;

        Action<string, int, WebHeaderCollection> actionWhenHttpCompleted;

        public RYTControl TargetRYTControl { get; set; }

        public bool bPageNavigation { get; set; }
        public HttpRequest HttpRequest { get; set; }

        #region Constructor

        public RYTFormAction()
        {

        }

        public RYTFormAction(Action<string, int, WebHeaderCollection> action, TemplatePage page)
        {
            actionWhenHttpCompleted = action;
            currentPage = page;
        }

        //public RYTFormAction(RYTControl control)
        //{

        //}

        #endregion

        #region Form Action

        public void Abort()
        {
            if (HttpRequest != null)
            {
                try
                {
                    HttpRequest.Abort();
                }
                catch
                {

                }
            }
        }

        public async void SubmitFormAction(RYTControl control)
        {
            RYTFormControl formControl = CommonHelper.FindParentFormControl(control);
            if (formControl == null)
            {
                return;
            }

            var keyValues = await loopFindFormChildSubmitKeyValues(formControl, null);
            string body = string.Empty;
            if (keyValues != null)
            {
                body = string.Join("&", keyValues.ToArray());
            }

            var url = formControl.ActionUrl_;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            SubmitFormAction(url, body, "POST");
        }

        public void SubmitFormAction(string _url, string _body, string methodType, bool bPageNavigate = false)
        {
            if (!_url.StartsWith(ConfigManager.SERVER_URL_WITH_PORT))
            {
                _url = string.Format("{0}{1}", ConfigManager.SERVER_URL_WITH_PORT, _url);
            }

            this.url = _url;
            this.url = ConfigManager.AppendUrlHightVersion(this.url);
            if (!string.IsNullOrEmpty(_body))
            {
                //this.body = Convert.ToBase64String
                this.body = _body.Replace("&amp;", "&");
            }

            this.methodType = methodType;
            if (this.methodType.Equals("get", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!string.IsNullOrEmpty(_body))
                {
                    this.url = string.Format("{0}?{1}", _url, _body);
                }
            }

            if (bPageNavigate)
            {
                newPage = new TemplatePage();
                pageTran.Navigate(newPage);
            }

            // 传明文body到tls，然后tls负责加密：
            HttpRequest = new HttpRequest(this.url, body);
            HttpRequest.OnSuccess += new HttpRequest.OnSuccessEventHandle(formSubmitRequest_OnSuccess);
            HttpRequest.OnFailed += new HttpRequest.OnFailedEventHandle(formSubmitRequest_OnFailed);
            HttpRequest.OnCanceled += new HttpRequest.OnCanceledEventHandle(formSubmitRequest_OnCanceled);
            HttpRequest.Run();
        }

        void formSubmitRequest_OnCanceled(string message)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                LogLib.RYTLog.Log(ConfigManager.HTTPREQUEST_CANCELED);
                if (ConfigManager.isTrackEnable)
                {
                    //RYTTrackLib.RYTTrack.Instanse.LogError(LogLib.RYTLog.Const.NetCancel, message);
                    RYTMainHelper.LogTrackError(LogLib.RYTLog.Const.NetCancel, message);
                }

                CheckToRemovePopPage();
                //RestoreCurrentPage();
            });
        }

        void formSubmitRequest_OnFailed(string error, WebExceptionStatus status)
        {
            if (status == WebExceptionStatus.RequestCanceled)
                return;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (actionWhenHttpCompleted != null)
                {
                    string errorStr = error ?? ConfigManager.SERVER_ERROR;
                    if (error.Equals("The remote server returned an error: NotFound.", StringComparison.CurrentCultureIgnoreCase))
                    {
                        errorStr = ConfigManager.CONNECTTION_ERROR;
                    }
                    actionWhenHttpCompleted(error, 404, null);
                }
                return;

                if (newPage != null)
                {
                    newPage.StopLoading();
                }

                if (!TasksHelper.IsInternetAvailable)
                {
                    MessageBox.Show(ConfigManager.NETWORK_ERROR, ConfigManager.Hint, MessageBoxButton.OK);
                    CheckToRemovePopPage();
                    RestoreCurrentPage();
                    return;
                }

                if (!string.IsNullOrEmpty(error))
                {
                    if (newPage != null)
                    {
                        newPage.StopLoading();
                    }

                    if (ConfigManager.isTrackEnable)
                    {
                        //RYTTrackLib.RYTTrack.Instanse.LogError(LogLib.RYTLog.Const.NetError, error);
                        RYTMainHelper.LogTrackError(LogLib.RYTLog.Const.NetError, error);
                    }

                    if (error.Equals("The remote server returned an error: NotFound.", StringComparison.CurrentCultureIgnoreCase) ||
                        error.Contains("error"))
                    {
                        MessageBox.Show(ConfigManager.CONNECTTION_ERROR, ConfigManager.Hint, MessageBoxButton.OK);
                    }
                    else
                    {
                        string errorMessage = AtomParser.ParserErrorText(error);
                        MessageBox.Show(errorMessage, ConfigManager.Hint, MessageBoxButton.OK);
                    }

                    CheckToRemovePopPage();
                }
                else
                {
                    MessageBox.Show(ConfigManager.SERVER_ERROR, ConfigManager.Hint, MessageBoxButton.OK);
                    CheckToRemovePopPage();
                    RestoreCurrentPage();
                }
            });
        }

        void formSubmitRequest_OnSuccess(string result, byte[] temp, int responseCode, WebHeaderCollection headers)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ParseFormRequest(result, responseCode, headers);
            });
        }

        void ParseFormRequest(string result, int responseCode, WebHeaderCollection headers)
        {
            if (currentPage != null && !currentPage.bActive)
            {
                return;
            }

            if (responseCode == 1599)
            {
                // 返回本地超时报文timeoutError.xml
                result = DataBaseManager.ReadAppPackageFile("timeoutError.xml", "text") as string;
            }

            //if (!TLS.IsTlsVersionBigger(1, 4))
            //{
            //    var bEncrypted = headers["X-Emp-Encrypted"];
            //    if (bEncrypted != null && bEncrypted.Equals("true", StringComparison.CurrentCultureIgnoreCase))
            //    {
            //        result = AESCipher.DoAesDecrypt(result);
            //    }
            //}

            if (result.Contains("<error"))
            {
                if (newPage != null)
                {
                    newPage.StopLoading();
                }

                string errorMessage = AtomParser.ParserErrorText(result);
                MessageBox.Show(errorMessage, ConfigManager.Hint, MessageBoxButton.OK);
                CheckToRemovePopPage();
                RestoreCurrentPage();

                return;
            }

            //if (!result.Contains(">"))
            //{
            //    result = AESCipher.DoAesDecrypt(result);
            //}

            if (actionWhenHttpCompleted != null)
            {
                actionWhenHttpCompleted(result, responseCode, headers);
            }

            if (TargetRYTControl == null)
            {
                if (bPageNavigation)
                {
                    //LuaScript.LuaManager.getInstance(this.newPage);
                    this.newPage = RYTMainHelper.NavigatePageByContentHTML(result, this.newPage, true);
                    if (this.newPage != null)
                        this.newPage.LuaManager.HistoryHtml.Add(result);

                    //if (this.pageNavigationWhenSuccess && !this.newPage.bEmptyBody)
                    //{
                    //    AppData.PageTransation_.Navigate(this.newPage);
                    //}
                    //else
                    //{
                    //    RestoreCurrentPage();
                    //}
                }
            }

            #region Table Control Update
            else
            {
                //RYTClassParser parser = new RYTClassParser();
                //string xmlContent = DataBaseLib.DataBaseManager.ReadAppPackageFile("class.xml", "text") as string;
                //List<Dictionary<string, Dictionary<string, string>>> objects = parser.parserClassScriptForResource(xmlContent);
                //XElement rootXhtml = XElement.Parse(result);

                //RYTCreateControls cc = new RYTCreateControls(AppData.LastBuildCSSStyles);
                //List<RYTControl> topControls = cc.CreateAllObjects(rootXhtml, objects, null);
                XElement rootXhtml = XElement.Parse(result);
                List<RYTControl> topControls = InitializeControl.CreateAllObjects(rootXhtml, this.newPage);

                if (TargetRYTControl is RYTTableControl)
                {
                    var oldTableControl = TargetRYTControl as RYTTableControl;
                    //var newTableControl = CommonHelper.FindFirstMatchedRYTControl<RYTTableControl>(controlList,
                    //    c => (c is RYTTableControl) && !string.IsNullOrEmpty((c as RYTTableControl).NextUrl_));

                    //if (newTableControl != null)
                    //{
                    //    oldTableControl.AppendNewGridDataToCurrentTable(newTableControl);
                    //}

                    if (topControls.Count == 1 && topControls[0] is RYTTableControl)
                    {
                        var newTableControl = topControls[0] as RYTTableControl;
                        newTableControl.BuildChildControls();
                        oldTableControl.AppendNewGridDataToCurrentTable(topControls[0] as RYTTableControl);
                    }
                }
            }
            #endregion
        }

        #endregion

        #region Help Method

        private void CheckToRemovePopPage()
        {
            if (this.currentPage != null && this.currentPage.PopPage != null)
            {
                this.currentPage.LayoutRoot.Children.Remove(this.currentPage.PopPage);
                this.currentPage.PopPage = null;
            }
        }

        private void RestoreCurrentPage()
        {
            if (pageTran.TopShowPage_ != null)
            {
                (pageTran.TopShowPage_ as TemplatePage).ReloadLuaScript();
            }
        }

        private async Task<List<string>> loopFindFormChildSubmitKeyValues(RYTControl ParentControl, string[] controlNames)
        {
            List<string> result = null;
            List<RYTControl> controls = LoopFindSubmitControl(ParentControl);

            foreach (var control in controls)
            {
                string keyValue = await GetControlNameAndValue(control);
                if (!string.IsNullOrEmpty(keyValue))
                {
                    if (result == null)
                    {
                        result = new List<string>();
                    }
                    result.Add(keyValue);

                    #region Set Name-Text to IsolatedStorage if tureSave = "yes"

                    if (control is RYTInputControl)
                    {
                        var inputControl = control as RYTInputControl;
                        if (inputControl.TrueSave_)
                        {
                            DataBaseManager.SetIsolatedKeyValue(inputControl.Name_, RYTong.DataBaseLib.RYTSecurity.Instance.Encrypt(inputControl.GetDefaultOrInputOrSelectedValue()));
                        }
                    }
                    else if (control is RYTPasswordControl)
                    {
                        var passwordControl = control as RYTPasswordControl;
                        if (passwordControl.TrueSave_)
                        {
                            DataBaseManager.SetIsolatedKeyValue(passwordControl.Name_, RYTong.DataBaseLib.RYTSecurity.Instance.Encrypt(passwordControl.GetDefaultOrInputOrSelectedValue()));
                        }
                    }

                    #endregion
                }
            }
            return result;
        }

        private List<RYTControl> LoopFindSubmitControl(RYTControl ParentControl)
        {
            List<RYTControl> controls = new List<RYTControl>();

            foreach (var control in ParentControl.ChildrenElements_)
            {
                controls.Add(control);

                var childResults = LoopFindSubmitControl(control);
                controls.AddRange(childResults);
            }

            return controls;
        }

        private async Task<string> GetControlNameAndValue(RYTControl control)
        {
            string keyValue = string.Empty;
            //var name = control.GetAttributeValueWithAttName("name");
            var name = control.Name_;
            var value = control.GetDefaultOrInputOrSelectedValue();

            if (!string.IsNullOrEmpty(name)) //&& !string.IsNullOrEmpty(value)
            {
                if (control is RYTSegmentControl)
                {
                    var segement = control as RYTSegmentControl;
                    value = segement.GetCheckedValue();
                }
                if ((control is RYTCheckBoxControl || control is RYTRadioControl) && value == string.Empty)
                {
                    return string.Empty;
                }
                if (control is RYTLabelControl)
                {
                    return string.Empty;
                }
                if (control is RYTSwitchControl)
                {
                    value = (control as RYTSwitchControl).SelectedValue_;
                }

                // 加密
                var mode = control.GetAttributeValueWithAttName("encryptMode");
                if (!string.IsNullOrEmpty(mode))
                {
                    if (value == null) value = string.Empty;
                    value = await encryptValue(mode, value.Trim(), control);
                }

                // 对Value值进行URI编码。
                keyValue = string.Format("{0}={1}", name.Trim(), HttpUtility.UrlEncode(value));
            }

            return keyValue;
        }

        private async Task<string> encryptValue(string mode, string p, RYTControl control)
        {
            Boolean opt = false;
            string res = TemplatePage.encryptValue(mode, p, ref opt);
            if (opt)
            {
                Dictionary<string, object> param = new Dictionary<string,object>();
                param["p1"] = p;
                Dictionary<string, string> modes = new Dictionary<string,string>();
                modes["p1"] = mode;
                res = await doOTP(param, modes);
            }
            return res;
        }

        public async Task<string> doOTP(Dictionary<String, object> parameters, Dictionary<String, String> modes)
        {
            
            // ClientRandom
            byte[] cUnixTime = TLS.ClientGmtUnixTime;
            byte[] clientRandom = TLS.GetClientRandom(28);
            byte[] RNC = TLS.UnionBytes(cUnixTime, clientRandom);

            byte[] serverBytes = null;

            if (!TLS.IsTlsVersionBigger(1, 4))
            {
                serverBytes = await GetData(RNC);
            }
            else
            {
                TLSKey.OneSecretOneTimeClientRandom = RNC;
                serverBytes = TLSKey.RandomServer;
            }

            byte[] seed = TLS.UnionBytes(RNC, serverBytes);
            byte[] secret = HMac.Prf(TLSKey.PreMasterSecret2, HMac.TlsOnceSecretConst(), seed, 48);
            var offset = 0;
            byte[] key = new byte[32];
            Array.Copy(secret, offset, key, 0, key.Length);
            offset += key.Length;
            byte[] iv = new byte[16];
            Array.Copy(secret, offset, iv, 0, iv.Length);

            Dictionary<String, object>.Enumerator e = (new Dictionary<string, object>(parameters)).GetEnumerator();
            while (e.MoveNext())
            {
                KeyValuePair<String, object> k = e.Current;
                String mode = modes[k.Key];
                if (mode.Equals("01") || mode.Equals("E1") || mode.Equals("AE1"))
                {
                    // 动态加密        
                    byte[] data = AESCipher.DoAesEncrypt(System.Text.UTF8Encoding.UTF8.GetBytes((String)k.Value), key, iv);
                    parameters[k.Key] = Convert.ToBase64String(data);
                }
                else if (mode.Equals("A1"))
                {
                    var base64Bytes = Convert.FromBase64String((string)k.Value);
                    byte[] data = AESCipher.DoAesEncrypt(base64Bytes, key, iv);
                    parameters[k.Key] = Convert.ToBase64String(data);
                }
            }
            return (string)parameters["p1"];

        }

        async Task<byte[]> GetData(byte[] RNC)
        {
            bool isDone = false;

            Task<byte[]> waitTask = new Task<byte[]>(() =>
            {
                while (!isDone)
                {
                    Task.Delay(10000);
                }
                return RNC;
            });

            String rncStr = Convert.ToBase64String(RNC);
            String url = ConfigManager.SERVER_URL_WITH_PORT;
            url += ConfigManager.CombineClientMakeOTP();
            String body = "clientrandom=" + URIEncode.escapeURIComponent(rncStr);
            var request = new HttpRequest(url, body, false, false);

            request.OnSuccess += (result, bytes, responseCode, responseHeaders) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (result == null)
                        {
                            //throw new Exception("网络连接失败！");
                            RYTLog.ShowMessage("网络连接失败");
                        }
                        if (result.IndexOf("<error") != -1)
                        {
                            String error = RYTUtils.getXMLResponseAttribute(result, "string=\"", 0, RYTUtils.MATCH);
                            //throw new Exception(error);
                            RYTLog.ShowMessage(error);
                        }
                        String serverRandom = RYTUtils.getXMLResponseAttribute(result, "serverrandom=\"", 0, RYTUtils.MATCH);
                        if (serverRandom == null)
                        {
                            RYTLog.ShowMessage(result);
                            isDone = true;
                            return;
                        }
                        byte[] RNS = Convert.FromBase64String(serverRandom);

                        RNC = RNS;
                        isDone = true;
                    });
            };
            request.OnFailed += (error, status) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    //StopLoading();
                    MessageBox.Show(ConfigManager.CONNECTTION_ERROR);
                    isDone = true;
                });
            };
            request.Run();
            waitTask.Start();
            return await waitTask;
        }

        

        #endregion
    }
}
