﻿namespace PhexensWuerfelraum.Ui.Installer.EmbeddedUI
{
    using Microsoft.Deployment.WindowsInstaller;
    using System;
    using System.Threading;
    using System.Windows;

    /// <summary>
    /// Interaction logic for SetupWizard.xaml
    /// </summary>
    public partial class SetupWizard : Window
    {
        private ManualResetEvent installStartEvent;
        private InstallProgressCounter progressCounter;
        private bool canceled;

        public SetupWizard(ManualResetEvent installStartEvent)
        {
            this.installStartEvent = installStartEvent;
            this.progressCounter = new InstallProgressCounter(0.5);
        }

        public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord,
            MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
        {
            try
            {
                WixSharp.CommonTasks.UACRevealer.Exit();

                this.progressCounter.ProcessMessage(messageType, messageRecord);
                this.progressBar.Value = this.progressBar.Minimum +
                    this.progressCounter.Progress * (this.progressBar.Maximum - this.progressBar.Minimum);

                switch (messageType)
                {
                    case InstallMessage.Error:
                    case InstallMessage.Warning:
                    case InstallMessage.Info:
                        string message = String.Format("{0}: {1}", messageType, messageRecord);
                        this.LogMessage(message);
                        break;
                }

                if (this.canceled)
                {
                    this.canceled = false;
                    return MessageResult.Cancel;
                }
            }
            catch (Exception ex)
            {
                this.LogMessage(ex.ToString());
                this.LogMessage(ex.StackTrace);
            }

            return MessageResult.OK;
        }

        private void LogMessage(string message)
        {
            this.messagesTextBox.Text += Environment.NewLine + message;
            this.messagesTextBox.ScrollToEnd();
        }

        internal void EnableExit()
        {
            this.progressBar.Visibility = Visibility.Hidden;
            this.exitButton.Visibility = Visibility.Visible;
            this.exitAndStartButton.Visibility = Visibility.Visible;
        }

        private void installButton_Click(object sender, RoutedEventArgs e)
        {
            WixSharp.CommonTasks.UACRevealer.Enter();
            this.installButton.Visibility = Visibility.Hidden;
            this.progressBar.Visibility = Visibility.Visible;
            this.installStartEvent.Set();
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void exitAndStartButton_Click(object sender, RoutedEventArgs e)
        {
            string appPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhexensWuerfelraum");
            appPath = System.IO.Path.Combine(appPath, "Client");
            appPath = System.IO.Path.Combine(appPath, "PhexensWuerfelraum.exe");

            System.Diagnostics.Process.Start(appPath);
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.installButton.Visibility == Visibility.Visible)
            {
                this.Close();
            }
            else
            {
                this.canceled = true;
            }
        }
    }
}