window.meuDinDinTheme = (() => {
    const key = "meudindin-theme";

    const resolveTheme = () => {
        const storedTheme = window.localStorage.getItem(key);
        if (storedTheme === "dark" || storedTheme === "light") {
            return storedTheme;
        }

        return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    };

    const applyTheme = (theme) => {
        document.documentElement.setAttribute("data-theme", theme);
        document.documentElement.style.colorScheme = theme;
        window.localStorage.setItem(key, theme);
        return theme;
    };

    return {
        getPreferredTheme: () => applyTheme(resolveTheme()),
        toggleTheme: () => applyTheme(resolveTheme() === "dark" ? "light" : "dark")
    };
})();
