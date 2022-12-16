namespace LoveLangInterpreter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            tbOutput.Text = LoveLang.Interpret(tbCode.Text, tbInput.Text);
        }
    }
}