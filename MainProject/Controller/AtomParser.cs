//
//  AtomParser
//
//  Created by wu.dong on 11/4/11.
//  Copyright 2011 RYTong. All rights reserved.
//

using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text;
using ControlLib;
using MainProject.Controller;
using MainProject.View;
using Windows.UI.Xaml;

namespace RYTong.MainProject.Controller
{
    public class AtomParser
    {

#if DEBUG
        public static TimeSpan dtControl;
        public static TimeSpan dtLayout;
#endif
        //public static void PhoneIdentityParser(String content, List<Microsoft.ApplicationInsights.Channel> menus, Dictionary<String, String> identityAttrs)
        //{
        //    XElement element = XElement.Parse(content);
        //    String rootName = element.Name.LocalName;
        //    if (rootName.Equals("phone_identity"))
        //    {
        //        //解析phone_identity属性。
        //        IEnumerable<XAttribute> attributes = element.Attributes();
        //        foreach (XAttribute attr in attributes)
        //        {
        //            identityAttrs[attr.Name.LocalName] = attr.Value;
        //        }
        //        //解析menu属性。
        //        element = element.Element("menu");
        //        element = element.Element("channel");
        //        while (element != null)
        //        {
        //            Channel ch = new Channel();
        //            //解析属性。
        //            IEnumerable<XAttribute> attrs = element.Attributes();
        //            foreach (XAttribute attr in attrs)
        //            {
        //                ch.propertys_[attr.Name.LocalName] = attr.Value;
        //            }
        //            //解析子节点。
        //            IEnumerable<XElement> subElements = element.Elements();
        //            foreach (XElement xe in subElements)
        //            {
        //                ch.propertys_[xe.Name.LocalName] = xe.Value;
        //            }
        //            //存储到menus
        //            menus.Add(ch);

        //            element = element.NextNode as XElement;
        //        }
        //    }
        //}

        /// <summary>
        /// Get XML Content in <![CDATA[ ... ]]>
        /// </summary>
        public static XElement GetXMLContent(string content)
        {
            XElement result = new XElement("content");
            XElement root = XElement.Parse(content);
            XElement entryNode = root.Elements().SingleOrDefault(r => r.Name.LocalName.Equals("entry"));
            if (entryNode == null)
                return result;
            XElement contentNode = entryNode.Elements().SingleOrDefault(c => c.Name.LocalName.Equals("content"));
            if (contentNode == null)
                return result;
            result.Add(contentNode.Value);
            string temp = result.ToString().Replace("&lt;", "<").Replace("&gt;", ">");
            result = XElement.Parse(temp);
            return result;
        }

        static List<Dictionary<string, Dictionary<string, string>>> classXmlObjects;

        /// <summary>
        /// Parser Html for new structure whit has Body Node 
        /// </summary>
        /*public static List<RYTControl> ParseHTML_New(XElement rootXhtml, List<string> luaScriptList, out FrameworkElement viewControl)
        {
            viewControl = null;

            if (classXmlObjects == null)
            {
                RYTClassParser parser = new RYTClassParser();
                string xmlContent = DataBaseLib.DataBaseManager.ReadAppPackageFile("class.xml", "text") as string;
                classXmlObjects = parser.parserClassScriptForResource(xmlContent);
            }

            RYTCreateControls cc = new RYTCreateControls();

#if DEBUG
            DateTime dtStart = DateTime.Now;
#endif

            List<RYTControl> topControls = cc.CreateAllObjects(rootXhtml, classXmlObjects, luaScriptList);
#if DEBUG
            dtControl = DateTime.Now - dtStart;
#endif
            if (topControls.Count == 1 && topControls[0] is RYTBodyControl)
            {
                topControls[0].LayoutSubviews();

                viewControl = topControls[0].View_;
            }
#if DEBUG
            dtLayout = DateTime.Now - dtStart - dtControl;
#endif
            return topControls;
        }*/

        public static List<RYTControl> ParseHTML_New(XElement rootXhtml, List<string> luaScriptList,TemplatePage page, out FrameworkElement viewControl)
        {
            viewControl = null;
            List<RYTControl> topControls = InitializeControl.CreateAllObjects(rootXhtml, luaScriptList, page);
            if (topControls.Count == 1 && topControls[0] is RYTBodyControl)
            {
                topControls[0].LayoutSubviews();
                viewControl = topControls[0].View as FrameworkElement;
            }

            //if (topControls.Count == 1)
            //{
            //    //topControls[0].LayoutSubviews();
            //    viewControl = topControls[0].View as FrameworkElement;
            //}
            return topControls;
        }
        /*public static List<RYTControl> ParseHTML(string content, Panel panel, List<string> luaScriptList)
        {
            RYTClassParser parser = new RYTClassParser();
            string xmlContent = DataBaseLib.DataBaseManager.ReadAppPackageFile("class.xml", "text") as string;
            var objects = parser.parserClassScriptForResource(xmlContent);
            XElement rootXhtml = XElement.Parse(content);
            RYTCreateControls cc = new RYTCreateControls();
            List<RYTControl> topControls = cc.CreateAllObjects(rootXhtml, objects, luaScriptList);
            List<RYTCSSStyle> allCSSStyles = cc.allCSSStyles_;

            // layout
            RYTViewLayout layout = new RYTViewLayout();
            layout.xPadding = 0;
            layout.yPadding = Constant.CONTROL_Y_PADDING;
            layout.horSpacing = Constant.CONTROL_HORIZONTAL_SPACING;
            layout.verSpacing = Constant.CONTROL_VERTICAL_SPACING;
            layout.layoutRootControls(topControls, panel);

            foreach (RYTControl c in topControls)
            {
                if (c.View_ != null && c.View_.Visibility == System.Windows.Visibility.Visible && c.View_.Opacity != 0)
                {
                    Canvas.SetLeft(c.View_, c.Frame_.X);
                    Canvas.SetTop(c.View_, c.Frame_.Y);
                    panel.Children.Add(c.View_);

                    panel.Height += c.View_.Height;
                }
            }

            return topControls;
        }*/

        //public static RYTControl ParseXElement(XElement tagXml, List<RYTCSSStyle> allCssStyles, List<string> luaScriptList,TemplatePage page)
        //{           
        //    List<RYTControl> controls = InitializeControl.CreateAllObjects(tagXml, allCssStyles, luaScriptList, page);
        //    RYTControl topControl = controls.FirstOrDefault();
        //    if (topControl != null && topControl.ChildrenElements_.Count > 0)
        //        topControl.LayoutSubviews();
        //    return topControl;
        //}

        /// <summary>
        /// Replace & to &amp; in xml
        /// </summary>
        public static string SanitizeXml(string sourceXML)
        {
            if (string.IsNullOrEmpty(sourceXML))
            {
                return sourceXML;
            }

            if (sourceXML.IndexOf('&') < 0)
            {
                return sourceXML;
            }

            StringBuilder result = new StringBuilder(sourceXML);
            result = result.Replace("&lt;", "<>lt;")
                           .Replace("&gt;", "<>gt;")
                           .Replace("&amp;", "<>amp;")
                           .Replace("&apos;", "<>apos;")
                           .Replace("&quot;", "<>quot;");

            result = result.Replace("&", "&amp;");

            result = result.Replace("<>lt;", "&lt;")
                           .Replace("<>gt;", "&gt;")
                           .Replace("<>amp;", "&amp;")
                           .Replace("<>apos;", "&apos;")
                           .Replace("<>quot;", "&quot;");

            return result.ToString();
        }

        public static string ReplaceSpecialChars(string oldXML)
        {
            if (string.IsNullOrEmpty(oldXML))
            {
                return string.Empty;
            }

            string newXML = oldXML.Replace("&", "[AT_Placeholder]");
            return newXML;
        }

        public static string ReplacePlaceHolder(string newXML)
        {
            if (string.IsNullOrEmpty(newXML))
            {
                return string.Empty;
            }

            string oldXML = newXML.Replace("[AT_Placeholder]", "&");
            return oldXML;
        }

        public static string ParserErrorText(string html)
        {
            string errorMessage = string.Empty;

            try
            {
                XElement root = XElement.Parse(html);
                var query = root.DescendantsAndSelf("error");
                if (query != null)
                {
                    var firstItem = query.FirstOrDefault();
                    if (firstItem != null)
                    {
                        if (firstItem.Attribute("string") != null)
                        {
                            errorMessage = firstItem.Attribute("string").Value.Trim();
                        }
                    }
                }
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = html;
            }

            return errorMessage;
        }
    }
}
