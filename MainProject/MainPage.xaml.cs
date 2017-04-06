using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using RYTLuaCplusLib;
using RYTong.LuaScript;
using System.Threading.Tasks;
using System.Xml.Linq;
using MainProject.Controller;
using MainProject.View;
using ControlLib;
using System.Collections.Generic;
using RYTong.MainProject.Controller;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Net;

namespace MainProject
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        LuaManager luaManager = null;
        ContentFrame frame = null;
        //ContentFrame contentFrame = new ContentFrame()
        //{
        //    Width=300,
        //    Height =300,
        //    Background = new SolidColorBrush(Colors.Green),
        //    HorizontalAlignment = HorizontalAlignment.Stretch,
        //    VerticalAlignment=VerticalAlignment.Stretch                
        //};
        RYTButtonControl rytButton = null;
        public MainPage()
        {
            this.InitializeComponent();
            //Button btn = new Button() { Width = 300, Height = 300 };
            //RootGrid.Children.Add(btn);
        
        }
        //private async void Button_Click(object sender, RoutedEventArgs e)
        //{
            //string text = DataBaseLib.DataBaseManager.ReadAppPackageFile("Test.xml", "Text") as string;
            //XElement element = XElement.Parse(text);
            //List<RYTControl> controls = InitializeControl.CreateAllObjects(element, new TemplatePage());
            //if (controls.Count > 0)
            //{
            //    Button button = controls[0].View as Button;
            //    if(button !=null)
            //    {
            //        button.HorizontalAlignment = HorizontalAlignment.Center;                
            //        RootGrid.Children.Add(button);                
            //    }
            //}
            //Lua.LuaL_dostring(luaManager.L,"window:alert(\"one\")");
            //if(this.rytButton!=null)
            //{
            //    rytButton.style.Width += 50;
            //}          
        //}
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            frame = new ContentFrame(true)
            {
                Width = 320,
                Height = 480,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            RootGrid.Children.Add(frame);
            string content = await DataBaseLib.DataBaseManager.ReadAppPackageTextAsync("Test.xml");
            frame.Navigate(content);

            //if (LuaManager.RootLuaManager == null)
            //    luaManager = LuaManager.GetRootLuaManager();
            //LuaManager.RootLuaManager.DetailV_ = this;            
            //string text = await DataBaseLib.DataBaseManager.ReadAppPackageTextAsync("Test.xml");
            //XElement element = XElement.Parse(text);
            //List<RYTControl> controls = InitializeControl.CreateAllObjects(element, new TemplatePage());
            //if (controls.Count > 0)
            //{
            //    Button button = controls[0].View as Button;
            //    rytButton = button.Tag as RYTButtonControl;
            //    if (rytButton.CurrentCSSStyle_ != null)
            //    {
            //        double width = rytButton.CurrentCSSStyle_.width_;
            //        double height = rytButton.CurrentCSSStyle_.height_;
            //    }
            //    if (button != null)
            //    {
            //        button.HorizontalAlignment = HorizontalAlignment.Center;
            //        RootGrid.Children.Add(button);
            //    }
            //}
        }
    }
}
