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
    public partial class Form3 : Form
    {
        public Thread PageOneThread = null;
        public Thread RunningThread = null;
        public string User = string.Empty;
        public string Pass = string.Empty;
        public bool IsRunning = false;

        public Form3()
        {
            InitializeComponent();
            User = AppConfig.GetUser();
            Pass = AppConfig.GetPass();
            InitForm();
        }

        public Form3(string user, string pass)
        {
            InitializeComponent();
            InitForm();
        }

        public bool GenLic = false;
        public Form3(bool bl)
        {
            InitializeComponent();
            if (bl)
            {
                GenLic = true;
                txtProductCode.ReadOnly = false;
            }
            InitForm();
        }

        public void InitForm()
        {
            label1.Text = AppConfig.GetTitle();
            Text = AppConfig.GetTitleOriginal();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (AppLicense.IsValid("", "", txtActivation.Text) == false)
            {
                MessageBox.Show("Invalid License Key");
                return;
            }

            DateTime dt2 = AppLicense.GetValidUntilDate("", "", AppConfig.GetLicense());
            DateTime dt = AppLicense.GetValidUntilDate("", "", txtActivation.Text);

            if (dt > dt2)
            {
                MessageBox.Show("Thank you for activating");
                AppConfig.AddOrUpdateAppSettings("License", txtActivation.Text);
            }
            else
            {
                MessageBox.Show("Fail to apply old license");
            }
            Close();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            txtProductCode.Text = AppLicense.GetProductCode();
            txtActivation.Text = AppConfig.GetLicense();
            if (AppLicense.IsValid("", "", txtActivation.Text))
            {
                DateTime dt = AppLicense.GetValidUntilDate("", "", txtActivation.Text);
                if (dt == DateTime.MinValue)
                {
                    lblUntil.Text = "No License";
                }
                else
                    lblUntil.Text = "License Valid Until : " + dt.ToString("dd MMM yyyy hh:mm tt");
            }
            else
            {
                lblUntil.Text = "No License";
            }
            txtProductCode_TextChanged(txtProductCode, EventArgs.Empty);
        }

        private void txtProductCode_TextChanged(object sender, EventArgs e)
        {
            if (GenLic == false) return;
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

        private void label1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
