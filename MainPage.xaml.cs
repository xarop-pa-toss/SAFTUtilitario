﻿using System.Diagnostics.Contracts;
using Microsoft.Maui.Controls;
using System.Reflection;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SAFTUtilitario;

public partial class MainPage : ContentPage
{
	private protected string FicheiroPath { get; set; }
	private protected string PastaPath { get; set; }
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
        if (!GetAndCheckControls()) { return; }

        else if (!ValidarNIF()) {
            await DisplayAlert("Erro", "NIF inválido", "OK");
            return;
        }

        string jarPath = CriarJarTemp();
        string comando = null;

        if (jarPath == null) {
            await DisplayAlert("Erro", "Ficheiro crítico à execução do programa não foi encontrado.", "OK");
            return;
        }
        else {
            comando = SetComando(jarPath, "validar");
        }

        // Exit Codes
        int exitCode = await ExecutarComando(comando);

        string titulo;
        if (exitCode == 0) { titulo = "Sucesso"; }
        else { titulo = "Erro"; }

        string erro = String.Format("Erro {0} \n{1}"
            , exitCode.ToString()
            , erroDict[exitCode]);
        await DisplayAlert(titulo, erro, "OK");

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
            FicheiroPath = resultado.FullPath;
            LabelNomeFicheiro.Text = resultado.FileName;
            PastaPath = Path.GetDirectoryName(FicheiroPath);
        }
		else { return; }
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
	private bool ValidarNIF() //https://pt.wikipedia.org/wiki/N%C3%BAmero_de_identifica%C3%A7%C3%A3o_fiscal
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

    private string CriarJarTemp()
	{
		// Get assembly com recursos (Resources) do projecto
		Assembly assembly = typeof(MainPage).GetTypeInfo().Assembly;

		// Get lista de recursos do projecto e encontra o path do ficheiro .jar que queremos executar
		string[] recursos = assembly.GetManifestResourceNames();
		string nome = null;

		// Get nome do ficheiro .jar nos recursos
		foreach (string x in recursos) {
			if (x.EndsWith(".jar")) {
				nome = x; break; 
			}
		}

		// *** Se encontrar um ficheiro .jar, get full path. ***
		if (nome != null) {
			// Get stream para o recurso
			using (Stream resourceStream = assembly.GetManifestResourceStream(nome))
			{
                // Cria ficheiro temporário (em pasta temp) para os conteudos do recurso.
                string tempPath = Path.Combine(Path.GetTempPath(), nome);

				using (FileStream fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
				{
                    // Copia recurso para ficheiro temporário
                    resourceStream.CopyTo(fileStream);
                    // Return path do ficheiro temporário
                    return fileStream.Name;
                }
            }
        }
		else { return null; }
		// ***
	}

    private bool GetAndCheckControls()
	{
		if (!File.Exists(FicheiroPath))
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

	private string SetComando (string jarPath, string operacao)
	{
		string comando = "java -jar " + jarPath
            + " -n " + NIF
            + " -p " + Password
            + " -a " + Ano
            + " -m " + Mes
            + " -op " + operacao
            + " -i " + FicheiroPath
			+ " -o " + PastaPath
			+ " && pause";

		return comando; 
    }

	private Task<int> ExecutarComando (string comando)
	{
        // Agarra o exit code
        var taskCompSource = new TaskCompletionSource<int>();
        
        // Configurações a utilizar pelo processo cmd.exe
        ProcessStartInfo processoStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/C " + comando,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };

        // Iniciar o processo
        Process processo = new Process
        {
            EnableRaisingEvents = true,
            StartInfo = processoStartInfo
        };
		processo.Exited += (s, e) => taskCompSource.SetResult(processo.ExitCode);
        processo.Start();

        /*
         * É esperado que o processo termine em pouco tempo, por isso faz algum sentido usar processo.StandardOutput.ReadToEnd() que não é assincrono
         * ou seja, bloqueia o UI enquanto lê as linhas. No entanto isso não é boa prática por isso iremos usar um StreamReader e uma Task
         * permitindo utilização do programa enquanto o SAFT é processado.
         */

        // Inicializar Reader
        StreamReader outputReader = processo.StandardOutput;

        // Método que faz update ao Editor com output
        async void UpdateEditor(string texto)
        {
            // InvokeOnMainThreadAsync faz update ao UI (main thread) a partir de um thread secundário
            await Device.InvokeOnMainThreadAsync(() =>
            {
                EditorCmdOutput.Text += texto;
            });
        }

        // Task de leitura do output e update do editor
        var leituraTask = Task.Run(async () =>
        {
            while (!outputReader.EndOfStream)
            {
                // Get linha
                var linha = await outputReader.ReadLineAsync();

                // Update Editor com linha
                UpdateEditor(linha + Environment.NewLine);
            }
        });

        processo.WaitForExit();
		return taskCompSource.Task;
    }
}