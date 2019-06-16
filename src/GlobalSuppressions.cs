// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
using System.Diagnostics.CodeAnalysis;

// Need to wait for https://github.com/dotnet/roslyn-analyzers/issues/2578 to be fixed
[assembly: SuppressMessage(
    "Design",
    "CA1062:Validate arguments of public methods",
    Justification = "We're using a helper method that lives in another assembly, which is currently unrecognized.")]

[assembly: SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "General exceptions are logged.")]

[assembly: SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "The assembly will not be localized.")]

[assembly: SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1516:Elements must be separated by blank line",
    Justification = "Rule is not required.")]
