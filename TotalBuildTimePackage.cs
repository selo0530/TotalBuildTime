using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace TotalBuildTime
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
	[Guid(TotalBuildTimePackage.PackageGuidString)]
	public sealed class TotalBuildTimePackage : AsyncPackage
	{
		/// <summary>
		/// TotalBuildTimePackage GUID string.
		/// </summary>
		public const string PackageGuidString = "3f6bdf45-7b4a-465d-a1b1-56d0e1829405";

		private OutputWindowPane m_outputPane;
		private DateTime m_buildStartTime;

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			var dte = (DTE2)GetService(typeof(DTE));
			dte.Events.BuildEvents.OnBuildBegin += OnBuildBegin;
			dte.Events.BuildEvents.OnBuildDone += OnBuildDone;
			OutputWindow outputWindow = dte.ToolWindows.OutputWindow;

			const string BUILD_OUTPUT_PANE_GUID = "{1BD8A850-02D1-11D1-BEE7-00A0C913D1F8}";
			foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes)
			{
				if (pane.Guid.ToUpper() == BUILD_OUTPUT_PANE_GUID)
				{
					m_outputPane = pane;
					break;
				}
			}
		}

		private void OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
		{
			m_buildStartTime = DateTime.Now;
		}

		private void OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			DateTime buildEndTime = DateTime.Now;
			TimeSpan gap = (buildEndTime - m_buildStartTime);

			if (m_outputPane != null)
			{
				m_outputPane.OutputString($"\rTotal build time: {gap.TotalSeconds:F3} seconds\r");
			}
		}

		#endregion
	}
}
