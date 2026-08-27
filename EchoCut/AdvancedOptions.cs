using EchoCut.Audio;

namespace EchoCut
{
    /// <summary>
    /// Diálogo con los parámetros del algoritmo que no caben en la ventana principal.
    /// La tolerancia de recorte también vive aquí, pero se sigue ajustando desde la ventana
    /// principal porque es el único parámetro que se toca a diario.
    /// </summary>
    /// <remarks>
    /// La rejilla se construye a partir de los atributos de <see cref="SilenceOptions"/> en lugar de
    /// llevar un control por parámetro. Con veintiún parámetros, la versión manual obligaba a tocar
    /// tres sitios —el diseñador, la carga y el volcado— por cada uno que se añadiera, y de hecho
    /// solo siete llegaron a exponerse: los otros catorce únicamente se podían cambiar editando el
    /// JSON a mano.
    /// </remarks>
    public partial class AdvancedOptions : Form
    {
        private SilenceOptions _options;

        /// <summary>Crea el diálogo sobre una copia de <paramref name="options"/>.</summary>
        /// <param name="options">
        /// Parámetros de partida. No se modifican directamente: <see cref="AdvancedOptions"/> trabaja
        /// sobre <see cref="SilenceOptions.Clone"/> para que cerrar con «Cancelar» no deje cambios a
        /// medias en la instancia del llamador.
        /// </param>
        public AdvancedOptions(SilenceOptions options)
        {
            InitializeComponent();

            // Se trabaja sobre una copia para que "Cancelar" no deje cambios a medias.
            _options = options.Clone();
            _options.Normalize();
            propertyGrid.SelectedObject = _options;
        }

        /// <summary>Parámetros resultantes. Solo son definitivos si el diálogo devolvió OK.</summary>
        /// <value>Copia de <see cref="SilenceOptions"/> editada por el usuario, ya acotada y validada.</value>
        public SilenceOptions Result => _options;

        private void btnDefaults_Click(object sender, EventArgs e)
        {
            _options = new SilenceOptions();
            propertyGrid.SelectedObject = _options;
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            // La rejilla no valida por celda, así que el acotado se aplica al aceptar. Refrescarla
            // después deja a la vista el valor que de verdad se va a guardar.
            _options.Normalize();
            propertyGrid.Refresh();

            if (_options.Validate() is { } problem)
            {
                MessageBox.Show(
                    this,
                    problem,
                    "Parámetros incoherentes",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.None;
            }
        }
    }
}
