# Registro de Cambios (Changelog)

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto se adhiere a [Versionado Semántico (SemVer)](https://semver.org/lang/es/).

---

## [1.0.0] - 2026-08-27

### Añadido

#### Motor DSP y Procesamiento de Audio (`EchoCut.Core`)
* **`SilenceDetector`**: Núcleo matemático puro y determinista para detección de silencio en memoria. Incorpora disparador de Schmitt con histéresis (6 dB), piso adaptativo de ruido, protección contra sobrecorte en másters silenciosos y verificación de decaimiento en colas de reverberación.
* **`Biquad`**: Filtro paso alto bilineal de 2.° orden calibrado con los parámetros RLB de la norma EBU R128 a ~38.14 Hz, portado de Audacity. Normalizado a ganancia unitaria pasante e inicializado (*primed*) para evitar transitorios por *offset* de continua o retumbes subsónicos de vinilo.
* **`LevelFramer`**: Procesamiento de muestras PCM en streaming mono a 22,050 Hz. Reduce el audio a curvas de nivel RMS por trama (20 ms) gestionando memoria con `ArrayPool<byte>` y `ArrayPool<double>` sin generar presión sobre el *Large Object Heap* (LOH).
* **Detección de Fundidos (*Fade-Outs*)**: Algoritmo de regresión lineal por mínimos cuadrados ($R^2 \ge 0.5$, pendiente $\le -6.0$ dB/s) que detecta desvanecimientos musicales naturales y aplica automáticamente un margen de guarda (`FadeGuardSeconds`).
* **Afinación de Corte en Frontera Silenciosa (`RefineCut`)**: Búsqueda del límite de trama con menor nivel acústico dentro de una ventana de tolerancia para atenuar clics o chasquidos de truncamiento.
* **`SilenceAnalyzer`**: Algoritmo de sondeo progresivo de cola (*progressive tail probing*) que inicia analizando los últimos 30 segundos y cuadruplica la ventana solo si toda la muestra es silencio, optimizando drásticamente la decodificación.

#### Integración con FFmpeg y Recorte sin Pérdidas
* **`AudioTrimmer`**: Recorte por copia de flujo directo (`-c copy`) sin recodificación. Preserva el 100% de la calidad original, conserva etiquetas y metadatos ID3v2 (`-map 0`, `-map_metadata 0`, `-id3v2_version 3`) y escribe exclusivamente en la subcarpeta `Recortados/`.
* **`AudioDecoder`**: Decodificación eficiente de colas de audio mediante `-sseof` en formato crudo de coma flotante (`f32le`).
* **`FFmpegLocator`**: Localizador robusto de `ffmpeg.exe` y `ffprobe.exe`, con soporte para el `PATH` del sistema, configuración persistida y diálogo interactivo con validación de ejecutables.
* **`FFmpegRunner`**: Ejecución asíncrona segura de subprocesos con drenado en paralelo de tuberías (`stdout` y `stderr`) y terminación limpia de árboles de procesos huérfanos al cancelar (`KillQuietly`).

#### Biblioteca, Escaneo y Formatos
* **`TrackScanner`**: Escaneo rápido de carpetas y extracción de metadatos mediante `TagLibSharp`. Reutiliza las duraciones medidas para evitar llamadas innecesarias a `ffprobe`.
* **Soporte Multiformato**: Compatibilidad verificada con 19 extensiones de audio: `.mp3`, `.flac`, `.wav`, `.aac`, `.m4a`, `.m4b`, `.m4p`, `.ogg`, `.oga`, `.wma`, `.aiff`, `.ape`, `.wv`, `.mpc`, `.mpp`, `.dsf`, `.webm`, `.aa` y `.aax`.
* **`TrackFormat`**: Formateador centralizado para duraciones (`mm:ss`), bitrates (`Kbps`) y tamaños (`MB`), garantizando consistencia absoluta entre la interfaz y las exportaciones.

#### Procesamiento Concurrente y Lotes
* **`BatchRunner` y `BatchProcessor`**: Ejecución en paralelo sobre `Parallel.ForEachAsync` con grado de concurrencia acotado (1 a 8 hilos por defecto según núcleos de CPU) para no saturar la E/S de disco.
* Soporte integral de cancelación cooperativa mediante `CancellationToken` y notificaciones de progreso hilo-seguras con `IProgress<T>`.

#### Interfaz de Usuario y Accesibilidad (`EchoCut`)
* **Rejilla Avanzada con `SortableBindingList<Song>`**: Enlace de datos con soporte completo de ordenación bidireccional por cabecera y desempate estable por nombre de archivo.
* **Previsualización Auditiva Contextual (`▶` / `⏹`)**: Reproducción instantánea de los últimos 3 segundos musicales más la tolerancia configurada antes del punto de corte mediante `AudioPreviewPlayer` y `SoundPlayer`.
* **Recorte Individual (`✂` / `✔` / `·`)**: Capacidad de recortar pistas individuales directamente desde la fila correspondiente de la rejilla.
* **Diseño Visual Accesible (WCAG 2.1 AAA)**: Contraste superior a 7:1 en fondos normales y conmutación a paleta luminosa sobre fondo de selección azul medianoche (`#17426c`), apta para usuarios con baja visión y daltonismo (protanopía y deuteranopía), respaldada por glifos semánticos redundantes.
* **Diálogo de Parámetros Avanzados (`AdvancedOptions`)**: Interfaz autogenerada mediante `PropertyGrid` que expone los 21 parámetros del algoritmo agrupados en 5 categorías (Umbral, Medición, Decisión, Fundido y Corte).
* **Integración con el Shell de Windows**: Opciones en menú contextual para *Mostrar en el Explorador* y *Abrir en Audacity* para inspección espectral manual.
* **`CsvExporter`**: Exportador de informes a CSV codificado en UTF-8 con marca de orden de bytes (BOM) y separador de lista regional del sistema operativo, permitiendo su apertura inmediata en Microsoft Excel sin asistentes de importación.
* **Persistencia de Preferencias (`AppSettings`)**: Guardado automático de tolerancias, hilos de trabajo y ubicación de FFmpeg en `%APPDATA%\EchoCut\settings.json`.

### Arquitectura
* **Aislamiento Arquitectónico Estricto**: Separación en dos proyectos independientes: `EchoCut.Core` configurado para `net10.0` puro (impidiendo dependencias de WinForms en el compilador) y `EchoCut` configurado para `net10.0-windows` enfocado exclusivamente en la presentación.
