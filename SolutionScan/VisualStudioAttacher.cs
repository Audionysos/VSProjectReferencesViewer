using System.IO;
using EnvDTE;
using DTEProcess = EnvDTE.Process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Process = System.Diagnostics.Process;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using static System.Console;


namespace SolutionScan {
	#region Classes

	public static class VisualStudioAttacher {
		public static string VSPorces = "devenv";
		public static string VSName = "!VisualStudio";

		#region Public Methods

		[DllImport("User32")]
		private static extern int ShowWindow(int hwnd, int nCmdShow);

		[DllImport("ole32.dll")]
		public static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

		[DllImport("ole32.dll")]
		public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);


		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SetFocus(IntPtr hWnd);


		public static string GetSolutionForVisualStudio(Process visualStudioProcess) {
			_DTE visualStudioInstance;
			if (TryGetVsInstance(visualStudioProcess, out visualStudioInstance)) {
				try {
					return visualStudioInstance.Solution.FullName;
				} catch (Exception) {
				}
			}
			return null;
		}

		public static Process GetAttachedVisualStudio(Process applicationProcess) {
			IEnumerable<Process> visualStudios = GetVisualStudioProcesses();

			foreach (Process visualStudio in visualStudios) {
				_DTE visualStudioInstance;
				if (TryGetVsInstance(visualStudio, out visualStudioInstance)) {
					try {
						foreach (Process debuggedProcess in visualStudioInstance.Debugger.DebuggedProcesses) {
							if (debuggedProcess.Id == applicationProcess.Id) {
								return debuggedProcess;
							}
						}
					} catch (Exception) {
					}
				}
			}
			return null;
		}

		public static void AttachVisualStudioToProcess(Process visualStudioProcess, Process applicationProcess) {
			_DTE visualStudioInstance;

			if (TryGetVsInstance(visualStudioProcess, out visualStudioInstance)) {
				//Find the process you want the VS instance to attach to...
				DTEProcess processToAttachTo = visualStudioInstance.Debugger.LocalProcesses.Cast<DTEProcess>().FirstOrDefault(process => process.ProcessID == applicationProcess.Id);

				//Attach to the process.
				if (processToAttachTo != null) {
					processToAttachTo.Attach();

					ShowWindow((int)visualStudioProcess.MainWindowHandle, 3);
					SetForegroundWindow(visualStudioProcess.MainWindowHandle);
				} else {
					throw new InvalidOperationException("Visual Studio process cannot find specified application '" + applicationProcess.Id + "'");
				}
			}
		}

		public static _DTE getVSForSolutions(List<string> sns) {
			var p = GetVisualStudioForSolutions(sns);
			TryGetVsInstance(p, out var d);
			return d;
		}

		public static Process GetVisualStudioForSolutions(List<string> solutionNames) {
			foreach (string solution in solutionNames) {
				var vsp = getVSProcess(solution);
				if (vsp != null) return vsp;
			}return null;
		}


		public static Process getVSProcess(string solutionName) {
			var ps = GetVisualStudioProcesses();
			if (ps == null) return null;

			foreach (Process p in ps) {
				if (!TryGetVsInstance(p, out var vs)) continue;
				try {
					string vssn = Path.GetFileName(vs.Solution.FullName); //solution oppened in current visual studio
					if (string.Compare(vssn, solutionName, true) == 0)
						return p;
				} catch (Exception) {}
			}
			return null;
		}

		#endregion

		#region Private Methods

		private static IEnumerable<Process> GetVisualStudioProcesses() {
			var ps = Process.GetProcesses()
				.Where(o => o.ProcessName.Contains(VSPorces));
			if(ps.Count() == 0) {
				WriteLine($@"Couldn't find any running process that contians ""{VSPorces}"" in name.");
				ps = null;
			}return ps;
		}

		/// <summary></summary>
		/// <param name="p">Process form which to take visualt studio</param>
		/// <param name="vs">Visual Studio instance to be set.</param>
		/// <returns></returns>
		private static bool TryGetVsInstance(Process p, out _DTE vs) {
			if (p == null) { vs = null; return false; }
			IntPtr numFetched = IntPtr.Zero;
			IMoniker[] monikers = new IMoniker[1];

			GetRunningObjectTable(0, out var rot);
			rot.EnumRunning(out var monikerEnumerator);
			monikerEnumerator.Reset();

			var names = new List<string>(10);
			int roPID = -1; //running object process id
			while (monikerEnumerator.Next(1, monikers, numFetched) == 0) {
				IBindCtx ctx;
				CreateBindCtx(0, out ctx);

				monikers[0].GetDisplayName(ctx, null, out var ron); //running object name
				names.Add(ron);

				rot.GetObject(monikers[0], out var ro);

				if (!(ro is _DTE) || !ron.StartsWith(VSName)) continue;
				roPID = int.Parse(ron.Split(':')[1]); 
				if (roPID != p.Id) continue;
				vs = (_DTE)ro; return true;
			}
			vs = null;

			if(roPID >= 0) {
				WriteLine($@"No object instance named ""{VSName}"" was found on specifed process ""{p}"".");
				WriteLine($@"Last checked object with such name was running on ""{Process.GetProcessById(roPID)}"" process.");
			}else {
				WriteLine($@"Couldn't find Visual Studio instance named ""{VSName}"" running in ""{p}"" process.");
				WriteLine($@"Try setting {nameof(VSName)} variable to be start of one of fallowing names:");
				foreach (var n in names) WriteLine($"	{n}"); 
			}
			return false;
		}

		#endregion
	}

	#endregion
}
