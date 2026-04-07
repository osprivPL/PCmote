using System.Text;

namespace PCmotePhone;

public partial class PilotMode : ContentPage
{
    private const double _sensitivy = 27.5;
    private const double _deadzone = 0.05;
    private bool _isClearingText = false;
    string temp = "";

    public PilotMode()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Gyroscope.Default.IsSupported && !Gyroscope.Default.IsMonitoring)
        {
            Gyroscope.Default.ReadingChanged += Gyroscope_ReadingChanged;

            // Game about 50Hz
            Gyroscope.Default.Start(SensorSpeed.Game);
        }

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {
            HiddenKeyboardInput.Focus();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Gyroscope.Default.IsSupported && Gyroscope.Default.IsMonitoring)
        {
            Gyroscope.Default.ReadingChanged -= Gyroscope_ReadingChanged;
            Gyroscope.Default.Stop();
        }
    }
    private void Gyroscope_ReadingChanged(object sender, GyroscopeChangedEventArgs e)
    {
        var data = e.Reading.AngularVelocity;

        double gyroZ = data.Z;
        double gyroX = data.X;

        if (Math.Abs(gyroZ) < _deadzone) gyroZ = 0;
        if (Math.Abs(gyroX) < _deadzone) gyroX = 0;

        double deltaX = -gyroZ * _sensitivy;
        double deltaY = -gyroX * _sensitivy;

        if (deltaX == 0 && deltaY == 0) return;


        //Format: MOUSE:X:Y\n (tcp connects messages)
        // Invariant Culture to have dot as separator
        string msg = $"MOUSE:{deltaX.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{deltaY.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n";
        byte[] dataToSend = System.Text.Encoding.UTF8.GetBytes(msg);

        if (GlobalThings.AppStream != null)
        {
            GlobalThings.AppStream.WriteAsync(dataToSend, 0, dataToSend.Length);
        }
    }

    //Keyboard:

    private void BringKeyboard(object sender, EventArgs e)
    {
        HiddenKeyboardInput.Unfocus();
        HiddenKeyboardInput.Focus();
        HiddenKeyboardInput.ShowSoftInputAsync(CancellationToken.None);
    }

    private void HiddenKeyboardInput_Unfocused(object sender, EventArgs e)
    {
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200),() =>
        {
            HiddenKeyboardInput.Unfocus();
            HiddenKeyboardInput.Focus();
            HiddenKeyboardInput.ShowSoftInputAsync(CancellationToken.None);
        });
    }

    private async void HiddenKeyboardInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isClearingText) return;

        string newText = e.NewTextValue;

        if (!string.IsNullOrEmpty(newText)) {
            char lastChar = newText.Last();
            string msg = $"PIL_KEYBOARD:{lastChar}\n";
            
            temp+= lastChar;
            await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes(msg));
            if (temp.Length >= 5)
            {
                await DisplayAlertAsync("A", temp, "OK");
                temp = "";
            }

            _isClearingText = true;
            HiddenKeyboardInput.Text = string.Empty;
            _isClearingText = false;
        }
    }


    //Buttons:

    private async void leftMouseButtonPressed(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_LEFTMOUSEBTNP\n"));
    }
    private async void leftMouseButtonReleased(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_LEFTMOUSEBTNR\n"));
    }

    private async void scrollUp(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_SCROLLUP\n"));
    }

    private async void scrollDown(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_SCROLLDOWN\n"));
    }

    private async void rightMouseButtonPressed(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_RIGHTMOUSEBTNP\n"));
    }
    private async void rightMouseButtonReleased(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_RIGHTMOUSEBTNR\n"));
    }

    private async void prevTrack(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_PREVTRACK\n"));
    }

    private async void stopPauseTrack(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_PAUSERESUME\n"));
    }

    private async void nextTrack(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_NEXTTRACK\n"));
    }

    private async void volDown(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_VOLDOWN\n"));
    }

    private async void volMute(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_VOLMUTE\n"));
    }

    private async void volUp(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_VOLUP\n"));
    }

    private async void showDesktop(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_SHOWDESKTOP\n"));
    }

    private async void lockPc(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_LOCKPC\n"));
    }

    private async void closeApp(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_CLOSEAPP\n"));
    }
}