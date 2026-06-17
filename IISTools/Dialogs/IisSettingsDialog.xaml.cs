using IISTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IISTools.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для IisSettingsDialog.xaml
    /// </summary>
    public partial class IisSettingsDialog : Window
    {
        private IisSettingsModel _settings;

        public IisSettingsDialog()
        {
            InitializeComponent();
            _settings = new IisSettingsModel();
            _settings.Load();
            _dataGridPings.ItemsSource = _settings.PingUrls;
        }

        private void OnAddPing(object sender, RoutedEventArgs e)
        {
            var pingUrls = _settings.PingUrls.ToList();
            pingUrls.Add(new IisSettingsPingUrlModel()
            {
                Id = Guid.NewGuid()
            });
            _settings.PingUrls = pingUrls.ToArray();
            _dataGridPings.ItemsSource = _settings.PingUrls;
        }

        private void OnDeletePing(object sender, RoutedEventArgs e)
        {
            var ping = _dataGridPings.SelectedValue as IisSettingsPingUrlModel;
            if (ping != null)
            {
                _settings.PingUrls = _settings.PingUrls.Where(x => x != ping).ToArray();
                _dataGridPings.ItemsSource = _settings.PingUrls;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            _settings.Save();
            Assistant.Settings.Value.Load();
            this.Close();
        }

        private void OnSelectedPing(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
