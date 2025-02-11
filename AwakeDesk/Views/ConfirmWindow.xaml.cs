﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace AwakeDesk.Views
{
    public partial class ConfirmWindow : Window, INotifyPropertyChanged
    {

        private bool confirmed = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler WindowClosed;


        public ConfirmWindow()
        {
            InitializeComponent();
            DataContext = this;
            SoftwareVersion.Text = App.AwakeDeskSettings.CurrentVersion;
            
        }

        private void CloseWindow()
        {
            this.DialogResult = confirmed;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Scatena l'evento quando la finestra viene chiusa
            WindowClosed?.Invoke(this, EventArgs.Empty);
        }


        private void ControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        #region UI Handlers
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            confirmed = true;
            CloseWindow();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            confirmed = false;
            CloseWindow();
        }

        #endregion

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }
    }
}