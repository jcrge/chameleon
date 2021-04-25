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
