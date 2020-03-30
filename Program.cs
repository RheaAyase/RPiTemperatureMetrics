using System;
using System.Device.Gpio;
using System.Threading;
using Iot.Device.OneWire;
using System.Threading.Tasks;
using Iot.Device.Tm1637;

namespace RPiTemp
{
	static class Program
	{
		private static Monitoring Monitoring;
		private static Config Config;
		private static Tm1637 Display;

		private static readonly CancellationTokenSource MainUpdateCancel = new CancellationTokenSource();

		static async Task Main(string[] args)
		{
			Config = Config.Load();
			Monitoring = new Monitoring(Config);
			Display = new Tm1637(Config.DisplayClkPin, Config.DisplayDataPin, PinNumberingScheme.Logical);
			Display.Brightness = 7;
			Display.ScreenOn = true;
			Display.ClearDisplay();
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
					double temp = 0;
					try
					{
						temp = (await dev.ReadTemperatureAsync()).Celsius;
					}
					catch( InvalidOperationException )
					{
						continue;
					}

					if( Math.Abs(Math.Round(temp, 3)) < 0.002 )
						continue;

					if( dev.DeviceId == Config.DeviceIdToDisplay )
					{
						SetDisplay(temp);
						await Task.Delay(100);
					}

					if( !Monitoring.Gauges.ContainsKey(dev.DeviceId) )
					{
						Console.WriteLine($"Temperature reported by '{dev.DeviceId}': {temp:F2}\u00B0C");
						await Task.Delay(1000);
						continue;
					}

					Monitoring.Gauges[dev.DeviceId].Set(temp);
					await Task.Delay(1000);
				}

				await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, (TimeSpan.FromSeconds(1f / Config.TargetFps) - (DateTime.UtcNow - frameTime)).TotalMilliseconds)));
			}
		}

		private static void SetDisplay(double temperature)
		{
			Display.ClearDisplay();

			int temp = (int)temperature;
			int digitCount = temp == 0 ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(temp)) + 1);
			if( digitCount < 1 || digitCount > 2 )
				throw new ArgumentException($"Value {temp} is outside of expected range."); //Sanity

			Character[] segments = {Character.Nothing, Character.Nothing, Character.Nothing, Character.Nothing};
			int[] digits = new int[digitCount];
			for( int i = digits.Length - 1; i >= 0; i-- )
			{
				digits[i] = temp % 10;
				temp /= 10;
			}

			if( temp < 0 )
			{
				if( digitCount == 1 )
				{
					segments[0] = Character.Minus;
					segments[1] = (Character)Enum.Parse(typeof(Character), $"Digit{digits[0]}");
				}
				else //2
				{
					segments[1] = Character.Minus;
					for( int i = 2; i < digits.Length; i++ )
						segments[i] = (Character)Enum.Parse(typeof(Character), $"Digit{digits[i-2]}");
				}
			}
			else
			{
				if( digitCount == 1 )
					segments[1] = (Character)Enum.Parse(typeof(Character), $"Digit{digits[0]}");
				else //2
					for( int i = 0; i < digits.Length; i++ )
						segments[i] = (Character)Enum.Parse(typeof(Character), $"Digit{digits[i]}");
			}

			if( temp >= 0 || digitCount != 2 )
			{
				segments[2] = Character.SegmentTop | Character.SegmentTopLeft | Character.SegmentTopRight | Character.SegmentMiddle;
				segments[3] = Character.C;
			}

			Display.Display(segments);
		}
	}
}
