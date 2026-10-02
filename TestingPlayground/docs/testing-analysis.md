# Análisis de testing y cobertura — TestingPlayground

Documento de apoyo para la sustentación técnica del proyecto. Resume qué se probó, con qué herramientas, con qué técnicas, qué cobertura se obtuvo y qué significa esa cobertura.

Cómo generar la cobertura y verla en VS Code: [code-coverage.md](code-coverage.md).

---

## 1. Estado del proyecto

| Elemento | Valor |
|---|---|
| SDK / target | .NET SDK 6.0.428 / `net6.0` |
| Compilación | 0 errores, 0 advertencias |
| Proyecto de pruebas | `tests/TestingPlayground.Tests` |
| Herramientas | xUnit 2.9.3, FluentAssertions 6.12.2, Moq 4.20.72, coverlet.collector 6.0.4, ReportGenerator 5.3.11 (local) |
| Métodos de prueba | 127 (`[Fact]` + `[Theory]`) |
| Casos ejecutados | **356**: cada `InlineData` de un `[Theory]` cuenta como un caso |
| Resultado | 356 passing, 0 failed, 0 skipped |

---

## 2. Code Coverage Metrics

### Line Coverage

Porcentaje de **líneas ejecutables** del código de producción que se ejecutaron al menos una vez durante las pruebas. Las líneas en blanco, los comentarios y las llaves sueltas no cuentan.

```csharp
public bool IsAdult(int age)
{
    if (age >= 18)
    {
        return true;
    }

    return false;
}
```

Con una sola prueba `IsAdult(20)` se ejecutan el `if` y el `return true`, pero no el `return false`: la cobertura de líneas es parcial. Al añadir `IsAdult(10)` llega al 100 %. Aun así, nadie comprobó la frontera `age = 18` ni `age = 17`: si el código dijera `age > 18`, ambas pruebas seguirían pasando. **Ejecutar una línea no es lo mismo que verificar su decisión.**

### Branch Coverage

Mide los **caminos de las decisiones**: cada `if`/`else`, cada `case` de un `switch`, cada operando de `&&`/`||`, cada `?:` y `??`. Una decisión binaria tiene 2 ramas, y ambas deben recorrerse.

```csharp
public decimal Discount(bool isMember) => isMember ? 0.10m : 0m;
```

Este método ocupa **una sola línea**. Con `Discount(true)` la cobertura de líneas es 100 %, pero solo se recorrió una de las dos ramas: **cobertura de ramas 50 %**. Por eso:

```text
Line Coverage = 100 %   puede coexistir con   Branch Coverage < 100 %
```

En este proyecto pasa algo parecido: la línea 59 de `CheckoutService` (`if (item is null)`) se ejecutó 20 veces, pero siempre con la condición en `false`. La línea cuenta como ejecutada y la rama `true` no (ver sección 4).

### Method Coverage

Porcentaje de métodos invocados al menos una vez. Es la métrica más gruesa: un método con 1 de sus 10 líneas ejecutadas ya cuenta como cubierto.

---

## 3. Cobertura real obtenida

Cifras extraídas del `coverage.cobertura.xml` generado por `scripts/coverage.ps1` (Coverlet 6.0.4, 2026-10-02) y confirmadas con el `Summary.txt` de ReportGenerator.

### Totales

| Métrica | Valor |
|---|---:|
| **Line coverage** | **99.52 %** (`line-rate` = 0.9952) |
| Lines covered | 415 |
| Lines valid (coverable) | 417 |
| **Branch coverage** | **99.09 %** (`branch-rate` = 0.9909) |
| Branches covered | 109 |
| Branches valid | 110 |
| Method coverage (ReportGenerator) | 100 % (70 de 70) |

> ReportGenerator muestra la cobertura de ramas redondeada como "99 %". El valor exacto es 109/110 = 99.09 %.

### Por clase

Coverlet reporta por separado la máquina de estados que el compilador genera para cada método `async` (por ejemplo, `CheckoutService/<CheckoutAsync>d__5`). En esta tabla esas filas se suman a su clase.

| Clase | Líneas | Ramas | Observación |
|---|---:|---:|---|
| TextAnalyzer | 81/81 (100 %) | 22/22 (100 %) | Incluye la pila de `HasBalancedBrackets` y todas las ramas de validación de `null`. |
| DateTimeService | 65/65 (100 %) | 26/26 (100 %) | La clase con más decisiones: fines de semana, rangos semiabiertos, 29 de febrero. |
| CollectionService | 61/61 (100 %) | 8/8 (100 %) | Pocas ramas: casi toda la lógica está delegada a LINQ. |
| RegexValidator | 48/48 (100 %) | 18/18 (100 %) | Cubre la separación entre formato de tarjeta y Luhn. |
| WildcardMatcher | 17/17 (100 %) | 2/2 (100 %) | La única decisión propia es `ignoreCase`. |
| AppointmentService | 24/24 (100 %) | 8/8 (100 %) | Las 4 decisiones (cliente, inicio, fin, solapamiento) en ambos sentidos. |
| **CheckoutService** | **52/54 (96.3 %)** | **15/16 (93.8 %)** | Única clase incompleta: el ítem `null` del carrito (sección 4). |
| UserRegistrationService | 42/42 (100 %) | 10/10 (100 %) | |
| Records (Appointment, CartItem, Order, User) | 25/25 (100 %) | 0/0 | Solo propiedades generadas por el compilador. |
| **Total** | **415/417** | **109/110** | |

Para validar la tabla: la suma de las líneas por clase (81+65+61+48+17+24+54+42+25 = 417) y de las ramas (22+26+8+18+2+8+16+10 = 110) coincide con los totales del XML.

---

## 4. Código no cubierto

El reporte muestra **un único punto sin cubrir**:

| Archivo | Método | Líneas / rama | Estado |
|---|---|---|---|
| `src/TestingPlayground/Advanced/Checkout/CheckoutService.cs` | `ValidateCart` (privado, llamado desde `CheckoutAsync`) | Línea 59 `if (item is null)`: rama `true` sin recorrer (1 de 2). Líneas 60–61 (el `throw`): 0 ejecuciones. | 2 líneas y 1 rama |

```csharp
foreach (var item in items)
{
    if (item is null)                       // línea 59: ejecutada 20 veces, siempre false
    {
        throw new ArgumentException(        // líneas 60-61: nunca ejecutadas
            "The cart cannot contain null items.", nameof(items));
    }
    ...
}
```

**Por qué no está cubierto.** Ninguna prueba envía un carrito con un elemento `null`. Las pruebas cubren el carrito `null` y el carrito vacío, pero no un carrito con un ítem `null` en su interior.

**Clasificación: B — código defensivo razonable** (con un componente de **D**).

- *Por qué D en parte:* el parámetro es `IReadOnlyList<CartItem>`, con elementos no anulables. Bajo el contrato de nullable reference types, un llamador que respete las anotaciones nunca envía `null`, y el compilador le advertiría si lo intentara.
- *Por qué no es código muerto (C):* las anotaciones de nulabilidad no se comprueban en ejecución. Un llamador puede saltárselas con `null!`, con código sin `#nullable enable` o con datos deserializados. Sin esta guarda, el siguiente acceso (`item.Quantity`) lanzaría un `NullReferenceException` sin contexto. La guarda lo convierte en un `ArgumentException` con un mensaje claro y garantiza que no se toque el inventario, el pago ni el repositorio.

**¿Merece una prueba?** Sí, pero no es prioritaria. Una prueba como `CheckoutAsync_CartContainsNullItem_ThrowsArgumentException` documentaría un contrato observable real (excepción clara y ninguna llamada externa), no solo subiría una cifra. No se añadió en esta fase porque la regla de la Fase 3 es no escribir pruebas para mover el porcentaje. Queda registrada como **mejora opcional**: si se agrega, debe justificarse por el contrato, no por llegar al 100 %.

No hay otras líneas ni ramas sin cubrir, ni código muerto detectado.

---

## 5. High coverage ≠ high test quality

Un 99.5 % de cobertura demuestra que casi todo el código **se ejecutó** durante las pruebas. No demuestra que algo se **haya verificado**. Por ejemplo:

```csharp
[Fact]
public async Task Checkout_Runs()
{
    await service.CheckoutAsync(customerId, items);   // sin aserciones
}
```

Esta prueba ejecutaría casi todo `CheckoutService` y subiría mucho la cobertura, pero seguiría pasando aunque el total se calculara mal, aunque se cobrara dos veces o aunque nunca se guardara la orden.

La evidencia más fuerte de calidad es comprobar que **la suite falla cuando el código está mal**. Para eso, en la Fase 2 se introdujeron deliberadamente cinco defectos (*mutaciones*) y se ejecutó la suite completa contra cada uno.

**Procedencia de los datos.** Los experimentos se hicieron durante la validación de la Fase 2, sobre una **copia temporal** de la solución fuera del repositorio. El código de `src/` nunca se modificó. Los resultados no quedaron guardados como archivos del proyecto; las cifras provienen de la salida de `dotnet test` registrada en esa sesión. No se repitieron en la Fase 3 para no volver a alterar código de producción sin necesidad.

| # | Mutación | Comportamiento roto | Tests que fallaron | Qué demuestra |
|---|---|---|---:|---|
| 1 | `RegexValidator`: `3\d{9}\z` → `3\d{9}$` en el celular | `"3001234567\n"` vuelve a aceptarse como celular válido | 2 | Los casos con salto de línea final protegen la decisión de anclar con `\z`. Fallaron `IsColombianMobile_InvalidNumbers_ReturnsFalse("3001234567\n")` y `IsColombianMobile_TrailingNewline_IsRejected`. |
| 2 | `UserRegistrationService`: `IsValidEmail(normalizedEmail)` → `IsValidEmail(email)` | Emails con espacios o mayúsculas alrededor se rechazan o se validan sin normalizar | 14 | La normalización está verificada desde muchos ángulos: resultado, consulta de duplicados, correo de bienvenida y variantes del `[Theory]`. |
| 3 | `CheckoutService`: ignorar el resultado de `HasStockAsync` | Se cobra y se guarda la orden aunque no haya stock | 2 | Las verificaciones de interacción (`ChargeAsync → Times.Never`) detectan un flujo que continúa cuando debía detenerse. |
| 4 | `AppointmentService`: `start <= UtcNow` → `start < UtcNow` | Se acepta una cita que empieza exactamente ahora | 1 | Solo una prueba de frontera (`StartIsInPast` con `minutesFromNow = 0`) distingue `<` de `<=`. Sin ella la mutación sobreviviría. |
| 5 | `DateTimeService.CalculateAge`: cumpleaños del 29/02 movido al 28/02 en años no bisiestos | Una persona nacida el 29/02/2000 tendría 1 año el 28/02/2001 | 2 | La regla de dominio del 29 de febrero está fijada por casos específicos y no depende de la casualidad. |

**Conclusión.** Las cinco mutaciones fueron detectadas (5/5). Que la mutación 4 solo la detecte **una** prueba ilustra por qué importan las fronteras: el 100 % de cobertura de `AppointmentService` se habría alcanzado igualmente sin el caso `start == UtcNow`, pero la suite no habría notado el defecto.

> Nota de método: en la mutación 1, un primer intento mal construido produjo `\$` (un dólar literal) en lugar de `$` y rompió 4 pruebas de números válidos. Ese resultado se descartó porque no correspondía a la mutación buscada. Las 2 pruebas reportadas corresponden a la mutación correcta.

---

## 6. Pruebas representativas

Una selección de 20 de los 127 métodos de prueba, elegidos porque cada uno muestra una técnica o herramienta distinta.

| # | Test | Clase probada | Herramienta | Técnica | Qué garantiza |
|---|---|---|---|---|---|
| 1 | `IsPalindrome_Palindromes_ReturnsTrue` | TextAnalyzer | xUnit `[Theory]` + `[InlineData]`, `Assert.True` | Pruebas parametrizadas | Ignora mayúsculas, espacios y puntuación (`"A man, a plan, a canal: Panama!"`). |
| 2 | `HasBalancedBrackets_UnbalancedOrMisorderedText_ReturnsFalse` | TextAnalyzer | xUnit `Assert.False` | Casos negativos | `"([)]"` se rechaza: el orden de cierre importa, no solo la cantidad. |
| 3 | `CalculateAge_BornOnFebruary29_HasBirthdayOnMarch1InNonLeapYears` | DateTimeService | xUnit `[Theory]`, `Assert.Equal` | Fechas, frontera, regla de dominio | Edad 0 el 28/02/2001 y 1 el 01/03/2001. |
| 4 | `AddBusinessDays_Negative_ThrowsArgumentOutOfRangeException` | DateTimeService | xUnit `Assert.Throws` | Excepciones, frontera inferior | `-1` e `int.MinValue` se rechazan y se informa `ParamName = "businessDays"`. |
| 5 | `RangesOverlap_HalfOpenRanges_DetectsOverlap` | DateTimeService | xUnit `[Theory]` | Rangos semiabiertos | `10:00–11:00` y `11:00–12:00` no se solapan; `10:59–11:30` sí. |
| 6 | `RemoveDuplicates_ResultIsOrderedUniqueSubsetOfInput` | CollectionService | FA `Should().Equal`, `OnlyHaveUniqueItems`, `BeSubsetOf` | Colecciones con orden | `3,1,3,2,1` produce exactamente `3,1,2`, en orden de primera aparición. |
| 7 | `SymmetricDifference_HasSetSemanticsAndExcludesCommonElements` | CollectionService | FA `BeEquivalentTo`, `NotIntersectWith` | Colecciones sin orden | El resultado es `{1,2,5,6}` como conjunto, sin elementos comunes. |
| 8 | `GetTop_ReturnsRequestedAmountInDescendingOrder` | CollectionService | FA `HaveCount`, `BeInDescendingOrder`, `ContainInOrder` | Aserciones encadenadas | Top-3 de `5,9,1,7,3` es `9,7,5`. |
| 9 | `Average_IsAlwaysBetweenMinimumAndMaximum` | CollectionService | FA `BeInRange` | Propiedad invariante | Para cualquier entrada, `min ≤ promedio ≤ max`. |
| 10 | `Average_NonTerminatingDecimal_IsApproximatelyCorrect` | CollectionService | FA `BeApproximately` | Comparación de `double` con tolerancia | `[1,1,2]` da ≈1.333 sin depender de la representación binaria exacta. |
| 11 | `IsColombianMobile_InvalidNumbers_ReturnsFalse` | RegexValidator | xUnit `[Theory]` con `null` | Regex, `null`/vacío/espacios, salto de línea final | `"3001234567\n"`, `"+57  3001234567"` y `null` son inválidos. |
| 12 | `IsValidDateFormat_NonExistentDateWithValidShape_ReturnsTrueBecauseOnlyFormatIsChecked` | RegexValidator | xUnit `[Theory]` | Contrato deliberado | `31/02/2026` pasa: el método valida solo el formato. |
| 13 | `PassesLuhn_CharactersOtherThanDigitsSpacesOrHyphens_ReturnsFalse` | RegexValidator | xUnit `[Theory]` | Entradas inválidas | `4111A11111111111` y `4111.1111…` se rechazan; no se eliminan caracteres arbitrarios. |
| 14 | `NormalizeWhitespace_Null_ThrowsReportingParameterName` | TextAnalyzer | FA `Should().Throw<>().WithParameterName` | Prueba de `null` | La excepción indica el parámetro culpable (`text`). |
| 15 | `ScheduleAsync_CustomerIsEmpty_ThrowsArgumentException` | AppointmentService | FA `ThrowAsync`, Moq `VerifyNoOtherCalls` | Async, prueba de `null` | Con cliente vacío no se toca el repositorio en absoluto. |
| 16 | `ScheduleAsync_StartIsInPast_ThrowsArgumentException` | AppointmentService | Moq `Setup().Returns` (reloj), `Verify(..., Times.Never)` | Reloj fijo, frontera `start == UtcNow` | Una cita en el pasado o en el instante actual se rechaza antes de consultar o guardar. |
| 17 | `ScheduleAsync_EndIsBeforeOrEqualToStart_ThrowsArgumentException` | AppointmentService | xUnit `Assert.ThrowsAsync` | Async con xUnit puro | Mismo patrón que FA `ThrowAsync`, para comparar estilos. |
| 18 | `ScheduleAsync_ValidAppointment_CreatesAndSavesAppointment` | AppointmentService | Moq `ReturnsAsync`, `Callback`, `Verify(..., token)`; xUnit `Assert.NotNull` | Captura de argumentos, propagación de `CancellationToken` | El mismo token llega a `HasOverlapAsync` y `AddAsync`, y se guarda la misma instancia que se devuelve. |
| 19 | `CheckoutAsync_ValidCart_ChargesTotalSavesAndNotifies` | CheckoutService | Moq `It.IsAny`, `It.Is<decimal>`, `Times.Once`, `Times.Exactly(2)` | Estado + interacción | Total 125.50, cobro exacto de 125.50, una consulta de stock por producto, guardado y notificación una vez. |
| 20 | `RegisterAsync_ValidData_NormalizesEmailAndCompletesRegistration` | UserRegistrationService | Moq `Returns` (hasher, reloj); FA `BeEquivalentTo` con objeto anónimo | Objetos complejos, reloj fijo | `"  SAMUEL@GMAIL.COM  "` termina como `samuel@gmail.com` en todas partes, con hash `HASH_TEST` y `CreatedAt` = 2026-10-02 15:30 UTC. |

---

## 7. xUnit frente a FluentAssertions

**No son competidores.** Cumplen papeles distintos:

- **xUnit** es el **framework de testing**: descubre las pruebas (`[Fact]`, `[Theory]`), crea una instancia nueva de la clase por cada prueba, las ejecuta, reporta resultados e incluye además una librería de aserciones básica (`Assert.*`).
- **FluentAssertions** es **solo una librería de aserciones**. No descubre ni ejecuta nada; reemplaza o complementa a `Assert.*` dentro de pruebas que sigue ejecutando xUnit.

```csharp
Assert.Equal(expected, actual);       // xUnit: el orden (expected, actual) es fácil de invertir
actual.Should().Be(expected);         // FA: se lee de izquierda a derecha
```

En un `Equal` simple la diferencia es estética. FluentAssertions aporta más en estos casos, todos tomados del proyecto:

**Strings, con aserciones encadenadas** (`NormalizeWhitespace_MixedWhitespace_ProducesCleanSingleSpacedText`):

```csharp
result.Should().StartWith("uno")
    .And.EndWith("tres")
    .And.NotContain("  ")
    .And.Be("uno dos tres");
```

**Colecciones, haciendo explícito si el orden importa:**

```csharp
result.Should().Equal(3, 1, 2);                        // orden exacto (RemoveDuplicates)
result.Should().BeEquivalentTo(new[] { 6, 5, 2, 1 });  // mismo contenido, cualquier orden (SymmetricDifference)
```

En xUnit, `Assert.Equal` sobre colecciones siempre exige el mismo orden. Para ignorarlo habría que ordenar ambos lados a mano.

**Objetos complejos** (`CheckoutAsync_ValidCart_ReturnsOrderWithCartContents`):

```csharp
result.Should().BeEquivalentTo(
    new Order(Guid.Empty, CustomerId, new[] { Keyboard, Mouse }, CartTotal),
    options => options.Excluding(order => order.Id));
```

Compara propiedad por propiedad, incluida la lista de ítems, e ignora el `Id` generado. Con `Assert` se necesitarían cuatro aserciones separadas.

**Excepciones** (`GetTop_ZeroQuantity_ThrowsReportingParameterAndValue`):

```csharp
// xUnit
var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _service.GetTop(new[] { 1, 2 }, 0));
Assert.Equal("quantity", ex.ParamName);

// FluentAssertions
act.Should().Throw<ArgumentOutOfRangeException>()
    .WithParameterName("quantity")
    .Which.ActualValue.Should().Be(0);
```

**Mensajes de fallo.** Con `Assert.True(x)`, un fallo solo dice *"Expected: True, Actual: False"*. Con FA y la cláusula `because`, el mensaje explica la regla (`IsColombianMobile_TrailingNewline_IsRejected`):

```csharp
_validator.IsColombianMobile("3001234567\n")
    .Should().BeFalse("the pattern is anchored with \\z, which does not allow a trailing newline");
```

**Criterio del proyecto.** Las pruebas de lógica pura usan principalmente `Assert` y cada clase tiene al final una sección FluentAssertions con los casos donde FA aporta algo concreto. Las pruebas de servicios avanzados usan FA porque comparan objetos y excepciones `async`.

---

## 8. Why Moq?

**Moq no ejecuta pruebas ni comprueba resultados.** Moq **sustituye dependencias**: crea, en tiempo de ejecución, objetos que implementan una interfaz (`IClock`, `IPaymentGateway`…) cuyo comportamiento controla la prueba y que además registran cómo fueron llamados.

```text
xUnit
  ↓  descubre y ejecuta la prueba
Moq
  ↓  controla las dependencias externas (reloj, repositorio, pasarela de pago, email)
FluentAssertions / Assert
  ↓  comprueba resultados (estado) y, junto con Moq.Verify, interacciones
```

Sin Moq, probar `CheckoutService` requeriría una pasarela de pago real, un inventario real y un servidor de correo. Con Moq, cada prueba decide la situación del sistema en una línea:

| Servicio | Dependencia simulada | Para qué |
|---|---|---|
| AppointmentService | `IClock.UtcNow` → `2026-10-02 15:30 UTC` | Decidir sin ambigüedad qué es "pasado" y qué es "futuro". |
| AppointmentService | `IAppointmentRepository.HasOverlapAsync` → `true` | Provocar el solapamiento sin necesitar una base de datos con citas. |
| CheckoutService | `IInventoryService.HasStockAsync` → `false` solo para "Mouse" | Simular falta de stock de un producto concreto. |
| CheckoutService | `IPaymentGateway.ChargeAsync` → `false` / `ThrowsAsync(TimeoutException)` | Simular un pago rechazado o una pasarela caída. |
| UserRegistrationService | `IPasswordHasher.Hash("Samuel123!")` → `"HASH_TEST"` | Hash predecible para comprobar que nunca se guarda la contraseña original. |
| UserRegistrationService | `IUserRepository.ExistsByEmailAsync` → `true` | Simular un email ya registrado. |

`RegexValidator` **no** se simula en `UserRegistrationServiceTests`: es lógica pura, rápida y determinista. Tampoco se crean mocks de `TextAnalyzer`, `DateTimeService` ni `CollectionService`: no tienen dependencias externas que aislar.

---

## 9. Mock, Stub y Fake

| Tipo | Qué es | Se verifica… | En este proyecto |
|---|---|---|---|
| **Stub** | Doble que devuelve respuestas predefinidas. | El **estado/resultado** del sistema bajo prueba, no el stub. | `_clock.Setup(c => c.UtcNow).Returns(Now)`: solo provee la hora. |
| **Mock** | Doble que además registra llamadas para verificarlas después. | Las **interacciones**: qué se llamó, con qué argumentos, cuántas veces. | `_payment.Verify(p => p.ChargeAsync(CustomerId, It.Is<decimal>(a => a == 125.50m)), Times.Once)`. |
| **Fake** | Implementación funcional simplificada (por ejemplo, un repositorio en memoria con un `List<T>`). | El comportamiento conjunto. | No se usa: no hay implementaciones de las interfaces de infraestructura. |

Un mismo `Mock<T>` de Moq puede actuar como stub o como mock según cómo se use en cada prueba. En `CheckoutAsync_ValidCart_ReturnsOrderWithCartContents`, `_payment` es un **stub**: solo devuelve `true` y no se verifica. En `CheckoutAsync_ValidCart_ChargesTotalSavesAndNotifies`, el mismo `_payment` es un **mock**: se verifica que se llamó una vez con 125.50. El nombre de la clase de Moq (`Mock<T>`) no determina el rol; lo determina el uso.

---

## 10. Arrange / Act / Assert

Cada prueba se divide en tres bloques separados por una línea en blanco. En el código no se escriben los comentarios `// Arrange`, solo la separación; aquí se añaden para explicarlo. Ejemplo real, `UserRegistrationServiceTests.RegisterAsync_ValidData_NeverStoresPlainPassword`:

```csharp
[Fact]
public async Task RegisterAsync_ValidData_NeverStoresPlainPassword()
{
    // Arrange: preparar dependencias y capturar lo que se guarde
    User? savedUser = null;
    _users
        .Setup(users => users.AddAsync(It.IsAny<User>()))
        .Callback<User>(user => savedUser = user)
        .Returns(Task.CompletedTask);

    // Act: una sola llamada al método bajo prueba
    await _service.RegisterAsync(Name, RawEmail, Password);

    // Assert: comprobar el resultado observable
    savedUser.Should().NotBeNull();
    savedUser!.PasswordHash.Should().Be(PasswordHash).And.NotBe(Password);
}
```

- Los **`Setup` de Moq pertenecen a Arrange**: describen el mundo antes de actuar. Los comunes a toda la clase (reloj fijo, hasher, "el email no existe") están en el **constructor** de la clase de pruebas. xUnit crea una instancia nueva por prueba, así que el constructor es un Arrange compartido sin estado filtrado entre pruebas.
- Los **`Verify` de Moq pertenecen a Assert**: comprueban interacciones después de actuar.
- **Act** debe ser una sola acción. Cuando el método lanza una excepción, Act se escribe como `Func<Task> act = () => …` y la aserción `await act.Should().ThrowAsync<…>()` lo ejecuta dentro de Assert.

---

## 11. Testing de estado frente a testing de interacción

- **Estado:** se comprueba el **resultado** observable: el valor devuelto o el objeto creado. *"`order.Total` es 125.50"*.
- **Interacción:** se comprueba **cómo** el sistema usó sus dependencias. *"`paymentGateway.ChargeAsync(customerId, 125.50)` fue llamado una vez"*.

`CheckoutServiceTests` necesita ambos porque el método tiene **efectos que no aparecen en su valor de retorno**:

| Pregunta | Tipo | Prueba |
|---|---|---|
| ¿El total es correcto? | Estado | `result.Total.Should().Be(CartTotal)` |
| ¿La orden devuelta contiene los ítems del carrito? | Estado | `BeEquivalentTo(new Order(...))` |
| ¿Se cobró exactamente el total? | Interacción | `Verify(ChargeAsync(CustomerId, It.Is<decimal>(a => a == CartTotal)), Times.Once)` |
| Sin stock, ¿se evitó el cobro? | Interacción | `Verify(ChargeAsync(...), Times.Never)` |
| ¿Se guardó antes de notificar? | Interacción (orden) | `calls.Should().Equal("save", "notify")` con `Callback` |

Si una orden se devuelve con el total correcto pero el cobro nunca ocurrió, una prueba de estado no lo detecta. Si se cobra correctamente pero la orden devuelta tiene un total equivocado, una prueba de interacción tampoco. Por eso se usan las dos.

**Equilibrio.** Verificar interacciones acopla la prueba a la implementación. Se verifican solo las que forman parte del **contrato observable** (se cobra, se guarda, se notifica, no se cobra sin stock), no detalles internos, como cuántas veces se lee `IClock.UtcNow`.

---

## 12. Boundary testing

Los errores tienden a concentrarse en los límites (`<` frente a `<=`, rangos inclusivos frente a exclusivos, el primer o el último elemento). Un caso "normal", lejos del límite, pasa con ambas versiones del operador.

| Frontera | Prueba | Resultado esperado |
|---|---|---|
| `start == UtcNow` | `ScheduleAsync_StartIsInPast_ThrowsArgumentException(minutesFromNow: 0)` | Rechazada (`ArgumentException`). |
| `start == UtcNow + 1 tick` | `ScheduleAsync_StartOneTickAfterNow_IsAccepted` | Aceptada. Junto con la anterior, fija el operador exacto `<=`. |
| `businessDays = 0` | `AddBusinessDays_Zero_ReturnsStartUnchangedEvenOnWeekend` | Devuelve `start` sin cambios, incluso si es sábado. |
| `businessDays < 0` | `AddBusinessDays_Negative_ThrowsArgumentOutOfRangeException(-1, int.MinValue)` | `ArgumentOutOfRangeException`. |
| `quantity = 0` (carrito) | `CheckoutAsync_QuantityNotPositive_ThrowsArgumentException(0)` | Rechazada sin llamar al inventario. |
| `quantity = 1` (carrito) | "Mouse" (cantidad 1) en `CheckoutAsync_ValidCart_ChargesTotalSavesAndNotifies` | Aceptada. |
| `quantity = 0` / `1` (`GetTop`) | `GetTop_QuantityNotPositive_…(0)` / `GetTop_…([4], 1)` | Excepción / un elemento. |
| `31/02/2026` | `IsValidDateFormat_NonExistentDateWithValidShape_…` | `true`: solo se valida el formato. |
| Salto de línea final en el teléfono | `IsColombianMobile_InvalidNumbers_ReturnsFalse("3001234567\n")` | `false` gracias a `\z`. |
| Rangos que se tocan | `RangesOverlap_…("10:00","11:00","11:00","12:00")` | No se solapan (`[start, end)`). |
| 29/02 en año no bisiesto | `CalculateAge_BornOnFebruary29_…("2001-02-28" → 0, "2001-03-01" → 1)` | Cumpleaños el 1 de marzo. |

La mutación 4 de la sección 5 muestra el efecto en la práctica: solo el caso `start == UtcNow` detectó el cambio de `<=` a `<`.

---

## 13. Tests deterministas

Una prueba determinista produce **siempre el mismo resultado**, en cualquier máquina, día y hora:

```text
misma entrada + mismo estado simulado = mismo resultado
```

Si `AppointmentService` usara `DateTime.UtcNow` directamente, la prueba *"una cita para el 2026-10-03 se acepta"* pasaría hoy y fallaría a partir de mañana. Para evitarlo, el servicio depende de `IClock` y la prueba fija el instante:

```csharp
private static readonly DateTime Now = new(2026, 10, 2, 15, 30, 0, DateTimeKind.Utc);

_clock.Setup(clock => clock.UtcNow).Returns(Now);
```

Con eso, *"una cita a `Now.AddMinutes(-1)` está en el pasado"* es cierto para siempre, y `User.CreatedAt` puede compararse con igualdad exacta (`2026-10-02 15:30 UTC`) en lugar de con un rango aproximado.

Otras decisiones con el mismo fin:
- Las pruebas de `DateTimeService` reciben fechas fijas (`2026-10-02` es viernes) y no consultan el reloj del sistema.
- Los `Guid` de productos y clientes de `CheckoutServiceTests` son constantes (`"aaaaaaaa-…"`).
- Una búsqueda en todos los `.cs` del repositorio confirma que no se usan `DateTime.Now`, `DateTime.UtcNow` ni `DateTime.Today` en ninguna parte.
- Las regex tienen un tiempo límite de 1 segundo, así que un patrón patológico falla en lugar de colgar la suite.

---

## 14. Tests asíncronos

Extracto abreviado de `AppointmentServiceTests`:

```csharp
public async Task ScheduleAsync_AppointmentOverlaps_ThrowsInvalidOperationException()   // async Task, nunca async void
{
    _repository
        .Setup(r => r.HasOverlapAsync(start, end, It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);                                       // Moq: Task<bool> ya completada

    Func<Task> act = () => _service.ScheduleAsync("Ana", start, end);

    await act.Should().ThrowAsync<InvalidOperationException>();   // FA, con await
}
```

| Elemento | Uso |
|---|---|
| `async Task` | xUnit espera a que la prueba termine. Con `async void`, xUnit no podría saber cuándo termina ni capturar sus excepciones. |
| `await` | Espera el resultado sin bloquear el hilo. |
| `ReturnsAsync(x)` | Configura un método que devuelve `Task<T>` para que devuelva una tarea ya completada con `x`. |
| `ThrowsAsync(ex)` (Moq) | Simula una dependencia asíncrona que falla (`CheckoutAsync_PaymentGatewayFails_…`). |
| `Assert.ThrowsAsync<T>` / `Should().ThrowAsync<T>()` | Comprueban que la tarea termina con la excepción esperada. Ambas deben llevar `await`. |

**Por qué nunca `.Result` ni `.Wait()`:**
- **Bloquean** un hilo esperando una tarea, lo que puede causar *deadlocks* en contextos con `SynchronizationContext`.
- **Envuelven las excepciones** en `AggregateException`, así que `Assert.Throws<InvalidOperationException>` ya no coincidiría y la prueba fallaría o comprobaría algo distinto.
- Si una prueba olvida el `await` de `act.Should().ThrowAsync<…>()`, la aserción **no se espera** y la prueba puede terminar en verde antes de que se evalúe. El compilador advierte con CS4014 cuando se descarta una tarea dentro de un método `async`, y para `Assert.ThrowsAsync` el analizador de xUnit añade la regla xUnit2021. Como el proyecto compila con 0 advertencias, no hay ningún caso así.

Una búsqueda en el repositorio confirma que no hay `.Result`, `.Wait()` ni `GetAwaiter().GetResult()` en las pruebas.

---

## 15. What not to test

| No se prueba | Por qué |
|---|---|
| Métodos privados (`ValidateCart`, `CalculateLuhnSum`, `IsBusinessDay`…) | Se prueban **a través** de los métodos públicos. Probarlos directamente ataría las pruebas a la estructura interna e impediría refactorizar. |
| LINQ de .NET (`Distinct`, `Except`, `OrderByDescending`) | Ya está probado por Microsoft. Se prueba el **contrato de nuestros métodos** (por ejemplo, orden de primera aparición), no la librería. |
| `Guid.NewGuid()` como algoritmo | Solo se comprueba lo observable: el `Id` no está vacío y dos citas tienen `Id` distintos. |
| Que Moq o xUnit funcionen | Una prueba que hace `Setup(...).Returns(5)` y luego afirma que el mock devuelve 5 solo prueba Moq. |
| Propiedades autogeneradas de los `record` | Las genera el compilador. Su cobertura llega sola al usarlas en pruebas reales. |
| Mensajes de excepción palabra por palabra | Se usan comodines (`WithMessage("*empty*")`) para fijar la idea sin volver frágil la prueba ante cambios de redacción. |
| Detalles no observables (cuántas veces se lee `IClock.UtcNow`) | No forman parte del contrato. Verificarlos haría fallar pruebas ante refactorizaciones válidas. |
| Pruebas triviales para subir la cobertura | Sin aserciones con significado no aportan confianza (sección 5). Por eso la línea 59–61 de `CheckoutService` se analizó en lugar de "cubrirse". |

---

## 16. VerifyNoOtherCalls

Se usa en `ScheduleAsync_CustomerIsEmpty_ThrowsArgumentException`:

```csharp
await act.Should().ThrowAsync<ArgumentException>().WithParameterName("customer");
_repository.VerifyNoOtherCalls();
```

**Qué hace.** Falla si el mock recibió **cualquier** llamada que no haya sido verificada antes con `Verify`. Como aquí no hay ningún `Verify` previo, significa *"el repositorio no fue tocado en absoluto"*.

**Ventajas**
- **Cubre métodos futuros.** `Verify(r => r.AddAsync(...), Times.Never)` solo vigila `AddAsync`. Si mañana el servicio llama a un nuevo `r.LogAttemptAsync(...)` antes de validar al cliente, solo `VerifyNoOtherCalls` lo detectaría.
- **Es conciso** cuando la expectativa es "ninguna interacción": una línea en lugar de un `Times.Never` por cada método de la interfaz.
- **Expresa bien la intención** en los rechazos por validación: la entrada es inválida y no debe producirse ningún efecto externo.

**Desventajas y por qué se usa con moderación**
- **Fragilidad.** En un caso exitoso obliga a verificar *todas* las llamadas. Cualquier llamada nueva y legítima rompe pruebas que no tienen que ver con ese cambio.
- **Acoplamiento a la implementación**, por la misma razón.
- **Solo afecta al mock sobre el que se invoca.** `_repository.VerifyNoOtherCalls()` no dice nada sobre `_clock`.

**Criterio del proyecto:** `VerifyNoOtherCalls` se usa en caminos de rechazo, donde "nada debe ocurrir" es el contrato. En los caminos de éxito y de error parcial se usan `Times.Once`, `Times.Never` y `Times.Exactly` explícitos sobre las interacciones que importan. En `CheckoutServiceTests` el helper `VerifyNoExternalCallsWereMade()` aplica la misma idea con `Times.Never` explícito sobre los cuatro colaboradores.

---

## 17. Reproducir los resultados

```powershell
dotnet build                 # 0 errores, 0 advertencias
dotnet test                  # 356 passing
.\scripts\coverage.ps1       # TestResults/ + CoverageReport/index.html
```

Las cifras de la sección 3 se pueden volver a comprobar en `CoverageReport/Summary.txt` o en los atributos `line-rate`, `branch-rate`, `lines-covered`, `lines-valid`, `branches-covered` y `branches-valid` del `coverage.cobertura.xml`.
