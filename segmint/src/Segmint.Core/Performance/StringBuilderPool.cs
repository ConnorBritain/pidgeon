// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Segmint.Core.Performance;

/// <summary>
/// Thread-safe StringBuilder pool for improved performance in high-throughput scenarios.
/// </summary>
public static class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> Pool = new DefaultObjectPoolProvider()
        .CreateStringBuilderPool(initialCapacity: 512, maximumRetainedCapacity: 4096);

    /// <summary>
    /// Gets a StringBuilder from the pool.
    /// </summary>
    /// <returns>A StringBuilder instance from the pool.</returns>
    public static StringBuilder Get()
    {
        return Pool.Get();
    }

    /// <summary>
    /// Returns a StringBuilder to the pool.
    /// </summary>
    /// <param name="sb">The StringBuilder to return.</param>
    public static void Return(StringBuilder sb)
    {
        if (sb != null)
        {
            Pool.Return(sb);
        }
    }

    /// <summary>
    /// Executes an action with a pooled StringBuilder and automatically returns it.
    /// </summary>
    /// <param name="action">The action to execute with the StringBuilder.</param>
    /// <returns>The resulting string.</returns>
    public static string Execute(Action<StringBuilder> action)
    {
        var sb = Get();
        try
        {
            action(sb);
            return sb.ToString();
        }
        finally
        {
            Return(sb);
        }
    }

    /// <summary>
    /// Executes a function with a pooled StringBuilder and automatically returns it.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="func">The function to execute with the StringBuilder.</param>
    /// <returns>The result of the function.</returns>
    public static T Execute<T>(Func<StringBuilder, T> func)
    {
        var sb = Get();
        try
        {
            return func(sb);
        }
        finally
        {
            Return(sb);
        }
    }
}