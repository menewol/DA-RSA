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



namespace DA_RSA
{
    public partial class Form1 : Form
    {
        int bitLength=1024;
        BigInteger N, E, D;
        Thread _pThread, _qThread, _eThread, GeneratorThread;

        public Form1()
        {
            InitializeComponent();
            Application.ApplicationExit += Application_ApplicationExit;
            ParameterizedThreadStart pts = new ParameterizedThreadStart(GenerateKeyPair);
            GeneratorThread = new Thread(pts);
            GeneratorThread.Start(bitLength);

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
        void Application_ApplicationExit(object sender, EventArgs e)
        {
            Log.Write();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Log.Write();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Write();
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

    }
}
