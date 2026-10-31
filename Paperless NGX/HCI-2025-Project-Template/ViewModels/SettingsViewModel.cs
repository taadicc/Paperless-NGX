using HCI_2025_Project_Template.Helpers;
using System;
using HCI_2025_Project_Template.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace HCI_2025_Project_Template.ViewModels
{
    /// <summary>Jedan predloženi akcent u paleti.</summary>
    public class AccentOption
    {
        public string Hex { get; }
        public string Name { get; }
        public Brush Swatch { get; }

        public AccentOption(string hex, string name)
        {
            Hex = hex;
            Name = name;
            Swatch = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
    }

    public class SettingsViewModel : ObservableObject
    {
        private bool _isDarkTheme;
        private string _accentHex = "#0D9488";
        private string _selectedLanguage = LocalizationService.Serbian;

        // ── Tema ──────────────────────────────────────────────────────

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme == value) return;
                _isDarkTheme = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThemeLabel));
                OnPropertyChanged(nameof(ThemeIcon));

                ThemeService.Apply(value ? ThemeService.Dark : ThemeService.Light);
            }
        }

        public string ThemeLabel => LocalizationService.Text(
            _isDarkTheme ? "L_DarkTheme" : "L_LightTheme",
            _isDarkTheme ? "Tamna tema" : "Svijetla tema");

        public string ThemeIcon => _isDarkTheme ? "WeatherNight" : "WeatherSunny";

        // ── Akcentna boja ─────────────────────────────────────────────

        public ObservableCollection<AccentOption> AccentOptions { get; } = new()
        {
            new AccentOption("#0D9488", "Tirkizna"),
            new AccentOption("#0EA5E9", "Nebo"),
            new AccentOption("#6366F1", "Indigo"),
            new AccentOption("#8B5CF6", "Ljubičasta"),
            new AccentOption("#EC4899", "Magenta"),
            new AccentOption("#F43F5E", "Malina"),
            new AccentOption("#F97316", "Narandžasta"),
            new AccentOption("#EAB308", "Zlatna"),
            new AccentOption("#22C55E", "Zelena"),
            new AccentOption("#64748B", "Grafit"),
        };

        public string AccentHex
        {
            get => _accentHex;
            set
            {
                if (_accentHex == value) return;
                _accentHex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AccentPreview));

                if (IsValidHex(value))
                    ThemeService.ApplyAccent(value);
            }
        }

        public Brush AccentPreview
        {
            get
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(_accentHex)); }
                catch { return Brushes.Gray; }
            }
        }

        public RelayCommand SelectAccentCommand { get; }

        // ── Jezik ─────────────────────────────────────────────────────

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value) return;
                _selectedLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSerbian));
                OnPropertyChanged(nameof(IsEnglish));

                LocalizationService.Apply(value);
                OnPropertyChanged(nameof(ThemeLabel));
            }
        }

        public bool IsSerbian
        {
            get => _selectedLanguage == LocalizationService.Serbian;
            set { if (value) SelectedLanguage = LocalizationService.Serbian; }
        }

        public bool IsEnglish
        {
            get => _selectedLanguage == LocalizationService.English;
            set { if (value) SelectedLanguage = LocalizationService.English; }
        }

        // ── Reset ─────────────────────────────────────────────────────

        public RelayCommand ResetCommand { get; }

        public SettingsViewModel()
        {
            // Pocetno stanje citamo iz servisa, da se prekidaci
            // slazu sa onim sto je stvarno primijenjeno
            _isDarkTheme = ThemeService.IsDark;
            _accentHex = ToHex(ThemeService.CurrentAccent);
            _selectedLanguage = LocalizationService.CurrentLanguage;

            SelectAccentCommand = new RelayCommand(param =>
            {
                if (param is string hex) AccentHex = hex;
            });

            ResetCommand = new RelayCommand(_ =>
            {
                IsDarkTheme = false;
                AccentHex = "#0D9488";
                SelectedLanguage = LocalizationService.Serbian;

                Notifier.Success("Podešavanja vraćena",
                    "Tema, boja i jezik su vraćeni na početne vrijednosti.");
            });

            // Ako se tema promijeni sa nekog drugog mjesta, prekidac to prati
            ThemeService.ThemeChanged += _ =>
            {
                _isDarkTheme = ThemeService.IsDark;
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeLabel));
                OnPropertyChanged(nameof(ThemeIcon));
            };

            LocalizationService.LanguageChanged += code =>
            {
                _selectedLanguage = code;
                OnPropertyChanged(nameof(SelectedLanguage));
                OnPropertyChanged(nameof(IsSerbian));
                OnPropertyChanged(nameof(IsEnglish));
                OnPropertyChanged(nameof(ThemeLabel));
            };
        }

        // ── Pomocno ───────────────────────────────────────────────────

        private static bool IsValidHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (!value.StartsWith("#")) return false;
            if (value.Length != 7 && value.Length != 9) return false;

            for (int i = 1; i < value.Length; i++)
                if (!Uri.IsHexDigit(value[i])) return false;

            return true;
        }

        private static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
