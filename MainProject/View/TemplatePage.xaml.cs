using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using RYTong.LuaScript;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RYTong.MainProject.Controller;
using ControlLib;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace MainProject.View
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class TemplatePage : Page
    {
        public TemplatePage()
        {
            this.InitializeComponent();
        }
        public List<RYTControl> allControls_ = null;
        public string HTML_ { get; internal set; }
        public LuaManager LuaManager { get; internal set; }
        public XElement RootXML_ { get; internal set; }
        public ContentFrame Frame { get; set; }
        internal void InitControls(FrameworkElement fe, List<RYTControl> controlList)
        {
            //this.TemplateRootGrid.Children.
            if (fe != null)
            {
                allControls_ = controlList;
                //bControlsInit_ = true;

                if (!TemplateRootGrid.Children.Contains(fe))
                {
                    TemplateRootGrid.Children.Insert(0, fe);
                }
            }
        }

        internal void RunLuaScript(List<string> scriptList)
        {
         
        }

        internal void StopLoading()
        {
        
        }

        internal void ClearPage()
        {
          
        }
    }
}
