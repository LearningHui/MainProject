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
using System.Collections.Generic;
using RYTong.MainProject.View;

namespace RYTong.MainProject.Controller
{
    public static class AppData
    {
        public static RYTPageTransationBase PageTransation_ { get; set; }

        public static List<RYTCSSStyle> LastBuildCSSStyles { get; set; }

        public static HostPage HostPage { get; set; }

        public static byte[] Key { get; set; }
        public static byte[] IV { get; set; }

        #region DB Key

        public const string DB_KEY_IsFirst = "is_first";

        #endregion

        public static bool IsFirstConnection;
        public static bool IsTlsConnection;

        static AppData()
        {
            IsFirstConnection = true;
        }
    }
}
