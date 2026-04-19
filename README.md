# 📰 CBC News Pipeline

## 📌 Overview

CBC News Pipeline is a backend system inspired by real-world media platforms (e.g. CBC / Radio-Canada).

It demonstrates how to design and build a **scalable, event-driven, and asynchronous data ingestion pipeline** using modern .NET technologies.

The system ingests RSS feeds, processes them asynchronously, stores the data in MongoDB, and provides **real-time monitoring through a dashboard**.

---

## 🧠 Key Concepts

- Asynchronous processing (message-driven architecture)
- Event-driven communication
- Idempotent data ingestion (no duplicates)
- Real-time updates with SignalR
- Fault tolerance with retry mechanism

---

## 🏗️ Architecture

The application is composed of multiple services communicating via RabbitMQ.


[Dashboard] → [API] → [RabbitMQ] → [Worker] → [MongoDB]
↓
[SignalR Hub]


### Components

- **API (ASP.NET Core Minimal API)**
  - Receives ingestion requests
  - Publishes messages to RabbitMQ

- **Worker Service (.NET Background Service)**
  - Consumes messages
  - Processes RSS feeds
  - Stores data in MongoDB

- **RabbitMQ**
  - Handles asynchronous communication

- **MongoDB**
  - Stores news articles and job tracking data

- **Dashboard (ASP.NET Core MVC / Razor Pages)**
  - Displays ingestion status
  - Shows job history
  - Receives real-time updates via SignalR

---

## 🔄 Data Flow

1. User triggers ingestion from Dashboard
2. API publishes `IngestionRequested` event
3. Worker consumes the message
4. RSS feed is processed
5. Stories are upserted into MongoDB
6. Events are emitted:
   - `IngestionStarted`
   - `IngestionCompleted`
   - `IngestionFailed`
7. Dashboard updates in real time via SignalR

---

## ⚙️ Technologies

- .NET 8 / C#
- ASP.NET Core (Minimal API + MVC)
- MongoDB
- RabbitMQ
- SignalR
- Docker (for local infrastructure)

---

## 🚀 Features

- Message-driven ingestion pipeline
- Retry mechanism for resilience
- Idempotent processing (no duplicate data)
- Real-time notifications (SignalR)
- Job tracking (status, inserted/updated counts)
- Clean and modular architecture

---

## 🧪 Error Handling & Retry

The system is designed to handle failures gracefully:

- Automatic retry on ingestion failure
- Failed jobs are tracked with status = **Failed**
- Successful retries update job status to **Completed**

Example:
- Unknown feed → ingestion fails → job marked as Failed
- Retry succeeds → job marked as Completed

---

## 📊 Dashboard

The dashboard provides:

- Live ingestion notifications
- Job history with status (Running, Completed, Failed)
- Real-time updates via SignalR
- Ingestion statistics (inserted / updated)

---

## 🐳 Running the Project

### Option 1 — Docker

```bash
docker compose up -d

Option 2 — Manual setup
Start MongoDB
Start RabbitMQ
Run API
Run Worker
Run Dashboard
📂 Project Structure
cbc-news-pipeline
│
├── docker-compose.yml
├── src
│   ├── Cbc.News.Api
│   ├── Cbc.News.Worker
│   ├── Cbc.News.Dashboard
│   └── Cbc.News.Contracts


🔒 Authentication

Currently implemented using JWT authentication.

Future improvement:

Integration with OAuth2 / OpenID Connect (e.g. Keycloak)
💡 Future Improvements
Dead-letter queue (DLQ)
Monitoring & metrics (Prometheus / Grafana)
Cloud deployment (Azure / containers)
Advanced event logging / audit trail
👩‍💻 Author

Yosra Houas
.NET Backend Developer
Montreal, Canada