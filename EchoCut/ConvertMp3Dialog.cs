using EchoCut.Audio;
using EchoCut.Processing;

namespace EchoCut
{
    /// <summary>Diálogo para elegir la calidad con la que «Convertir a MP3» codifica las canciones.</summary>
    /// <remarks>
    /// Cada calidad lleva una línea que explica cuándo conviene: «VBR V0» o «CBR 320» no le dicen nada
    /// a quien no conoce LAME, y la diferencia entre tasa variable y constante es la que decide.
    /// </remarks>
    public partial class ConvertMp3Dialog : Form
    {
        /// <summary>Opciones del diálogo, con la explicación que acompaña a cada calidad.</summary>
        private static readonly (Mp3Quality Quality, string Hint)[] Options =
        [
            (Mp3Quality.VbrV0, "Recomendado: transparente y más ligero que 320 kbps."),
            (Mp3Quality.VbrV2, "Muy buena calidad con menos tamaño; ideal para el móvil."),
            (Mp3Quality.Cbr320, "La máxima del formato, para reproductores antiguos."),
            (Mp3Quality.Cbr192, "Tamaño contenido y compatible con todo."),
        ];

        /// <summary>Crea el diálogo.</summary>
        /// <param name="selected">Calidad marcada al abrir; normalmente, la de la última vez.</param>
        /// <param name="convertCount">Canciones que se convertirán.</param>
        /// <param name="skippedCount">Canciones que se omitirán por ser ya MP3.</param>
        public ConvertMp3Dialog(Mp3Quality selected, int convertCount, int skippedCount)
        {
            InitializeComponent();

            string songs = convertCount == 1 ? "1 canción" : $"{convertCount} canciones";
            string skipped = skippedCount switch
            {
                0 => string.Empty,
                1 => " Se omite 1 que ya es MP3: volver a codificarla solo le quitaría calidad.",
                _ => $" Se omiten {skippedCount} que ya son MP3: volver a codificarlas solo les quitaría calidad.",
            };

            lblIntro.Text = $"Se convertirán {songs} a MP3 con la calidad que elijas.{skipped}";

            RadioButton[] radios = [rdoVbrV0, rdoVbrV2, rdoCbr320, rdoCbr192];
            for (int i = 0; i < Options.Length; i++)
            {
                radios[i].Text = $"{Options[i].Quality.DisplayName()}{Environment.NewLine}{Options[i].Hint}";
                radios[i].Tag = Options[i].Quality;
                radios[i].Checked = Options[i].Quality == selected;
            }

            lblNote.Text = $"Las copias se escriben en la subcarpeta «{ConversionService.OutputFolderName}» junto a cada original, con sus etiquetas y su carátula. Los originales no se modifican.";
        }

        /// <summary>Calidad elegida. Solo es definitiva si el diálogo devolvió OK.</summary>
        /// <value>La calidad de la opción marcada.</value>
        public Mp3Quality Selected =>
            new[] { rdoVbrV0, rdoVbrV2, rdoCbr320, rdoCbr192 }.FirstOrDefault(radio => radio.Checked)?.Tag is Mp3Quality quality
                ? quality
                : Mp3Quality.VbrV0;
    }
}
