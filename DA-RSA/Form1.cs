using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using Microsoft.Win32;


namespace DA_RSA
{
    public partial class Form1 : Form
    {
        MySqlConnection conn1;
        bool logged = false;
        EndPoint server;
        Thread ListenerThread, BlThread;
        string[] blacklist;
        bool lehr = false;
        

        public Form1(string[] args)
        {
            InitializeComponent();
            notifyIcon1.Visible = true;
            Application.ApplicationExit += Application_ApplicationExit;
            Application.ApplicationExit += new EventHandler(this.OnAppExit);
            conn1 = new MySqlConnection(@"server='213.47.71.253';database='rsa';uid='rsa';pwd='rsa'");
            
            if (args.Length >= 1 && args[0] == "-lehrer")
            {
                lehr = true;
            }

            if (lehr == false)
            {
                ListenerThread = new Thread(Receive);
                ListenerThread.Start();

                ParameterizedThreadStart pts = new ParameterizedThreadStart(blacklisten);
                BlThread = new Thread(pts);
                blacklist = new string[] { "cmd", "regedit", "taskmgr", "powershell", "msconfig" };
                BlThread.Start(blacklist);


                UInt32 m;
                UInt32.TryParse("1", out m);
                Write("DisableTaskMgr", m);
            }
            else
            {
                UInt32 m;
                UInt32.TryParse("0", out m);
                Write("DisableTaskMgr", m);
            }
        }

        public void Receive()
        {

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.10.10"), IPAddress.Any));
            socket.Bind(new IPEndPoint(IPAddress.Any, 5554));
            server = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                byte[] tmpBuffer = new byte[32];
                
                int tmp = socket.ReceiveFrom(tmpBuffer, ref server);
                //MessageBox.Show(tmp.ToString() + " " + server.ToString());

                string cmd = Encoding.Default.GetString(tmpBuffer, 0, 3);

                if (cmd == "log")
                {
                    if (logged == false)
                    {
                        this.Show();
                    }
                }
            }
        }

        public void blacklisten(object o)
        {
            string[] bl = (string[])o;

            while (true)
            {
                Process[] p = Process.GetProcesses();

                foreach (string s in bl)
                {
                    foreach (Process x in p)
                    {
                        if (s == x.ProcessName)
                        {
                            try
                            {
                                x.Kill();
                                break;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    }

                    p = Process.GetProcesses();
                }
            }
        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            Log.Write();
            Application.Exit();
            this.Close();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Log.Write();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Write();
        }
       

        private void button_login_Click(object sender, EventArgs e)
        {
            offlogin();
            //login();
        }

        public void login()
        {
            conn1.Open();
            bool lehrer = false;
            string name = textBox_name.Text;
            string pw = GetMd5Hash(textBox_pw.Text);
            bool passt = false;

            MySqlCommand cmd = new MySqlCommand("SELECT name, passwort FROM Logindaten WHERE name = '" + name + "' AND passwort = '" + pw + "';", conn1);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (rdr[0].ToString() == "") passt = false;
                else passt = true;
            }
            rdr.Close();

            if (passt == true)
            {
                MySqlCommand cmd2 = new MySqlCommand("SELECT lehrer FROM Logindaten WHERE name='" + name + "' AND passwort='" + pw + "';", conn1);
                rdr = cmd2.ExecuteReader();
                while (rdr.Read())
                {
                    lehrer = Convert.ToBoolean(rdr[0].ToString());
                }
                rdr.Close();

                if (lehrer == true)
                {
                    LehrerForm lform = new LehrerForm(notifyIcon1);
                    logged = true;
                    this.Hide();
                    lform.ShowDialog();
                    this.Close();
                }
                else
                {
                    SchuelerForm sform = new SchuelerForm(name);
                    logged = true;
                    this.Hide();
                    sform.ShowDialog();
                }
            }
            else
            {
                label_check.Text = "falsches PW oder Acc";
            }
            conn1.Close();
        }

        public void offlogin()
        {
            string name = textBox_name.Text;
            string pw = textBox_pw.Text;

            if (name == "lehrer" && pw == "lehrer")
            {
                LehrerForm lform = new LehrerForm(notifyIcon1);
                logged = true;
                this.Hide();
                lform.ShowDialog();
                lform.Close();
                this.Show();
            }
            else if (name == "schueler" && pw == "schueler")
            {
                SchuelerForm sform = new SchuelerForm(name);
                logged = true;
                this.Hide();
                sform.ShowDialog();
                Close();
            }
            else
            {
                label_check.Text = "falsches PW oder Acc";
            }
        }

        private void OnAppExit(object sender, EventArgs e)
        {
            if (lehr == false)
            {
                Application.Restart();
            }
        }

        static string GetMd5Hash(string input)
        {
            MD5 md5Hash = MD5.Create();
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexad ecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            BenutzerErstellen berst = new BenutzerErstellen(conn1);
            berst.ShowDialog();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (logged == false)
            {
                this.Show();
            }
        }

        public void Write(string KeyName, UInt32 Value)
        {
            try
            {
                // Setting
                RegistryKey rk = Registry.CurrentUser;
                // I have to use CreateSubKey 
                // (create or open it if already exits), 
                // 'cause OpenSubKey open a subKey as read-only
                RegistryKey sk1 = rk.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\");
                // Save the value

                sk1.SetValue(KeyName, Value, RegistryValueKind.DWord);

                //return Convert.ToString(sk1.GetValue(KeyName));

            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                MessageBox.Show("Writing registry " + KeyName.ToUpper());
                //return "...";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void textBox_pw_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                offlogin();
            }
        }

        private void textBox_pw_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
           
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }
    }
}
