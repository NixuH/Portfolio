Ten plik wyjaśnia, jak Visual Studio utworzyło projekt.

Następujące narzędzia zostały użyte do wygenerowania tego projektu:
- create-vite

Następujące kroki zostały użyte do wygenerowania tego projektu:
- Utwórz projekt React z create-vite: `npm init --yes vite@latest taskmanagement.client -- --template=react-ts  --no-rolldown --no-immediate`.
- Zaktualizuj `vite.config.ts`, aby skonfigurować serwer proxy i certyfikaty.
- Dodaj `@type/node` do wpisywania `vite.config.js`.
- Zaktualizuj składnik `App`, aby pobrać i wyświetlić informacje o pogodzie.
- Utwórz plik projektu (`taskmanagement.client.esproj`).
- Utwórz `launch.json`, aby włączyć debugowanie.
- Dodaj projekt do rozwiązania.
- Zaktualizuj punkt końcowy serwera proxy, aby był punktem końcowym serwera zaplecza.
- Dodaj projekt do listy projektów startowych.
- Zapisz ten plik.
