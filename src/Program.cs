using System;
using UI;

namespace Main
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            var application = new System.Windows.Application();
            var window = new MainWindow();
            application.Run(window);
        }
    }
}