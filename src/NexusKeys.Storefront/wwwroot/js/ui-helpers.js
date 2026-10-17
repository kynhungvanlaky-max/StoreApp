window.nexusUi = {
    copyText: async function (text) {
        await navigator.clipboard.writeText(text);
        return true;
    },

    share: async function (payload) {
        if (navigator.share) {
            try {
                await navigator.share(payload);
                return "shared";
            } catch (error) {
                if (error && error.name === "AbortError") {
                    return "cancelled";
                }
                throw error;
            }
        }

        await navigator.clipboard.writeText(payload.url);
        return "copied";
    }
};

(function () {
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    let observer;
    let motionFrame;

    function initializeMotion() {
        if (reducedMotion.matches) {
            document.querySelectorAll(".nui-reveal").forEach((element) => element.classList.add("nui-visible"));
            return;
        }

        if (!observer) {
            observer = new IntersectionObserver((entries) => {
                entries.forEach((entry) => {
                    if (!entry.isIntersecting) return;
                    entry.target.classList.add("nui-visible");
                    observer.unobserve(entry.target);
                });
            }, { rootMargin: "200px 0px", threshold: 0.01 });
        }

        const revealTargets = document.querySelectorAll("[data-nui-reveal], .nui-home > div > section:not(:first-of-type)");
        revealTargets.forEach((element, index) => {
            if (element.dataset.nuiMotionReady === "true") return;
            element.dataset.nuiMotionReady = "true";
            element.classList.add("nui-reveal");
            element.style.transitionDelay = `${Math.min(index % 3, 2) * 40}ms`;

            const rect = element.getBoundingClientRect();
            if (rect.top < window.innerHeight + 150) {
                element.classList.add("nui-visible");
            } else {
                observer.observe(element);
            }
        });

        document.querySelectorAll("figure, .nui-product-media, .nui-detail-gallery").forEach((element) => element.classList.add("nui-media"));
    }

    function scheduleMotion() {
        cancelAnimationFrame(motionFrame);
        motionFrame = requestAnimationFrame(initializeMotion);
    }

    document.addEventListener("DOMContentLoaded", scheduleMotion);
    new MutationObserver(scheduleMotion).observe(document.documentElement, { childList: true, subtree: true });
    window.nexusUi.initializeMotion = scheduleMotion;
})();
