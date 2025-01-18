using UI;

namespace Main
{
    public class Program
    {
        private Program() { }
        [STAThread]
        public static void Main()
        {
            var application = new System.Windows.Application();
            var window = new OffsideDetectorWindow();
            application.Run(window);
        }
    }
}