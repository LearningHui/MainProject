//
//  RYTOfflineStorageManager
//
//  Created by wang.ping on 09/11/12.
//  Copyright 2011 RYTong. All rights reserved.
//

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
using Microsoft.Phone.Notification;
using System.IO;
using System.Diagnostics;
using System.Text;
using RYTong.ControlLib;
using RYTong.TLSLib;

namespace RYTong.MainProject.Controller
{
    public class RYTPushManager
    {
        public HttpNotificationChannel HttpChannel { get; private set; }
        public const string ChannelName = "rytChannel";

        public RYTPushManager()
        {
        }

        public void Start()
        {
            HttpChannel = HttpNotificationChannel.Find(ChannelName);
            if (HttpChannel == null || HttpChannel.ChannelUri == null)
            {
                if (HttpChannel != null)
                {
                    HttpChannel.Close();
                    HttpChannel.Dispose();
                }
                HttpChannel = new HttpNotificationChannel(ChannelName, "NotificationService");
                HttpChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(httpchanel_ChannelUriUpdated);

                HttpChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(httpchanel_ErrorOccurred);
                HttpChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(httpchanel_HttpNotificationReceived);

                //程序运行时处理toast;
                HttpChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(httpchanel_ShellToastNotificationReceived);

                HttpChannel.Open();

                //程序不在运行时处理toast;
                HttpChannel.BindToShellToast();

                //程序不在运行时处理tile;
                HttpChannel.BindToShellTile();
            }
            else
            {
                HttpChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(httpchanel_ChannelUriUpdated);
                HttpChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(httpchanel_ErrorOccurred);
                HttpChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(httpchanel_HttpNotificationReceived);

                //程序运行时处理toast;
                HttpChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(httpchanel_ShellToastNotificationReceived);

                RegisterEwpPushHandle();
            }

        }

        void RegisterEwpPushHandle()
        {
            //Debug.WriteLine("ChannelUri:{0}", e.ChannelUri.AbsoluteUri);
            if (HttpChannel != null && HttpChannel.ChannelUri != null)
            {
                // Here to send e.ChannelUri to RYT Server
                string url = ConfigManager.PushServerUrl;
                string body = string.Format("token={0}&uid={1}&os=winphone&client=ebank",
                                            HttpUtility.UrlEncode(HttpChannel.ChannelUri.AbsoluteUri.ToString()),
                                            HttpUtility.UrlEncode(CommonHelper.GetDeviceUniqueId()));

                HttpRequest request = new HttpRequest(url, body, false, false);
                request.OnSuccess += new HttpRequest.OnSuccessEventHandle(request_OnSuccess);
                request.OnFailed += new HttpRequest.OnFailedEventHandle(request_OnFailed);
                request.RunHttpRequest();

                string msg = string.Format("向IP:[{0}]发送了推送Url:[{1}]", url, HttpChannel.ChannelUri.AbsoluteUri);
            }
        }

        //程序运行时处理toast;
        void httpchanel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            StringBuilder message = new StringBuilder();
            string relativeUri = string.Empty;

            message.AppendFormat("Received Toast {0}:\n", DateTime.Now.ToShortTimeString());

            // Parse out the information that was part of the message.
            string title = string.Empty;
            string content = string.Empty;

            foreach (string key in e.Collection.Keys)
            {
                message.AppendFormat("{0}: {1}\n", key, e.Collection[key]);
                message.Append(e.Collection[key]);
                if (string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    relativeUri = e.Collection[key];
                }
                else if (key.StartsWith("wp:Text1", StringComparison.CurrentCultureIgnoreCase))
                {
                    title = e.Collection[key];
                }
                else if (key.StartsWith("wp:Text2", StringComparison.CurrentCultureIgnoreCase))
                {
                    content = e.Collection[key];
                }
            }

            // Display a dialog of all the fields in the toast.
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(title + " " + content, ConfigManager.PushNotification, MessageBoxButton.OK);
                });
        }

        void httpchanel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {
            using (var Reader = new StreamReader(e.Notification.Body))
            {
                string msg = Reader.ReadToEnd();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(msg, "推送测试标题", MessageBoxButton.OK);
                });
            }
        }

        void httpchanel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(e.Message, "推送测试标题", MessageBoxButton.OK);
            });
        }

        void httpchanel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            RegisterEwpPushHandle();

            //Deployment.Current.Dispatcher.BeginInvoke(() =>
            //    {
            //        MessageBox.Show(msg, "推送启动提示", MessageBoxButton.OK);
            //    });
        }

        void request_OnFailed(string error, WebExceptionStatus status)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   MessageBox.Show(error, "推送启动[失败]提示", MessageBoxButton.OK);
               });
        }

        void request_OnSuccess(string result, byte[] temp, int responseCode, WebHeaderCollection reponseHeaders)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   MessageBox.Show(result, "推送启动[成功]提示", MessageBoxButton.OK);
               });
        }
    }
}
