using PCmotePhone.Models;
using System.Text;
using System.Text.Json;

namespace PCmotePhone;

public partial class Commands : ContentPage
{
    public Commands()
    {
        InitializeComponent();
        Task.Run(() => waitForJson());
    }

    private async void sendCommand(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes(CommandEntry.Text));
    }

    private void createCommandView(string header, string comm)
    {
        var titleLabel = new Label
        {
            Text = header,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold
        };
        titleLabel.SetDynamicResource(Label.TextColorProperty, "PrimaryTextColor");

        var commandLabel = new Label
        {
            Text = comm,
            FontSize = 13,
            TextColor = Colors.Gray
        };

        var stackLayout = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 4,
            Children = { titleLabel, commandLabel }
        };
        Grid.SetColumn(stackLayout, 0);

        var executeButton = new Button
        {
            Text = "Execute",
            TextColor = Colors.White,
            CornerRadius = 8,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(20, 0),
            HeightRequest = 40,
            VerticalOptions = LayoutOptions.Center
        };
        executeButton.SetDynamicResource(Button.BackgroundColorProperty, "Primary");
        executeButton.Clicked += async (s, e) =>
        {
            try
            {
                await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes(comm));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Nie można wysłać komendy: {ex.Message}", "OK");
            }
        };
        Grid.SetColumn(executeButton, 1);

        var grid = new Grid
        {
            Padding = new Thickness(15),
            ColumnSpacing = 15,
            ColumnDefinitions =
    {
        new ColumnDefinition(GridLength.Star),
        new ColumnDefinition(GridLength.Auto)
    },
            Children = { stackLayout, executeButton }
        };

        var border = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            StrokeThickness = 1,
            Stroke = Colors.Gray,
            Margin = new Thickness(10, 5),
            Content = grid
        };
        border.SetDynamicResource(Border.BackgroundColorProperty, "PageBackgroundColor");
        commandsView.Children.Add(border);
    }

    private async void waitForJson()
    {
        // json z serwera na komende
        commandsView.Children.Clear();
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("GET_JSON"));

        byte[] buffer = new byte[4096];
        int bytesRead = await GlobalThings.AppStream.ReadAsync(buffer, 0, buffer.Length);

        if (bytesRead > 0)
        {
            string json = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var deserializedJson = JsonSerializer.Deserialize<ShellCommand[]>(json);
                    foreach (var obj in deserializedJson ?? Array.Empty<ShellCommand>())
                    {
                        createCommandView(obj.Header, obj.Command);
                    }

                    //await DisplayAlertAsync("Sukces!", $"Wczytano komend: {deserializedJson?.Length ?? 0}", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Błąd deserializacji", $"Nie można wczytać JSON-a: {ex.Message}", "OK");
                }
            });

        }
    }
}