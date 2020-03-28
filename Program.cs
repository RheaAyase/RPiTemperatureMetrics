using System;
using System.Linq.Expressions;
using System.Threading;
using Iot.Device.OneWire;
using System.Threading.Tasks;

namespace GrafanaTemp
{
	static class Program
	{
		private static Monitoring Monitoring;
		private static Config Config;

		private static CancellationTokenSource MainUpdateCancel;
		private static Task MainUpdateTask;

		static void Main(string[] args)
		{
			Config = Config.Load();
			Monitoring = new Monitoring(Config);
			MainUpdateCancel = new CancellationTokenSource();
			MainUpdateTask = Task.Factory.StartNew(MainUpdate, MainUpdateCancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			Console.WriteLine("Meep!");
			MainUpdateTask.Wait();
		}

		private static async Task MainUpdate()
		{
			while( !MainUpdateCancel.IsCancellationRequested )
			{
				DateTime frameTime = DateTime.UtcNow;

				foreach( var dev in OneWireThermometerDevice.EnumerateDevices() )
				{
					if( !Monitoring.Gauges.ContainsKey(dev.DeviceId) )
					{
						Console.WriteLine($"Temperature reported by '{dev.DeviceId}': " + (await dev.ReadTemperatureAsync()).Celsius.ToString("F2") + "\u00B0C");
						continue;
					}

					Monitoring.Gauges[dev.DeviceId].Set((await dev.ReadTemperatureAsync()).Celsius);
				}

				await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, (TimeSpan.FromSeconds(1f / Config.TargetFps) - (DateTime.UtcNow - frameTime)).TotalMilliseconds)));
			}
		}
	}
}
