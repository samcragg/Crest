// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("Crest.DataAccess")]
[assembly: InternalsVisibleTo("Crest.Host")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Allow NSubstitute to mock our types
[assembly: InternalsVisibleTo("Host.UnitTests")]
