// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segmint.GUI.Services;

/// <summary>
/// Service interface for managing dialogs and user interactions.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an information dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows a warning dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ShowWarningAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <param name="confirmText">Text for confirm button.</param>
    /// <param name="cancelText">Text for cancel button.</param>
    /// <returns>True if confirmed, false if cancelled.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel");

    /// <summary>
    /// Shows a file open dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="filter">File filter string.</param>
    /// <param name="multiSelect">Allow multiple file selection.</param>
    /// <returns>Selected file paths or null if cancelled.</returns>
    Task<string[]?> ShowOpenFileDialogAsync(string title, string filter, bool multiSelect = false);

    /// <summary>
    /// Shows a file save dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="filter">File filter string.</param>
    /// <param name="defaultFileName">Default file name.</param>
    /// <returns>Selected file path or null if cancelled.</returns>
    Task<string?> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName = "");

    /// <summary>
    /// Shows a folder browser dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="selectedPath">Initially selected path.</param>
    /// <returns>Selected folder path or null if cancelled.</returns>
    Task<string?> ShowFolderDialogAsync(string title, string selectedPath = "");

    /// <summary>
    /// Shows an input dialog for text entry.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="prompt">Input prompt text.</param>
    /// <param name="defaultValue">Default input value.</param>
    /// <returns>Entered text or null if cancelled.</returns>
    Task<string?> ShowInputDialogAsync(string title, string prompt, string defaultValue = "");

    /// <summary>
    /// Shows a selection dialog with multiple options.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="prompt">Selection prompt text.</param>
    /// <param name="options">Available options.</param>
    /// <param name="defaultSelection">Default selected option index.</param>
    /// <returns>Selected option or null if cancelled.</returns>
    Task<string?> ShowSelectionDialogAsync(string title, string prompt, IEnumerable<string> options, int defaultSelection = 0);

    /// <summary>
    /// Shows a progress dialog for long-running operations.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="operation">Async operation to execute.</param>
    /// <param name="canCancel">Whether the operation can be cancelled.</param>
    /// <returns>Task representing the operation.</returns>
    Task ShowProgressDialogAsync(string title, Func<IProgress<string>, Task> operation, bool canCancel = false);
}