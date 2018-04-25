using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsyncErrorHandling
{
    public partial class UI : Form
    {
        public UI()
        {
            InitializeComponent();
            //btnMain.Click += BtnMainClick_CannotCatchExceptionsFromAsyncVoid_ResultingInUnhandledException_WithSynchronousThrowing;
            //btnMain.Click += BtnMainClick_CannotCatchExceptionsFromAsyncVoid_ResultingInUnhandledException_WithAsynchronousThrowing;
            //btnMain.Click += BtnMainClick_ThrowingSynchronously_IsStillAnUnhandledException;
            //btnMain.Click += BtnMainClick_CanWrapSynchronousVoid_WithTryCatch_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_IfExceptionsAreCaughtInsideAsyncVoid_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_AwaitingReturnedFaultedTask_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_AwaitingAwaitedFaultedTask_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_TwoLevelsOfNestedAsyncAwaitCalls_IsStillAnUnhandledExceptions;
            //btnMain.Click += BtnMainClick_SynchronouslyWaitingForFaultedTaskIsStupidBut_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_Responsive_ButWillThrow_CrossThreadOnlyInDebugMode;
            //btnMain.Click += BtnMainClick_InvokeToTheRescue;
        }

        private async void BtnMainClick_CannotAwaitAsyncVoid(object sender, EventArgs e)
        {
            //Uncomment this, but it won't compile
            //await ThrowExceptionAsync();

            btnMain.Text = "Will end up here after Unhadled Exception is thrown!";
        }

        private void BtnMainClick_CannotCatchExceptionsFromAsyncVoid_ResultingInUnhandledException_WithSynchronousThrowing(object sender, EventArgs e)
        {
            try
            {
                ThrowSynchronousExceptionInAsyncVoid();
            }
            catch (Exception)
            {
                // The exception is never caught here!
            }

            btnMain.Text = "Will end up here after Unhadled Exception is thrown!";
        }

        private async void ThrowSynchronousExceptionInAsyncVoid()
        {
            throw new InvalidOperationException();
        }

        private void BtnMainClick_CannotCatchExceptionsFromAsyncVoid_ResultingInUnhandledException_WithAsynchronousThrowing(object sender, EventArgs e)
        {
            try
            {
                ThrowAsynchronousExceptionInAsyncVoid();
            }
            catch (Exception)
            {
                // The exception is never caught here!
            }

            btnMain.Text = "Will end up here after Unhadled Exception is thrown!";
        }

        private async void ThrowAsynchronousExceptionInAsyncVoid()
        {
            await Task.Run(() => throw new InvalidOperationException());
        }

        private void BtnMainClick_ThrowingSynchronously_IsStillAnUnhandledException(object sender, EventArgs e)
        {
            ThrowException();

            btnMain.Text = "Seriously, this is never, ever, gonna get hit!";
        }

        private void ThrowException()
        {
            throw new InvalidOperationException();
        }

        private void BtnMainClick_CanWrapSynchronousVoid_WithTryCatch_NoExceptionLeak(object sender, EventArgs e)
        {
            try
            {
                ThrowException();
            }
            catch (Exception)
            {
                // The exception IS caught here. We good!!!
            }

            btnMain.Text = "No issues. Watch out of the *async* magic transformation!";
        }

        private void BtnMainClick_IfExceptionsAreCaughtInsideAsyncVoid_NoExceptionLeak(object sender, EventArgs e)
        {
            try
            {
                ThrownExceptionCaughtInsideAsyncVoid();
            }
            catch (Exception)
            {
                // The exception IS caught here!
            }

            btnMain.Text = "We are OK";
        }

        private async void ThrownExceptionCaughtInsideAsyncVoid()
        {
            try
            {
                await Task.Run(() => throw new InvalidOperationException());
            }
            catch (Exception)
            {
                //Will get caught
            }
        }

        private async void BtnMainClick_AwaitingReturnedFaultedTask_NoExceptionLeak(object sender, EventArgs e)
        {
            try
            {
                await ReturnTaskThatWillBeFaulted();
            }
            catch (Exception)
            {
                // The exception IS caught here. We good!!!
            }

            btnMain.Text = "We are OK";
        }

        private Task ReturnTaskThatWillBeFaulted()
        {
            return Task.Run(() => throw new InvalidOperationException());
        }

        private async void BtnMainClick_AwaitingAwaitedFaultedTask_NoExceptionLeak(object sender, EventArgs e)
        {
            try
            {
                await AwaitingTaskThatWillBeFaulted();
            }
            catch (Exception)
            {
                // The exception IS caught here. We good!!!
            }

            btnMain.Text = "We are OK";
        }

        private async Task AwaitingTaskThatWillBeFaulted()
        {
            await Task.Run(() => throw new InvalidOperationException());
        }

        private async void BtnMainClick_TwoLevelsOfNestedAsyncAwaitCalls_IsStillAnUnhandledException(object sender, EventArgs e)
        {
            await AwaitingTaskThatWillBeFaulted();
        }

        private void BtnMainClick_SynchronouslyWaitingForFaultedTaskIsStupidBut_NoExceptionLeak(object sender, EventArgs e)
        {
            try
            {
                SyncronouslyWaintingTaskThatWillBeFaulted();
            }
            catch (Exception)
            {
                // The exception IS caught here. We good!!!
            }

            btnMain.Text = "We are OK";
        }

        private void SyncronouslyWaintingTaskThatWillBeFaulted()
        {
            Task.Run(() => throw new InvalidOperationException()).Wait();
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
