document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("loginForm");
    const usernameInput = document.getElementById("username");
    const passwordInput = document.getElementById("password");
    const usernameError = document.getElementById("usernameError");

    // Handle form submission
    form.addEventListener("submit", function (event) {
        let isValid = true;

        // Simple validation example
        if (usernameInput.value.trim() === "") {
            usernameError.textContent = "Email is required.";
            isValid = false;
        }

        if (passwordInput.value.trim() === "") {
            passwordError.textContent = "Password is required.";
            isValid = false;
        }

        if (!isValid) {
            event.preventDefault(); // Stop form submission
        }
    });
});