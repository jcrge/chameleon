# Camaleón
Este es el repositorio de mi Proyecto de Fin de Ciclo.

# CHANGELOG
## Semana 2021/04/05 - 2021/04/11
- Tiempo utilizado para las preparaciones para el proyecto.
  - Aprender [Xamarin.Forms, Xamarin.Android](https://docs.microsoft.com/en-us/learn/paths/build-mobile-apps-with-xamarin-forms/?WT.mc_id=docs-dotnet-learn), creación de vistas personalizadas para Xamarin.Android, programación asíncrona con async/await, [patrón de Microsoft para disponer de recursos](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-5.0), etc.
  - Preparar el entorno en nuevo ordenador (Windows, particiones de disco, Visual Studio 2019, copiar cosas del ordenador antiguo, etc.).
## Semana 2021/04/12 - 2021/04/18
- Se crea el proyecto.
- Se crea la interfaz del reproductor de audio (vista personalizada).
  - No ha sido fácil hacer que la vista se adapte correctamente a las diferentes configuraciones de tamaño (`android:layout_width`, `android:layout_height`, etc.). En general son más difíciles de programar que los componentes de Windows Forms. Actualmente se expande para ocupar todo el ancho aunque su `android:layout_width` sea `wrap_content`, pero sí se comporta correctamente a lo largo, que es lo que será importante para la aplicación final.
  - La actividad principal está preparada para que el usuario elija un archivo de audio o vídeo y este se cargue en una instancia de esta vista. Esta actividad cambiará luego y solo está así temporalmente para hacer pruebas.
- Se hace funcional la interfaz del reproductor.
  - Se implementa IDisposable para liberar el SimpleAudioPlayer y el flujo de audio. Es importante liberar bien los recursos, ya que se trabajará con muchos archivos de audio y reproductores y muchos tendrán una vida muy corta.
  - Se gestionan los eventos y establecen valores sensatos para saltar a diferentes puntos del audio de forma óptima.
    - Los saltos no son demasiado cortos como para llegar rápido a un punto específico ni demasiado largos como para hacer tedioso escuchar una misma palabra o frase corta repetidas veces. Se implementan pulsaciones cortas y largas para cumplir satisfactoriamente ambas demandas.
- Se crea la grabadora, una vista que extiende Button.
- Se elimina la dependencia en Xam.Plugin.SimpleAudioPlayer a favor de utilizar directamente MediaPlayer, ya que la grabadora utiliza MediaRecorder y SimpleMediaPlayer no aporta nada especial a MediaPlayer para aplicaciones Xamarin.Android sin Xamarin.Forms.
- Se modifica la actividad temporal de prueba para conectar una grabadora con el reproductor, de modo que las grabaciones son reproducibles.
## Semana 2021/04/19 - 2021/04/25
- Se crea la clase `WAVEdition`, con los métodos estáticos `IsPcmWav` y `Split` que permiten, respectivamente, comprobar si un archivo es un archivo de audio WAV en formato PCM válido y subdividir audios de esta clase a partir de un punto de ruptura en milisegundos.
- Se crea `StagingArea`, clase que gestionará la compresión y descompresión de archivos de proyecto, regulará el acceso al índice en el que se registran los segmentos de audio existentes en el proyecto que el usuario tiene abierto, etc.
  - Actualmente contiene varios métodos a bajo nivel para gestionar un directorio raíz configurable. En la práctica, este será `FileSystem.AppDataDirectory`, el directorio que Android asigna a la aplicación para guardar internamente y por un tiempo indefinido los datos que esta necesite para funcionar correctamente. El uso de este directorio facilitará el acceso a los archivos que están comprimidos dentro de los archivos de proyecto y permitirá que las sesiones queden guardadas al cerrar la aplicación y se puedan restablecer en la próxima ejecución.
  - En este directorio se guarda tanto el proyecto actualmente abierto en formato descomprimido como un archivo adicional con una ruta al archivo comprimido donde se deben guardar los cambios.
  - `StagingArea` puede estar en estado cargado o no cargado.
    - En estado no cargado, el usuario de la clase llama a métodos para preparar el directorio raíz con un proyecto nuevo o uno existente, limpiar el directorio, etc.
    - Una vez se llama a `StagingArea.Load`, se cargan los contenidos del directorio en memoria y hacen accesibles para el usuario de la clase. Notablemente, se expone una clase índice con una lista de los segmentos de audio guardados en el proyecto, con la que se podrá trabajar sin tener que interactuar de forma directa con el sistema de archivos.
    - De este modo, el objetivo de `StagingArea` es ofrecer una abstracción entre el sistema de archivos y la estructura e información del proyecto.
  - Pronto también se añadirán varios métodos adicionales a más alto nivel para poder expresar las diferentes operaciones que modifican el estado del proyecto de forma atómica. Por ejemplo, la división de un segmento de audio del proyecto en dos subsegmentos se solicitará mediante uno de estos métodos y consistirá en dar los siguientes pasos:
    - Leer el actual valor `NextId`, un número entero guardado en el índice que se incrementa según se van añadiendo audios al proyecto.
    - Utilizar `WAVEdition.Split` para dividir el audio original en los nuevos audios `{NextId}.wav` y `{NextId + 1}.wav`.
    - Eliminar del índice la entrada del audio original.
    - Insertar dos nuevas entradas contiguas en el índice para cada uno de los nuevos audios, con IDs `NextId` y `NextId + 1`, ocupando el primero de ellos la posición que ocupaba el audio eliminado para que así los dos nuevos segmentos se muestren al usuario donde previamente había uno solo.
    - Actualizar `NextId` a `NextId + 2`.
    - Eliminar el archivo de audio cuya entrada en el índice ya no existe.
    - Llamar a `StagingArea.Flush` para que los cambios en la clase índice se guarden de inmediato en su archivo asociado en `FileSystem.AppDataDirectory`. De lo contrario, si el usuario cerrase la aplicación sin guardar manualmente, la próxima vez que entrase el índice seguiría teniendo solamente la entrada eliminada y esta enlazaría con un archivo ya no existente.
## Semana 2021/04/26 - 2021/05/02
- Se implementa la clase `Project`.
  - En vez de asignar a `StagingArea` todas las operaciones de manipulación del directorio de trabajo, existirán en esta última clase solo los métodos para prepararlo con un proyecto (es decir, o uno nuevo o uno ya existente).
  - Una vez el directorio esté listo con un proyecto, `StagingArea.Load` devuelve un `Project` con métodos para realizar operaciones sobre el proyecto preparado.
  - De este modo ya no es necesario distinguir constantemente en `StagingArea` entre estados cargado y no cargado y la clase es conceptualmente más simple.
- Se implementan métodos en `Project` para realizar modificaciones controladas sobre un proyecto.
  - `UpdateCompressedFile()`: actualiza el archivo de proyecto comprimido con la información del directorio de trabajo.
  - `AppendChunk(string path)`: añade un segmento de audio al final de la lista de segmentos del proyecto.
  - `SplitChunk(string sourceChunkId, int midpointMsec)`: divide un segmento de audio existente en el proyecto en dos subsegmentos en función de su ID y del punto de ruptura en milisegundos.
  - `DeleteChunk(string id)`: elimina un segmento de audio del proyecto.
- A partir de esta semana debería quedar solo implementar las interfaces gráficas para poner en funcionamiento las herramientas que he ido creando hasta ahora (de gestión del proyecto, grabación de audio, reproducción...).
## Semana 2021/05/03 - 2021/05/09
- Se remplaza el archivo `stored-at.txt` que contenía la ruta al archivo comprimido actualmente abierto por `compressed-state.json`. Este archivo contiene la siguiente información:
  - La ruta al archivo comprimido actualmente abierto (al igual que indicaba `stored-at.txt`) o null en caso de que no se haya especificado una. Si se siguiese utilizando `stored-at.txt`, el usuario tendría que especificar alguna ruta necesariamente durante la creación del proyecto, lo que no era deseable desde el punto de vista de la usabilidad. Este cambio nos permitirá preguntar por primera vez por la ruta una vez se presione el botón de _guardar_ o el de _guardar como..._. Posteriormente, el botón de _guardar_ utilizará automáticamente la ruta establecida, mientras que _guardar como..._ asignará una nueva, como es costumbre en otras aplicaciones.
  - Un booleano indicando si se han producido cambios en el directorio de trabajo que no hayan sido guardados en el archivo comprimido. El programa lo utilizará para preguntar al usuario si desea guardar los cambios en el proyecto antes de cerrarlo, en caso de que el archivo comprimido no esté actualizado.
- Se implementa la actividad de inicio de la aplicación. Esta contiene botones para abrir un proyecto existente, crear uno nuevo o restablecer la última sesión (en caso de que exista una). En todos los casos se acaba preparando de algún modo el directorio de trabajo mediante `StagingArea` para después lanzar la actividad `ProjectActivity`, que será la actividad principal del proyecto (actualmente vacía).
## Semana 2021/05/10 - 2021/05/16
- Se añade la duración de los segmentos de audio al `index.json`, realizando los cambios pertinentes en funciones como `Project.AppendChunk` y `Project.SplitChunk` para que este valor se corresponda siempre con el correcto y modificando ligeramente la API de `WAVEdition` para hacer eficiente y práctico el acceso a este dato. Disponer de esta duración en el índice nos permite mostrarla junto a cada segmento en la actividad principal de proyecto, ya que de otro modo habría que cargar todos los archivos de audio al iniciar la actividad solo para obtener esta información y esto sería prohibitivamente ineficiente.
- Se implementa parcialmente la actividad principal de proyecto (`ProjectActivity`), en la que se muestran los segmentos de audio para un proyecto ya cargado en el directorio de trabajo. Actualmente, la actividad muestra en un RecyclerView, para cada segmento, su nombre, duración y subtítulos. Manteniendo pulsado sobre alguna entrada de la lista, esta se selecciona y se entra en modo de selección múltiple, permitiendo seleccionar y deseleccionar más segmentos de audio pulsando sobre ellos. Los elementos seleccionados se marcan claramente con un fondo verde. Pulsando el botón de retroceso se deseleccionan todos los elementos y se termina el modo de selección múltiple. Pronto, los elementos seleccionados se podrán dividir, eliminar y clonar pulsando en los botones correspondientes en la barra superior de la actividad. Dichos botones ya están implementados a nivel de interfaz gráfica. También se incluyen en la barra superior botones para "guardar" y "guardar como...", completamente funcionales.
- Se cambia la filosofía de guardado de proyectos. En vez de abrir y guardar archivos en un directorio arbitrario del sistema elegido por el usuario, todos los proyectos se guardan en un directorio interno de la aplicación. Cuando el usuario guarda un proyecto, se pide únicamente un nombre y el archivo se guarda con ese nombre en dicho directorio, y cuando el usuario elige abrir un proyecto, se muestra únicamente una lista con los proyectos existentes en la carpeta. Este cambio de última hora lo he considerado necesario al encontrarme con que Android, pese a que permite lanzar un diálogo para guardar un archivo en una ruta elegida por el usuario, no le permite al programador averiguar cuál es dicha ruta, sino que únicamente proporciona una URI a través de la que se puede abrir un flujo, lo que complicaba el modo de trabajo planteado en las clases `StagingArea` y `Project`. Este cambio supuso una refactorización que culminó también con la desaparición de la clase `StagingAreaFS` y, por tanto, la conversión de los métodos de `StagingArea` a estáticos, y me hizo también implementar dos actividades con las que no contaba al principio del proyecto (una para pedir un nombre válido y disponible de proyecto para guardarlo, y otra para mostrar los proyectos guardados previamente y poder escoger uno).
## Semana 2021/05/17 - 2021/05/23
- Se empieza a pedir confirmación para descartar la sesión actual únicamente si hay cambios pendientes de ser guardados.
- Se remplaza la transición de la actividad principal a la actividad de proyecto por la transición por defecto, ya que la que había puesto hace que en algunos dispositivos (al menos en el mío) no se dibuje la actividad correctamente debido a las transparencias. Este es un problema de Android y no de mi propio código y también me ocurre, por ejemplo, en la pantalla de desbloqueo.
- Se implementan las acciones de los botones de eliminar los elementos seleccionados y de dividir el audio seleccionado que se muestran en la pantalla de proyecto. Se hace que estos botones solo sean visibles cuando pueden ser utilizados: ambos solo cuando hay algún elemento seleccionado y el botón de división en caso de que solo se haya seleccionado un elemento. Se hacen completamente funcionales estos botones, creando, en el caso de la acción de división de audio, dos actividades para pedir un punto de ruptura y previsualizar el resultado antes de aceptarlo.
- Se refina el modo de selección para que se termine automáticamente si se deseleccionan todos los elementos, lo que es más amigable para el usuario.
- Se corrige un bug en `WAVEdition.Split` que permitía dividir los datos de un audio en una cantidad no entera de muestras, dados algunos valores específicos para el punto de ruptura y un audio que requiera varios bytes por muestra (por ejemplo, los audios estéreos, ya que cada canal requiere un valor de muestra de al menos un byte). Este bug provocaba que se crease un segmento derecho corrupto o en el que las muestras del canal izquierdo pasasen al derecho y viceversa.
- Se implementa la actividad en la que el usuario trabaja con un segmento de audio. Ofrece reproductores (instancias del componente personalizado `AudioPlayer`) para el segmento de audio original y para el último intento de imitarlo por parte del usuario, además de una grabadora para realizar intentos, un botón de comparación que permite reproducir ambos audios el uno tras el otro en bucle, y campos de texto donde visualizar y editar los metadatos del segmento guardados en el índice del proyecto (título, subtítulos y observaciones). Los distintos componentes se bloquean entre sí temporalmente para evitar que el usuario realice acciones incorrectas: por ejemplo, se corta la reproducción de audios y no se permite utilizar el botón de comparación durante las grabaciones, ni se puede iniciar una grabación durante una comparación.
- Se corrige un bug que provocaba problemas de compartición de recursos al utilizar `AudioRecorder` (el componente grabadora personalizado) varias veces seguidas. Cuando se producía el problema, el flujo del archivo de audio del reproductor conectado a la grabadora quedaba corrupto y, al reproducir la grabación, la actividad colapsaba con señales de error de Android a bajo nivel (no excepciones), ya que la máquina de estado interna de la implementación de Android de `MediaRecorder` en la que `AudioRecorder` se basa entraba en un estado incorrecto.
- Se muestra el título del proyecto en la barra superior de la actividad de proyecto, ya que antes de este cambio no había ningún modo de saber qué proyecto era el que estaba abierto.
