# Catálogo CH

La selección contiene 32 opciones: Ash y Roshi, conservados, más 30 personajes elegidos de Characters ch.zip. La lista reproducible está en ch_character_selection.json. Los nombres siguen los identificadores del paquete.

Los antiguos personajes Package/tr… desaparecen del selector, pero sus archivos se conservan. Las partidas que tenían uno de ellos pasan a Serena. El nombre propio y la foto del perfil no cambian.

Los modelos se exportaron con Blender a Package/ch…/Model.fbx, con esqueleto, materiales y asignaciones explícitas en import.json. Los iconos están en Icons/Package. PackageTrainerMotion reconoce LArm/RArm y LThigh/RThigh además de los huesos del paquete anterior.

Se comprobaron 30 modelos y sus texturas/iconos, se renderizaron miniaturas con Blender y se compiló el código C#. La validación de Unity no pudo iniciar; hay que revisar los modelos en Play. No se copiaron los efectos adicionales ni las secuencias .animseq del paquete: el movimiento usa el adaptador procedural.

Conversión: extraer los subarchivos seleccionados en tmp/characters-ch/source y ejecutar Blender --background --python Tools/import_ch_characters.py. Las miniaturas se generan con Tools/render_ch_characters.py. Ambos scripts omiten salidas ya generadas.
