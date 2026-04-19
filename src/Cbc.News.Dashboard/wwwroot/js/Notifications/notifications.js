const statusElement = document.getElementById("signalrStatus");
const notificationsElement = document.getElementById("notifications");

const connection = new signalR.HubConnectionBuilder()
    .withUrl(window.dashboardConfig.notificationsHubUrl)
    .withAutomaticReconnect()
    .build();

connection.on("ingestionRequested", message => {
    if (!notificationsElement) return;

    const li = document.createElement("li");
    li.className = "list-group-item list-group-item-warning fw-semibold";
    li.textContent = `Ingestion requested → feed=${message.feed} jobId=${message.jobId}`;

    notificationsElement.prepend(li);

    setTimeout(() => {
        li.className = "list-group-item";
    }, 2500);
});

connection.start()
    .then(() => {
        if (statusElement) {
            statusElement.innerHTML = `<span class="badge bg-success">Connected</span>`;
        }
    })
    .catch(err => {
        console.error(err);
        if (statusElement) {
            statusElement.innerHTML = `<span class="badge bg-danger">Connection failed</span>`;
        }
    });