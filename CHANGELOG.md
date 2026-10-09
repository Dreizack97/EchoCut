# Registro de Cambios (Changelog)

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto se adhiere a [Versionado Semántico (SemVer)](https://semver.org/lang/es/).

---

## [Sin publicar]

### Añadido
#### Conversión a MP3 y Duplicados por Audio
* **Convertir a MP3** (`Utilidades`, `Ctrl+Shift+M`): Copia en MP3 de las canciones cargadas en la subcarpeta `MP3/` junto a cada original, con etiquetas y carátula; calidad VBR V0, VBR V2, CBR 320 o CBR 192, recordada en `AppSettings.Mp3Quality`. Las que ya son MP3 se omiten. `Mp3Converter`, `Mp3Quality` y `ConversionService` en el motor.
* **Buscar duplicados por audio** (`Utilidades`, `Ctrl+Shift+D`): Huella acústica propia según Haitsma y Kalker —33 bandas logarítmicas entre 300 y 2000 Hz, 32 bits por trama de 23 ms— en `EchoCut.Fingerprints` (`FftPlan`, `FingerprintBuilder`, `AudioFingerprint`), con búsqueda de candidatas por índice invertido y verificación por tasa de bits distintos tras alinear (`DuplicateDetector`). Reconoce la misma grabación en otro formato, tasa de bits o volumen y con silencios distintos; exige que coincida el 80 % de lo que suena en ambas para no confundir una mezcla con la canción que contiene.
* **Ventana de duplicados**: Grupos con parecido, duración, formato, bitrate, tamaño y carpeta; propone conservar la copia de mejor calidad (`DuplicateFinder`), permite escuchar, abrir la ubicación y cambiar las marcas, y envía las marcadas a la Papelera de reciclaje tras confirmarlo, retirando sus filas del listado.

#### Resultado Editado y Detección de Silencios Posteriores
* **Onda con las ediciones aplicadas**: `WaveformRenderer` pinta cada columna con la ganancia de los fundidos y, en la línea de tiempo del resultado, juntando los tramos del original que suenan en ella; se dibuja a partir del resumen ya cargado, sin volver a decodificar.
* **«Ver resultado»** (`Ctrl+R`): Las tres vistas pasan a la línea de tiempo de la copia, sin lo borrado y con los empalmes marcados en morado; el detalle del final usa la pista completa para poder juntar lo que queda a ambos lados de un borrado.
* **Ajuste del recorte sobre el resultado**: Con «Ver resultado» activo, las marcas de inicio y final se arrastran o se mueven con el teclado, y los campos de «Copia» muestran y aceptan tiempos del resultado; todo se traduce al original, y un borde llevado a un empalme o al final del resultado queda exactamente ahí. `WaveformView.MarkersOnly` deja mover solo las marcas, y un clic marca desde dónde escuchar. Deshacer sigue disponible y rehace la vista si cambia lo borrado.
* **«Detectar silencios»** (`F5`): Analiza el resultado editado y propone el comienzo y el final de la copia; se deshace con `Ctrl+Z` y la fila adopta el análisis al aceptar o guardar.
* **`SilenceAnalyzer.AnalyzeEditedAsync` y `EditedSampleSink`**: Análisis del resultado decodificando el archivo completo a través de `PcmEditor`, el mismo código que escribe la copia, porque un borrado impide el sondeo de la cola con `-sseof`.
* **`TrackAnalysis.AnalyzedEdits` y `Timeline`**: Los silencios se miden en el resultado y los cortes se traducen al original; la traducción sobrevive a los cambios de tolerancia. `DeletedRegions` gana `OutputSecondsAt` y `SourceSpans` para traducir entre ambas líneas de tiempo.
* **Análisis por lote del resultado**: `AnalysisRequest` lleva las ediciones de cada fila; las editadas se analizan sobre su resultado y las demás siguen con el sondeo rápido de los bordes.

### Cambiado
* **`Song.ApplyEdits`** descarta un análisis hecho sobre el resultado si las ediciones cambian, porque sus silencios eran los de otra copia.
* **Editor de forma de onda**: Recibe sus servicios en `WaveformEditorServices`; los botones de zoom y la escala en dB se compactan para que la barra quepa entera, y la ventana se abre a 1280 × 820.

#### Editor de Forma de Onda Rediseñado
* **Estilo de la ventana principal**: Barra de herramientas con las acciones agrupadas (reproducir | aplicar a la selección | deshacer | zoom, con ayuda y escala en dB a la derecha) y barra de estado que describe cada opción al pasar el ratón y resume la copia: duración final, fundidos, borrado y si saldrá sin pérdida o se recodificará. Desaparecen los marcos de grupo y la barra de selección.
* **Inspector**: Panel a la derecha con la copia (comienzo, final, duración final y escucha de los bordes) y una sección contextual para la selección, un fundido —con su curva dibujada y su nivel a mitad de camino— o un fragmento borrado, todos con sus instantes editables. Un clic sobre un fundido o sobre lo borrado lo muestra.
* **Deshacer y rehacer** (`Ctrl+Z` / `Ctrl+Y`): Historial de instantáneas del tramo, los fundidos y los borrados; un arrastre cuenta como un solo cambio, y el aviso de cambios sin aplicar compara con lo que tiene la fila.
* **Reproducción general** (`Espacio`): Suena la selección o, sin ella, desde el punto marcado con un clic, con las ediciones aplicadas.
* **Zoom y desplazamiento**: Rueda para desplazar y `Ctrl`+rueda para acercar bajo el puntero en cualquier vista, además de *Acercar*, *Alejar*, *Ver selección* y *Ver todo* sobre la última vista tocada; la vista del final no sale del audio cargado.
* **Tabla única de atajos**: Los del editor se atienden, se anuncian en tooltips y barra de estado, se listan en su ventana *Atajos* (`F1`) y aparecen en la de la ventana principal desde la misma tabla.

#### Selección, Borrado y Ventanas de Forma de Onda
* **Seleccionar y luego actuar**: Como en Audacity, se arrastra en cualquiera de las tres vistas —también la de la pista completa— para seleccionar un tramo, compartido por todas, y se le aplica una aparición, una desaparición, un borrado o una restauración desde la nueva barra de selección. Los bordes de la selección y de los fundidos se arrastran en cualquier vista.
* **Borrar selección** (`Supr`): Quita el audio seleccionado y une lo anterior con lo posterior, como el `WaveTrack::Clear` de Audacity: muestras ajustadas a la más cercana y empalme en seco. Lo borrado se dibuja gris y rayado; un clic sobre él lo selecciona y *Restaurar* lo devuelve.
* **`TimeRegion`, `DeletedRegions`, `AudioEdits` y `PcmEditor`** (`EchoCut.Core/Audio`): Tramo de tiempo, conjunto normalizado e inmutable de fragmentos borrados con su traducción a tramas y al cursor, agregado de fundidos y borrados, y editor de PCM por bloques que comparten la copia y la escucha.
* **Botón «Guardar»**: Aplica los ajustes a la fila y escribe la copia en `Recortados/` sin cerrar el editor, por la misma vía que el recorte individual de la rejilla.
* **Varias ventanas de forma de onda**: El editor deja de ser modal; hay una ventana por pista, con su nombre en el título y en la barra de tareas, y volver a abrirla la trae al frente. Al cerrarla con cambios sin aplicar pregunta qué hacer; se cierra sola si su fila desaparece.
* **Escucha de la selección** (hasta 30 s) con las ediciones aplicadas, y reproducción exclusiva en toda la aplicación: empezar a escuchar en una ventana detiene lo que sonara en otra.

#### Fundidos de Aparición y Desaparición
* **`Fade`, `TrackFades` y `FadeShape`** (`EchoCut.Core/Audio`): Modelo inmutable del fundido sobre la selección del usuario (tiempo absoluto del original) y curvas portadas de los preajustes de «Adjustable fade» de Audacity: lineal, curva S, coseno, redondeada, logarítmica (−3 dB a mitad) y exponencial (desde −60 dB).
* **`FadeEnvelope`**: Aplicación muestra a muestra con la indexación del `FadeEffectBase` de Audacity (`n/N` al aparecer, `(N−1−n)/N` al desaparecer), misma ganancia en todos los canales y bloques fuera del fundido sin recorrer.
* **`AudioRenderer`**: Copia editada mediante dos procesos de FFmpeg unidos por tubería (decodificación `f32le` exacta a la muestra → fundidos y borrados → codificación), sin materializar el audio y borrando la salida a medias si falla o se cancela.
* **`AudioStreamInfo` y `AudioEncoding`**: Sondeo del formato con ffprobe y recodificación al mismo códec, tasa de bits y resolución del original (MP3, AAC, Vorbis, Opus, WMA, FLAC, ALAC, WavPack y PCM).
* **`TrackEditor.CopyTags`**: Copia de etiquetas y carátula del original a la copia recodificada con TagLibSharp.
* **Editor de forma de onda**: Selección del tramo de aparición y desaparición arrastrando sobre el detalle de cada borde (con imán a las marcas de recorte y bordes ajustables), casilla, campos numéricos y curva por fundido, envolvente ámbar en las tres vistas y escucha con el fundido aplicado.

### Cambiado
* **`AudioPreviewPlayer`**: Decodifica en flotante para pasar por `PcmEditor` y sitúa el cursor saltando lo borrado.
* **`TrimService`**: Las pistas sin fundido que llegue a la copia siguen recortándose con `-c copy`; solo las que lo llevan se recodifican.
* **`Song`**: Los fundidos (`Fades`) duran la sesión, hacen recortable la pista aunque no se elimine silencio y la marcan como «Ajustado».
* **`FFmpegRunner.Start`**: Puede redirigir la entrada estándar.

## [1.2.0] - 2026-10-02

### Añadido
* **`TrimRange`**: Valor inmutable con el tramo (inicio y final, en tiempo absoluto del original) que conserva la copia recortada. Base para el recorte del silencio inicial y el ajuste manual desde la forma de onda.
* **Detección y recorte del silencio inicial**: `SilenceDetector.AnalyzeLeadingFrames` analiza el principio invirtiendo en el tiempo su curva de nivel, reutilizando íntegro el algoritmo del final (umbral adaptativo, histéresis, guarda de fundido de entrada y afinación del corte).
* **Sondeo del principio en `SilenceAnalyzer`**: Ventana inicial de 10 s que se cuadruplica si resulta toda silencio; si la ventana del final ya cubrió el archivo, el principio se analiza sobre las mismas tramas sin decodificar de nuevo.
* **`AudioDecoder.DecodeRangeIntoAsync`**: Decodificación de un tramo arbitrario, compartiendo con la de la cola el lanzamiento de FFmpeg y la lectura del PCM.
* **Categoría `7 · Inicio`** en el diálogo Avanzado: `Recortar silencio inicial` (activado por defecto) y `Silencio inicial mínimo (s)` (1.0 s).
* **`EdgeTrim`**: Medida y decisión por borde; `TrackAnalysis` deriva de ambos bordes el tramo conservado, el ahorro total y si procede recortar.
* **Rejilla y CSV**: Columna «Silencio inicial (s)» y, en el CSV, el inicio de la copia y las métricas de fundido y fondo de cada borde.

#### Forma de Onda y Ajuste Manual del Recorte
* **`WaveformBuilder` y `Waveform`** (`EchoCut.Core/Waveforms`): Resumen de la señal por bloques de 256 muestras (mínimo, máximo y energía) calculado en streaming sobre `ISampleSink`, con memoria de `ArrayPool`; sirve cualquier nivel de zoom sin volver a decodificar.
* **`WaveformRenderer`, `AmplitudeScale` y `WaveformPalette`**: Pintado de picos y banda RMS en píxeles ARGB en memoria, en escala lineal o en dB (rango de 60 dB), con los azules de Audacity y una paleta gris para la zona que se eliminaría.
* **`WaveformService`**: Carga de la pista completa a 44.1 kHz para la vista general y el detalle del principio, y del final con `-sseof` situado con la misma referencia que el corte final.
* **`WaveformEditor`**: Ventana con la pista completa y el detalle de su principio y su final, marcas y campos numéricos sincronizados, casilla de escala en dB, escucha de cada borde y restablecimiento a la decisión del análisis. Se abre con «Ver forma de onda y ajustar recorte…» en el menú contextual de la rejilla.
* **`WaveformView`**: Control con la zona a eliminar resaltada, regla de tiempo y referencias de amplitud; marcas arrastrables o movibles con el teclado y valor expuesto a lectores de pantalla.
* **Ajuste manual en `Song`**: `ManualRange` prevalece sobre el análisis en el recorte, la previsualización y la columna de recorte, sobrevive a un nuevo análisis y a los cambios de tolerancia durante la sesión, y marca la fila con el nuevo estado «Ajustado».
* **`AudioDecoder`**: Frecuencia de salida configurable por instancia y `DecodeAllIntoAsync` para decodificar el archivo completo sin acotarlo por la duración de los metadatos.
* **Cursor de reproducción**: Al escuchar un borde en `WaveformEditor`, un cursor rojo recorre las vistas con el instante que informa el dispositivo y los botones de escucha pasan a reproducir o detener.
* **`FFmpegRunner.ReadOutputAsync`**: Lectura a memoria de salidas binarias acotadas de FFmpeg, con la misma gestión de cancelación y errores que el resto del ejecutor.

#### Gestión de Metadatos en Lote
* **Limpiar metadatos**: Botón **«Limpiar metadatos»** y método `TrackEditor.StripMetadata` que eliminan todas las etiquetas de las pistas cargadas mediante `TagLibSharp`, en paralelo, previa confirmación y con un resumen de los archivos que no se pudieron procesar.
* **Normalizar**: Botón **«Normalizar»**, método `TrackEditor.NormalizeTrack` y utilidad `TextNormalizer`, que eliminan los acentos diacríticos (conservando la «ñ») y aplican mayúscula inicial a cada palabra en el nombre del archivo y en los metadatos de texto (título, subtítulo, artistas, álbum, género, compositores, copyright y comentarios), previa confirmación.

### Corregido
* **Columna «Silencio inicial (s)»**: Se repinta al asignar el análisis; antes podía quedarse vacía hasta el siguiente repintado completo de la fila.
* **Icono de las ventanas**: Las ventanas de propiedades y de forma de onda muestran el icono de la aplicación, como la principal y la de parámetros avanzados.

### Modificado
* **Previsualización con NAudio**: `AudioPreviewPlayer` reproduce PCM estéreo decodificado en memoria con `WaveOut` de NAudio.WinMM en lugar de `SoundPlayer` y un WAV temporal; expone la posición real de reproducción y avisa al terminar, de modo que la rejilla ya no estima el final con un temporizador. NAudio solo se referencia desde el proyecto de interfaz.
* **`AudioTrimmer` y `TrimRequest`**: El recorte recibe un `TrimRange` en lugar de un único instante de corte. Si el inicio es mayor que cero se añade `-ss` como opción de entrada (antes de `-i`), manteniendo la copia de flujo sin recodificar; si es cero, la línea de órdenes es idéntica a la anterior.
* **`SilenceResult`**: Pasa a ser un `record` independiente del borde; `TrailingSilenceSeconds` se renombra a `SilenceSeconds`.
* **Recálculo por tolerancia**: La regla que rehace la decisión al mover la tolerancia pasa de `Main` a `TrackAnalysis.WithOptions`, y también se aplica al aceptar el diálogo Avanzado.
* **CSV**: «Silencio (s)», «Fundido» y «Toca fondo» pasan a «Silencio final (s)», «Fundido final» y «Final toca fondo».

---

## [1.1.0] - 2026-08-27

### Añadido

#### Gestión y Edición de Metadatos (`EchoCut.Core` y `EchoCut`)
* **Modelo `TrackProperties`**: Objeto de transferencia de datos (DTO) desacoplado para representar metadatos editables de audio (título, subtítulo, artistas, álbum, año, pista, género, compositores, copyright, comentarios) y propiedades de solo lectura del archivo (tamaño en bytes, atributos y marcas temporales del sistema de archivos).
* **Servicio `TrackEditor`**: Capa de persistencia en `EchoCut.Core` para actualización no destructiva de metadatos mediante `TagLibSharp` y renombrado de archivos (`File.Move`) con validación de nombres seguros para el sistema operativo.
* **Ventana Modal `SongPropertiesDialog`**: Diálogo accesible que replica fielmente el aspecto y distribución de la ventana de propiedades de audio de Windows:
  * Pestaña *General*: Icono de audio nativo del sistema, cuadro de texto editable para el nombre del archivo, tipo de archivo, aplicación asociada, ubicación en disco, tamaño en MB y bytes exactos, atributos y marcas temporales de creación, modificación y último acceso.
  * Pestaña *Detalles*: Rejilla categorizada (`SongDetailsViewModel`) para consultar y editar metadatos organizados en Descripción, Multimedia, Audio, Origen y Contenido; visualización de características acústicas técnicas (duración, tasa de bits, canales y frecuencia de muestreo) y botón de acción para remover metadatos personales.
  * Evento `TrackUpdated` para sincronización en tiempo real con la ventana principal y el diccionario de indexación al pulsar «Aplicar», manteniendo la coherencia de datos ante cierres por la «X» o «Cancelar».

#### Interacción con Cuadrícula, Eliminación y Multiselección
* **Eliminación Física Permanente**: Capacidad de eliminar archivos del disco (`File.Delete`) y desindexarlos en memoria desde la cuadrícula (`dataGrid`) mediante la tecla **Suprimir** o la opción de menú contextual **«Eliminar archivo(s)»**, con cuadro de confirmación modal preventivo y detención automática de previsualizaciones activas para liberar descriptores de archivo.
* **Carga de Archivos Sueltos**: Botón **`Archivo(s)...`** y método `TrackScanner.ScanFiles` para importar pistas individuales o selecciones múltiples sin necesidad de escanear carpetas enteras.
* **Escaneo y Destino Multidirectorio**: Soporte en `TrackScanner` y `TrimService` para escanear lotes de directorios y generar subcarpetas `Recortados/` independientes en la ubicación de cada archivo de origen.
* **Multiselección e Integración con Audacity**: Menú contextual para abrir todas las pistas seleccionadas de forma simultánea en una única sesión de Audacity, y evento de doble clic en celda (`dataGrid_CellDoubleClick`) para apertura directa de la pista.

#### Parámetros Avanzados y Previsualización
* **Tiempo de Previsualización Configurable**: Nueva categoría `6 · Previsualización` y propiedad `PreviewSeconds` en `SilenceOptions` (configurable en `AdvancedOptions` entre 0.5 y 30.0 segundos) para definir la cantidad exacta de música previa al corte a reproducir.
* **`AudioPreview.WindowFor`**: Admite duración contextual configurable antes del punto de corte, adaptando la decodificación en `AudioPreviewPlayer` y la duración del temporizador de reproducción.

### Modificado
* **`CONTRIBUTING.md`**: Actualizado con directrices completas del flujo de trabajo Git Flow (`main`, `develop`, `feature/*`), convención de commits atómicos y la recomendación obligatoria de usar *Create a merge commit* para preservar el árbol visual de ramas.

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
