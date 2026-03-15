using System.Text;

namespace PCmotePhone;

public partial class PilotMode : ContentPage
{
    private const double Sensitivity = 27.5;
    private const double Deadzone = 0.05;

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

            // Game daje około 50 odswiezen na sekunde, wiec n spali tel xd
            Gyroscope.Default.Start(SensorSpeed.Game);
        }
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

        if (Math.Abs(gyroZ) < Deadzone) gyroZ = 0;
        if (Math.Abs(gyroX) < Deadzone) gyroX = 0;

        double deltaX = -gyroZ * Sensitivity;
        double deltaY = -gyroX * Sensitivity;

        if (deltaX == 0 && deltaY == 0) return;


        //Format: MOUSE:X:Y\n (bo tcp skleja wiadomosci)
        // Invariant Culture by byla kropka zamiast przecinka
        string msg = $"MOUSE:{deltaX.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{deltaY.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n";
        byte[] dataToSend = System.Text.Encoding.UTF8.GetBytes(msg);

        // async by n blokowalo tel
        if (GlobalThings.AppStream != null)
        {
            GlobalThings.AppStream.WriteAsync(dataToSend, 0, dataToSend.Length);
        }
    }

    private async void leftMouseButton(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_LEFTMOUSEBTN"));
    }

    private async void scrollUp(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_SCROLLUP"));
    }

    private async void scrollDown(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_SCROLLDOWN"));
    }

    private async void rightMouseButton(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_RIGHTMOUSEBTN"));
    }

    private async void prevTrack(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_PREVTRACK"));
    }

    private async void stopPauseTrack(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_PAUSERESUME"));
    }

    private async void nextTrack(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_NEXTTRACK"));
    }

    private async void volDown(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_VOLDOWN"));
    }

    private async void volMute(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_VOLMUTE"));
    }

    private async void volUp(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_VOLUP"));
    }

    private async void showDesktop(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_SHOWDESKTOP"));
    }

    private async void lockPc(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_LOCKPC"));
    }

    private async void closeApp(object sender, EventArgs e)
    {
        await GlobalThings.AppStream.WriteAsync(Encoding.UTF8.GetBytes("PIL_CLOSEAPP"));
    }
}