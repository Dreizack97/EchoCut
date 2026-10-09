namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="Main"/> que define los atajos de teclado y los da a conocer: en los
    /// menús y en los tooltips.
    /// </summary>
    /// <remarks>
    /// Todos salen de una sola tabla, <see cref="_shortcuts"/>: lo que se anuncia y lo que funciona
    /// no pueden desincronizarse, porque el texto de cada atajo se genera a partir de la tecla que
    /// lo dispara.
    /// </remarks>
    public partial class Main
    {
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
        /// <param name="Scope">Dónde funciona.</param>
        private sealed record ShortcutBinding(Keys Keys, ToolStripItem Item, ShortcutScope Scope = ShortcutScope.Window);

        private IReadOnlyList<ShortcutBinding> _shortcuts = [];

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
                new(Keys.Control | Keys.O, btnFile),
                new(Keys.Control | Keys.Shift | Keys.O, btnPath),
                new(Keys.F5, btnAnalyze),
                new(Keys.Control | Keys.R, btnCropAll),
                new(Keys.Escape, btnStop),
                new(Keys.Control | Keys.E, btnExport),
                new(Keys.Control | Keys.M, mnuAddMetadata),
                new(Keys.Control | Keys.N, mnuNormalize),
                new(Keys.Control | Keys.Shift | Keys.C, mnuAddSequence),
                new(Keys.Control | Keys.Shift | Keys.Q, mnuRemoveLeading),

                // Entrar abre la opción en negrita del menú contextual, como el verbo por defecto
                // en el Explorador, y Alt+Entrar las propiedades, como allí.
                new(Keys.Enter, mnuWaveform, ShortcutScope.Grid),
                new(Keys.F2, mnuRenameSong, ShortcutScope.Grid),
                new(Keys.Alt | Keys.Enter, mnuEditSong, ShortcutScope.Grid),
                new(Keys.Control | Keys.Shift | Keys.E, mnuOpenFolder, ShortcutScope.Grid),
                new(Keys.Delete, mnuDeleteSong, ShortcutScope.Grid),

                new(Keys.Control | Keys.Oemcomma, btnAdvanced),
            ];

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
    }
}
