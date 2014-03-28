using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TestWindowMoveAndResize
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084)        //WM_NCHITTEST
            {
                long x = (long)m.LParam & 0xFFFF;               //mouse relative to left top of screen
                long y = ((long)m.LParam >> 16) & 0xFFFF;
                x -= Left;
                y -= Top;
                if(x < 5)
                {
                    if (y < 5)
                        m.Result = (System.IntPtr)13;       //HTTOPLEFT
                    else if (y > Height - 5)
                        m.Result = (System.IntPtr)16;       //HTBOTTOMLEFT
                    else
                        m.Result = (System.IntPtr)10;       //HTLEFT
                }
                else if (x > Width - 5)
                {
                    if (y < 5)
                        m.Result = (System.IntPtr)14;       //HTTOPRIGHT
                    else if (y > Height - 5)
                        m.Result = (System.IntPtr)17;       //HTBOTTOMRIGHT
                    else
                        m.Result = (System.IntPtr)11;       //HTRIGHT
                }
                else
                {
                    if (y < 5)
                        m.Result = (System.IntPtr)12;       //HTTOP
                    else if (y > Height - 5)
                        m.Result = (System.IntPtr)15;       //HTBOTTOM
                    else
                        m.Result = (System.IntPtr)2;        //HTCAPTION
                }
            }
            else
                base.WndProc(ref m);
        }
    }
}
