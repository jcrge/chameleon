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
