# Master Roshi — paquete proporcionado por el usuario

Origen: Wii - Dragon Ball: Revenge of King Piccolo, Characters - Master Roshi.zip.
Se conservaron el DAE y las cinco texturas originales. El PSK permanece en el ZIP original.
Se añadió `Z_UP` al DAE: las posiciones originales usan Z como altura, pero el archivo omitía ese dato.

El adaptador ImportedTrainerModel carga N02_321out desde Resources, asigna las dos texturas referenciadas por el DAE, normaliza la altura y orienta el rostro hacia delante. La opción Sabio del vestidor usa este modelo; los otros dos estilos conservan los modelos procedurales.

No contiene clips de animación. Se aplica una pose relajada y un balanceo básico de hombros/caderas conservando sus rotaciones iniciales. No equivale a clips de correr, esquivar o lanzar.
El vestidor desactiva las opciones de apariencia procedural para este modelo, pues sus texturas y accesorios no están separados para ese sistema. Se puede cambiar de personaje y de nombre normalmente.

Pendiente verificar visualmente la importación en Unity y probar movimiento en dispositivo.
