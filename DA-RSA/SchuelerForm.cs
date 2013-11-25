using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;

namespace DA_RSA
{
    public partial class SchuelerForm : Form
    {
        Thread ListenerThread,ReceiverThread;
        BigInteger N, E;
        Socket c;

        public SchuelerForm()
        {
            InitializeComponent();
            ListenerThread = new Thread(Receive);
            ListenerThread.Start();

            ReceiverThread = new Thread(Receive2);
            ReceiverThread.Start();


            OnAppStart();
            Application.ApplicationExit += new EventHandler(this.OnAppExit);
        }

        private void SchuelerForm_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }

        public Process[] GetProcessList()
        {
            Process[] processlist = Process.GetProcesses();

            return processlist;
        }

        public void Receive()
        {

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.10.10"), IPAddress.Any));
            socket.Bind(new IPEndPoint(IPAddress.Any, 5555));

            byte[] tmpBuffer = new byte[1024];
            EndPoint tmpEp = new IPEndPoint(IPAddress.Any, 0);
            int tmp = socket.ReceiveFrom(tmpBuffer, ref tmpEp);
            N = BigInteger.Parse(Encoding.Default.GetString(tmpBuffer, 0, tmp));
            tmp = socket.ReceiveFrom(tmpBuffer, ref tmpEp);
            E = BigInteger.Parse(Encoding.Default.GetString(tmpBuffer, 0, tmp));
            notifyIcon1.ShowBalloonTip(2000, "Public Key reveiced", "Es wurde einer öffentlicher Schlüssel empfangen", ToolTipIcon.Info);
            IPEndPoint ipep = (IPEndPoint)tmpEp;
            ipep.Port = 5555;
            socket.SendTo(Encoding.Default.GetBytes("iwas"),ipep);
           
        }
        public void Receive2()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Bind(new IPEndPoint(IPAddress.Any, 8888));
            s.Listen(100);
            while (true)
            {
                c = s.Accept();
                byte[] buffer = new byte[2048];
                try
                {
                    int length = c.Receive(buffer);
                    MessageBox.Show(Encoding.Default.GetString(buffer,0,length));
                    
                }
                catch
                {
                   
                }
            
            }
        }

        private Bitmap TakeScreenshot(bool onlyForm)
        {
            int StartX, StartY;
            int Width, Height;

            if (onlyForm) StartX = this.Left;
            else StartX = 0;

            if (onlyForm) StartY = this.Left;
            else StartY = 0;

            if (onlyForm) Width = this.Width;
            else Width = Screen.PrimaryScreen.Bounds.Width;

            if (onlyForm) Height = this.Height;
            else Height = Screen.PrimaryScreen.Bounds.Height;

            Bitmap Screenshot = new Bitmap(Width, Height);
            Graphics G = Graphics.FromImage(Screenshot);

            G.CopyFromScreen(StartX, StartY, 0, 0, new Size(Width, Height), CopyPixelOperation.SourceCopy);
            return Screenshot;
        }

        public void WriteReg(UInt32 Value)
        {
            string KeyName = "DisableTaskMgr";
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
            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                MessageBox.Show("Writing registry " + KeyName.ToUpper());
                Log.Add(e.Message);
            }
        }

        private void OnAppExit(object sender, EventArgs e)
        {
            UInt32 m;
            UInt32.TryParse("0", out m);
            WriteReg(m);
            Log.Write();
        }

        private void OnAppStart()
        {
            UInt32 m;
            UInt32.TryParse("1", out m);
            //WriteReg(m);
        }

        private void SchuelerForm_Load(object sender, EventArgs e)
        {

        }
        //CaptureScreenToFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\bild.png", ImageFormat.Png);
    }
}
