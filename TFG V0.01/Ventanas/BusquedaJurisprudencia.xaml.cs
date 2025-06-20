﻿using JurisprudenciaApi.Controllers;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Para ObservableCollection
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HtmlAgilityPack; // Added using
using TFG_V0._01.Supabase;
using TFG_V0._01.Supabase.Models;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using TFG_V0._01.Models;
using ComunidadAutonomaFrontend = TFG_V0._01.Models.ComunidadAutonoma;
using ProvinciaFrontend = TFG_V0._01.Models.Provincia;

namespace TFG_V0._01.Ventanas
{
    /// <summary>
    /// Lógica de interacción para BusquedaJurisprudencia.xaml
    /// </summary>
    public partial class BusquedaJurisprudencia : Window
    {
        #region variables animacion
        private Storyboard fadeInStoryboard;
        private Storyboard shakeStoryboard;
        private Storyboard meshAnimStoryboard;

        // Brushes y fondo animado
        private RadialGradientBrush mesh1Brush;
        private RadialGradientBrush mesh2Brush;
        #endregion

        #region Variables API
        private static readonly HttpClient client = new HttpClient();
        // La URL base de la API se puede configurar aquí o en un archivo de configuración
        private static string ApiBaseUrl = "http://localhost:5146"; // Valor por defecto para desarrollo local
        // Para producción, cambiar a la URL donde se publique la API
        // Ejemplo: "https://tu-api-produccion.com"
        private static readonly Dictionary<string, string> TipoOrganoMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Tribunal Supremo", "11|12|13|14|15|16" },
            { "Tribunal Supremo. Sala de lo Civil", "11" },
            { "Tribunal Supremo. Sala de lo Penal", "12" },
            { "Tribunal Supremo. Sala de lo Contencioso", "13" },
            { "Tribunal Supremo. Sala de lo Social", "14" },
            { "Tribunal Supremo. Sala de lo Militar", "15" },
            { "Tribunal Supremo. Sala de lo Especial", "16" },
            { "Audiencia Nacional", "22|2264|23|24|25|26|27|28|29" },
            { "Audiencia Nacional. Sala de lo Penal", "22" },
            { "Sala de Apelación de la Audiencia Nacional", "2264" },
            { "Audiencia Nacional. Sala de lo Contencioso", "23" },
            { "Audiencia Nacional. Sala de lo Social", "24" },
            { "Audiencia Nacional. Juzgados Centrales de Instrucción", "27" },
            { "Audiencia Nacional. Juzgado Central de Menores", "26" },
            { "Audiencia Nacional. Juzgado Central de Vigilancia Penitenciaria", "25" },
            { "Audiencia Nacional. Juzgados Centrales de lo Contencioso", "29" },
            { "Audiencia Nacional. Juzgados Centrales de lo Penal", "28" },
            { "Tribunal Superior de Justicia", "31|31201202|33|34" },
            { "Tribunal Superior de Justicia. Sala de lo Civil y Penal", "31" },
            { "Sección de Apelación Penal. TSJ Sala de lo Civil y Penal", "31201202" },
            { "Tribunal Superior de Justicia. Sala de lo Contencioso", "33" },
            { "Tribunal Superior de Justicia. Sala de lo Social", "34" },
            { "Audiencia Provincial", "37" },
            { "Audiencia Provincial. Tribunal Jurado", "38" },
            { "Tribunal de Marca de la UE", "1001" },
            { "Juzgado de Primera Instancia", "42" },
            { "Juzgado de Instrucción", "43" },
            { "Juzgado de lo Contencioso Administrativo", "45" },
            { "Juzgado de Menores", "53" },
            { "Juzgado de Primera Instancia e Instrucción", "41" },
            { "Juzgado de lo Mercantil", "47" },
            { "Juzgados de Marca de la UE", "1002" },
            { "Juzgado de lo Penal", "51" },
            { "Juzgado de lo Social", "44" },
            { "Juzgado de Vigilancia Penitenciaria", "52" },
            { "Juzgado de Violencia sobre la Mujer", "48" },
            { "Tribunal Militar Territorial", "83" },
            { "Tribunal Militar Central", "85" },
            { "Consejo Supremo de Justicia Militar", "75" },
            { "Audiencia Territorial", "36" }
        };
        public ObservableCollection<JurisprudenciaResult> ResultadosBusqueda { get; set; }
        public ICommand LimpiarCommand { get; private set; }
        #endregion

        #region Nuevas variables para paginación
        private int _paginaActual = 1;
        private const int RegistrosPorPaginaConst = 10; // Cuántos cargar por página
        private bool _isLoading = false; // Para evitar múltiples llamadas simultáneas
        private JurisprudenciaSearchParameters _lastSearchParameters; // Para recordar los filtros al cargar más
        #endregion

        public ObservableCollection<ComunidadAutonomaFrontend> LocalizacionesJerarquicas { get; set; }

        // Implementación simple de ICommand para enlazar comandos en XAML
        public class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<object?, bool>? _canExecute;

            public event EventHandler? CanExecuteChanged;

            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

            public void Execute(object? parameter) => _execute(parameter);

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #region Aplicar modo oscuro/claro cargado por sistema
        private void AplicarModoSistema()
        {
            this.Tag = MainWindow.isDarkTheme;
           
            // Los brushes se crean/actualizan en CrearBrushesParaTema() o CrearFondoAnimado()
           
           // Crear nuevos estilos dinámicamente para textos
           var primaryTextStyle = new Style(typeof(TextBlock));
           var secondaryTextStyle = new Style(typeof(TextBlock));

           if (MainWindow.isDarkTheme)
           {
               primaryTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"))));
               secondaryTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0B0B0"))));
           }
           else
           {
               primaryTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#303030"))));
               secondaryTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#606060"))));
           }

           // Reemplazar los recursos existentes (asegúrate de que estas claves existan en XAML)
           this.Resources["PrimaryTextStyle"] = primaryTextStyle;
           this.Resources["SecondaryTextStyle"] = secondaryTextStyle;

           // Forzar actualización de estilos en ComboBoxes
           ActualizarEstilosComboBoxes();

           navbar.ActualizarTema(MainWindow.isDarkTheme);
           IniciarAnimacionMesh();
        }
        #endregion


        #region boton cambiar tema
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            // Alternar el estado del tema
            MainWindow.isDarkTheme = !MainWindow.isDarkTheme;

            // Crear nuevos brushes con los colores del tema actual
            CrearBrushesParaTema();
            
            // Aplicar el resto de cambios de tema
            AplicarModoSistema();
            var fadeAnimation = CrearFadeAnimation(0.7, 0.9, 0.3, true);
            this.BeginAnimation(OpacityProperty, fadeAnimation);
        }

        #endregion

        #region Control de ventana sin bordes
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Maximized;
            }
            else
            {
                this.DragMove();
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LegislacionTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var legislacionWindow = new Legislacion();
            legislacionWindow.ShowDialog();
        }
        #endregion

        #region Animaciones
        private void InitializeAnimations()
        {
           fadeInStoryboard = CrearStoryboard(this, OpacityProperty, CrearFadeAnimation(0, 1, 0.5));
           shakeStoryboard = CrearStoryboard(null, TranslateTransform.XProperty, CrearShakeAnimation());
        }

        private Storyboard CrearStoryboard(DependencyObject target, DependencyProperty property, DoubleAnimation animation)
        {
            var storyboard = new Storyboard();
            if (target != null && property != null)
            {
                Storyboard.SetTarget(animation, target);
                Storyboard.SetTargetProperty(animation, new PropertyPath(property));
            }
            storyboard.Children.Add(animation);
            return storyboard;
        }

        private DoubleAnimation CrearFadeAnimation(double from, double to, double durationSeconds, bool autoReverse = false) =>
            new()
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                AutoReverse = autoReverse
            };

        private DoubleAnimation CrearShakeAnimation() =>
            new()
            {
                From = 0,
                To = 5,
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
                Duration = TimeSpan.FromSeconds(0.05)
            };

        private void BeginFadeInAnimation()
        {
            this.Opacity = 0;
            fadeInStoryboard.Begin();
        }

        private void ShakeElement(FrameworkElement element)
        {
            var trans = new TranslateTransform();
            element.RenderTransform = trans;
            trans.BeginAnimation(TranslateTransform.XProperty, CrearShakeAnimation());
        }
        #endregion

        #region API Interaction
        private async void BuscarButton_Click(object sender, RoutedEventArgs e)
        {
            _paginaActual = 1; // Resetear a la primera página para una nueva búsqueda
            ResultadosBusqueda.Clear(); // Limpiar resultados anteriores
            CargarMasButton.Visibility = Visibility.Collapsed; // Ocultar botón al iniciar nueva búsqueda

            _lastSearchParameters = GetSearchParametersFromUI(); // Guardar los parámetros actuales
            _lastSearchParameters.PaginaActual = _paginaActual;
            _lastSearchParameters.RegistrosPorPagina = RegistrosPorPaginaConst;

            await RealizarBusquedaAsync(_lastSearchParameters);
        }

        private async void CargarMasButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoading || _lastSearchParameters == null) return; // No cargar si ya está cargando o no hay búsqueda previa

            _paginaActual++;
            _lastSearchParameters.PaginaActual = _paginaActual;
            // _lastSearchParameters.RegistrosPorPagina ya está establecido

            await RealizarBusquedaAsync(_lastSearchParameters, esCargaAdicional: true);
        }

        private async Task RealizarBusquedaAsync(JurisprudenciaSearchParameters parameters, bool esCargaAdicional = false)
        {
            if (_isLoading) return;

            _isLoading = true;
            // Opcional: Deshabilitar botones para evitar clics múltiples
            BuscarButton.IsEnabled = false;
            CargarMasButton.IsEnabled = false;
            // Opcional: Mostrar un indicador de carga
            // ResultadosTextBlock.Text = "Cargando..."; 
            // ResultadosTextBlock.Visibility = Visibility.Visible;


            try
            {
                string jsonParameters = JsonSerializer.Serialize(parameters, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                var content = new StringContent(jsonParameters, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("/api/Jurisprudencia/search", content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    List<JurisprudenciaResult>? nuevosResultados = JsonSerializer.Deserialize<List<JurisprudenciaResult>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (nuevosResultados != null && nuevosResultados.Any())
                    {
                        foreach (var result in nuevosResultados)
                        {
                            ResultadosBusqueda.Add(result);
                        }
                        // Mostrar el botón "Cargar Más" si la API devolvió el número completo de registros esperados
                        CargarMasButton.Visibility = nuevosResultados.Count == RegistrosPorPaginaConst ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else
                    {
                        CargarMasButton.Visibility = Visibility.Collapsed; // No hay más resultados
                        if (esCargaAdicional)
                        {
                            MessageBox.Show("No hay más resultados para cargar.", "Fin de los resultados", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        // Si no es carga adicional y no hay resultados, el DataTrigger del ResultadosTextBlock se encargará.
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    string errorMsg = $"Error al buscar: {response.StatusCode}\n{errorContent}";
                    MessageBox.Show(errorMsg, "Error API", MessageBoxButton.OK, MessageBoxImage.Error);
                    CargarMasButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (HttpRequestException httpEx)
            {
                string errorMsg = $"Error de conexión: Verifique que la API ({ApiBaseUrl}) esté ejecutándose.\n{httpEx.Message}";
                MessageBox.Show(errorMsg, "Error Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                CargarMasButton.Visibility = Visibility.Collapsed;
            }
            catch (JsonException jsonEx)
            {
                string errorMsg = "Error al procesar la respuesta de la API:\n" + jsonEx.Message;
                MessageBox.Show(errorMsg, "Error Deserialización", MessageBoxButton.OK, MessageBoxImage.Error);
                CargarMasButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                string errorMsg = "Ocurrió un error inesperado:\n" + ex.Message;
                MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CargarMasButton.Visibility = Visibility.Collapsed;
            }
            finally
            {
                _isLoading = false;
                BuscarButton.IsEnabled = true;
                CargarMasButton.IsEnabled = true;
                // Opcional: Ocultar indicador de carga
                // if (ResultadosBusqueda.Any()) ResultadosTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private async Task CargarDatosInicialesAsync()
        {
            try
            {
                var response = await client.GetAsync($"{ApiBaseUrl}/api/Jurisprudencia/initialData");
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var initialData = JsonSerializer.Deserialize<InitialDataResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Añadir "Todos" si no viene de la API y es necesario
                    initialData.Jurisdicciones.Insert(0, "Todos");
                    initialData.TiposResolucion.Insert(0, "Todos");
                    initialData.OrganosJudiciales.Insert(0, "Todos");

                    // Cargar comunidades autónomas
                    var comunidadesResponse = await client.GetAsync($"{ApiBaseUrl}/api/Jurisprudencia/comunidades");
                    if (comunidadesResponse.IsSuccessStatusCode)
                    {
                        string comunidadesJson = await comunidadesResponse.Content.ReadAsStringAsync();
                        var comunidades = JsonSerializer.Deserialize<List<ComunidadAutonomaFrontend>>(comunidadesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        // Asignar los datos a los ComboBox
                        JurisdiccionComboBox.ItemsSource = initialData.Jurisdicciones;
                        TipoResolucionComboBox.ItemsSource = initialData.TiposResolucion;
                        OrganoJudicialComboBox.ItemsSource = initialData.OrganosJudiciales;

                        // Seleccionar "Todos" por defecto
                        JurisdiccionComboBox.SelectedItem = "Todos";
                        TipoResolucionComboBox.SelectedItem = "Todos";
                        OrganoJudicialComboBox.SelectedItem = "Todos";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos iniciales: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LocalizacionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComunidadAutonomaFrontend comunidad)
            {
                try
                {
                    // Cargar las provincias de la comunidad seleccionada
                    var response = await client.GetAsync($"{ApiBaseUrl}/api/Jurisprudencia/provincias/{comunidad.Nombre}");
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var provincias = JsonSerializer.Deserialize<List<ProvinciaFrontend>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        // Actualizar las provincias de la comunidad seleccionada
                        if (comunidad.Provincias != null && comunidad.Provincias.GetType() != typeof(ObservableCollection<ProvinciaFrontend>))
                        {
                            comunidad.Provincias = new ObservableCollection<ProvinciaFrontend>(comunidad.Provincias);
                        }
                        foreach (var provincia in comunidad.Provincias)
                        {
                            // Convertir sedes de string a Sede si es necesario
                            if (provincia.Sedes != null && provincia.Sedes.Count > 0 && provincia.Sedes.First() is string)
                            {
                                var sedesConvertidas = provincia.Sedes.Cast<string>().Select(nombre => new TFG_V0._01.Models.Sede { Nombre = nombre }).ToList();
                                provincia.Sedes = new ObservableCollection<TFG_V0._01.Models.Sede>(sedesConvertidas);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar las provincias: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private JurisprudenciaSearchParameters GetSearchParametersFromUI()
        {
            var parameters = new JurisprudenciaSearchParameters();

            parameters.Jurisdiccion = (JurisdiccionComboBox.SelectedItem as string == "Todos" || JurisdiccionComboBox.SelectedItem == null) ? null : JurisdiccionComboBox.SelectedItem as string;
            
            if (TipoResolucionComboBox.SelectedItem is string tipoResValue && !string.IsNullOrEmpty(tipoResValue) && tipoResValue != "Todos")
            {
                parameters.TiposResolucion = new List<string> { tipoResValue };
            }

            if (OrganoJudicialComboBox.SelectedItem is string orgValue && !string.IsNullOrEmpty(orgValue) && orgValue != "Todos")
            {
                parameters.OrganosJudiciales = new List<string> { orgValue };
            }

            // Recoger selección de comunidades y provincias
            var comunidadesSeleccionadas = new List<string>();
            var provinciasSeleccionadas = new List<string>();

            foreach (var comunidad in LocalizacionesJerarquicas)
            {
                if (comunidad.IsChecked)
                {
                    comunidadesSeleccionadas.Add(comunidad.Codigo);
                }

                foreach (var provincia in comunidad.Provincias)
                {
                    if (provincia.IsChecked)
                    {
                        provinciasSeleccionadas.Add(provincia.Codigo);
                    }
                }
            }

            parameters.ComunidadesAutonomas = comunidadesSeleccionadas.Any() ? comunidadesSeleccionadas : null;
            parameters.Provincias = provinciasSeleccionadas.Any() ? provinciasSeleccionadas : null;

            if (IdiomaComboBox.SelectedItem is ComboBoxItem iItem && iItem.Content != null)
            {
                string? idiomaValue = iItem.Content.ToString();
                parameters.Idioma = (idiomaValue == "Todos" || string.IsNullOrEmpty(idiomaValue)) ? null : idiomaValue;
            }

            parameters.Roj = string.IsNullOrWhiteSpace(NumeroRojTextBox.Text) ? null : NumeroRojTextBox.Text;
            parameters.Ecli = string.IsNullOrWhiteSpace(EcliTextBox.Text) ? null : EcliTextBox.Text;
            parameters.NumeroResolucion = string.IsNullOrWhiteSpace(NumeroResolucionTextBox.Text) ? null : NumeroResolucionTextBox.Text;
            parameters.NumeroRecurso = string.IsNullOrWhiteSpace(NumeroRecursoTextBox.Text) ? null : NumeroRecursoTextBox.Text;
            parameters.Ponente = string.IsNullOrWhiteSpace(PonenteTextBox.Text) ? null : PonenteTextBox.Text;
            parameters.Seccion = string.IsNullOrWhiteSpace(SeccionTextBox.Text) ? null : SeccionTextBox.Text;
            parameters.Legislacion = string.IsNullOrWhiteSpace(LegislacionTextBox.Text) ? null : LegislacionTextBox.Text;
            parameters.FechaDesde = FechaDesdeDatePicker.SelectedDate;
            parameters.FechaHasta = FechaHastaDatePicker.SelectedDate;

            return parameters;
        }

        private void EjecutarLimpiarFormulario(object? parameter)
        {
            // Resetear ComboBoxes a "Todos"
            JurisdiccionComboBox.SelectedItem = "Todos";
            TipoResolucionComboBox.SelectedItem = "Todos";
            OrganoJudicialComboBox.SelectedItem = "Todos";
           

            // Modificar para seleccionar el ComboBoxItem con Content="Todos" para IdiomaComboBox
            var idiomaTodosItem = IdiomaComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content?.ToString() == "Todos");
            if (idiomaTodosItem != null)
            {
                IdiomaComboBox.SelectedItem = idiomaTodosItem;
            }

            // Resetear TextBoxes a vacío
            SeccionTextBox.Text = string.Empty;
            NumeroRojTextBox.Text = string.Empty;
            EcliTextBox.Text = string.Empty;
            NumeroResolucionTextBox.Text = string.Empty;
            NumeroRecursoTextBox.Text = string.Empty;
            PonenteTextBox.Text = string.Empty;
            LegislacionTextBox.Text = string.Empty;

            // Resetear DatePickers a null
            FechaDesdeDatePicker.SelectedDate = null;
            FechaHastaDatePicker.SelectedDate = null;

            // Limpiar resultados de búsqueda
            ResultadosBusqueda.Clear();
            _paginaActual = 1;
            CargarMasButton.Visibility = Visibility.Collapsed;

            // Limpiar selecciones del TreeView
            if (LocalizacionesJerarquicas != null)
            {
                foreach (var comunidad in LocalizacionesJerarquicas)
                {
                    comunidad.IsChecked = false;
                    foreach (var provincia in comunidad.Provincias)
                    {
                        provincia.IsChecked = false;
                    }
                }
                // Marcar solo 'Todas' como seleccionada por defecto
                var todas = LocalizacionesJerarquicas.FirstOrDefault(c => c.Nombre == "Todas");
                if (todas != null)
                    todas.IsChecked = true;
            }
        }
        #endregion

        private void VerDocumentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string url)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        // Mostrar el panel y cargar la URL
                        MostrarWebView(url);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo abrir el documento: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("La URL del documento no está disponible.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void MostrarWebView(string url)
        {
            try
            {
                // Mostrar el overlay
                OverlayPanel.Visibility = Visibility.Visible;

                // Mostrar el panel directamente sin animación
                WebViewPanel.Visibility = Visibility.Visible;

                // Inicializar WebView2 si es necesario
                if (DocumentWebView.CoreWebView2 == null)
                {
                    await DocumentWebView.EnsureCoreWebView2Async();

                    // Configurar el WebView para PDFs
                    DocumentWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    DocumentWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    DocumentWebView.CoreWebView2.Settings.IsPinchZoomEnabled = true;
                    DocumentWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                }

                // Asegurarse de que el manejador esté suscrito antes de navegar
                // Primero desuscribir para evitar suscripciones múltiples si MostrarWebView es llamado varias veces
                DocumentWebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                DocumentWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                // Cargar la URL inicial que contiene la previsualización/enlace de descarga
                DocumentWebView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                // Manejar errores generales (como la inicialización del WebView)
                MessageBox.Show($"Error al preparar la visualización del documento:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CerrarWebView();
            }
        }

        private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Este evento se dispara cuando la navegación se completa (con éxito o error)
            if (sender is WebView2 webView && e.IsSuccess)
            {
                try
                {
                    // JavaScript para encontrar el enlace de descarga del PDF por su texto.
                    // Buscamos un <a> tag que contenga el texto 'descargar la resolución aquí'.
                    // Nota: El texto exacto puede variar o puede estar dentro de un span/otro elemento.
                    // Si este selector falla, necesitaremos inspeccionar el HTML de la página.
                    string script = @"
                        (function() {
                            const links = document.querySelectorAll('a');
                            for (const link of links) {
                                // Buscar un enlace cuyo texto contenga 'descargar la resolución aquí' (insensible a mayúsculas/minúsculas y espacios)
                                if (link.textContent.toLowerCase().includes('descargar la resolución aquí'.toLowerCase())) {
                                    return link.href;
                                }
                            }
                            return null; // No se encontró el enlace
                        })();";

                    string jsonResult = await webView.CoreWebView2.ExecuteScriptAsync(script);

                    // Deserializar el valor devuelto (una cadena JSON: puede ser "null" o una URL entre comillas)
                    string? pdfUrl = JsonSerializer.Deserialize<string>(jsonResult);

                    if (!string.IsNullOrEmpty(pdfUrl))
                    {
                        // Evitar bucles infinitos al volver a navegar
                        webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;

                        // Navegar directamente al PDF encontrado
                        // Es importante que la URL sea absoluta. ExecuteScriptAsync(link.href) debería dar la URL absoluta.
                        webView.CoreWebView2.Navigate(pdfUrl);
                    }
                    // Si no se encuentra URL de PDF incrustado, simplemente mostramos la página completa cargada.
                }
                catch (Exception scriptEx)
                {
                    // Manejar errores al ejecutar el script o deserializar el resultado
                    MessageBox.Show($"Error al intentar extraer la URL del PDF incrustado:\n{scriptEx.Message}", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Continuar mostrando la página completa si falla la extracción
                }
            }
             // Si la navegación no fue exitosa (e.IsSuccess es false), no intentamos extraer nada.
        }

        private void CerrarWebView()
        {
            // Cerrar directamente sin animación
            WebViewPanel.Visibility = Visibility.Collapsed;
            OverlayPanel.Visibility = Visibility.Collapsed;
        }

        #region WebView
        private void CerrarWebView_Click(object sender, RoutedEventArgs e)
        {
            CerrarWebView();
        }

        private void OverlayPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CerrarWebView();
        }

        private void WebViewPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                CerrarWebView();
            }
        }
        #endregion

        #region Fondo Animado
        private void CrearFondoAnimado()
        {
            // Crear los brushes
            mesh1Brush = new RadialGradientBrush();
            mesh1Brush.Center = new Point(0.3, 0.3);
            mesh1Brush.RadiusX = 0.5;
            mesh1Brush.RadiusY = 0.5;
            mesh1Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#de9cb8"), 0));
            mesh1Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#9dcde1"), 1));
            // NO congelar aquí para permitir modificaciones posteriores

            mesh2Brush = new RadialGradientBrush();
            mesh2Brush.Center = new Point(0.7, 0.7);
            mesh2Brush.RadiusX = 0.6;
            mesh2Brush.RadiusY = 0.6;
            mesh2Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#dc8eb8"), 0));
            mesh2Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#98d3ec"), 1));
            // NO congelar aquí para permitir modificaciones posteriores

            // Crear el DrawingBrush
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(new GeometryDrawing(mesh1Brush, null, new RectangleGeometry(new Rect(0, 0, 1, 1))));
            drawingGroup.Children.Add(new GeometryDrawing(mesh2Brush, null, new RectangleGeometry(new Rect(0, 0, 1, 1))));
            var meshGradientBrush = new DrawingBrush(drawingGroup) { Stretch = Stretch.Fill };
            ((Grid)this.Content).Background = meshGradientBrush;
        }

        private void CrearBrushesParaTema()
        {
            // Detener la animación actual si existe
            meshAnimStoryboard?.Stop();
            
            // Crear nuevos brushes con los colores del tema actual
            mesh1Brush = new RadialGradientBrush();
            mesh1Brush.Center = new Point(0.3, 0.3);
            mesh1Brush.RadiusX = 0.5;
            mesh1Brush.RadiusY = 0.5;

            mesh2Brush = new RadialGradientBrush();
            mesh2Brush.Center = new Point(0.7, 0.7);
            mesh2Brush.RadiusX = 0.6;
            mesh2Brush.RadiusY = 0.6;

            if (MainWindow.isDarkTheme)
            {
                // Colores mesh oscuro
                mesh1Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#8C7BFF"), 0));
                mesh1Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#08a693"), 1));
                mesh2Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#3a4d5f"), 0));
                mesh2Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#272c3f"), 1));
            }
            else
            {
                // Colores mesh claro
                mesh1Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#de9cb8"), 0));
                mesh1Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#9dcde1"), 1));
                mesh2Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#dc8eb8"), 0));
                mesh2Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#98d3ec"), 1));
            }

            // Crear el nuevo DrawingBrush y aplicarlo al fondo
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(new GeometryDrawing(mesh1Brush, null, new RectangleGeometry(new Rect(0, 0, 1, 1))));
            drawingGroup.Children.Add(new GeometryDrawing(mesh2Brush, null, new RectangleGeometry(new Rect(0, 0, 1, 1))));
            var meshGradientBrush = new DrawingBrush(drawingGroup) { Stretch = Stretch.Fill };
            ((Grid)this.Content).Background = meshGradientBrush;
        }

        private void IniciarAnimacionMesh()
        {
            // Detener si ya existe
            meshAnimStoryboard?.Stop();
            meshAnimStoryboard = new Storyboard();
            // Animar Center de mesh1
            var anim1 = new PointAnimationUsingKeyFrames();
            anim1.KeyFrames.Add(new EasingPointKeyFrame(new Point(0.3, 0.3), KeyTime.FromTimeSpan(TimeSpan.Zero)));
            anim1.KeyFrames.Add(new EasingPointKeyFrame(new Point(0.7, 0.5), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(4))) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            anim1.KeyFrames.Add(new EasingPointKeyFrame(new Point(0.3, 0.3), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(8))) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            anim1.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard.SetTarget(anim1, mesh1Brush);
            Storyboard.SetTargetProperty(anim1, new PropertyPath(RadialGradientBrush.CenterProperty));
            meshAnimStoryboard.Children.Add(anim1);
            // Animar Center de mesh2
            var anim2 = new PointAnimationUsingKeyFrames();
            anim2.KeyFrames.Add(new EasingPointKeyFrame(new Point(0.7, 0.7), KeyTime.FromTimeSpan(TimeSpan.Zero)));
            anim2.KeyFrames.Add(new EasingPointKeyFrame(new Point(0.4, 0.4), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(4))) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            anim2.KeyFrames.Add(new EasingPointKeyFrame(new Point(0.7, 0.7), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(8))) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            anim2.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard.SetTarget(anim2, mesh2Brush);
            Storyboard.SetTargetProperty(anim2, new PropertyPath(RadialGradientBrush.CenterProperty));
            meshAnimStoryboard.Children.Add(anim2);
            meshAnimStoryboard.Begin();
        }
        #endregion

        #region Actualizar Estilos ComboBoxes

        private void ActualizarEstilosComboBoxes()
        {
            // Lista de nombres de ComboBoxes a actualizar
            var comboBoxNames = new List<string>
            {
                "JurisdiccionComboBox",
                "TipoResolucionComboBox",
                "OrganoJudicialComboBox",
                "LocalizacionComboBox",
                "IdiomaComboBox"
            };

            foreach (var name in comboBoxNames)
            {
                if (FindName(name) is ComboBox comboBox)
                {
                    // Guarda el estilo actual
                    var currentStyle = comboBox.Style;
                    // Establece el estilo a null temporalmente
                    comboBox.Style = null;
                    // Restaura el estilo original para forzar la re-evaluación
                    comboBox.Style = currentStyle;
                }
            }
        }

        #endregion

        private void ComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo != null && !combo.IsDropDownOpen)
            {
                combo.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        

        public async Task CargarLocalizacionesDesdeApiAsync()
        {
            try
            {
                var response = await client.GetAsync($"{ApiBaseUrl}/api/Jurisprudencia/comunidades");
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var comunidades = JsonSerializer.Deserialize<List<ComunidadAutonomaFrontend>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // FILTRAR comunidades, provincias y sedes con nombre 'TODOS' o 'Todas'
                    comunidades = comunidades?.Where(c => c.Nombre.ToUpper() != "TODOS" && c.Nombre.ToUpper() != "TODAS")
                        .Select(c => {
                            c.Provincias = new System.Collections.ObjectModel.ObservableCollection<ProvinciaFrontend>(
                                c.Provincias?.Where(p => p.Nombre.ToUpper() != "TODOS" && p.Nombre.ToUpper() != "TODAS")
                                .Select(p => {
                                    p.Sedes = new System.Collections.ObjectModel.ObservableCollection<Sede>(
                                        p.Sedes?.Where(s => s.Nombre.ToUpper() != "TODOS" && s.Nombre.ToUpper() != "TODAS") ?? new List<Sede>()
                                    );
                                    return p;
                                }) ?? new List<ProvinciaFrontend>()
                            );
                            return c;
                        })
                        .ToList();

                    // Limpiar la colección actual
                    LocalizacionesJerarquicas.Clear();

                    // Añadir las nuevas comunidades
                    if (comunidades != null)
                    {
                        //MessageBox.Show($"Comunidades tras filtro: {comunidades.Count}");
                        foreach (var comunidad in comunidades)
                        {
                            LocalizacionesJerarquicas.Add(comunidad);
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"Error al cargar las localizaciones: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las localizaciones: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ObtenerResumenSeleccion()
        {
            var resumen = new List<string>();
            foreach (var comunidad in LocalizacionesJerarquicas)
            {
                if (comunidad.IsChecked)
                    resumen.Add($"{comunidad.Nombre.ToUpper()}(C) |");
                else
                {
                    foreach (var provincia in comunidad.Provincias)
                    {
                        if (provincia.IsChecked)
                            resumen.Add($"{provincia.Nombre.ToUpper()}(P) |");
                        else
                        {
                            foreach (var sede in provincia.Sedes)
                            {
                                if (sede.IsChecked)
                                    resumen.Add($"{sede.Nombre.ToUpper()}(S) |");
                            }
                        }
                    }
                }
            }
            if (resumen.Count == 0)
                return "Todas";
            return string.Join(" ", resumen);
        }

        public BusquedaJurisprudencia()
        {
            InitializeComponent();
            ResultadosBusqueda = new ObservableCollection<JurisprudenciaResult>();
            LocalizacionesJerarquicas = new ObservableCollection<ComunidadAutonomaFrontend>();
            this.DataContext = this;

            // Inicializar brushes para el mesh gradient con los colores del tema actual
            CrearBrushesParaTema();
            IniciarAnimacionMesh();

            InitializeAnimations();
            AplicarModoSistema();

            if (client.BaseAddress == null)
            {
                client.BaseAddress = new Uri(ApiBaseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            // Cargar datos iniciales
            _ = CargarDatosInicialesAsync();

            // Inicializar comandos
            LimpiarCommand = new RelayCommand(EjecutarLimpiarFormulario);

            // Cargar localizaciones desde la API
            _ = CargarLocalizacionesDesdeApiAsync();
        }
    }

    // Clase para realizar cálculos matemáticos en bindings
    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string operation)
            {
                if (operation.StartsWith("*"))
                {
                    if (double.TryParse(operation.Substring(1), out double multiplier))
                    {
                        return doubleValue * multiplier;
                    }
                }
                else if (operation.StartsWith("/"))
                {
                    if (double.TryParse(operation.Substring(1), out double divisor))
                    {
                        return doubleValue / divisor;
                    }
                }
                else if (operation.StartsWith("+"))
                {
                    if (double.TryParse(operation.Substring(1), out double addend))
                    {
                        return doubleValue + addend;
                    }
                }
                else if (operation.StartsWith("-"))
                {
                    if (double.TryParse(operation.Substring(1), out double subtrahend))
                    {
                        return doubleValue - subtrahend;
                    }
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
