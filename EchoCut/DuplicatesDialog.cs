using EchoCut.Audio;
using EchoCut.Library;
using EchoCut.Playback;
using EchoCut.Processing;
using EchoCut.Shell;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;

namespace EchoCut
{
    /// <summary>
    /// Ventana con los grupos de canciones duplicadas por su audio y las acciones para resolverlos.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cada grupo propone conservar la copia de mejor calidad —sin pérdida, más tasa de bits, más
    /// duración— y deja marcadas las demás. Nada se borra sin confirmar, y lo que se borra va a la
    /// Papelera de reciclaje: un falso positivo, o una preferencia distinta, siempre tiene vuelta atrás.
    /// </para>
    /// <para>
    /// Se puede escuchar cualquier copia antes de decidir. Las rutas enviadas a la Papelera quedan en
    /// <see cref="DeletedPaths"/> para que la ventana principal retire sus filas.
    /// </para>
    /// </remarks>
    public partial class DuplicatesDialog : Form
    {
        /// <summary>Lo que suena al escuchar una copia: suficiente para reconocerla.</summary>
        private const double PreviewSeconds = 15.0;

        /// <summary>Texto del botón de escucha mientras suena.</summary>
        private const string StopText = "⏹ Detener";

        private readonly string _ffmpegPath;
        private readonly List<string> _deleted = [];
        private readonly CancellationTokenSource _closing = new();
        private AudioPreviewPlayer? _player;
        private string _playText = string.Empty;
        private bool _updating;

        /// <summary>Si ya hay un recálculo del resumen pendiente de ejecutarse.</summary>
        private bool _selectionQueued;

        /// <summary>Crea la ventana con los grupos encontrados.</summary>
        /// <param name="groups">Grupos de duplicados, cada uno con su mejor copia primero.</param>
        /// <param name="ffmpegPath">Ruta de FFmpeg para escuchar las copias.</param>
        public DuplicatesDialog(IReadOnlyList<DuplicateGroup> groups, string ffmpegPath)
        {
            ArgumentNullException.ThrowIfNull(groups);
            InitializeComponent();
            _ffmpegPath = ffmpegPath;
            _playText = btnPlay.Text;

            Populate(groups);
            int copies = groups.Sum(group => group.Members.Count - 1);
            string found = groups.Count == 1 ? "Se encontró 1 grupo" : $"Se encontraron {groups.Count} grupos";
            lblSummary.Text = $"{found} de canciones con el mismo audio y {copies} {(copies == 1 ? "copia" : "copias")} de más. "
                + "En cada grupo se propone conservar la de mejor calidad (en negrita) y enviar las marcadas a la Papelera de reciclaje.";
            UpdateSelection();
        }

        /// <value>Rutas de los archivos enviados a la Papelera, para retirar sus filas.</value>
        public IReadOnlyList<string> DeletedPaths => _deleted;

        /// <remarks>
        /// <see cref="Enumerable.OfType{TResult}"/> y no <c>Cast</c>: mientras el control inserta sus
        /// elementos en la ventana nativa, la colección puede devolver huecos nulos.
        /// </remarks>
        private IEnumerable<ListViewItem> Items => lvwDuplicates.Items.OfType<ListViewItem>();

        private ListViewItem? Current => lvwDuplicates.SelectedItems.Count > 0 ? lvwDuplicates.SelectedItems[0] : null;

        /// <summary>Llena la lista con un grupo de la vista por cada grupo de duplicados.</summary>
        private void Populate(IReadOnlyList<DuplicateGroup> groups)
        {
            _updating = true;
            lvwDuplicates.BeginUpdate();
            try
            {
                for (int index = 0; index < groups.Count; index++)
                {
                    DuplicateGroup group = groups[index];
                    ListViewGroup header = new($"Grupo {index + 1} · {group.Members.Count} copias · se propone conservar «{group.Members[0].Track.Name}»");
                    lvwDuplicates.Groups.Add(header);

                    foreach (DuplicateMember member in group.Members)
                    {
                        TrackInfo track = member.Track;
                        ListViewItem item = new(member.IsBest ? $"{track.Name}  (conservar)" : track.Name, header)
                        {
                            Tag = member,
                            Checked = !member.IsBest,
                            ToolTipText = track.FilePath,
                        };

                        if (member.IsBest)
                        {
                            item.Font = new Font(lvwDuplicates.Font, FontStyle.Bold);
                        }

                        item.SubItems.AddRange(
                        [
                            member.Similarity.ToString("P0", CultureInfo.CurrentCulture),
                            TrackFormat.Duration(track.Duration),
                            track.Extension.TrimStart('.').ToUpperInvariant(),
                            TrackFormat.Bitrate(track.BitrateKbps),
                            TrackFormat.Size(track.SizeBytes),
                            track.Directory,
                        ]);

                        lvwDuplicates.Items.Add(item);
                    }
                }
            }
            finally
            {
                lvwDuplicates.EndUpdate();
                _updating = false;
            }
        }

        /// <summary>Encola el recálculo del resumen en lugar de hacerlo en mitad del aviso.</summary>
        /// <remarks>
        /// El control avisa de cada casilla también mientras crea su ventana nativa e inserta en ella los
        /// elementos uno a uno, y en ese momento su colección todavía tiene huecos. Recalcular cuando el
        /// mensaje en curso termina evita leerla a medias y, de paso, hace una sola cuenta cuando cambian
        /// muchas casillas seguidas.
        /// </remarks>
        private void lvwDuplicates_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (_updating || _selectionQueued || !IsHandleCreated)
            {
                return;
            }

            _selectionQueued = true;
            BeginInvoke(() =>
            {
                _selectionQueued = false;
                if (!IsDisposed)
                {
                    UpdateSelection();
                }
            });
        }

        /// <summary>Copia que representa un elemento de la lista, si lo es.</summary>
        private static DuplicateMember? MemberOf(ListViewItem item) => item.Tag as DuplicateMember;

        private void lvwDuplicates_SelectedIndexChanged(object? sender, EventArgs e)
        {
            btnPlay.Enabled = Current is not null;
            btnReveal.Enabled = Current is not null;
        }

        private async void lvwDuplicates_ItemActivate(object? sender, EventArgs e) => await TogglePlaybackAsync().ConfigureAwait(true);

        /// <summary>
        /// Resume lo marcado y avisa si en algún grupo están marcadas todas las copias: no quedaría ninguna.
        /// </summary>
        private void UpdateSelection()
        {
            List<ListViewItem> marked = [.. Items.Where(item => item.Checked && MemberOf(item) is not null)];
            long bytes = marked.Sum(item => MemberOf(item)!.Track.SizeBytes);
            List<string> emptied = [.. lvwDuplicates.Groups.Cast<ListViewGroup>()
                .Where(group => group.Items.Count > 0 && group.Items.OfType<ListViewItem>().All(item => item.Checked))
                .Select(group => group.Header[..group.Header.IndexOf(" ·", StringComparison.Ordinal)])];

            string summary = marked.Count == 0
                ? "Ninguna copia marcada."
                : $"{marked.Count} {(marked.Count == 1 ? "copia marcada" : "copias marcadas")} para la Papelera · {TrackFormat.Size(bytes)}.";

            lblSelection.Text = emptied.Count == 0
                ? summary
                : $"{summary} ⚠ En {string.Join(", ", emptied)} están marcadas todas las copias: no quedaría ninguna.";
            lblSelection.ForeColor = emptied.Count == 0 ? SystemColors.ControlText : Color.FromArgb(163, 18, 18);
            btnRecycle.Enabled = marked.Count > 0;
        }

        /// <summary>Vuelve a la propuesta: marcadas todas salvo la mejor de cada grupo.</summary>
        private void btnSuggest_Click(object? sender, EventArgs e) => SetChecks(item => MemberOf(item) is { IsBest: false });

        private void btnClear_Click(object? sender, EventArgs e) => SetChecks(_ => false);

        private void SetChecks(Func<ListViewItem, bool> check)
        {
            _updating = true;
            try
            {
                foreach (ListViewItem item in Items)
                {
                    item.Checked = check(item);
                }
            }
            finally
            {
                _updating = false;
            }

            UpdateSelection();
        }

        private void btnReveal_Click(object? sender, EventArgs e)
        {
            if (Current?.Tag is not DuplicateMember member)
            {
                return;
            }

            try
            {
                ExternalApps.RevealInExplorer(member.Track.FilePath);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "No se pudo abrir la carpeta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnPlay_Click(object? sender, EventArgs e) => await TogglePlaybackAsync().ConfigureAwait(true);

        /// <summary>
        /// Escucha unos segundos de la copia seleccionada, o detiene lo que suene. Empieza a un cuarto de
        /// la canción, donde ya suena la música y no la entrada, que en muchas es casi igual.
        /// </summary>
        /// <remarks>Nunca lanza: es el cuerpo de manejadores <c>async void</c>.</remarks>
        private async Task TogglePlaybackAsync()
        {
            if (_player?.IsPlaying == true)
            {
                StopPlayback();
                return;
            }

            if (Current?.Tag is not DuplicateMember member)
            {
                return;
            }

            try
            {
                if (_player is null)
                {
                    _player = new AudioPreviewPlayer(_ffmpegPath);
                    _player.PlaybackCompleted += (_, _) => StopPlayback();
                }

                double duration = member.Track.DurationSeconds;
                double start = Math.Max(0.0, Math.Min(duration * 0.25, duration - PreviewSeconds));
                await _player.PlayAsync(member.Track.FilePath, new PreviewWindow(start, Math.Min(PreviewSeconds, duration - start)), null, _closing.Token).ConfigureAwait(true);

                if (!IsDisposed && _player.IsPlaying)
                {
                    btnPlay.Text = StopText;
                }
            }
            catch (OperationCanceledException)
            {
                // La ventana se cerró mientras se preparaba el audio.
            }
            catch (Exception exception)
            {
                if (!IsDisposed)
                {
                    MessageBox.Show(this, exception.Message, "No se pudo reproducir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void StopPlayback()
        {
            _player?.Stop();
            if (!IsDisposed)
            {
                btnPlay.Text = _playText;
            }
        }

        /// <summary>Envía las copias marcadas a la Papelera de reciclaje, tras confirmarlo.</summary>
        private void btnRecycle_Click(object? sender, EventArgs e)
        {
            List<ListViewItem> marked = [.. Items.Where(item => item.Checked && MemberOf(item) is not null)];
            if (marked.Count == 0)
            {
                return;
            }

            long bytes = marked.Sum(item => MemberOf(item)!.Track.SizeBytes);
            string warning = lblSelection.ForeColor == SystemColors.ControlText
                ? string.Empty
                : $"{Environment.NewLine}{Environment.NewLine}⚠ En algún grupo están marcadas todas las copias: de esa canción no quedará ninguna.";

            if (MessageBox.Show(
                    this,
                    $"¿Enviar {marked.Count} {(marked.Count == 1 ? "archivo" : "archivos")} ({TrackFormat.Size(bytes)}) a la Papelera de reciclaje?{warning}",
                    "Enviar a la Papelera",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            StopPlayback();
            List<string> errors = [];
            _updating = true;
            lvwDuplicates.BeginUpdate();
            try
            {
                foreach (ListViewItem item in marked)
                {
                    string path = MemberOf(item)!.Track.FilePath;
                    try
                    {
                        FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        _deleted.Add(path);
                        item.Remove();
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
                    {
                        errors.Add($"{Path.GetFileName(path)}: {exception.Message}");
                    }
                }

                RemoveResolvedGroups();
            }
            finally
            {
                lvwDuplicates.EndUpdate();
                _updating = false;
            }

            UpdateSelection();

            if (errors.Count > 0)
            {
                MessageBox.Show(this, $"No se pudieron enviar a la Papelera:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, errors)}", "Enviar a la Papelera", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Quita de la lista los grupos que ya no tienen más de una copia: están resueltos.</summary>
        private void RemoveResolvedGroups()
        {
            foreach (ListViewGroup group in lvwDuplicates.Groups.Cast<ListViewGroup>().ToList())
            {
                if (group.Items.Count <= 1)
                {
                    foreach (ListViewItem item in group.Items.OfType<ListViewItem>().ToList())
                    {
                        item.Remove();
                    }

                    lvwDuplicates.Groups.Remove(group);
                }
            }

            if (lvwDuplicates.Items.Count == 0)
            {
                lblSummary.Text = "Ya no quedan duplicados: todos los grupos están resueltos.";
            }
        }

        private void DuplicatesDialog_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _closing.Cancel();
            StopPlayback();
            _player?.Dispose();
        }
    }
}
