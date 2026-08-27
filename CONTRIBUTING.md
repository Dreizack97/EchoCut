# Guía de Contribución para EchoCut

¡Muchas gracias por tu interés en contribuir a **EchoCut**! 🎉

EchoCut es un proyecto de código abierto desarrollado con los más altos estándares de ingeniería de software en **.NET 10** y **C# 14**, enfocado en la precisión acústica, rendimiento multihilo y accesibilidad universal.

Tanto si deseas reportar un error, proponer una mejora en el algoritmo DSP, pulir la interfaz gráfica o mejorar la documentación, esta guía te orientará en todo el proceso.

---

## 🧭 Principios de Arquitectura y Código Limpio

Para mantener la calidad y consistencia del proyecto, todo cambio debe alinearse con nuestros pilares de diseño:

### 1. Pureza e Independencia de `EchoCut.Core`
* El proyecto `EchoCut.Core` tiene como objetivo `net10.0` deliberadamente (sin sufijo `-windows`).
* **Regla de oro**: El compilador prohíbe terminantemente agregar referencias a WinForms, `System.Drawing`, `System.Windows.Forms` o `System.Media` en `EchoCut.Core`.
* El motor DSP debe permanecer desacoplado, puro y determinista, capaz de probarse con señales sintéticas en memoria sin levantar ventanas ni procesos gráficos.

### 2. Eficiencia Extrema en Memoria (Ruta Crítica DSP)
* **Cero Alocaciones en el LOH**: El audio decodificado nunca debe materializarse completamente en memoria. Utiliza siempre streaming sobre `ISampleSink` y renta de búferes con `ArrayPool<byte>.Shared` y `ArrayPool<double>.Shared`.
* Emplea `Span<T>` y `ReadOnlySpan<T>` para transformaciones y cálculos locales en la curva de niveles de trama.
* Respeta la recolección y devolución obligatoria de búferes en bloques `finally`.

### 3. Inmutabilidad y Seguridad de Hilos (*Thread-Safety*)
* El procesamiento por lote corre concurrentemente mediante `Parallel.ForEachAsync`.
* Los objetos que viajan entre los servicios de lote y los canales de progreso (`TrackInfo`, `TrackAnalysis`, `PreviewWindow`, `TrimRequest`, `TrimOutcome`) deben ser registros inmutables (`record` o `readonly record struct`).

### 4. Modismos Modernos de C# 14 y .NET 10
* **Espacios de nombres con ámbito de archivo** (*file-scoped namespaces*): `namespace EchoCut.Audio;`.
* **Constructores primarios** (*primary constructors*) en clases y registros siempre que simplifiquen la inicialización.
* **Expresiones de colección**: usa `[...]` en lugar de `new List<T>()` o `new T[] { ... }`.
* **Pattern Matching** moderno para legibilidad (`is not null`, `switch expressions`, guardas condicionales).

### 5. Documentación Rigurosa en Código (Comentarios XML)
* En EchoCut, los comentarios XML no repiten lo obvio; explican el **porqué técnico** y el fundamento acústico/físico de las decisiones.
* Toda nueva clase, método público o propiedad expuesta debe incluir comentarios XML completos en español mexicano claro y preciso.

---

## 🛠️ Configuración del Entorno de Desarrollo

### Requisitos
1. **.NET 10 SDK** (versión 10.0.x o superior).
2. **Visual Studio 2026 / Visual Studio 2022 v17.12+** (con la carga de trabajo de escritorio .NET) o **Visual Studio Code / JetBrains Rider** con soporte de C# Dev Kit.
3. **FFmpeg y FFprobe** instalados y accesibles en el `PATH` del sistema.
4. **Git**.

### Comandos de Compilación

```powershell
# Clonar tu bifurcación (fork)
git clone https://github.com/tu-usuario/EchoCut.git
cd EchoCut

# Restaurar paquetes NuGet
dotnet restore EchoCut.slnx

# Compilar la solución completa
dotnet build EchoCut.slnx

# Ejecutar el proyecto
dotnet run --project EchoCut/EchoCut.csproj
```

---

## 🌿 Flujo de Trabajo con Git

1. **Haz un Fork del repositorio**: Trabaja siempre sobre tu propio fork antes de enviar un Pull Request.
2. **Crea una rama descriptiva**:
   ```bash
   git checkout -b feature/nueva-mejora-dsp
   # o
   git checkout -b fix/correccion-frecuencia-corte
   ```
3. **Convención de Mensajes de Commit**:
   Utilizamos [Conventional Commits](https://www.conventionalcommits.org/es/v1.0.0/) en español:
   * `feat: agregar exportación en formato JSON para diagnósticos`
   * `fix: resolver cálculo de umbral en pistas con muestreo a 96 kHz`
   * `perf: optimizar recorrido de tramas en LevelFramer`
   * `docs: actualizar tabla de parámetros en README`
   * `refactor: desacoplar lógica de desempate en SortableBindingList`
4. **Mantén tu rama actualizada**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

---

## 📋 Lista de Verificación (Checklist) para Pull Requests

Antes de solicitar la revisión de tu Pull Request, asegúrate de cumplir con los siguientes puntos:

- [ ] **Compilación limpia**: `dotnet build EchoCut.slnx` compila con **0 errores y 0 advertencias**.
- [ ] **Aislamiento de arquitectura**: Ninguna referencia de WinForms o escritorio fue introducida en `EchoCut.Core`.
- [ ] **Gestión de memoria**: Todo búfer rentado de `ArrayPool` se retorna en un bloque `finally`.
- [ ] **Documentación XML**: Todos los tipos y miembros públicos nuevos cuentan con documentación XML explicativa en español.
- [ ] **Pruebas manuales realizadas**: Has probado el cambio con archivos de audio reales (MP3, FLAC, WAV) y verificado que el recorte sin pérdida (`-c copy`) se ejecuta correctamente y que los archivos originales permanecen intactos.
- [ ] **Compatibilidad de configuración**: Si modificaste `SilenceOptions`, verificaste que `Normalize()` y `Validate()` acotan adecuadamente los nuevos valores para evitar excepciones con valores corruptos de `%APPDATA%\EchoCut\settings.json`.

---

## 🐛 Reportar Errores y Sugerir Mejoras

* **Reporte de fallos (*Bug Reports*)**: Por favor, incluye tu versión de Windows, la versión de FFmpeg (`ffmpeg -version`), el formato de audio que causó el problema y el mensaje de error capturado en el diálogo o en el archivo CSV exportado.
* **Solicitud de características (*Feature Requests*)**: Explica detalladamente el caso de uso, el beneficio para el flujo de trabajo de bibliotecas de audio y cómo interactuaría con la arquitectura no destructiva existente.

---

¡Gracias por ayudar a que EchoCut sea la herramienta de recorte de audio más sólida y confiable del ecosistema .NET!
