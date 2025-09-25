// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Data;

/// <summary>
/// Marker class to force loading of Pidgeon.Data assembly and its embedded resources.
/// When this class is referenced, it causes the entire assembly to be loaded,
/// making embedded resources available to services.
/// </summary>
public static class DataAssemblyMarker
{
    /// <summary>
    /// Forces the assembly to be loaded by accessing this property.
    /// </summary>
    public static string AssemblyName => typeof(DataAssemblyMarker).Assembly.GetName().Name ?? "Pidgeon.Data";
}