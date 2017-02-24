using System;
using Android.App;

namespace Plugin.ImageCropper
{
	public class MonitoredActivity : Activity
	{
		#region IMonitoredActivity implementation

		public event EventHandler Destroying;
		public event EventHandler Stopping;
		public event EventHandler Starting;

		#endregion

		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			Destroying?.Invoke (this, EventArgs.Empty);
		}

		protected override void OnStop ()
		{
			base.OnStop ();

			Stopping?.Invoke (this, EventArgs.Empty);
		}

		protected override void OnStart ()
		{
			base.OnStart ();

			Starting?.Invoke (this, EventArgs.Empty);
		}
	}
}
