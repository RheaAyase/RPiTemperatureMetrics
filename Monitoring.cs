using System;
using System.Collections.Generic;
using Prometheus;

namespace GrafanaTemp
{
	public class Monitoring: IDisposable
	{
		private readonly MetricPusher Prometheus;

		public readonly Dictionary<string, Gauge> Gauges = new Dictionary<string, Gauge>();

		public Monitoring(Config config)
		{
			foreach( KeyValuePair<string, (string, string)> id in config.DeviceIds )
			{
				if( this.Gauges.ContainsKey(id.Key) )
					continue;
				this.Gauges.Add(id.Key, Metrics.CreateGauge(id.Value.Item1, id.Value.Item2));
			}

			if( this.Prometheus == null )
				this.Prometheus = new MetricPusher(config.PrometheusEndpoint, config.PrometheusJob, "rpi", intervalMilliseconds:(long)(1f / config.TargetFps * 1000));
			this.Prometheus.Start();
		}

		public void Dispose()
		{
			this.Prometheus.Stop();
			((IDisposable)this.Prometheus)?.Dispose();
		}
	}
}
