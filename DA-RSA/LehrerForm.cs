using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DA_RSA
{
    public partial class LehrerForm : Form
    {
        int bitLength = 1024;
        BigInteger N, E, D;
        Thread _pThread, _qThread, _eThread, GeneratorThread,t,authThread;
        Socket socket= new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress mcast = IPAddress.Parse("239.255.10.10");
        List<IPAddress> clients = new List<IPAddress>();
        Socket tmp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            

        public LehrerForm()
        {
            InitializeComponent();
            authThread = new Thread(authListener);
            authThread.Start();
            ParameterizedThreadStart pts = new ParameterizedThreadStart(GenerateKeyPair);
            GeneratorThread = new Thread(pts);
            GeneratorThread.Start(bitLength);

            tmp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tmp.Bind(new IPEndPoint(IPAddress.Any, 5555));
            //t = new Thread(doRevImage);
            //t.IsBackground = true;
            //t.Start();

            button1.Enabled = false;



        }
        public void BroadCastKeyPair(BigInteger n, BigInteger e)
        {
            socket.SendTo(Encoding.Default.GetBytes(n.ToString()), new IPEndPoint(mcast, 5555));
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
                string[] s = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                socket.SendTo(Encoding.Default.GetBytes("GetScreenshot"), new IPEndPoint(IPAddress.Parse(s[0]), 6868));
                TcpListener listen = new TcpListener(IPAddress.Any, 6868);
                TcpClient client;
                NetworkStream netStream;
                int bytesRead = 0;

                listen.Start();

                client = listen.AcceptTcpClient();
                netStream = client.GetStream();

                byte[] length = new byte[8];
                bytesRead = netStream.Read(length, 0, 8);
                int z = Convert.ToInt32(Encoding.Default.GetString(length));

                byte[] screen = new byte[z];
                netStream.Read(screen, 8, z);

                File.WriteAllBytes("C:\\Users\\Ayhan Cetin\\Pictures\\screenshot.jpeg", screen);
            }
        }
        private void authListener()
        {
            byte[] buff = new byte[1024];
            EndPoint endp = new IPEndPoint(IPAddress.Any, 0);
            MessageBox.Show("recv");
            while (true)
            {
                int anz = tmp.ReceiveFrom(buff, 1024, SocketFlags.None, ref endp);
                if (anz != 0)
                {
                    listBox1.Invoke((Action)delegate { listBox1.Items.Add(endp.ToString()); });
                }
            }
            
        }
        private void doRevImage()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //receive screenshot on port 6868.
            s.Bind(new IPEndPoint(IPAddress.Any, 6868));
            byte[] buff = new byte[1024];
            EndPoint endp = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                int anz = s.ReceiveFrom(buff, 1024, SocketFlags.None, ref endp);
                byte[] screen = new byte[anz];
                if (anz != 0)
                {
                    try
                    {
                        for (int i = 0; i < anz; i++)
                        {
                            screen[i] = buff[i];
                        }
                        //File.WriteAllBytes("C:\\Users\\Drmola\\Pictures\\Screenshot2.png", screen);
                        StreamWriter srw = new StreamWriter("C:\\Users\\Drmola\\Pictures\\Screenshot2.png");
                        

                        //srw.Write(tmp);
                    }
                    finally
                    {
                        t.Abort();
                    }
                }
            }
        }
    }
}
