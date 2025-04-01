using System;
using System.Collections.Generic;
using System.Windows;

namespace AccesClientWPF.Views
{
    public partial class ChooseBrowserWindow : Window
    {
        public string SelectedBrowserExe { get; private set; }
        public bool SetAsDefault { get; private set; }
        public bool JustOpenOnce { get; private set; }

        public ChooseBrowserWindow(IEnumerable<string> browserPaths)
        {
            InitializeComponent(); // Généré par WPF lors de la compilation

            // Alimente la ComboBox
            BrowsersCombo.ItemsSource = browserPaths;
            BrowsersCombo.SelectedIndex = 0;

            // Boutons
            OpenOnceButton.Click += (s, e) =>
            {
                SelectedBrowserExe = BrowsersCombo.SelectedItem as string;
                JustOpenOnce = true;
                DialogResult = true;
            };

            SetDefaultButton.Click += (s, e) =>
            {
                SelectedBrowserExe = BrowsersCombo.SelectedItem as string;
                SetAsDefault = true;
                DialogResult = true;
            };
        }
    }
}
