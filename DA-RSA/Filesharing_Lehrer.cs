using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DA_RSA
{
    public partial class Filesharing_Lehrer : Form
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress mcast = IPAddress.Parse("239.255.10.10");
        OpenFileDialog ofd = new OpenFileDialog();
        NotifyIcon noti;
        public Filesharing_Lehrer(NotifyIcon nf1)
        {
            InitializeComponent();
            noti = nf1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ofd.ShowDialog();
            textBox1.Text = ofd.FileName;
        }

        private void button2_Click(object sender, EventArgs e)
        {

            byte[] temp;
            temp = File.ReadAllBytes((string)ofd.FileName);
            byte[] sendArr = new byte[temp.Length + 10];
            int zw = temp.Length;
            Buffer.BlockCopy(temp, 0, sendArr, 10, zw);

            sendArr[0] = (int)'h';
            string[] z = ofd.FileName.Split('.');
            temp = Encoding.Default.GetBytes(z[z.Length-1]);
            Buffer.BlockCopy(temp,0,sendArr,1,temp.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(zw),0,sendArr,8,2);

            socket.SendTo(sendArr, new IPEndPoint(mcast, 5555));

            noti.ShowBalloonTip(2000, "Datei versendet", "Die Datei "+ofd.SafeFileName+" wurde versendet.", ToolTipIcon.Info);

            this.Close();
        }
    }
}
