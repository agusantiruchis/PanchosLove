# PanchosLove API

API REST en **C# / ASP.NET Core 8** para la panchería gourmet PanchosLove:
menú digital, pedidos (local y delivery), panel de cocina en tiempo real,
inventario de insumos con descuento automático y métricas de venta.

## Cómo abrirlo
1. Instalá **Visual Studio 2022 (17.8+)** con la carga de trabajo *ASP.NET y desarrollo web* (incluye el SDK de .NET 8).
2. Abrí `PanchosLove.sln`.
3. Presioná **F5**. Se abre Swagger en `http://localhost:5080/swagger`.

La base de datos es **SQLite** (`panchoslove.db`, se crea sola con datos de ejemplo). No necesitás instalar nada más.

## Usuarios de ejemplo (cambiarlos antes de producción)
| Rol    | Email                    | Contraseña  |
|--------|--------------------------|-------------|
| Admin  | admin@panchoslove.com    | Admin123!   |
| Cajero | cajero@panchoslove.com   | Cajero123!  |
| Cocina | cocina@panchoslove.com   | Cocina123!  |

En Swagger: `POST /api/auth/login` → copiá el `token` → botón **Authorize**.

## Endpoints
| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | Público | Devuelve JWT |
| GET | `/api/menu` | Público | Menú por categorías + extras; `inStock` según inventario real |
| POST | `/api/orders` | Público | Crea pedido (web o mostrador). Valida y descuenta stock |
| GET | `/api/orders/track/{number}` | Público | Estado del pedido |
| GET | `/api/orders?status=&date=` | Admin, Cajero | Pedidos del día |
| GET | `/api/orders/{id}` | Admin, Cajero, Cocina | Detalle |
| PUT | `/api/orders/{id}/status` | Admin, Cajero | Cambia estado; `Cancelado` repone stock |
| GET | `/api/kitchen/orders` | Cocina, Cajero, Admin | Cola de cocina (Pendiente/EnPreparacion/Listo) |
| PUT | `/api/kitchen/orders/{id}/status` | Cocina, Cajero, Admin | Avanza el pedido |
| GET/POST/PUT/DELETE | `/api/products` | Admin | ABM de productos y recetas |
| PATCH | `/api/products/{id}/availability?available=` | Admin | Pausar/activar en el menú |
| GET/POST/PUT | `/api/categories` | Admin | Categorías |
| GET/POST/PUT | `/api/ingredients` | Admin | Insumos (`?lowStock=true` para alertas) |
| POST | `/api/ingredients/{id}/restock` | Admin | Ingreso de mercadería |
| POST | `/api/ingredients/{id}/adjust` | Admin | Ajuste por conteo físico |
| GET | `/api/ingredients/{id}/movements` | Admin | Historial de movimientos |
| GET | `/api/metrics/summary?from=&to=` | Admin | Ventas, ticket promedio, top productos, por hora/día/medio de pago |

### Tiempo real (SignalR)
Hub: `/hubs/kitchen` (JWT por `?access_token=`). Eventos: `OrderCreated`, `OrderStatusChanged`, `LowStock`.

### Ejemplo de pedido
```json
POST /api/orders
{
  "customerName": "Juan",
  "phone": "1122334455",
  "type": "Delivery",
  "deliveryAddress": "Mitre 123, Avellaneda",
  "paymentMethod": "Efectivo",
  "items": [
    { "productId": 2, "quantity": 2, "extraIds": [6], "notes": "sin cebolla" },
    { "productId": 4, "quantity": 2 }
  ]
}
```

## Cómo funciona el stock
Cada producto (y extra) tiene una **receta**: cuántos gramos/unidades de cada insumo consume.
Al crear un pedido se suma lo requerido, se valida que alcance y se descuenta en una sola operación
registrando un `StockMovement`. Si falta algo, responde `409` con el detalle. Al cancelar, se devuelve lo descontado.

## Estructura
```
PanchosLove.Api/
  Controllers/  Auth, Menu, Products, Ingredients, Orders, Kitchen, Metrics
  Services/     OrderService (lógica de pedidos y stock), Support (errores, hora AR)
  Models/       Entidades y enums
  Data/         AppDbContext, DbSeeder
  Dtos/         Contratos de entrada/salida
  Hubs/         KitchenHub (SignalR)
```

## Antes de producción
- Cambiar `Jwt:Key` (User Secrets o variables de entorno) y las contraseñas de ejemplo.
- Pasar a SQL Server/PostgreSQL y usar migraciones de EF Core (hoy usa `EnsureCreated`).
- Agregar control de concurrencia de stock si habrá varios cajeros muy simultáneos.
- Ajustar `Cors:Origins` a los dominios reales del front.
