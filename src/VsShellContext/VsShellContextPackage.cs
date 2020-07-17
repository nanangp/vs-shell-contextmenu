using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Outstance.VsShellContext
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidVsShellContextPkgString)]
    public sealed class VsShellContextPackage : Package
    {
        private DTE _dte;
        private IVsMonitorSelection _monitorSelection;
        private readonly Guid SolutionExplorerGuid = new Guid(EnvDTE.Constants.vsWindowKindSolutionExplorer);
        private const int DocumentFrame = 1;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VsShellContextPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this));
            base.Initialize();

            _dte = GetService(typeof(SDTE)) as DTE;
            _monitorSelection = (IVsMonitorSelection)GetService(typeof(SVsShellMonitorSelection));
             
            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidVsShellContextCmdSet, (int)PkgCmdIDList.cmdShellContextMenu);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID );
                mcs.AddCommand( menuItem );
            }
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            WindowType winType = GetActiveWindowType();
            if (winType == WindowType.Unknown)
                return;

            try
            {
                IList<string> filenames = null;

                if (winType == WindowType.DocumentEditor)
                {
                    var doc = _dte.ActiveDocument;
                    if (doc == null)
                        return;
                    filenames = new[] {doc.FullName};
                }
                else if (winType == WindowType.SolutionExplorer)
                {
                    filenames = PopulateFileNamesFromSolutionExplorer();
                }

                if (filenames == null)
                    return;
                    
                using (var c = new ShellContextMenu())
                {
                    var fileInfos = filenames.Select(f => new FileInfo(f)).ToArray();
                    c.ShowContextMenu(fileInfos, System.Windows.Forms.Cursor.Position);
                }
            }
            catch (Exception ex)
            {
                MessageBox("Error",
                    string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace),
                    OLEMSGICON.OLEMSGICON_CRITICAL);
            }

        }

        private IList<string> PopulateFileNamesFromSolutionExplorer()
        {
            // TODO: (NP) Better null validation. For now just let them bubble up as exception.
            var uiH = (UIHierarchy)_dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;
            var selItems = uiH.SelectedItems as Array;

            var result = new List<string>();

            var selHierarchyItems = selItems.Cast<UIHierarchyItem>();
            foreach (var hierarchyItem in selHierarchyItems)
            {
                // Top-level project. See below for projects in folders.
                var project = hierarchyItem.Object as Project;
                if (project != null)
                {
                    result.Add(project.FullName);
                    continue;
                }

                var projectItem = hierarchyItem.Object as ProjectItem;
                if (projectItem != null)
                {
                    // Somehow, projects inside a Solution Folder (i.e. "virtual" folder) gets wrapped inside another project item..
                    if (projectItem.Object is Project)
                        result.Add(((Project)projectItem.Object).FullName);
                    else //This is a normal file.
                        result.Add(projectItem.Properties.Item("FullPath").Value.ToString());
                }
            }
            return result;
        }

        private WindowType GetActiveWindowType()
        {
            object element;
            _monitorSelection.GetCurrentElementValue((uint)Microsoft.VisualStudio.VSConstants.VSSELELEMID.SEID_WindowFrame, out element);
            if (element == null)
                return WindowType.Unknown;

            var window = element as IVsWindowFrame;

            // Using VSFPROPID_Type we could quickly identify if it's a Document Frame (1), or a Tool Window (2)
            object windowFrameType;
            window.GetProperty((int)__VSFPROPID.VSFPROPID_Type, out windowFrameType);
            if ((int)windowFrameType == DocumentFrame)
                return WindowType.DocumentEditor;

            // Well it's not the editor. Now check by guid to see what it is.
            Guid windowTypeGuid;
            window.GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out windowTypeGuid);

            if (windowTypeGuid.Equals(SolutionExplorerGuid))
                return WindowType.SolutionExplorer;
            if (windowTypeGuid.Equals(VSConstants.VsEditorFactoryGuid.TextEditor_guid))
                return WindowType.DocumentEditor;

            // Log unknown window
            OutputWindow.Log("Current window Guid: " + windowTypeGuid);
            return WindowType.Unknown;
        }

        private void MessageBox(string title, string message, OLEMSGICON icon = OLEMSGICON.OLEMSGICON_NOICON)
        {
            // Show a Message Box
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       title,
                       message,
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       icon,
                       0,        // false
                       out result));
        }

    }
    public enum WindowType
    {
        Unknown,
        SolutionExplorer,
        DocumentEditor,
    }
}
