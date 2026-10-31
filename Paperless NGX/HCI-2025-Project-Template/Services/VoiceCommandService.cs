using System;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition;
using System.Windows;

namespace HCI_2025_Project_Template.Services
{
    /// <summary>
    /// Glasovne komande preko prepoznavanja govora ugradjenog u Windows.
    /// Ne koristi internet ni vanjski servis: sve se obradjuje lokalno.
    ///
    /// Namjerno se ne prepoznaje slobodan govor, nego samo unaprijed zadata
    /// lista komandi. Time je prepoznavanje pouzdano i ne moze se desiti da
    /// aplikacija izvrsi nesto sto korisnik nije rekao.
    /// </summary>
    public static class VoiceCommandService
    {
        private static SpeechRecognitionEngine? _engine;

        /// <summary>
        /// Prag pouzdanosti. Desktop prepoznavac za pojedinacne rijeci
        /// cesto vraca 0.3-0.6, pa bi visok prag odbacivao ispravne komande.
        /// </summary>
        private const float MinConfidence = 0.30f;

        public static bool IsListening { get; private set; }
        public static bool IsAvailable { get; private set; }

        /// <summary>Prepoznata komanda: "upload", "search" ili "refresh".</summary>
        public static event Action<string>? CommandRecognized;

        /// <summary>Javlja se pri paljenju/gasenju, da se dugme osvjezi.</summary>
        public static event Action<bool>? ListeningChanged;

        public static readonly string[] Commands = { "upload", "search", "refresh" };

        /// <summary>
        /// Priprema prepoznavanje. Vraca false ako racunar nema
        /// instaliran engleski jezicki paket ili nema mikrofon.
        /// </summary>
        public static bool Initialize()
        {
            if (_engine != null) return IsAvailable;

            try
            {
                var installed = SpeechRecognitionEngine.InstalledRecognizers();

                System.Diagnostics.Debug.WriteLine(
                    $"[glas] instalirani prepoznavaci: {installed.Count}");
                foreach (var r in installed)
                    System.Diagnostics.Debug.WriteLine($"[glas]   {r.Id} / {r.Culture.Name}");

                // Prepoznavanje trazi instaliran jezicki paket. Na srpskom
                // ga prakticno nema, pa trazimo bilo koji engleski.
                var recognizer = installed
                    .FirstOrDefault(r => r.Culture.TwoLetterISOLanguageName == "en")
                    ?? installed.FirstOrDefault();

                if (recognizer == null)
                {
                    System.Diagnostics.Debug.WriteLine("[glas] nema nijednog prepoznavaca");
                    IsAvailable = false;
                    return false;
                }

                _engine = new SpeechRecognitionEngine(recognizer);

                var builder = new GrammarBuilder(new Choices(Commands))
                {
                    Culture = recognizer.Culture
                };
                _engine.LoadGrammar(new Grammar(builder));

                _engine.SpeechRecognized += OnRecognized;
                _engine.SpeechRecognitionRejected += OnRejected;
                _engine.SpeechDetected += (_, _) =>
                    System.Diagnostics.Debug.WriteLine("[glas] zvuk otkriven");
                _engine.AudioStateChanged += (_, e) =>
                    System.Diagnostics.Debug.WriteLine($"[glas] audio: {e.AudioState}");

                _engine.SetInputToDefaultAudioDevice();

                IsAvailable = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[glas] priprema: {ex}");
                IsAvailable = false;
                _engine = null;
                return false;
            }
        }

        public static void Toggle()
        {
            if (IsListening) Stop();
            else Start();
        }

        public static void Start()
        {
            if (!Initialize())
            {
                Notifier.Warning("Glasovne komande nisu dostupne",
                    "Računar nema instaliran engleski paket za prepoznavanje govora.");
                return;
            }

            try
            {
                _engine!.RecognizeAsync(RecognizeMode.Multiple);
                IsListening = true;
                ListeningChanged?.Invoke(true);

                Notifier.Info("Slušam",
                    "Recite: upload, search ili refresh.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[glas] start: {ex.Message}");
                Notifier.Error("Mikrofon nije dostupan",
                    "Druga aplikacija možda koristi mikrofon.");
                IsListening = false;
                ListeningChanged?.Invoke(false);
            }
        }

        public static void Stop()
        {
            try
            {
                _engine?.RecognizeAsyncCancel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[glas] stop: {ex.Message}");
            }
            finally
            {
                IsListening = false;
                ListeningChanged?.Invoke(false);
            }
        }

        private static void OnRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            var text = e.Result.Text.ToLowerInvariant().Trim();
            System.Diagnostics.Debug.WriteLine(
                $"[glas] prepoznato: \"{text}\" ({e.Result.Confidence:P0})");

            if (e.Result.Confidence < MinConfidence)
            {
                Notifier.Info("Nisam siguran", "Pokušajte ponovo, malo razgovjetnije.");
                return;
            }

            if (!Commands.Contains(text)) return;

            Application.Current?.Dispatcher.Invoke(() => CommandRecognized?.Invoke(text));
        }

        private static void OnRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            var guess = e.Result?.Text ?? "(nista)";
            System.Diagnostics.Debug.WriteLine($"[glas] odbijeno, cuo: \"{guess}\"");
        }
    }
}