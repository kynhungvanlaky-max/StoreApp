window.adminPortalBridge = {
    signIn: function (baseUrl, token) {
        if (!baseUrl || !token) {
            throw new Error("Admin portal URL and access token are required.");
        }

        const parsedUrl = new URL(baseUrl, window.location.origin);
        if (parsedUrl.protocol !== "http:" && parsedUrl.protocol !== "https:") {
            throw new Error("Unsupported admin portal protocol.");
        }

        const form = document.createElement("form");
        form.method = "POST";
        form.action = `${parsedUrl.href.replace(/\/+$/, "")}/Auth/Bridge`;
        form.target = "_self";
        form.style.display = "none";

        const tokenInput = document.createElement("input");
        tokenInput.type = "hidden";
        tokenInput.name = "token";
        tokenInput.value = token;
        form.appendChild(tokenInput);

        document.body.appendChild(form);
        form.submit();
    }
};
