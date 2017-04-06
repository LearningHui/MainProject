using ControlLib;
using MainProject.View;
using RYTong.LuaScript;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace RYTong.MainProject.Controller
{
    public class ContentFrame : ContentControl
    {
        public ContentFrame(bool isRootLuaManager)
        {
            if (isRootLuaManager == true && LuaManager.RootLuaManager == null)
                LuaManager = LuaManager.GetRootLuaManager();
            else if (isRootLuaManager == false)
                LuaManager = new LuaManager();
            Task<string> rytLua = Task.Run<string>(() =>
            {
                return DataBaseLib.DataBaseManager.ReadAppPackageTextAsync("RYTL.lua");
            });
            Task<string> sltContent = Task.Run<string>(() =>
            {
                return DataBaseLib.DataBaseManager.ReadAppPackageTextAsync("SLT2content.lua");
            });
            LuaManager.loadLuaString(rytLua.Result);
            LuaManager.loadLuaString(sltContent.Result);
        }
        public Page CurrentPage { get; set; }
        public LuaManager LuaManager {get;set;}
        public void Navigate(string content)
        {
            string html = content;
            TemplatePage templatePage = null;
#if DEBUG
            //html = RYTMainHelper.FormatXML(html);
#endif
            html = html.Trim();
            if (html.Contains("<error"))
            {
                //string errorMessage = AtomParser.ParserErrorText(html);
                //LogLib.RYTLog.Log(errorMessage);
            }
            if (templatePage == null)
                templatePage = new TemplatePage();
            try
            {
                html = LuaManager.doSLTParaser(html);
                //if (html.StartsWith("Error"))
                //    LogLib.RYTLog.Log("SLT2 error");
                templatePage.LuaManager = LuaManager;
                templatePage.Frame = this;
                templatePage.HTML_ = html;
                LuaManager.DetailV_ = templatePage;
                List<string> scriptList = new List<string>();
                FrameworkElement fe = null;
                XElement rootXhtml = XElement.Parse(html);
                templatePage.RootXML_ = rootXhtml;
                List<RYTControl> controlList = AtomParser.ParseHTML_New(rootXhtml, scriptList, templatePage, out fe);
                templatePage.InitControls(fe, controlList);
                templatePage.RunLuaScript(scriptList);
                templatePage.StopLoading();
                if (CurrentPage != null)
                {
                    CurrentPage.Visibility = Visibility.Collapsed;
                    (CurrentPage as TemplatePage).ClearPage();
                }
                CurrentPage = templatePage;
                this.Content = templatePage;
            }
            catch (Exception e)
            {
                e.Source = html;
                //RYTMainHelper.ExceptionHandle(e);
            }
        }
    }
}
