using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsyncWorkshop.Basics
{
    public partial class ErrorHandling : Form
    {
        public ErrorHandling()
        {
            InitializeComponent();
            //btnMain.Click += BtnMainClick_CannotCatchExceptionsFromAsyncVoid_ResultingInUnhandledException_WithSynchronousThrowing;
            //btnMain.Click += BtnMainClick_CannotCatchExceptionsFromAsyncVoid_ResultingInUnhandledException_WithAsynchronousThrowing;
            //btnMain.Click += BtnMainClick_ThrowingSynchronously_IsStillAnUnhandledException;
            //btnMain.Click += BtnMainClick_CanWrapSynchronousVoid_WithTryCatch_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_IfExceptionsAreCaughtInsideAsyncVoid_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_AwaitingReturnedFaultedTask_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_AwaitingAwaitedFaultedTask_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_TwoLevelsOfNestedAsyncAwaitCalls_IsStillAnUnhandledException;
            //btnMain.Click += BtnMainClick_SynchronouslyWaitingForFaultedTaskIsStupidBut_NoExceptionLeak;
            //btnMain.Click += BtnMainClick_Responsive_ButWillThrow_CrossThreadOnlyInDebugMode;
            //btnMain.Click += BtnMainClick_NullWillNotBeWrappedInATask_InTaskReturningMethodsNotMarkedWithAsync;
            //btnMain.Click += BtnMainClick_NullWillBeWrappedInATask_InTaskReturningMethodsMarkedWithAsync;
        }

        private async void BtnMainClick_CannotAwaitAsyncVoid(object sender, EventArgs e)
        {
            //Uncomment this, but it won't compile
            //await ThrowExceptionAsync();

            btnMain.Text = "Will end up here after Unhandled Exception is thrown!";
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

            btnMain.Text = "Will end up here after Unhandled Exception is thrown!";
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

            btnMain.Text = "Will end up here after Unhandled Exception is thrown!";
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
                SynchronouslyWaitingTaskThatWillBeFaulted();
            }
            catch (Exception)
            {
                // The exception IS caught here. We good!!!
            }

            btnMain.Text = "We are OK";
        }

        private void SynchronouslyWaitingTaskThatWillBeFaulted()
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

            btnMain.Text = "Hi, well this was awkward!";
        }

        private async void BtnMainClick_NullWillNotBeWrappedInATask_InTaskReturningMethodsNotMarkedWithAsync(object sender, EventArgs e)
        {
            //The logic here is that the source of asynchrony is somewhere deep in the call hierarchy.
            //So, in order to optimize the flow, you don't async/await all the way up, which will basically
            //just create useless FSMs (finite state machine), until you reach the last level where the
            //asynchrony source's result is actually used, you just return the task instead of awaiting it
            //in intermediary processing calls. You end up with 2 awaits (at the source and at the top where
            //the result is used) and returns in between

            await IntermediaryTaskProcessingStep_ReturningNull(sender); //This will take down the program

            btnMain.Text = "Never gonna give you up, but you won't get here!";
        }

        private Task<int?> IntermediaryTaskProcessingStep_ReturningNull(object sender)
        {
            if (sender is Button button)
                return null; //This is the stupid part. Since you don't async/await in this intermediary step,
                             //there is no automating wrapping of this null in a Task, which means up above
                             //at the top you're awaiting on a Task that is null => NRE (when you should be
                             //awaiting a Task with a Result of null)

            return CreateTaskAsync();
        }

        private async Task<int?> CreateTaskAsync()
        {
            await Task.Delay(3000);

            return 6;
        }

        private async void BtnMainClick_NullWillBeWrappedInATask_InTaskReturningMethodsMarkedWithAsync(object sender, EventArgs e)
        {
            //The logic here is that the source of asynchrony is somewhere deep in the call hierarchy.
            //So, in order to optimize the flow, you don't async/await all the way up, which will basically
            //just create useless FSMs (finite state machine), until you reach the last level where the
            //asynchrony source's result is actually used, you just return the task instead of awaiting it
            //in intermediary processing calls. You end up with 2 awaits (at the source and at the top where
            //the result is used) and returns in between

            var result = await IntermediaryTaskProcessingStep_ReturningTaskOfNull(sender); //This will take down the program

            btnMain.Text = $@"Result was: {(result == null ? "null" : result.ToString())}";
        }

        private Task<int?> IntermediaryTaskProcessingStep_ReturningTaskOfNull(object sender)
        {
            if (sender is Button button)
                return Task.FromResult<int?>(null); //This is the stupid part. Since you don't async/await in this intermediary step,
            //there is no automating wrapping of this null in a Task, which means up above
            //at the top you're awaiting on a Task that is null => NRE (when you should be
            //awaiting a Task with a Result of null)

            return CreateTaskAsync();
        }
    }
}
