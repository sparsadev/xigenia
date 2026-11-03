// Xigenia appearance controller — Light / Dark / System (default)
// Reuses Bootstrap 5.3.3 data-bs-theme on <html>
// Storage key: xigenia.appearance | Supported: light, dark, system
(function () {
  var STORAGE_KEY = "xigenia.appearance";
  var VALID = { light: 1, dark: 1, system: 1 };
  var initialized = false;
  var mediaQuery = null;
  var currentPreference = "system";
  var listenerAttached = false;

  function normalize(pref) {
    if (typeof pref !== "string") return "system";
    var v = pref.trim().toLowerCase();
    return Object.prototype.hasOwnProperty.call(VALID, v) ? v : "system";
  }

  function getStoredPreference() {
    try {
      var raw = window.localStorage.getItem(STORAGE_KEY);
      if (raw === null || raw === undefined) return "system";
      return normalize(raw);
    } catch (_e) {
      return "system";
    }
  }

  function getEffectiveTheme(pref) {
    var n = normalize(pref || currentPreference);
    if (n === "light" || n === "dark") return n;
    try {
      if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches) return "dark";
    } catch (_e) {}
    return "light";
  }

  function applyEffectiveTheme(effective) {
    var theme = effective === "dark" ? "dark" : "light";
    try {
      document.documentElement.setAttribute("data-bs-theme", theme);
      // Keep color-scheme in sync for native controls
      document.documentElement.style.colorScheme = theme;
    } catch (_e) {}
  }

  function persistPreference(pref) {
    var n = normalize(pref);
    currentPreference = n;
    try {
      window.localStorage.setItem(STORAGE_KEY, n);
    } catch (_e) {}
    applyEffectiveTheme(getEffectiveTheme(n));
  }

  function onSystemChange() {
    if (currentPreference !== "system") return;
    applyEffectiveTheme(getEffectiveTheme("system"));
  }

  function ensureMediaListener() {
    if (listenerAttached) return;
    try {
      if (!window.matchMedia) return;
      mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
      if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener("change", onSystemChange);
      } else if (mediaQuery.addListener) {
        // Safari < 14 fallback
        mediaQuery.addListener(onSystemChange);
      }
      listenerAttached = true;
    } catch (_e) {}
  }

  function init() {
    if (initialized) return;
    initialized = true;
    currentPreference = getStoredPreference();
    applyEffectiveTheme(getEffectiveTheme(currentPreference));
    ensureMediaListener();
  }

  // Public API for Blazor interop and manual use
  var api = {
    init: init,
    getPreference: function () { return currentPreference; },
    getEffectiveTheme: function () { return getEffectiveTheme(currentPreference); },
    setPreference: function (pref) { persistPreference(pref); return currentPreference; },
    // Exposed for testing/inspection
    _normalize: normalize
  };

  // Expose globally
  try {
    window.xigeniaAppearance = api;
  } catch (_e) {}

  // Auto-init immediately to minimize flash (before CSS paints)
  init();
})();
