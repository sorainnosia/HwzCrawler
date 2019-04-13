using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Configuration;
using ScrapperMinDLL;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace HwzCrawler
{
    public partial class Form2 : Form
    {
        public string User = string.Empty;
        public string Pass = string.Empty;
        public AppLibrary _Crawler = null;

        public Form2()
        {
            InitializeComponent();
            User = AppConfig.GetUser();
            Pass = AppConfig.GetPass();
            
            InitForm();
        }

        public Form2(string user, string pass)
        {
            InitializeComponent();
            User = user;
            Pass = pass;

            InitForm();
        }

        public void AddText(RichTextBox ctl, string title, string url, string str)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<RichTextBox, string, string, string>(AddText), ctl, title, url, str);
                return;
            }

            string s = "[Found] link:' ";
            ctl.AppendText(s );
            //LinkLabel link = new LinkLabel();
            //link.Text = url;
            //link.LinkClicked += Link_LinkClicked;
            //ctl.Controls.Add(link);
            ctl.AppendText(str);
            title = title.Replace(" - www.hardwarezone.com.sg", "");
            string s2 = " ', title:'" + title + "', url:' " + url + " '";
            ctl.AppendText(s2 + "\r\n");
            richTextBox1.ScrollToCaret();

            _Crawler.AddLog(s);
        }

        private void Link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
        }

        public void AddTextRich1(string title, string url, string str)
        {
            AddText(richTextBox1, title, url, str);
        }

        public void SetTextLblProcessing(string str)
        {
            SetText(lblProcessing, str);
        }

        public bool GetCheckBox1()
        {
            return GetCheck(checkBox1);
        }

        public void SetCheckBox1(bool val)
        {
            SetCheck(checkBox1, val);
        }

        public void InitForm()
        {
            btnStart.Image = picStart.Image;
            label4.Text = AppConfig.GetTitle();
            Text = AppConfig.GetTitleOriginal();

            if (AppConfig.GetRequireLogin() == false) button3.Visible = false;
            _Crawler = new AppLibrary(User, Pass);
            _Crawler.AddText = new Action<string, string, string>(AddTextRich1);
            _Crawler.SetText = new Action<string>(SetTextLblProcessing);
            _Crawler.GetCheck = new Func<bool>(GetCheckBox1);
            _Crawler.SetCheck = new Action<bool>(SetCheckBox1);
        }

        public void SetText(Control ctl, string str)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Control, string>(SetText), ctl, str.Replace("&", "&&"));
                return;
            }
            ctl.Text = str;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (AppConfig.GetRequireLogin() && (string.IsNullOrEmpty(User) || string.IsNullOrEmpty(Pass)))
            {
                MessageBox.Show("Login is not saved in previous form");
                return;
            }
            if (btnStart.Image == picStart.Image)
            {
                btnStart.Image = picStop.Image;
                lblStatus.Text = "Started";
                _Crawler.Start();
            }
            else
            {
                lblProcessing.Text = string.Empty;
                btnStart.Image = picStart.Image;
                lblStatus.Text = "Stopped";
                _Crawler.Stop();
                _Crawler.StopAll();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _Crawler.Stop();
            _Crawler.StopAll();
            Environment.Exit(0);
        }

        public void SetCheck(CheckBox chk, bool value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<CheckBox, bool>(SetCheck), chk, value);
                return;
            }
            chk.Checked = value;
        }

        public bool GetCheck(CheckBox chk)
        {
            if (InvokeRequired)
            {
                return (bool)Invoke(new Func<CheckBox, bool>(GetCheck), chk);
            }
            return chk.Checked;
        }

        private void richTextBox2_Leave(object sender, EventArgs e)
        {
            AppConfig.AddOrUpdateAppSettings("Domains", richTextBox2.Text.Replace("\n", "{N}"));
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            richTextBox2.Text = AppConfig.GetDomains();
            richTextBox3.Text = AppConfig.GetIgnoreDomains();
            richTextBox4.Text = AppConfig.GetIgnoreBodies();
            richTextBox5.Text = AppConfig.GetBodies();
            richTextBox6.Text = AppConfig.GetMaxThreadPage().ToString();
            richTextBox7.Text = AppConfig.GetParallelism().ToString();
            checkBox1.Checked = AppConfig.GetFirstPage();

            if (AppLicense.IsValid("", "", AppConfig.GetLicense()) == false)
            {
                MessageBox.Show("Invalid License Key");
                Environment.Exit(0);
                return;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result"));

        }

        private void richTextBox3_Leave(object sender, EventArgs e)
        {
            AppConfig.AddOrUpdateAppSettings("IgnoreDomains", richTextBox3.Text.Replace("\n", "{N}"));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _Crawler.Stop();
            _Crawler.StopAll();
            Form1 frm = new Form1(true);
            frm.ShowInTaskbar = ShowInTaskbar;
            frm.ShowDialog();
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            AppConfig.AddOrUpdateAppSettings("FirstPage", checkBox1.Checked ? "1" : "0");
            if (checkBox1.Checked && _Crawler.RunningThread != null)
            {
                _Crawler.StopAll();
            }
            if (_Crawler.IsRunning)
            {
                if (checkBox1.Checked == false && _Crawler.RunningThread == null)
                {
                    _Crawler.StartAll();
                }
            }
        }

        private void richTextBox4_Leave(object sender, EventArgs e)
        {
            AppConfig.AddOrUpdateAppSettings("IgnoreBodies", richTextBox4.Text.Replace("\n", "{N}"));
        }

        private void richTextBox5_Leave(object sender, EventArgs e)
        {
            AppConfig.AddOrUpdateAppSettings("Bodies", richTextBox5.Text.Replace("\n", "{N}"));
        }

        private void label4_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void richTextBox6_Leave(object sender, EventArgs e)
        {
            AppConfig.AddOrUpdateAppSettings("MaxThreadPage", richTextBox6.Text);
        }

        private void richTextBox7_Leave(object sender, EventArgs e)
        {
            AppConfig.AddOrUpdateAppSettings("Parallelism", richTextBox7.Text);
        }

        private void panel5_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }
    }
}
