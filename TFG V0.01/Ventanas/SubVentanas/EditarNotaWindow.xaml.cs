using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TFG_V0._01.Supabase.Models;
using TFG_V0._01.Supabase;

namespace TFG_V0._01.Ventanas.SubVentanas
{
    public partial class EditarNotaWindow : Window, INotifyPropertyChanged
    {
        private readonly SupabaseNotas _notasService;
        private readonly int _idCaso;
        private readonly Nota _notaOriginal;

    #region ☁ SUPABASE
        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MessageBox.Show("Por favor, ingrese un título para la nota.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _notasService.InicializarAsync();

                if (_notaOriginal == null)
                {
                    // Crear nueva nota
                    var nuevaNotaDto = new NotaInsertDto
                    {
                        IdCaso = _idCaso,
                        Nombre = Nombre,
                        Descripcion = Descripcion,
                    };
                    await _notasService.InsertarAsync(nuevaNotaDto);
                }
                else
                {
                    // Actualizar nota existente
                    _notaOriginal.Nombre = Nombre;
                    _notaOriginal.Descripcion = Descripcion;
                    await _notasService.ActualizarAsync(_notaOriginal);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la nota: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        

        private string _tituloVentana;
        private string _nombre;
        private string _descripcion;

        public string TituloVentana
        {
            get => _tituloVentana;
            set
            {
                _tituloVentana = value;
                OnPropertyChanged(nameof(TituloVentana));
            }
        }

        public string Nombre
        {
            get => _nombre;
            set
            {
                _nombre = value;
                OnPropertyChanged(nameof(Nombre));
            }
        }

        public string Descripcion
        {
            get => _descripcion;
            set
            {
                _descripcion = value;
                OnPropertyChanged(nameof(Descripcion));
            }
        }

        public EditarNotaWindow(int idCaso, Nota notaExistente = null)
        {
            InitializeComponent();
            DataContext = this;
            _notasService = new SupabaseNotas();
            _idCaso = idCaso;
            _notaOriginal = notaExistente;

            if (notaExistente != null)
            {
                TituloVentana = "Modificar Nota";
                Nombre = notaExistente.Nombre;
                Descripcion = notaExistente.Descripcion;
            }
            else
            {
                TituloVentana = "Nueva Nota";
                Nombre = "";
                Descripcion = "";
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    #endregion
    }
    
} 