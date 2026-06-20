using System;
using System.Windows.Forms;

namespace STF_EC_SyncAxis
{
    static class Program
    {
        /// <summary>
        /// 搴旂敤绋嬪簭鍏ュ彛鐐?        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
