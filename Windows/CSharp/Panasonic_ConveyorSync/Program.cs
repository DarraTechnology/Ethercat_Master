using System;
using System.Windows.Forms;

namespace Panasonic_ConveyorSync
{
    static class Program
    {
        /// <summary>
        /// 应用程序入口点
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
