function getAuthHeaders() {
    const token = window.authConfig?.token;

    if (!token) {
        return {};
    }

    return {
        "Authorization": `Bearer ${token}`
    };
}

const notificationsConnection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7263/hubs/notifications")
    .withAutomaticReconnect()
    .build();

notificationsConnection.on("ingestionRequested", function (payload) {
    const feedName = getFeedName(payload);

    const eventItem = buildNotificationItem(
        "info",
        "Ingestion requested",
        feedName
            ? `Ingestion requested for feed '${feedName}'.`
            : "Ingestion requested."
    );

    showToast(eventItem.type, eventItem.title, eventItem.message);
    appendNotificationHistory(eventItem);
});

notificationsConnection.on("ingestionStarted", function (payload) {
    const feedName = getFeedName(payload);

    const eventItem = buildNotificationItem(
        "info",
        "Ingestion started",
        feedName
            ? `Ingestion started for feed '${feedName}'.`
            : "Ingestion started."
    );

    showToast(eventItem.type, eventItem.title, eventItem.message);
    appendNotificationHistory(eventItem);
});

notificationsConnection.on("ingestionCompleted", function (payload) {
    const feedName = getFeedName(payload);
    const storiesCount = getStoriesCount(payload);

    let message = "Ingestion completed successfully.";

    if (feedName && storiesCount != null) {
        message = `Ingestion completed for feed '${feedName}' with ${storiesCount} stories.`;
    } else if (feedName) {
        message = `Ingestion completed for feed '${feedName}'.`;
    }

    const eventItem = buildNotificationItem(
        "success",
        "Ingestion complete",
        message
    );

    showToast(eventItem.type, eventItem.title, eventItem.message);
    appendNotificationHistory(eventItem);
});

notificationsConnection.on("ingestionFailed", function (payload) {
    const feedName = getFeedName(payload);
    const error = getErrorMessage(payload);

    let message = "Ingestion failed.";

    if (feedName && error) {
        message = `Feed '${feedName}' failed. Error: ${error}`;
    } else if (feedName) {
        message = `Ingestion failed for feed '${feedName}'.`;
    } else if (error) {
        message = `Ingestion failed. Error: ${error}`;
    }

    const eventItem = buildNotificationItem(
        "error",
        "Ingestion failed",
        message
    );

    showToast(eventItem.type, eventItem.title, eventItem.message);
    appendNotificationHistory(eventItem);
});

async function startSignalR() {
    try {
        await notificationsConnection.start();
        console.log("Notifications SignalR connected");
        updateSignalRStatus(true);
    } catch (err) {
        console.error("SignalR connection error:", err);
        updateSignalRStatus(false);
        setTimeout(startSignalR, 5000);
    }
}

notificationsConnection.onreconnecting(() => {
    updateSignalRStatus(false);
});

notificationsConnection.onreconnected(() => {
    updateSignalRStatus(true);
});

notificationsConnection.onclose(() => {
    updateSignalRStatus(false);
});

document.addEventListener("DOMContentLoaded", async function () {
    const clearButton = document.getElementById("clearNotificationHistoryBtn");

    if (clearButton) {
        clearButton.addEventListener("click", function () {
            const container = document.getElementById("notificationHistory");
            if (!container) return;

            container.innerHTML = `<div class="list-group-item text-muted">No events yet.</div>`;
            localStorage.removeItem("cbc-notification-history");
        });
    }

    await loadNotificationsFromApi();
    await startSignalR();
});

async function loadNotificationsFromApi() {
    const container = document.getElementById("notificationHistory");
    if (!container) return;

    try {
        const response = await fetch("https://localhost:7263/api/notifications?limit=20", {
            headers: getAuthHeaders()
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const items = await response.json();

        if (!items || !items.length) {
            container.innerHTML = `<div class="list-group-item text-muted">No events yet.</div>`;
            return;
        }

        container.innerHTML = "";

        for (const item of items) {
            renderNotificationItem(container, mapApiNotification(item), false);
        }

        saveNotificationsToStorage(items.map(mapApiNotification));
    } catch (err) {
        console.error("Failed to load notifications from API:", err);
        loadNotificationHistoryFromStorage();
    }
}

function mapApiNotification(item) {
    return {
        type: item.type || "info",
        title: item.title || "Notification",
        message: item.message || "",
        timestamp: formatTime(item.createdAtUtc)
    };
}

function buildNotificationItem(type, title, message) {
    return {
        type,
        title,
        message,
        timestamp: new Date().toLocaleTimeString()
    };
}

function appendNotificationHistory(eventItem) {
    const container = document.getElementById("notificationHistory");
    if (!container) return;

    saveNotificationToStorage(eventItem);
    renderNotificationItem(container, eventItem, true);
}

function renderNotificationItem(container, eventItem, prepend = false) {
    const emptyState = container.querySelector(".text-muted");
    if (emptyState) {
        emptyState.remove();
    }

    const item = document.createElement("div");
    item.className = "list-group-item";

    item.innerHTML = `
        <div class="d-flex justify-content-between align-items-start">
            <div class="me-3">
                <div class="fw-semibold">
                    <span class="badge ${getBadgeColor(eventItem.type)} me-2">${escapeHtml((eventItem.type || "info").toUpperCase())}</span>
                    ${escapeHtml(eventItem.title)}
                </div>
                <div class="small text-muted mt-1">${escapeHtml(eventItem.message)}</div>
            </div>
            <small class="text-muted">${escapeHtml(eventItem.timestamp)}</small>
        </div>
    `;

    if (prepend) {
        container.insertBefore(item, container.firstChild);
    } else {
        container.appendChild(item);
    }

    const maxItems = 20;
    while (container.children.length > maxItems) {
        container.removeChild(container.lastChild);
    }
}

function saveNotificationToStorage(eventItem) {
    const key = "cbc-notification-history";
    const items = JSON.parse(localStorage.getItem(key) || "[]");

    items.unshift(eventItem);

    const trimmed = items.slice(0, 20);
    localStorage.setItem(key, JSON.stringify(trimmed));
}

function saveNotificationsToStorage(items) {
    localStorage.setItem("cbc-notification-history", JSON.stringify(items.slice(0, 20)));
}

function loadNotificationHistoryFromStorage() {
    const container = document.getElementById("notificationHistory");
    if (!container) return;

    const key = "cbc-notification-history";
    const items = JSON.parse(localStorage.getItem(key) || "[]");

    if (!items.length) {
        container.innerHTML = `<div class="list-group-item text-muted">No events yet.</div>`;
        return;
    }

    container.innerHTML = "";

    for (const item of items) {
        renderNotificationItem(container, item, false);
    }
}

function updateSignalRStatus(isConnected) {
    const status = document.getElementById("signalrStatus");
    if (!status) return;

    if (isConnected) {
        status.textContent = "Connected";
        status.className = "badge text-bg-success";
    } else {
        status.textContent = "Disconnected";
        status.className = "badge text-bg-danger";
    }
}

function formatTime(value) {
    if (!value) return "";

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";

    return date.toLocaleTimeString();
}

function showToast(type, title, message) {
    const container = document.getElementById("toastContainer");
    if (!container) return;

    const bg = getToastColor(type);
    const id = "toast-" + Date.now();

    const html = `
        <div id="${id}" class="toast align-items-center text-white ${bg} border-0 mb-2" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <strong>${escapeHtml(title)}</strong><br/>
                    ${escapeHtml(message)}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML("beforeend", html);

    const toastElement = document.getElementById(id);
    const toast = new bootstrap.Toast(toastElement, { delay: 5000 });

    toast.show();

    toastElement.addEventListener("hidden.bs.toast", () => {
        toastElement.remove();
    });
}

function getToastColor(type) {
    switch (type) {
        case "success":
            return "bg-success";
        case "error":
            return "bg-danger";
        case "warning":
            return "bg-warning text-dark";
        default:
            return "bg-primary";
    }
}

function getBadgeColor(type) {
    switch (type) {
        case "success":
            return "text-bg-success";
        case "error":
            return "text-bg-danger";
        case "warning":
            return "text-bg-warning";
        default:
            return "text-bg-primary";
    }
}

function escapeHtml(value) {
    if (!value) return "";

    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function getFeedName(payload) {
    return payload?.feedName
        || payload?.FeedName
        || payload?.feed
        || payload?.Feed
        || "";
}

function getStoriesCount(payload) {
    return payload?.storiesCount
        ?? payload?.StoriesCount
        ?? payload?.storiesInserted
        ?? payload?.StoriesInserted
        ?? null;
}

function getErrorMessage(payload) {
    return payload?.error
        || payload?.Error
        || "";
}