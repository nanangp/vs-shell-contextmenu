// Guids.cs
// MUST match guids.h
using System;

namespace Outstance.VsShellContext
{
    static class GuidList
    {
        public const string guidVsShellContextPkgString = "985f5dc4-11c5-497e-bd9d-a84a2ddc7816";
        public const string guidVsShellContextCmdSetString = "a799b2b9-9c74-404e-a504-53ed1caf2e61";

        public static readonly Guid guidVsShellContextCmdSet = new Guid(guidVsShellContextCmdSetString);
    };
}