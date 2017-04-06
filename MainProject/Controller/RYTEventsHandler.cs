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
using RYTong.ControlLib;
using RYTong.ExtendControlLib;
using RYTong.TLSLib;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using RYTong.ChartControlLib;
using RYTong.MainProject.View;

namespace RYTong.MainProject.Controller
{
    public static class LuaScriptEventAction
    {
        public static void OnLuaEventAction(RYTControl control, string luaMethod)
        {
            Page page = null;

            if (control is RYTOptionControl)
            {
                var selectControl = control.Parent_;
                page = CommonHelper.FindCurrentPage(selectControl);
            }
            else
            {
                page = CommonHelper.FindCurrentPage(control);
            }

            if (page == null)
            {
                var fe = control.View_;
                if (fe == null)
                {
                    fe = control.Parent_.View_;
                }
                page = CommonHelper.FindCurrentPage(control.View_);
            }

            try
            {
                View.TemplatePage templatePage = page as View.TemplatePage;
                if(templatePage!=null)
                {
                    templatePage.LuaManager.DetailV_ = templatePage;
                    templatePage.LuaManager.performLuaFunction(luaMethod);
                }
            }
            catch (Exception e)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!string.IsNullOrEmpty(e.Message))
                        {
                            RYTMainHelper.ExceptionHandle(e, "@Function : " + luaMethod, LogLib.RYTLog.Const.LuaPortError);
                        }
                    });
            }
        }
    }

    public static class TableEventAction
    {
        public static void ScrollToBottomHandler(RYTTableControl tableControl, string nextUrl)
        {
            string url = ConfigManager.ConvertURLPrefix(nextUrl);
            string body = ConfigManager.ConvertURLPostfix(nextUrl);

            RYTFormAction action = new RYTFormAction() { TargetRYTControl = tableControl };
            action.SubmitFormAction(url, body, "POST", false);
        }
    }

    public static class AlinkEventAction
    {
        public static void ClickEventHandler(RYTAControl control, LinkType type, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                switch (type)
                {
                    case LinkType.HTTP:
                        TasksHelper.StartWebBrowserTask(value);
                        break;
                    case LinkType.Local:
                        string fileContent = CommonHelper.ReadLocalDataString(value);
                        if (!string.IsNullOrEmpty(fileContent))
                        {
                            TemplatePage page = CommonHelper.FindCurrentPage(control) as TemplatePage;
                            if(page !=null)
                                page.LuaManager.HistoryHtml.Add(fileContent);// save html
                            else
                                LogLib.RYTLog.Log("can't find current page error in static method ClickEventHandler()");
                            RYTMainHelper.NavigatePageByContentHTML(fileContent);
                        }
                        break;
                    case LinkType.Mail:
                        var mailInfo = RYTAControl.ConvertMailInfo(value);
                        TasksHelper.StartEmailTask(mailInfo.Body, mailInfo.To, mailInfo.Subject, mailInfo.CC, mailInfo.BCC);
                        break;
                    case LinkType.SMS:
                        TasksHelper.StartSMSTask(value);
                        break;
                    case LinkType.TEL:
                        TasksHelper.StartPhoneCallTask(control.Title_, value);
                        break;
                    case LinkType.None:
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public static class ButtonEventAction
    {
        public static void ClickEventAction(object sender, EventArgs e)
        {
            RYTButtonControl control = sender as RYTButtonControl;
            if (control == null)
                return;

            if (control.IsSubmit)
            {
                var action = new RYTFormAction() { bPageNavigation = true };
                action.SubmitFormAction(control);
            }
        }
    }

    public static class PieChartEventAction
    {
        public static void SelectedEventAction(RYTPieChartControl control, PieChartInfo selectedInfo)
        {

        }
    }

    public static class ImageEventAction
    {
        public static void EWPImageDownloadAction(RYTImageControl control, FrameworkElement img, string ewpUrl)
        {
            //“emp_local://img/1.png?w=100&h=150”
            string serverUrl = ConfigManager.SERVER_URL_WITH_PORT + "/map/get_pic";
            serverUrl = ConfigManager.AppendUrlHightVersion(serverUrl);
            string body = string.Empty;
            int index = ewpUrl.IndexOf("|");
            string url_;//-传给ewp参数url
            if (index != -1)
            {
                url_ = ewpUrl.Substring(0, index);
                body = "url=" + HttpUtility.UrlEncode(url_);
                body += "&" + ewpUrl.Substring(index + 1);
            }
            else
            {
                body = "url=" + HttpUtility.UrlEncode(ewpUrl);
            }

            //var Request = new HttpRequest(serverUrl, body, false, false);
            var Request = new HttpRequest(serverUrl, body);

            Request.OnSuccess += (result, temp, responseCode, headers) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {                            
                            var imageBytes = temp;
                            if (!TLS.IsTlsVersionBigger(1, 4))
                                imageBytes = AESCipher.DoAesDecrypt(temp);

                            //System.IO.Stream stream = CommonHelper.GetImageStream("giftest.gif");
                            //Stream stream1 = new MemoryStream(imageBytes);
                            //if (img is Controls.Toolkit.RYTImageGIF)
                            //{
                            //    //(img as Controls.Toolkit.RYTImageGIF).SetImagGIFSource(stream);
                            //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://img.sc115.com/uploads/png/110125/2011012514020588.png");
                            //    request.BeginGetResponse(
                            //        (result1) =>
                            //        {
                            //            HttpWebRequest request1 = (HttpWebRequest)result1.AsyncState;
                            //            WebResponse response = request1.EndGetResponse(result1);
                            //            Stream st = response.GetResponseStream();
                            //            //(img as Controls.Toolkit.RYTImageGIF).SetImagGIFSource(st);
                                      

                            //        }, request);
                            //    return;
                            //}


                            using (var ms = new MemoryStream(imageBytes))
                            {                                
                                BitmapImage bi = new BitmapImage();
                                bi.SetSource(ms);
                                
                                // 没有上传参数时 根据图片像素大小显示
                                if (index == -1)
                                {
                                    if (control !=null && control is RYTImageControl)
                                    {
                                        img.Width = bi.PixelWidth / RYTong.ControlLib.Constant.PXScale;
                                        img.Height = bi.PixelHeight / RYTong.ControlLib.Constant.PXScale;
                                    }
                                    //if (img.Tag is RYTButtonControl)
                                    //{
                                    //    RYTButtonControl btn = img.Tag as RYTButtonControl;
                                    //    if (btn.View_.ActualHeight != 0 && btn.View_.ActualHeight < img.Height)
                                    //    {
                                    //        img.ClearValue(FrameworkElement.WidthProperty);
                                    //        img.ClearValue(FrameworkElement.HeightProperty);
                                    //    }

                                    //}                                    
                                    
                                }  
                                if(img is Image)
                                {
                                    (img as Image).Source = bi;
                                }
                                else if (img is Controls.Toolkit.RYTImageGIF)
                                {
                                    (img as Controls.Toolkit.RYTImageGIF).SetImagGIFSource(ms);
                                }
                                if (img.Tag is RYTButtonControl)
                                {
                                    RYTButtonControl btn = img.Tag as RYTButtonControl;
                                    btn.BuildChildControls();
                                    //if (btn.View_.ActualHeight != 0 && btn.View_.ActualHeight < img.Height)
                                    //{
                                    //    img.ClearValue(FrameworkElement.WidthProperty);
                                    //    img.ClearValue(FrameworkElement.HeightProperty);
                                    //}

                                }      
                                
                            }
                        }
                        catch (Exception e)
                        {

                            if (control != null && !string.IsNullOrEmpty(control.GetAttributeValueWithAttName("failed")) && control is RYTImageControl)
                            {
                                control.image_.Source = CommonHelper.GetImageBrush(control.GetAttributeValueWithAttName("failed")).ImageSource;
                            }
                            Debug.WriteLine("Ewp图片解析异常.." + e.Message + "@" + ewpUrl);
                        }
                    });
            };
            Request.OnFailed += (error, state) =>
            {
                if (control != null && !string.IsNullOrEmpty(control.GetAttributeValueWithAttName("failed")) && control is RYTImageControl)
                {
                    control.image_.Source = CommonHelper.GetImageBrush(control.GetAttributeValueWithAttName("failed")).ImageSource;
                }
                Debug.WriteLine("Ewp图片下载失败.." + error + "@" + ewpUrl);
            };

            Request.Run();
        }

        public static void EWPButtonImageDownloadAction(RYTControl control, Image img,bool isLeftImage, string ewpUrl)
        {
            //“emp_local://img/1.png?w=100&h=150”
            string serverUrl = ConfigManager.SERVER_URL_WITH_PORT + "/map/get_pic";
            serverUrl = ConfigManager.AppendUrlHightVersion(serverUrl);
            string body = string.Empty;
            int index = ewpUrl.IndexOf("|");
            string url_;//-传给ewp参数url
            if (index != -1)
            {
                url_ = ewpUrl.Substring(0, index);
                body = "url=" + HttpUtility.UrlEncode(url_);
                body += "&" + ewpUrl.Substring(index + 1);
            }
            else
            {
                body = "url=" + HttpUtility.UrlEncode(ewpUrl);
            }

            //var Request = new HttpRequest(serverUrl, body, false, false);
            var Request = new HttpRequest(serverUrl, body);

            Request.OnSuccess += (result, temp, responseCode, headers) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        var imageBytes = temp;
                        if (!TLS.IsTlsVersionBigger(1, 4))
                            imageBytes = AESCipher.DoAesDecrypt(temp);                        

                        using (var ms = new MemoryStream(imageBytes))
                        {
                            BitmapImage bi = new BitmapImage();
                            bi.SetSource(ms);

                            // 没有上传参数时 根据图片像素大小显示
                            if (index == -1)
                            {
                                //if (control != null && control is RYTImageControl)
                                //{
                                //    img.Width = bi.PixelWidth / RYTong.ControlLib.Constant.PXScale;
                                //    img.Height = bi.PixelHeight / RYTong.ControlLib.Constant.PXScale;
                                //}                                                             

                            }
                            if (img !=null)
                            {
                                img.Source = bi;
                            }
                            if (isLeftImage)
                            {
                                (control as RYTButtonControl).LeftBitmapImage = bi;
                                img.Margin = (control as RYTButtonControl).LeftImageThickness;
                            }                                
                            else
                            {
                                (control as RYTButtonControl).RightBitmapImage = bi;
                                img.Margin = (control as RYTButtonControl).RightImageThickness;
                            }
                                
                            (control as RYTButtonControl).BuildChildControls();                            
                        }
                    }
                    catch (Exception e)
                    {

                        if (control != null && !string.IsNullOrEmpty(control.GetAttributeValueWithAttName("failed")) && control is RYTImageControl)
                        {
                            //control.image_.Source = CommonHelper.GetImageBrush(control.GetAttributeValueWithAttName("failed")).ImageSource;
                        }
                        Debug.WriteLine("Ewp图片解析异常.." + e.Message + "@" + ewpUrl);
                    }
                });
            };
            Request.OnFailed += (error, state) =>
            {
                if (control != null && !string.IsNullOrEmpty(control.GetAttributeValueWithAttName("failed")) && control is RYTImageControl)
                {
                    //control.image_.Source = CommonHelper.GetImageBrush(control.GetAttributeValueWithAttName("failed")).ImageSource;
                }
                Debug.WriteLine("Ewp图片下载失败.." + error + "@" + ewpUrl);
            };

            Request.Run();
        }

        public static void EWPSegmentTitleImageDownloadAction(RYTControl control, string ewpUrl)
        {
            //“emp_local://img/1.png?w=100&h=150”
            string serverUrl = ConfigManager.SERVER_URL_WITH_PORT + "/map/get_pic";
            serverUrl = ConfigManager.AppendUrlHightVersion(serverUrl);
            string body = string.Empty;
            int index = ewpUrl.IndexOf("|");
            string url_;//-传给ewp参数url
            if (index != -1)
            {
                url_ = ewpUrl.Substring(0, index);
                body = "url=" + HttpUtility.UrlEncode(url_);
                body += "&" + ewpUrl.Substring(index + 1);
            }
            else
            {
                body = "url=" + HttpUtility.UrlEncode(ewpUrl);
            }

            //var Request = new HttpRequest(serverUrl, body, false, false);
            var Request = new HttpRequest(serverUrl, body);

            Request.OnSuccess += (result, temp, responseCode, headers) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        var imageBytes = temp;
                        if (!TLS.IsTlsVersionBigger(1, 4))
                            imageBytes = AESCipher.DoAesDecrypt(temp);

                        using (var ms = new MemoryStream(imageBytes))
                        {
                            BitmapImage bi = new BitmapImage();
                            bi.SetSource(ms);
                            ImageBrush imageBrush = new ImageBrush() { ImageSource = bi,Stretch=Stretch.Fill};
                            (control as RYTSegmentControl).SegmentButton.TitleImageBrush = imageBrush;
                            (control as RYTSegmentControl).SegmentButton.Content = null;
                            
                        }
                    }
                    catch (Exception e)
                    {
                        
                        Debug.WriteLine("Ewp_SegmentTitleImage图片解析异常.." + e.Message + "@" + ewpUrl);
                    }
                });
            };
            Request.OnFailed += (error, state) =>
            {
                Debug.WriteLine("Ewp_SegmentTitleImage图片下载失败.." + error + "@" + ewpUrl);
            };

            Request.Run();
        }
    }
}
