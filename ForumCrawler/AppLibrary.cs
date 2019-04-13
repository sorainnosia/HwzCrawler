using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ScrapperMinDLL;
using System.IO;
using System.Text.RegularExpressions;

namespace HwzCrawler
{
    public class AppLibrary
    {
        public ScrapperMin sm = ScrapperMin.InitWC();
        public string ResultDir = "Result";
        public string ScriptDir = "Scripts";
        public string DONTDELETE = "[DONTDELETE].txt";
        public string LoginScript = "SBF_LOGIN.TXT";
        public string PageScript = "SBF.TXT";
        public string LogFile = "logs.txt";
        public bool HasLogin = false;
        public object LoginLock = new object();
        public Thread PageOneThread = null;
        public Thread RunningThread = null;
        public string User = string.Empty;
        public string Pass = string.Empty;
        public bool IsRunning = false;
        public bool IsRunningAll = false;
        public object LockAll = new object();
        public object FileLock = new object();
        public List<string> ALL = new List<string>();
        public object LogLock = new object();
        public Action<string, string, string> AddText;
        public Action<string> SetText;
        public Func<bool> GetCheck;
        public Action<bool> SetCheck;
        public Dictionary<string, object> LinksLock = new Dictionary<string, object>();
        public Dictionary<string, string> ReadFile = new Dictionary<string, string>();

        public AppLibrary(string user, string pass)
        {
            InitAll();
            DONTDELETE = Path.Combine(ResultDir, DONTDELETE);
        }

        public void Start()
        {
            IsRunning = true;

            try
            {
                ThreadStart ts2 = new ThreadStart(StartPageOne);
                PageOneThread = new Thread(ts2);
                PageOneThread.Start();
                AddLog("[Success] Start()");
            }
            catch (Exception ex)
            {
                AddLog("[Fail] Start() " + ex.Message);
            }
            StartAll();
        }

        public void StartAll()
        {
            lock (LockAll)
            {
                if (AppConfig.GetFirstPage() == false)
                {
                    IsRunningAll = true;
                    if (RunningThread == null)
                    {
                        try
                        {
                            ThreadStart ts = new ThreadStart(StartReal);
                            RunningThread = new Thread(ts);
                            RunningThread.Start();
                            AddLog("[Success] StartAll()");
                        }
                        catch (Exception ex)
                        {
                            AddLog("[Fail] StartAll() " + ex.Message);
                        }
                    }
                }
            }
        }

        public void AddAllPage(string url)
        {
            lock (FileLock)
            {
                ALL.Add(url);
                WriteToFile(DONTDELETE, url + "\r\n");
            }
        }

        public void WriteToFile(string filename, string s)
        {
            if (string.IsNullOrEmpty(filename)) return;
            if (filename.ToLower().EndsWith(".txt")) filename = filename.Substring(0, filename.Length - 4);
            if (string.IsNullOrEmpty(filename)) return;

            if (Directory.Exists(ResultDir) == false) Directory.CreateDirectory(ResultDir);
            if (File.Exists(filename + ".txt") == false)
            {
                File.WriteAllText(filename + ".txt", s);
            }
            else
            {
                File.AppendAllText(filename + ".txt", s);
            }
        }
        
        public void AddLog(string s)
        {
            lock (LogLock)
            {
                string filename = Path.Combine(ResultDir, LogFile);
                WriteToFile(filename, s + "\r\n");
            }
        }

        public void AddFile(string title, string url, string str)
        {
            try
            {
                Uri u = new Uri(str);
                string filename = Path.Combine(ResultDir, u.Authority.Replace(".", ""));
                try
                {
                    if (LinksLock.ContainsKey(filename) == false) LinksLock.Add(filename, new object());
                }
                catch { }

                lock (LinksLock[filename])
                {
                    if (Directory.Exists("Result") == false) Directory.CreateDirectory("Result");
                    if (File.Exists(filename + ".txt"))
                    {
                        if (ReadFile.ContainsKey(filename + ".txt") == false)
                        {
                            ReadFile.Add(filename + ".txt", File.ReadAllText(filename + ".txt"));
                        }
                        if (ReadFile[filename + ".txt"].Contains(str)) return;
                    }
                    WriteToFile(filename + ".txt", str + "\r\n");
                    ReadFile[filename + ".txt"] = ReadFile[filename + ".txt"] + str + "\r\n";
                    if (AddText != null) AddText(title, url, str);
                }
            }
            catch { }
        }

        public void InitAll()
        {
            if (ALL.Count == 0)
            {
                if (Directory.Exists("Result") == false) Directory.CreateDirectory("Result");
                if (File.Exists(DONTDELETE))
                {
                    lock (FileLock)
                    {
                        ALL = File.ReadAllLines(DONTDELETE).ToList();
                    }
                }
            }
        }

        public void AddLink(string str)
        {
            InitAll();
            string[] ss = str.Split(new char[] { ' ' });
            if (ss == null || ss.Length == 0 || ss.Length != 2) return;
            string uu = AppConfig.GetThreadLink();

            Uri uri = null;
            try
            {
                uri = new Uri(uu);
            }
            catch
            {
                AddLog("[Ignore] IsThreadUri:false, url:'" + uu + "'");
                return;
            }

            string url = string.Format(uu, ss[0], ss[1]);
            if (ALL.Contains(url)) return;

            lock (LoginLock)
            {
                if (HasLogin == false && AppConfig.GetRequireLogin())
                {
                    string scriptprefix = AppConfig.GetScriptPrefix();
                    if (File.Exists(Path.Combine(ScriptDir, scriptprefix + LoginScript)) == false) return;
                    string[] strx = null;
                    try
                    {
                        strx = sm.Multiple(File.ReadAllText(Path.Combine(ScriptDir, scriptprefix + LoginScript)), new List<string> { User, Pass });
                    }
                    catch { }
                    if (strx != null && strx.Length == 1 && strx[0] != "SUCCESS LOGIN") return;
                    HasLogin = true;
                }
            }

            string page = string.Empty;
            try
            {
                page = sm.WC.MethodPage(url, "GET", "", new Dictionary<string, string>());
            }
            catch { }
            if (string.IsNullOrEmpty(page)) return;

            string title = "";
            string next = AppConfig.GotNext();
            if (page.IndexOf(next) >= 0)
            {
                AddAllPage(url);
            }

            string[] tts = StringOps.TagMatch(page, "<title>", "</title>");
            if (tts != null && tts.Length > 0) title = tts[0];
            if (SetText != null) SetText("[" + ss[1].ToString() + "] " + title);

            string regex = AppConfig.GetRegex();
            var linkParser = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matchesCol = linkParser.Matches(page);
            if (matchesCol == null || matchesCol.Count == 0)
            {
                AddLog("[NotFound] title:'" + title + "', url:'" + url + "'");
                return;
            }

            List<string> domains = new List<string>();
            string[] doms = AppConfig.GetDomains().Split(new char[] { '\n' });
            string[] ignores = AppConfig.GetIgnoreDomains().Split(new char[] { '\n' });
            if (ignores.Length > 0 && ignores[0] == "") ignores = new string[0];
            string[] bodies = AppConfig.GetIgnoreBodies().Split(new char[] { '\n' });
            if (bodies.Length > 0 && bodies[0] == "") bodies = new string[0];
            string[] bodiesMatches = AppConfig.GetBodies().Split(new char[] { '\n' });
            if (bodies.Length > 0 && bodies[0] == "") bodies = new string[0];

            if (doms.Length > 0 && doms[0] != "")
            {
                foreach (string dom in doms)
                {
                    domains.Add(dom.Trim());
                }
            }

            if (bodies.Length > 0 && bodies[0] != "")
            {
                foreach (string body in bodies)
                {
                    if (page.ToLower().Contains(body.ToLower()))
                    {
                        AddLog("[Ignore] ContainsBody:'" + body + "', title:'" + title + "', url:'" + url + "'");
                        return;
                    }
                }
            }
            if (bodiesMatches.Length > 0 && bodiesMatches[0] != "")
            {
                bool contains = false;
                foreach (string body in bodiesMatches)
                {
                    if (body.ToLower().StartsWith("http") && page.ToLower().Contains(body.ToLower()))
                    {
                        contains = true;
                        break;
                    }
                    else if (body.ToLower().StartsWith("http") == false)
                    {
                        if (page.ToLower().Contains(body.ToLower()))
                        {
                            contains = true;
                            break;
                        }
                    }

                }
                if (contains == false)
                {
                    AddLog("[Ignore] NotContainsBody:true, title:'" + title + "', url:'" + url + "'");
                    return;
                }
            }

            int count = 0;
            foreach (Match m in matchesCol)
            {
                if (m == null || string.IsNullOrEmpty(m.Value)) continue;

                string urlx = m.Value;
                if (urlx.Contains("imgur"))
                {
                    Console.WriteLine("a");
                }
                if (urlx.Contains("\"")) urlx = m.Value.Substring(0, m.Value.IndexOf("\""));
                if (urlx.Contains("<")) urlx = m.Value.Substring(0, m.Value.IndexOf("<"));
                if (urlx.Contains(".") == false)
                {
                    AddLog("[Ignore] ContainsDot:false, title:'" + title + "', url:'" + url + "', urlx:'" + urlx + "'");
                    continue;
                }
                if (urlx.Contains("..."))
                {
                    AddLog("[Ignore] Contains3Dots:true, title:'" + title + "', url:'" + url + "', urlx:'" + urlx + "'");
                    continue;
                }
                try
                {
                    Uri urlxx = new Uri(urlx);
                }
                catch
                {
                    AddLog("[Ignore] IsUri:false, title:'" + title + "', url:'" + url + "', urlx:'" + urlx + "'");
                    continue;
                }
                bool ig = false;
                foreach (string ig2 in ignores)
                {
                    if (urlx.ToLower().Contains(ig2.ToLower()))
                    {
                        ig = true;
                        AddLog("[Ignore] ig2:'" + ig2 + "', title:'" + title + "', url:'" + url + "', urlx:'" + urlx + "'");
                        break;
                    }
                }
                if (ig) continue;

                if (domains.Count > 0)
                {
                    foreach (string dom in domains)
                    {
                        if (urlx.ToLower().IndexOf(dom.ToLower()) < 0) continue;

                        count++;
                        AddFile(title, url, urlx);
                        break;
                    }
                }
                else
                {
                    count++;
                    AddFile(title, url, urlx);
                }
            }
            if (count == 0)
            {
                AddLog("[NotFound] title:'" + title + "', url:'" + url + "'");
            }
        }

        public void StartPageOne()
        {
            InitAll();
            string uu = AppConfig.GetThreadLink();
            bool switchall = AppConfig.GetSwitchAll();

            while (IsRunning)
            {
                try
                {
                    string scriptprefix = AppConfig.GetScriptPrefix();
                    string[] ts = ScrapperMinPool.RunMultiple(scriptprefix + PageScript, new string[] { User, Pass, "1" });

                    List<string> threads = new List<string>();
                    string[] doms = AppConfig.GetIgnoreDomains().Split(new char[] { '\n' });
                    if (doms != null && doms.Length > 0 && doms[0] == "") doms = new string[0];
                    List<string> domains = new List<string>();
                    if (doms.Length > 0 && doms[0] != "")
                    {
                        foreach (string dom in doms)
                        {
                            domains.Add(dom.Trim());
                        }
                    }

                    foreach (string str in ts)
                    {
                        string[] ss = str.Split(new char[] { ' ' });
                        if (ss == null || ss.Length != 2) continue;

                        int t = 0;
                        if (int.TryParse(ss[1], out t) == false) continue;

                        int maxPage = AppConfig.GetMaxThreadPage();
                        if (t > maxPage && maxPage != 0)
                        {
                            string sss = string.Format(uu, ss[0], t.ToString());
                            AddLog("[Skip] ExceedMaxPageThread:true, url:'" + sss + "'");
                            continue;
                        }

                        for (int i = 1; i <= t; i++)
                        {
                            string sss = string.Format(uu, ss[0], i.ToString());
                            if (ALL.Contains(sss)) continue;

                            bool contains = false;
                            foreach (string dom in domains)
                            {
                                if (sss.ToLower().Contains(dom.ToLower()))
                                {
                                    AddLog("[Skip] IgnoreDomains:'" + dom + "', url:'" + sss + "'");
                                    contains = true;
                                    break;
                                }
                            }
                            if (contains) continue;

                            string s = ss[0] + " " + i.ToString();
                            if (threads.Contains(s) == false)
                            {
                                threads.Add(s);
                            }
                        }
                    }

                    if (threads.Count == 0 && switchall && GetCheck() == true) SetCheck(false);
                    Parallel.ForEach(threads,
                        new ParallelOptions { MaxDegreeOfParallelism = AppConfig.GetParallelism() },
                       t =>
                       {
                           if (IsRunning == false) return;
                           try
                           {
                               AddLink(t);
                           }
                           catch (Exception ex) { AddLog("[Fail] Add Log " + ex.Message); }
                       });
                }
                catch { }
            }
        }

        public object ThreadsLock = new object();
        public int ThreadsDone = 0;
        public void StartReal()
        {
            InitAll();
            string uu = AppConfig.GetThreadLink();

            int pageIndex = 1;
            while (IsRunningAll)
            {
                pageIndex++;
                string scriptprefix = AppConfig.GetScriptPrefix();
                string[] ts = ScrapperMinPool.RunMultiple(scriptprefix + PageScript, new string[] { User, Pass, pageIndex.ToString() });

                if ((ts == null || ts.Length == 0))
                {
                    pageIndex = 1;
                    continue;
                }

                List<string> threads = new List<string>();
                string[] doms = AppConfig.GetIgnoreDomains().Split(new char[] { '\n' });
                if (doms != null && doms.Length > 0 && doms[0] == "") doms = new string[0];
                List<string> domains = new List<string>();
                if (doms.Length > 0 && doms[0] != "")
                {
                    foreach (string dom in doms)
                    {
                        domains.Add(dom.Trim());
                    }
                }

                foreach (string str in ts)
                {
                    string[] ss = str.Split(new char[] { ' ' });
                    if (ss == null || ss.Length != 2) continue;

                    int t = 0;
                    if (int.TryParse(ss[1], out t) == false) continue;

                    int maxPage = AppConfig.GetMaxThreadPage();
                    if (t > maxPage && maxPage != 0)
                    {
                        string sss = string.Format(uu, ss[0], t.ToString());
                        AddLog("[Skip] ExceedMaxPageThread:true, url:'" + sss + "'");
                        continue;
                    }

                    for (int i = 1; i <= t; i++)
                    {
                        string sss = string.Format(uu, ss[0], i.ToString());
                        if (ALL.Contains(sss)) continue;

                        bool contains = false;
                        foreach (string dom in domains)
                        {
                            if (sss.ToLower().Contains(dom.ToLower()))
                            {
                                AddLog("[Skip] IgnoreDomains:'" + dom + "', url:'" + sss + "'");
                                contains = true;
                                break;
                            }
                        }
                        if (contains) continue;

                        string s = ss[0] + " " + i.ToString();
                        if (threads.Contains(s) == false)
                        {
                            threads.Add(s);
                        }
                    }
                }

                Parallel.ForEach(threads,
                    new ParallelOptions { MaxDegreeOfParallelism = AppConfig.GetParallelism() },
                   t =>
                   {
                       if (IsRunningAll == false) return;
                       if (AppConfig.GetFirstPage()) return;
                       try
                       {
                           AddLink(t);
                       }
                       catch (Exception ex) { AddLog("[Fail] Add Log " + ex.Message); }

                       lock (ThreadsLock) ThreadsDone++;
                       if (ThreadsDone == threads.Count)
                       {
                           lock (ThreadsLock) ThreadsDone = 0;
                       }
                   });

                if (AppConfig.GetFirstPage()) return;
            }
        }

        public void Stop()
        {
            IsRunning = false;

            if (PageOneThread != null)
            {
                try
                {
                    PageOneThread.Abort();
                }
                catch { }
                PageOneThread = null;
            }
        }

        public void StopAll()
        {
            IsRunningAll = false;
            lock (LockAll)
            {
                if (RunningThread != null)
                {
                    try
                    {
                        RunningThread.Abort();
                    }
                    catch { }
                    RunningThread = null;
                }
            }
        }
    }
}
