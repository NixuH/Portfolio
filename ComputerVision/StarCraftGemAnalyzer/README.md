## Opis projektu
- Projekt to narzędzie do automatycznej analizy planszy mini-gry „Gwiezdne klejnoty” dostępnej w StarCraft II (Salon Gier). Jego zadaniem było rozpoznawanie i klasyfikowanie klejnotów na planszy w grze typu match-3 (połącz trzy).

- Był to mój pierwszy projekt wykorzystujący rozpoznawanie obrazu. Połączyłem w nim analizę obrazu z bezpośrednią interakcją z grą. System przechwytywał obraz, wykrywał elementy planszy za pomocą dopasowywania wzorców w OpenCV, a następnie odtwarzał jej aktualny stan w formie danych możliwych do dalszego przetwarzania.

- Moduł analizy planszy był częścią większego skryptu automatyzującego rozgrywkę. Początkowa wersja systemu podejmowania decyzji była oparta na regułach i analizie możliwych ruchów, z planem późniejszego rozwinięcia systemu o bardziej zaawansowane metody podejmowania decyzji.

## Technologie
- Python
- OpenCV
- NumPy
- PyAutoGUI

## Jak działa
1. Przechwycenie obrazu planszy.
2. Wykrycie klejnotów za pomocą template matching.
3. Zapisanie stanu planszy w strukturze danych.
4. Przekazanie danych do dalszej analizy.