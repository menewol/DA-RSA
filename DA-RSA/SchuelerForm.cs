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
using System.Runtime.Serialization;
using System.IO;

namespace DA_RSA
{
    public partial class SchuelerForm : Form
    {
        Thread ListenerThread,ReceiverThread,BlThread;
        BigInteger N, E;
        Socket c;
        EndPoint server;

        public SchuelerForm()
        {
            InitializeComponent();
            ListenerThread = new Thread(Receive);
            ListenerThread.Start();
            notifyIcon_rsa.Visible = true;
            ReceiverThread = new Thread(Receive2);
            ReceiverThread.Start();


            //OnAppStart();
            //Application.ApplicationExit += new EventHandler(this.OnAppExit);
        }

        private void SchuelerForm_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }

        public void Receive()
        {

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.10.10"), IPAddress.Any));
            socket.Bind(new IPEndPoint(IPAddress.Any, 5555));
            server = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                byte[] tmpBuffer = new byte[1024];
                
                int tmp = socket.ReceiveFrom(tmpBuffer, ref server);
                //MessageBox.Show(tmp.ToString() + " " + server.ToString());

                string cmd = Encoding.Default.GetString(tmpBuffer, 0, 1);

                if (cmd == "a")
                {
                    N = BigInteger.Parse(Encoding.Default.GetString(tmpBuffer, 1, tmp));
                    int tmp1 = socket.ReceiveFrom(tmpBuffer, ref server);
                    E = BigInteger.Parse(Encoding.Default.GetString(tmpBuffer, 0, tmp1));
                    notifyIcon_rsa.ShowBalloonTip(2000, "Public Key reveiced", "Es wurde einer öffentlicher Schlüssel empfangen", ToolTipIcon.Info);
                    IPEndPoint ipep = (IPEndPoint)server;
                    ipep.Port = 5555;
                    socket.SendTo(Encoding.Default.GetBytes(Encrypt("iwas")), ipep);
                }
                else if (cmd == "f")
                {
                    string s = Encoding.Default.GetString(tmpBuffer, 1, tmpBuffer.Length - 1);
                    string[] blacklist = s.Split(';');
                    ParameterizedThreadStart pts = new ParameterizedThreadStart(blacklisten);
                    BlThread = new Thread(pts);
                    BlThread.Start(blacklist);
                }
            }

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
                    if (length != 0)
                    {
                        string cmd = Encoding.Default.GetString(buffer, 0, length);
                        if (cmd == "b")
                        {
                            Bitmap bmp = TakeScreenshot(false);
                            bmp.Save(Directory.GetCurrentDirectory() + "\\bild.png", ImageFormat.Png);
                            IPEndPoint ipep = (IPEndPoint)server;
                            ipep.Port = 6868;

                            send_data_sync(Directory.GetCurrentDirectory() + "\\bild.png", "bild.png", ipep.Address, ipep.Port);
                        }
                        else if (cmd == "c")
                        {
                            IPEndPoint ipep = (IPEndPoint)server;
                            ipep.Port = 6868;
                            getProcessList(ipep.Address, ipep.Port);//get process
                        }
                        else if (cmd.Substring(0,1) == "d")
                        {
                            Process p = Process.GetProcessById(Convert.ToInt32(cmd.Substring(1)));
                            p.Kill();
                        }
                        else if (cmd.Substring(0,1) == "e")
                        {
                            string[] msg = cmd.Substring(1).Split(':');
                            notifyIcon_rsa.ShowBalloonTip(20000, msg[0], msg[1], ToolTipIcon.Info);
                        }
                        else if (cmd.Substring(0, 1) == "s")
                        {
                            Process.Start("shutdown", "/s /t 1 /f");
                        }
                        //string tmp = Environment.GetFolderPath(Environment.SpecialFolder.History);
                    }
                }
                catch
                {
                   
                }
            
            }
        }

        public void getProcessList(IPAddress ip, int i)
        {
            Process[] p = Process.GetProcesses();

            StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + "\\processe.txt");
            foreach (Process x in p)
            {
                sw.WriteLine(Convert.ToString(x.ProcessName + "\t" + x.Id + "\t" + x.WorkingSet64));
            }
            sw.Close();

            send_data_sync(Directory.GetCurrentDirectory() + "\\processe.txt", "processe.txt", ip, i);
        }

        public void blacklisten(object o)
        {
            string[] bl = (string[])o;

            while (true)
            {
                Process[] p = Process.GetProcesses();

                //foreach (Process x in p)
                //{
                //    for (int i = 0; i < bl.Length; i++)
                //    {
                //        if (x.ProcessName == bl[i])
                //        {
                //            x.Kill();
                //            break;
                //        }
                //    }
                //}
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

        public void send_data_sync(object filename, object safefilename, IPAddress sendTO, int portSendTO)
        {
            IPAddress ipAddress = sendTO;
            int port = portSendTO;
            int bufferSize = 4096;
            TcpClient client = new TcpClient();
            NetworkStream netStream;

            // Connect to server
            if (client.Connected != true)
            {
                try
                {
                    client.Connect(new IPEndPoint(ipAddress, port));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("connection failed: " + ex.Message);
                }
            }
            netStream = client.GetStream();

            // Read bytes from image
            byte[] data = File.ReadAllBytes((string)filename);

            // Build the package
            byte[] dataLength = BitConverter.GetBytes(data.Length);
            string tmp = (string)safefilename;
            byte[] fileNameLength = BitConverter.GetBytes(tmp.Length);
            byte[] fileName = Encoding.Default.GetBytes((string)safefilename);
            byte[] package = new byte[4 + 4 + data.Length + fileName.Length];
            dataLength.CopyTo(package, 0);
            fileNameLength.CopyTo(package, 4);
            fileName.CopyTo(package, 8);
            data.CopyTo(package, 8 + fileName.Length);

            // Send to server
            int bytesSent = 0;
            int bytesLeft = package.Length, datalength = package.Length;
            while (bytesLeft > 0)
            {

                int nextPacketSize = (bytesLeft > bufferSize) ? bufferSize : bytesLeft;

                netStream.Write(package, bytesSent, nextPacketSize);
                bytesSent += nextPacketSize;
                bytesLeft -= nextPacketSize;

            }

            // Clean up
            netStream.Close();
            client.Close();
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

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("LALALA");
        }
        //CaptureScreenToFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\bild.png", ImageFormat.Png);
        private string Encrypt(string SrcText)
        {
            string tempOutput = null;
            try
            {
                BigInteger e = E;
                BigInteger n = N;

                if (SrcText.Length > n)
                {
                    MessageBox.Show("Cannot encrypt data longer than: " + n + "\r\n\r\nUse key pair of bigger length.",
                                    "Error");

                    return null;
                }

                if (SrcText.Length == 0)
                {
                    return null;
                }

                tempOutput = BigInteger.ModPow(SrcText.Length, e, n).ToString();

                for (int i = 0; i < SrcText.Length; i++)
                {
                    char chr = SrcText[i];

                    BigInteger c = BigInteger.ModPow(Convert.ToInt32(chr), e, n);

                    if (tempOutput == "")
                    {
                        tempOutput = c.ToString();
                    }
                    else
                    {
                        tempOutput += " " + c.ToString();
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Enter valid public key components to encrypt.", "Error");
            }
            return tempOutput;
        }

        private string Decrypt(string CipherText)
        {
            string tempOutput = null;
            if (CipherText.Length == 0)
            {
                return null;
            }

            try
            {
                BigInteger d = 0;
                BigInteger n = N;

                string text = "";

                try
                {
                    string[] cipher = CipherText.Split(' ');

                    BigInteger m = BigInteger.ModPow(BigInteger.Parse(cipher[0]), d, n);

                    if (cipher.Length == 1)
                    {
                        tempOutput = m.ToString();
                    }
                    else
                    {
                        BigInteger length = m;

                        for (int i = 1; i < cipher.Length; i++)
                        {
                            m = BigInteger.ModPow(BigInteger.Parse(cipher[i]), d, n);

                            //M = (M % (BigInteger.Pow(2, 31)));

                            char chr = Convert.ToChar(Convert.ToInt32(m.ToString()));

                            text += chr;
                        }

                        if (length == text.Length)
                        {
                            tempOutput = text;
                        }
                        else
                        {
                            MessageBox.Show("Error decrypting: incomplete message.", "Error");
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Error decrypting, invalid private key.", "Error");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Enter valid private key components to decrypt.", "Error");
            }
            return tempOutput;
        }


    }
}
