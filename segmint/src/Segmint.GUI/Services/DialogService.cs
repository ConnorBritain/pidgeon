// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Segmint.GUI.Services;

/// <summary>
/// Implementation of dialog service using native Windows dialogs.
/// </summary>
public class DialogService : IDialogService
{
    public Task ShowInfoAsync(string title, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        });
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        });
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string title, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        var result = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialogResult = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            result = dialogResult == MessageBoxResult.Yes;
        });
        return Task.FromResult(result);
    }

    public Task<string[]?> ShowOpenFileDialogAsync(string title, string filter, bool multiSelect = false)
    {
        string[]? result = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                Multiselect = multiSelect
            };

            if (dialog.ShowDialog() == true)
            {
                result = dialog.FileNames;
            }
        });
        return Task.FromResult(result);
    }

    public Task<string?> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName = "")
    {
        string? result = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = defaultFileName
            };

            if (dialog.ShowDialog() == true)
            {
                result = dialog.FileName;
            }
        });
        return Task.FromResult(result);
    }

    public Task<string?> ShowFolderDialogAsync(string title, string selectedPath = "")
    {
        string? result = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            // For now, use a simple input dialog for folder selection
            // In a production app, you'd use a proper folder picker like Windows.Storage.Pickers
            var inputDialog = new InputDialog(title, "Enter folder path:", selectedPath);
            if (inputDialog.ShowDialog() == true)
            {
                result = inputDialog.InputText;
            }
        });
        return Task.FromResult(result);
    }

    public Task<string?> ShowInputDialogAsync(string title, string prompt, string defaultValue = "")
    {
        string? result = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new InputDialog(title, prompt, defaultValue);
            if (dialog.ShowDialog() == true)
            {
                result = dialog.InputText;
            }
        });
        return Task.FromResult(result);
    }

    public Task<string?> ShowSelectionDialogAsync(string title, string prompt, IEnumerable<string> options, int defaultSelection = 0)
    {
        string? result = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new SelectionDialog(title, prompt, options.ToList(), defaultSelection);
            if (dialog.ShowDialog() == true)
            {
                result = dialog.SelectedOption;
            }
        });
        return Task.FromResult(result);
    }

    public async Task ShowProgressDialogAsync(string title, Func<IProgress<string>, Task> operation, bool canCancel = false)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var dialog = new ProgressDialog(title, canCancel);
            var progress = new Progress<string>(message => dialog.UpdateProgress(message));
            
            dialog.Show();
            
            try
            {
                await operation(progress);
            }
            finally
            {
                dialog.Close();
            }
        });
    }
}

/// <summary>
/// Simple input dialog for text entry.
/// </summary>
public partial class InputDialog : Window
{
    public string InputText { get; private set; } = "";

    public InputDialog(string title, string prompt, string defaultValue)
    {
        Title = title;
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var promptLabel = new System.Windows.Controls.Label { Content = prompt, Margin = new Thickness(10) };
        var inputTextBox = new System.Windows.Controls.TextBox { Text = defaultValue, Margin = new Thickness(10) };
        
        var buttonPanel = new System.Windows.Controls.StackPanel 
        { 
            Orientation = System.Windows.Controls.Orientation.Horizontal, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10)
        };

        var okButton = new System.Windows.Controls.Button { Content = "OK", Width = 75, Margin = new Thickness(5, 0, 0, 0) };
        var cancelButton = new System.Windows.Controls.Button { Content = "Cancel", Width = 75, Margin = new Thickness(5, 0, 0, 0) };

        okButton.Click += (s, e) => { InputText = inputTextBox.Text; DialogResult = true; };
        cancelButton.Click += (s, e) => { DialogResult = false; };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);

        System.Windows.Controls.Grid.SetRow(promptLabel, 0);
        System.Windows.Controls.Grid.SetRow(inputTextBox, 1);
        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

        grid.Children.Add(promptLabel);
        grid.Children.Add(inputTextBox);
        grid.Children.Add(buttonPanel);

        Content = grid;
        inputTextBox.Focus();
        inputTextBox.SelectAll();
    }
}

/// <summary>
/// Simple selection dialog for choosing from options.
/// </summary>
public partial class SelectionDialog : Window
{
    public string? SelectedOption { get; private set; }

    public SelectionDialog(string title, string prompt, List<string> options, int defaultSelection)
    {
        Title = title;
        Width = 400;
        Height = 300;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var promptLabel = new System.Windows.Controls.Label { Content = prompt, Margin = new Thickness(10) };
        var listBox = new System.Windows.Controls.ListBox { Margin = new Thickness(10) };
        
        foreach (var option in options)
        {
            listBox.Items.Add(option);
        }
        
        if (defaultSelection >= 0 && defaultSelection < options.Count)
        {
            listBox.SelectedIndex = defaultSelection;
        }

        var buttonPanel = new System.Windows.Controls.StackPanel 
        { 
            Orientation = System.Windows.Controls.Orientation.Horizontal, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10)
        };

        var okButton = new System.Windows.Controls.Button { Content = "OK", Width = 75, Margin = new Thickness(5, 0, 0, 0) };
        var cancelButton = new System.Windows.Controls.Button { Content = "Cancel", Width = 75, Margin = new Thickness(5, 0, 0, 0) };

        okButton.Click += (s, e) => { SelectedOption = listBox.SelectedItem?.ToString(); DialogResult = true; };
        cancelButton.Click += (s, e) => { DialogResult = false; };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);

        System.Windows.Controls.Grid.SetRow(promptLabel, 0);
        System.Windows.Controls.Grid.SetRow(listBox, 1);
        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

        grid.Children.Add(promptLabel);
        grid.Children.Add(listBox);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }
}

/// <summary>
/// Simple progress dialog for long-running operations.
/// </summary>
public partial class ProgressDialog : Window
{
    private readonly System.Windows.Controls.Label _statusLabel;

    public ProgressDialog(string title, bool canCancel)
    {
        Title = title;
        Width = 400;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        _statusLabel = new System.Windows.Controls.Label { Content = "Processing...", Margin = new Thickness(10) };
        var progressBar = new System.Windows.Controls.ProgressBar { IsIndeterminate = true, Height = 20, Margin = new Thickness(10) };

        System.Windows.Controls.Grid.SetRow(_statusLabel, 0);
        System.Windows.Controls.Grid.SetRow(progressBar, 1);

        grid.Children.Add(_statusLabel);
        grid.Children.Add(progressBar);

        if (canCancel)
        {
            var cancelButton = new System.Windows.Controls.Button { Content = "Cancel", Width = 75, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(10) };
            cancelButton.Click += (s, e) => Close();
            
            System.Windows.Controls.Grid.SetRow(cancelButton, 2);
            grid.Children.Add(cancelButton);
        }

        Content = grid;
    }

    public void UpdateProgress(string message)
    {
        Dispatcher.Invoke(() => _statusLabel.Content = message);
    }
}