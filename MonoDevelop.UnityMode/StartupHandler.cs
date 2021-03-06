using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects;
using MonoDevelop.UnityMode.UnityRestClient;
using System.Linq;
using System;
using System.Collections.Generic;

namespace MonoDevelop.UnityMode
{
	public class StartupHandler : CommandHandler
	{
		private RestService restService;

		protected override void Run ()
		{
			Workbench workbench = IdeApp.Workbench;
			WorkbenchWindow workbenchWindow = workbench.RootWindow;

			var assetsFolderPad = workbench.GetPad<AssetsFolderPad>();

			if (assetsFolderPad != null && !assetsFolderPad.Visible)
				assetsFolderPad.Visible = true;

			var solutionPad = workbench.Pads.SolutionPad;

			if (solutionPad != null && solutionPad.Visible)
				solutionPad.Visible = false;

			workbenchWindow.FocusInEvent += WorkbenchFocusInEvent;

			SetupStartupOptions ();

			RestClient.SetServerUrl(StartupOptions.UnityRestServerUrl);

			//Mono.Addins.AddinManager.AddinEngine.Registry.DisableAddin ("MonoDevelop.VersionControl");

			//IdeApp.CommandService.CommandEntrySetPostProcessor += MyPostProcessor;

			//((DefaultWorkbench)IdeApp.Workbench.RootWindow).RecreateMenu ();

			InitializeRestServiceAndPair ();
			IdeApp.Workbench.ShowCommandBar ("UnityDebugging");


			//var dw = (DefaultWorkbench)ww;

			/*
			Gtk.HBox contentBox = dw.Toolbar.ContentBox;
			//contentBox.PackStart(new Gtk.Label ("LUCASLUCAS"), false, false, 0);
			//contentBox.PackStart(new Gtk.Button ("LUCASLUCAS"), false, false, 0);

			var statusAreaAlign = new Gtk.Alignment (100, 0, 1, 1);
			var button = new Gtk.Button ("AISHDASDADAS");
			button.Visible = true;
			statusAreaAlign.Add (button);
			contentBox.PackStart (statusAreaAlign, true, true, 0);
			contentBox.PackStart (button, false, false, 10);
			//MonoDevelop.Debugger.DebuggerService;

			dw.Toolbar.unityDebugButton.Toggled += (sender, e) => {if (true) {
					DebugEditorHandler.Doit();
					}
				};*/
		}


		CommandEntrySet MyPostProcessor(CommandEntrySet input)
		{
			var toRemove = new List<CommandEntry> ();
			foreach(CommandEntry ce in input)
			{
				var theSet = ce as CommandEntrySet;
				if (theSet != null)
					MyPostProcessor (theSet);

				var id = ce.CommandId as string;
				if (isBlackListed (id))
					toRemove.Add (ce);
			}
//			foreach (var r in toRemove)
//				input.Remove (r);

			return input;
		}

		bool isBlackListed(string command)
		{
			switch (command)
			{
			/*			case "MonoDevelop.Ide.Commands.ProjectCommands.CleanSolution":
			case "MonoDevelop.Ide.Commands.ProjectCommands.RebuildSolution":
			case "MonoDevelop.Ide.Commands.ProjectCommands.Rebuild":
			case "MonoDevelop.Ide.Commands.ProjectCommands.Clean":*/
			case "Project":
			case "Build":
			case "Tools":
			case "RecentProjects":
			case "MonoDevelop.Ide.Commands.FileCommands.NewProject":
			case "MonoDevelop.Ide.Commands.FileCommands.NewWorkspace":
			case "MonoDevelop.Ide.Commands.FileCommands.CloseWorkspace":
			case "MonoDevelop.Ide.Commands.ViewCommands.ShowWelcomePage":
			case "MonoDevelop.Ide.Commands.HelpCommands.About":
			case "MonoDevelop.Ide.Updater.UpdateCommands.CheckForUpdates":
				return true;
			default:
				return false;
			}
		}

		void SetupStartupOptions ()
		{
			var args = Environment.GetCommandLineArgs ();

			var p = new Mono.Options.OptionSet ();
			p.Add ("unityProcessId=", "Unity Process Id", (int i) => StartupOptions.UnityProcessId = i);
			p.Add("unityRestServerUrl=", "Unity REST Server URL", s => StartupOptions.UnityRestServerUrl = s);

			LoggingService.Log (MonoDevelop.Core.Logging.LogLevel.Info, "ARGS: " + String.Join("!",args));

			try 
			{
				p.Parse (args);
			} 
			catch(Mono.Options.OptionException e)
			{
				LoggingService.LogInfo("OptionException: " + e.ToString());
			}

			LoggingService.LogInfo("Unity Process ID: " + StartupOptions.UnityProcessId);
			LoggingService.LogInfo("Unity REST Server Url: " + StartupOptions.UnityRestServerUrl);
		}

		void InitializeRestServiceAndPair()
		{
			UnityModeAddin.Initialize ();

			restService = new RestService ( fileOpenRequest => 
			{
				var fileOpenInformation = new FileOpenInformation (fileOpenRequest.File, null, fileOpenRequest.Line, 0, OpenDocumentOptions.BringToFront);

				try
				{
					DispatchService.GuiDispatch(() =>
					{
						if (IdeApp.Workbench.Documents.Any(d => d.FileName == fileOpenInformation.FileName))
						{
							var docs = IdeApp.Workbench.Documents.Where(d => d.FileName == fileOpenInformation.FileName);
							docs.ElementAt(0).Select();
						}
						else
						{
							IdeApp.Workbench.OpenDocument(fileOpenInformation);
							DispatchService.GuiDispatch(IdeApp.Workbench.GrabDesktopFocus);
							
						}
					});
				}
				catch (Exception e)
				{
					LoggingService.LogError(e.ToString());
				}
			}
			);

			DispatchService.BackgroundDispatch(() =>
			{
				LoggingService.LogInfo("Sending Unity Pair request");
				var result = RestClient.Pair(restService.Url, "MonoDevelop " + MonoDevelop.BuildInfo.VersionLabel);
				LoggingService.LogInfo("Unity Pair Request: " + result.Result);
				StartupOptions.UnityProcessId = result.unitypid;
			});

			UnityModeAddin.UpdateUnityProjectState();
		}

		private void WorkbenchFocusInEvent(object o, Gtk.FocusInEventArgs args)
		{
			UnityModeAddin.UpdateUnityProjectState();
		}
	}
}
