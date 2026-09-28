# Plan de ramas del MVP

## Reglas de trabajo

Cada rama `modxxx` nace desde `develop`, aborda un único hito, documenta su alcance y se integra a `develop` sólo después de aprobar compilación y tests. La integración a `main` se realiza desde `develop` al alcanzar un hito estable. Las ramas `modxxx` son temporales y se eliminan después de su integración.

## `mod001` — Bootstrap del repositorio, app, CI y documentación

**Origen:** `develop`.

**Alcance:** crear la solución .NET, la app WPF mínima, un proyecto xUnit con una regla real testeada, la estructura inicial de carpetas, el workflow de GitHub Actions y la documentación del MVP. No incluye funcionalidad fiscal.

**Pruebas de integración:** `dotnet restore`, `dotnet build` y `dotnet test` exitosos; la app inicia y muestra `ARCA Facturador`; el workflow ejecuta sobre Windows.

## `mod002` — Configuración fiscal local y constantes del comprobante

**Origen:** `develop` luego de integrar `mod001`.

**Alcance:** modelar y validar la configuración de un único CUIT y punto de venta; definir Factura C, consumidor final, transferencia bancaria, concepto servicios, código `0001`, descripción `Honorarios por servicio`, cantidad `1` y unidad local `Otras unidades`. Implementar reglas puras para `FchServDesde` como primer día del mes, `FchServHasta` como último día del mes y `FchVtoPago` como fecha actual de emisión/autorización. Sin persistencia ni ARCA.

**Pruebas de integración:** tests de constantes, validaciones y fechas, incluidos febrero, año bisiesto y cambio de año; build y tests completos exitosos.

## `mod003` — Persistencia mínima SQLite

**Origen:** `develop` luego de integrar `mod002`.

**Alcance:** incorporar SQLite con sólo `settings`, `products` e `invoices`; inicialización automática y repositorios simples. Guardar lo indispensable para configuración, precios frecuentes y recuperación de comprobantes. Sin auditoría, usuarios ni reportes.

**Pruebas de integración:** creación de base vacía, lectura/escritura de cada entidad, restricciones mínimas y tests con base temporal aislada; build y tests completos exitosos.

## `mod004` — UI local de facturación sin ARCA

**Origen:** `develop` luego de integrar `mod003`.

**Alcance:** construir la pantalla principal con importe editable, datos fijos visibles, fechas calculadas, validación y confirmación previa. Simular la emisión local sin invocar servicios externos.

**Pruebas de integración:** tests de lógica de presentación y validación de importes; prueba manual del flujo completo y de mensajes de error; build y tests completos exitosos.

## `mod005` — Catálogo simple de precios frecuentes

**Origen:** `develop` luego de integrar `mod004`.

**Alcance:** permitir alta, edición, selección y baja de importes frecuentes, manteniendo fijo el concepto del servicio. Evitar funciones de inventario o gestión comercial.

**Pruebas de integración:** CRUD persistente, selección que completa el importe, validación de duplicados o valores inválidos y prueba manual de usabilidad; build y tests completos exitosos.

## `mod006` — PDF local del comprobante

**Origen:** `develop` luego de integrar `mod005`.

**Alcance:** generar un PDF local con datos fiscales, detalle local fijo, importes, fechas y campos reservados para CAE y vencimiento. Definir ubicación y nombre deterministas. No autorizar todavía ante ARCA.

**Pruebas de integración:** generación válida y legible, verificación automatizada de campos esenciales, manejo de ruta no disponible y revisión visual del documento; build y tests completos exitosos.

## `mod007` — WSAA en homologación

**Origen:** `develop` luego de integrar `mod006`.

**Alcance:** firmar la solicitud de acceso con certificado local, invocar WSAA de homologación, interpretar y almacenar temporalmente el Ticket de Acceso, y renovarlo cuando corresponda. Nunca versionar secretos.

**Pruebas de integración:** tests unitarios de solicitud y vigencia, manejo de certificado inválido o vencido, prueba manual controlada contra homologación y logs sin datos sensibles; build y tests completos exitosos.

## `mod008` — WSFEv1 en homologación para Factura C

**Origen:** `develop` luego de integrar `mod007`.

**Alcance:** consultar el último comprobante, construir la solicitud de Factura C de servicios, enviarla a WSFEv1 en homologación, interpretar aprobación, rechazo, observaciones y CAE, persistir el resultado y completar el PDF local.

**Pruebas de integración:** serialización y mapeo de campos, numeración consecutiva, fechas e importes, respuestas aprobadas y rechazadas simuladas, y emisión manual exitosa en homologación; build y tests completos exitosos.

## `mod009` — Reconciliación y errores críticos

**Origen:** `develop` luego de integrar `mod008`.

**Alcance:** evitar duplicados ante cortes o respuestas inciertas; reconciliar mediante consultas a ARCA antes de reintentar; distinguir error recuperable, rechazo y comprobante autorizado; asegurar recuperación del CAE y PDF.

**Pruebas de integración:** escenarios de timeout antes y después de autorizar, reintento sin duplicación, recuperación de comprobante existente y mensajes accionables; build y tests completos exitosos.

## `mod010` — Producción y seguridad local

**Origen:** `develop` luego de integrar `mod009`.

**Alcance:** separar homologación de producción, validar CUIT, punto de venta y certificado, proteger secretos y rutas locales, reducir logs sensibles e incluir confirmaciones que eviten emisiones accidentales.

**Pruebas de integración:** selección inequívoca de ambiente, ausencia de secretos en repositorio y logs, validación de configuración incompleta y prueba controlada de conexión productiva sin emisión accidental; build y tests completos exitosos.

## `mod011` — Conexión real desde GUI y primera prueba productiva

**Origen:** `develop` luego de integrar `mod010`.

**Alcance:** conectar la pantalla principal con el flujo real WSAA + WSFEv1, agregar prueba de conexión no emisora, emitir Factura C electrónica desde la GUI, mostrar confirmación fuerte en producción, persistir autorización/rechazo/pendiente, regenerar PDF con CAE y documentar el checklist de primera emisión real. No empaqueta todavía la aplicación.

**Pruebas de integración:** tests de emisión autorizada, rechazo y error recuperable; prueba de conexión productiva sin emisión; primera emisión real controlada desde GUI; verificación posterior en ARCA; build y tests completos exitosos.

## `mod012` — Instalación local y empaquetado

**Origen:** `develop` luego de integrar `mod011`.

**Alcance:** publicar la aplicación para Windows, crear un mecanismo simple de instalación o distribución, definir directorios de datos y PDF, acceso directo y procedimiento de respaldo y actualización en la PC objetivo.

**Pruebas de integración:** instalación limpia, primer inicio, persistencia entre versiones, permisos de escritura, desinstalación sin pérdida accidental de datos y prueba en la PC objetivo; build y tests completos exitosos.

## `mod013` — Cierre del MVP y pase a `main`

**Origen:** `develop` luego de integrar `mod012`.

**Alcance:** ejecutar pruebas de aceptación de punta a punta, corregir únicamente bloqueantes del MVP, completar manual operativo y checklist de recuperación, fijar versión y preparar el PR/MR de `develop` a `main`.

**Pruebas de integración:** emisión real controlada de una Factura C, verificación en ARCA, persistencia y PDF con CAE, reinicio y consulta local, instalación reproducible, CI verde y aceptación del usuario. Luego se etiqueta la versión estable correspondiente.
