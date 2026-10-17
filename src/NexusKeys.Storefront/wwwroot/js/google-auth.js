window.googleAuthInterop = {
    client: null,
    clientId: null,
    dotNetObject: null,
    retryTimer: null,
    retryCount: 0,

    initialize: function (clientId, dotNetObject) {
        this.clientId = clientId;
        this.dotNetObject = dotNetObject;

        if (!window.google || !window.google.accounts) {
            clearTimeout(this.retryTimer);
            if (this.retryCount++ < 20) {
                this.retryTimer = setTimeout(() => this.initialize(clientId, dotNetObject), 500);
            }
            return false;
        }

        clearTimeout(this.retryTimer);
        this.retryCount = 0;
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
        return true;
    },

    prompt: function () {
        if (this.client) {
            this.client.requestAccessToken();
            return true;
        } else {
            return false;
        }
    }
};
