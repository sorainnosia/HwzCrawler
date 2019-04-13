using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HwzCrawler
{
    public class AppLicense
    {
        public static bool RequireLicense = false;
        public static string GetProductCode()
        {
            return "";
        }

        public static bool IsValid(string prefix, string postfix, string license)
        {
            return true;
        }

        public static string GetLicense(string id, string prefix, string postfix, DateTime validuntil)
        {
            return "";
        }

        public static DateTime GetValidUntilDate(string prefix, string postfix, string license)
        {
            return DateTime.Now.AddDays(7);
        }
    }
}
