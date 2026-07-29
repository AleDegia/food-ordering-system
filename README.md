# 🍽️ Gestione Ristoranti Web

![Login](./loginRisto.png)

Applicazione web full-stack sviluppata come progetto personale per
approfondire lo sviluppo di applicazioni client-server con **ASP.NET
Core Web API**, **React** ed **Entity Framework Core**.

Il progetto consente la gestione di ristoranti e prenotazioni attraverso
un backend REST e un frontend React.

> 🚧 **Progetto attualmente in sviluppo.**

------------------------------------------------------------------------

# Tecnologie utilizzate

## Backend

-   ASP.NET Core Web API
-   Entity Framework Core
-   SQL Server
-   C#
-   Dependency Injection
-   Repository Pattern
-   Session Authentication

## Frontend

-   React
-   JavaScript
-   HTML5
-   CSS3
-   Vite

------------------------------------------------------------------------

# Funzionalità implementate

## Utenti

-   Registrazione
-   Login
-   Gestione sessione

## Ristoranti

-   Creazione ristorante
-   Modifica
-   Eliminazione
-   Visualizzazione elenco
-   Upload immagine
-   Associazione proprietario

## Prenotazioni

-   Creazione prenotazioni
-   Visualizzazione prenotazioni utente

## Dashboard

-   Statistiche generali
-   Gestione dati principali

------------------------------------------------------------------------

# Architettura

Il backend è organizzato secondo una struttura a livelli.

``` text
Domain
│
├── Entities
├── Interfaces
└── Services

Infrastructure
│
├── DbContext
├── Repositories
└── Dependency Injection

Web API
│
├── Controllers
├── Program.cs
└── Configuration

ClientApp
│
├── React
├── Components
├── Pages
└── Services
```

------------------------------------------------------------------------

# Database

Il progetto utilizza **SQL Server** tramite Entity Framework Core.

Le entità principali sono:

-   Utente
-   Ristorante
-   Prenotazione
-   Tipologia

------------------------------------------------------------------------

# Installazione

## 1. Clonare il repository

``` bash
git clone https://github.com/AleDegia/c-OreilyExcercises.git
```

## 2. Configurare il database

Modificare la connection string in `appsettings.json`.

Esempio:

``` json
"ConnectionStrings": {
  "GestioneRistorantiConnectionString": "Server=YOUR_SERVER;Database=GestioneRistorantiDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

## 3. Applicare le migrazioni

``` bash
dotnet ef database update
```

## 4. Avviare il backend

``` bash
dotnet run
```

## 5. Avviare il frontend

Entrare nella cartella del client:

``` bash
cd ClientApp
```

Installare le dipendenze:

``` bash
npm install
```

Avviare React:

``` bash
npm run dev
```

------------------------------------------------------------------------

# Obiettivi del progetto

Questo progetto nasce con l'obiettivo di approfondire:

-   Sviluppo di API REST con ASP.NET Core
-   Entity Framework Core
-   Architetture multilayer
-   Repository Pattern
-   Dependency Injection
-   Sviluppo frontend con React
-   Integrazione frontend/backend

------------------------------------------------------------------------

# Miglioramenti previsti

-   Password hashing
-   Autenticazione tramite JWT o ASP.NET Identity
-   DTO e AutoMapper
-   Validazioni avanzate
-   Gestione dei tavoli
-   Unit Test
-   Docker
-   CI/CD
-   Deploy su Azure
