// NexusKeys Theme Manager - Enforces Epic Games Store Dark Aesthetic
window.themeManager = {
    getTheme: function() {
        return 'nexus';
    },
    setTheme: function(theme) {
        document.documentElement.setAttribute('data-theme', 'nexus');
        document.documentElement.classList.remove('light-theme');
        localStorage.setItem('nexus_theme', 'nexus');

        var themeColor = document.querySelector('meta[name="theme-color"]');
        if (themeColor) {
            themeColor.setAttribute('content', '#121212');
        }

        window.dispatchEvent(new CustomEvent('nexus-theme-changed', {
            detail: { theme: 'nexus' }
        }));
        return 'nexus';
    },
    initTheme: function() {
        return this.setTheme('nexus');
    },
    toggleTheme: function() {
        // Epic Games Store is permanently dark themed
        return this.setTheme('nexus');
    }
};

// Immediate initialization to prevent any light theme flashing
(function() {
    try {
        window.themeManager.initTheme();
    } catch(e) {}
})();
