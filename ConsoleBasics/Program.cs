using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleBasics
{
    class Program
    {
        const string DownloadLink = "https://go.microsoft.com/fwlink/?linkid=2088631";

        static async Task Main()
        {
            Log("Starting the program");

            //Fire & forget
            TimeIt(() => CpuBound("Files\\pollock.jpeg"));

            await Task.WhenAll(
                TimeIt(() => CpuBound("Files\\smaller-pollock.jpeg")),
                TimeIt(() => IoBound_ForcedAsync()),
                TimeIt(() => IoBound_NaturalAsync()));

            Log("Finished the whole program. What thread did we end up finishing on?");

            Console.Read();
        }

        private static void Log(string message)
        {
            Console.WriteLine(message.Trim() + $", on thread #{Thread.CurrentThread.ManagedThreadId} ({(Thread.CurrentThread.IsThreadPoolThread ? "part of the ThreadPool" : "stand-alone thread")})");
        }

        public static async Task TimeIt(Expression<Func<Task>> action)
        {
            if (action.Body.NodeType != ExpressionType.Call)
                return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var methodName = ((MethodCallExpression)action.Body).Method.Name;

            Log($"Executing {methodName}");

            await action.Compile()();

            Log($"Finished executing {methodName} in {sw.ElapsedMilliseconds} ms");
        }

        public static async Task<T> TimeIt<T>(Expression<Func<Task<T>>> action)
        {
            if (action.Body.NodeType != ExpressionType.Call)
                return default(T);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var methodName = ((MethodCallExpression)action.Body).Method.Name;

            Log($"Executing {methodName}");

            var result = await action.Compile()();

            Log($"Finished executing {methodName} in {sw.ElapsedMilliseconds} ms, returned {result}");

            return result;
        }

        private static Task<int> CpuBound(string bitmapToBeProcessed)
        {
            return Task.Run(() =>
            {
                Log("CPU bound processing");

                int pixels = 0;
                using (var bmp = new Bitmap(bitmapToBeProcessed))
                {
                    for (int i = 0; i < bmp.Width; i++)
                    {
                        for (int j = 0; j < bmp.Height; j++)
                        {
                            var pixel = bmp.GetPixel(i, j);

                            if (pixel == Color.Black)
                            {
                                pixels++;
                            }
                        }
                    }
                }

                return pixels;
            });
        }

        private static async Task IoBound_NaturalAsync()
        {
            using (var wc = new WebClient())
            {
                await wc.DownloadFileTaskAsync(DownloadLink, "net48-offline.exe");
            }
        }

        private static async Task IoBound_ForcedAsync()
        {
            using (var wc = new WebClient())
            {
                Log("Downloading synchronously using a Task");

                await Task.Run(() => wc.DownloadFile(DownloadLink, "net48-offline(1).exe"));
            }
        }
    }
}
