﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
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
using TFG_V0._01.Supabase;
using System.Collections.ObjectModel;
using TFG_V0._01.Supabase.Models;
using System.Globalization;
using System.ComponentModel;
using TFG_V0._01.Helpers;
using System.IO;

namespace TFG_V0._01.Ventanas
{
    public partial class Clientes : Window, INotifyPropertyChanged
    {
        #region variables animacion
        private Storyboard fadeInStoryboard;
        private Storyboard shakeStoryboard;
        private Storyboard meshAnimStoryboard; // Storyboard para la animación del gradiente de malla

        // Brushes para el fondo animado
        private RadialGradientBrush mesh1Brush;
        private RadialGradientBrush mesh2Brush;
        #endregion

        #region variables
        private readonly SupabaseClientes _supabaseClientes;
        private readonly SupabaseCasos _supabaseCasos;
        private readonly SupabaseCasoEtiquetas _supabaseCasoEtiquetas;
        private readonly SupabaseEstados _supabaseEstados;
        private readonly SupabaseDocumentos _supabaseDocumentos;
        public ObservableCollection<Cliente> ListaClientes { get; set; } = new ObservableCollection<Cliente>();
        private Cliente _selectedCliente;
        public ObservableCollection<Documento> DocumentosCliente { get; set; } = new ObservableCollection<Documento>();
        public ObservableCollection<Caso> HistorialCasos { get; set; } = new ObservableCollection<Caso>();
        public ObservableCollection<Caso> CasosActivos
        {
            get { return _casosActivos; }
            set
            {
                _casosActivos = value;
                OnPropertyChanged(nameof(CasosActivos));
            }
        }
        public Cliente SelectedCliente
        {
            get => _selectedCliente;
            set
            {
                _selectedCliente = value;
                if (_selectedCliente != null && _selectedCliente.id.HasValue)
                {
                    CargarDocumentosCliente(_selectedCliente.id.Value);
                }
                DataContext = null;
                DataContext = this;
                _ = CargarCasosActivosAsync();
                _ = CargarHistorialCasosAsync();
            }
        }
        public string EditNombre { get; set; }
        public string EditApellido1 { get; set; }
        public string EditApellido2 { get; set; }
        public string EditDNI { get; set; }
        public string EditTelefono1 { get; set; }
        public string EditTelefono2 { get; set; }
        public string EditEmail1 { get; set; }
        public string EditEmail2 { get; set; }
        public string EditDireccion { get; set; }
        public DateTime EditFechaContrato { get; set; }
        private bool isEditPersonalInfoVisible = false;
        public bool IsEditPersonalInfoVisible
        {
            get => isEditPersonalInfoVisible;
            set
            {
                isEditPersonalInfoVisible = value;
                DataContext = null;
                DataContext = this;
            }
        }
        private ObservableCollection<Caso> _casosActivos;
        private ObservableCollection<Estado> _estadosDisponibles = new ObservableCollection<Estado>();
        public ICommand VerDetallesCommand { get; }
        private ICollectionView _clientesView;
        private string _textoComboCliente;
        public string TextoComboCliente
        {
            get => _textoComboCliente;
            set { _textoComboCliente = value; OnPropertyChanged(nameof(TextoComboCliente)); }
        }
        #endregion

        #region Constructor
        public Clientes()
        {
            InitializeComponent();
            _supabaseClientes = new SupabaseClientes();
            _supabaseCasos = new SupabaseCasos();
            _supabaseCasoEtiquetas = new SupabaseCasoEtiquetas();
            _supabaseEstados = new SupabaseEstados();
            _supabaseDocumentos = new SupabaseDocumentos();
            InitializeAnimations(); // Asegúrate de que esto inicialice meshAnimStoryboard si no lo hace ya
            FindMeshBrushes(); // Nuevo método para encontrar los pinceles en el XAML
            CargarIdiomas(MainWindow.idioma);
            AplicarModoSistema();
            IniciarAnimacionMesh(); // Iniciar la animación del gradiente
            _ = CargarClientesAsync();
            this.DataContext = this;
            _casosActivos = new ObservableCollection<Caso>();
            _ = CargarCasosActivosAsync();
            VerDetallesCommand = new RelayCommand<Caso>(VerDetalles);
            _ = CargarEstadosAsync();

            // Suscribirse a los eventos del control AñadirCasoWindow
            if (AddCasoControl != null)
            {
                AddCasoControl.CasoGuardado += (s, e) =>
                {
                    HideSlidePanelCaso();
                    _ = CargarCasosActivosAsync();
                };
                AddCasoControl.CasoCancelado += (s, e) =>
                {
                    HideSlidePanelCaso();
                };
            }
        }
        #endregion

        #region Eventos
        private async Task CargarClientesAsync()
        {
            try
            {
                await _supabaseClientes.InicializarAsync();
                var clientes = await _supabaseClientes.ObtenerClientesAsync();
                ListaClientes.Clear();
                foreach (var cliente in clientes)
                    ListaClientes.Add(cliente);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes: {ex.Message}");
            }
        }

        private async Task AgregarClienteAsync(Cliente nuevoCliente)
        {
            try
            {
                await _supabaseClientes.InicializarAsync();
                var nuevoClienteDto = new ClienteInsertDto
                {
                    nombre = nuevoCliente.nombre,
                    apellido1 = nuevoCliente.apellido1,
                    apellido2 = nuevoCliente.apellido2,
                    email1 = nuevoCliente.email1,
                    email2 = nuevoCliente.email2,
                    telf1 = nuevoCliente.telf1,
                    telf2 = nuevoCliente.telf2,
                    direccion = nuevoCliente.direccion,
                    fecha_contrato = nuevoCliente.fecha_contrato
                };
                await _supabaseClientes.InsertarClienteAsync(nuevoClienteDto);
                await CargarClientesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar cliente: {ex.Message}");
            }
        }

        private async Task ActualizarClienteAsync(Cliente cliente)
        {
            try
            {
                await _supabaseClientes.InicializarAsync();
                await _supabaseClientes.ActualizarClienteAsync(cliente);
                int? idActualizado = cliente.id;
                await CargarClientesAsync();

                // Vuelve a seleccionar el cliente actualizado
                if (idActualizado.HasValue)
                {
                    var actualizado = ListaClientes.FirstOrDefault(c => c.id == idActualizado);
                    if (actualizado != null)
                    {
                        SelectedCliente = actualizado;
                        OnPropertyChanged(nameof(SelectedCliente));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar cliente: {ex.Message}");
            }
        }

        private async Task EliminarClienteAsync(int id)
        {
            try
            {
                await _supabaseClientes.InicializarAsync();
                await _supabaseClientes.EliminarClienteAsync(id);
                await CargarClientesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar cliente: {ex.Message}");
            }
        }
        #endregion

        #region Eventos de la interfaz

        private void EditarInformacion_Click(object sender, RoutedEventArgs e)
        {
            EditNombre = SelectedCliente.nombre;
            EditApellido1 = SelectedCliente.apellido1;
            EditApellido2 = SelectedCliente.apellido2;
            EditDNI = SelectedCliente.dni;
            EditTelefono1 = SelectedCliente.telf1;
            EditTelefono2 = SelectedCliente.telf2;
            EditEmail1 = SelectedCliente.email1;
            EditEmail2 = SelectedCliente.email2;
            EditDireccion = SelectedCliente.direccion;
            EditFechaContrato = SelectedCliente.fecha_contrato;
            IsEditPersonalInfoVisible = true;
        }

        private bool IsValidDni(string dni)
        {
            return IsValidDniFormat(dni) && IsValidDniLetter(dni);
        }

        private bool IsValidDniFormat(string dni)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(dni, @"^\d{8}[A-HJ-NP-TV-Z]$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private bool IsValidDniLetter(string dni)
        {
            if (dni == null || dni.Length != 9) return false;
            string letras = "TRWAGMYFPDXBNJZSQVHLCKE";
            if (!int.TryParse(dni.Substring(0, 8), out int numero)) return false;
            char letraEsperada = letras[numero % 23];
            return char.ToUpper(dni[8]) == letraEsperada;
        }

        private bool IsValidNie(string nie)
        {
            if (nie == null || nie.Length != 9) return false;
            string letras = "TRWAGMYFPDXBNJZSQVHLCKE";
            char first = char.ToUpper(nie[0]);
            if (first != 'X' && first != 'Y' && first != 'Z') return false;
            string nieNum = nie;
            switch (first)
            {
                case 'X': nieNum = "0" + nie.Substring(1); break;
                case 'Y': nieNum = "1" + nie.Substring(1); break;
                case 'Z': nieNum = "2" + nie.Substring(1); break;
            }
            if (!int.TryParse(nieNum.Substring(0, 8), out int numero)) return false;
            char letraEsperada = letras[numero % 23];
            return char.ToUpper(nie[8]) == letraEsperada;
        }

        private bool IsValidNif(string nif)
        {
            return IsValidDni(nif) || IsValidNie(nif);
        }

        private async void GuardarInformacion_Click(object sender, RoutedEventArgs e)
        {
            // Comprobar si hay cambios
            bool hayCambios =
                SelectedCliente.nombre != EditNombre ||
                SelectedCliente.apellido1 != EditApellido1 ||
                SelectedCliente.apellido2 != EditApellido2 ||
                SelectedCliente.email1 != EditEmail1 ||
                SelectedCliente.email2 != EditEmail2 ||
                SelectedCliente.telf1 != EditTelefono1 ||
                SelectedCliente.telf2 != EditTelefono2 ||
                SelectedCliente.direccion != EditDireccion ||
                SelectedCliente.dni != EditDNI;

            if (!hayCambios)
            {
                IsEditPersonalInfoVisible = false;
                return;
            }

            // Comprobar si el DNI ya existe en otro cliente
            var clientesExistentes = await _supabaseClientes.ObtenerClientesAsync();
            if (clientesExistentes.Any(c => c.dni != null && c.dni.Equals(EditDNI, StringComparison.OrdinalIgnoreCase) && c.id != SelectedCliente.id))
            {
                MessageBox.Show($"El DNI '{EditDNI}' ya existe en la base de datos. Por favor, introduce un DNI único.", "DNI duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar formato y letra del NIF (DNI o NIE) antes de guardar
            if (!IsValidNif(EditDNI))
            {
                MessageBox.Show("El DNI/NIE introducido no es válido. Debe tener el formato y la letra correctos según el algoritmo oficial.", "Identificador no válido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Si hay cambios, actualiza el cliente
            SelectedCliente.nombre = EditNombre;
            SelectedCliente.apellido1 = EditApellido1;
            SelectedCliente.apellido2 = EditApellido2;
            SelectedCliente.email1 = EditEmail1;
            SelectedCliente.email2 = EditEmail2;
            SelectedCliente.telf1 = EditTelefono1;
            SelectedCliente.telf2 = EditTelefono2;
            SelectedCliente.direccion = EditDireccion;
            SelectedCliente.dni = EditDNI;

            IsEditPersonalInfoVisible = false;
            await ActualizarClienteAsync(SelectedCliente);
        }

        private void CancelarInformacion_Click(object sender, RoutedEventArgs e)
        {
            EditNombre = SelectedCliente.nombre;
            EditApellido1 = SelectedCliente.apellido1;
            EditApellido2 = SelectedCliente.apellido2;
            EditDNI = SelectedCliente.dni;
            EditTelefono1 = SelectedCliente.telf1;
            EditTelefono2 = SelectedCliente.telf2;
            EditEmail1 = SelectedCliente.email1;
            EditEmail2 = SelectedCliente.email2;
            EditDireccion = SelectedCliente.direccion;
            EditFechaContrato = SelectedCliente.fecha_contrato;
            IsEditPersonalInfoVisible = false;
            DataContext = null;
            DataContext = this;
        }


        private async void CargarDocumentosCliente(int clienteId)
        {
            try
            {
                await _supabaseDocumentos.InicializarAsync();
                var documentos = await _supabaseDocumentos.ObtenerDocumentosPorClienteAsync(clienteId);
                DocumentosCliente.Clear();
                foreach (var documento in documentos)
                {
                    DocumentosCliente.Add(documento);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar documentos del cliente: {ex.Message}");
            }
        }

        #endregion

        #region Aplicar modo oscuro/claro cargado por sistema
        private void AplicarModoSistema()
        {
            this.Tag = MainWindow.isDarkTheme; // Usar Tag para que los estilos DataTrigger funcionen
            var button = this.FindName("ThemeButton") as Button;
            var icon = button?.Template.FindName("ThemeIcon", button) as Image;
            // meshGradientBrush ya no se usa directamente para cambiar colores aquí

            if (mesh1Brush == null || mesh2Brush == null) return; // Asegurarse de que los pinceles se encontraron

            if (MainWindow.isDarkTheme)
            {
                // Aplicar modo oscuro
                if (icon != null)
                {
                    icon.Source = new BitmapImage(new Uri("/TFG V0.01;component/Recursos/Iconos/sol.png", UriKind.Relative));
                }
                // Colores mesh oscuro (copiar de Home.xaml.cs)
                mesh1Brush.GradientStops[0].Color = (Color)ColorConverter.ConvertFromString("#8C7BFF");
                mesh1Brush.GradientStops[1].Color = (Color)ColorConverter.ConvertFromString("#08a693");
                mesh2Brush.GradientStops[0].Color = (Color)ColorConverter.ConvertFromString("#3a4d5f");
                mesh2Brush.GradientStops[1].Color = (Color)ColorConverter.ConvertFromString("#272c3f");

                navbar.ActualizarTema(true);
            }
            else
            {
                // Aplicar modo claro
                if (icon != null)
                {
                    icon.Source = new BitmapImage(new Uri("/TFG V0.01;component/Recursos/Iconos/luna.png", UriKind.Relative));
                }
                // Colores mesh claro (copiar de Home.xaml.cs)
                mesh1Brush.GradientStops[0].Color = (Color)ColorConverter.ConvertFromString("#de9cb8");
                mesh1Brush.GradientStops[1].Color = (Color)ColorConverter.ConvertFromString("#9dcde1");
                mesh2Brush.GradientStops[0].Color = (Color)ColorConverter.ConvertFromString("#dc8eb8");
                mesh2Brush.GradientStops[1].Color = (Color)ColorConverter.ConvertFromString("#98d3ec");

                navbar.ActualizarTema(false);
            }
        }
        #endregion

        #region modo oscuro/claro + navbar



        #endregion



        #region boton cambiar tema
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            // Alternar el estado del tema
            MainWindow.isDarkTheme = !MainWindow.isDarkTheme;
            AplicarModoSistema();
            // Puedes añadir una animación de fade para la ventana si quieres
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.7, // o el valor actual
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300) // duración de la transición
            };
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
        #endregion

        #region Navbar botones



        #endregion

        #region Animaciones
        private void InitializeAnimations()
        {
            // Animación de entrada con fade
            fadeInStoryboard = new Storyboard();
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            Storyboard.SetTarget(fadeIn, this);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            fadeInStoryboard.Children.Add(fadeIn);

            // Animación de shake para error
            shakeStoryboard = new Storyboard();
            DoubleAnimation shakeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 5,
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
                Duration = TimeSpan.FromSeconds(0.05)
            };
            shakeStoryboard.Children.Add(shakeAnimation);

            // meshAnimStoryboard se inicializará en IniciarAnimacionMesh
        }

        private void BeginFadeInAnimation()
        {
            this.Opacity = 0;
            fadeInStoryboard.Begin();
        }

        private void ShakeElement(FrameworkElement element)
        {
            TranslateTransform trans = new TranslateTransform();
            element.RenderTransform = trans;

            DoubleAnimation anim = new DoubleAnimation
            {
                From = 0,
                To = 5,
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
                Duration = TimeSpan.FromSeconds(0.05)
            };

            trans.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        private void FindMeshBrushes()
        {
            // Intentar encontrar los pinceles de gradiente por nombre en los recursos
            // Esto asume que los RadialGradientBrush Mesh1 y Mesh2 están definidos directamente en Window.Resources
            if (this.Resources.Contains("Mesh1") && this.Resources["Mesh1"] is RadialGradientBrush brush1)
            {
                mesh1Brush = brush1;
            }
            if (this.Resources.Contains("Mesh2") && this.Resources["Mesh2"] is RadialGradientBrush brush2)
            {
                mesh2Brush = brush2;
            }

            // Si no se encontraron como recursos directos, buscar en el DrawingBrush
            if (mesh1Brush == null || mesh2Brush == null)
            {
                if (this.FindName("meshGradientBrush") is DrawingBrush drawingBrush && drawingBrush.Drawing is DrawingGroup drawingGroup)
                {
                    if (drawingGroup.Children.Count >= 2 &&
                        drawingGroup.Children[0] is GeometryDrawing geoDrawing1 && geoDrawing1.Brush is RadialGradientBrush radialBrush1 &&
                        drawingGroup.Children[1] is GeometryDrawing geoDrawing2 && geoDrawing2.Brush is RadialGradientBrush radialBrush2)
                    {
                        mesh1Brush = radialBrush1;
                        mesh2Brush = radialBrush2;
                    }
                }
            }
            // Importante: Si los pinceles se usan como StaticResource dentro del DrawingBrush, 
            // obtener referencias a los recursos originales puede ser complicado. 
            // Una alternativa es clonarlos si es necesario modificar sus propiedades (Center).
        }

        private void IniciarAnimacionMesh()
        {
            if (mesh1Brush == null || mesh2Brush == null) return; // Asegurarse de que los pinceles se encontraron

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

        #region eventos primer dialogo
        private void clientecheck(object sender, RoutedEventArgs e)
        {
            ClientSelectorGrid.Visibility = Visibility.Collapsed;
            ClientDetailsGrid.Visibility = Visibility.Visible;
        }
        #endregion

        private async Task CargarCasosActivosAsync()
        {
            if (SelectedCliente == null || !SelectedCliente.id.HasValue)
                return;

            try
            {
                await _supabaseCasos.InicializarAsync();
                var todosLosCasos = (await _supabaseCasos.ObtenerTodosAsync())
                    .Where(c => c.id_cliente == SelectedCliente.id.Value)
                    .ToList();

                var casosActivos = todosLosCasos
                    .Where(caso => caso.Estado != null && !string.Equals(caso.Estado.nombre, "Cerrado", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                CasosActivos = new ObservableCollection<Caso>(casosActivos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar casos activos: {ex.Message}");
            }
        }

        private async Task CargarHistorialCasosAsync()
        {
            if (SelectedCliente == null || !SelectedCliente.id.HasValue)
                return;

            try
            {
                await _supabaseCasos.InicializarAsync();
                var todosLosCasos = (await _supabaseCasos.ObtenerTodosAsync())
                    .Where(c => c.id_cliente == SelectedCliente.id.Value)
                    .ToList();

                HistorialCasos = new ObservableCollection<Caso>(todosLosCasos);
                OnPropertyChanged(nameof(HistorialCasos));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial de casos: {ex.Message}");
            }
        }

        private void VerDetalles(Caso caso)
        {
            // Asegura que la referencia de Estado sea la misma que la de la lista
            var estadoCorrecto = _estadosDisponibles.FirstOrDefault(e => e.id == caso.id_estado);
            caso.Estado = estadoCorrecto;

            // Actualizar la información en el panel deslizante
            PopupTitulo.Text = $"Caso #{caso.referencia}";
            PopupDescripcion.Text = caso.descripcion;
            PopupCliente.Text = $"Cliente: {caso.Cliente?.nombre ?? "No especificado"}";
            PopupTipo.Text = $"Tipo: {caso.TipoCaso?.nombre ?? "No especificado"}";
            PopupEstado.Text = $"Estado: {caso.Estado?.nombre ?? "No especificado"}";
            PopupFecha.Text = $"Fecha de inicio: {caso.fecha_inicio:dd/MM/yyyy}";

            // Cargar documentos relacionados
            _ = CargarDocumentosDelCaso(caso.id);

            // Mostrar el panel deslizante
            ShowSlidePanelDetalles();
        }

        private async Task CargarDocumentosDelCaso(int idCaso)
        {
            try
            {
                await _supabaseDocumentos.InicializarAsync();
                var documentos = await _supabaseDocumentos.ObtenerPorCasoAsync(idCaso);
                PopupDocumentos.ItemsSource = documentos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar documentos: {ex.Message}");
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
            if (SlidePanelCaso.Visibility == Visibility.Visible)
            {
                HideSlidePanelCaso();
                AddCasoControl.ResetFields();
            }
            else if (SlidePanelDetalles.Visibility == Visibility.Visible)
            {
                HideSlidePanelDetalles();
            }
        }

        private void MostrarDetallesCaso_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Caso caso)
            {
                VerDetalles(caso);
            }
        }

        private async Task CargarEstadosAsync()
        {
            try
            {
                await _supabaseEstados.InicializarAsync();
                var estados = await _supabaseEstados.ObtenerTodosAsync();
                _estadosDisponibles.Clear();
                foreach (var estado in estados)
                {
                    _estadosDisponibles.Add(estado);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar estados: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ComboClientes_Loaded(object sender, RoutedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo != null)
            {
                _clientesView = CollectionViewSource.GetDefaultView(combo.ItemsSource);
            }
        }

        private void ComboClientes_KeyUp(object sender, KeyEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || _clientesView == null) return;

            var text = combo.Text?.ToLower() ?? "";
            _clientesView.Filter = item =>
            {
                var cliente = item as Cliente;
                if (cliente == null) return false;

                var nombreCompleto = cliente.nombre_cliente?.ToLower() ?? "";
                return nombreCompleto.Contains(text);
            };
            _clientesView.Refresh();
        }

        private void ComboClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || _clientesView == null) return;

            if (combo.SelectedItem is Cliente cliente)
            {
                TextoComboCliente = cliente.nombre_cliente;
            }
            _clientesView.Filter = null;
            _clientesView.Refresh();
        }

        private void ClearComboBox_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.TemplatedParent is ComboBox combo)
            {
                combo.SelectedItem = null;
                _clientesView?.Refresh();
            }
        }

        private async void VerDocumento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Documento doc)
            {
                try
                {
                    var storage = new TFG_V0._01.Supabase.SupaBaseStorage();
                    await storage.InicializarAsync();

                    // Usar solo el nombre del archivo para la descarga
                    string nombreArchivo = System.IO.Path.GetFileName(doc.ruta.Replace("\\", "/"));

                    string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nombreArchivo);

                    // Descargar el archivo como byte[] desde Supabase
                    var fileBytes = await storage.DescargarArchivoAsync("documentos", nombreArchivo);

                    // Guardar en una ruta temporal
                    await System.IO.File.WriteAllBytesAsync(tempPath, fileBytes);

                    // Abrir el archivo
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir el documento: {ex.Message}\nRuta: {((Documento)btn.DataContext).ruta}");
                }
            }
        }

        private async void DescargarDocumento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Documento doc)
            {
                try
                {
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = doc.nombre + doc.extension_archivo,
                        DefaultExt = doc.extension_archivo,
                        Filter = $"Archivos {doc.extension_archivo}|*{doc.extension_archivo}|Todos los archivos|*.*"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        var storage = new TFG_V0._01.Supabase.SupaBaseStorage();
                        await storage.InicializarAsync();

                        // Usar solo el nombre del archivo para la descarga
                        string nombreArchivo = System.IO.Path.GetFileName(doc.ruta.Replace("\\", "/"));

                        // Descargar el archivo como byte[] desde Supabase
                        var fileBytes = await storage.DescargarArchivoAsync("documentos", nombreArchivo);

                        // Guardar el archivo en la ubicación seleccionada
                        await System.IO.File.WriteAllBytesAsync(saveFileDialog.FileName, fileBytes);

                        MessageBox.Show("Documento descargado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al descargar el documento: {ex.Message}\nRuta: {((Documento)btn.DataContext).ruta}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnNuevoCaso_Click(object sender, RoutedEventArgs e)
        {
            ShowSlidePanelCaso();
        }

        private void ShowSlidePanelCaso()
        {
            SlidePanelCaso.Visibility = Visibility.Visible;
            OverlayPanel.Visibility = Visibility.Visible;
            DoubleAnimation slideInAnimation = new DoubleAnimation
            {
                From = 400,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            SlidePanelCasoTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
        }

        private void HideSlidePanelCaso()
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
                SlidePanelCaso.Visibility = Visibility.Collapsed;
                OverlayPanel.Visibility = Visibility.Collapsed;
            };

            SlidePanelCasoTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
        }

        private void btnVolver_Click(object sender, RoutedEventArgs e)
        {
            ClientDetailsGrid.Visibility = Visibility.Collapsed;
            ClientSelectorGrid.Visibility = Visibility.Visible;
            ClientesComboBox.SelectedItem = null;
        }

        private void ComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo != null && !combo.IsDropDownOpen)
            {
                combo.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private void CargarIdiomas(int idioma)
        {
            var idiomas = new (string SeleccionarCliente, string Continuar, string Volver, string EditarInfo, string Guardar, string Cancelar,
                string FechaContrato, string Nombre, string Apellido1, string Apellido2, string DNI, string Telefono1, string Telefono2,
                string Email1, string Email2, string Direccion, string CasosActivos, string NuevoCaso, string HistorialCasos,
                string DocumentosCliente, string Ver, string Descargar, string Detalles, string Estado, string Tipo, string Cliente, string Titulo, string Subido, string NoEspecificado)[]
            {
                // Español
                ("Seleccionar Cliente", "Continuar", "Volver", "Editar Información", "Guardar", "Cancelar",
                 "Fecha de contrato: {0:dd/MM/yyyy}", "Nombre:", "Primer Apellido:", "Segundo Apellido:", "DNI:", "Teléfono 1:", "Teléfono 2:",
                 "Email 1:", "Email 2:", "Dirección:", "Casos Activos", "Nuevo Caso", "Historial de Casos",
                 "Documentos del Cliente", "Ver", "Descargar", "Ver Detalles", "Estado:", "Tipo:", "Cliente:", "Título:", "Subido: {0:dd/MM/yyyy}", "No especificado"),
                // Inglés
                ("Select Client", "Continue", "Back", "Edit Information", "Save", "Cancel",
                 "Contract date: {0:dd/MM/yyyy}", "Name:", "First Surname:", "Second Surname:", "ID:", "Phone 1:", "Phone 2:",
                 "Email 1:", "Email 2:", "Address:", "Active Cases", "New Case", "Case History",
                 "Client Documents", "View", "Download", "View Details", "Status:", "Type:", "Client:", "Title:", "Uploaded: {0:dd/MM/yyyy}", "Not specified"),
                // Catalán
                ("Seleccionar Client", "Continuar", "Tornar", "Editar Informació", "Desar", "Cancel·lar",
                 "Data de contracte: {0:dd/MM/yyyy}", "Nom:", "Primer Cognom:", "Segon Cognom:", "DNI:", "Telèfon 1:", "Telèfon 2:",
                 "Email 1:", "Email 2:", "Adreça:", "Casos Actius", "Nou Cas", "Historial de Casos",
                 "Documents del Client", "Veure", "Descarregar", "Veure Detalls", "Estat:", "Tipus:", "Client:", "Títol:", "Pujat: {0:dd/MM/yyyy}", "No especificat"),
                // Gallego
                ("Seleccionar Cliente", "Continuar", "Volver", "Editar Información", "Gardar", "Cancelar",
                 "Data de contrato: {0:dd/MM/yyyy}", "Nome:", "Primeiro Apelido:", "Segundo Apelido:", "DNI:", "Teléfono 1:", "Teléfono 2:",
                 "Email 1:", "Email 2:", "Enderezo:", "Casos Activos", "Novo Caso", "Historial de Casos",
                 "Documentos do Cliente", "Ver", "Descargar", "Ver Detalles", "Estado:", "Tipo:", "Cliente:", "Título:", "Subido: {0:dd/MM/yyyy}", "Non especificado"),
                // Euskera
                ("Bezeroa Hautatu", "Jarraitu", "Itzuli", "Informazioa Editatu", "Gorde", "Utzi",
                 "Kontratu data: {0:dd/MM/yyyy}", "Izena:", "Lehen Abizena:", "Bigarren Abizena:", "NAN:", "Telefonoa 1:", "Telefonoa 2:",
                 "Email 1:", "Email 2:", "Helbidea:", "Kasuan Aktiboak", "Kasua Berria", "Kasuen Historia",
                 "Bezeroaren Dokumentuak", "Ikusi", "Deskargatu", "Xehetasunak Ikusi", "Egoera:", "Mota:", "Bezeroa:", "Izenburua:", "Igotakoa: {0:dd/MM/yyyy}", "Zehaztu gabe")
            };

            if (idioma < 0 || idioma >= idiomas.Length)
                idioma = 0;

            var t = idiomas[idioma];

            SelecCliente.Text = t.SeleccionarCliente;
            btnconfirmar.Content = t.Continuar;

            if (btnVolver != null)
                ((btnVolver.Content as StackPanel)?.Children[1] as TextBlock)!.Text = t.Volver;


            if (btneditmod != null)
                btneditmod.Content = t.EditarInfo;
            if (btnguardarmod != null)
                btnguardarmod.Content = t.Guardar;
            if (btncancelarmod != null)
                btncancelarmod.Content = t.Cancelar;


            nombremod.Text = t.Nombre;
            apellido1mod.Text = t.Apellido1;
            apellido2mod.Text = t.Apellido2;
            dnimod.Text = t.DNI;
            telef1mod.Text = t.Telefono1;
            telef2mod.Text = t.Telefono2;
            email1mod.Text = t.Email1;
            email2mod.Text = t.Email2;
            direccionmod.Text = t.Direccion;
            dni.Text = t.DNI;
            telef1.Text = t.Telefono1;
            telef2.Text = t.Telefono2;
            email1.Text = t.Email1;
            email2.Text = t.Email2;
            direccion.Text = t.Direccion;
            titulocasosactivos.Text = t.CasosActivos;
            btnNuevoCaso.Content = t.NuevoCaso;
            titulohistorialcasos.Text = t.HistorialCasos;
            titulodocumentos.Text = t.DocumentosCliente;
            PopupTitulo.Text = t.Titulo;
            PopupDescripcion.Text = t.Detalles;
            PopupCliente.Text = t.Cliente;
            PopupTipo.Text = t.Tipo;
            PopupEstado.Text = t.Estado;
            PopupFecha.Text = t.Subido;
        }

        // Método temporal de depuración para comparar nombres de archivos
        private async void DebugCompararNombreDocumento(Documento doc)
        {
            try
            {
                var storage = new TFG_V0._01.Supabase.SupaBaseStorage();
                await storage.InicializarAsync();
                var archivos = await storage.ListarArchivosAsync("documentos");
                string nombreBD = System.IO.Path.GetFileName(doc.ruta ?? "").Trim();
                string resultado = $"Nombre en BD: '{nombreBD}'\n\nArchivos en Supabase:\n";
                foreach (var archivo in archivos)
                {
                    resultado += $"'{archivo.Name}'\n";
                    if (archivo.Name.Trim() == nombreBD)
                        resultado += "--> ¡COINCIDE!\n";
                }
                MessageBox.Show(resultado, "Comparación de nombres de archivo");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en depuración: {ex.Message}");
            }
        }
    }
}
