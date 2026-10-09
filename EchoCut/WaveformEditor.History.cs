using EchoCut.Audio;

namespace EchoCut
{
    /// <summary>
    /// La parte de <see cref="WaveformEditor"/> que recuerda los cambios para deshacerlos y
    /// rehacerlos, y que sabe si hay algo sin llevar a la fila.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Se guardan instantáneas completas de lo que decide el editor —tramo, fundidos y borrados—, no
    /// operaciones: todo es inmutable y pequeño, y así deshacer es simplemente volver a una foto
    /// anterior, sin lógica inversa por cada acción.
    /// </para>
    /// <para>
    /// Un arrastre produce decenas de avisos; todos cuentan como un solo cambio. La instantánea se
    /// toma con el primero y no se vuelve a tomar hasta soltar el ratón.
    /// </para>
    /// </remarks>
    public partial class WaveformEditor
    {
        /// <summary>Cambios que se pueden deshacer: más que suficientes para una sesión de edición.</summary>
        private const int MaximumHistory = 100;

        private readonly LinkedList<EditorState> _undo = new();
        private readonly Stack<EditorState> _redo = new();

        /// <summary>Si ya se tomó la instantánea del arrastre en curso.</summary>
        private bool _gestureOpen;

        /// <summary>Si se está restaurando una instantánea: esos cambios no son del usuario.</summary>
        private bool _restoring;

        /// <summary>Lo que había la última vez que se llevaron las decisiones a la fila.</summary>
        private EditorState _applied = EditorState.Empty;

        /// <value><c>true</c> si hay cambios que todavía no se llevaron a la fila.</value>
        private bool IsDirty => !Snapshot().SameAs(_applied);

        /// <summary>Recuerda cómo estaba todo justo antes de que el usuario cambie algo.</summary>
        /// <remarks>Se llama antes de mutar; dentro de un arrastre, solo la primera vez.</remarks>
        private void BeginChange()
        {
            if (_restoring || _gestureOpen)
            {
                return;
            }

            _undo.AddLast(Snapshot());
            if (_undo.Count > MaximumHistory)
            {
                _undo.RemoveFirst();
            }

            _redo.Clear();
            _gestureOpen = MouseButtons != MouseButtons.None;
        }

        /// <summary>Refleja que hubo un cambio: botones de deshacer y resumen.</summary>
        private void EndChange()
        {
            UpdateHistoryButtons();
            UpdateSummary();
        }

        /// <summary>Al soltar el ratón termina el arrastre: el siguiente cambio ya es otro.</summary>
        private void View_MouseUp(object? sender, MouseEventArgs e) => _gestureOpen = false;

        private void btnUndo_Click(object? sender, EventArgs e) => Undo();

        private void btnRedo_Click(object? sender, EventArgs e) => Redo();

        private void Undo()
        {
            if (_undo.Last is not { } last)
            {
                return;
            }

            _undo.RemoveLast();
            _redo.Push(Snapshot());
            Restore(last.Value);
            SetStatus("Se deshizo el último cambio.");
        }

        private void Redo()
        {
            if (!_redo.TryPop(out EditorState? next))
            {
                return;
            }

            _undo.AddLast(Snapshot());
            Restore(next);
            SetStatus("Se rehízo el cambio.");
        }

        /// <summary>Toma la foto de lo que decide el editor ahora mismo.</summary>
        private EditorState Snapshot() => new(_startSeconds, _endSeconds, _manual, _fadeIn, _fadeOut, _deletions);

        /// <summary>Vuelve a una foto anterior y lo lleva a toda la ventana.</summary>
        /// <remarks>
        /// La selección no forma parte de la foto: tras deshacer podría señalar algo que ya no está, así
        /// que se quita, y el inspector vuelve a su estado de reposo.
        /// </remarks>
        private void Restore(EditorState state)
        {
            _restoring = true;
            try
            {
                _manual = state.Manual;
                _fadeIn = state.FadeIn;
                _fadeOut = state.FadeOut;
                _deletions = state.Deletions;
                ApplyRange(state.StartSeconds, state.EndSeconds);
                ApplyFades();
                ApplyDeletions();
                ApplySelection(null);
                ShowInspector(InspectorTarget.None);
            }
            finally
            {
                _restoring = false;
            }

            _gestureOpen = false;
            UpdateHistoryButtons();
            UpdateSummary();
        }

        /// <summary>Anota que lo que hay ahora es lo que tiene la fila.</summary>
        private void MarkApplied()
        {
            _applied = Snapshot();
            UpdateHistoryButtons();
        }

        private void UpdateHistoryButtons()
        {
            btnUndo.Enabled = _undo.Count > 0;
            btnRedo.Enabled = _redo.Count > 0;
        }

        /// <summary>Foto de lo que decide el editor.</summary>
        /// <param name="StartSeconds">Comienzo de la copia.</param>
        /// <param name="EndSeconds">Final de la copia.</param>
        /// <param name="Manual">Si el tramo es un ajuste manual.</param>
        /// <param name="FadeIn">Aparición, o <c>null</c>.</param>
        /// <param name="FadeOut">Desaparición, o <c>null</c>.</param>
        /// <param name="Deletions">Fragmentos borrados.</param>
        private sealed record EditorState(double StartSeconds, double EndSeconds, bool Manual, Fade? FadeIn, Fade? FadeOut, DeletedRegions Deletions)
        {
            /// <value>Estado que no coincide con ningún otro, para antes de la primera foto.</value>
            public static EditorState Empty { get; } = new(double.NaN, double.NaN, false, null, null, DeletedRegions.Empty);

            /// <summary>Compara dos fotos por su contenido, incluidos los fragmentos borrados.</summary>
            /// <remarks>
            /// La igualdad del registro compararía <see cref="DeletedRegions"/> por referencia, y dos
            /// conjuntos iguales construidos por caminos distintos se tomarían por cambios.
            /// </remarks>
            public bool SameAs(EditorState other) =>
                StartSeconds.Equals(other.StartSeconds)
                && EndSeconds.Equals(other.EndSeconds)
                && Manual == other.Manual
                && FadeIn == other.FadeIn
                && FadeOut == other.FadeOut
                && Deletions.Regions.SequenceEqual(other.Deletions.Regions);
        }
    }
}
