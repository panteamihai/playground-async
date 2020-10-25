using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsyncWorkshop.Basics
{
    public partial class ContextSwitching : Form
    {
        public ContextSwitching()
        {
            InitializeComponent();
            //btnMain.Click += BtnMainClick_Responsive_ButWillThrow_CrossThreadOnlyInDebugMode;
            //btnMain.Click += BtnMainClick_InvokeToTheRescue;
        }

        private async void BtnMainClick_Responsive_ButWillThrow_CrossThreadOnlyInDebugMode(object sender, EventArgs e)
        {
            await Task.Run(
                () =>
                {
                    try
                    {
                        Thread.Sleep(3000);
                        btnMain.Text = "No cross-thread access, please!";
                    }
                    catch (Exception ioe)
                    {
                        txtMessage.Invoke(new Action(() => txtMessage.Text = ioe.Message));
                        //Additional info: https://stackoverflow.com/questions/3972727/why-is-cross-thread-operation-exception-not-thrown-while-running-exe-in-bin-debu
                    }
                });

            btnMain.Text = "Hi, well this was awkward";
        }

        private async void BtnMainClick_InvokeToTheRescue(object sender, EventArgs e)
        {
            await Task.Run(
                () =>
                {
                    Thread.Sleep(3000);
                    btnMain.Invoke(new Action(() => btnMain.Text =
                                "We were on the ThreadPool, no kiddin! Now we have switched to the UI Thread to show this long message."
                                + " Please wait as we're going back to the pool for a little wait, and then back to the UI"));
                    Thread.Sleep(3000);
                });

            btnMain.Text = "Came back to the UI";
        }
    }
}
