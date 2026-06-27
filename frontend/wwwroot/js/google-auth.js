window.googleAuthInterop = {
    client: null,

    initialize: function (clientId, dotNetObject) {
        if (!window.google || !window.google.accounts) {
            console.error("Google Identity Services script not loaded.");
            return;
        }

        // Initialize Token Client (OAuth2 Implicit Flow) to support Custom Buttons
        this.client = window.google.accounts.oauth2.initTokenClient({
            client_id: clientId,
            scope: 'https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile',
            callback: (tokenResponse) => {
                if (tokenResponse && tokenResponse.access_token) {
                    // Return the access_token back to Blazor
                    dotNetObject.invokeMethodAsync("HandleGoogleLogin", tokenResponse.access_token);
                }
            },
        });
    },

    prompt: function () {
        if (this.client) {
            this.client.requestAccessToken();
        } else {
            console.error("Google Token Client not initialized. Check if Client ID is provided.");
        }
    }
};
