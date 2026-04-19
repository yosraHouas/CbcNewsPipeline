let jobsChartInstance = null;

function getAuthHeaders() {
    const token = window.authConfig?.token;

    if (!token) {
        return {};
    }

    return {
        "Authorization": `Bearer ${token}`
    };
}
function formatChartLabel(date) {
    if (!date) return "";

    return new Date(date).toLocaleTimeString([], {
        hour: "2-digit",
        minute: "2-digit"
    });
}

function hasAdminWidgets() {
    return !!document.getElementById("jobsCount")
        || !!document.getElementById("jobsChart")
        || !!document.getElementById("startIngestionBtn");
}

// Charge les statistiques affichées sur la home
async function loadHomeStats() {
    try {
        const storiesResponse = await fetch(window.dashboardConfig.storiesUrl, {
            headers: getAuthHeaders()
        })
        if (!storiesResponse.ok) {
            throw new Error("Erreur API stories");
        }

        const storiesData = await storiesResponse.json();

        const storiesCount = document.getElementById("storiesCount");
        if (storiesCount) {
            storiesCount.textContent = storiesData.count ?? 0;
        }

        // Charger les jobs seulement si les widgets admin existent
        if (hasAdminWidgets()) {
            const jobsResponse = await fetch(window.dashboardConfig.jobsUrl, {
                headers: getAuthHeaders()
            });
            if (!jobsResponse.ok) {
                throw new Error("Erreur API jobs");
            }

            const jobsData = await jobsResponse.json();

            const jobsCount = document.getElementById("jobsCount");
            if (jobsCount) {
                jobsCount.textContent = jobsData.count ?? 0;
            }

            renderJobsChart(jobsData.items ?? []);
        }
    } catch (error) {
        console.error("Erreur chargement stats home:", error);
    }
}

// Construit le graphique Chart.js
function renderJobsChart(jobs) {
    const canvas = document.getElementById("jobsChart");
    if (!canvas || typeof Chart === "undefined") return;

    const labels = [...jobs]
        .reverse()
        .map(job => formatChartLabel(job.startedAtUtc));

    const insertedValues = [...jobs]
        .reverse()
        .map(job => job.storiesInserted ?? 0);

    if (jobsChartInstance) {
        jobsChartInstance.destroy();
    }

    jobsChartInstance = new Chart(canvas, {
        type: "bar",
        data: {
            labels: labels,
            datasets: [
                {
                    label: "Stories Inserted",
                    data: insertedValues
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    }
                }
            }
        }
    });
}

// Lance une ingestion via l'API
async function startIngestion() {
    const button = document.getElementById("startIngestionBtn");
    const feedSelect = document.getElementById("feedSelect");
    const message = document.getElementById("ingestionMessage");

    if (!button || !feedSelect || !message) return;

    const feed = feedSelect.value;

    try {
        button.disabled = true;
        button.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span>Starting...`;
        message.innerHTML = "";

        const response = await fetch(`${window.dashboardConfig.ingestionUrl}?feed=${encodeURIComponent(feed)}`, {
            method: "POST",
            headers: getAuthHeaders()
        });

        if (!response.ok) {
            throw new Error("Erreur lors du démarrage de l’ingestion");
        }

        await response.json();

        message.innerHTML = `<span class="text-success">Ingestion started for "${feed}"</span>`;

        loadHomeStats();
    } catch (error) {
        console.error("Erreur start ingestion:", error);
        message.innerHTML = `<span class="text-danger">Impossible de démarrer l’ingestion</span>`;
    } finally {
        button.disabled = false;
        button.innerHTML = `<i class="bi bi-cloud-arrow-down me-2"></i>Start ingestion`;
    }
}

// Initialise la connexion SignalR
const signalrHomeStatus = document.getElementById("signalrHomeStatus");

const homeConnection = new signalR.HubConnectionBuilder()
    .withUrl(window.dashboardConfig.notificationsHubUrl)
    .withAutomaticReconnect()
    .build();

homeConnection.start()
    .then(() => {
        if (signalrHomeStatus) {
            signalrHomeStatus.innerHTML = `<span class="badge bg-success bg-opacity-75 rounded-pill px-3 py-2">Connected</span>`;
        }
    })
    .catch(err => {
        console.error(err);
        if (signalrHomeStatus) {
            signalrHomeStatus.innerHTML = `<span class="badge bg-danger rounded-pill px-3 py-2">Offline</span>`;
        }
    });

homeConnection.on("ingestionRequested", () => {
    loadHomeStats();
});

// Initialise le bouton Start Ingestion
const startIngestionBtn = document.getElementById("startIngestionBtn");
if (startIngestionBtn) {
    startIngestionBtn.addEventListener("click", startIngestion);
}

// Chargement initial + refresh périodique
loadHomeStats();
setInterval(loadHomeStats, 5000);

async function loadPipelineStatus() {

    try {

        const response = await fetch(
            window.dashboardConfig.apiBaseUrl + "/pipeline/status",
            { headers: getAuthHeaders() }
        );

        if (!response.ok) return;

        const data = await response.json();

        document.getElementById("lastFeed").innerText =
            data.lastFeed ?? "-";

        document.getElementById("storiesProcessed").innerText =
            (data.storiesInserted ?? 0) + (data.storiesUpdated ?? 0);

        document.getElementById("lastRun").innerText =
            data.lastRun
                ? new Date(data.lastRun).toLocaleString()
                : "-";

    } catch (err) {
        console.error("Pipeline status error", err);
    }
}

loadPipelineStatus();
setInterval(loadPipelineStatus, 5000);