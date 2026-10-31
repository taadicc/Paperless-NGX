using HCI_2025_Project_Template.ViewModels;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HCI_2025_Project_Template.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LangLabel.Text = Services.LocalizationService.ShortLabel;

            // ── Glasovne komande ──────────────────────────────────────
            // Mikrofon mijenja izgled dok slusa, da korisnik uvijek zna
            // da li ga aplikacija cuje.
            Services.VoiceCommandService.ListeningChanged += listening =>
            {
                MicIcon.Kind = listening
                    ? PackIconKind.Microphone
                    : PackIconKind.MicrophoneOutline;

                MicDot.Visibility = listening
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };

            // Komanda "search" trazi od prozora da postavi fokus u pretragu
            if (DataContext is MainViewModel vm)
                vm.FocusSearchRequested += () =>
                {
                    SearchBox.Focus();
                    SearchBox.SelectAll();
                };

            // ── Ctrl+F ────────────────────────────────────────────────
            // Ide kroz PreviewKeyDown, a ne kroz InputBindings, jer je
            // postavljanje fokusa stvar prozora a ne ViewModela.
            PreviewKeyDown += (s, args) =>
            {
                if (args.Key == Key.F &&
                    (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    SearchBox.Focus();
                    SearchBox.SelectAll();
                    args.Handled = true;
                }
            };

            // Mikrofon se gasi kad se prozor zatvori
            Closing += (s, args) => Services.VoiceCommandService.Stop();

            Loaded += async (s, e) =>
            {
                if (DataContext is MainViewModel model)
                    await model.LoadDocuments();
            };
        }

        // ── Glas ──────────────────────────────────────────────────────

        private void ToggleVoice_Click(object sender, RoutedEventArgs e)
            => Services.VoiceCommandService.Toggle();

        // ── Desni panel ───────────────────────────────────────────────

        private void SelectedThumb_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedDocument != null)
                vm.OpenDetailWindowCommand.Execute(vm.SelectedDocument.Id);
        }

        // ── Jezik ─────────────────────────────────────────────────────

        private void ToggleLanguage_Click(object sender, RoutedEventArgs e)
        {
            Services.LocalizationService.Toggle();
            LangLabel.Text = Services.LocalizationService.ShortLabel;
        }

        // ── Pretraga ──────────────────────────────────────────────────

        private void SearchBox_GotFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.ShowHistoryCommand.Execute(null);
        }

        private void SearchBox_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Kratka odgoda da klik na stavku iz liste stigne da se izvrsi
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(180)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                if (DataContext is MainViewModel vm)
                    vm.CloseSearchPopupsCommand.Execute(null);
            };
            timer.Start();
        }

        // ── Meni profila ──────────────────────────────────────────────

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
            => ProfilePopup.IsOpen = !ProfilePopup.IsOpen;

        private void ProfileSettings_Click(object sender, RoutedEventArgs e)
        {
            ProfilePopup.IsOpen = false;
            if (DataContext is MainViewModel vm)
                vm.GoToSettingsCommand.Execute(null);
        }

        private void ProfileTheme_Click(object sender, RoutedEventArgs e)
        {
            ProfilePopup.IsOpen = false;
            Services.ThemeService.Toggle();
        }

        private void ProfileLogout_Click(object sender, RoutedEventArgs e)
        {
            ProfilePopup.IsOpen = false;
            Services.VoiceCommandService.Stop();

            var login = new LoginView();
            login.Show();
            Close();
        }
    }
}