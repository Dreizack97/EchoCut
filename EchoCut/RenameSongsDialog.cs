using EchoCut.Library;
using EchoCut.Objects;

namespace EchoCut
{
    /// <summary>
    /// Diálogo para renombrar varias pistas a la vez, con vista previa del resultado.
    /// </summary>
    /// <remarks>
    /// Renombrar en lote es fácil de hacer mal y tedioso de deshacer: contar de más al quitar
    /// caracteres o elegir pocos dígitos para el consecutivo se nota en cientos de archivos. La
    /// vista previa muestra cada nombre antes y después, y el botón no se habilita mientras algún
    /// nombre nuevo no sea válido, se repita o choque con un archivo que ya existe.
    /// </remarks>
    public partial class RenameSongsDialog : Form
    {
        private readonly RenameMode _mode;
        private readonly IReadOnlyList<RenameItem> _items;
        private string[] _newNames = [];

        /// <summary>Crea el diálogo para las pistas indicadas, en el orden en que se numerarán.</summary>
        /// <param name="mode">Transformación a aplicar.</param>
        /// <param name="items">Pistas a renombrar, en el orden de la rejilla.</param>
        /// <param name="selectionOnly">Si las pistas son la selección y no todo el listado, para decirlo en el texto.</param>
        public RenameSongsDialog(RenameMode mode, IReadOnlyList<RenameItem> items, bool selectionOnly)
        {
            InitializeComponent();

            _mode = mode;
            _items = items;

            pnlSequence.Visible = mode == RenameMode.Sequence;
            pnlRemove.Visible = mode == RenameMode.RemoveLeading;
            txtSeparator.Text = TrackNaming.DefaultSeparator;

            string scope = (items.Count, selectionOnly) switch
            {
                (1, _) => "la canción",
                (_, true) => $"las {items.Count} canciones seleccionadas",
                _ => $"las {items.Count} canciones del listado",
            };

            (Text, lblIntro.Text) = mode == RenameMode.Sequence
                ? ("Agregar consecutivo", $"Se antepondrá un número consecutivo al nombre de {scope}, en el orden en que aparecen en la rejilla.")
                : ("Quitar caracteres iniciales", $"Se quitarán caracteres del principio del nombre de {scope}.");

            RefreshPreview();
        }

        /// <summary>Nombre nuevo de cada pista, sin extensión y en el orden recibido.</summary>
        /// <value>Un nombre por pista; igual al actual si no cambia. Solo es definitivo si el diálogo devolvió OK.</value>
        public IReadOnlyList<string> NewNames => _newNames;

        private void Option_Changed(object? sender, EventArgs e) => RefreshPreview();

        /// <summary>Recalcula los nombres nuevos, los valida y repinta la vista previa.</summary>
        private void RefreshPreview()
        {
            // Los controles pueden avisar de un cambio durante InitializeComponent, antes de que
            // existan las pistas; el constructor vuelve a llamar aquí al terminar.
            if (_items is null)
            {
                return;
            }

            _newNames = [.. _items.Select(Compose)];

            // Dos pistas de la misma carpeta con el mismo nombre nuevo chocarían entre sí al renombrar.
            HashSet<string> repeated = _items
                .Select((item, i) => Path.Combine(item.Directory, _newNames[i].Trim() + item.Extension))
                .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int problems = 0;
            int changes = 0;

            lstPreview.BeginUpdate();
            lstPreview.Items.Clear();

            for (int i = 0; i < _items.Count; i++)
            {
                RenameItem item = _items[i];
                string newName = _newNames[i];
                bool changed = !string.Equals(item.Name, newName, StringComparison.Ordinal);
                string? problem = changed ? ProblemOf(item, newName, repeated) : null;

                changes += changed ? 1 : 0;
                problems += problem is null ? 0 : 1;

                ListViewItem row = new([item.Name, newName, problem ?? (changed ? string.Empty : "Sin cambios")]);
                if (problem is not null)
                {
                    row.ForeColor = SongPresentation.Failed;
                }

                lstPreview.Items.Add(row);
            }

            lstPreview.EndUpdate();

            lblProblem.Text = problems switch
            {
                0 => string.Empty,
                1 => "Un nombre nuevo no es válido o ya existe; ajusta las opciones para continuar.",
                _ => $"{problems} nombres nuevos no son válidos o ya existen; ajusta las opciones para continuar.",
            };

            btnRename.Enabled = problems == 0 && changes > 0;
        }

        private string Compose(RenameItem item, int index) => _mode == RenameMode.Sequence
            ? TrackNaming.WithSequence(item.Name, (int)numStart.Value + index, (int)numDigits.Value, txtSeparator.Text)
            : TrackNaming.WithoutLeading(item.Name, (int)numCount.Value, chkTrim.Checked);

        /// <summary>Explica por qué no se puede usar el nombre nuevo de una pista, si es el caso.</summary>
        /// <remarks>
        /// Un archivo que ya existe con ese nombre bloquea aunque también vaya a renombrarse: el lote
        /// corre en paralelo y no garantiza que el otro se aparte antes.
        /// </remarks>
        private static string? ProblemOf(RenameItem item, string newName, HashSet<string> repeated)
        {
            if (TrackNaming.GetProblem(newName) is { } problem)
            {
                return problem;
            }

            string newPath = Path.Combine(item.Directory, newName.Trim() + item.Extension);
            string oldPath = Path.Combine(item.Directory, item.Name + item.Extension);

            if (repeated.Contains(newPath))
            {
                return "Otra canción de la carpeta tendría el mismo nombre.";
            }

            return !string.Equals(newPath, oldPath, StringComparison.OrdinalIgnoreCase) && File.Exists(newPath)
                ? "Ya existe un archivo con ese nombre."
                : null;
        }
    }
}
