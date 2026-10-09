using EchoCut.Shell;

namespace EchoCut
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        /// <param name="args">
        /// Archivos o carpetas a cargar; los pasa el menú contextual del Explorador.
        /// </param>
        /// <remarks>
        /// Si ya hay una ventana abierta, este proceso solo le entrega sus rutas y termina. Si la
        /// entrega falla —la otra ventana se está cerrando—, abre su propia ventana en lugar de
        /// perder las rutas.
        /// </remarks>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            using SingleInstance instance = new();
            if (!instance.IsPrimary && SingleInstance.TrySend(args))
            {
                return;
            }

            Main main = new();
            main.EnqueuePaths(args);

            if (instance.IsPrimary)
            {
                instance.Listen(main.EnqueuePaths);
            }

            Application.Run(main);
        }
    }
}