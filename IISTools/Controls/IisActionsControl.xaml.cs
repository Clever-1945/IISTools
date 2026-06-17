using EnvDTE;
using EnvDTE80;
using IISTools.Dialogs;
using IISTools.Helpers;
using IISTools.Models;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace IISTools.Controls
{
    /// <summary>
    /// Логика взаимодействия для IisActionsControl.xaml
    /// </summary>
    public partial class IisActionsControl : UserControl
    {
        public class IisActionsPingTag
        {
            public Image Image;
            public IisSettingsPingUrlModel SettingsPing;
        }

        private IisInfoHelper _iisInfo = new IisInfoHelper();
        private int _countProcess = 0;
        private DTE2 _dte;

        public IisActionsControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            Render();
        }

        private void Render()
        {
            TryRunTask(() => ApplyStartStop());
            TryRunTask(() => ApplyOpenIISConsole());
            TryRunTask(() => ApplyClose());
            TryRunTask(() => ApplySettings());
            TryRunTask(() => ApplyPing());
            TryRunTask(() => ApplyProcess());
            TryRunTask(() => ApplyRebuild());
        }

        private void ClearElements(Grid grid)
        {
            var elements = grid.Children.Cast<UIElement>().Where(x => x is Button || x is Image).ToArray();
            foreach (var element in elements)
            {
                grid.Children.Remove(element);
            }
        }

        private void TryStartLoadint()
        {
            if (Interlocked.Increment(ref _countProcess) == 1)
            {
                Dispatcher.Invoke(() =>
                {
                    _progressBarLoading.Visibility = Visibility.Visible;
                });
            }
        }

        private void TryStopLoadint()
        {
            if (Interlocked.Decrement(ref _countProcess) < 1)
            {
                Dispatcher.Invoke(() =>
                {
                    _progressBarLoading.Visibility = Visibility.Collapsed;
                });
            }
        }

        private async Task TryRunTask(Func<Task> action)
        {
            TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
            ThreadPool.QueueUserWorkItem(async (s) => 
            {
                try
                {
                    TryStartLoadint();
                    await action();
                }
                finally
                {
                    TryStopLoadint();
                    taskCompletionSource.TrySetResult(0);
                }
            });

            await taskCompletionSource.Task;
        }

        private void CloseParentPopup()
        {
            DependencyObject current = this;

            while (current != null)
            {
                if (current is Popup popup)
                {
                    popup.IsOpen = false;
                    return;
                }

                current = LogicalTreeHelper.GetParent(current);
            }
        }

        private async Task ApplyProcess()
        {
            var listProcess = Assistant.GetIISProcess();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            for (int i = 0; i < listProcess.Length; i++)
            {
                var process = listProcess[i];
                _gridProcessContent.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(30, GridUnitType.Pixel)
                });

                var image = new Image();
                image.Source = ResourceHelper.GetSource("IISTools.Resources.iis.png");

                var button = new Button();
                button.Content = $"{process.Owner} ( {process.Id} )";
                button.Tag = process;
                button.Click += (object sender, RoutedEventArgs e) => 
                {
                    ConnectToProcess(sender as Button);
                    CloseParentPopup();
                };

                _gridProcessContent.Children.Add(image);
                _gridProcessContent.Children.Add(button);

                Grid.SetRow(image, i);
                Grid.SetColumn(image, 0);

                Grid.SetRow(button, i);
                Grid.SetColumn(button, 1);
            }
        }

        private async Task ApplyPing()
        {
            var pingUrls = Assistant.Settings.Value.PingUrls;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            for(int i = 0; i < pingUrls.Length; i++)
            {
                _gridPingContent.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(30, GridUnitType.Pixel)
                });

                var image = new Image();
                image.Source = ResourceHelper.GetSource("IISTools.Resources.ping.png");

                var button = new Button();
                button.Content = pingUrls[i].Name ?? pingUrls[i].Url;
                button.Tag = new IisActionsPingTag() 
                {
                    Image = image,
                    SettingsPing = pingUrls[i]
                };
                button.Click += (object sender, RoutedEventArgs e) => ProcessingPing(sender as Button);

                _gridPingContent.Children.Add(image);
                _gridPingContent.Children.Add(button);

                Grid.SetRow(image, i);
                Grid.SetColumn(image, 0);

                Grid.SetRow(button, i);
                Grid.SetColumn(button, 1);
            }
        }

        private void ProcessingPing(Button button)
        {
            var settings = button?.Tag as IisActionsPingTag;
            if (settings == null)
                return;

            settings.Image.Source = ResourceHelper.GetSource("IISTools.Resources.processing.png");
            button.ToolTip = null;
            ThreadPool.QueueUserWorkItem((s) =>
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadString(settings.SettingsPing.Url);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        settings.Image.Source = ResourceHelper.GetSource("IISTools.Resources.success.png");
                        
                        ClearElements(_gridProcessContent);
                        _gridProcessContent.RowDefinitions.Clear();
                    });

                    TryRunTask(() => ApplyProcess());
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        settings.Image.Source = ResourceHelper.GetSource("IISTools.Resources.error.png");
                        button.ToolTip = $"{ex.Message}\r\n{ex.StackTrace}";
                    });
                }
            });
        }

        private void ConnectToProcess(Button button)
        {
            var processModel = button?.Tag as ProcessModel;
            if (processModel == null)
                return;

            if (_dte == null)
                return;

            Debugger2 debugger = (Debugger2)_dte.Debugger;
            Processes processes = debugger.LocalProcesses;
            var processId = processModel.Id;
            var iisProcess = processes.Cast<Process>().FirstOrDefault(p => p.ProcessID == processId);

            iisProcess?.Attach();
        }

        private async Task ApplySettings()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var image = new Image();
            image.Source = ResourceHelper.GetSource("IISTools.Resources.settings.png");

            var button = new Button();
            button.Content = "Настройки";
            button.Click += (object sender, RoutedEventArgs e) =>
            {
                CloseParentPopup();
                var dialog = new IisSettingsDialog();
                dialog.ShowDialog();
            };

            _gridStaticContent.Children.Add(image);
            _gridStaticContent.Children.Add(button);

            int rowNumber = 6;

            Grid.SetRow(image, rowNumber);
            Grid.SetColumn(image, 0);

            Grid.SetRow(button, rowNumber);
            Grid.SetColumn(button, 1);
        }

        private async Task ApplyClose()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var image = new Image();
            image.Source = ResourceHelper.GetSource("IISTools.Resources.close.png");

            var button = new Button();
            button.Content = "Закрыть панель";
            button.Click += (object sender, RoutedEventArgs e) =>
            {
                CloseParentPopup();
            };

            _gridStaticContent.Children.Add(image);
            _gridStaticContent.Children.Add(button);

            int rowNumber = 7;

            Grid.SetRow(image, rowNumber);
            Grid.SetColumn(image, 0);

            Grid.SetRow(button, rowNumber);
            Grid.SetColumn(button, 1);
        }

        private async Task ApplyOpenIISConsole()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var image = new Image();
            image.Source = ResourceHelper.GetSource("IISTools.Resources.iis.png");

            var button = new Button();
            button.Content = "Запустить консоль";
            button.Click += (object sender, RoutedEventArgs e) => 
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "inetmgr.exe",
                    UseShellExecute = true
                });
                CloseParentPopup();
            };

            _gridStaticContent.Children.Add(image);
            _gridStaticContent.Children.Add(button);

            int rowNumber = 2;

            Grid.SetRow(image, rowNumber);
            Grid.SetColumn(image, 0);

            Grid.SetRow(button, rowNumber);
            Grid.SetColumn(button, 1);
        }

        private async Task<bool> WaitRebuild()
        {
            var buildTaskSource = new TaskCompletionSource<bool>();
            var _buildEvents = _dte.Events.BuildEvents;

            void OnBuildDone(vsBuildScope scope, vsBuildAction action)
            {
                _buildEvents.OnBuildDone -= OnBuildDone;

                bool isSuccess = _dte.Solution.SolutionBuild.LastBuildInfo == 0;
                buildTaskSource.SetResult(isSuccess);
            }

            _buildEvents.OnBuildDone += OnBuildDone;

            return await buildTaskSource.Task;
        }

        private async Task ApplyRebuild()
        {
            var status = await _iisInfo.GetStatus();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var image = new Image();
            image.Source = ResourceHelper.GetSource("IISTools.Resources.build.png");

            var button = new Button();
            _gridStaticContent.Children.Add(image);
            _gridStaticContent.Children.Add(button);

            int rowNumber = 3;

            Grid.SetRow(image, rowNumber);
            Grid.SetColumn(image, 0);

            Grid.SetRow(button, rowNumber);
            Grid.SetColumn(button, 1);

            button.Content = status == IisInfoHelper.IisRunState.RUNNING
                ? "Остановить, собрать, запустить"
                : "Пересобрать";

            button.Click += (object sender, RoutedEventArgs e) => 
            {
                CloseParentPopup();
                ThreadPool.QueueUserWorkItem(async (s) =>
                {
                    if (status == IisInfoHelper.IisRunState.RUNNING)
                    {
                        await _iisInfo.Stop();
                    }

                    _dte.ExecuteCommand("Build.RebuildSolution");
                    // _dte.Solution.SolutionBuild.Build(WaitForBuildToFinish: true);

                    await WaitRebuild();
                    if (status == IisInfoHelper.IisRunState.RUNNING)
                    {
                        await _iisInfo.Start();
                    }
                });
            };
        }

        private async Task ApplyStartStop()
        {            
            var status = await _iisInfo.GetStatus();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var image = new Image();
            var button = new Button();
            _gridStaticContent.Children.Add(image);
            _gridStaticContent.Children.Add(button);

            if (status == IisInfoHelper.IisRunState.RUNNING)
            {
                image.Source = ResourceHelper.GetSource("IISTools.Resources.stop.png");
                button.Content = "Остановить";
                button.ToolTip = "Сейчас сервер запущен";
                button.Click += async (object sender, RoutedEventArgs e) =>
                {
                    await _iisInfo.Stop();
                    CloseParentPopup();
                };
            }
            else if (status == IisInfoHelper.IisRunState.STOPPED)
            {
                image.Source = ResourceHelper.GetSource("IISTools.Resources.start.png");
                button.Content = "Запустить";
                button.ToolTip = "Сейчас сервер остановлен";
                button.Click += async (object sender, RoutedEventArgs e) =>
                {
                    await _iisInfo.Start();
                    CloseParentPopup();
                };
            }
            else
            {
                image.Source = ResourceHelper.GetSource("IISTools.Resources.help.png");
                button.Content = "Статус неизвестен";
            }

            int rowNumber = 1;

            Grid.SetRow(image, rowNumber);
            Grid.SetColumn(image, 0);

            Grid.SetRow(button, rowNumber);
            Grid.SetColumn(button, 1);
        }
    }
}
