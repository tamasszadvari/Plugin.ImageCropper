using System;
using System.Threading;
using Android.App;
using Android.OS;

namespace Plugin.ImageCropper
{
	public class BackgroundJob
	{
		#region Static helpers
		public static void StartBackgroundJob (MonitoredActivity activity, string title, string message, Action job, Handler handler)
		{
			// Make the progress dialog uncancelable, so that we can gurantee
			// the thread will be done before the activity getting destroyed.
			var dialog = ProgressDialog.Show (activity, title, message, true, false);
			ThreadPool.QueueUserWorkItem (w => new BackgroundJob (activity, job, dialog, handler).Run ());
		}
		#endregion

		private MonitoredActivity _activity;
		private readonly ProgressDialog _progressDialog;
		private readonly Action _job;
		private readonly Handler _handler;

		public BackgroundJob (MonitoredActivity activity, Action job, ProgressDialog progressDialog, Handler handler)
		{
			_activity = activity;
			_progressDialog = progressDialog;
			_job = job;
			_handler = handler;

			_activity.Destroying += (sender, e) => {
				// We get here only when the onDestroyed being called before
				// the cleanupRunner. So, run it now and remove it from the queue
				CleanUp ();
				handler.RemoveCallbacks (CleanUp);
			};

			_activity.Stopping += (sender, e) => progressDialog.Hide ();
			_activity.Starting += (sender, e) => progressDialog.Show ();
		}

		public void Run ()
		{
			try {
				_job ();
			}
			finally
			{
				_handler.Post (CleanUp);
			}
		}

		private void CleanUp ()
		{
			if (_progressDialog.Window != null)
			{
				_progressDialog.Dismiss ();
			}
		}
	}
}
