using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScrapperMinDLL;
using System.Configuration;
using System.Runtime.InteropServices;

namespace HwzCrawler
{
    public partial class Form1 : Form
    {
        public bool Force = false;
        public Form1()
        {
            InitializeComponent();
            InitForm();
        }

        public Form1(bool force)
        {
            InitializeComponent();
            Force = force;
            InitForm();
        }

        public void InitForm()
        {
            label1.Text = AppConfig.GetTitle();
            Text = AppConfig.GetTitleOriginal();
            if (AppLicense.RequireLicense == false)
            {
                button3.Visible = false;
                button4.Visible = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool requireLogin = AppConfig.GetRequireLogin();

            if (requireLogin && (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Text)))
            {
                MessageBox.Show("Invalid user login");
                return;
            }
            if (requireLogin)
            {
                string str = ScrapperMinPool.RunSingle(AppConfig.GetScriptPrefix() + "SBF_LOGIN", new string[] { txtUsername.Text, txtPassword.Text });
                if (str == "FAIL LOGIN")
                {
                    MessageBox.Show("Invalid user login");
                    return;
                }
            }

            AppConfig.AddOrUpdateAppSettings("User", txtUsername.Text);
            AppConfig.AddOrUpdateAppSettings("Pass", txtPassword.Text);
            if (AppLicense.IsValid("", "", AppConfig.GetLicense()) == false)
            {
                MessageBox.Show("Invalid License Key");
                return;
            }
            Form2 frm = new Form2(txtUsername.Text, txtPassword.Text);
            frm.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form3 frm = new Form3();
            frm.ShowDialog();
            if (AppLicense.IsValid("", "", AppConfig.GetLicense()))
            {
                button4.Visible = false;
                if (AppConfig.GetRequireLogin() == false)
                {
                    txtUsername.Visible = false;
                    txtPassword.Visible = false;
                    label3.Visible = false;
                    label2.Text = "Click NEXT to continue";
                    label2.TextAlign = ContentAlignment.MiddleCenter;
                    button4.Visible = false;
                }
            }
        }

        private int count = 0;

        private void panel3_Click(object sender, EventArgs e)
        {
            count++;
            if (count == 10)
            {
                count = 0;
                Form3 frm = new Form3(true);
                frm.ShowDialog();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtUsername.Text = AppConfig.GetUser();
            txtPassword.Text = AppConfig.GetPass();
            button4.Visible = AppLicense.RequireLicense & string.IsNullOrEmpty(AppConfig.GetLicense());

            bool requireLogin = AppConfig.GetRequireLogin();

            bool licenseValid = AppLicense.IsValid("", "", AppConfig.GetLicense());
            if (licenseValid) button4.Visible = false;
            if (Force == false && licenseValid)
            {
                if (requireLogin)
                { 
                    if ((string.IsNullOrEmpty(txtUsername.Text) == false && string.IsNullOrEmpty(txtPassword.Text) == false))
                    {
                        Form2 frm = new Form2(txtUsername.Text, txtPassword.Text);
                        frm.ShowInTaskbar = true;
                        frm.ShowDialog();
                    }
                }
                else
                {
                    Form2 frm = new Form2(txtUsername.Text, txtPassword.Text);
                    frm.ShowInTaskbar = true;
                    frm.ShowDialog();
                }
                return;
            }

            if (requireLogin == false)
            {
                txtUsername.Visible = false;
                txtPassword.Visible = false;
                
                label3.Visible = false;
                if (licenseValid == false)
                {
                    label2.Text = "Please activate license or trial";
                }
                else
                {
                    label2.Text = "Click NEXT to continue";
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (AppLicense.IsValid("", "", AppConfig.GetLicense()) == false)
            {
                string str = AppLicense.GetLicense("", "", "", DateTime.Now.AddDays(7));
                AppConfig.AddOrUpdateAppSettings("License", str);
                if (string.IsNullOrEmpty(str) == false)
                {
                    MessageBox.Show("Trial is activated");
                    button4.Visible = false;
                    if (AppConfig.GetRequireLogin() == false)
                    {
                        txtUsername.Visible = false;
                        txtPassword.Visible = false;
                        label3.Visible = false;
                        label2.Text = "Click NEXT to continue";
                        label2.TextAlign = ContentAlignment.MiddleCenter;
                    }
                }
                else
                {
                    MessageBox.Show("Fail activating trial");
                    return;
                }
            }
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
