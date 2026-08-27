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
git clone https://github.com/Dreizack97/EchoCut.git
cd EchoCut

# Restaurar paquetes NuGet
dotnet restore EchoCut.slnx

# Compilar la solución completa
dotnet build EchoCut.slnx

# Ejecutar el proyecto
dotnet run --project EchoCut/EchoCut.csproj
```

---

## 🌿 Flujo de Trabajo con Git (Git Flow)

El flujo estándar y más confiable adoptado en EchoCut es **Git Flow** (o flujo basado en ramas funcionales). En este esquema:
* `main`: Representa exclusivamente código estable y probado en producción. Cada despliegue se etiqueta con una versión semántica formal.
* `develop`: Centraliza la integración continua del desarrollo diario y sirve como base común para nuevas funcionalidades.
* `feature/*`: Ramas de trabajo aisladas para cada tarea, ajuste o nueva funcionalidad. Se originan siempre desde `develop` y se reintegran mediante *Pull Request* (PR).

---

### 1. Crear y Publicar la Rama `develop`
> [!NOTE]
> Configuración inicial por única vez.

Crea la rama `develop` directamente a partir del último commit funcional de `main` y súbela al repositorio remoto:

```bash
git checkout main
git pull origin main
git checkout -b develop
git push -u origin develop
```

---

### 2. Crear una Rama `feature/*` para Cada Tarea
> [!IMPORTANT]
> Toda nueva funcionalidad debe partir **siempre desde `develop`**, manteniendo `main` intacto.

Cada vez que comiences un cambio o nueva funcionalidad, actualiza `develop` y desprende tu rama de trabajo con un nombre descriptivo:

```bash
git checkout develop
git pull origin develop
git checkout -b feature/nombre-de-la-funcionalidad
```
*(Ejemplos: `feature/explorador-canciones`, `feature/filtro-dsp-rlb`, `fix/deteccion-hiss`)*

---

### 3. Confirmación de Cambios (Conventional Commits)
Confirma los cambios mediante commits atómicos enfocados en una única intención, con mensajes en español y siguiendo [Conventional Commits](https://www.conventionalcommits.org/es/v1.0.0/):
* `feat(core): incorporar modelo y editor de metadatos para pistas de audio`
* `feat(ui): implementar ventana modal de propiedades de canción y metadatos`
* `fix(audio): corregir cálculo de umbral adaptativo en pistas con hiss`
* `perf(processing): optimizar reutilización de búferes con ArrayPool`
* `docs(contributing): actualizar directrices de Git Flow y ramas funcionales`

---

### 4. Integrar la `feature` de Vuelta a `develop`
Tras realizar tus commits, asegurar una compilación limpia (`dotnet build EchoCut.slnx`) y validar pruebas localmente:

1. Sube la rama y abre un **Pull Request (PR)** hacia `develop`:
   ```bash
   git push -u origin feature/nombre-de-la-funcionalidad
   ```
2. **Estrategia de fusión (*Merge Strategy*)**:
   > [!IMPORTANT]
   > Al aceptar los Pull Requests, selecciona la opción **"Create a merge commit"** en lugar de *Squash and merge* o *Rebase and merge*, ya que estas dos últimas aplanan el historial linealmente y eliminan la forma gráfica de ramas separadas, dificultando la trazabilidad del árbol de desarrollo.
3. Una vez revisado, aprobado y fusionado el PR, elimina la rama `feature` local y remota para mantener limpio el repositorio:
   ```bash
   git checkout develop
   git pull origin develop
   git branch -d feature/nombre-de-la-funcionalidad
   git push origin --delete feature/nombre-de-la-funcionalidad
   ```

---

### 5. Pasar Código Estable de `develop` a `main` (Lanzamientos a Producción)
> [!TIP]
> Solo versiones listas y validadas para producción.

Cuando `develop` acumule suficientes cambios probados y estables para un despliegue oficial, abre un PR de `develop` hacia `main` o realiza la integración con etiqueta de versión:

```bash
git checkout main
git pull origin main
git merge --no-ff develop
git tag -a v1.0.0 -m "Versión estable 1.0.0"
git push origin main --tags
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
