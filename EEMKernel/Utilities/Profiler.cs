//using System;
//using System.Diagnostics;
//using EemRdx.Helpers;

//namespace EemRdx.Utilities
//{
//	/// <inheritdoc cref="Profiler" />
//	/// <summary>
//	/// This code was provided by Digi as a simple profiler
//	/// Usage:
//	///		Wrap code you want to profile in:
//	///			using(new Profiler("somename"))
//	///			{
//	///				// code to profile
//	///			}
//	/// TODO: Update log to EEM specific so it can be isolated from the generic SE Game Log (MyLog.Default.Info)
//	/// </summary>
//	public struct Profiler : IDisposable
//	{
//		private readonly string _name;
//		private readonly long _start;

//		public Profiler(string name = "unnamed")
//		{
//			_name = name;
//			_start = Stopwatch.GetTimestamp();
//		}

//		public void Dispose()
//		{
//			long end = Stopwatch.GetTimestamp();
//			TimeSpan timespan = new TimeSpan(end - _start);
//			if(Constants.EnableProfilingLog)
//				EEMSessionKernel.Static.Log.ProfilingLog?.WriteToLog(_name, $"{timespan.TotalMilliseconds:0.##########}ms");
//		}
//	}
//}
