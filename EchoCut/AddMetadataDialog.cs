using EchoCut.Library;

namespace EchoCut
{
    /// <summary>
    /// Diálogo para aplicar las mismas etiquetas a todas las pistas cargadas.
    /// </summary>
    /// <remarks>
    /// Pensado para completar un lote que comparte datos —un álbum, un artista, un género— sin
    /// editar las canciones una a una. Los campos en blanco no se tocan, de modo que fijar el álbum
    /// no borra los títulos que cada pista ya tuviera.
    /// </remarks>
    public partial class AddMetadataDialog : Form
    {
        /// <summary>Crea el diálogo.</summary>
        /// <param name="trackCount">Pistas a las que afectará la acción, para decirlo en el texto.</param>
        public AddMetadataDialog(int trackCount)
        {
            InitializeComponent();

            lblIntro.Text = (trackCount == 1 ? "Los valores se aplicarán a la canción cargada" : $"Los valores se aplicarán a las {trackCount} canciones cargadas")
                + " y reemplazarán los que ya tengan. Deja en blanco lo que no quieras cambiar.";

            UpdateState();
        }

        /// <summary>Etiquetas escritas. Solo son definitivas si el diálogo devolvió OK.</summary>
        /// <value>Parche con los valores de cada campo; nunca vacío si se aceptó.</value>
        public TagPatch Patch => new(
            Artist: txtArtist.Text,
            Title: chkTitleFromFile.Checked ? null : txtTitle.Text,
            Album: txtAlbum.Text,
            Genre: txtGenre.Text,
            Comment: txtComment.Text,
            TitleFromFileName: chkTitleFromFile.Checked);

        /// <summary>Describe, una línea por propiedad, lo que el parche cambiará.</summary>
        /// <param name="patch">Parche a describir.</param>
        /// <returns>Una línea por cada propiedad que no esté en blanco, en el orden del diálogo.</returns>
        internal static IEnumerable<string> Describe(TagPatch patch)
        {
            if (!string.IsNullOrWhiteSpace(patch.Artist))
            {
                yield return $"Artista: «{patch.Artist.Trim()}»";
            }

            if (patch.TitleFromFileName)
            {
                yield return "Título: el nombre del archivo de cada canción";
            }
            else if (!string.IsNullOrWhiteSpace(patch.Title))
            {
                yield return $"Título: «{patch.Title.Trim()}»";
            }

            if (!string.IsNullOrWhiteSpace(patch.Album))
            {
                yield return $"Álbum: «{patch.Album.Trim()}»";
            }

            if (!string.IsNullOrWhiteSpace(patch.Genre))
            {
                yield return $"Género: «{patch.Genre.Trim()}»";
            }

            if (!string.IsNullOrWhiteSpace(patch.Comment))
            {
                yield return $"Comentarios: «{patch.Comment.Trim()}»";
            }
        }

        private void Field_Changed(object? sender, EventArgs e) => UpdateState();

        /// <summary>
        /// Deshabilita el título escrito cuando se toma del archivo y el botón de aplicar cuando no
        /// hay nada que aplicar: un lote vacío abriría cada archivo para no cambiar nada.
        /// </summary>
        private void UpdateState()
        {
            txtTitle.Enabled = !chkTitleFromFile.Checked;
            btnApply.Enabled = !Patch.IsEmpty;
        }
    }
}
