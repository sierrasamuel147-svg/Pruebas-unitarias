# Code coverage en TestingPlayground

Esta guía explica cómo generar y visualizar la cobertura de código del proyecto de dos formas complementarias:

1. **Reporte HTML reproducible** con Coverlet + ReportGenerator (`scripts/coverage.ps1`).
2. **Cobertura gráfica dentro de VS Code** con la cobertura nativa de C# Dev Kit.

El análisis de los resultados está en [testing-analysis.md](testing-analysis.md).

---

## 1. Requisitos

| Elemento | Versión usada | Notas |
|---|---|---|
| .NET SDK | 6.0.428 | No se requiere ni se instala otro SDK. |
| coverlet.collector | 6.0.4 | Ya referenciado en `tests/TestingPlayground.Tests`. |
| ReportGenerator | 5.3.11 | Herramienta **local** declarada en `.config/dotnet-tools.json`; compatible con el runtime de .NET 6. |
| PowerShell | Windows PowerShell 5.1 o PowerShell 7 | Para ejecutar el script. |

ReportGenerator no se instala globalmente. El script ejecuta `dotnet tool restore`, que descarga exactamente la versión fijada en el manifest. Por eso cualquier equipo con el SDK de .NET 6 obtiene la misma herramienta.

---

## 2. Reporte HTML con un solo comando

Desde la raíz del repositorio:

```powershell
.\scripts\coverage.ps1
```

El script:

1. Elimina `TestResults/` y `CoverageReport/` si existen, para no mezclar ejecuciones viejas.
2. Restaura las herramientas locales (`dotnet tool restore`).
3. Ejecuta toda la suite con el colector de Coverlet:
   `dotnet test TestingPlayground.sln --collect:"XPlat Code Coverage" --results-directory TestResults`
4. Busca automáticamente cada `coverage.cobertura.xml` bajo `TestResults/`. Ese archivo queda en una subcarpeta con GUID, por eso no se usan rutas fijas.
5. Genera con ReportGenerator los formatos `Html`, `HtmlSummary`, `Cobertura` y `TextSummary` en `CoverageReport/`.
6. Si cualquier paso falla (por ejemplo, una prueba en rojo), se detiene con código de salida distinto de 0 e indica qué paso falló.

Salida final esperada:

```text
Tests: PASS
Coverage XML: generated (1 file(s))
HTML report: generated

Open:
  CoverageReport/index.html
```

### Archivos generados

| Ruta | Contenido |
|---|---|
| `TestResults/<guid>/coverage.cobertura.xml` | Datos crudos de Coverlet: líneas y ramas por archivo. |
| `CoverageReport/index.html` | Reporte navegable: resumen, clases y código fuente coloreado. |
| `CoverageReport/summary.html` | Resumen en una sola página. |
| `CoverageReport/Summary.txt` | Resumen en texto plano, útil en consola o en CI. |
| `CoverageReport/Cobertura.xml` | Cobertura consolidada, útil si luego hay varios proyectos de prueba. |

Ninguno de estos archivos se versiona: `.gitignore` excluye `TestResults/` y `CoverageReport/`. En cambio, `.config/dotnet-tools.json`, `scripts/coverage.ps1` y `docs/` sí se versionan.

### Si PowerShell bloquea el script

Con la política `RemoteSigned`, los scripts locales se ejecutan sin problema. Si la política de la máquina es más restrictiva:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\coverage.ps1
```

---

## 3. Cobertura gráfica en VS Code con C# Dev Kit

C# Dev Kit integra la cobertura en la vista **Testing** de VS Code, así que no hace falta ninguna extensión adicional como Coverage Gutters.

> Requiere una versión **actualizada** de VS Code y de C# Dev Kit. La API de cobertura de VS Code y el comando *Run Tests with Coverage* de C# Dev Kit son relativamente recientes. Si no los ves, actualiza ambas extensiones y recarga la ventana.

### Pasos

1. **Abrir Testing** en la Activity Bar (el icono del matraz).
2. **Refrescar las pruebas** con el botón *Refresh Tests* si el árbol está vacío o desactualizado. Deben aparecer los 127 métodos de prueba; los `[Theory]` se despliegan en sus casos.
3. **Ejecutar *Run Tests with Coverage*.** Es el botón con el icono de escudo/play en la parte superior de la vista Testing, o el comando `Test: Run All Tests with Coverage`. También puedes hacer clic derecho sobre una clase o un test concreto y elegir *Run Test with Coverage*.
4. **Abrir la Command Palette** (`Ctrl+Shift+P`).
5. **Ejecutar `Test: Show Coverage`.** Según la versión de VS Code, el comando puede llamarse `Test: Open Coverage`. Para ver las marcas dentro del editor también existe `Test: Toggle Inline Coverage`.
6. **Navegar por el árbol de cobertura.** En la vista Testing aparece la sección **Test Coverage**, con el porcentaje por carpeta, archivo y método.
7. **Abrir archivos de `src/`**, por ejemplo `src/TestingPlayground/Advanced/Checkout/CheckoutService.cs`.
8. **Interpretar las marcas del margen:**
   - **verde** = la línea fue ejecutada por al menos una prueba;
   - **rojo** = la línea no fue ejecutada por ninguna prueba.

   En `CheckoutService.cs` deberían verse en rojo las líneas del `if (item is null)` (líneas 59–61), que son lo único sin cubrir del proyecto.

> C# Dev Kit calcula la cobertura con su propio motor (Microsoft Code Coverage), no con Coverlet. Los porcentajes pueden diferir unas décimas del reporte HTML porque cada herramienta cuenta las líneas y ramas a su manera. Para cifras oficiales y reproducibles, usa `scripts/coverage.ps1`. La vista de VS Code sirve para explorar el código mientras programas.

---

## 4. Test Explorer frente a Test Coverage

Son dos preguntas distintas.

| | **Test Explorer** | **Test Coverage** |
|---|---|---|
| Pregunta principal | ¿Qué pruebas existen? ¿Cuáles pasan? ¿Cuáles fallan? ¿Cuánto tardan? | ¿Qué partes del código de producción fueron **ejecutadas** por esas pruebas? |
| Unidad | Prueba (método o caso de un `[Theory]`). | Línea, rama, método del código de `src/`. |
| Resultado típico | `356/356 passing` | `99.5 % lines, 99.1 % branches` |
| No responde | Qué código quedó sin ejecutar. | Si las aserciones son buenas o si el comportamiento es correcto. |

**`356/356 tests passing` no significa "100 % del código correctamente probado".** Significa que ninguna aserción falló. Una prueba sin aserciones también "pasa", y una línea puede ejecutarse sin que ninguna aserción compruebe su efecto. La cobertura indica qué código se ejecutó, no qué se verificó. Para la discusión completa, con evidencia de este proyecto, ver [testing-analysis.md](testing-analysis.md#high-coverage--high-test-quality).
