function getAuthHeaders() {
    const token = window.authConfig?.token;

    if (!token) {
        return {};
    }

    return {
        "Authorization": `Bearer ${token}`
    };
}

function getStatusBadge(status) {
    switch (status) {
        case "Pending":
            return `<span class="badge bg-warning text-dark">Pending</span>`;
        case "Running":
            return `<span class="badge bg-info text-dark">Running</span>`;
        case "Completed":
            return `<span class="badge bg-success">Completed</span>`;
        case "Failed":
            return `<span class="badge bg-danger">Failed</span>`;
        default:
            return `<span class="badge bg-secondary">${status ?? "Unknown"}</span>`;
    }
}

function formatDate(date) {
    if (!date) return "";

    return new Date(date).toLocaleString(undefined, {
        dateStyle: "medium",
        timeStyle: "short"
    });
}

async function loadJobs() {
    try {
        const baseUrl = new URL(window.dashboardConfig.jobsUrl);

        const response = await fetch(baseUrl.toString(), {
            headers: getAuthHeaders()
        });

        if (!response.ok) {
            throw new Error(`Erreur API jobs: ${response.status}`);
        }

        const data = await response.json();

        const table = document.getElementById("jobsTable");
        if (!table) return;

        const items = data.items ?? [];

        table.innerHTML = "";

        if (items.length === 0) {
            table.innerHTML = `
                <tr>
                    <td colspan="7" class="text-center text-muted py-4">
                        No jobs yet.
                    </td>
                </tr>`;
            return;
        }

        items.forEach(job => {
            const row = document.createElement("tr");

            row.innerHTML = `
                <td>${job.id ?? ""}</td>
                <td>${job.feed ?? ""}</td>
                <td>${getStatusBadge(job.status)}</td>
                <td>${job.storiesInserted ?? 0}</td>
                <td>${job.storiesUpdated ?? 0}</td>
                <td>${formatDate(job.startedAtUtc)}</td>
                <td>${formatDate(job.finishedAtUtc)}</td>`;

            row.classList.add("new-row");
            table.appendChild(row);
        });
    } catch (error) {
        console.error("Erreur chargement jobs:", error);

        const table = document.getElementById("jobsTable");

        if (table) {
            table.innerHTML = `
                <tr>
                    <td colspan="7" class="text-danger text-center py-4">
                        Impossible de charger les jobs
                    </td>
                </tr>`;
        }
    }
}

loadJobs();
setInterval(loadJobs, 3000);