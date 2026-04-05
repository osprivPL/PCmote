using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text.Json;

namespace PCmotePhone
{
    public partial class MainPage : ContentPage
    {
        
        private bool _isConnected = false;
        public ObservableCollection<string> SavedIps { get; set; } = new ObservableCollection<string>();


        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            string savedJson = Preferences.Default.Get("SavedIPs", string.Empty);

            if (!string.IsNullOrEmpty(savedJson))
            {
                SavedIps = JsonSerializer.Deserialize<ObservableCollection<string>>(savedJson);
            }

            IpEntry.Text = SavedIps.FirstOrDefault() ?? string.Empty;
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
                GlobalThings.AppClient = new TcpClient();
                await GlobalThings.AppClient.ConnectAsync(IpEntry.Text, port);
                GlobalThings.AppStream = GlobalThings.AppClient.GetStream();

                _isConnected = true;

                _ = Task.Run(MonitorConnectionAsync);

                ConnectionLayout.IsVisible = false;
                MenuLayout.IsVisible = true;
                if (!SavedIps.Contains(IpEntry.Text))
                {
                    SavedIps.Add(IpEntry.Text);
                    SavedIps.Move(SavedIps.Count - 1, 0); 
                }
                else
                {
                    SavedIps.Move(SavedIps.IndexOf(IpEntry.Text), 0);
                }

                string jsonToSave = JsonSerializer.Serialize(SavedIps);
                Preferences.Default.Set("SavedIPs", jsonToSave);
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Błąd: {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
        }

        private async Task MonitorConnectionAsync()
        {
            try
            {
                while (_isConnected)
                {
                    await Task.Delay(1000);

                    Socket socket = GlobalThings.AppClient.Client;

                    if ((socket == null) || (socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0))
                    {
                        break; 
                    }
                }
            }
            catch
            {
            }

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
            _isConnected = false;
            DisconnectAndResetUI();
        }

        // Funkcja sprzątająca
        private void DisconnectAndResetUI()
        {
            GlobalThings.AppStream?.Close();
            GlobalThings.AppClient?.Close();

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

        private async void showHistoryOfIps(object sender, EventArgs e)
        {
            if (SavedIps.Count == 0)
            {
                await DisplayAlertAsync("History", "There are no saved ips", "OK");
                return;
            }
            string choosenIp = await DisplayActionSheetAsync("Choose saved IP", "Cancel", null, SavedIps.ToArray());

            if (choosenIp != "Cancel" && !string.IsNullOrEmpty(choosenIp))
            {
                IpEntry.Text = choosenIp;
            }
        }
    }
}