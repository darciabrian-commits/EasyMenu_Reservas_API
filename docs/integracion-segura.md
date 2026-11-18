# Integración segura de Reservas API

## Credencial de servicio

Todos los endpoints MVC requieren autenticación. La API acepta una clave de servicio en
`X-Reservas-Service-Key`; el valor esperado se configura como `Authentication__ServiceApiKey`
en el entorno del servidor. Debe ser un secreto aleatorio de al menos 32 bytes. Si falta o
es demasiado corto, la API no autentica solicitudes y responde 401. La comparación es en
tiempo constante.

La clave es únicamente una credencial entre servidores: EasyMenu Web podría adjuntarla a
su `HttpClient` servidor-a-servidor, pero no debe incluirla en vistas, JavaScript, cookies
ni solicitudes originadas en el navegador. Esta clave no autentica al usuario de Web ni
representa sus roles. Antes del despliegue, confirmar en el proyecto Web el flujo de sesión,
el uso de Bearer con la API principal y la autorización por usuario; mantener esa decisión
separada de la identidad del servicio.

No se habilita CORS: las llamadas servidor-a-servidor no lo necesitan. Si se decide que el
navegador llame directamente, definir y aprobar explícitamente los orígenes antes de
configurarlo.

## Contrato de reservas y disponibilidad

Se conservan las rutas existentes bajo `/api/Reservas` y `/api/Mesas`; todas requieren
la credencial de servicio. Los controladores mantienen 201 para creación, 400 para datos
inválidos, 404 para identificadores inexistentes y 409 para disponibilidad o concurrencia.
Los errores de SQL no se devuelven al cliente.

La disponibilidad sigue significando igualdad exacta de mesa, fecha y hora. No se conoce
la duración comercial de una reserva: no se calculan intervalos ni solapamientos entre horas
distintas hasta que el negocio defina esa regla.

## SQL Server, configuración y migraciones

Proporcionar `ConnectionStrings__DefaultConnection` mediante variables de entorno/secretos
del despliegue. No hay una cadena de conexión por defecto en `appsettings.json`. Usar
`Encrypt=True` y validar el certificado del servidor (`TrustServerCertificate=False`); no
almacenar secretos en Git ni desactivar la validación del certificado como solución de
producción.

La migración `ReservaDisponibilidadActiva` hace una prevalidación antes de limitar `Estado`
a 20 caracteres y crear el índice filtrado único. Si ya hay estados más largos o reservas
activas duplicadas, la migración falla sin borrar registros. Un operador debe revisar y
resolver los datos preservándolos antes de volver a generar/revisar el script. La aplicación
no aplica migraciones al arrancar: generar y revisar el SQL, y aplicarlo manualmente solo a
la base exclusiva de Reservas, nunca a la base principal.
