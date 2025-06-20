﻿using Polly;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TFG_V0._01.Helpers;
using TFG_V0._01.Supabase;
using TFG_V0._01.Supabase.Models;
using TFG_V0._01.Ventanas.SubVentanas;
using SupabaseCaso = TFG_V0._01.Supabase.Models.Caso;
using SupabaseTarea = TFG_V0._01.Supabase.Models.Tarea;
using System.Windows.Threading;

namespace TFG_V0._01.Ventanas
{
    public partial class Home : Window, INotifyPropertyChanged
    {
        #region 🎬 variables animacion
        private static Storyboard fadeInStoryboard;
        private static Storyboard shakeStoryboard;
        private static Storyboard meshAnimStoryboard;
        private static RadialGradientBrush mesh1Brush;
        private static RadialGradientBrush mesh2Brush;
        private DispatcherTimer progressTimer;
        private DateTime startTime;
        private readonly TimeSpan duration = TimeSpan.FromSeconds(2.0);

        static Home()
        {
            fadeInStoryboard = new Storyboard();
            shakeStoryboard = new Storyboard();
            meshAnimStoryboard = new Storyboard();
            mesh1Brush = new RadialGradientBrush();
            mesh2Brush = new RadialGradientBrush();
        }
        #endregion

        #region 📊 variables
        private readonly SupabaseAutentificacion _authService;
        private readonly SupabaseClientes _clientesService;
        private readonly SupabaseCasos _supabaseCasos;
        private readonly SupabaseEventosCitas _eventosCitasService;
        private readonly SupabaseDocumentos _documentosService;
        private readonly SupabaseTareas _tareasService;
        private readonly SupabaseEstadosEventos _estadosEventosService;
        private readonly SupabaseCasos _casosService;

        private ObservableCollection<Estado> _estadosDisponibles = new();
        private ObservableCollection<Tarea> _tareasPendientesLista = new();
        private ObservableCollection<CasoViewModel> _casosRecientesLista = new();
        private ObservableCollection<EventoViewModel> _eventosProximosLista = new();
        private ObservableCollection<DiaSemanaModel> _diasSemana = new();
        private ObservableCollection<Caso> _casosDisponibles = new();
        private ObservableCollection<EventoViewModel> _eventosDiaSeleccionado = new();
        private ObservableCollection<EventoViewModel> _eventosDeHoy = new();

        public ObservableCollection<Tarea> TareasPendientesLista
        {
            get => _tareasPendientesLista;
            set
            {
                _tareasPendientesLista = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CasoViewModel> CasosRecientesLista
        {
            get => _casosRecientesLista;
            set
            {
                _casosRecientesLista = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<EventoViewModel> EventosProximosLista
        {
            get => _eventosProximosLista;
            set
            {
                _eventosProximosLista = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DiaSemanaModel> DiasSemana
        {
            get => _diasSemana;
            set
            {
                _diasSemana = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Caso> CasosDisponibles
        {
            get => _casosDisponibles;
            set
            {
                _casosDisponibles = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<EventoViewModel> EventosDiaSeleccionado
        {
            get => _eventosDiaSeleccionado;
            set
            {
                _eventosDiaSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<EventoViewModel> EventosDeHoy
        {
            get => _eventosDeHoy;
            set
            {
                _eventosDeHoy = value;
                OnPropertyChanged();
            }
        }

        public ICommand VerDetallesCommand { get; }

        #region 📊 Propiedades
        private int _clientCount;
        private int _previousClientCount;
        private string _clientCountChange;
        private int _casosActivos;
        private int _documentos;
        private int _tareasPendientes;
        private int _casosRecientes;
        private int _eventosProximos;

        public int ClientCount
        {
            get => _clientCount;
            set
            {
                if (_clientCount != value)
                {
                    _previousClientCount = _clientCount;
                    _clientCount = value;
                    OnPropertyChanged();
                    UpdateClientCountChange();
                }
            }
        }

        public string ClientCountChange
        {
            get => _clientCountChange;
            set
            {
                if (_clientCountChange != value)
                {
                    _clientCountChange = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CasosActivos
        {
            get => _casosActivos;
            set
            {
                if (_casosActivos != value)
                {
                    _casosActivos = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Documentos
        {
            get => _documentos;
            set
            {
                if (_documentos != value)
                {
                    _documentos = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TareasPendientes
        {
            get => _tareasPendientes;
            set
            {
                if (_tareasPendientes != value)
                {
                    _tareasPendientes = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CasosRecientes
        {
            get => _casosRecientes;
            set
            {
                if (_casosRecientes != value)
                {
                    _casosRecientes = value;
                    OnPropertyChanged();
                }
            }
        }

        public int EventosProximos
        {
            get => _eventosProximos;
            set
            {
                if (_eventosProximos != value)
                {
                    _eventosProximos = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        private DateOnly fechaActual = DateOnly.FromDateTime(DateTime.Now);

        private string mesText;

        private string anio;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region 🎨 Modelo para el día de la semana
        public class DiaSemanaModel : INotifyPropertyChanged
        {
            public string Nombre { get; set; }
            public DateTime Fecha { get; set; }
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged(); }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region 🎨 Variables adicionales
        public ObservableCollection<string> EstadosDisponibles { get; set; } = new ObservableCollection<string> { "Pendiente", "En progreso", "Finalizado" };

        private DiaSemanaModel _diaSeleccionado;
        public DiaSemanaModel DiaSeleccionado
        {
            get => _diaSeleccionado;
            set
            {
                if (_diaSeleccionado != null) _diaSeleccionado.IsSelected = false;
                _diaSeleccionado = value;
                if (_diaSeleccionado != null) _diaSeleccionado.IsSelected = true;
                OnPropertyChanged();
                FiltrarEventosPorDia();
            }
        }

        public ICommand SeleccionarDiaCommand { get; set; }
        #endregion

        #region 🎨 Variables adicionales
        private int? _selectedCasoId;
        public int? SelectedCasoId
        {
            get => _selectedCasoId;
            set { _selectedCasoId = value; OnPropertyChanged(); }
        }
        #endregion

        #region ⚡ Inicializacion
        public Home()
        {
            InitializeComponent();
            this.DataContext = this;

            // Initialize all services
            _authService = new SupabaseAutentificacion();
            _clientesService = new SupabaseClientes();
            _supabaseCasos = new SupabaseCasos();
            _documentosService = new SupabaseDocumentos();
            _tareasService = new SupabaseTareas();
            _eventosCitasService = new SupabaseEventosCitas();
            _estadosEventosService = new SupabaseEstadosEventos();
            _casosService = new SupabaseCasos();

            // Initialize all collections
            TareasPendientesLista = new ObservableCollection<Tarea>();
            CasosRecientesLista = new ObservableCollection<CasoViewModel>();
            EventosProximosLista = new ObservableCollection<EventoViewModel>();
            DiasSemana = new ObservableCollection<DiaSemanaModel>();
            CasosDisponibles = new ObservableCollection<Caso>();
            EventosDiaSeleccionado = new ObservableCollection<EventoViewModel>();
            EventosDeHoy = new ObservableCollection<EventoViewModel>();
            _estadosDisponibles = new ObservableCollection<Estado>();

            // Load initial data
            CargarTareasPendientes();
            CargarIdioma(MainWindow.idioma);

            // Initialize UI
            InitializeAnimations();
            CrearFondoAnimado();
            AplicarModoSistema();
            BeginFadeInAnimation();

            // Initialize counters
            ClientCount = 0;
            ClientCountChange = "+0";
            CasosActivos = 0;
            Documentos = 0;
            TareasPendientes = 0;
            CasosRecientes = 0;
            EventosProximos = 0;
            mesText = string.Empty;
            anio = string.Empty;

            // Initialize commands and week
            SeleccionarDiaCommand = new RelayCommand<DateTime>(SeleccionarDia);
            InicializarSemanaActual();

            SelectedCasoId = null;
        }
        #endregion

        #region ⌛ Patalla de carga
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try 
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                StartLoadingAnimation();

                if (MainWindow.tipoBBDD)
                {
                    await Task.WhenAll(
                        CargarDatosDashboard(),
                        InicializarServiciosSupabase()
                    );
                }

                if (AddCasoControl != null)
                {
                    AddCasoControl.CasoGuardado += OnCasoGuardado;
                    AddCasoControl.CasoCancelado += OnCasoCancelado;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la carga inicial: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 🌓 Aplicar modo oscuro/claro cargado por sistema
        private void AplicarModoSistema()
        {
            this.Tag = MainWindow.isDarkTheme;
            var button = FindName("ThemeButton") as Button;
            var icon = button?.Template.FindName("ThemeIcon", button) as Image;

            // Cambiar fondo mesh gradient
            if (MainWindow.isDarkTheme)
            {
                if (icon != null)
                    icon.Source = new BitmapImage(new Uri("/TFG V0.01;component/Recursos/Iconos/sol.png", UriKind.Relative));
                // Colores mesh oscuro
                mesh1Brush.GradientStops[0].Color = (Color)ColorConverter.ConvertFromString("#8C7BFF");
                mesh1Brush.GradientStops[1].Color = (Color)ColorConverter.ConvertFromString("#08a693");
                mesh2Brush.GradientStops[0].Color = (Color)ColorConverter.ConvertFromString("#3a4d5f");
                mesh2Brush.GradientStops[1].Color = (Color)ColorConverter.ConvertFromString("#272c3f");
            }
            else
            {
                if (icon != null)
                    icon.Source = new BitmapImage(new Uri("/TFG V0.01;component/Recursos/Iconos/luna.png", UriKind.Relative));
                // Colores mesh claro
                mesh1Brush.GradientStops[0].Color = (Color)ColorConverter.ConvertFromString("#de9cb8");
                mesh1Brush.GradientStops[1].Color = (Color)ColorConverter.ConvertFromString("#9dcde1");
                mesh2Brush.GradientStops[0].Color = (Color)ColorConverter.ConvertFromString("#dc8eb8");
                mesh2Brush.GradientStops[1].Color = (Color)ColorConverter.ConvertFromString("#98d3ec");
            }

            // Crear nuevos estilos dinámicamente
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

            // Reemplazar los recursos existentes
            this.Resources["PrimaryTextStyle"] = primaryTextStyle;
            this.Resources["SecondaryTextStyle"] = secondaryTextStyle;

            navbar.ActualizarTema(MainWindow.isDarkTheme);
            IniciarAnimacionMesh();
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.isDarkTheme = !MainWindow.isDarkTheme;
            AplicarModoSistema();
            var fadeAnimation = CrearFadeAnimation(0.7, 0.9, 0.3, true);
            this.BeginAnimation(OpacityProperty, fadeAnimation);
        }
        #endregion

        #region 🎬  Animaciones

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

        #region 📅 Cambiar de fecha Calendario
        private static readonly string[] NombresMeses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

        private void CambiarMesCalendario(int incremento)
        {
            int nuevoMes = fechaActual.Month + incremento;
            int nuevoAnio = fechaActual.Year;

            if (nuevoMes < 1)
            {
                nuevoMes = 12;
                nuevoAnio--;
            }
            else if (nuevoMes > 12)
            {
                nuevoMes = 1;
                nuevoAnio++;
            }

            fechaActual = new DateOnly(nuevoAnio, nuevoMes, 1);
            ActualizarTextoFechaElegida();
        }

        private void mesAnterior(object sender, RoutedEventArgs e)
        {
            CambiarMesCalendario(-1);
        }

        private void mesSiguiente(object sender, RoutedEventArgs e)
        {
            CambiarMesCalendario(1);
        }

        private void mesActual(object sender, RoutedEventArgs e)
        {
            var fechaHoy = DateOnly.FromDateTime(DateTime.Now);

            if (fechaActual.Equals(fechaHoy))
            {
                if (sender is Button btn)
                    ShakeElement(btn);
            }
            else
            {
                fechaActual = fechaHoy;
                ActualizarTextoFechaElegida();
            }
        }

        private void ActualizarTextoFechaElegida()
        {
            mesText = NombresMeses[fechaActual.Month - 1];
            anio = fechaActual.Year.ToString();
        }
        #endregion

        #region 🈳 Idioma
        private void CargarIdioma(int idioma)
        {
            navbar.ActualizarIdioma(idioma);

            var idiomas = new (string Titulo, string Subtitulo, string ResumenCasos, string ResumenClientes, string ResumenDocumentos, string ResumenEventos,
                string Lunes, string Martes, string Miercoles, string Jueves, string Viernes, string Sabado, string Domingo,
                string ListaTareas, string BtnAñadirTarea, string BtnVerTodosCasos, string CasosRecientes, string NCasos, string CCliente, string CTipo, string CEstado, string CAcciones, string Version, string Hoy,
                string SemanaActual, string EventosDiaSeleccionado, string Documentos, string ProximosEventos, string CasoAsociado, string TituloTarea, string DescripcionTarea, 
                string FechaVencimiento, string Prioridad, string EstadoTarea, string AñadirTarea, string Copyright)[]
            {
                ("Panel de control.", "Bienvenido a la aplicación de gestión de casos. Se encuentra en el Dashboard de la aplicacion.",
                 "Casos Activos:", "Clientes:", "Documentos:", "Eventos Póximos:",
                 "Lun", "Mar", "Mie", "Jue", "Vie", "Sab", "Dom",
                 "Tareas Pendientes", "Añadir Tarea", "Ver todos los casos", "Casos Recientes", "Nº Caso", "Cliente", "Tipo", "Estado", "Acciones", "Versión: ", "Hoy",
                 "Semana actual", "Eventos del día seleccionado", "Documentos", "Próximos Eventos", "Caso asociado:", "Título:", "Descripción:", 
                 "Fecha de vencimiento:", "Prioridad:", "Estado:", "Añadir Tarea", "© 2025 TFG"),
                ("Dashboard", "Welcome to the case management application. You are on the application's dashboard.",
                 "Active Cases:", "Clients:", "Documents:", "Upcoming Events:",
                 "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun",
                 "Pending Tasks", "Add Task", "View All Cases", "Recent Cases", "Case No.", "Client", "Type", "Status", "Actions", "Version: ", "Today",
                 "Current Week", "Selected Day Events", "Documents", "Upcoming Events", "Associated Case:", "Title:", "Description:", 
                 "Due Date:", "Priority:", "Status:", "Add Task", "© 2025 TFG"),
                ("Panell de control", "Benvingut a l'aplicació de gestió de casos. Estàs al panell de l'aplicació.",
                 "Casos Actius:", "Clients:", "Documents:", "Esdeveniments propers:",
                 "Dll", "Dt", "Dc", "Dj", "Dv", "Ds", "Dg",
                 "Tasques pendents", "Afegir tasca", "Veure tots els casos", "Casos recents", "Nº Cas", "Client", "Tipus", "Estat", "Accions", "Versió: ", "Avui",
                 "Setmana actual", "Esdeveniments del dia seleccionat", "Documents", "Esdeveniments propers", "Cas associat:", "Títol:", "Descripció:", 
                 "Data de venciment:", "Prioritat:", "Estat:", "Afegir tasca", "© 2025 TFG"),
                ("Panel de control", "Benvido á aplicación de xestión de casos. Estás no panel da aplicación.",
                 "Casos activos:", "Clientes:", "Documentos:", "Eventos próximos:",
                 "Lun", "Mar", "Mér", "Xov", "Ven", "Sáb", "Dom",
                 "Tarefas pendentes", "Engadir tarefa", "Ver todos os casos", "Casos recentes", "Nº Caso", "Cliente", "Tipo", "Estado", "Accións", "Versión: ", "Hoxe",
                 "Semana actual", "Eventos do día seleccionado", "Documentos", "Eventos próximos", "Caso asociado:", "Título:", "Descrición:", 
                 "Data de vencemento:", "Prioridade:", "Estado:", "Engadir tarefa", "© 2025 TFG"),
                ("Kontrol panela", "Ongi etorri kasuen kudeaketa aplikaziora. Aplikazioaren panel nagusian zaude.",
                 "Kasuan aktiboak:", "Bezeroak:", "Dokumentuak:", "Hurrengo ekitaldiak:",
                 "Al", "Ar", "Az", "Og", "Or", "La", "Ig",
                 "Zain dauden zereginak", "Zeregina gehitu", "Kasu guztiak ikusi", "Azken kasuak", "Kasua Nº", "Bezeroa", "Mota", "Egoera", "Ekintzak", "Bertsioa: ", "Gaur",
                 "Aste unekoa", "Aukeratutako eguneko ekitaldiak", "Dokumentuak", "Hurrengo ekitaldiak", "Lotutako kasua:", "Izenburua:", "Deskribapena:", 
                 "Epemuga:", "Lehentasuna:", "Egoera:", "Zeregina gehitu", "© 2025 TFG")
            };

            if (idioma < 0 || idioma >= idiomas.Length)
                idioma = 0;

            var t = idiomas[idioma];

            titulo.Text = t.Titulo;
            subtitulo.Text = t.Subtitulo;
            resumenCasos.Text = t.ResumenCasos;
            resumenClientes.Text = t.ResumenClientes;
            resumenDocumentos.Text = t.ResumenDocumentos;
            resumenEventos.Text = t.ResumenEventos;
            listaTareas.Text = t.ListaTareas;
            btnAñadirTarea.Content = t.BtnAñadirTarea;
            casosRecientes.Text = t.CasosRecientes;
            txtNcaso.Text = t.NCasos;
            txtCliente.Text = t.CCliente;
            txtTipo.Text = t.CTipo;
            txtEstado.Text = t.CEstado;
            txtAcciones.Text = t.CAcciones;
            txtVersion.Text = t.Version;
            hoy.Text = t.Hoy;

            // Nuevos TextBlock
            txtSemanaActual.Text = t.SemanaActual;
            txtEventosDiaSeleccionado.Text = t.EventosDiaSeleccionado;
            txtDocumentos.Text = t.Documentos;
            txtProximosEventos.Text = t.ProximosEventos;
            txtCasoAsociado.Text = t.CasoAsociado;
            txtTituloTarea.Text = t.TituloTarea;
            txtDescripcionTarea.Text = t.DescripcionTarea;
            txtFechaVencimiento.Text = t.FechaVencimiento;
            txtPrioridad.Text = t.Prioridad;
            txtEstadoTarea.Text = t.EstadoTarea;
            txtAñadirTarea.Text = t.AñadirTarea;
            txtCopyright.Text = t.Copyright;
        }
        #endregion

        #region 🎨 Fondo Animado
        private void CrearFondoAnimado()
        {
            // Crear los brushes
            mesh1Brush = new RadialGradientBrush();
            mesh1Brush.Center = new Point(0.3, 0.3);
            mesh1Brush.RadiusX = 0.5;
            mesh1Brush.RadiusY = 0.5;
            mesh1Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#de9cb8"), 0));
            mesh1Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#9dcde1"), 1));
            mesh1Brush.Freeze();
            mesh1Brush = mesh1Brush.Clone();

            mesh2Brush = new RadialGradientBrush();
            mesh2Brush.Center = new Point(0.7, 0.7);
            mesh2Brush.RadiusX = 0.6;
            mesh2Brush.RadiusY = 0.6;
            mesh2Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#dc8eb8"), 0));
            mesh2Brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#98d3ec"), 1));
            mesh2Brush.Freeze();
            mesh2Brush = mesh2Brush.Clone();

            // Crear el DrawingBrush
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(new GeometryDrawing(mesh1Brush, null, new RectangleGeometry(new Rect(0, 0, 1, 1))));
            drawingGroup.Children.Add(new GeometryDrawing(mesh2Brush, null, new RectangleGeometry(new Rect(0, 0, 1, 1))));
            meshGradientBrush = new DrawingBrush(drawingGroup) { Stretch = Stretch.Fill };
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

        #region Animación de Carga
        private void StartLoadingAnimation()
        {
            startTime = DateTime.Now;
            progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps para animación suave
            };
            progressTimer.Tick += UpdateProgressArc;
            progressTimer.Start();
        }

        private void UpdateProgressArc(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            double progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
            double angle = progress * 360;

            UpdateArc(angle);

            // Actualizar texto de porcentaje
            int percentage = (int)(progress * 100);
            PercentageText.Text = $"{percentage}%";

            if (progress >= 1.0)
            {
                progressTimer.Stop();

                // Animación de finalización
                DoubleAnimation fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(0.5)
                };

                fadeOut.Completed += (s, args) => LoadingOverlay.Visibility = Visibility.Collapsed;
                LoadingOverlay.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void UpdateArc(double angle)
        {
            double radius = 90;
            double centerX = 100;
            double centerY = 100;

            double startAngle = -90; // Comenzar desde arriba
            double endAngle = startAngle + angle;

            double startRadians = (Math.PI / 180) * startAngle;
            double endRadians = (Math.PI / 180) * endAngle;

            double startX = centerX + radius * Math.Cos(startRadians);
            double startY = centerY + radius * Math.Sin(startRadians);

            double endX = centerX + radius * Math.Cos(endRadians);
            double endY = centerY + radius * Math.Sin(endRadians);

            // Actualizar la figura del arco
            ProgressFigure.StartPoint = new Point(startX, startY);
            ProgressArc.Point = new Point(endX, endY);
            ProgressArc.IsLargeArc = angle > 180;
        }
        #endregion

        #region ☁ SUPABASE
        private async Task InicializarServiciosSupabase()
        {
            try
            {
                await Task.WhenAll(
                    _documentosService.InicializarAsync(),
                    _tareasService.InicializarAsync(),
                    _eventosCitasService.InicializarAsync()
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar servicios de Supabase: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CargarDatosDashboard()
        {
            try
            {
                await InicializarServiciosSupabase();

                // Usar los métodos manuales para traer todos los registros
                var clientesTask = _clientesService.ObtenerTodosClientesManualAsync();
                var casosTask = _supabaseCasos.ObtenerTodosAsync();
                var documentosTask = _documentosService.ObtenerTodosDocumentosManualAsync();
                var tareasTask = _tareasService.ObtenerTodosAsync();
                var eventosCitasTask = _eventosCitasService.ObtenerTodosEventosManualAsync();

                // Esperar a que todas las tareas se completen
                await Task.WhenAll(clientesTask, casosTask, documentosTask, tareasTask, eventosCitasTask);

                // Procesar clientes
                var clientes = await clientesTask;
                ClientCount = clientes?.Count ?? 0;
                var fechaLimite = DateTime.Now.AddDays(-28);
                var clientesNuevos = clientes?.Where(c => c.fecha_contrato >= fechaLimite).Count() ?? 0;
                ClientCountChange = $"+{clientesNuevos}";

                // Procesar casos
                var casos = await casosTask;
                CasosActivos = casos?.Count ?? 0;
                var casosNuevos = casos?.Where(c => c.fecha_inicio >= fechaLimite).Count() ?? 0;
                scoreCasos.Text = $"+{casosNuevos}";

                // Procesar documentos (sin límite)
                var documentos = await documentosTask;
                Documentos = documentos?.Count ?? 0;
                var documentosNuevos = documentos?.Where(d => d.fecha_subid >= fechaLimite).Count() ?? 0;
                scoreDocumentos.Text = $"+{documentosNuevos}";

                // Procesar tareas
                var tareas = await tareasTask;
                var tareasPendientes = tareas.Where(t => t.estado != "Completada").ToList();
                TareasPendientes = tareasPendientes.Count();
                TareasPendientesLista.Clear();
                foreach (var tarea in tareasPendientes)
                    TareasPendientesLista.Add(tarea);

                // Procesar casos recientes
                CasosRecientesLista.Clear();
                var fechaLimiteCasos = DateTime.Now.Date.AddDays(-28);
                var casosRecientes = await _supabaseCasos.ObtenerTodosAsync();
                var casosFiltrados = casosRecientes
                    .Where(caso => caso.fecha_inicio.Date >= fechaLimiteCasos)
                    .OrderByDescending(caso => caso.fecha_inicio);
                foreach (var caso in casosFiltrados)
                {
                    CasosRecientesLista.Add(new CasoViewModel
                    {
                        id = caso.id,
                        referencia = caso.referencia,
                        nombre_cliente = caso.nombre_cliente,
                        tipo_nombre = caso.tipo_nombre,
                        estado = caso.estado_nombre,
                        estado_color = ObtenerColorEstadoCaso(caso.estado_nombre)
                    });
                }
                CasosRecientes = CasosRecientesLista.Count;

                // Procesar eventos del día actual (solo para hoy)
                var eventosCitas = await eventosCitasTask;
                var hoy = DateTime.Now.Date;
                var eventosHoy = eventosCitas
                    .Where(e => e.Fecha.Date == hoy)
                    .Count();
                EventosProximos = eventosHoy;
                if (ProxEventos != null)
                {
                    ProxEventos.Text = $"+{eventosHoy}";
                }

                // Llenar la colección de todos los eventos para la semana
                var colores = new[] { "#1976D2", "#43A047", "#FFA726", "#E53935", "#8E24AA", "#00ACC1", "#FDD835", "#F4511E", "#3949AB", "#00897B" };
                var random = new Random();
                EventosProximosLista.Clear();
                var estados = await _estadosEventosService.ObtenerEstadosEventos();
                foreach (var evento in eventosCitas)
                {
                    var eventoViewModel = new EventoViewModel
                    {
                        Id = evento.Id,
                        Titulo = evento.Titulo,
                        Descripcion = evento.Descripcion,
                        Fecha = evento.Fecha,
                        EstadoNombre = estados.FirstOrDefault(s => s.Id == evento.IdEstado)?.Nombre ?? "Sin estado",
                        EstadoColor = ObtenerColorEstado(estados.FirstOrDefault(s => s.Id == evento.IdEstado)?.Nombre),
                        FechaInicio = evento.FechaInicio,
                        IdCaso = evento.IdCaso,
                        NombreCaso = casos.FirstOrDefault(c => c.id == evento.IdCaso)?.titulo ?? "Sin caso"
                    };
                    EventosProximosLista.Add(eventoViewModel);
                }
                FiltrarEventosPorDia();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos del dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CargarScoreCasos()
        {
            try
            {
                await _supabaseCasos.InicializarAsync();
                var casos = await _supabaseCasos.ObtenerTodosAsync();
                var fechaLimite = DateTime.Now.AddDays(-28);

                // Casos activos creados en los últimos 28 días y que no estén cerrados
                var casosActivosPeriodo = casos
                    .Where(c => c.fecha_inicio >= fechaLimite && c.estado_nombre != "Cerrado")
                    .ToList();

                CasosActivos = casosActivosPeriodo.Count;

                // Contador de nuevos casos activos (mismo filtro)
                UpdateCasosCountChange(casosActivosPeriodo.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar casos: {ex.Message}");
            }
        }

        private async Task CargarScoreDocumentos()
        {
            try
            {
                await _documentosService.InicializarAsync();
                var documentos = await _documentosService.ObtenerTodosDocumentosManualAsync();
                var fechaLimite = DateTime.Now.AddDays(-28);

                // Filtrar documentos subidos en los últimos 28 días
                var documentosNuevos = documentos.Where(d => d.fecha_subid >= fechaLimite).ToList();

                // Obtener todos los casos para verificar el estado
                await _supabaseCasos.InicializarAsync();
                var casos = await _supabaseCasos.ObtenerTodosAsync();
                var casosNoCerrados = casos.Where(c => c.estado_nombre != "Cerrado").Select(c => c.id).ToHashSet();

                // Solo contar documentos cuyo caso asociado no esté cerrado
                var documentosNuevosActivos = documentosNuevos.Where(d => casosNoCerrados.Contains(d.id_caso)).ToList();

                Documentos = documentosNuevosActivos.Count;
                UpdateDocumentosCountChange(documentosNuevosActivos.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar documentos: {ex.Message}");
            }
        }

        private async Task CargarCasosRecientes()
        {
            try
            {
                await _supabaseCasos.InicializarAsync();
                var casos = await _supabaseCasos.ObtenerTodosAsync();

                var fechaLimite = DateTime.Now.Date.AddDays(-28);

                var casosRecientes = casos
                    .Where(c => c.fecha_inicio.Date >= fechaLimite)
                    .OrderByDescending(c => c.fecha_inicio)
                    .ToList();

                CasosRecientesLista.Clear();
                foreach (var caso in casosRecientes)
                {
                    CasosRecientesLista.Add(new CasoViewModel
                    {
                        id = caso.id,
                        referencia = caso.referencia,
                        nombre_cliente = caso.nombre_cliente,
                        tipo_nombre = caso.tipo_nombre,
                        estado = caso.estado_nombre,
                        estado_color = ObtenerColorEstadoCaso(caso.estado_nombre)
                    });
                }
                CasosRecientes = CasosRecientesLista.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los casos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ActualizarTarea(SupabaseTarea tarea)
        {
            try
            {
                await _tareasService.InicializarAsync();
                var updateDto = new TFG_V0._01.Supabase.Models.TareaUpdateDto
                {
                    titulo = tarea.titulo,
                    descripcion = tarea.descripcion,
                    fecha_creacion = tarea.fecha_creacion,
                    fecha_fin = tarea.fecha_fin,
                    id_caso = tarea.id_caso,
                    prioridad = tarea.prioridad,
                    estado = tarea.estado
                };
                await _tareasService.ActualizarTarea(tarea.id.Value, updateDto);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar la tarea: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EliminarCaso(int id)
        {
            try
            {
                await _supabaseCasos.EliminarAsync(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar el caso: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateClientCountChange()
        {
            var fechaLimite = DateTime.Now.AddDays(-28);
            int diferencia = _clientCount - _previousClientCount;

            // Solo mostrar la diferencia si es reciente (últimos 28 días)
            if (diferencia > 0)
            {
                ClientCountChange = $"+{diferencia}";
            }
            else
            {
                ClientCountChange = "+0";
            }
        }

        private void UpdateCasosCountChange(int nuevosCasos)
        {
            if (nuevosCasos > 0)
            {
                scoreCasos.Text = $"+{nuevosCasos}";
            }
            else
            {
                scoreCasos.Text = "+0";
            }
        }

        private void UpdateDocumentosCountChange(int nuevosDocumentos)
        {
            if (nuevosDocumentos > 0)
            {
                scoreDocumentos.Text = $"+{nuevosDocumentos}";
            }
            else
            {
                scoreDocumentos.Text = "+0";
            }
        }

        private string ObtenerColorEstadoCaso(string estado)
        {
            return estado?.ToLower() switch
            {
                "abierto" => "#2196F3", // Azul
                "en proceso" => "#4CAF50", // Verde
                "cerrado" => "#F44336", // Rojo
                "pendiente" => "#FF9800", // Naranja
                "revisado" => "#9E9E9E", // Gris
                _ => "#9E9E9E"  // Gris por defecto
            };
        }

        private async void CheckBox_TareaFinalizada(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is SupabaseTarea tarea)
            {
                try
                {
                    if (checkBox.IsChecked == true)
                    {
                        if (tarea.estado != "Completada")
                            tarea.estadoAnterior = tarea.estado;
                        tarea.estado = "Completada";
                    }
                    else
                    {
                        tarea.estado = !string.IsNullOrEmpty(tarea.estadoAnterior) ? tarea.estadoAnterior : "Pendiente";
                    }
                    await ActualizarTarea(tarea);
                    // No refrescar la lista aquí, para que la tarea siga visible hasta que el usuario refresque manualmente o cambie de ventana
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar la tarea: {ex.Message}");
                    checkBox.IsChecked = !checkBox.IsChecked; // Revertir el cambio
                }
            }
        }

        private void VerTodosCasos_Click(object sender, RoutedEventArgs e)
        {
            var casosWindow = new Casos();
            casosWindow.Show();
            this.Close();
        }

        private void EditarCaso_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CasoViewModel caso)
            {
                var casosWindow = new Casos(caso.id);
                casosWindow.Show();
                this.Close();
            }
        }
        private async void MostrarDetallesCaso_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CasoViewModel casoVM)
            {
                try
                {
                    await _supabaseCasos.InicializarAsync();
                    // Obtener el caso con sus relaciones
                    var caso = await _supabaseCasos.ObtenerPorIdAsync(casoVM.id);
                    if (caso == null)
                    {
                        MessageBox.Show("El caso no se encontró en la base de datos.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Actualizar la información básica del caso en el panel
                    PopupTitulo.Text = $"Caso #{caso.referencia}";
                    PopupDescripcion.Text = caso.descripcion;
                    PopupCliente.Text = $"Cliente: {caso.Cliente?.nombre ?? "No especificado"}";
                    PopupTipo.Text = $"Tipo: {caso.TipoCaso?.nombre ?? "No especificado"}";
                    PopupEstado.Text = $"Estado: {caso.Estado?.nombre ?? "No especificado"}";
                    PopupFecha.Text = $"Fecha de inicio: {caso.fecha_inicio:dd/MM/yyyy}";

                    // Obtener y mostrar los documentos relacionados
                    await _documentosService.InicializarAsync();
                    var documentos = await _documentosService.ObtenerPorCasoAsync(caso.id);
                    PopupDocumentos.ItemsSource = documentos;

                    // Obtener y filtrar los próximos eventos
                    await _eventosCitasService.InicializarAsync();
                    var eventos = await _eventosCitasService.ObtenerEventosCitasPorCaso(caso.id);
                    var proximosEventos = eventos
                        .Where(e => e.Fecha >= DateTime.Now)
                        .OrderBy(e => e.Fecha)
                        .Take(3)
                        .Select(e => new
                        {
                            titulo = e.Titulo,
                            fecha = e.Fecha.ToString("dd/MM/yyyy HH:mm"),
                            descripcion = e.Descripcion
                        })
                        .ToList();
                    PopupEventos.ItemsSource = proximosEventos;

                    // Mostrar el panel deslizante
                    ShowSlidePanelDetalles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar los detalles del caso: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowSlidePanelDetalles()
        {
            SlidePanelDetalles.Visibility = Visibility.Visible;
            OverlayPanel.Visibility = Visibility.Visible;
            DoubleAnimation slideInAnimation = new DoubleAnimation
            {
                From = 400,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            SlidePanelDetallesTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
        }

        private void HideSlidePanelDetalles()
        {
            DoubleAnimation slideOutAnimation = new DoubleAnimation
            {
                From = 0,
                To = 400,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slideOutAnimation.Completed += (s, e) =>
            {
                SlidePanelDetalles.Visibility = Visibility.Collapsed;
                OverlayPanel.Visibility = Visibility.Collapsed;
            };

            SlidePanelDetallesTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
        }

        private void OverlayPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Ocultar todos los paneles si están visibles
            if (SlidePanel.Visibility == Visibility.Visible)
            {
                HideSlidePanel();
            }
            if (SlidePanelCliente.Visibility == Visibility.Visible)
            {
                HideSlidePanelCliente();
            }
            if (SlidePanelDetalles.Visibility == Visibility.Visible)
            {
                HideSlidePanelDetalles();
            }
            if (SlidePanelTarea.Visibility == Visibility.Visible)
            {
                CerrarPanelTarea();
            }
        }

        // Método para manejar el guardado del caso
        private void OnCasoGuardado(object sender, EventArgs e)
        {
            HideSlidePanel();
            // Aquí puedes añadir la lógica para actualizar la lista de casos si es necesario
        }

        // Método para manejar la cancelación
        private void OnCasoCancelado(object sender, EventArgs e)
        {
            HideSlidePanel();
        }

        private void btnAddCliente_Click(object sender, RoutedEventArgs e)
        {
            ShowSlidePanelCliente();
        }

        private void ShowSlidePanelCliente()
        {
            SlidePanelCliente.Visibility = Visibility.Visible;
            OverlayPanel.Visibility = Visibility.Visible;

            // Suscribirse a los eventos del control
            AddClienteControl.ClienteGuardado += OnClienteGuardado;
            AddClienteControl.ClienteCancelado += OnClienteCancelado;

            var slideInAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            SlidePanelClienteTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
        }

        private void HideSlidePanelCliente()
        {
            // Desuscribirse de los eventos del control
            if (AddClienteControl != null)
            {
                AddClienteControl.ClienteGuardado -= OnClienteGuardado;
                AddClienteControl.ClienteCancelado -= OnClienteCancelado;
            }

            var slideOutAnimation = new DoubleAnimation
            {
                To = 400,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slideOutAnimation.Completed += (s, e) =>
            {
                SlidePanelCliente.Visibility = Visibility.Collapsed;
                OverlayPanel.Visibility = Visibility.Collapsed;
            };

            SlidePanelClienteTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
        }

        // Método para manejar el guardado del cliente
        private void OnClienteGuardado(object sender, EventArgs e)
        {
            HideSlidePanelCliente();
            // Aquí puedes añadir la lógica para actualizar la lista de clientes si es necesario
        }

        // Método para manejar la cancelación
        private void OnClienteCancelado(object sender, EventArgs e)
        {
            HideSlidePanelCliente();
        }

        // Manejadores de eventos para el efecto hover en botones de añadir
        private void btnAddCaso_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                ShakeElement(btn);
            }
        }

        private void btnAddCaso_MouseLeave(object sender, MouseEventArgs e)
        {
            // No es necesario hacer nada al salir del hover para este efecto
        }

        private void btnAddCliente_MouseEnter(object sender, MouseEventArgs e)
        {
             if (sender is Button btn)
            {
                ShakeElement(btn);
            }
        }

        private void btnAddCliente_MouseLeave(object sender, MouseEventArgs e)
        {
             // No es necesario hacer nada al salir del hover para este efecto
        }

        /*
        public async Task<Caso> ObtenerPorIdAsync(int id)
        {
            return await _context.Casos
                .Include(c => c.Documentos)
                .Include(c => c.Cliente)
                .Include(c => c.Tipo)
                .Include(c => c.Estado)
                // Agrega más Includes si tienes más relaciones
                .FirstOrDefaultAsync(c => c.id == id);
        }
        */

        #endregion

        #region 🐞 Resvisiones Bugs 
        //revisar funcion CheckBox_TareaFinalizada => no funciona al 100% en local.
        #endregion

        #region 💥 Cambiar Mes
        // Implementación de los manejadores de eventos
        private void btnMesAnterior_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                ShakeElement(element);
            }
        }

        private void btnMesAnterior_MouseLeave(object sender, MouseEventArgs e)
        {
            // No necesitas hacer nada al quitar el ratón si la animación no es repetitiva
        }

        private void btnMesSiguiente_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                ShakeElement(element);
            }
        }

        private void btnMesSiguiente_MouseLeave(object sender, MouseEventArgs e)
        {
            // No necesitas hacer nada al quitar el ratón si la animación no es repetitiva
        }

        // Variables para la semana mostrada
        private DateTime _lunesSemanaMostrada = DateTime.Today;

        private void SemanaAnterior_Click(object sender, RoutedEventArgs e)
        {
            _lunesSemanaMostrada = _lunesSemanaMostrada.AddDays(-7);
            ActualizarSemanaMostrada();
        }

        private void SemanaSiguiente_Click(object sender, RoutedEventArgs e)
        {
            _lunesSemanaMostrada = _lunesSemanaMostrada.AddDays(7);
            ActualizarSemanaMostrada();
        }

        private void SemanaHoy_Click(object sender, RoutedEventArgs e)
        {
            _lunesSemanaMostrada = DateTime.Today.AddDays(-(int)(DateTime.Today.DayOfWeek == 0 ? 6 : DateTime.Today.DayOfWeek - DayOfWeek.Monday));
            ActualizarSemanaMostrada();
            DiaSeleccionado = DiasSemana.FirstOrDefault(d => d.Fecha.Date == DateTime.Today.Date) ?? DiasSemana[0];
        }

        private void ActualizarSemanaMostrada()
        {
            DiasSemana.Clear();
            string[] nombres = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
            for (int i = 0; i < 7; i++)
            {
                var fecha = _lunesSemanaMostrada.AddDays(i);
                DiasSemana.Add(new DiaSemanaModel
                {
                    Nombre = nombres[i] + " " + fecha.Day,
                    Fecha = fecha,
                    IsSelected = false
                });
            }
            SemanaActualRango.Text = $"{_lunesSemanaMostrada:dd/MM} - {_lunesSemanaMostrada.AddDays(6):dd/MM}";
            // Si el día seleccionado no está en la semana, selecciona el lunes
            if (!DiasSemana.Any(d => d.Fecha.Date == DiaSeleccionado?.Fecha.Date))
                DiaSeleccionado = DiasSemana[0];
            FiltrarEventosPorDia();
        }

        // Modificar InicializarSemanaActual para usar _lunesSemanaMostrada
        private void InicializarSemanaActual()
        {
            _lunesSemanaMostrada = DateTime.Today.AddDays(-(int)(DateTime.Today.DayOfWeek == 0 ? 6 : DateTime.Today.DayOfWeek - DayOfWeek.Monday));
            ActualizarSemanaMostrada();
            DiaSeleccionado = DiasSemana.FirstOrDefault(d => d.Fecha.Date == DateTime.Today.Date) ?? DiasSemana[0];
        }

        private async void FiltrarEventosPorDia()
        {
            try
            {
                if (EventosDiaSeleccionado == null)
                {
                    EventosDiaSeleccionado = new ObservableCollection<EventoViewModel>();
                }
                else
                {
                    EventosDiaSeleccionado.Clear();
                }

                if (DiaSeleccionado == null) return;

                // Ensure services are initialized
                if (_eventosCitasService == null || _estadosEventosService == null || _casosService == null)
                {
                    await InicializarServiciosSupabase();
                }

                // Initialize services if needed
                await _eventosCitasService.InicializarAsync();
                await _estadosEventosService.InicializarAsync();
                await _casosService.InicializarAsync();

                var eventos = await _eventosCitasService.ObtenerEventosCitas();
                var estados = await _estadosEventosService.ObtenerEstadosEventos();
                var casos = await _casosService.ObtenerTodosCasosManualAsync();

                if (eventos == null || estados == null || casos == null)
                {
                    MessageBox.Show("Error al cargar los datos necesarios.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var eventosDelDia = eventos
                    .Where(e => e != null && e.Fecha.Date == DiaSeleccionado.Fecha.Date)
                    .OrderBy(e => e.FechaInicio)
                    .GroupBy(e => e.Id) // Agrupa por ID para evitar duplicados
                    .Select(g => g.First()) // Toma solo uno por ID
                    .Select(e => new EventoViewModel
                    {
                        Id = e.Id,
                        Titulo = e.Titulo ?? "Sin título",
                        Descripcion = e.Descripcion ?? "Sin descripción",
                        Fecha = e.Fecha,
                        EstadoNombre = estados.FirstOrDefault(s => s != null && s.Id == e.IdEstado)?.Nombre ?? "Sin estado",
                        EstadoColor = ObtenerColorEstado(estados.FirstOrDefault(s => s != null && s.Id == e.IdEstado)?.Nombre),
                        FechaInicio = e.FechaInicio,
                        IdCaso = e.IdCaso,
                        NombreCaso = casos.FirstOrDefault(c => c != null && c.id == e.IdCaso)?.titulo ?? "Sin caso"
                    })
                    .ToList();

                foreach (var evento in eventosDelDia)
                {
                    EventosDiaSeleccionado.Add(evento);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al filtrar eventos por día: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 🛠️ Métodos de Colores
        private string ObtenerColorEstado(string estado)
        {
            return estado?.ToLower() switch
            {
                "finalizado" => "#F44336",   // Rojo
                "programado" => "#FFA726",   // Naranja
                "cancelado" => "#757575",    // Gris
                _ => "#BDBDBD"              // Gris claro por defecto
            };
        }
        #endregion

        #region 🗓️ Selección de Día
        private void SeleccionarDia(DateTime fecha)
        {
            var dia = DiasSemana.FirstOrDefault(d => d.Fecha.Date == fecha.Date);
            if (dia != null)
                DiaSeleccionado = dia;
        }
        #endregion

        #region 🎬 Animaciones de Paneles
        private void HideSlidePanel()
        {
            DoubleAnimation slideOutAnimation = new DoubleAnimation
            {
                From = 0,
                To = 400,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slideOutAnimation.Completed += (s, e) =>
            {
                SlidePanel.Visibility = Visibility.Collapsed;
                OverlayPanel.Visibility = Visibility.Collapsed;
            };

            SlidePanelTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
        }

        private void btnAddCaso_Click(object sender, RoutedEventArgs e)
        {
            ShowSlidePanel();
        }

        private void ShowSlidePanel()
        {
            SlidePanel.Visibility = Visibility.Visible;
            OverlayPanel.Visibility = Visibility.Visible;
            DoubleAnimation slideInAnimation = new DoubleAnimation
            {
                From = 400,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            SlidePanelTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
        }
        #endregion

        #region 🗑️ Eliminar Caso
        private async void EliminarCaso_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CasoViewModel caso)
            {
                var result = MessageBox.Show("¿Estás seguro de que deseas eliminar este caso?", "Confirmar eliminación",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await EliminarCaso(caso.id);
                        await CargarCasosRecientes();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar el caso: {ex.Message}");
                    }
                }
            }
        }
        #endregion

        #region 📝 Panel de Tareas
        private async void btnAñadirTarea_Click(object sender, RoutedEventArgs e)
        {
            // Configurar el grid
            SlidePanelTarea.Visibility = Visibility.Visible;
            OverlayPanel.Visibility = Visibility.Visible;

            // Configurar prioridades
            cbPrioridadTarea.ItemsSource = new[] { "Alta", "Media", "Baja" };
            cbPrioridadTarea.SelectedIndex = -1; // Ninguna seleccionada

            // Configurar estados
            cbEstadoTarea.ItemsSource = new[] { "Pendiente", "En progreso", "Completada" };
            cbEstadoTarea.SelectedIndex = -1; // Ninguna seleccionada

            // Limpiar campos
            txtTituloTarea.Text = "";
            txtDescripcionTarea.Text = "";
            dpFechaVencimientoTarea.SelectedDate = DateTime.Now.AddDays(7); // Una semana por defecto

            // Animar la entrada
            var animation = new DoubleAnimation
            {
                From = 400,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            SlidePanelTareaTransform.BeginAnimation(TranslateTransform.XProperty, animation);

            await CargarCasosDisponibles();
            SelectedCasoId = null;
        }

        private void btnCancelarTarea_Click(object sender, RoutedEventArgs e)
        {
            CerrarPanelTarea();
        }

        private void CerrarPanelTarea()
        {
            // Crear y ejecutar la animación de cierre
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 400,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            
            animation.Completed += (s, e) =>
            {
                SlidePanelTarea.Visibility = Visibility.Collapsed;
                OverlayPanel.Visibility = Visibility.Collapsed;
            };
            
            SlidePanelTareaTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private async Task CargarTareasPendientes()
        {
            try
            {
                await _tareasService.InicializarAsync();
                var tareas = await _tareasService.ObtenerTareasPendientes();
                // Inicializar la propiedad 'completada' según el estado
                foreach (var tarea in tareas)
                {
                    tarea.completada = tarea.estado == "Completada";
                }
                TareasPendientesLista.Clear();
                foreach (var tarea in tareas)
                {
                    TareasPendientesLista.Add(tarea);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las tareas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TareaCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is Tarea tarea)
            {
                try
                {
                    tarea.estado = checkBox.IsChecked == true ? "Completada" : "Pendiente";
                    var updateDto = new TFG_V0._01.Supabase.Models.TareaUpdateDto
                    {
                        titulo = tarea.titulo,
                        descripcion = tarea.descripcion,
                        fecha_creacion = tarea.fecha_creacion,
                        fecha_fin = tarea.fecha_fin,
                        id_caso = tarea.id_caso,
                        prioridad = tarea.prioridad,
                        estado = tarea.estado
                    };
                    await _tareasService.ActualizarTarea(tarea.id.Value, updateDto);
                    await Task.Run(() => CargarTareasPendientes()); // Wrap the void method in Task.Run
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar el estado de la tarea: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Revertir el cambio en caso de error
                    tarea.completada = !tarea.completada;
                }
            }
        }

        private void ResetFieldsTarea()
        {
            cbCasoTarea.SelectedIndex = -1;
            txtTituloTarea.Text = string.Empty;
            txtDescripcionTarea.Text = string.Empty;
            dpFechaVencimientoTarea.SelectedDate = null;
            cbPrioridadTarea.SelectedIndex = -1;
            cbEstadoTarea.SelectedIndex = -1;
        }

        private async void GuardarTarea_Click(object sender, RoutedEventArgs e)
        {
            // Validación básica
            if (string.IsNullOrWhiteSpace(txtTituloTarea.Text))
            {
                MessageBox.Show("El título es obligatorio.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (dpFechaVencimientoTarea.SelectedDate == null)
            {
                MessageBox.Show("La fecha de vencimiento es obligatoria.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cbPrioridadTarea.SelectedItem == null)
            {
                MessageBox.Show("La prioridad es obligatoria.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cbEstadoTarea.SelectedItem == null)
            {
                MessageBox.Show("El estado es obligatorio.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (SelectedCasoId == null)
                {
                    MessageBox.Show("Debes seleccionar un caso.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var tarea = new TFG_V0._01.Supabase.Models.TareaInsertDto
                {
                    titulo = txtTituloTarea.Text,
                    descripcion = txtDescripcionTarea.Text,
                    fecha_creacion = DateTime.Now,
                    fecha_fin = dpFechaVencimientoTarea.SelectedDate,
                    prioridad = cbPrioridadTarea.SelectedItem.ToString(),
                    estado = cbEstadoTarea.SelectedItem.ToString(),
                    id_caso = SelectedCasoId
                };

                await _tareasService.CrearTarea(tarea);
                await CargarTareasPendientes();
                CerrarPanelTarea();
                ResetFieldsTarea();
                MessageBox.Show("Tarea creada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la tarea: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelarTarea_Click(object sender, RoutedEventArgs e)
        {
            ResetFieldsTarea();
            CerrarPanelTarea();
        }

        #endregion

        #region 🗂️ Cargar Casos Disponibles

        private async Task CargarCasosDisponibles()
        {
            await _supabaseCasos.InicializarAsync();
            var casos = await _supabaseCasos.ObtenerTodosAsync();
            CasosDisponibles = new ObservableCollection<Caso>(casos);
        }

        private async Task CargarEventosDeHoy()
        {
            try
            {
                var eventos = await _eventosCitasService.ObtenerEventosCitas();
                var estados = await _estadosEventosService.ObtenerEstadosEventos();
                var casos = await _casosService.ObtenerTodosCasosManualAsync();

                var eventosDeHoy = eventos
                    .Where(e => e.Fecha.Date == DateTime.Today)
                    .OrderBy(e => e.FechaInicio)
                    .Select(e => new EventoViewModel
                    {
                        Id = e.Id,
                        Titulo = e.Titulo,
                        Descripcion = e.Descripcion,
                        Fecha = e.Fecha,
                        EstadoNombre = estados.FirstOrDefault(s => s.Id == e.IdEstado)?.Nombre ?? "Sin estado",
                        EstadoColor = ObtenerColorEstado(estados.FirstOrDefault(s => s.Id == e.IdEstado)?.Nombre),
                        FechaInicio = e.FechaInicio,
                        IdCaso = e.IdCaso,
                        NombreCaso = casos.FirstOrDefault(c => c.id == e.IdCaso)?.titulo ?? "Sin caso"
                    })
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    EventosDeHoy.Clear();
                    foreach (var evento in eventosDeHoy)
                    {
                        EventosDeHoy.Add(evento);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los eventos de hoy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        public class EventoViewModel : INotifyPropertyChanged
        {
            public int Id { get; set; }
            public string Titulo { get; set; }
            public string Descripcion { get; set; }
            public DateTime Fecha { get; set; }
            public string EstadoNombre { get; set; }
            public string EstadoColor { get; set; }
            public TimeSpan FechaInicio { get; set; }
            public int IdCaso { get; set; }
            public string NombreCaso { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}