using System.Windows.Forms;

namespace HBMusicCreator
{
    public partial class FrmScoreInfo : Form
    {
        public string Title
        {
            get => txtTitle.Text;
            set => txtTitle.Text = value;
        }

        public string Composer
        {
            get => txtComposer.Text;
            set => txtComposer.Text = value;
        }

        public string Information
        {
            get => txtInfo.Text;
            set => txtInfo.Text = value;
        }

        public FrmScoreInfo() => InitializeComponent();
    }
}
