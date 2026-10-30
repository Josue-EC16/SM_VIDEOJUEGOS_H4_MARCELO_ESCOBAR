# Super Mario Bros (2D)

MARCELO JOSUE ESCOBAR CHIPANA 
EVALUACIÓN INDIVIDUAL HITO 4 PROGRAMACIÓN GRÁFICA Y MULTIMEDIA

# CREDITOS
Repositorio original:

https://github.com/Jesusbautistah/SUPER-MArio.git

Autor: Jesusbautistah

## Controles y final de partida

**A/D o flechas** para caminar, **Espacio** para saltar, **S** para entrar a la
tubería bonus y **D** para salir por conexiones laterales.

Al agotar las vidas aparece **Game Over**, con únicamente **Volver a intentar**
(reinicia en `1-1`, con tres vidas, cero monedas y Mario pequeño) y **Salir**.
Las primeras columnas de bronce de `1-2` forman un tramo continuo.

## Menú y recorrido completo

Abre `Assets/Scenes/MainMenu.unity` para empezar desde el menú. También es la
primera escena de la compilación. **JUGAR** comienza una partida nueva en `1-1`;
las banderas conectan `1-1 → 1-2 → 1-3`. Al terminar el tercer nivel se vuelve al
menú con el mensaje de recorrido completado. **ELEGIR NIVEL** permite comenzar
en cualquiera de los tres niveles con tres vidas y cero monedas. **SALIR**
cierra el juego (en el editor detiene Play). **Esc** vuelve al menú durante una partida.

En `1-3`, mantén **Espacio**, **W** o **↑** para nadar hacia arriba; suelta para
descender suavemente o mantén **S / ↓** para bajar más rápido. Evita los peces
y usa **D** en la tubería lateral del final para salir a la superficie y llegar
a la bandera. La natación utiliza los fotogramas existentes de Mario; peces,
burbujas y nuevos audios se generan localmente, sin paquetes adicionales.

Las herramientas de **Educativo > Campaña** reconstruyen sólo el nivel acuático
y el menú, conservando los mapas anteriores. Las pruebas optativas del ejecutable
de desarrollo se activan con `-part1Smoke`; `-aquaticSmoke` verifica específicamente
la natación, los peces, la entrada baja de la tubería y ambos tamaños de Mario.
`-menuQuitSmoke` comprueba el botón Salir del menú principal. Los informes y
capturas quedan en la carpeta `Verification`, junto al ejecutable.
`Builds/` y `Docs/` siguen excluidos de Git.

