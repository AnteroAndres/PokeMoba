# Perfil personal

Haz clic en la tarjeta superior izquierda del menú principal para editar nombre y foto. La galería permite elegir retratos disponibles en el proyecto. Guarda los cambios para aplicarlos; Cancelar conserva los anteriores. El nombre propio permanece al cambiar de personaje.

El nombre admite 3–16 letras, números o guion bajo. El registro local profile_names.json no distingue mayúsculas y ofrece tres variantes si otro identificador ya reservó el nombre. La exclusividad entre dispositivos requiere un servidor; todavía no existe esa validación online.

## Comprobarlo

1. Guarda un nombre y un retrato en el editor de perfil.
2. Cambia de personaje: tu nombre y foto deben conservarse.
3. Reinicia Play: ambos deben mantenerse.
4. En Unity, abre PokeMoba → Validar nombres de perfil. La consola debe indicar «Perfil: prueba correcta». La prueba usa dos identidades y un archivo temporal; no modifica tu partida.

Tools/tests/ProfileNameRegistryTests.cs incluye nueve comprobaciones de disponibilidad, duplicados, variantes, persistencia, renombrado y formato.
