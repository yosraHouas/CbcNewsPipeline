(function () {
    const badgeElement = document.getElementById("notificationsBadge");
    if (!badgeElement || !window.signalR || !window.dashboardConfig) return;

    let notificationsCount = 0;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(window.dashboardConfig.notificationsHubUrl)
        .withAutomaticReconnect()
        .build();

    function updateBadge() {
        if (notificationsCount > 0) {
            badgeElement.style.display = "inline-block";
            badgeElement.textContent = notificationsCount;
        } else {
            badgeElement.style.display = "none";
        }
    }

    connection.on("ingestionRequested", () => {
        notificationsCount++;
        updateBadge();
    });

    connection.start().catch(err => {
        console.error("Erreur connexion SignalR navbar:", err);
    });

    window.resetNotificationsBadge = function () {
        notificationsCount = 0;
        updateBadge();
    };


})();