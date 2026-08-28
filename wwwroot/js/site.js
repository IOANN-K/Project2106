document.addEventListener("DOMContentLoaded", () => {
    const themeToggle = document.getElementById("theme-toggle");
    const systemTheme = window.matchMedia("(prefers-color-scheme: dark)");

    const updateThemeControl = () => {
        const isDark = document.documentElement.dataset.theme === "dark";
        document.getElementById("theme-color")?.setAttribute(
            "content",
            isDark ? "#0e1512" : "#174f3a"
        );
        themeToggle?.setAttribute(
            "aria-label",
            isDark ? "Switch to light theme" : "Switch to dark theme"
        );
        themeToggle?.setAttribute(
            "title",
            isDark ? "Switch to light theme" : "Switch to dark theme"
        );
    };

    themeToggle?.addEventListener("click", () => {
        const nextTheme = document.documentElement.dataset.theme === "dark"
            ? "light"
            : "dark";

        document.documentElement.dataset.theme = nextTheme;
        try {
            localStorage.setItem("atlas-theme", nextTheme);
        } catch {
            // Theme still applies for the current page if storage is unavailable.
        }
        updateThemeControl();
    });

    systemTheme.addEventListener("change", event => {
        let hasSavedTheme = false;
        try {
            hasSavedTheme = Boolean(localStorage.getItem("atlas-theme"));
        } catch {
            hasSavedTheme = false;
        }

        if (!hasSavedTheme) {
            document.documentElement.dataset.theme = event.matches ? "dark" : "light";
            updateThemeControl();
        }
    });

    updateThemeControl();
});
