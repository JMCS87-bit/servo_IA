
#include <WiFi.h>
#include <WiFiUdp.h>
#include <ESP32Servo.h>
#include "config.h"

// ---------------- CONFIGURACION WIFI ----------------



WiFiUDP udp;

const uint16_t UDP_PORT = 4210;

// ---------------- CONFIGURACION SERVO ----------------

Servo servo;

const int SERVO_PIN = 18;

const int ANGULO_MIN = 5;
const int ANGULO_MAX = 180;

int posicionActual = 90;
int posicionObjetivo = 90;

float velocidad = 90.0; // Grados por segundo

bool servoActivo = true;
bool enMovimiento = false;

unsigned long tiempoAnterior = 0;

// ---------------- PROTOTIPOS ----------------

void procesarComando(String comando);
void moverServo();
void responderEstado();

// ---------------- SETUP ----------------

void setup() {

  Serial.begin(115200);

  // Conectar WiFi

  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  Serial.print("Conectando WiFi");

  while (WiFi.status() != WL_CONNECTED) {
    delay(300);
    Serial.print(".");
  }

  Serial.println("\nWiFi conectado");
  Serial.println(WiFi.localIP());

  // Inicializar servo

  servo.setPeriodHertz(50);

  servo.attach(SERVO_PIN, 500, 2400);

  servo.write(posicionActual);

  // Iniciar UDP

  udp.begin(UDP_PORT);

  Serial.println("Controlador SG90 iniciado");

}

// ---------------- LOOP ----------------

void loop() {

  // Recibir comandos UDP

  int packetSize = udp.parsePacket();

  if (packetSize) {

    char buffer[256];

    int len = udp.read(buffer, sizeof(buffer) - 1);

    if (len > 0) {

      buffer[len] = '\0';

      String comando = String(buffer);

      comando.trim();

      Serial.println("Comando: " + comando);

      procesarComando(comando);

    }

  }

  // Actualizar movimiento

  moverServo();

}

// ---------------- PROCESAR COMANDOS ----------------

void procesarComando(String comando) {

  // MOVE:angulo:velocidad

  if (comando.startsWith("MOVE:")) {

    int separador = comando.indexOf(':', 5);

    if (separador < 0) return;

    int angulo = comando.substring(5, separador).toInt();

    float nuevaVelocidad =
      comando.substring(separador + 1).toFloat();

    if (angulo < ANGULO_MIN || angulo > ANGULO_MAX) {
      return;
    }

    if (nuevaVelocidad <= 0 || nuevaVelocidad > 6000) {
      return;
    }

    if (!servoActivo) {

      servo.attach(SERVO_PIN, 500, 2400);

      servoActivo = true;

    }

    posicionObjetivo = angulo;

    velocidad = nuevaVelocidad;

    enMovimiento = posicionActual != posicionObjetivo;

    tiempoAnterior = millis();

    responderEstado();

  }

  // STOP

  else if (comando == "STOP") {

    posicionObjetivo = posicionActual;

    enMovimiento = false;

    responderEstado();

  }

  // HOLD

  else if (comando == "HOLD") {

    if (!servoActivo) {

      servo.attach(SERVO_PIN, 500, 2400);

      servoActivo = true;

    }

    servo.write(posicionActual);

    enMovimiento = false;

    responderEstado();

  }

  // RELEASE

  else if (comando == "RELEASE") {

    servo.detach();

    servoActivo = false;

    enMovimiento = false;

    responderEstado();

  }

  // GET_STATUS

  else if (comando == "GET_STATUS") {

    responderEstado();

  }

}

// ---------------- CONTROL DE MOVIMIENTO ----------------

void moverServo() {

  if (!enMovimiento || !servoActivo) return;

  unsigned long ahora = millis();

  unsigned long intervalo = ahora - tiempoAnterior;

  if (intervalo < 20) return;

  tiempoAnterior = ahora;

  float incremento = velocidad * (intervalo / 1000.0);

  int diferencia = posicionObjetivo - posicionActual;

  if (abs(diferencia) <= incremento) {

    posicionActual = posicionObjetivo;

    enMovimiento = false;

  }

  else if (diferencia > 0) {

    posicionActual += max(1, (int)incremento);

  }

  else {

    posicionActual -= max(1, (int)incremento);

  }

  servo.write(posicionActual);

  if (!enMovimiento) {

    responderEstado();

  }

}

// ---------------- RESPUESTA ----------------

void responderEstado() {

  String respuesta =

    "POS:" + String(posicionActual) +

    ";TARGET:" + String(posicionObjetivo) +

    ";MOVING:" + String(enMovimiento ? 1 : 0) +

    ";ACTIVE:" + String(servoActivo ? 1 : 0);

  udp.beginPacket(udp.remoteIP(), udp.remotePort());

  udp.print(respuesta);

  udp.endPacket();

}
