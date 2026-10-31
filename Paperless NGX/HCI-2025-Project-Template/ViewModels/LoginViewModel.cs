using HCI_2025_Project_Template.Helpers;
using HCI_2025_Project_Template.Services;
using HCI_2025_Project_Template.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HCI_2025_Project_Template.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private string? _username;
        private bool _isBusy;

        public string? Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public RelayCommand LoginCommand { get; }

        public LoginViewModel()
        {
            // Dozvoljavamo klik uvijek, da bismo mogli dati povratnu informaciju
            LoginCommand = new RelayCommand(ExecuteLogin);
        }

        private async void ExecuteLogin(object? parameter)
        {
            if (IsBusy) return;

            var passwordBox = parameter as PasswordBox;
            string password = passwordBox?.Password ?? "";

            if (string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Molimo unesite korisničko ime i lozinku.", "Prazna polja",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Polje za korisničko ime ne može biti prazno.", "Nedostaje podatak",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Molimo unesite lozinku.", "Nedostaje podatak",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsBusy = true;

            try
            {
                var service = new LoginService();
                var result = await service.Authenticate(Username, password);

                switch (result.Status)
                {
                    case LoginStatus.Success:
                        var main = new MainWindow();
                        main.Show();

                        foreach (Window item in Application.Current.Windows)
                        {
                            if (item is LoginView)
                            {
                                item.Close();
                                break;
                            }
                        }
                        break;

                    case LoginStatus.InvalidCredentials:
                        MessageBox.Show("Neispravno korisničko ime ili lozinka. Pokušajte ponovo.",
                            "Prijava neuspješna", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;

                    case LoginStatus.NetworkError:
                        MessageBox.Show($"Nije moguće uspostaviti vezu sa serverom.\n\nDetalji: {result.Message}",
                            "Nema veze sa serverom", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;

                    default:
                        MessageBox.Show($"Greška na serveru. {result.Message}",
                            "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Neočekivana greška: {ex.Message}", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}