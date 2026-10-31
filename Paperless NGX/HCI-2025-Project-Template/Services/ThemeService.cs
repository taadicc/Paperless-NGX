using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace HCI_2025_Project_Template.Services
{
    /// <summary>
    /// Mijenja temu (svijetla/tamna) i akcentnu boju u toku rada.
    /// Radi po istom principu kao LocalizationService: rjecnik sa bojama
    /// se zamjenjuje, a svi stilovi ga citaju preko DynamicResource.
    /// </summary>
    public static class ThemeService
    {
        private const string RegKey = @"SOFTWARE\PaperlessNGX";

        public const string Light = "Light";
        public const string Dark = "Dark";

        public static string CurrentTheme { get; private set; } = Light;
        public static Color CurrentAccent { get; private set; } = Color.FromRgb(0x0D, 0x94, 0x88);

        public static event Action<string>? ThemeChanged;

        public static bool IsDark => CurrentTheme == Dark;

        /// <summary>Ucitava sacuvanu temu i akcent pri pokretanju aplikacije.</summary>
        public static void LoadSaved()
        {
            var theme = Light;
            string? accentHex = null;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegKey);
                theme = (string?)key?.GetValue("Theme", Light) ?? Light;
                accentHex = (string?)key?.GetValue("AccentColor", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[tema] citanje: {ex.Message}");
            }

            Apply(theme, save: false);

            if (!string.IsNullOrWhiteSpace(accentHex))
                ApplyAccent(accentHex, save: false);
        }

        public static void Toggle()
            => Apply(CurrentTheme == Light ? Dark : Light);

        public static void Apply(string theme, bool save = true)
        {
            if (theme != Light && theme != Dark) theme = Light;

            var app = Application.Current;
            if (app == null) return;

            try
            {
                var dict = new ResourceDictionary
                {
                    Source = new Uri(
                        $"pack://application:,,,/Resources/Theme.{theme}.xaml",
                        UriKind.Absolute)
                };

                var merged = app.Resources.MergedDictionaries;

                // Ukloni prethodnu temu - prepoznajemo je po markeru
                var previous = merged.FirstOrDefault(d => d.Contains("ThemeMarker"));
                int index = previous != null ? merged.IndexOf(previous) : -1;

                if (previous != null) merged.Remove(previous);

                // Tema mora doci PRIJE Styles.xaml da bi stilovi vidjeli boje
                if (index >= 0) merged.Insert(index, dict);
                else merged.Add(dict);

                CurrentTheme = theme;

                // MaterialDesign ima svoju paletu koju koriste Card, TextBox,
                // ComboBox i DataGrid. Bez ovoga ta polja ostaju svijetla
                // i u tamnoj temi.
                ApplyMaterialDesignTheme();

                // Korisnicki akcent se ponovo primjenjuje preko nove teme
                ReapplyAccent();

                if (save) Save("Theme", theme);

                ThemeChanged?.Invoke(theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[tema] {theme}: {ex.Message}");
                Notifier.Warning("Tema nije promijenjena",
                    "Odabrana tema trenutno nije dostupna.");
            }
        }

        /// <summary>Postavlja akcentnu boju iz color pickera, npr. "#0D9488".</summary>
        public static void ApplyAccent(string hex, bool save = true)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                CurrentAccent = color;
                ApplyMaterialDesignTheme();
                ReapplyAccent();

                if (save) Save("AccentColor", hex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[akcent] {hex}: {ex.Message}");
                Notifier.Warning("Boja nije primijenjena",
                    "Unesena vrijednost nije ispravna boja.");
            }
        }

        /// <summary>
        /// Prepisuje akcentne cetkice izvedenim nijansama izabrane boje.
        /// Suptilna podloga i ivica se racunaju iz osnovne boje, pa
        /// svaka izabrana boja automatski dobije uskladjenu paletu.
        /// </summary>
        private static void ReapplyAccent()
        {
            var app = Application.Current;
            if (app == null) return;

            var c = CurrentAccent;
            bool dark = IsDark;

            // Gradijenti u ekranima citaju Color.* kljuceve, pa i njih azuriramo
            app.Resources["Color.Accent"] = c;
            app.Resources["Color.AccentDeep"] = Shift(c, 0.84);
            app.Resources["Color.AccentLight"] = Shift(c, 1.28);

            app.Resources["Accent"] = new SolidColorBrush(c);
            app.Resources["Accent.Hover"] = new SolidColorBrush(Shift(c, dark ? 1.14 : 0.86));
            app.Resources["Accent.Subtle"] = new SolidColorBrush(
                dark ? Mix(c, Color.FromRgb(0x0D, 0x15, 0x20), 0.82)
                     : Mix(c, Colors.White, 0.90));
            app.Resources["Accent.Border"] = new SolidColorBrush(
                dark ? Mix(c, Color.FromRgb(0x0D, 0x15, 0x20), 0.55)
                     : Mix(c, Colors.White, 0.60));
            app.Resources["Accent.Text"] = new SolidColorBrush(
                dark ? Shift(c, 1.25) : Shift(c, 0.78));

            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            gradient.GradientStops.Add(new GradientStop(c, 0));
            gradient.GradientStops.Add(new GradientStop(Shift(c, 1.25), 1));
            app.Resources["Accent.Gradient"] = gradient;
        }

        /// <summary>
        /// Uskladjuje MaterialDesign paletu sa nasom temom i akcentom.
        /// Bez ovoga ugradjene kontrole zadrzavaju svoje svijetle boje.
        /// </summary>
        private static void ApplyMaterialDesignTheme()
        {
            try
            {
                var helper = new PaletteHelper();
                var mdTheme = helper.GetTheme();

                mdTheme.SetBaseTheme(IsDark ? BaseTheme.Dark : BaseTheme.Light);
                mdTheme.SetPrimaryColor(CurrentAccent);

                helper.SetTheme(mdTheme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[tema] MaterialDesign: {ex.Message}");
            }
        }

        /// <summary>Posvjetljuje (>1) ili potamnjuje (&lt;1) boju.</summary>
        private static Color Shift(Color c, double factor)
        {
            byte Clamp(double v) => (byte)Math.Max(0, Math.Min(255, v));
            return Color.FromRgb(Clamp(c.R * factor), Clamp(c.G * factor), Clamp(c.B * factor));
        }

        /// <summary>Mijesa boju sa drugom; amount 0 = bez promjene, 1 = potpuno druga.</summary>
        private static Color Mix(Color c, Color other, double amount)
        {
            byte Lerp(byte a, byte b) => (byte)(a + (b - a) * amount);
            return Color.FromRgb(Lerp(c.R, other.R), Lerp(c.G, other.G), Lerp(c.B, other.B));
        }

        private static void Save(string name, string value)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegKey);
                key?.SetValue(name, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[tema] upis: {ex.Message}");
            }
        }
    }
}
