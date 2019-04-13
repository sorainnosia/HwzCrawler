using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Windows.Forms;

namespace HwzCrawler
{
    public static class AppConfig
    {
        public static string GetTitle()
        {
            return ((ConfigurationManager.AppSettings["Title"] ?? "") == "" ? "HWZ Crawler" : ConfigurationManager.AppSettings["Title"]).ToUpper();
        }

        public static string GetTitleOriginal()
        {
            return ((ConfigurationManager.AppSettings["Title"] ?? "") == "" ? "HWZ Crawler" : ConfigurationManager.AppSettings["Title"]);
        }

        public static string GetRegex()
        {
            return ((ConfigurationManager.AppSettings["Regex"] ?? "") == "" ? @"\b(?:https?://|www\.)\S+\b" : ConfigurationManager.AppSettings["Regex"]);
        }

        public static string GetDomains()
        {
            return (ConfigurationManager.AppSettings["Domains"] ?? "").Replace("{N}", "\n");
        }

        public static int GetParallelism()
        {
            string parallel = (ConfigurationManager.AppSettings["Parallelism"] ?? "4");
            int res = 0;
            if (int.TryParse(parallel, out res) == false) return 4;
            if (res == 0) res = 4;
            return res;
        }

        public static int GetMaxThreadPage()
        {
            string maxThreadPage = (ConfigurationManager.AppSettings["MaxThreadPage"] ?? "0");
            int res = 0;
            if (int.TryParse(maxThreadPage, out res) == false) return 0;
            return res;
        }

        public static string GotNext()
        {
            return ((ConfigurationManager.AppSettings["GotNext"] ?? "") ?? "") == "" ? "<a rel=\"next\"" : ConfigurationManager.AppSettings["GotNext"];
        }

        public static string GetUser()
        {
            return (ConfigurationManager.AppSettings["User"] ?? "");
        }

        public static string GetPass()
        {
            return (ConfigurationManager.AppSettings["Pass"] ?? "");
        }

        public static string GetIgnoreDomains()
        {
            return (ConfigurationManager.AppSettings["IgnoreDomains"] ?? "").Replace("{N}", "\n");
        }

        public static string GetBodies()
        {
            return (ConfigurationManager.AppSettings["Bodies"] ?? "").Replace("{N}", "\n");
        }

        public static string GetIgnoreBodies()
        {
            return (ConfigurationManager.AppSettings["IgnoreBodies"] ?? "").Replace("{N}", "\n");
        }

        public static bool GetRequireLogin()
        {
            string str = ConfigurationManager.AppSettings["RequireLogin"] ?? "";
            str = str.ToUpper().Trim();
            if (string.IsNullOrEmpty(str)) str = "1";
            if (str == "1" || str == "Y" || str == "TRUE") return true;
            return false;
        }

        public static bool GetSwitchAll()
        {
            string str = ConfigurationManager.AppSettings["SwitchAll"] ?? "";
            str = str.ToUpper().Trim();
            if (string.IsNullOrEmpty(str)) str = "1";
            if (str == "1" || str == "Y" || str == "TRUE") return true;
            return false;
        }

        public static bool GetFirstPage()
        {
            return (ConfigurationManager.AppSettings["FirstPage"] ?? "") == "1" || (ConfigurationManager.AppSettings["FirstPage"] ?? "").ToUpper().Trim() == "Y" || (ConfigurationManager.AppSettings["FirstPage"] ?? "").ToUpper().Trim() == "TRUE";
        }

        public static string GetThreadLink()
        {
            return ((ConfigurationManager.AppSettings["ThreadLink"] ?? "") == "" ? "https://sbf.net.nz/showthread.php?t={0}&page={1}" : ConfigurationManager.AppSettings["ThreadLink"]);
        }

        public static string GetScriptPrefix()
        {
            return ConfigurationManager.AppSettings["ScriptPrefix"] ?? "";
        }

        public static string GetLicense()
        {
            return ConfigurationManager.AppSettings["License"] ?? "";
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
    }
}
