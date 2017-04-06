using RYTong.LuaScript;

namespace RYTong.MainProject.Controller
{
    public class PerformanceTest
    {
        private PerformanceTest() { }

        public static void Run()
        {
            LuaUtility.ExtendDelagate = ExtendLuaMethod;
        }

        public static bool ExtendLuaMethod(string str1, string str2)
        {
            return true;
        }

        public static void Dispose()
        {
            LuaUtility.ExtendDelagate = null;
        }
    }
}
