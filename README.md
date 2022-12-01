# LEDMatrixVideoPlayer

This solution plays video on LED Matrix connected to Arduino (or other Arduino compatible boards like ESP32).

## LEDMatrixVideoPlayer project

This app will split the video into bitmap and sends out RGB byte data via USB serial.

## ArduinoSketch: led_matrix_video_player.ino

This sketch will decode bitmap data into LED Matrix data array and pass it to FastLED library.
