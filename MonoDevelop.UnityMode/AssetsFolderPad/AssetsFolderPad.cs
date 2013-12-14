using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;

namespace MonoDevelop.UnityMode
{
	public class AssetsFolderPad : TreeViewPad
	{
		public static AssetsFolderPad Singleton;

		public AssetsFolderPad ()
		{
			Singleton = this;
		}

		public override void Initialize (NodeBuilder[] builders, TreePadOption[] options, string contextMenuPath)
		{
			base.Initialize (builders, options, contextMenuPath);

			IdeApp.Workspace.ItemAddedToSolution += Refresh;
			IdeApp.Workspace.FileAddedToProject += Refresh;
			IdeApp.Workspace.FileRemovedFromProject += Refresh;
			IdeApp.Workspace.FileRenamedInProject += Refresh;
			IdeApp.Workspace.WorkspaceItemOpened += Refresh;
			IdeApp.Workbench.ActiveDocumentChanged += OnWindowChanged;
			Refresh (null, null);
		}

		public class TreeViewBuilder : MonoDevelop.UnityMode.FolderNodeBuilder.IBuilder
		{
			ExtensibleTreeView view;

			public TreeViewBuilder(ExtensibleTreeView view)
			{
				this.view = view;
			}
			public void AddChild (object o)
			{
				view.AddChild (o);
			}
		}

		public void Refresh(object bah, EventArgs args)
		{
			base.TreeView.Clear ();

			MonoDevelop.UnityMode.FolderNodeBuilder.BuildChildNodes2 (new TreeViewBuilder(TreeView), new Folder ("/Users/lucas/monodevelop/monodevelop/main/build/bin/MyDir",null));

			//foreach (ProjectFile file in project.Files) {
			//		MonoDevelop.Core.LoggingService.Log (MonoDevelop.Core.Logging.LogLevel.Info,"File: " + file.FilePath.FileName);
			//		TreeView.AddChild (file);
			//	}
		}

		void OnWindowChanged (object ob, EventArgs args)
		{
			Gtk.Application.Invoke (delegate {
				SelectActiveFile ();
			});
		}

		void SelectActiveFile ()
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.Project == null)
				return;

			string file = doc.FileName;
			if (file == null)
				return;

			ProjectFile pf = doc.Project.Files.GetFile (file);
			if (pf == null)
				return;

			ITreeNavigator nav = treeView.GetNodeAtObject (pf, true);
			if (nav == null)
				return;

			nav.ExpandToNode ();
			nav.Selected = true;
		}
	}
	
}