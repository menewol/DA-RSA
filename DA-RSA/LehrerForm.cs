using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DA_RSA
{
    public partial class LehrerForm : Form
    {
        int bitLength = 1024;
        BigInteger N, E, D;
        Thread _pThread, _qThread, _eThread, GeneratorThread,t,ProcListener,authThread;
        Socket socket= new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress mcast = IPAddress.Parse("239.255.10.10");
        List<IPAddress> clients = new List<IPAddress>();  
        static TcpClient client;
        static int bufferSize = 1024;
        static NetworkStream netStream;
        static int bytesRead = 0;
        static int allBytesRead = 0;
        int i = 0;
        EndPoint endp;

        public LehrerForm()
        {
            InitializeComponent();
            authThread = new Thread(authListener);
            authThread.IsBackground = true;
            authThread.Start();
            ParameterizedThreadStart pts = new ParameterizedThreadStart(GenerateKeyPair);
            GeneratorThread = new Thread(pts);
            GeneratorThread.IsBackground = true;
            GeneratorThread.Start(bitLength);

            button1.Enabled = false;

            Application.ApplicationExit += Application_ApplicationExit;

        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            authThread.Abort();
            GeneratorThread.Abort();
            _qThread.Abort();
            _pThread.Abort();
            _eThread.Abort();
            this.Close();
        }
        public void BroadCastKeyPair(BigInteger n, BigInteger e)
        {
            socket.SendTo(Encoding.Default.GetBytes("a" + n.ToString()), new IPEndPoint(mcast, 5555));
            socket.SendTo(Encoding.Default.GetBytes(e.ToString()), new IPEndPoint(mcast, 5555));
            button1.Invoke((Action)delegate {
                button1.Enabled = true;
            });
        }
        public void GenerateKeyPair(object TEMPbitLength)
        {
            int bitLength = (int)TEMPbitLength;
            Generator.Initialize(2);
            BigInteger numMin = BigInteger.Pow(2, (this.bitLength / 2) - 1);
            BigInteger numMax = BigInteger.Pow(2, (this.bitLength / 2));
            var p = new PrimeNumber();
            var q = new PrimeNumber();
            p.SetNumber(Generator.Random(numMin, numMin));
            q.SetNumber(Generator.Random(numMin, numMax));
            _pThread = new Thread(p.RabinMiller);
            _qThread = new Thread(q.RabinMiller);
            _pThread.IsBackground = true;
            _qThread.IsBackground = true;
            DateTime start = DateTime.Now;
            _pThread.Start();
            _qThread.Start();
            while (_pThread.IsAlive || _qThread.IsAlive)
            {
                Application.DoEvents();
                TimeSpan ts = DateTime.Now - start;
                if (ts.TotalMilliseconds > (1000 * 60 * 5))
                {
                    try
                    {
                        _pThread.Abort();
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }

                    try
                    {
                        _qThread.Abort();
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }

                    MessageBox.Show("Key generating error: timeout.\r\n\r\nIs your bit length too large?", "Error");
                    
                    Log.Add("Key generating error: timeout.\r\n\r\nIs your bit length too large?");

                    break;
                }
            }
            if (p.GetFoundPrime() && q.GetFoundPrime())
            {
                BigInteger n = p.GetPrimeNumber() * q.GetPrimeNumber();
                BigInteger euler = (p.GetPrimeNumber() - 1) * (q.GetPrimeNumber() - 1);
                var e = new PrimeNumber();
                while (true)
                {
                    e.SetNumber(Generator.Random(2, euler - 1));
                    start = DateTime.Now;
                    _eThread = new Thread(e.RabinMiller);
                    _eThread.IsBackground = true;
                    _eThread.Start();

                    while (_eThread.IsAlive)
                    {
                        Application.DoEvents();
                        TimeSpan ts = DateTime.Now - start;

                        if (ts.TotalMilliseconds > (1000 * 60 * 5))
                        {
                            MessageBox.Show("Key generating error: timeout.\r\n\r\nIs your bit length too large?", "Error");
                            
                            Log.Add("Key generating error: timeout.\r\n\r\nIs your bit length too large?");
                            break;
                        }

                    }

                    if (e.GetFoundPrime() && (BigInteger.GreatestCommonDivisor(e.GetPrimeNumber(), euler) == 1))
                    {
                        break;
                    }
                }
                if (e.GetFoundPrime())
                {
                    BigInteger d = MathExtended.ModularLinearEquationSolver(e.GetPrimeNumber(), 1, euler);
                    if (d > 0)
                    {
                        // N
                        N = n;
                      
                        
                        // E
                        E = e.GetPrimeNumber();

                        // D
                        D = d;

                        BroadCastKeyPair(N, E);
                        Log.Add("Successfully created key pair.");
                        
                    }
                    else
                    {
                        Log.Add("Error: Modular equation solver fault.");
                        
                        MessageBox.Show("Error using mathematical extensions.\r\ne = " + e + "\r\neuler = " + euler + "\r\np = " + p.GetPrimeNumber() + "\r\n" + q.GetPrimeNumber(), "Error");
                        Log.Add("Error using mathematical extensions.\r\ne = " + e + "\r\neuler = " + euler + "\r\np = " + p.GetPrimeNumber() + "\r\n" + q.GetPrimeNumber());
                    }
                }

            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                string[] adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(adresse[0]), 8888);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(ipep);
                s.Send(Encoding.Default.GetBytes("b"));
                s.Close();
                t = new Thread(doRevImage);
                t.IsBackground = true;
                t.Start();
            }
            
        }
        private void authListener()
        {
            Socket tmp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tmp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tmp.Bind(new IPEndPoint(IPAddress.Any, 5555));
            byte[] buff = new byte[1024];
            while (true)
            {
                endp = new IPEndPoint(IPAddress.Any, 0);
                int anz = tmp.ReceiveFrom(buff, 1024, SocketFlags.None, ref endp);
                if (anz != 0)
                {
                    listBox1.Invoke((Action)delegate
                    {
                        listBox1.Items.Add(endp.ToString());
                    });
    
                }
            }
        }
        private void doRevImage()
        {
            TcpListener listen = new TcpListener(IPAddress.Any,6868);
            listen.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            while (true)
            {
                listen.Start();

                // Accept client
                client = listen.AcceptTcpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                netStream = client.GetStream();

                // Read length of incoming data
                byte[] length = new byte[8];
                bytesRead = netStream.Read(length, 0, 8);
                int dataLength = BitConverter.ToInt32(length, 0);
                int tmp = BitConverter.ToInt32(length, 4);

                //Read file name.
                byte[] filename = new byte[tmp];
                bytesRead = netStream.Read(filename, 0, tmp);
                string fileName = Encoding.Default.GetString(filename);

                // Read the data
                int bytesLeft = dataLength;
                byte[] data = new byte[dataLength];
                while (bytesLeft > 0)
                {
                    int nextPacketSize = (bytesLeft > bufferSize) ? bufferSize : bytesLeft;
                    bytesRead = netStream.Read(data, allBytesRead, nextPacketSize);
                    allBytesRead += bytesRead;
                    bytesLeft -= bytesRead;
                }
                string[] adresse = new string[0];

                listBox1.Invoke((Action)delegate
                    {
                         adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                    });

                adresse[0].Replace(".", "-");

                if (Directory.Exists(Directory.GetCurrentDirectory() + "\\received Files\\"+adresse[0]))
                {
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0] + "\\" + i.ToString() + fileName, data);
                }
                else
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0]);
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0] + "\\" + i.ToString() + fileName, data);
                }

                // Clean up
                netStream.Close();
                client.Close();
                listen.Stop();
                
                //
                //System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "\\received Files\\" + fileName);
                allBytesRead = 0;
                bytesLeft = 0;
                bytesRead = 0;
                dataLength = 0;
                Form frm = new Form();
                Image img = Image.FromFile(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0] + "\\" + i.ToString() + fileName);
                Bitmap objBitmap = new Bitmap(img, new Size((img.Width / 3) * 2, (img.Height / 3) * 2));
                PictureBox ptb = new PictureBox();
                ptb.Width = (img.Width / 3) * 2;
                ptb.Height = (img.Height / 3) * 2;
                ptb.Image = objBitmap;
                frm.Controls.Add(ptb);
                frm.Width = ((img.Width / 3) * 2) + 18;
                frm.Height = ((img.Height / 3) * 2) + 40;
                frm.ShowDialog();
                i++;
                frm.Close();
                t.Abort();
            }
        }

        private void doRevProcess()
        {
            TcpListener listen = new TcpListener(IPAddress.Any, 6868);
            listen.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            while (true)
            {
                listen.Start();

                // Accept client
                client = listen.AcceptTcpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                netStream = client.GetStream();

                // Read length of incoming data
                byte[] length = new byte[8];
                bytesRead = netStream.Read(length, 0, 8);
                int dataLength = BitConverter.ToInt32(length, 0);
                int tmp = BitConverter.ToInt32(length, 4);

                //Read file name.
                byte[] filename = new byte[tmp];
                bytesRead = netStream.Read(filename, 0, tmp);
                string fileName = Encoding.Default.GetString(filename);

                // Read the data
                int bytesLeft = dataLength;
                byte[] data = new byte[dataLength];
                while (bytesLeft > 0)
                {
                    int nextPacketSize = (bytesLeft > bufferSize) ? bufferSize : bytesLeft;
                    bytesRead = netStream.Read(data, allBytesRead, nextPacketSize);
                    allBytesRead += bytesRead;
                    bytesLeft -= bytesRead;
                }
                string[] adresse = new string[0];

                listBox1.Invoke((Action)delegate
                    {
                        adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                    });

                adresse[0].Replace(".", "-");

                if (Directory.Exists(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0]))
                {
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0] + "\\" + i.ToString() + fileName, data);
                }
                else
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0]);
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0] + "\\" + i.ToString() + fileName, data);
                }

                // Clean up
                netStream.Close();
                client.Close();
                listen.Stop();

                Form frm = new Form();
                ListBox lsb = new ListBox();
                StreamReader srw = new StreamReader(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0] + "\\" + i.ToString() + fileName);

                String s = srw.ReadToEnd();
                string[] prozesse = s.Split('\n');
                foreach (string item in prozesse)
                {
                    listBox1.Items.Add(item);
                }
                frm.Controls.Add(lsb);
                frm.ShowDialog();
                i++;
                frm.Close();

            }
        }

        private void LehrerForm_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
             if (listBox1.SelectedIndex != -1)
            {
                string[] adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(adresse[0]), 8888);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(ipep);
                s.Send(Encoding.Default.GetBytes("c"));
                s.Close();
                ProcListener = new Thread(doRevProcess);
                ProcListener.IsBackground = true;
                ProcListener.Start();
            }
        }
    }
}
