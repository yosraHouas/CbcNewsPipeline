document.addEventListener("DOMContentLoaded", () => {

    const demoAccount = document.getElementById("demoAccount");
    const usernameInput = document.getElementById("usernameInput");
    const passwordInput = document.getElementById("passwordInput");

    const togglePasswordBtn = document.getElementById("togglePassword");
    const togglePasswordIcon = document.getElementById("togglePasswordIcon");

    if (!usernameInput || !passwordInput) return;

    // Focus automatique
    usernameInput.focus();

    // Comptes démo
    const demoAccounts = {
        admin: { username: "admin", password: "Admin123!" },
        user: { username: "user", password: "User123!" }
    };

    if (demoAccount) {
        demoAccount.addEventListener("change", () => {

            const selected = demoAccounts[demoAccount.value];

            if (!selected) return;

            usernameInput.value = selected.username;
            passwordInput.value = selected.password;

        });
    }

    // Afficher / masquer mot de passe
    if (togglePasswordBtn && togglePasswordIcon) {

        togglePasswordBtn.addEventListener("click", () => {

            const isHidden = passwordInput.type === "password";

            passwordInput.type = isHidden ? "text" : "password";
            togglePasswordIcon.textContent = isHidden ? "🙈" : "👁";

        });

    }

});
