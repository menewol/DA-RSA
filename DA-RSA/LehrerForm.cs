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
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Net.Mail;

namespace DA_RSA
{
    public partial class LehrerForm : Form
    {
        int bitLength = 512;
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
        string[] adresse;
        List<IPEndPoint> clList = new List<IPEndPoint>();
        MySqlConnection conn;
        NotifyIcon noti;

        public LehrerForm(NotifyIcon nf1)
        {
            InitializeComponent();
            noti = nf1;
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
            //button1.invoke((action)delegate {
            //    button1.enabled = true;
            //});
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
                        
                        //byte[] buf = Encoding.Default.GetBytes("abcd");

                        BroadCastKeyPair(N, E);                       
                        
                        //BigInteger m = BitConverter.ToInt64(buf,0);
                        //BigInteger c = BigInteger.ModPow(m, D, N);
                        //BigInteger f = BigInteger.ModPow(E, m, N);
                        //MessageBox.Show(c.ToString() + " = " + f.ToString());
                        
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
                adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                adresse = adresse[0].Split('-');
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(adresse[1]), 8888);
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
            int i = 0;
            Socket tmp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tmp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tmp.Bind(new IPEndPoint(IPAddress.Any, 5555));
            byte[] buff = new byte[2048];
            while (true)
            {
                endp = new IPEndPoint(IPAddress.Any, 0);
                int anz = tmp.ReceiveFrom(buff, 2048, SocketFlags.None, ref endp);
                string temp = Encoding.Default.GetString(buff, 0, anz);
                temp = Decrypt(temp);

                if (anz != 0)
                {
                    listBox1.Invoke((Action)delegate
                    {
                        listBox1.Items.Add(temp + "-" + endp.ToString());
                    });
                    tabPage1.Invoke((Action)delegate
                    {
                        ListBox lsb = new ListBox();
                        tabPage1.Controls.Add(lsb);
                        lsb.Size = new Size(120, 121);
                        lsb.Items.Add(temp);
                        lsb.Items.Add(endp.ToString());
                        if (i < 7)
                        {
                            lsb.Location = new Point(15 + (130 * i), 85);
                        }
                        else if(i < 14)
                        {
                            lsb.Location = new Point(15 + (130 * i), 216);
                        }
                        else lsb.Location = new Point(15 + (130 * i), 347);
                    });
                    i++;
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
                allBytesRead = 0;
                bytesLeft = 0;
                bytesRead = 0;
                dataLength = 0;
                //bullshit
                
                StreamReader srw = new StreamReader(Directory.GetCurrentDirectory() + "\\received Files\\" + adresse[0] + "\\" + i.ToString() + fileName);

                String s = srw.ReadToEnd();
                listView1.Invoke((Action)delegate
                {
                    listView1.Items.Clear();
                    listView1.FullRowSelect = true;
                    listView1.AutoArrange = false;
                    listView1.MultiSelect = false;
                    listView1.View = View.Details;
                    foreach (string item in RegularWetzer(s))
                    {
                        string temp = Decrypt(item);
                        if (temp != null)
                        {
                            string[] barbieschloss = temp.Split(';');
                            int z = Convert.ToInt32(barbieschloss[2]);
                            barbieschloss[2] = (Math.Round((double)z/1000)).ToString() + " KB";
                            ListViewItem lsv = new ListViewItem(barbieschloss);
                            listView1.Items.Add(lsv); 
                        }
                       
                    }

                });
                


                i++;
                //frm.Close();
                ProcListener.Abort();
            }
        }
        static DataTable ConvertListToDataTable(List<string[]> list)
        {
            // New table.
            DataTable table = new DataTable();

            // Get max columns.
            int columns = 0;
            foreach (var array in list)
            {
                if (array.Length > columns)
                {
                    columns = array.Length;
                }
            }

            // Add columns.
            for (int i = 0; i < columns; i++)
            {
                table.Columns.Add();
            }

            // Add rows.
            foreach (var array in list)
            {
                table.Rows.Add(array);
            }

            return table;
        }
        private List<string> RegularWetzer(string input)
        {
            string s = input;
            List<string> list = new List<string>();
            string pattern2 = "\r\n";

            string[] substrings = Regex.Split(s, pattern2);
            //for (int i = 0; i < substrings.Length; i++)
            //{
            //    list.Add(substrings[i].Split('\t'));
            //}
            for (int i = 0; i < substrings.Length; i++)
            {
                list.Add(substrings[i]);
            }

            return list;
        }

        private void LehrerForm_Load(object sender, EventArgs e)
        {
            authThread = new Thread(authListener);
            authThread.IsBackground = true;
            authThread.Start();
            ParameterizedThreadStart pts = new ParameterizedThreadStart(GenerateKeyPair);
            GeneratorThread = new Thread(pts);
            GeneratorThread.IsBackground = true;
            GeneratorThread.Start(bitLength);

            button1.Enabled = true;

            Application.ApplicationExit += Application_ApplicationExit;

            SendMessage(textBox1.Handle, 0x1501, 1, "Titel.");
            SendMessage(textBox2.Handle, 0x1501, 1, "Message.");

            try
            {
                conn = new MySqlConnection(@"server='127.0.0.1';database='rsa_daten';uid='root';pwd=''");
                conn.Open();
            }
            catch (Exception)
            {
                MessageBox.Show("Datenbankverbindung konnte nicht aufgebaut werden!");
                //throw;
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {

             if (listBox1.SelectedIndex != -1)
            {
                adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                adresse = adresse[0].Split('-');
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(adresse[1]), 8888);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(ipep);
                s.Send(Encoding.Default.GetBytes("c"));
                s.Close();
                ProcListener = new Thread(doRevProcess);
                ProcListener.IsBackground = true;
                ProcListener.Start();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int tmpID=0;
            adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
            adresse = adresse[0].Split('-');
            

            if (listView1.SelectedItems.Count > 0)
            {
                ListView.SelectedListViewItemCollection process = this.listView1.SelectedItems;
                foreach (ListViewItem item in process)
                {
                    tmpID = Convert.ToInt32(item.SubItems[1].Text);
                }  
            }
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(adresse[1]), 8888);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(ipep);
            s.Send(Encoding.Default.GetBytes("d"+tmpID.ToString()));
            s.Send(Encoding.Default.GetBytes("c"));
        }

        private void button4_Click(object sender, EventArgs e)
        {

            if (listBox1.SelectedIndex != -1)
            {
                adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                adresse = adresse[0].Split('-');
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(adresse[1]), 8888);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(ipep);
                s.Send(Encoding.Default.GetBytes("e" + textBox1.Text + ":" + textBox2.Text));
            }
            else
            {
                MessageBox.Show("Sie müssen einen Client auswählen.");
            }
            
        }
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                adresse = listBox1.Items[listBox1.SelectedIndex].ToString().Split(':');
                adresse = adresse[0].Split('-');
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(adresse[1]), 8888);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(ipep);
                s.Send(Encoding.Default.GetBytes("s"));
            }
            else
            {
                MessageBox.Show("Sie müssen einen Client auswählen.");
            }
        }

        private void bearbeitenToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void rechnerSuchenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BroadCastKeyPair(N, E);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label1.Text = "Anzahl verwaltbarer Rechner: " + clList.Count.ToString();
        }

        private void button_bl_Click(object sender, EventArgs e)
        {
            //Blacklist
            Blacklist bl = new Blacklist(conn);
            bl.ShowDialog();
        }

        private void button_sbl_Click(object sender, EventArgs e)
        {
            //starten der Blacklist
            string s = "f";
            MySqlCommand cmd = new MySqlCommand("SELECT Prozess FROM blacklist;", conn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                s += rdr[0].ToString() + ";";
            }
            rdr.Close();
            
            socket.SendTo(Encoding.Default.GetBytes(s), new IPEndPoint(mcast, 5555));
        }

        private string Encrypt(string SrcText)
        {
            string tempOutput = null;

            byte[] tempb = new byte[4];

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

                for (int i = 0; i < SrcText.Length; i+=4)
                {
                    //char chr = SrcText[i];

                    for (int t = 0; t < 4; t++)
                    {
                        tempb[t] = (byte)SrcText[i + t];
                    }

                    BigInteger c = BigInteger.ModPow(BitConverter.ToInt32(tempb,0), e, n);

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
                BigInteger d = D;
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

                            //char chr = Convert.ToChar(Convert.ToInt32(m.ToString()));
                            byte[] tempb = m.ToByteArray();


                            text += Encoding.Default.GetString(tempb, 0, 4);

                            //text += chr;
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

        private void button6_Click(object sender, EventArgs e)
        {
            string s = "g";
            socket.SendTo(Encoding.Default.GetBytes(s), new IPEndPoint(mcast, 5555));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Filesharing_Lehrer fsl = new Filesharing_Lehrer(noti);
            fsl.ShowDialog();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                string email = "";

                MySqlCommand cmd = new MySqlCommand("SELECT email FROM logindaten WHERE name='" + listBox1.Items[listBox1.SelectedIndex].ToString().Split('-')[0] + "';", conn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    email = rdr[0].ToString();
                }
                rdr.Close();

                MailMessage mailMsg = new MailMessage();
                mailMsg.To.Add(email);
                // From
                MailAddress mailAddress = new MailAddress("da.rsa@htl-ottakring.ac.at");
                mailMsg.From = mailAddress;

                // Subject and Body
                mailMsg.Subject = "Schulpc";
                mailMsg.Body = "Du hast vergessen deinen PC herunterzufahren. Bitte komm zurück und schalte ihn aus!";

                // Init SmtpClient and send on port 587 in my case. (Usual=port25)
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
                System.Net.NetworkCredential credentials =
                   new System.Net.NetworkCredential("da.rsa@htl-ottakring.ac.at", "1l4Co72R03n5q8cK");
                smtpClient.Credentials = credentials;
                smtpClient.EnableSsl = true;
                smtpClient.Send(mailMsg);
            }
            else
            {
                MessageBox.Show("Sie müssen einen Client auswählen!");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            socket.SendTo(Encoding.Default.GetBytes("log"), new IPEndPoint(mcast, 5554));
        }

    }
}
