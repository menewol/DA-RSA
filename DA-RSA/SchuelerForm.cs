﻿using System;
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

namespace DA_RSA
{
    public partial class SchuelerForm : Form
    {
        Thread ListenerThread;
        BigInteger N, E;

        public SchuelerForm()
        {
            InitializeComponent();
            ListenerThread = new Thread(Receive);
            ListenerThread.Start();
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
            EndPoint tmpEp = new IPEndPoint(IPAddress.Loopback, 0);
            int tmp = socket.ReceiveFrom(tmpBuffer, ref tmpEp);
            N = BigInteger.Parse(Encoding.Default.GetString(tmpBuffer, 0, tmp));
            tmp = socket.ReceiveFrom(tmpBuffer, ref tmpEp);
            E = BigInteger.Parse(Encoding.Default.GetString(tmpBuffer, 0, tmp));



            byte[] buffer = new byte[1024];
            int bytes;
            EndPoint from = new IPEndPoint(IPAddress.Loopback, 0);
            while (true)
            {
                bytes = socket.ReceiveFrom(buffer, ref from);
                string cmd = Encoding.Default.GetString(buffer, 0, bytes);
                if (bytes != 0)
                {
                    if (cmd == "GetScreenshot")
                    {
                        MessageBox.Show("hier.");
                    }
                }
            }

        }

        private class GDI32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }
        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
        }
        public Image CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);
            return img;
        }
        public Image CaptureScreen()
        {
            return CaptureWindow(User32.GetDesktopWindow());
        }
        public void CaptureScreenToFile(string filename, ImageFormat format)
        {
            Image img = CaptureScreen();
            img.Save(filename, format);
        }
        //CaptureScreenToFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\bild.png", ImageFormat.Png);
    }
}
