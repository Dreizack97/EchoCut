namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> que define los atajos de teclado y los da a conocer: en los
    /// menús, en los tooltips, en la barra de estado y en la ventana «Atajos de teclado».
    /// </summary>
    /// <remarks>
    /// Todos salen de una sola tabla, <see cref="_shortcuts"/>: lo que se anuncia y lo que funciona
    /// no pueden desincronizarse, porque el texto de cada atajo se genera a partir de la tecla que
    /// lo dispara.
    /// </remarks>
    public partial class Main
    {
        private const string GroupOpen = "Abrir canciones";
        private const string GroupProcess = "Procesar";
        private const string GroupUtilities = "Utilidades";
        private const string GroupGrid = "Rejilla (con la lista seleccionada)";
        private const string GroupFilter = "Filtro";
        private const string GroupWaveform = "Ventana de forma de onda";
        private const string GroupHelp = "Ayuda y opciones";

        /// <summary>Orden en que se presentan los grupos en la ventana de atajos.</summary>
        private static readonly string[] GroupOrder =
            [GroupOpen, GroupProcess, GroupUtilities, GroupGrid, GroupFilter, GroupWaveform, GroupHelp];

        /// <summary>Dónde funciona un atajo.</summary>
        private enum ShortcutScope
        {
            /// <summary>En cualquier parte de la ventana.</summary>
            Window,

            /// <summary>Solo con el foco en la rejilla: actúa sobre la fila actual o la selección.</summary>
            Grid,
        }

        /// <summary>Atajo que dispara una opción de la barra o de un menú.</summary>
        /// <param name="Keys">Tecla con sus modificadores.</param>
        /// <param name="Item">Botón u opción de menú que ejecuta.</param>
        /// <param name="Group">Grupo con el que se presenta.</param>
        /// <param name="Scope">Dónde funciona.</param>
        private sealed record ShortcutBinding(Keys Keys, ToolStripItem Item, string Group, ShortcutScope Scope = ShortcutScope.Window);

        private IReadOnlyList<ShortcutBinding> _shortcuts = [];

        /// <summary>Descripción de cada opción, sin el atajo, para la barra de estado.</summary>
        private readonly Dictionary<ToolStripItem, string> _descriptions = [];

        /// <summary>
        /// Mensaje que había en la barra de estado antes de mostrar una descripción, o <c>null</c>
        /// si no se está mostrando ninguna.
        /// </summary>
        private string? _statusBeforeHint;

        /// <summary>Registra los atajos y anuncia cada uno junto a su opción.</summary>
        /// <remarks>
        /// <see cref="ToolStripButton"/> no admite <c>ShortcutKeys</c>, y los de las opciones de
        /// menú solo se atienden con el menú abierto o en su formulario; se resuelven todos en
        /// <see cref="ProcessCmdKey"/> y aquí solo se muestran: en el menú, a la derecha de la
        /// opción; en los botones, al final de su tooltip.
        /// </remarks>
        private void InitializeShortcuts()
        {
            _shortcuts =
            [
                new(Keys.Control | Keys.O, btnFile, GroupOpen),
                new(Keys.Control | Keys.Shift | Keys.O, btnPath, GroupOpen),
                new(Keys.F5, btnAnalyze, GroupProcess),
                new(Keys.Control | Keys.R, btnCropAll, GroupProcess),
                new(Keys.Escape, btnStop, GroupProcess),
                new(Keys.Control | Keys.E, btnExport, GroupProcess),
                new(Keys.Control | Keys.M, mnuAddMetadata, GroupUtilities),
                new(Keys.Control | Keys.N, mnuNormalize, GroupUtilities),
                new(Keys.Control | Keys.Shift | Keys.C, mnuAddSequence, GroupUtilities),
                new(Keys.Control | Keys.Shift | Keys.Q, mnuRemoveLeading, GroupUtilities),
                new(Keys.Control | Keys.Shift | Keys.M, mnuConvertMp3, GroupUtilities),
                new(Keys.Control | Keys.Shift | Keys.D, mnuFindDuplicates, GroupUtilities),

                // Entrar abre la opción en negrita del menú contextual, como el verbo por defecto
                // en el Explorador, y Alt+Entrar las propiedades, como allí.
                new(Keys.Enter, mnuWaveform, GroupGrid, ShortcutScope.Grid),
                new(Keys.F2, mnuRenameSong, GroupGrid, ShortcutScope.Grid),
                new(Keys.Alt | Keys.Enter, mnuEditSong, GroupGrid, ShortcutScope.Grid),
                new(Keys.Control | Keys.Shift | Keys.E, mnuOpenFolder, GroupGrid, ShortcutScope.Grid),
                new(Keys.Delete, mnuDeleteSong, GroupGrid, ShortcutScope.Grid),

                new(Keys.Control | Keys.Oemcomma, btnAdvanced, GroupHelp),
                new(Keys.F1, btnShortcuts, GroupHelp),
            ];

            // Las descripciones se toman antes de añadir el atajo a los tooltips.
            HookHints(toolStrip.Items);
            HookHints(contextMenuStrip.Items);
            contextMenuStrip.Closed += (_, _) => HideHint();

            foreach (ShortcutBinding shortcut in _shortcuts)
            {
                string keys = ShortcutText.Of(shortcut.Keys);

                if (shortcut.Item is ToolStripMenuItem menuItem)
                {
                    menuItem.ShortcutKeyDisplayString = keys;
                }
                else
                {
                    shortcut.Item.ToolTipText = $"{shortcut.Item.ToolTipText} ({keys})";
                }
            }
        }

        /// <summary>
        /// Resuelve los atajos de la tabla y los dos propios del filtro.
        /// </summary>
        /// <remarks>
        /// Ctrl+F lleva al filtro y, dentro de él, Esc lo vacía antes que detener un lote: es la
        /// acción más cercana a donde está el foco. Un atajo cuya opción está deshabilitada no se
        /// consume, para no robarle la tecla al control que tiene el foco; así Esc solo detiene
        /// cuando hay algo que detener.
        /// </remarks>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                txtFilter.Focus();
                txtFilter.SelectAll();
                return true;
            }

            if (keyData == Keys.Escape && txtFilter.Focused && txtFilter.TextLength > 0)
            {
                ClearFilter();
                return true;
            }

            if (_shortcuts.FirstOrDefault(shortcut => shortcut.Keys == keyData) is { } match && TryRun(match))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>Ejecuta la opción de un atajo si está disponible.</summary>
        /// <returns><c>true</c> si se ejecutó y la tecla queda consumida.</returns>
        /// <remarks>
        /// Los atajos de la rejilla refrescan antes el estado del menú contextual, que de otro modo
        /// solo se actualiza al abrirlo con el ratón. La disponibilidad se comprueba subiendo por los
        /// menús que contienen la opción: un submenú de «Utilidades» no debe ejecutarse mientras
        /// «Utilidades» está deshabilitado por un lote en curso.
        /// </remarks>
        private bool TryRun(ShortcutBinding shortcut)
        {
            if (shortcut.Scope == ShortcutScope.Grid)
            {
                if (!dataGrid.ContainsFocus)
                {
                    return false;
                }

                UpdateRowMenuState();
            }

            for (ToolStripItem? item = shortcut.Item; item is not null; item = item.OwnerItem)
            {
                if (!item.Enabled)
                {
                    return false;
                }
            }

            shortcut.Item.PerformClick();
            return true;
        }

        // --------------------------------------------------------------- Descripción en la barra

        /// <summary>
        /// Muestra en la barra de estado qué hace cada opción, y su atajo, al pasar el ratón por ella.
        /// </summary>
        /// <remarks>
        /// La descripción se toma del tooltip de cada opción; en los menús no se muestra como
        /// tooltip, porque sus desplegables no los muestran, pero sirve de texto para la barra.
        /// </remarks>
        private void HookHints(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripSeparator)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.ToolTipText))
                {
                    _descriptions[item] = item.ToolTipText;
                }

                item.MouseEnter += (_, _) => ShowHint(item);
                item.MouseLeave += (_, _) => HideHint();

                if (item is ToolStripDropDownItem { DropDownItems.Count: > 0 } dropDown)
                {
                    dropDown.DropDown.Closed += (_, _) => HideHint();
                    HookHints(dropDown.DropDownItems);
                }
            }
        }

        private void ShowHint(ToolStripItem item)
        {
            string? keys = _shortcuts.FirstOrDefault(shortcut => shortcut.Item == item) is { } shortcut
                ? ShortcutText.Of(shortcut.Keys)
                : null;

            string? description = _descriptions.GetValueOrDefault(item);
            if (description is null && keys is null)
            {
                return;
            }

            _statusBeforeHint ??= lblStatus.Text;
            lblStatus.Text = (description, keys) switch
            {
                (null, _) => $"{ActionName(item)} · Atajo: {keys}",
                (_, null) => description,
                _ => $"{description} · Atajo: {keys}",
            };
        }

        /// <summary>Devuelve a la barra de estado el mensaje que había antes de la descripción.</summary>
        private void HideHint()
        {
            if (_statusBeforeHint is not { } previous || !IsAlive)
            {
                return;
            }

            _statusBeforeHint = null;
            lblStatus.Text = previous;
        }

        // ----------------------------------------------------------------- Ventana de atajos

        private void btnShortcuts_Click(object? sender, EventArgs e)
        {
            using ShortcutsDialog dialog = new(DescribeShortcuts());
            dialog.ShowDialog(this);
        }

        /// <summary>Todos los atajos de la aplicación, agrupados y en orden de presentación.</summary>
        /// <remarks>
        /// A los de la tabla se suman los que no disparan una opción de menú —el doble clic, las
        /// teclas del filtro, del cuadro de renombrar y de la forma de onda— para que la ventana
        /// sea la referencia completa.
        /// </remarks>
        private IReadOnlyList<ShortcutEntry> DescribeShortcuts()
        {
            List<ShortcutEntry> entries =
            [
                .. _shortcuts.Select(shortcut => new ShortcutEntry(shortcut.Group, ActionName(shortcut.Item), ShortcutText.Of(shortcut.Keys))),
                new(GroupGrid, "Abrir en Audacity", "Doble clic"),
                new(GroupGrid, "Confirmar o descartar el nombre al renombrar", "Entrar / Esc"),
                new(GroupFilter, "Ir al filtro por nombre", ShortcutText.Of(Keys.Control | Keys.F)),
                new(GroupFilter, "Vaciar el filtro", "Esc"),

                // Los del editor salen de su propia tabla, la misma que atiende sus teclas.
                .. WaveformEditor.DescribeShortcuts().Select(entry => entry with { Group = GroupWaveform }),
            ];

            // OrderBy es estable: dentro de cada grupo se conserva el orden de la lista.
            return [.. entries.OrderBy(entry => Array.IndexOf(GroupOrder, entry.Group))];
        }

        /// <summary>Nombre de una opción sin glifos decorativos ni puntos suspensivos.</summary>
        private static string ActionName(ToolStripItem item)
        {
            string text = (item.Text ?? string.Empty).Replace("&", string.Empty).TrimEnd('…').Trim();
            int start = 0;
            while (start < text.Length && !char.IsLetterOrDigit(text[start]))
            {
                start++;
            }

            return text[start..];
        }
    }
}
