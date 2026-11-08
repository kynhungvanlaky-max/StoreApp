window.adminPortalBridge = {
    signIn: function (baseUrl, token) {
        if (!token) {
            throw new Error("Admin portal access token is required.");
        }

        // Sanitize baseUrl: If empty, or pointing to API/storefront by mistake, route to admin
        if (!baseUrl || 
            baseUrl.indexOf("nexuskeys-api") !== -1 || 
            baseUrl.indexOf("nexuskeys-storefront") !== -1 ||
            baseUrl.indexOf("localhost:5021") !== -1 ||
            baseUrl.indexOf("localhost:5205") !== -1) {
            if (window.location.hostname === "localhost" || window.location.hostname === "127.0.0.1") {
                baseUrl = "http://localhost:3001";
            } else {
                baseUrl = "https://nexuskeys-admin.onrender.com";
            }
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
