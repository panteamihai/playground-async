using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsyncWorkshop
{
    public partial class Execution : Form
    {
        public Execution()
        {
            InitializeComponent();
            //btnMain.Click += BtnMainClick_WhatHappensToAsyncVoidDeepInside_SynchronousWork_FakeAwait;
            //btnMain.Click += BtnMainClick_WhatHappensToAsyncVoidDeepInside_SynchronousWork_TCS;
            //btnMain.Click += BtnMainClick_WhatHappensToAsyncVoidDeepInside_AsynchronousWork;
            //btnMain.Click += BtnMainClick_NotAwaitingTasks_WhichOnlyProduceResults;
            //btnMain.Click += BtnMainClick_NotAwaitingTasks_WhichProduceSideEffects;
        }

        private void BtnMainClick_WhatHappensToAsyncVoidDeepInside_SynchronousWork_FakeAwait(object sender, EventArgs e)
        {
            DoSomethingInAsyncVoid_ButIsSynchronousInNature_FakeAwait();
            btnMain.Text = "This is executed only after the async void call has finished synchonously executing.";
        }

        private async void DoSomethingInAsyncVoid_ButIsSynchronousInNature_FakeAwait()
        {
            await SomethingThatReturnsATask_ButIsSynchronousInNature_FakeAwait();
        }

        private async Task SomethingThatReturnsATask_ButIsSynchronousInNature_FakeAwait()
        {
            Thread.Sleep(1000);
            btnMain.Text = "In the task. Still on the UI thread.";
            btnMain.Refresh();
            Thread.Sleep(2000);

            //This is only hear for wrapping the above synchronous code in a Task
            if (false)
                await Task.Delay(500);
        }

        private void BtnMainClick_WhatHappensToAsyncVoidDeepInside_SynchronousWork_TCS(object sender, EventArgs e)
        {
            DoSomethingInAsyncVoid_ButIsSynchronousInNature_TCS();
            btnMain.Text = "This is executed only after the async void call has finished synchonously executing.";
        }

        private async void DoSomethingInAsyncVoid_ButIsSynchronousInNature_TCS()
        {
            await SomethingThatReturnsATask_ButIsSynchronousInNature_TCS();
        }

        private Task SomethingThatReturnsATask_ButIsSynchronousInNature_TCS()
        {
            var tcs = new TaskCompletionSource<object>();

            Thread.Sleep(1000);
            btnMain.Text = "In the task. Still on the UI thread.";
            btnMain.Refresh();
            Thread.Sleep(2000);

            tcs.SetResult(null);

            return tcs.Task;
        }

        private void BtnMainClick_WhatHappensToAsyncVoidDeepInside_AsynchronousWork(object sender, EventArgs e)
        {
            DoSomethingInAsyncVoid_AsynchronousInNature();
            btnMain.Text = "2. This is executed immediately after the async void call, with no wait time.";
        }

        private async void DoSomethingInAsyncVoid_AsynchronousInNature()
        {
            await SomethingThatReturnsATask_AsynchronousInNature();
            btnMain.Text = "4. This is executed from the async continuation long after the handler finished doing its stuff.";
        }

        private async Task SomethingThatReturnsATask_AsynchronousInNature()
        {
            btnMain.Text = "1. This is executed synchronously because it occurs before the asynchrony source"
                           + " (Task.Delay, Task.Run, a TaskCompletionSource running on a new thread / TP thread, async I/O stuff from the framework)";
            btnMain.Refresh();
            Thread.Sleep(3000);

            await Task.Delay(5000);
            btnMain.Text = "3. You won't see this because it gets overwritten immediately";
        }

        private void BtnMainClick_NotAwaitingTasks_WhichOnlyProduceResults(object sender, EventArgs e)
        {
            SpinOff_ATaskWithNoSideEffects_ButDontAwaitIt();
            btnMain.Text = "We've slept enough so that the task produced the result, but we can't access it anyway!";
        }

        private void SpinOff_ATaskWithNoSideEffects_ButDontAwaitIt()
        {
            ComputeAnswerToUniverseLifeAndEverything();
            btnMain.Text = "This is executed immediately after spinning off the task";
            btnMain.Refresh();
            Thread.Sleep(1500);
        }

        private Task<int> ComputeAnswerToUniverseLifeAndEverything()
        {
            return Task.Run(
                () =>
                {
                    Thread.Sleep(1250);
                    return 42;
                });
        }

        private void BtnMainClick_NotAwaitingTasks_WhichProduceSideEffects(object sender, EventArgs e)
        {
            SpinOff_ATaskWithSideEffects_ButDontAwaitIt();
            btnMain.Text = "We've slept enough so that the task produced the result, and it manifested the side effects";
        }

        private void SpinOff_ATaskWithSideEffects_ButDontAwaitIt()
        {
            ShowAnswerToUniverseLifeAndEverything();
            btnMain.Text = "This is executed immediately after spinning off the task";
            btnMain.Refresh();
            Thread.Sleep(2500);
        }

        private Task ShowAnswerToUniverseLifeAndEverything()
        {
            return Task.Run(
                () =>
                {
                    Thread.Sleep(1250);
                    MessageBox.Show("42");
                });
        }
    }
}
