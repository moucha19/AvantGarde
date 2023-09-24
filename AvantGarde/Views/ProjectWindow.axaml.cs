// -----------------------------------------------------------------------------
// PROJECT   : Avant Garde
// COPYRIGHT : Andy Thomas (C) 2022-23
// LICENSE   : GPL-3.0-or-later
// HOMEPAGE  : https://github.com/kuiperzone/AvantGarde
//
// Avant Garde is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later version.
//
// Avant Garde is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along
// with Avant Garde. If not, see <https://www.gnu.org/licenses/>.
// -----------------------------------------------------------------------------

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvantGarde.Projects;
using AvantGarde.Utility;

namespace AvantGarde.Views;

/// <summary>
/// Shows project properties.
/// </summary>
public partial class ProjectWindow : AvantWindow
{
    private readonly DispatcherTimer _timer;
    private DotnetProject? _clone;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ProjectWindow()
    {
        InitializeComponent();

        _timer = new(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, UpdateTimerHandler);
        Closed += WindowClosedHandler;

#if DEBUG
        this.AttachDevTools();
#endif
    }

    /// <summary>
    /// Gets or sets the project. The instance is modified on OK.
    /// </summary>
    public DotnetProject? Project { get; set; }

    private void UpdateAssemblyPathControls(string? newPath = null)
    {
        Debug.WriteLine("Update assembly box");

        if (AssemblyOverrideCheck.IsChecked == true)
        {
            Debug.WriteLine("Override is true");
            BrowseButton.IsEnabled = true;
            AssemblyOverrideBox.IsEnabled = true;

            if (newPath != null)
            {
                AssemblyOverrideBox.Text = newPath;
            }
        }
        else
        {
            Debug.WriteLine("Override is false");
            BrowseButton.IsEnabled = false;
            AssemblyOverrideBox.IsEnabled = false;
            AssemblyOverrideBox.Text = Project?.MakeLocalName(Project?.AssemblyPath?.FullName);
        }

        UpdateWarnings();
    }

    private void UpdateWarnings()
    {
        UpdateProject(_clone);

        if (_clone?.Refresh() == true)
        {
            var e = _clone.Error;
            WarnImage.IsVisible = !string.IsNullOrEmpty(e?.Message);

            WarnBlock1.IsVisible = WarnImage.IsVisible;
            WarnBlock1.Text = e?.Message;

            WarnBlock2.IsVisible = !string.IsNullOrEmpty(e?.Details);
            WarnBlock2.Text = e?.Details;
        }
    }

    private void UpdateProject(DotnetProject? project)
    {
        if (project != null)
        {
            // Do not refresh here
            if (AppCombo.IsEnabled)
            {
                project.Properties.AppProjectName = AppCombo.SelectedItem as string;
            }

            var path = AssemblyOverrideBox.Text;

            if (AssemblyOverrideCheck.IsChecked == true)
            {
                project.Properties.AssemblyOverride = project.MakeLocalName(path);
            }
            else
            {
                project.Properties.AssemblyOverride = null;
            }
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (Project != null)
        {
            Title = "Project - " + Project.ProjectName;

            // Keep internal clone we can make changes to
            _clone = new DotnetProject(Project.FullName, Project.Solution);

            OutputBlock.Text = Project.OutputType + " (" + Project.Solution?.Properties.Build + ")";
            TargetBlock.Text = Project.TargetFramework;
            AvaloniaBlock.Text = Project.AvaloniaVersion;

            if (!Project.IsApp)
            {
                var temp = new List<string>();

                if (Project.Solution != null)
                {
                    foreach (var item in Project.Solution.Projects.Values)
                    {
                        if (item.IsApp)
                        {
                            temp.Add(item.ProjectName);
                        }
                    }
                }

                AppCombo.IsEnabled = true;
                AppCombo.ItemsSource = temp;
                AppCombo.SelectedItem = Project.GetApp()?.ProjectName;
            }
            else
            {
                AppCombo.IsEnabled = false;
                AppCombo.ItemsSource = new string[] { "N/A" };
                AppCombo.SelectedIndex = 0;
            }

            AssemblyOverrideCheck.IsChecked = Project.Properties.AssemblyOverride != null;
            UpdateAssemblyPathControls(Project.Properties.AssemblyOverride);

            _timer.Start();
        }
    }

    private void WindowClosedHandler(object? sender, EventArgs e)
    {
        _timer.Stop();
        _clone = null;
    }

    private void UpdateTimerHandler(object? sender, EventArgs e)
    {
        UpdateWarnings();
    }

    private async void BrowseButtonClickHandler(object? sender, RoutedEventArgs e)
    {
        try
        {
            var opts = new FilePickerOpenOptions();
            opts.Title = $"Project Assembly ({Project?.Solution?.Properties.Build})";
            opts.AllowMultiple = false;

            var type = new FilePickerFileType("Assembly (*.dll)");
            type.Patterns = new string[] { "*.dll" };
            opts.FileTypeFilter = new FilePickerFileType[] { type };

            var paths = await StorageProvider.OpenFilePickerAsync(opts);

            if (paths?.Count > 0)
            {
                Debug.WriteLine("BrowseButtonClickHandler: " + paths[0].Path.AbsolutePath);
                UpdateAssemblyPathControls(paths[0].Path.AbsolutePath);
            }
        }
        catch (Exception x)
        {
            await MessageBox.ShowDialog(this, x);
        }
    }

    private void AssemblyCheckClickHandler(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("AssemblyCheckHandler Checked: " + (AssemblyOverrideCheck.IsChecked == true));
        UpdateAssemblyPathControls();
    }

    private void OkClickHandler(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("OK Click");
        UpdateProject(Project);
        Close(true);
    }

    private void CancelClickHandler(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Cancel Click");
        Close(false);
    }

}