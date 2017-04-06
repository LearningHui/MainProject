//
//  RYTCreateControls
//  RYTong
//
//  Created by zou.penghui on 16/1/13.
//  Copyright 2015 RYTong. All rights reserved.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using RYTong.ControlLib;
using RYTong.DataBaseLib;
using RYTong.ChartControlLib;
using RYTong.MainProject.View;
using RYTong.ClassScriptParser;

namespace RYTong.MainProject.Controller
{
    /*public class RYTCreateControls
    {
        public List<RYTCSSStyle> allCSSStyles_;
        public TemplatePage TemplatePage { get; set; }
        public List<RYTCSSStyle> HostTemplatePageStyles { get; set; }//for showContent,when poppage has no css,search this cssStyle
        List<RYTControl> topControls_;

        // For controls created by name
        List<string> controlNamesList = new List<string>() { "orgCity", "departDate" };

        public RYTCreateControls()
        {
            allCSSStyles_ = new List<RYTCSSStyle>();
            AppData.LastBuildCSSStyles = allCSSStyles_;
            #region when invoke showContent,try to get host page styles
            object page = null;
            if (LuaScript.LuaManager.RootLuaManager != null)
            {
                page = LuaScript.LuaManager.RootLuaManager.DetailV_;
            }
            if (page != null)
            {
                TemplatePage templatePage = page as TemplatePage;
                if (templatePage != null && templatePage.PopPage != null)
                {
                    this.HostTemplatePageStyles = templatePage.allControls_[0].CssStyles_;
                }
            }
            #endregion
        }

        public RYTCreateControls(List<RYTCSSStyle> styles)
        {
            allCSSStyles_ = styles;
        }

        public List<RYTControl> CreateAllObjects(XElement rootXMLElement, List<Dictionary<String, Dictionary<String, String>>> objects, List<string> luaScriptList)
        {
            topControls_ = new List<RYTControl>();
            XElement _element = null;

            #region Find "Content" Root Node

            if (!rootXMLElement.Name.LocalName.Equals("content"))
            {
                _element = rootXMLElement.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("content"));
            }
            else
            {
                _element = rootXMLElement;
            }

            if (_element == null)
            {
                _element = new XElement("content");
                _element.Add(rootXMLElement);
            }

            #endregion

            #region Loop Nodes to Init Style, scrpit & control

            foreach (var xmlNode in _element.Elements())
            {
                if (xmlNode.Name.LocalName.Equals("head", StringComparison.CurrentCultureIgnoreCase))
                {                    
                    foreach (var item in xmlNode.Elements("link"))
                    {
                        if (item != null)
                        {
                            var refAttr = item.Attribute("ref");
                            if (refAttr != null)
                            {
                                #region GlobalCssStyle
                                //查找全局样式列表                                
                                string filename = System.IO.Path.GetFileName(refAttr.Value);
                                var queryGlobalStyle = RYTCSSStyle.GlobalCssStyles.FirstOrDefault(c => c.Keys.FirstOrDefault().EndsWith(filename));                                                              
                                var globalAttr = item.Attribute("global");
                                if (globalAttr != null)
                                {
                                    if (globalAttr.Value.Equals("true"))
                                    {
                                        if (queryGlobalStyle != null && queryGlobalStyle.Keys.FirstOrDefault().Equals(refAttr.Value))//如果有同路径的样式列表，提高优先级
                                        {
                                            RYTCSSStyle.GlobalCssStyles.Remove(queryGlobalStyle);
                                            RYTCSSStyle.GlobalCssStyles.Add(queryGlobalStyle);
                                        }
                                        else
                                        {
                                            if(queryGlobalStyle != null)//移除同名但不同路径的样式列表
                                            {
                                                RYTCSSStyle.GlobalCssStyles.Remove(queryGlobalStyle);
                                            }
                                            //添加全局样式列表
                                            var globalCssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;                                            
                                            List<RYTCSSStyle> globalStyles = null;
                                            if (!string.IsNullOrEmpty(globalCssContent))
                                            {
                                                globalStyles = new List<RYTCSSStyle>();
                                                RYTControl.initAllStyleWithSource(globalCssContent, globalStyles);
                                                Dictionary<String, List<RYTCSSStyle>> globalCssStyle = new Dictionary<string, List<RYTCSSStyle>>();
                                                globalCssStyle.Add(refAttr.Value, globalStyles);
                                                RYTCSSStyle.GlobalCssStyles.Add(globalCssStyle);                                                
                                            }
                                            else
                                            {
                                                var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                                RYTMainHelper.ExceptionHandle(null, message);
                                            }
                                        }
                                        continue;
                                    }
                                }                                
                                #endregion
                                
                                var cssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                                if (!string.IsNullOrEmpty(cssContent))
                                {
                                    RYTControl.initAllStyleWithSource(cssContent, allCSSStyles_);
                                }
                                else
                                {
                                    var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                    RYTMainHelper.ExceptionHandle(null, message);
                                }
                            }
                        }
                    }

                    var styleNode = xmlNode.Elements("style").FirstOrDefault(c => c.Name.LocalName.Equals("style", StringComparison.CurrentCultureIgnoreCase));
                    if (styleNode != null)
                    {
                        RYTControl.initAllStyleWithSource(styleNode.Value, allCSSStyles_);
                    }

                    // Load Lua Script to luaScriptList
                    if (luaScriptList != null)
                    {
                        var scriptNodes = xmlNode.Elements("script");
                        foreach (var s in scriptNodes)
                        {
                            if (s.Attribute("src") != null)
                            {
                                string srcStr = s.Attribute("src").Value.Trim();
                                //当以“/”开头需要加载远程脚本文件，否则在客户端本地加载该脚本文件
                                if (srcStr.StartsWith("/"))
                                {
                                    luaScriptList.Add(srcStr);
                                }
                                else
                                {
                                    var path = s.Attribute("src").Value;
                                    if (path.Equals("RYTL.lua"))
                                    {
                                        continue;
                                    }
                                    string localscript = DataBaseManager.ReadFileByType(path, "text") as string;
                                    if (!string.IsNullOrEmpty(localscript))
                                    {
                                        luaScriptList.Add(localscript.Trim());
                                    }
                                    else
                                    {
                                        var message = LogLib.RYTLog.Const.CombineLostFileMessage(path);
                                        RYTMainHelper.ExceptionHandle(null, message);
                                    }
                                }
                            }
                            else
                            {
                                luaScriptList.Add(s.Value.Trim());
                            }
                        }
                    }
                }

                #region head标签外面有link标签时
                else if (xmlNode.Name.LocalName.Equals("link", StringComparison.CurrentCultureIgnoreCase))
                {
                    var refAttr = xmlNode.Attribute("ref");
                    if (refAttr != null)
                    {
                        #region GlobalCssStyle
                        //查找全局样式列表                                
                        string filename = System.IO.Path.GetFileName(refAttr.Value);
                        var queryGlobalStyle = RYTCSSStyle.GlobalCssStyles.FirstOrDefault(c => c.Keys.FirstOrDefault().EndsWith(filename));
                        var globalAttr = xmlNode.Attribute("global");
                        if (globalAttr != null)
                        {
                            if (globalAttr.Value.Equals("true"))
                            {
                                if (queryGlobalStyle != null && queryGlobalStyle.Keys.FirstOrDefault().Equals(refAttr.Value))//如果有同路径的样式列表，提高优先级
                                {
                                    RYTCSSStyle.GlobalCssStyles.Remove(queryGlobalStyle);
                                    RYTCSSStyle.GlobalCssStyles.Add(queryGlobalStyle);
                                }
                                else
                                {
                                    if (queryGlobalStyle != null)//移除同名但不同路径的样式列表
                                    {
                                        RYTCSSStyle.GlobalCssStyles.Remove(queryGlobalStyle);
                                    }
                                    //添加全局样式列表
                                    var globalCssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                                    List<RYTCSSStyle> globalStyles = null;
                                    if (!string.IsNullOrEmpty(globalCssContent))
                                    {
                                        globalStyles = new List<RYTCSSStyle>();
                                        RYTControl.initAllStyleWithSource(globalCssContent, globalStyles);
                                        Dictionary<String, List<RYTCSSStyle>> globalCssStyle = new Dictionary<string, List<RYTCSSStyle>>();
                                        globalCssStyle.Add(refAttr.Value, globalStyles);
                                        RYTCSSStyle.GlobalCssStyles.Add(globalCssStyle);
                                    }
                                    else
                                    {
                                        var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                        RYTMainHelper.ExceptionHandle(null, message);
                                    }
                                }                                
                            }
                            else
                            {
                                var cssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                                if (!string.IsNullOrEmpty(cssContent))
                                {
                                    RYTControl.initAllStyleWithSource(cssContent, allCSSStyles_);
                                }
                                else
                                {
                                    var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                    RYTMainHelper.ExceptionHandle(null, message);
                                } 
                            }
                        }

                        #endregion
                        else
                        {
                            var cssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                            if (!string.IsNullOrEmpty(cssContent))
                            {
                                RYTControl.initAllStyleWithSource(cssContent, allCSSStyles_);
                            }
                            else
                            {
                                var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                RYTMainHelper.ExceptionHandle(null, message);
                            }    
                        }                                                                  
                    }
                }
                #endregion

                else if (xmlNode.Name.LocalName.Equals("style", StringComparison.CurrentCultureIgnoreCase))
                {
                    RYTControl.initAllStyleWithSource(xmlNode.Value, allCSSStyles_);
                }

                #region Create Control
                else
                {
                    RYTControl control = this.CreateObject(xmlNode, objects, null); //创建本身。

                    if (control != null)
                    {
                        if (control is RYTSwitchControl && topControls_.Count >= 1 && topControls_[topControls_.Count - 1] is RYTSwitchControl)
                        {
                            RYTSwitchControl switchControl = (RYTSwitchControl)topControls_[topControls_.Count - 1];
                            switchControl.AddSwitchControl((RYTSwitchControl)control);
                        }
                        else if (control is RYTPieChartControl && topControls_.Count >= 1 && topControls_[topControls_.Count - 1] is RYTPieChartControl)
                        {
                            RYTPieChartControl piechartControl = (RYTPieChartControl)topControls_[topControls_.Count - 1];
                            piechartControl.AddPieChartControl(control as RYTPieChartControl);

                            var parentControl = piechartControl.Parent_;
                            if (parentControl != null)
                            {
                                parentControl.ChildrenElements_.Remove(control);
                            }
                        }
                        else
                        {
                            topControls_.Add(control);
                        }
                    }
                    this.CreateSubObjects(xmlNode, objects, control);//递归创建子节点。
                }
                #endregion
            }

            #endregion

            return topControls_;
        }

        public void CreateSubObjects(XElement element, List<Dictionary<String, Dictionary<String, String>>> objects, RYTControl parent)
        {
            IEnumerable<XElement> subElements = element.Elements();
            RYTControl lastControl = null;
            foreach (XElement xe in subElements)
            {
                //get next XElement
                XNode nextNode = xe.NodesAfterSelf().FirstOrDefault(x => x.GetType() == typeof(XElement));
                           
                RYTControl control = this.CreateObject(xe, objects, parent);//创建本身。
                bool bAddSubControl = false;
               
                if (control != null)
                {
                    // 对Segement, switch控件做特殊处理：
                    // 如果上一个控件也是Segement, switch的话，把这个控件和上一个Segement, switch控件整合在一起
                    try
                    {
                        if (control is RYTSegmentControl && topControls_.Count >= 1 &&
                            lastControl != null && lastControl is RYTSegmentControl && lastControl.Name_ == control.Name_)
                        {
                            RYTSegmentControl segmentControl = (RYTSegmentControl)lastControl;
                            //segmentControl.SegmentButtonList.Add((RYTSegmentControl)control);
                            segmentControl.AddSegmentControlToHost((RYTSegmentControl)control);
                            segmentControl.Parent_.ChildrenElements_.Remove(control);
                            bAddSubControl = true;

                            if (nextNode != null && (nextNode as XElement) != null)
                            {
                                XElement nextElement = nextNode as XElement;
                                string elementName_Next = nextElement.Name.LocalName;
                                string typeAttibute_Next = string.Empty;
                                string nameAttribute_Next = string.Empty;
                                if (nextElement.Attribute("type") != null)
                                {
                                    typeAttibute_Next = nextElement.Attribute("type").Value;
                                }
                                if (nextElement.Attribute("name") != null)
                                {
                                    nameAttribute_Next = nextElement.Attribute("name").Value;
                                }
                                if (elementName_Next != "input" || typeAttibute_Next != "segment" || nameAttribute_Next != control.Name_)
                                {
                                    segmentControl.CreatSegment();                                   
                                }
                            }
                            else
                            {
                                segmentControl.CreatSegment();                                
                            }

                        }
                        else if (control is RYTSegmentControl)
                        {
                            (control as RYTSegmentControl).AddSegmentControlToHost((RYTSegmentControl)control);

                            //(control as RYTSegmentControl).SegmentButtonList.Add((RYTSegmentControl)control);
                        }
                        else if (control is RYTSwitchControl && topControls_.Count >= 1 &&
                                 lastControl != null && lastControl is RYTSwitchControl && lastControl.Name_ == control.Name_)
                        {
                            RYTSwitchControl switchControl = (RYTSwitchControl)lastControl;
                            switchControl.AddSwitchControl(control as RYTSwitchControl);
                            var parentControl = switchControl.Parent_;
                            if (parentControl != null)
                            {
                                parentControl.ChildrenElements_.Remove(control);
                                bAddSubControl = true;
                            }
                        }
                        else if (control is RYTPieChartControl && topControls_.Count >= 1 &&
                                 lastControl != null && lastControl is RYTPieChartControl)
                        {
                            RYTPieChartControl piechartControl = (RYTPieChartControl)lastControl;
                            piechartControl.AddPieChartControl(control as RYTPieChartControl);
                            var parentControl = piechartControl.Parent_;
                            if (parentControl != null)
                            {
                                parentControl.ChildrenElements_.Remove(control);
                                bAddSubControl = true;
                            }
                        }
                        else if (topControls_.Count >= 1 && lastControl != null)
                        {
                            if (control.ToString().Contains("RYTPieChartControl_New") &&
                                lastControl.ToString().Contains("RYTPieChartControl_New"))
                            {
                                RYTControl piechartControl = lastControl;
                                MethodInfo mi = piechartControl.GetType().GetMethod("AddPieChartControl");
                                if (mi != null)
                                {
                                    mi.Invoke(piechartControl, new object[] { control });
                                }
                                var parentControl = piechartControl.Parent_;
                                if (parentControl != null)
                                {
                                    parentControl.ChildrenElements_.Remove(control);
                                    bAddSubControl = true;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        
                        throw;
                    }
                }

                if (!bAddSubControl)
                {
                    lastControl = control;
                }

                this.CreateSubObjects(xe, objects, control);//递归创建子节点。
            }
        }

        private T FindChildControl<T>(RYTControl parent) where T : RYTControl
        {
            T findChild = null;
            if (parent.ChildrenElements_.Count == 0)
            {
                return null;
            }

            foreach (var child in parent.ChildrenElements_)
            {
                if (child is T)
                {
                    findChild = child as T;
                    break;
                }
                else
                {
                    findChild = FindChildControl<T>(child);
                }
            }

            return findChild;
        }

        public RYTControl CreateObject(XElement element, List<Dictionary<String, Dictionary<String, String>>> definedClassesDictList, RYTControl parent)
        {
            RYTControl control = null;
            var _elementName = element.Name.LocalName;
            var _elementType = string.Empty;
            XAttribute tempAttribute = element.Attribute("type");
            if (tempAttribute != null)
            {
                _elementType = tempAttribute.Value;
            }
            var _elementStyle = string.Empty;
            tempAttribute = element.Attribute("style");
            if (tempAttribute != null)
            {
                _elementStyle = tempAttribute.Value;
            }

            #region Get temp Attribute

            tempAttribute = element.Attribute("name");
            string nameValue = string.Empty;
            if (tempAttribute != null)
            {
                nameValue = tempAttribute.Value;
            }

            tempAttribute = element.Attribute("segment");
            string segmentValue = string.Empty;
            if (tempAttribute != null)
            {
                segmentValue = tempAttribute.Value;
            }

            tempAttribute = element.Attribute("switch");
            string switchValue = string.Empty;
            if (tempAttribute != null)
            {
                switchValue = tempAttribute.Value;
            }

            #endregion

            #region Find Matched Class & Create Control

            var dictQueryByTag = definedClassesDictList.Where(c => c["attribute"]["tag"].Equals(_elementName));

            if (dictQueryByTag.Count() > 0)
            {
                #region For Segment & Switch Control

                if (!string.IsNullOrEmpty(segmentValue))
                {
                    //if (string.IsNullOrEmpty(switchValue))
                    //{
                    //    continue;
                    //}

                    var segmentQuery = dictQueryByTag.FirstOrDefault(c => c["attribute"].ContainsKey("segment") && c["attribute"]["segment"].Equals(segmentValue, StringComparison.CurrentCultureIgnoreCase));
                    if (segmentQuery != null)
                    {
                        control = this.CreateObject(element, segmentQuery["attribute"]["class"], segmentQuery["attribute"], segmentQuery["param"], parent);
                        return control;
                    }
                }

                if (!string.IsNullOrEmpty(switchValue))
                {
                    var switchQuery = dictQueryByTag.FirstOrDefault(c => c["attribute"].ContainsKey("switch") && c["attribute"]["switch"].Equals(switchValue, StringComparison.CurrentCultureIgnoreCase));
                    if (switchQuery != null)
                    {
                        control = this.CreateObject(element, switchQuery["attribute"]["class"], switchQuery["attribute"], switchQuery["param"], parent);
                        return control;
                    }
                }

                #endregion

                // Create object by tag & const name in controlNamesList
                if (!string.IsNullOrEmpty(nameValue) && controlNamesList.Contains(nameValue))
                {
                    var query = dictQueryByTag.FirstOrDefault(c => c["attribute"].ContainsKey("name") && c["attribute"]["name"].Equals(nameValue, StringComparison.CurrentCultureIgnoreCase));
                    if (query != null)
                    {
                        control = this.CreateObject(element, query["attribute"]["class"], query["attribute"], query["param"], parent);
                        return control;
                    }
                }

                // Create object by tag & type 
                if (!string.IsNullOrEmpty(_elementType))
                {
                    var query = dictQueryByTag.Where(c => c["attribute"].ContainsKey("type") && c["attribute"]["type"].Equals(_elementType, StringComparison.CurrentCultureIgnoreCase));
                    if (query != null && query.Count() > 0)
                    {
                        if (!string.IsNullOrEmpty(_elementStyle))
                        {
                            var styleQuery = query.FirstOrDefault(c => c["attribute"].ContainsKey("style") && c["attribute"]["style"].Equals(_elementStyle, StringComparison.CurrentCultureIgnoreCase));
                            if (styleQuery != null)
                            {
                                control = this.CreateObject(element, styleQuery["attribute"]["class"], styleQuery["attribute"], styleQuery["param"], parent);
                                return control;
                            }
                        }

                        // Default:
                        var firstQuery = query.FirstOrDefault();
                        control = this.CreateObject(element, firstQuery["attribute"]["class"], firstQuery["attribute"], firstQuery["param"], parent);
                        return control;
                    }
                    //else 
                    //{
                    //    //MessageBox.Show("(Element Parser Not Defined By Type) : \n" + element);
                    //}
                }

                // Create object only by tag 
                else if (dictQueryByTag.Count() > 0)
                {
                    var theOnlyOneDict = dictQueryByTag.First();
                    control = this.CreateObject(element, theOnlyOneDict["attribute"]["class"], theOnlyOneDict["attribute"], theOnlyOneDict["param"], parent);
                    return control;
                }
                else // (Not defined in current project)
                {
                    LogLib.RYTLog.ShowMessage("(Parser Failed)Unknown Element : " + _elementName);
                }
            }
            else  // (Not defined in current project)
            {
                LogLib.RYTLog.ShowMessage("(Parser Failed)Unknown Element : " + _elementName);
            }

            #endregion

            return control;
        }

        public RYTControl CreateObject(XElement element, String className, Dictionary<string, string> objectNodeDict, Dictionary<String, String> param_s, RYTControl parent)
        {
            if (className == null || element == null)
            {
                return null;
            }

            #region Create control Instance with Reflection
            string method = objectNodeDict["init"];

            Assembly assembly = Assembly.Load("RYTong.ControlLib");
            Type _class = assembly.GetType("RYTong.ControlLib." + className);            
            if (_class == null)
            {
                assembly = Assembly.Load("RYTong.ExtendControlLib");
                _class = assembly.GetType("RYTong.ExtendControlLib." + className);                
            }
            if (_class == null)
            {
                assembly = Assembly.Load("RYTong.ChartControlLib");
                _class = assembly.GetType("RYTong.ChartControlLib." + className);    
                if (_class == null)
                {
                    assembly = Assembly.Load("RYTong.ExtendJSLib");
                    _class = assembly.GetType("RYTong.ExtendJSLib." + className);
                }

                if (_class == null)
                {
                    assembly = Assembly.Load("RYTong.CustomControlLib");
                    _class = assembly.GetType("RYTong.CustomControlLib" + className);
                }
            }

            if (_class == null)
            {
                RYTong.LogLib.RYTLog.ShowMessage(string.Format("控件:[{0}]没有定义", className));
                return null;
            }

            Object _object = System.Activator.CreateInstance(_class);
            MethodInfo methods = _class.GetMethod(method);
            if (param_s != null && param_s.Count > 0)
            {
                Object[] pars = new Object[] { param_s };
                methods.Invoke(_object, pars);
            }
            else
            {
                methods.Invoke(_object, null);
            }
            #endregion

            if (_object is RYTControl)
            {
                RYTControl control = _object as RYTControl;

                #region Set Values to control object
                //加属性。
                IEnumerable<XAttribute> attributes = element.Attributes();
                Dictionary<String, String> attDic = new Dictionary<string, string>();
                foreach (XAttribute attr in attributes)
                {
                    attDic[attr.Name.LocalName] = attr.Value.Trim();
                }
                //将设置了回调方法的控件放入一个集合中，方便track模块快速从dom树中获取控件而不是遍历整个dom树
                if (attDic.ContainsKey("onclick") || attDic.ContainsKey("onfocus") || attDic.ContainsKey("onblur") || attDic.ContainsKey("onchange"))
                {
                    RYTong.MainProject.View.TemplatePage.TemplateInstanse.EventControsList.Add(control);
                }
                if (attDic.ContainsKey("id"))
                {
                    string value = string.Empty;
                    attDic.TryGetValue("id", out value);
                    if(!string.IsNullOrEmpty(value))
                        TemplatePage.TemplateInstanse.ControlsWithIdDic[value] = control;
                }
                control.SetAttributesDict(attDic); // Set Attributes Dict
                control.XElement = element;        // Set XML
                control.ElementName_ = element.Name.LocalName; // Set Element Name
                if (objectNodeDict.ContainsKey("type")) // Set XML Type Attribute
                {
                    control.ElementType_ = objectNodeDict["type"];
                }
                if (element.Value != null) // Set XML Inner Text(Value) to Title_
                {
                    //control.Title_ = AtomParser.ReplacePlaceHolder(element.Value.Trim());
                    control.Title_ = element.Value;
                }

                #endregion

                // Find matched style and set instance to control
                control.GetCSSStyle(allCSSStyles_, this.HostTemplatePageStyles, parent, TemplatePage.TemplateInstanse.ControlsWithSpecifiedCssDic);

                // Init View & Apply Style
                control.InitView();
                control.ApplyStyle();

                if (parent != null)
                {
                    parent.ChildrenElements_.Add(control);

                    // Store parent Object
                    control.Parent_ = parent;
                }

                #region Event Register

                control.LuaEvent += (s, e) =>
                {                    

                    #region Track Event Action 
                    if (control.TrackEventTriggerAction==TrackEventTrigger.None)
                    {
                        LuaScriptEventAction.OnLuaEventAction(s, e);
                        return;
                    }
                    if (control.TrackOnClickAction != null && control.TrackEventTriggerAction == TrackEventTrigger.Click)
                    {
                        control.TrackOnClickAction(control, control.Name_, control.GetDefaultOrInputOrSelectedValue());
                    }
                    //else if (control.TrackOnFocusAction != null && control.TrackEventTriggerAction == TrackEventTrigger.Focus)
                    //{
                    //    control.TrackOnFocusAction(control, control.Name_, control.GetDefaultOrInputOrSelectedValue());
                    //}
                    //else if (control.TrackOnBlurAction != null && control.TrackEventTriggerAction == TrackEventTrigger.Blur)
                    //{
                    //    control.TrackOnBlurAction(control, control.Name_, control.GetDefaultOrInputOrSelectedValue());
                    //}
                    //else if (control.TrackOnChangeAction != null && control.TrackEventTriggerAction == TrackEventTrigger.Change)
                    //{
                    //    control.TrackOnChangeAction(control, control.Name_, control.GetDefaultOrInputOrSelectedValue());
                    //} 

                    #endregion

                    LuaScriptEventAction.OnLuaEventAction(s, e);
                    
                };

                if (control is RYTButtonControl)
                {
                    (control as RYTButtonControl).Clicked += (sender, e) =>
                    {
                        ButtonEventAction.ClickEventAction(sender, e);
                    };
                    (control as RYTButtonControl).EWPButtonImageDownloadEvent += (sender, t, isLeft, v) =>
                    {
                        ImageEventAction.EWPButtonImageDownloadAction(sender, t, isLeft, v);
                    };
                }
                else if (control is RYTSegmentControl)
                {
                    (control as RYTSegmentControl).EWPSegmentTitleImageDownloadEvent += (sender, uri) =>
                    {
                        ImageEventAction.EWPSegmentTitleImageDownloadAction(sender, uri);
                    };
                }
                else if (control is RYTPieChartControl)
                {
                    (control as RYTPieChartControl).PieChartSelected += (sender, e) =>
                    {
                        PieChartEventAction.SelectedEventAction(sender, e);
                    };
                }
                else if (control is RYTTableControl)
                {
                    (control as RYTTableControl).TableScrollBottomEvent += (sender, e) =>
                    {
                        TableEventAction.ScrollToBottomHandler(sender, e);
                    };
                }
                else if (control is RYTAControl)
                {
                    (control as RYTAControl).Clicked += (sender, t, v) =>
                    {
                        AlinkEventAction.ClickEventHandler(sender, t, v);
                    };
                }
                else if (control is RYTImageControl)
                {
                    (control as RYTImageControl).EWPImageDownloadEvent += (sender, t, v) =>
                    {
                        ImageEventAction.EWPImageDownloadAction(sender, t, v);
                    };
                }

                #endregion

                #region Get key-value by control Name in IsolatedStorage And Set Value

                // Air China Project only support: RYTInputControl & RYTPasswordControl
                if (!string.IsNullOrEmpty(control.Name_))
                {
                    if (control is RYTInputControl)
                    {
                        string strValue = DataBaseManager.GetIsolatedObjectValue<string>(control.Name_);
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            strValue = RYTong.DataBaseLib.RYTSecurity.Instance.Decrypt(strValue);
                            (control as RYTInputControl).SetInputText(strValue);
                        }
                    }
                    else if (control is RYTPasswordControl)
                    {
                        string strValue = DataBaseManager.GetIsolatedObjectValue<string>(control.Name_);
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            strValue = RYTong.DataBaseLib.RYTSecurity.Instance.Decrypt(strValue);
                            (control as RYTPasswordControl).SetPasswordValue(strValue);
                        }
                    }
                }

                #endregion

                return control;
            }

            return null;
        }

    }
    */
    public class InitializeControl
    {
        private InitializeControl()
        {
        }
        public static List<Dictionary<string, Dictionary<string, string>>> ClassXmlObjects { get; set; }
        static InitializeControl()
        {
            if (ClassXmlObjects == null)
            {
                RYTClassParser parser = new RYTClassParser();
                string xmlContent = DataBaseLib.DataBaseManager.ReadAppPackageFile("class.xml", "text") as string;
                ClassXmlObjects = parser.parserClassScriptForResource(xmlContent);
            }
        }
        public static List<RYTControl> CreateAllObjects(XElement rootXMLElement, List<string> luaScriptList, TemplatePage page)
        {
            return CreateAllObjects(rootXMLElement, null, luaScriptList, page);
        }
        public static List<RYTControl> CreateAllObjects(XElement rootXMLElement, TemplatePage page)
        {
            return CreateAllObjects(rootXMLElement, null, null, page);
        }
        public static List<RYTControl> CreateAllObjects(XElement rootXMLElement, List<RYTCSSStyle> styles, TemplatePage page)
        {
            return CreateAllObjects(rootXMLElement, styles, null, page);
        }
        public static List<RYTControl> CreateAllObjects(XElement rootXMLElement, List<RYTCSSStyle> styles, List<string> luaScriptList, TemplatePage page)
        {
            List<RYTControl> controls = new List<RYTControl>();
            if (styles == null)
                styles = new List<RYTCSSStyle>();
            XElement _element = null;

            #region Find "Content" Root Node

            if (!rootXMLElement.Name.LocalName.Equals("content"))
            {
                _element = rootXMLElement.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("content"));
            }
            else
            {
                _element = rootXMLElement;
            }

            if (_element == null)
            {
                _element = new XElement("content");
                _element.Add(rootXMLElement);
            }

            #endregion

            #region Loop Nodes to Init Style, scrpit & control

            foreach (var xmlNode in _element.Elements())
            {
                if (xmlNode.Name.LocalName.Equals("head", StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (var item in xmlNode.Elements("link"))
                    {
                        if (item != null)
                        {
                            var refAttr = item.Attribute("ref");
                            if (refAttr != null)
                            {
                                #region GlobalCssStyle
                                //查找全局样式列表                                
                                string filename = System.IO.Path.GetFileName(refAttr.Value);
                                var queryGlobalStyle = RYTCSSStyle.GlobalCssStyles.FirstOrDefault(c => c.Keys.FirstOrDefault().EndsWith(filename));
                                var globalAttr = item.Attribute("global");
                                if (globalAttr != null)
                                {
                                    if (globalAttr.Value.Equals("true"))
                                    {
                                        if (queryGlobalStyle != null && queryGlobalStyle.Keys.FirstOrDefault().Equals(refAttr.Value))//如果有同路径的样式列表，提高优先级
                                        {
                                            RYTCSSStyle.GlobalCssStyles.Remove(queryGlobalStyle);
                                            RYTCSSStyle.GlobalCssStyles.Add(queryGlobalStyle);
                                        }
                                        else
                                        {
                                            if (queryGlobalStyle != null)//移除同名但不同路径的样式列表
                                            {
                                                RYTCSSStyle.GlobalCssStyles.Remove(queryGlobalStyle);
                                            }
                                            //添加全局样式列表
                                            var globalCssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                                            List<RYTCSSStyle> globalStyles = null;
                                            if (!string.IsNullOrEmpty(globalCssContent))
                                            {
                                                globalStyles = new List<RYTCSSStyle>();
                                                RYTControl.initAllStyleWithSource(globalCssContent, globalStyles);
                                                Dictionary<String, List<RYTCSSStyle>> globalCssStyle = new Dictionary<string, List<RYTCSSStyle>>();
                                                globalCssStyle.Add(refAttr.Value, globalStyles);
                                                RYTCSSStyle.GlobalCssStyles.Add(globalCssStyle);
                                            }
                                            else
                                            {
                                                var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                                RYTMainHelper.ExceptionHandle(null, message);
                                            }
                                        }
                                        continue;
                                    }
                                }
                                #endregion

                                var cssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                                if (!string.IsNullOrEmpty(cssContent))
                                {
                                    RYTControl.initAllStyleWithSource(cssContent, styles);
                                }
                                else
                                {
                                    var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                    RYTMainHelper.ExceptionHandle(null, message);
                                }
                            }
                        }
                    }
                    var styleNode = xmlNode.Elements("style").FirstOrDefault(c => c.Name.LocalName.Equals("style", StringComparison.CurrentCultureIgnoreCase));
                    if (styleNode != null)
                        RYTControl.initAllStyleWithSource(styleNode.Value, styles);

                    // Load Lua Script to luaScriptList
                    if (luaScriptList != null)
                    {
                        var scriptNodes = xmlNode.Elements("script");
                        foreach (var s in scriptNodes)
                        {
                            if (s.Attribute("src") != null)
                            {
                                string srcStr = s.Attribute("src").Value.Trim();
                                //当以“/”开头需要加载远程脚本文件，否则在客户端本地加载该脚本文件
                                if (srcStr.StartsWith("/"))
                                {
                                    luaScriptList.Add(srcStr);
                                }
                                else
                                {
                                    var path = s.Attribute("src").Value;
                                    if (path.Equals("RYTL.lua"))
                                    {
                                        continue;
                                    }
                                    string localscript = DataBaseManager.ReadFileByType(path, "text") as string;
                                    if (!string.IsNullOrEmpty(localscript))
                                    {
                                        luaScriptList.Add(localscript.Trim());
                                    }
                                    else
                                    {
                                        var message = LogLib.RYTLog.Const.CombineLostFileMessage(path);
                                        RYTMainHelper.ExceptionHandle(null, message);
                                    }
                                }
                            }
                            else
                            {
                                luaScriptList.Add(s.Value.Trim());
                            }
                        }
                    }
                }

                #region head标签外面有link标签时
                else if (xmlNode.Name.LocalName.Equals("link", StringComparison.CurrentCultureIgnoreCase))
                {
                    var refAttr = xmlNode.Attribute("ref");
                    if (refAttr != null)
                    {
                        #region GlobalCssStyle
                        //查找全局样式列表                                
                        string filename = System.IO.Path.GetFileName(refAttr.Value);
                        var queryGlobalStyle = RYTCSSStyle.GlobalCssStyles.FirstOrDefault(c => c.Keys.FirstOrDefault().EndsWith(filename));
                        var globalAttr = xmlNode.Attribute("global");
                        if (globalAttr != null)
                        {
                            if (globalAttr.Value.Equals("true"))
                            {
                                if (queryGlobalStyle != null && queryGlobalStyle.Keys.FirstOrDefault().Equals(refAttr.Value))//如果有同路径的样式列表，提高优先级
                                {
                                    RYTCSSStyle.GlobalCssStyles.Remove(queryGlobalStyle);
                                    RYTCSSStyle.GlobalCssStyles.Add(queryGlobalStyle);
                                }
                                else
                                {
                                    if (queryGlobalStyle != null)//移除同名但不同路径的样式列表
                                    {
                                        RYTCSSStyle.GlobalCssStyles.Remove(queryGlobalStyle);
                                    }
                                    //添加全局样式列表
                                    var globalCssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                                    List<RYTCSSStyle> globalStyles = null;
                                    if (!string.IsNullOrEmpty(globalCssContent))
                                    {
                                        globalStyles = new List<RYTCSSStyle>();
                                        RYTControl.initAllStyleWithSource(globalCssContent, globalStyles);
                                        Dictionary<String, List<RYTCSSStyle>> globalCssStyle = new Dictionary<string, List<RYTCSSStyle>>();
                                        globalCssStyle.Add(refAttr.Value, globalStyles);
                                        RYTCSSStyle.GlobalCssStyles.Add(globalCssStyle);
                                    }
                                    else
                                    {
                                        var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                        RYTMainHelper.ExceptionHandle(null, message);
                                    }
                                }
                            }
                            else
                            {
                                var cssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                                if (!string.IsNullOrEmpty(cssContent))
                                {
                                    RYTControl.initAllStyleWithSource(cssContent, styles);
                                }
                                else
                                {
                                    var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                    RYTMainHelper.ExceptionHandle(null, message);
                                }
                            }
                        }

                        #endregion
                        else
                        {
                            var cssContent = DataBaseManager.ReadFileByType(refAttr.Value, "text") as string;
                            if (!string.IsNullOrEmpty(cssContent))
                            {
                                RYTControl.initAllStyleWithSource(cssContent, styles);
                            }
                            else
                            {
                                var message = LogLib.RYTLog.Const.CombineLostFileMessage(refAttr.Value);
                                RYTMainHelper.ExceptionHandle(null, message);
                            }
                        }
                    }
                }
                #endregion

                else if (xmlNode.Name.LocalName.Equals("style", StringComparison.CurrentCultureIgnoreCase))
                    RYTControl.initAllStyleWithSource(xmlNode.Value, styles);
                else
                {
                    RYTControl control = CreateObject(xmlNode, styles, null, page); //创建本身。
                    if (control != null)
                    {
                        controls.Add(control);
                        CreateSubObjects(xmlNode, styles, control, page);//递归创建子节点。
                    }
                }
            }

            #endregion

            return controls;
        }

        static void CreateSubObjects(XElement element, List<RYTCSSStyle> styles, RYTControl parent, TemplatePage page)
        {
            IEnumerable<XElement> subElements = element.Elements();
            RYTControl lastControl = null;
            foreach (XElement xe in subElements)
            {
                //get next XElement
                XNode nextNode = xe.NodesAfterSelf().FirstOrDefault(x => x.GetType() == typeof(XElement));

                RYTControl control = CreateObject(xe, styles, parent, page);//创建本身。
                bool bAddSubControl = false;
                if (control != null)
                {
                    // 对Segement, switch控件做特殊处理：
                    // 如果上一个控件也是Segement, switch的话，把这个控件和上一个Segement, switch控件整合在一起
                    try
                    {
                        if (control is RYTSegmentControl &&
                            lastControl != null && lastControl is RYTSegmentControl && lastControl.Name_ == control.Name_)
                        {
                            RYTSegmentControl segmentControl = (RYTSegmentControl)lastControl;
                            //segmentControl.SegmentButtonList.Add((RYTSegmentControl)control);
                            segmentControl.AddSegmentControlToHost((RYTSegmentControl)control);
                            segmentControl.Parent_.ChildrenElements_.Remove(control);
                            bAddSubControl = true;

                            if (nextNode != null && (nextNode as XElement) != null)
                            {
                                XElement nextElement = nextNode as XElement;
                                string elementName_Next = nextElement.Name.LocalName;
                                string typeAttibute_Next = string.Empty;
                                string nameAttribute_Next = string.Empty;
                                if (nextElement.Attribute("type") != null)
                                {
                                    typeAttibute_Next = nextElement.Attribute("type").Value;
                                }
                                if (nextElement.Attribute("name") != null)
                                {
                                    nameAttribute_Next = nextElement.Attribute("name").Value;
                                }
                                if (elementName_Next != "input" || typeAttibute_Next != "segment" || nameAttribute_Next != control.Name_)
                                {
                                    segmentControl.CreatSegment();
                                }
                            }
                            else
                            {
                                segmentControl.CreatSegment();
                            }
                        }
                        else if (control is RYTSegmentControl)
                        {
                            (control as RYTSegmentControl).AddSegmentControlToHost((RYTSegmentControl)control);

                            //(control as RYTSegmentControl).SegmentButtonList.Add((RYTSegmentControl)control);
                        }
                        else if (control is RYTSwitchControl &&
                                 lastControl != null && lastControl is RYTSwitchControl && lastControl.Name_ == control.Name_)
                        {
                            RYTSwitchControl switchControl = (RYTSwitchControl)lastControl;
                            switchControl.AddSwitchControl(control as RYTSwitchControl);
                            var parentControl = switchControl.Parent_;
                            if (parentControl != null)
                            {
                                parentControl.ChildrenElements_.Remove(control);
                                bAddSubControl = true;
                            }
                        }
                        else if (control is RYTPieChartControl &&
                                 lastControl != null && lastControl is RYTPieChartControl)
                        {
                            RYTPieChartControl piechartControl = (RYTPieChartControl)lastControl;
                            piechartControl.AddPieChartControl(control as RYTPieChartControl);
                            var parentControl = piechartControl.Parent_;
                            if (parentControl != null)
                            {
                                parentControl.ChildrenElements_.Remove(control);
                                bAddSubControl = true;
                            }
                        }
                        else if (lastControl != null)
                        {
                            if (control.ToString().Contains("RYTPieChartControl_New") &&
                                lastControl.ToString().Contains("RYTPieChartControl_New"))
                            {
                                RYTControl piechartControl = lastControl;
                                MethodInfo mi = piechartControl.GetType().GetMethod("AddPieChartControl");
                                if (mi != null)
                                {
                                    mi.Invoke(piechartControl, new object[] { control });
                                }
                                var parentControl = piechartControl.Parent_;
                                if (parentControl != null)
                                {
                                    parentControl.ChildrenElements_.Remove(control);
                                    bAddSubControl = true;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {

                        throw;
                    }
                }

                if (!bAddSubControl)
                {
                    lastControl = control;
                }

                CreateSubObjects(xe, styles, control, page);//递归创建子节点。
            }
        }

        static RYTControl CreateObject(XElement element, List<RYTCSSStyle> styles, RYTControl parent, TemplatePage page)
        {
            RYTControl control = null;
            string _elementName = element.Name.LocalName;

            string _elementType = string.Empty;
            XAttribute tempAttribute = element.Attribute("type");
            if (tempAttribute != null)
                _elementType = tempAttribute.Value;

            string _elementStyle = string.Empty;
            tempAttribute = element.Attribute("style");
            if (tempAttribute != null)
                _elementStyle = tempAttribute.Value;

            tempAttribute = element.Attribute("name");
            string nameValue = string.Empty;
            if (tempAttribute != null)
                nameValue = tempAttribute.Value;

            #region Find Matched Class & Create Control

            var dictQueryByTag = ClassXmlObjects.Where(c => c["attribute"]["tag"].Equals(_elementName));
            if (dictQueryByTag.Count() > 0)
            {
                // Create object by tag & type 
                if (!string.IsNullOrEmpty(_elementType))
                {
                    var query = dictQueryByTag.Where(c => c["attribute"].ContainsKey("type") && c["attribute"]["type"].Equals(_elementType, StringComparison.CurrentCultureIgnoreCase));
                    if (query != null && query.Count() > 0)
                    {
                        if (!string.IsNullOrEmpty(_elementStyle))//暂时平台没应用style特性
                        {
                            var styleQuery = query.FirstOrDefault(c => c["attribute"].ContainsKey("style") && c["attribute"]["style"].Equals(_elementStyle, StringComparison.CurrentCultureIgnoreCase));
                            if (styleQuery != null)
                            {
                                control = CreateObject(element, styleQuery["attribute"]["class"], styleQuery["attribute"], styleQuery["param"], styles, parent, page);
                                return control;
                            }
                        }
                        var firstQuery = query.FirstOrDefault();
                        control = CreateObject(element, firstQuery["attribute"]["class"], firstQuery["attribute"], firstQuery["param"], styles, parent, page);
                        return control;
                    }
                    else
                    {
                        LogLib.RYTLog.ShowMessage("(Element Parser Not Defined By Type) : \n" + element);
                    }
                }
                // Create object only by tag 
                else
                {
                    var theOnlyOneDict = dictQueryByTag.First();
                    control = CreateObject(element, theOnlyOneDict["attribute"]["class"], theOnlyOneDict["attribute"], theOnlyOneDict["param"], styles, parent, page);
                    return control;
                }
            }
            else  // (Not defined in current project)
            {
                LogLib.RYTLog.ShowMessage("(Parser Failed)Unknown Element : " + _elementName);
            }
            #endregion

            return control;
        }

        static RYTControl CreateObject(XElement element, String className, Dictionary<string, string> objectNodeDict, Dictionary<String, String> param_s, List<RYTCSSStyle> styles, RYTControl parent, TemplatePage page)
        {
            if (className == null || element == null)
                return null;

            #region Create control Instance with Reflection
            string method = objectNodeDict["init"];
            Assembly assembly = Assembly.Load("RYTong.ControlLib");
            Type _class = assembly.GetType("RYTong.ControlLib." + className);
            if (_class == null)
            {
                assembly = Assembly.Load("RYTong.ExtendControlLib");
                _class = assembly.GetType("RYTong.ExtendControlLib." + className);
            }
            if (_class == null)
            {
                assembly = Assembly.Load("RYTong.ChartControlLib");
                _class = assembly.GetType("RYTong.ChartControlLib." + className);
                if (_class == null)
                {
                    assembly = Assembly.Load("RYTong.ExtendJSLib");
                    _class = assembly.GetType("RYTong.ExtendJSLib." + className);
                }

                if (_class == null)
                {
                    assembly = Assembly.Load("RYTong.CustomControlLib");
                    _class = assembly.GetType("RYTong.CustomControlLib" + className);
                }
            }
            if (_class == null)
            {
                RYTong.LogLib.RYTLog.ShowMessage(string.Format("控件:[{0}]没有定义", className));
                return null;
            }
            Object _object = System.Activator.CreateInstance(_class);
            /*
            MethodInfo methods = _class.GetMethod(method);//暂未使用映射表中的init特性指定的方法初始化控件属性
            if (param_s != null && param_s.Count > 0)
            {
                Object[] pars = new Object[] { param_s };
                methods.Invoke(_object, pars);
            }
            else
            {
                methods.Invoke(_object, null);
            }*/
            #endregion

            if (_object is RYTControl)
            {
                RYTControl control = _object as RYTControl;

                #region Set Values to control object
                //加属性。
                IEnumerable<XAttribute> attributes = element.Attributes();
                Dictionary<String, String> attDic = new Dictionary<string, string>();
                foreach (XAttribute attr in attributes)
                {
                    attDic[attr.Name.LocalName] = attr.Value.Trim();
                }
                //用于快速遍历查找含事件的元素而不是遍历整个dom树
                if (attDic.ContainsKey("onclick") || attDic.ContainsKey("onfocus") || attDic.ContainsKey("onblur") || attDic.ContainsKey("onchange"))
                {
                    page.EventControsList.Add(control);
                }
                if (attDic.ContainsKey("id"))
                {
                    string value = string.Empty;
                    attDic.TryGetValue("id", out value);
                    if (!string.IsNullOrEmpty(value))
                        page.ControlsWithIdDic[value] = control;
                }
                control.SetAttributesDict(attDic); // Set Attributes Dict
                control.XElement = element;        // Set XML
                control.ElementName_ = element.Name.LocalName; // Set Element Name
                if (objectNodeDict.ContainsKey("type")) // Set XML Type Attribute
                    control.ElementType_ = objectNodeDict["type"];
                if (element.Value != null) // Set XML Inner Text(Value) to Title_
                {
                    //control.Title_ = AtomParser.ReplacePlaceHolder(element.Value.Trim());
                    control.Title_ = element.Value;
                }

                #endregion
                List<RYTCSSStyle> parentPageStyles = null;
                if (page.ParentPage != null)
                    parentPageStyles = page.ParentPage.allControls_[0].CssStyles_;
                // Find matched style and set instance to control
                control.GetCSSStyle(styles, parentPageStyles, parent, page.ControlsWithSpecifiedCssDic);

                // Init View & Apply Style
                control.InitView();
                control.ApplyStyle();

                if (parent != null)
                {
                    parent.ChildrenElements_.Add(control);
                    control.Parent_ = parent;
                }

                #region Event Register

                control.LuaEvent += (s, e) =>
                {

                    #region Track Event Action 
                    if (control.TrackEventTriggerAction == TrackEventTrigger.None)
                    {
                        LuaScriptEventAction.OnLuaEventAction(s, e);
                        return;
                    }
                    if (control.TrackOnClickAction != null && control.TrackEventTriggerAction == TrackEventTrigger.Click)
                    {
                        control.TrackOnClickAction(control, control.Name_, control.GetDefaultOrInputOrSelectedValue());
                    }
                    #endregion

                    LuaScriptEventAction.OnLuaEventAction(s, e);

                };

                if (control is RYTButtonControl)
                {
                    (control as RYTButtonControl).Clicked += (sender, e) =>
                    {
                        ButtonEventAction.ClickEventAction(sender, e);
                    };
                    (control as RYTButtonControl).EWPButtonImageDownloadEvent += (sender, t, isLeft, v) =>
                    {
                        ImageEventAction.EWPButtonImageDownloadAction(sender, t, isLeft, v);
                    };
                }
                else if (control is RYTSegmentControl)
                {
                    (control as RYTSegmentControl).EWPSegmentTitleImageDownloadEvent += (sender, uri) =>
                    {
                        ImageEventAction.EWPSegmentTitleImageDownloadAction(sender, uri);
                    };
                }
                else if (control is RYTPieChartControl)
                {
                    (control as RYTPieChartControl).PieChartSelected += (sender, e) =>
                    {
                        PieChartEventAction.SelectedEventAction(sender, e);
                    };
                }
                else if (control is RYTTableControl)
                {
                    (control as RYTTableControl).TableScrollBottomEvent += (sender, e) =>
                    {
                        TableEventAction.ScrollToBottomHandler(sender, e);
                    };
                }
                else if (control is RYTAControl)
                {
                    (control as RYTAControl).Clicked += (sender, t, v) =>
                    {
                        AlinkEventAction.ClickEventHandler(sender, t, v);
                    };
                }
                else if (control is RYTImageControl)
                {
                    (control as RYTImageControl).EWPImageDownloadEvent += (sender, t, v) =>
                    {
                        ImageEventAction.EWPImageDownloadAction(sender, t, v);
                    };
                }

                #endregion

                #region Get key-value by control Name in IsolatedStorage And Set Value

                // Air China Project only support: RYTInputControl & RYTPasswordControl
                if (!string.IsNullOrEmpty(control.Name_))
                {
                    if (control is RYTInputControl)
                    {
                        string strValue = DataBaseManager.GetIsolatedObjectValue<string>(control.Name_);
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            strValue = RYTong.DataBaseLib.RYTSecurity.Instance.Decrypt(strValue);
                            (control as RYTInputControl).SetInputText(strValue);
                        }
                    }
                    else if (control is RYTPasswordControl)
                    {
                        string strValue = DataBaseManager.GetIsolatedObjectValue<string>(control.Name_);
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            strValue = RYTong.DataBaseLib.RYTSecurity.Instance.Decrypt(strValue);
                            (control as RYTPasswordControl).SetPasswordValue(strValue);
                        }
                    }
                }

                #endregion

                return control;
            }

            return null;
        }

    }
}
