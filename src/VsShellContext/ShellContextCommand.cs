using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Threading;
using static Outstance.VsShellContext.Constants;

namespace Outstance.VsShellContext
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ShellContextCommand
    {
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a799b2b9-9c74-404e-a504-53ed1caf2e61");

        private readonly IVsMonitorSelection selectionSvc;
        private readonly EnvDTE80.DTE2 dteSvc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellContextCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        private ShellContextCommand(
            OleMenuCommandService commandSvc,
            IVsMonitorSelection selectionSvc,
            EnvDTE80.DTE2 dteSvc)
        {
            try
            {
                this.selectionSvc = selectionSvc ?? throw new ArgumentNullException(nameof(IVsMonitorSelection));
                this.dteSvc = dteSvc ?? throw new ArgumentNullException(nameof(DTE));
                commandSvc = commandSvc ?? throw new ArgumentNullException(nameof(IMenuCommandService));

                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Execute, menuCommandID);

                //if (commandSvc.FindCommand(menuCommandID) is null)
                commandSvc.AddCommand(menuItem);
            }
            catch(Exception ex)
            {
                OutputWindow.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static async Task InitializeAsync(Microsoft.VisualStudio.Shell.IAsyncServiceProvider provider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dteSvc = await provider.GetServiceAsync(typeof(SDTE)) as EnvDTE80.DTE2;
            var commandSvc = await provider.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var selectionSvc = await provider.GetServiceAsync(typeof(IVsMonitorSelection)) as IVsMonitorSelection;

            new ShellContextCommand(commandSvc, selectionSvc, dteSvc);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                var files = GetSelectedFiles().ToList();
#if DEBUG
                OutputWindow.Log("Selected:");
                OutputWindow.Log(string.Join(Environment.NewLine, files));
#endif

                using (var c = new ShellContextMenu())
                {
                    var selectedItems = files.Select(f =>
                        Directory.Exists(f)
                        ? new DirectoryInfo(f)
                        : new FileInfo(f) as FileSystemInfo);
                    c.ShowContextMenu(selectedItems, System.Windows.Forms.Cursor.Position);
                }
            }
            catch (Exception ex)
            {
                OutputWindow.LogException(ex);
            }
        }

        /// <summary>
        /// Check where we are being called from, then return the relevant file(s).
        /// If an editor window, return the single active doc. 
        /// Otherwise, the solution explorer may have multiple selections.
        /// </summary>
        private IEnumerable<string> GetSelectedFiles()
        {
            var winType = GetActiveWindowType();
            if (winType == WindowType.DocumentEditor)
            {
                var doc = this.dteSvc.ActiveDocument;
                if (doc != null)
                    yield return doc.FullName;
            }
            else if (winType == WindowType.SolutionExplorer)
            {
                foreach (var f in PopulateFileNamesFromSolutionExplorer())
                    yield return f;
            }
        }

        /// <summary>
        /// Attempt to figure out where the action is triggered from. 
        /// So far the only possible trigger points are the Solution Explorer,
        /// or from the source code editor.
        /// </summary>
        private WindowType GetActiveWindowType()
        {
            _ = this.selectionSvc.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var element);
            if (!(element is IVsWindowFrame window))
                return WindowType.Unknown;

            // Using VSFPROPID_Type we could quickly identify if it's a Document Frame (1), or a Tool Window (2)
            _ = window.GetProperty((int)__VSFPROPID.VSFPROPID_Type, out var windowFrameType);
            if ((int)windowFrameType == DocumentFrame)
                return WindowType.DocumentEditor;

            // Well it's not the editor. Now check by guid to see what it is.
            _ = window.GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out var windowTypeGuid);

            if (windowTypeGuid.Equals(SolutionExplorerGuid))
                return WindowType.SolutionExplorer;
            if (windowTypeGuid.Equals(VSConstants.VsEditorFactoryGuid.TextEditor_guid))
                return WindowType.DocumentEditor;

            // Log unknown window
            OutputWindow.Log($"Unknown window Guid: {windowTypeGuid}", true);
            return WindowType.Unknown;
        }

        /// <summary>
        /// Depending on where the item lives in the Solution Explorer, they may come 
        /// in as different types. This methods attempt to decode that into file names
        /// and their full paths.
        /// </summary>
        private IEnumerable<string> PopulateFileNamesFromSolutionExplorer()
        {
            var uiH = (UIHierarchy)this.dteSvc.Windows?.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer)?.Object;
            var selItems = uiH?.SelectedItems as Array;

            foreach (var hierarchyItem in selItems.Cast<UIHierarchyItem>())
            {
                // Top-level project. See below for projects in folders.
                if (hierarchyItem.Object is Project project)
                {
                    yield return project.FullName;
                }
                else if (hierarchyItem.Object is ProjectItem projectItem)
                {
                    if (projectItem.Object is Project innerProject)
                    {
                        // Projects inside a Solution Folder (i.e. "virtual" folder) 
                        // sometimes get wrapped inside another project item. 
                        // At least they were in VS2015, but not reproducible in 2019.
                        yield return innerProject.FullName;
                    }
                    else
                    {
                        // Oh look, a normal file! (or something else completely unknown, 
                        // but hopefully it would throw the appropriate exception)
                        yield return projectItem.FileNames[1];
                    }
                }
            }
        }
    }
}
