using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outstance.VsShellContext
{
    public static class Constants
    {
        public enum WindowType
        {
            Unknown,
            SolutionExplorer,
            DocumentEditor,
        }

        public const int DocumentFrame = 1;
        public static readonly Guid SolutionExplorerGuid = new Guid(EnvDTE.Constants.vsWindowKindSolutionExplorer);
    }
}
