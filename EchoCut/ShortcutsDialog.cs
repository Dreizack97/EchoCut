namespace EchoCut
{
    /// <summary>
    /// Ventana de referencia con todos los atajos de teclado, agrupados por dónde funcionan.
    /// </summary>
    /// <remarks>
    /// Los tooltips y la barra de estado solo enseñan el atajo de la opción sobre la que está el
    /// ratón; esta ventana es el único sitio donde se ven todos a la vez, incluidos los que no tienen
    /// opción de menú, como los de la forma de onda.
    /// </remarks>
    public partial class ShortcutsDialog : Form
    {
        /// <summary>Crea la ventana con los atajos indicados.</summary>
        /// <param name="entries">Atajos ya ordenados; los grupos aparecen en el orden en que llegan.</param>
        public ShortcutsDialog(IReadOnlyList<ShortcutEntry> entries)
        {
            InitializeComponent();

            Dictionary<string, ListViewGroup> groups = [];

            lstShortcuts.BeginUpdate();
            foreach (ShortcutEntry entry in entries)
            {
                if (!groups.TryGetValue(entry.Group, out ListViewGroup? group))
                {
                    group = new ListViewGroup(entry.Group, entry.Group);
                    groups[entry.Group] = group;
                    lstShortcuts.Groups.Add(group);
                }

                lstShortcuts.Items.Add(new ListViewItem([entry.Action, entry.Keys], group));
            }

            lstShortcuts.EndUpdate();
        }
    }
}
