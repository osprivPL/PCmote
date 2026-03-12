using System.Net.Sockets;

namespace PCmotePhone
{
    public partial class MainPage : ContentPage
    {
        

        // Zmienna, która mówi nam, czy celowo trzymamy połączenie
        private bool _isConnected = false;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ConnectBtn_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(IpEntry.Text) || !int.TryParse(PortEntry.Text, out int port))
            {
                ErrorLabel.Text = "Sprawdź poprawność IP i Portu!";
                ErrorLabel.IsVisible = true;
                return;
            }

            ErrorLabel.IsVisible = false;

            try
            {
                // 1. Tworzymy klienta i łączymy (bez bloku "using", bo chcemy go zatrzymać!)
                GlobalThings.AppClient = new TcpClient();
                await GlobalThings.AppClient.ConnectAsync(IpEntry.Text, port);
                GlobalThings.AppStream = GlobalThings.AppClient.GetStream();

                _isConnected = true;

                // 2. Uruchamiamy "strażnika" połączenia w tle (nie blokuje on aplikacji)
                _ = Task.Run(MonitorConnectionAsync);

                // 3. Puszczamy użytkownika do menu
                ConnectionLayout.IsVisible = false;
                MenuLayout.IsVisible = true;
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Błąd: {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
        }

        // Metoda działająca w tle, która wykrywa zerwanie połączenia
        private async Task MonitorConnectionAsync()
        {
            try
            {
                while (_isConnected)
                {
                    // Sprawdzamy stan co sekundę, żeby nie zamęczyć procesora telefonu
                    await Task.Delay(1000);

                    // Wyciągamy surowe gniazdo sieciowe (Socket) z naszego klienta
                    Socket socket = GlobalThings.AppClient.Client;

                    // Logika: 
                    // socket.Poll z SelectRead zwraca true jeśli przyszły dane LUB jeśli przerwano połączenie.
                    // socket.Available mówi nam, ile bajtów czeka na odczyt.
                    // Zatem: jeśli Poll = true, ale Available = 0, to wiemy na 100%, że to rozłączenie (brak danych, a gniazdo reaguje).
                    if (socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0)
                    {
                        break; // Serwer zamknął połączenie
                    }
                }
            }
            catch
            {
                // Wystąpił błąd sieci (np. telefon stracił zasięg, ucięto Wi-Fi)
            }

            // Jeśli pętla się przerwała, a my nie kliknęliśmy "Rozłącz", to znaczy że to awaria
            if (_isConnected)
            {
                _isConnected = false;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.Navigation.PopToRootAsync();
                    await DisplayAlert("Utracono połączenie", "Serwer został wyłączony lub wystąpił błąd sieci.", "OK");
                    DisconnectAndResetUI();
                });
            }
        }

        private void DisconnectBtn_Clicked(object sender, EventArgs e)
        {
            // Zmieniamy flagę, żeby "strażnik" wiedział, że rozłączamy się celowo
            // i nie wyrzucał alertu o awarii
            _isConnected = false;
            DisconnectAndResetUI();
        }

        // Funkcja sprzątająca
        private void DisconnectAndResetUI()
        {
            // Zamykamy rurę i kuriera
            GlobalThings.AppStream?.Close();
            GlobalThings.AppClient?.Close();

            // Przywracamy ekran logowania
            MenuLayout.IsVisible = false;
            ConnectionLayout.IsVisible = true;
        }

        private void openPilot(object sender, EventArgs e)
        {
            Navigation.PushAsync(new PilotMode());
        }

        private void openCommands(object sender, EventArgs e)
        {
            Navigation.PushAsync(new Commands());
        }
    }
}