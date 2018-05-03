using System.Windows.Forms;

namespace AudioPlayer
{
    public partial class Player : UserControl
    {
        public Player()
        {
            InitializeComponent();
        }

        public void Play(string file)
        {
            axWindowsMediaPlayer1.settings.autoStart = false;
            axWindowsMediaPlayer1.settings.volume = 100;
            axWindowsMediaPlayer1.URL = file;
            axWindowsMediaPlayer1.Ctlcontrols.play();
        }
    }
}
