// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: This class provides services for registering and unregistering
**          a managed server for use by COM.
**
**
**
**
** Change the way how to register and unregister a managed server
**
=============================================================================*/
namespace System.Runtime.InteropServices {
    
    using System;
    using System.IO;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;

  [Flags]
    public enum RegistrationClassContext
    {
    
 
        InProcessServer                 = 0x1, 
        InProcessHandler                = 0x2, 
        LocalServer                     = 0x4, 
        InProcessServer16               = 0x8,
        RemoteServer                    = 0x10,
        InProcessHandler16              = 0x20,
        Reserved1                       = 0x40,
        Reserved2                       = 0x80,
        Reserved3                       = 0x100,
        Reserved4                       = 0x200,
        NoCodeDownload                  = 0x400,
        Reserved5                       = 0x800,
        NoCustomMarshal                 = 0x1000,
        EnableCodeDownload              = 0x2000,
        NoFailureLog                    = 0x4000,
        DisableActivateAsActivator      = 0x8000,
        EnableActivateAsActivator       = 0x10000,
        FromDefaultContext              = 0x20000
    }


    [Flags]
    public enum RegistrationConnectionType
    {
        SingleUse                = 0, 
        MultipleUse              = 1, 
        MultiSeparate            = 2, 
        Suspended                = 4, 
        Surrogate                = 8, 
    }
}
