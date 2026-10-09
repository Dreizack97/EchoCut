using EchoCut.Library;

namespace EchoCut
{
    /// <summary>
    /// Diálogo para elegir qué propiedades de las pistas modifica la acción «Normalizar».
    /// </summary>
    /// <remarks>
    /// Normalizar todo de una vez no siempre conviene: un comentario o un aviso de derechos de autor
    /// en TitleCase pierde legibilidad, y renombrar los archivos rompe listas de reproducción que
    /// apuntan a ellos. El diálogo deja quitar esas propiedades del lote sin renunciar al resto.
    /// </remarks>
    public partial class NormalizeOptions : Form
    {
        /// <summary>
        /// Propiedades normalizables con el nombre con el que se presentan, en el orden del diálogo.
        /// </summary>
        /// <remarks>
        /// El nombre del archivo va primero porque es el único cambio que se ve fuera de la etiqueta:
        /// renombra el archivo en disco. Comentario y derechos de autor van al final porque son los
        /// que menos suele interesar normalizar.
        /// </remarks>
        internal static readonly IReadOnlyList<FieldOption> Options =
        [
            new(NormalizableFields.FileName, "Nombre del archivo (lo renombra en disco)"),
            new(NormalizableFields.Title, "Título"),
            new(NormalizableFields.Subtitle, "Subtítulo"),
            new(NormalizableFields.Performers, "Intérpretes"),
            new(NormalizableFields.AlbumArtists, "Artistas del álbum"),
            new(NormalizableFields.Album, "Álbum"),
            new(NormalizableFields.Genres, "Géneros"),
            new(NormalizableFields.Composers, "Compositores"),
            new(NormalizableFields.Comment, "Comentario"),
            new(NormalizableFields.Copyright, "Derechos de autor"),
        ];

        /// <summary>Si las casillas las está cambiando el propio diálogo y no el usuario.</summary>
        private bool _updating;

        /// <summary>Crea el diálogo con la selección de partida.</summary>
        /// <param name="selected">Propiedades marcadas al abrir; normalmente, las de la última vez.</param>
        /// <param name="trackCount">Pistas a las que afectará la acción, para decirlo en el texto.</param>
        public NormalizeOptions(NormalizableFields selected, int trackCount)
        {
            InitializeComponent();

            lblIntro.Text = (trackCount == 1 ? "Elige qué propiedades se normalizarán en la canción cargada" : $"Elige qué propiedades se normalizarán en las {trackCount} canciones cargadas")
                + ": se quitan los acentos (la «ñ» se conserva) y cada palabra empieza con mayúscula.";

            _updating = true;
            foreach (FieldOption option in Options)
            {
                lstFields.Items.Add(option, selected.HasFlag(option.Field));
            }

            _updating = false;
            UpdateState(Selected);
        }

        /// <summary>Propiedades marcadas. Solo son definitivas si el diálogo devolvió OK.</summary>
        /// <value>Combinación de <see cref="NormalizableFields"/>; nunca vacía si se aceptó.</value>
        public NormalizableFields Selected => lstFields.CheckedItems
            .Cast<FieldOption>()
            .Aggregate(NormalizableFields.None, (fields, option) => fields | option.Field);

        /// <summary>Nombres con los que se presentan las propiedades indicadas, en el orden del diálogo.</summary>
        /// <param name="fields">Propiedades a describir.</param>
        /// <returns>Un nombre por propiedad incluida en <paramref name="fields"/>.</returns>
        internal static IEnumerable<string> Describe(NormalizableFields fields) =>
            Options.Where(option => fields.HasFlag(option.Field)).Select(option => option.Text);

        /// <summary>
        /// Refleja la selección en «Seleccionar todo» y en el botón de aceptar.
        /// </summary>
        /// <remarks>
        /// <c>ItemCheck</c> llega antes de que la casilla cambie, así que la selección se calcula con
        /// el valor nuevo en lugar de leer <see cref="Selected"/>, que aún tendría el anterior.
        /// </remarks>
        private void lstFields_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (_updating || lstFields.Items[e.Index] is not FieldOption option)
            {
                return;
            }

            NormalizableFields selected = e.NewValue == CheckState.Checked
                ? Selected | option.Field
                : Selected & ~option.Field;

            UpdateState(selected);
        }

        /// <summary>
        /// Marca todo, salvo que ya esté todo marcado, en cuyo caso lo desmarca.
        /// </summary>
        /// <remarks>
        /// La casilla no cambia sola (<c>AutoCheck</c> desactivado): con la selección a medias muestra
        /// el estado indeterminado, y un clic ahí debe marcar todo, como en el Explorador, y no
        /// desmarcarlo como haría el ciclo por defecto.
        /// </remarks>
        private void chkAll_Click(object? sender, EventArgs e)
        {
            bool check = chkAll.CheckState != CheckState.Checked;

            _updating = true;
            for (int i = 0; i < lstFields.Items.Count; i++)
            {
                lstFields.SetItemChecked(i, check);
            }

            _updating = false;
            UpdateState(Selected);
        }

        private void UpdateState(NormalizableFields selected)
        {
            chkAll.CheckState = selected switch
            {
                NormalizableFields.None => CheckState.Unchecked,
                NormalizableFields.All => CheckState.Checked,
                _ => CheckState.Indeterminate,
            };

            // Aceptar sin nada marcado lanzaría un lote que recorre y abre cada archivo para no
            // cambiar nada.
            btnNormalize.Enabled = selected != NormalizableFields.None;
        }

        /// <summary>Propiedad normalizable tal como se muestra en la lista.</summary>
        /// <param name="Field">Propiedad que representa.</param>
        /// <param name="Text">Nombre con el que se presenta.</param>
        internal sealed record FieldOption(NormalizableFields Field, string Text)
        {
            /// <inheritdoc/>
            public override string ToString() => Text;
        }
    }
}
