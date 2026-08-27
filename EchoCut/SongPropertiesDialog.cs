using EchoCut.Library;
using EchoCut.Objects;

namespace EchoCut
{
    /// <summary>
    /// Ventana modal para ver y editar las propiedades y metadatos de una canción,
    /// replicando la ventana de propiedades de archivos multimedia de Windows.
    /// </summary>
    public partial class SongPropertiesDialog : Form
    {
        private readonly TrackInfo _initialTrack;
        private readonly TrackProperties _properties;
        private readonly SongDetailsViewModel _viewModel;
        private bool _isDirty;

        /// <summary>
        /// Pista con los metadatos y nombre actualizados si se aplicaron o aceptaron cambios.
        /// </summary>
        public TrackInfo? UpdatedTrack { get; private set; }

        /// <summary>
        /// Se desencadena inmediatamente cada vez que se guardan y aplican cambios en disco.
        /// </summary>
        public event EventHandler<TrackInfo>? TrackUpdated;

        /// <summary>Inicializa el diálogo con la pista a editar.</summary>
        /// <param name="track">Pista cuyos datos se cargarán en las pestañas.</param>
        public SongPropertiesDialog(TrackInfo track)
        {
            ArgumentNullException.ThrowIfNull(track);
            _initialTrack = track;

            InitializeComponent();

            Text = $"Propiedades de {track.Name}";

            _properties = TrackEditor.LoadProperties(track.FilePath);
            _viewModel = new SongDetailsViewModel(_properties);

            PopulateGeneralTab();
            PopulateDetailsTab();

            txtName.TextChanged += (_, _) => MarkDirty();
            propertyGrid.PropertyValueChanged += (_, _) => MarkDirty();
        }

        private void PopulateGeneralTab()
        {
            txtName.Text = _properties.FileName;
            lblExtension.Text = _properties.Extension;
            lblTypeValue.Text = $"Archivo de audio ({_properties.Extension})";
            txtLocation.Text = _properties.Directory;
            lblSizeValue.Text = $"{TrackFormat.Size(_properties.SizeBytes)} ({_properties.SizeBytes:N0} bytes)";

            lblCreatedValue.Text = _properties.CreationTime.ToString("F");
            lblModifiedValue.Text = _properties.LastWriteTime.ToString("F");
            lblAccessedValue.Text = _properties.LastAccessTime.ToString("d");

            try
            {
                if (File.Exists(_properties.FilePath))
                {
                    using Icon? fileIcon = Icon.ExtractAssociatedIcon(_properties.FilePath);
                    if (fileIcon is not null)
                    {
                        picIcon.Image = fileIcon.ToBitmap();
                    }
                }
            }
            catch
            {
                // Si el sistema no puede extraer el icono, se continúa sin él
            }
        }

        private void PopulateDetailsTab()
        {
            propertyGrid.SelectedObject = _viewModel;
        }

        private void MarkDirty()
        {
            _isDirty = true;
            btnApply.Enabled = true;
        }

        private bool ApplyChanges()
        {
            string newName = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show(
                    this,
                    "El nombre de archivo no puede estar vacío.",
                    "Nombre no válido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtName.Focus();
                return false;
            }

            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show(
                    this,
                    "El nombre del archivo contiene caracteres no válidos.",
                    "Nombre no válido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtName.Focus();
                return false;
            }

            _properties.FileName = newName;

            try
            {
                TrackInfo saved = TrackEditor.SaveProperties(_properties);
                UpdatedTrack = saved;
                _isDirty = false;
                btnApply.Enabled = false;

                Text = $"Propiedades de {saved.Name}";
                txtLocation.Text = saved.Directory;
                lblSizeValue.Text = $"{TrackFormat.Size(saved.SizeBytes)} ({saved.SizeBytes:N0} bytes)";
                propertyGrid.Refresh();

                TrackUpdated?.Invoke(this, saved);

                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    $"No se pudieron guardar los cambios:\n\n{exception.Message}",
                    "Error al guardar propiedades",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            if (_isDirty)
            {
                if (!ApplyChanges())
                {
                    DialogResult = DialogResult.None;
                    return;
                }
            }

            DialogResult = UpdatedTrack is not null ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            ApplyChanges();
        }

        private void lnkRemovePersonal_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MessageBox.Show(
                    this,
                    "¿Desea eliminar todos los metadatos de esta canción?",
                    "Quitar metadatos",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _properties.Title = string.Empty;
            _properties.Subtitle = string.Empty;
            _properties.Comment = string.Empty;
            _properties.Performers = string.Empty;
            _properties.AlbumArtist = string.Empty;
            _properties.Album = string.Empty;
            _properties.Year = 0;
            _properties.Track = 0;
            _properties.Genre = string.Empty;
            _properties.Composers = string.Empty;
            _properties.Copyright = string.Empty;

            propertyGrid.Refresh();
            MarkDirty();
        }
    }
}
