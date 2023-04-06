using System.Diagnostics.Contracts;
using Microsoft.Maui.Controls;
using System.Reflection;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;

namespace SAFTUtilitario;

public partial class MainPage : ContentPage
{
    private protected string pickerPath { get; set; }
    private protected string pickerPasta { get; set; }
    private protected string pickerNome { get; set; }
    private protected string exePath { get; set; }
    private protected string jarPath { get; set; }
	private protected string saftPath { get; set; }
    private protected string saftPasta { get; set; }
    private protected string NIF { get; set; }
	private protected string Password { get; set; }
	private protected string Ano { get; set; }
	private protected string Mes { get; set; }
    private List<string> AnosList { get; set; }
    private List<string> MesesList {get; set; }	
    private IDictionary<int, string> erroDict = new Dictionary<int, string>();
    private Task leituraTask;

    public MainPage()
	{
        InitializeComponent();

		BindingContext = this;

		// Event de alteração do valor de PickerAno para mudar valores de PickerMes
		PickerAnos.SelectedIndexChanged += OnPickerAnosSelectedIndexChanged;

		// Lista com anos de 2017 até ao corrente
		AnosList = Enumerable.Range(2017, DateTime.Now.Year - 2016)
            .Select(y => y.ToString()).Reverse()
            .ToList();
		
        PickerAnos.ItemsSource = AnosList;
		PickerMeses.ItemsSource = MesesList;

		PickerAnos.SelectedIndex = 0;
		PickerMeses.SelectedIndex = 0;

        // Lista com meses do ano corrente
        UpdateMesesList();

        // Dicionário com os erros de cmd.exe
        erroDict.Add(0, "Operação executada com sucesso!");
        erroDict.Add(1, "O comando executado não é válido.");
        erroDict.Add(2, "O sistema não conseguiu encontrar o ficheiro .jar a executar.");
        erroDict.Add(3, "O sistema não conseguiu encontrar o directório de instalação.");
        erroDict.Add(5, "Acesso negado. O utilizador não tem permissão para aceder ao directório de instalação ou ao ficheiro a executar.");
        erroDict.Add(unchecked((int)3221225794), "Memória virtual insuficiente. O Windows ficou sem memória e não consegue executar o programa.");
        erroDict.Add(221225495, "Não foi possivel abrir o ficheiro .jar. É possivel que o utilizador não tenha permissões neste computador.");
    }


    // MAIN
    private async void Main(string operacao, object sender, EventArgs e)
    {
        // Get permissões admin
        PermissionStatus statusread = await Permissions.RequestAsync<Permissions.StorageRead>();
        PermissionStatus statuswrite = await Permissions.RequestAsync<Permissions.StorageWrite>();

        // Get root do programa (para ter acesso a Resources)
        exePath = AppDomain.CurrentDomain.BaseDirectory;

        if (!GetAndCheckControls()) { return; }

        else if (!ValidarNIF()) {
            await DisplayAlert("Erro", "NIF inválido", "OK");
            return;
        }

        // Get .jar a executar ( pesquisar sobre "Build Action MauiAsset" usado para update de ficheiros após release)
        jarPath = GetJar();
        // Copiar ficheiro SAFT para os Resources da app e faz update às variaveis globais de Path e Pasta da localização do ficheiro
        CopiarFicheiroSAFT();

        string comando = null;

        if (jarPath == null) {
            await DisplayAlert("Erro", "Ficheiro crítico à execução do programa não foi encontrado.", "OK");
            return;
        }
        else {
           comando = SetComando(jarPath, "validar");
        }

        await ExecutarComando(comando);

        return;
    }

    private string SetComando(string jarPath, string operacao)
    {
        string comando = "java -jar \"" + jarPath + "\""
            + " -n " + NIF
            + " -p " + Password
            + " -a " + Ano
            + " -m " + Mes
            + " -op " + operacao
            + " -i \"" + saftPath + "\""
            + " -o \"" + saftPasta + "\"";

        return comando;
    }

    private async Task ExecutarComando(string comando)
    {
        // Configurações a utilizar pelo processo cmd.exe
        ProcessStartInfo processoStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c " + comando,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Verb = "runas" // run as (admin rights por causa do /c)
        };

        // Iniciar o processo
        Process processo = new Process
        {
            EnableRaisingEvents = true,
            StartInfo = processoStartInfo
        };

        /*
         * É esperado que o processo termine em pouco tempo, por isso faz algum sentido usar processo não assincrono,
         * ou seja, que bloqueia o UI enquanto lê as linhas. 
         * No entanto isso não é boa prática por tentámos usar um StreamReader e uma Task permitindo utilização do programa enquanto o SAFT é processado.
         * Infelizmente o .jar, quando dá erro, processa tudo isso de uma vez só e não aparecia no Output.
         * Por isso usamos um StringBuilder; deixamos o cmd.exe correr até ao fim e apanhamos o output de uma vez só.
         */

        // Inicializar builder
        StringBuilder outputString = new StringBuilder();

        // Configurar o output redirect para stdout e errout
        processo.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputString.AppendLine(e.Data);
            }
        };

        processo.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputString.AppendLine(e.Data);
            }
        };

        // Check se ficheiro SAFT está em uso por outro processo

        processo.Start();
        processo.BeginOutputReadLine();
        processo.BeginErrorReadLine();
        await processo.WaitForExitAsync();
        processo.Close();

        UpdateEditor(outputString.ToString());
        EditorCmdOutput.Text += Environment.NewLine;


        // Método que faz update ao Editor com output
        async void UpdateEditor(string texto)
        {
            // InvokeOnMainThreadAsync faz update ao UI (main thread) a partir de um thread secundário
            await Device.InvokeOnMainThreadAsync(() =>
            {
                EditorCmdOutput.Text += texto;
            });
        }
        return;
    }


    // *** Eventos ***
    void OnPickerAnosSelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateMesesList();
        PickerMeses.ItemsSource = MesesList;
    }

    private async void OnSelectFileClicked(object sender, EventArgs e)
	{
		var resultado = await FilePicker.PickAsync();
			
        if (resultado != null)
        {
            pickerPath = resultado.FullPath;
            pickerNome = resultado.FileName;
            pickerPasta = Path.GetDirectoryName(saftPath);

            LabelNomeFicheiro.Text = pickerNome;
            
        }
		else { return; }
	}

    private void OnBtnApagarClicked(object sender, EventArgs e)
    {
        EditorCmdOutput.Text = "";
    }

	private async void OnSelectValidarClicked(object sender, EventArgs e)
	{
        Main("validar", sender, e);
    }

	private async void OnSelectSubmeterClicked(object sender, EventArgs e)
	{
        Main("enviar", sender, e);
    }
    

    // *** Funções suporte ***
	private bool ValidarNIF() //https://pt.wikipedia.org/wikioN%C3%BAmero_de_identifica%C3%A7%C3%A3o_fiscal
    {
        int tamanhoNumero = 9; // Tamanho do número NIF

        string filteredNumber = Regex.Match(NIF, @"[0-9]+").Value; // extrair Número

        if (filteredNumber.Length != tamanhoNumero || int.Parse(filteredNumber[0].ToString()) == 0) { return false; } // Verificar Tamanho, e zero no inicio

        int calculoCheckSum = 0;
        // Calcular check sum
        for (int i = 0; i < tamanhoNumero - 1; i++)
        {
            calculoCheckSum += (int.Parse(filteredNumber[i].ToString())) * (tamanhoNumero - i);
        }

        int digitoVerificacao = 11 - (calculoCheckSum % 11);

        if (digitoVerificacao > 9) { digitoVerificacao = 0; }
        // retornar validação
        return digitoVerificacao == int.Parse(filteredNumber[tamanhoNumero - 1].ToString());
    }

    void UpdateMesesList()
    {
        // Indice 0 do PickerAnos é sempre o ano corrente
        int meses = PickerAnos.SelectedIndex == 0 ? DateTime.Now.Month : 12;

        MesesList = Enumerable.Range(1, meses)
            .Select(m => m.ToString("D2")).Reverse()
            .ToList();
    }

    private string GetJar()
    {
		// Get assembly com recursos (Resources) do projecto
		Assembly assembly = typeof(MainPage).GetTypeInfo().Assembly;

		// Get lista de recursos do projecto e encontra o path do ficheiro .jar que queremos executar
		string[] recursos = assembly.GetManifestResourceNames();

        // Get nome do ficheiro .jar nos recursos
        string jarNomeRecurso = recursos.FirstOrDefault(x => x.EndsWith(".jar")); // Isto retorna algo como "SAFTUtilitario.Resources.saft.jar". Tem de ser tratado

        if (jarNomeRecurso != null)
        {
            string[] separado = jarNomeRecurso.Split('.');
            jarNomeRecurso = separado[separado.Length - 2] + "." + separado[separado.Length - 1];

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", jarNomeRecurso);
        }
        else return null;
	}
    
    private void CopiarFicheiroSAFT()
    {
        saftPasta = Path.Combine(exePath, "Resources", "SAFT");

        if (!Directory.Exists(saftPasta))
        {
            Directory.CreateDirectory(saftPasta);
        }

        saftPath = Path.Combine(saftPasta, pickerNome);

        File.Copy(pickerPath, saftPath, true);
    }

    private bool GetAndCheckControls()
	{
		if (!File.Exists(pickerPath))
		{
			DisplayAlert("Erro", "O ficheiro não está presente no caminho seleccionado.", "OK");
            return false;
		}

        NIF = EntryNIF.Text;
        Password = EntryPassword.Text;
        Ano = (string)PickerAnos.SelectedItem;
        Mes = (string)PickerMeses.SelectedItem;

        if (NIF == null ||
			Password == null ||
			Ano == null ||
			Mes == null)
        { 
            DisplayAlert("Erro", "Dados insuficientes para processar o ficheiro SAFT. Por favor verifique se existem campos em branco.", "OK");
            return false;
        }
		return true;
    }



}