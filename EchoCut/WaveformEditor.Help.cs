using EchoCut.Audio;
using System.Globalization;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que enseña a usarlo, como la ventana principal: los
    /// atajos, la descripción de cada opción en la barra de estado y el resumen permanente de la copia.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Los atajos salen de una sola tabla, <see cref="Shortcuts"/>: de ella se atienden las teclas, se
    /// anuncian en los tooltips y en la barra de estado, se llena la ventana «Atajos» y la ventana
    /// principal toma los del editor para su propia lista. Así no pueden contradecirse.
    /// </para>
    /// <para>
    /// Mientras se escribe en un campo, las teclas sin Ctrl (Espacio, Supr, Esc) conservan su
    /// significado de siempre; solo guardar, aceptar y la ayuda funcionan en cualquier parte.
    /// </para>
    /// </remarks>
    public partial class WaveformEditor
    {
        private const string GroupPlayback = "Reproducción";
        private const string GroupEdit = "Edición";
        private const string GroupView = "Vista";
        private const string GroupTrim = "Recorte";
        private const string GroupWindow = "Ventana";

        /// <summary>Atajos del editor, en el orden en que se presentan.</summary>
        private static readonly EditorShortcut[] Shortcuts =
        [
            new(Keys.Space, GroupPlayback, "Reproducir la selección, o desde el punto marcado; volver a pulsar detiene", Command.Play),
            new(Keys.Control | Keys.Shift | Keys.A, GroupEdit, "Convertir la selección en la aparición", Command.FadeIn),
            new(Keys.Control | Keys.Shift | Keys.D, GroupEdit, "Convertir la selección en la desaparición", Command.FadeOut),
            new(Keys.Delete, GroupEdit, "Borrar la selección", Command.Delete),
            new(Keys.Control | Keys.Shift | Keys.R, GroupEdit, "Restaurar lo borrado dentro de la selección", Command.Restore),
            new(Keys.Control | Keys.Z, GroupEdit, "Deshacer", Command.Undo),
            new(Keys.Control | Keys.Y, GroupEdit, "Rehacer", Command.Redo),
            new(Keys.Control | Keys.Shift | Keys.Z, GroupEdit, "Rehacer", Command.Redo, Listed: false),
            new(Keys.Control | Keys.Oemplus, GroupView, "Acercar la vista activa", Command.ZoomIn),
            new(Keys.Control | Keys.Add, GroupView, "Acercar la vista activa", Command.ZoomIn, Listed: false),
            new(Keys.Control | Keys.OemMinus, GroupView, "Alejar la vista activa", Command.ZoomOut),
            new(Keys.Control | Keys.Subtract, GroupView, "Alejar la vista activa", Command.ZoomOut, Listed: false),
            new(Keys.Control | Keys.E, GroupView, "Ajustar la vista activa a la selección", Command.ZoomSelection),
            new(Keys.Control | Keys.F, GroupView, "Devolver la vista activa a su tramo inicial", Command.ZoomFit),
            new(Keys.Control | Keys.S, GroupWindow, "Guardar la copia sin cerrar", Command.Save, WhileEditing: true),
            new(Keys.Control | Keys.Enter, GroupWindow, "Aceptar y cerrar", Command.Accept, WhileEditing: true),
            new(Keys.F1, GroupWindow, "Ver los atajos", Command.Shortcuts, WhileEditing: true),
        ];

        /// <summary>Gestos sin opción propia, que también aparecen en la ventana de atajos.</summary>
        private static readonly ShortcutEntry[] Gestures =
        [
            new(GroupEdit, "Seleccionar un tramo", "Arrastrar sobre la onda"),
            new(GroupEdit, "Ajustar la selección o un fundido", "Arrastrar su borde"),
            new(GroupEdit, "Marcar desde dónde escuchar, o mostrar un fundido o lo borrado", "Clic"),
            new(GroupEdit, "Quitar la selección", "Esc"),
            new(GroupView, "Desplazar la vista", "Rueda"),
            new(GroupView, "Acercar o alejar bajo el puntero", "Ctrl+rueda"),
            new(GroupTrim, "Elegir la marca de inicio o la de fin", "Inicio / Fin"),
            new(GroupTrim, "Mover la marca elegida 0,01 s / 0,1 s / 1 s", "← → / Shift+← → / Ctrl+← →"),
            new(GroupWindow, "Cerrar (sin selección)", "Esc"),
        ];

        /// <summary>
        /// Mensaje que había en la barra de estado antes de mostrar una descripción, o <c>null</c> si
        /// no se está mostrando ninguna.
        /// </summary>
        private string? _statusBeforeHint;

        /// <summary>Qué hace cada atajo.</summary>
        private enum Command
        {
            Play,
            FadeIn,
            FadeOut,
            Delete,
            Restore,
            Undo,
            Redo,
            ZoomIn,
            ZoomOut,
            ZoomSelection,
            ZoomFit,
            Save,
            Accept,
            Shortcuts,
        }

        /// <summary>Atajos del editor tal como se presentan en una ventana de atajos.</summary>
        /// <returns>Los atajos y gestos, agrupados por lo que tocan.</returns>
        /// <remarks>La ventana principal los incluye en su propia lista, bajo «Forma de onda».</remarks>
        public static IReadOnlyList<ShortcutEntry> DescribeShortcuts() =>
        [
            .. Shortcuts.Where(s => s.Listed).Select(s => new ShortcutEntry(s.Group, s.Action, ShortcutText.Of(s.Keys))),
            .. Gestures,
        ];

        /// <inheritdoc/>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool editing = ActiveControl is NumericUpDown or TextBoxBase or ComboBox;

            if (keyData == Keys.Escape && !editing && _selection is not null)
            {
                ApplySelection(null);
                ShowInspector(InspectorTarget.None);
                return true;
            }

            foreach (EditorShortcut shortcut in Shortcuts)
            {
                if (shortcut.Keys != keyData || (editing && !shortcut.WhileEditing) || _saving)
                {
                    continue;
                }

                if (ItemFor(shortcut.Command) is ToolStripItem { Enabled: false } or Control { Enabled: false })
                {
                    return true;
                }

                Run(shortcut.Command);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>Anuncia cada atajo junto a su opción y engancha la descripción en la barra de estado.</summary>
        private void InitializeHelp()
        {
            foreach (EditorShortcut shortcut in Shortcuts.Where(s => s.Listed))
            {
                switch (ItemFor(shortcut.Command))
                {
                    case ToolStripItem item:
                        item.ToolTipText = $"{item.ToolTipText} ({ShortcutText.Of(shortcut.Keys)})";
                        break;

                    case Control control when toolTip.GetToolTip(control) is { Length: > 0 } text && !text.Contains('('):
                        toolTip.SetToolTip(control, $"{text} ({ShortcutText.Of(shortcut.Keys)})");
                        break;
                }
            }

            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is not ToolStripSeparator)
                {
                    item.MouseEnter += (_, _) => ShowHint(item.ToolTipText);
                    item.MouseLeave += (_, _) => HideHint();
                }
            }

            foreach (Control control in (Control[])[btnPlayStart, btnPlayEnd, btnReset, btnContextAction, btnSave, btnAccept, btnCancel])
            {
                control.MouseEnter += (_, _) => ShowHint(toolTip.GetToolTip(control));
                control.MouseLeave += (_, _) => HideHint();
            }
        }

        /// <summary>Opción de la ventana que dispara cada atajo, para anunciarlo y respetar si está habilitada.</summary>
        private object? ItemFor(Command command) => command switch
        {
            Command.Play => btnPlay,
            Command.FadeIn => btnFadeIn,
            Command.FadeOut => btnFadeOut,
            Command.Delete => btnDelete,
            Command.Restore => btnRestore,
            Command.Undo => btnUndo,
            Command.Redo => btnRedo,
            Command.ZoomIn => btnZoomIn,
            Command.ZoomOut => btnZoomOut,
            Command.ZoomSelection => btnZoomSelection,
            Command.ZoomFit => btnZoomFit,
            Command.Save => btnSave,
            Command.Accept => btnAccept,
            Command.Shortcuts => btnShortcuts,
            _ => null,
        };

        private void Run(Command command)
        {
            switch (ItemFor(command))
            {
                case ToolStripItem item:
                    item.PerformClick();
                    break;

                case Button button:
                    button.PerformClick();
                    break;
            }
        }

        private void btnShortcuts_Click(object? sender, EventArgs e)
        {
            using ShortcutsDialog dialog = new(DescribeShortcuts());
            dialog.ShowDialog(this);
        }

        // ------------------------------------------------------------------------ Barra de estado

        /// <summary>Muestra un mensaje en la barra de estado; si hay una descripción a la vista, espera a que se retire.</summary>
        private void SetStatus(string message)
        {
            if (IsDisposed)
            {
                return;
            }

            lblStatus.ToolTipText = message;
            if (_statusBeforeHint is not null)
            {
                _statusBeforeHint = message;
                return;
            }

            lblStatus.Text = message;
        }

        private void ShowHint(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            _statusBeforeHint ??= lblStatus.Text;
            lblStatus.Text = description;
        }

        private void HideHint()
        {
            if (_statusBeforeHint is not { } previous || IsDisposed)
            {
                return;
            }

            _statusBeforeHint = null;
            lblStatus.Text = previous;
        }

        /// <summary>
        /// Resume la copia en la barra de estado y en el inspector: cuánto durará, cuántos fundidos y
        /// cuánto borrado lleva, y si saldrá sin pérdida o habrá que recodificarla.
        /// </summary>
        private void UpdateSummary()
        {
            TrimRange kept = KeptRange;
            double finalSeconds = Edits?.KeptSeconds(kept) ?? kept.DurationSeconds;
            string clock = Clock(finalSeconds);

            lblFinalValue.Text = clock;
            lblFinalLength.Text = $"⏱ {clock}";
            lblFinalLength.ToolTipText = $"La copia durará {clock}; el original dura {Clock(_durationSeconds)}.";

            int fades = (_fadeIn is null ? 0 : 1) + (_fadeOut is null ? 0 : 1);
            lblFadeCount.Text = $"◢◣ {fades} {(fades == 1 ? "fundido" : "fundidos")}";

            double deleted = _deletions.Within(kept).TotalSeconds;
            lblDeletedTotal.Text = $"⌫ {deleted.ToString("0.00", CultureInfo.CurrentCulture)} s borrados";

            bool reencode = Edits?.Within(kept) is not null;
            lblEncoding.Text = reencode ? "♻ Se recodificará" : "✔ Sin pérdida";
            lblEncoding.ToolTipText = reencode
                ? "Los fundidos y los borrados cambian las muestras: la copia se volverá a codificar al mismo formato."
                : "La copia saldrá por copia de flujo, idéntica al original en lo que conserva.";
        }

        /// <summary>Duración como minutos, segundos y milésimas, la misma precisión que las marcas.</summary>
        private static string Clock(double seconds)
        {
            double rounded = Math.Round(Math.Max(0.0, seconds), 3);
            int minutes = (int)(rounded / 60);
            return $"{minutes:00}:{(rounded - (minutes * 60)).ToString("00.000", CultureInfo.CurrentCulture)}";
        }

        /// <summary>Un atajo del editor.</summary>
        /// <param name="Keys">Teclas que lo disparan.</param>
        /// <param name="Group">Grupo en la ventana de atajos.</param>
        /// <param name="Action">Qué hace, tal como se presenta.</param>
        /// <param name="Command">Acción que ejecuta.</param>
        /// <param name="Listed">Si aparece en la ventana de atajos; los alias no se repiten.</param>
        /// <param name="WhileEditing">Si funciona también mientras se escribe en un campo.</param>
        private sealed record EditorShortcut(Keys Keys, string Group, string Action, Command Command, bool Listed = true, bool WhileEditing = false);
    }
}
