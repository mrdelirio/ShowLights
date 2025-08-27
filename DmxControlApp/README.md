# DmxControlApp – Controllo DMX per set live (Blazor Web + ASP.NET Core)

Applicazione web per controllo luci DMX via Art‑Net, con gestione multi‑universo, patch canali, scene e cue, generazione preset con AI (open source via Ollama o OpenAI) e modalità live reattiva all’audio. Include stub per Bluetooth LE via Web Bluetooth.

## Funzionalità principali
- Canali DMX: fader per canali, invio frame Art‑Net, multi‑universo
- Patch: etichette e numero canali attivi per universo, salvataggio stato
- Scene: creazione/applicazione scene per universo, generazione con AI
- Cues: lista cue con fade/hold, loop, speed, crossfade per canale
- Live: modalità audio‑reactive (JS + WebAudio) con invio DMX
- Bluetooth: stub discovery/connessione con Web Bluetooth (browser compatibili)
- Settings: configurazione Art‑Net Target IP e provider AI, modelli Ollama
- Stato: export/import JSON di Scenes/Patch/Cues

## Stack
- Server: ASP.NET Core 8 (Minimal APIs)
- Client: Blazor Web App (.NET 8) con interattività WebAssembly e Bootstrap
- AI: Ollama (open source) o OpenAI (opzionale)
- Rete: Art‑Net (UDP 6454)

## Requisiti
- .NET SDK 8.0+
- Rete locale con dispositivi Art‑Net (o simulatore) raggiungibili
- Per AI open source: Ollama in esecuzione locale (`ollama serve`)
- Per Web Bluetooth: browser Chromium con HTTPS o `localhost`

## Setup rapido
```bash
# (Facoltativo) Installazione locale .NET nello workspace
# curl -fsSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh && \
# chmod +x dotnet-install.sh && ./dotnet-install.sh --channel 8.0 --install-dir ~/.dotnet
# export PATH="$HOME/.dotnet:$PATH"

cd DmxControlApp
# Ripristino e build
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build
```

## Avvio
```bash
# Avvio del server ASP.NET (hosta anche il client Blazor)
cd DmxControlApp/DmxControlApp
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet run --urls http://0.0.0.0:5080
# Apri http://localhost:5080 nel browser
```

Suggerimento: usa `https://localhost:xxxx` per funzionalità Web Bluetooth (richiede contesto sicuro). Puoi avviare con HTTPS se il certificato dev è attendibile sul sistema.

## Configurazione (da UI)
- Vai a Settings:
  - Art‑Net Target IP: IP broadcast/rete del nodo Art‑Net (es. `255.255.255.255` o IP specifico)
  - AI Provider: `Ollama` (consigliato, open source) o `OpenAI`
  - Ollama: BaseUrl (es. `http://localhost:11434`), carica e seleziona modello (es. `mistral`)
  - OpenAI: BaseUrl, Model e API Key (se usi OpenAI)
- Patch: imposta canali attivi e etichette per universo, salva

## AI open source (Ollama)
```bash
ollama serve
ollama pull mistral  # o altro modello (llama3, qwen, ecc.)
```
In Settings seleziona Provider `Ollama`, imposta BaseUrl e clicca "Load Models" per scegliere il modello.

## AI OpenAI (opzionale)
```bash
export OPENAI_API_KEY=sk-...
# In Settings seleziona Provider OpenAI e (se vuoi) imposta BaseUrl/Model
```

## Flusso d’uso (UI)
1) Patch: configura universe e canali/etichette, salva
2) Channels: imposta universe/canali, muovi fader, invia frame
3) Scenes: crea scene manualmente o con "Generate with AI" (per l’universo attivo)
4) Cues: carica scene in cue, imposta fade/hold/loop/speed, Play/Stop/Next/Prev
5) Live: avvia microfono per modalità audio‑reactive (stub), invia DMX
6) Settings: gestisci IP Art‑Net, provider AI, export/import stato JSON

## Test rapido (API)
- Invio DMX (Art‑Net):
```bash
curl -X POST http://localhost:5080/api/artnet/send \
  -H 'Content-Type: application/json' \
  -d '{"universe":0, "values":[255,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]}'
```
- Preset AI:
```bash
curl -X POST http://localhost:5080/api/ai/preset \
  -H 'Content-Type: application/json' \
  -d '{"prompt":"warm wash", "channels":16}'
```
- Settings (GET/POST):
```bash
curl http://localhost:5080/api/settings
curl -X POST http://localhost:5080/api/settings -H 'Content-Type: application/json' \
  -d '{"ArtNet":{"TargetIp":"255.255.255.255"},"AI":{"Provider":"Ollama","OpenAI":{"BaseUrl":"https://api.openai.com/v1","Model":"gpt-4o-mini","ApiKey":""},"Ollama":{"BaseUrl":"http://localhost:11434","Model":"mistral"}}}'
```
- Stato (Scenes/Patch/Cues):
```bash
curl http://localhost:5080/api/state
curl -X POST http://localhost:5080/api/state -H 'Content-Type: application/json' -d @state.json
curl -OJ http://localhost:5080/api/state/export
curl -X POST http://localhost:5080/api/state/import --data-binary @state.json -H 'Content-Type: application/json'
```
- Modelli Ollama:
```bash
curl http://localhost:5080/api/ai/ollama/models
```

## Persistenza
- Config e stato vengono salvati in `DmxControlApp/DmxControlApp/data/`:
  - `settings.json` (sanitizzato, la chiave OpenAI non viene salvata)
  - `state.json` (Scenes/Patch/Cues)

## Struttura progetto (principale)
- `DmxControlApp/` soluzione
  - `DmxControlApp/` server ASP.NET Core
    - `Services/` ArtNet, AI, Settings, State
    - `Models/` DmxFrame, State models
    - `Components/` App/Layout/Pages (server shell)
    - `wwwroot/app.js` (audio + Web Bluetooth)
  - `DmxControlApp.Client/` client Blazor
    - `Pages/` Channels, Scenes, Live, Bluetooth, Settings, Patch, Cues

## Note e troubleshooting
- Art‑Net: assicurati che firewall e rete permettano UDP 6454 verso il nodo
- Web Bluetooth: usa Chrome/Edge su `https://localhost` (o contesto sicuro)
- AI: se Ollama non risponde, verifica `ollama serve` e il modello scaricato
- Se l’audio non parte, consenti i permessi microfono al browser

## Licenze
- L’app usa provider AI open source via Ollama per default. Verifica le licenze dei modelli che utilizzi (es. Mistral). OpenAI è opzionale.

---
Per estensioni future: MIDI in/out, timeline editor, fixture library, effetto chaser.