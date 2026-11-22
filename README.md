\# Control de Servomotor con IA



Proyecto para controlar un servomotor SG90 mediante instrucciones en lenguaje natural, utilizando inteligencia artificial Gemini, C# y un ESP32.



El sistema permite interpretar instrucciones escritas, convertirlas en secuencias de movimientos y ejecutarlas mediante comunicación UDP.



\## Arquitectura del proyecto



El proyecto está compuesto por dos sistemas:



\- \*\*C# (.NET 10):\*\* recibe las instrucciones del usuario, consulta a Gemini y genera las acciones necesarias.

\- \*\*ESP32:\*\* recibe los comandos mediante UDP y controla el servomotor SG90.



\## Estructura



```text

servomotor\_ia/

│

├── PruebaGemini/

│   ├── Program.cs

│   ├── ControlServo.csproj

│   ├── .env.example

│   └── ...

│

├── servo\_con\_ia/

│   ├── servo\_con\_ia.ino

│   └── config.example.h

│

├── .gitignore

└── README.md

```



\## Componentes



\- ESP32 WROOM.

\- Servomotor SG90.

\- Fuente externa de 5 V para el servomotor.

\- Computadora con Windows.

\- Conexión Wi-Fi.



\## Conexiones ESP32



| Componente | ESP32 |

|---|---|

| Señal del servo | GPIO 18 |

| Alimentación servo | 5 V externa |

| GND servo | GND común |



\*\*Importante:\*\* utilizar una fuente externa de 5 V para alimentar el servomotor y conectar su GND con el GND del ESP32.



\## Tecnologías utilizadas



\### Software



\- C# y .NET 10.

\- Google Gemini API.

\- ESP32 Arduino Core.

\- Biblioteca ESP32Servo.

\- Comunicación UDP.

\- DotNetEnv.



\## Configuración del ESP32



1\. Abrir la carpeta `servo\_con\_ia`.

2\. Copiar `config.example.h`.

3\. Renombrar la copia como `config.h`.

4\. Configurar las credenciales Wi-Fi:



```cpp

\#define WIFI\_SSID "NOMBRE\_DE\_TU\_WIFI"

\#define WIFI\_PASSWORD "TU\_CONTRASENA"

```



5\. Abrir `servo\_con\_ia.ino` en Arduino IDE.

6\. Instalar la biblioteca ESP32Servo.

7\. Seleccionar la placa ESP32 correspondiente.

8\. Compilar y cargar el firmware.



\## Configuración de C#



Dentro de `PruebaGemini`:



1\. Copiar `.env.example`.

2\. Crear una copia llamada `.env`.

3\. Configurar los siguientes valores:



```env

GEMINI\_API\_KEY=TU\_API\_KEY\_DE\_GEMINI

ESP32\_IP=192.168.1.100

UDP\_PORT=4210

```



La API Key se obtiene desde Google AI Studio.



\## Instalación y ejecución



Requisitos:



\- .NET 10 SDK.

\- Visual Studio o Visual Studio Code (opcional).

\- Arduino IDE.

\- API Key de Gemini.



Abrir una terminal en la carpeta `PruebaGemini` y ejecutar:



```bash

dotnet restore

dotnet run

```



\## Comunicación UDP



El sistema utiliza el puerto UDP 4210.



Comandos principales:



| Comando | Función |

|---|---|

| MOVE:angulo:velocidad | Mover el servo |

| STOP | Detener el movimiento |

| HOLD | Mantener posición |

| RELEASE | Desactivar el servo |

| GET\_STATUS | Consultar estado |



Ejemplo:



```text

MOVE:90:100

```



Mueve el servomotor hacia 90 grados con una velocidad configurada de 100 grados por segundo.



\## Parámetros de movimiento



\- Ángulo permitido en C#: 5° a 175°.

\- Velocidad configurada en C#: 1 a 600 °/s.



La velocidad representa la velocidad solicitada por el controlador. La velocidad física real depende de las características del servomotor y de las condiciones de alimentación.



\## Seguridad



Las credenciales privadas se almacenan en archivos separados:



\- `PruebaGemini/.env`

\- `servo\_con\_ia/config.h`



Estos archivos están excluidos mediante `.gitignore`.



Nunca publiques tu API Key ni las credenciales de tu red Wi-Fi.



\## Estado del proyecto



Versión inicial funcional.



Actualmente permite enviar instrucciones en lenguaje natural a Gemini y ejecutar secuencias de movimientos mediante C# y ESP32.



\## Autor



Proyecto de experimentación con inteligencia artificial, electrónica y programación.

