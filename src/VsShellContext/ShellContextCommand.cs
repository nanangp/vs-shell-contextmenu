using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using static Outstance.VsShellContext.Constants;
using Microsoft.VisualStudio;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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


        private readonly IVsMonitorSelection _selectionSvc;
        private readonly EnvDTE.DTE _dteSvc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellContextCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        public ShellContextCommand(
            OleMenuCommandService commandSvc, 
            IVsMonitorSelection selectionSvc,
            EnvDTE.DTE dteSvc)
        {
            _selectionSvc = selectionSvc ?? throw new ArgumentNullException(nameof(selectionSvc));
            _dteSvc = dteSvc ?? throw new ArgumentNullException(nameof(dteSvc));
            commandSvc = commandSvc ?? throw new ArgumentNullException(nameof(commandSvc));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandSvc.AddCommand(menuItem);

        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static void Initialize(Package package)
        {
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            try
            {
                var files = GetSelectedFiles();

                OutputWindow.Log(string.Join(",", files));
                using (var c = new ShellContextMenu())
                {
                    c.ShowContextMenu(files.Select(f => new FileInfo(f)), System.Windows.Forms.Cursor.Position);
                }
            }
            catch (Exception ex)
            {
                OutputWindow.Log($"Error: {ex.Message}\r\n{ex.StackTrace}", true);
            }
        }

        /// <summary>
        /// Check where we are being called from, then return the relevant file.
        /// If an editor window, return the single active doc. 
        /// Otherwise, the solution explorer may have multiple selections.
        /// </summary>
        private IEnumerable<string> GetSelectedFiles()
        {
            var winType = GetActiveWindowType();
            if (winType == WindowType.DocumentEditor)
            {
                var doc = _dteSvc.ActiveDocument;
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
        /// 
        /// </summary>
        private WindowType GetActiveWindowType()
        {
            _selectionSvc.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var element);
            if (!(element is IVsWindowFrame window))
                return WindowType.Unknown;

            // Using VSFPROPID_Type we could quickly identify if it's a Document Frame (1), or a Tool Window (2)
            window.GetProperty((int)__VSFPROPID.VSFPROPID_Type, out var windowFrameType);
            if ((int)windowFrameType == DocumentFrame)
                return WindowType.DocumentEditor;

            // Well it's not the editor. Now check by guid to see what it is.
            window.GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out var windowTypeGuid);

            if (windowTypeGuid.Equals(SolutionExplorerGuid))
                return WindowType.SolutionExplorer;
            if (windowTypeGuid.Equals(VSConstants.VsEditorFactoryGuid.TextEditor_guid))
                return WindowType.DocumentEditor;

            // Log unknown window
            OutputWindow.Log($"Unknown window Guid: {windowTypeGuid}", true);
            return WindowType.Unknown;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> PopulateFileNamesFromSolutionExplorer()
        {
            // TODO: (NP) Better null validation. For now just let them bubble up as exception.
            var uiH = (EnvDTE.UIHierarchy)_dteSvc.Windows?.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer)?.Object;
            var selItems = uiH?.SelectedItems as Array;

            var selHierarchyItems = selItems.Cast<EnvDTE.UIHierarchyItem>();
            foreach (var hierarchyItem in selHierarchyItems)
            {
                // Top-level project. See below for projects in folders.
                if (hierarchyItem.Object is EnvDTE.Project project)
                {
                    yield return project.FullName;
                    continue;
                }

                if (hierarchyItem.Object is EnvDTE.ProjectItem projectItem)
                {
                    // Somehow, projects inside a Solution Folder (i.e. "virtual" folder) gets wrapped inside another project item..
                    if (projectItem.Object is EnvDTE.Project p)
                        yield return p.FullName;
                    else //This is a normal file.
                        yield return projectItem.Properties.Item("FullPath")?.Value?.ToString();
                    continue;
                }
            }
        }
    }
}
