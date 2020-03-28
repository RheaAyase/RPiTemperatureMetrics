using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

using guid = System.UInt64;

namespace GrafanaTemp
{
	public class Config
	{
		public const string Filename = "config.json";

		public float TargetFps = 0.03f;
		public string PrometheusEndpoint = "";
		public string PrometheusJob = "";
		/// <summary>
		/// Dictionary of DeviceIDs as key and Prometheus gauge name & description as value.
		/// </summary>
		public Dictionary<string, (string, string)> DeviceIds = new Dictionary<string, (string, string)>();

		private Config(){}
		public static Config Load()
		{
			string path = Filename;

			Config config = new Config();
			config.DeviceIds.Add("deviceId", ("gauge_name", "Description"));
			if( !File.Exists(path) )
			{
				string json = JsonConvert.SerializeObject(config, Formatting.Indented);
				File.WriteAllText(path, json);
				Console.WriteLine("Default config created.");
				Environment.Exit(0);
			}

			config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
			return config;
		}

		public void Save()
		{
			string path = Path.Combine(Filename);
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
		}
	}
}
