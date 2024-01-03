using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Demo
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();
        [DllImport("kernel32.dll")]
        static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);
        [DllImport("winmm")]
        static extern void timeBeginPeriod(int t);
        [DllImport("winmm")]
        static extern void timeEndPeriod(int t);

        static void Main(string[] args)
        {
            //ControlCPUUtilization();
            ControlCPUAsSineCurve();
        }

        /// <summary>
        /// 控制 CPU 使用率曲线为正弦曲线
        /// </summary>
        static void ControlCPUAsSineCurve()
        {
            var numberOfCores = Environment.ProcessorCount; // 获取 CPU 核心数量（逻辑处理器）
            var amplitude = 25; // 正弦波的最大振幅，它表示CPU占用率波动的最大范围。例如，如果幅度是25%，CPU占用率将在基线的上下波动25%。如果基线是50%，那么CPU占用率将在25%（50%-25%）到75%（50%+25%）之间变化。
            var period = 10000; // 完成一个完整正弦波所需的时间，周期用毫秒来表示。周期越长，CPU占用率变化的越慢。在这个例子中，10000毫秒（即10秒）是从正弦波的一个峰值到下一个相同峰值所需的时间。
            var baseline = 50;  // CPU占用率波动的中心点，基线被设置为50%，意味着CPU占用率的波动是围绕50%这个中心值进行的。加上幅度，CPU占用率将在25%到75%之间波动。

            for (var i = 0; i < numberOfCores; i++)
            {
                timeBeginPeriod(1);

                var coreId = i;
                Task.Run(() =>
                {
                    SetThreadAffinityMask(GetCurrentThread(), new IntPtr(1 << coreId)); // 设置线程亲和性

                    while (true)
                    {
                        // 当前时间相对于正弦波周期的位置。DateTime.Now.Ticks获取当前时间的tick数（1 tick = 100纳秒）。将ticks转换为毫秒，并使用模运算符%取周期period的余数，确保结果在一个周期范围内（例如，如果周期是10000毫秒，currentTime的值将在0到9999之间变化）
                        var currentTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % period;
                        // 目标CPU占用率。这里使用了正弦函数Math.Sin来生成周期性变化的占用率。(2 * Math.PI * currentTime) / period计算当前时间点在正弦波中的角度。然后，将正弦值乘以amplitude并加上baseline，得到目标CPU占用率。这个占用率会随着时间在正弦波形状内变化。
                        var targetCpuUsage = baseline + amplitude * Math.Sin((2 * Math.PI * currentTime) / period);
                        // 线程需要保持忙碌状态的时间，以毫秒为单位。这里将目标CPU占用率乘以10来估算忙碌时间。例如，如果目标占用率是50%，忙碌时间将是500毫秒。
                        var busyTime = (int)(targetCpuUsage * 10);
                        // 线程在每秒中需要保持空闲状态的时间，用于调整CPU占用率。在这里，它是通过从1000毫秒（即1秒）中减去busyTime来计算的。这样，忙碌时间和空闲时间加起来总是一秒，确保CPU占用率调整符合目标曲线
                        var idleTime = 1000 - busyTime;

                        var start = Environment.TickCount;
                        while (Environment.TickCount - start < busyTime)
                        {
                        }
                        Thread.Sleep(idleTime); // 休眠
                    }
                });

                timeEndPeriod(1);
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }


        /// <summary>
        /// 控制 CPU 使用率
        /// </summary>
        static void ControlCPUUtilization()
        {
            var numberOfCores = Environment.ProcessorCount; // 获取 CPU 核心数量（逻辑处理器）
            var targetCpuUsage = 50; // 目标 CPU 使用率
            // 线程需要保持忙碌状态的时间，以毫秒为单位。这里将目标CPU占用率乘以10来估算忙碌时间。例如，如果目标占用率是50%，忙碌时间将是500毫秒。
            var busyTime = 10;
            // 线程在每秒中需要保持空闲状态的时间，用于调整CPU占用率。在这里，它是通过从1000毫秒（即1秒）中减去busyTime来计算的。这样，忙碌时间和空闲时间加起来总是一秒，确保CPU占用率调整符合目标曲线
            var idleTime = 10;

            for (int i = 0; i < numberOfCores; i++)
            {
                timeBeginPeriod(1);

                var coreId = i;
                Task.Run(() =>
                {
                    var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", coreId.ToString());
                    SetThreadAffinityMask(GetCurrentThread(), new IntPtr(1 << coreId)); // 设置线程亲和性

                    while (true)
                    {
                        // 调整休眠时间以匹配目标CPU使用率
                        if (cpuUsage.NextValue() > targetCpuUsage)
                        {
                            Thread.Sleep(idleTime);
                        }
                        else
                        {
                            var start = Environment.TickCount;
                            while (Environment.TickCount - start < busyTime)
                            {
                            }
                            Thread.Sleep(idleTime); // 休眠
                        }
                    }
                });

                timeEndPeriod(1);
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
