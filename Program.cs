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

		private static readonly CancellationTokenSource MainUpdateCancel = new CancellationTokenSource();

		static async Task Main(string[] args)
		{
			Config = Config.Load();
			Monitoring = new Monitoring(Config);
			await MainUpdate();
		}

		private static async Task MainUpdate()
		{
			Console.WriteLine("Meep!");
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
