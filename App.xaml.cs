namespace SAFTUtilitario;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		Application.Current.UserAppTheme = AppTheme.Light;
		MainPage = new AppShell();
	}

    protected override Window CreateWindow(IActivationState activationState)
    {
		var janela = base.CreateWindow(activationState);

        // *** KNOWN MAUI BUG *** DOES NOT WORK ***
        // Get largura e altura do display (tendo em conta resolução e percentagem de "zoom")
        //var display = DeviceDisplay.Current.MainDisplayInfo;
        //var alturaMonitor = display.Height;
        //var larguraMonitor = display.Width;

        //const int altura = 600;
        //const int largura = 400;

        // Set dimensões para a aplicação
        //janela.MinimumHeight = altura;
        //janela.MaximumHeight = altura;
        //janela.MinimumWidth = largura;

        // Set tamanho da janela da app (deve ser double)
        //janela.Height = altura;
        //janela.Width = largura;

        return janela;	
    }
}
