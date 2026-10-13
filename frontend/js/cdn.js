/* ============================================================
   CDN LOADER - Mitsubishi Service Booking System
   Semua external resource di-load dari sini.
   Update versi Kendo/jQuery cukup di file ini saja.
   ============================================================ */
(function () {
    var KENDO_THEME = 'https://kendo.cdn.telerik.com/themes/14.5.0/default/default-main.css';
    var JQUERY_URL  = 'https://code.jquery.com/jquery-3.7.1.min.js';
    var KENDO_JS    = 'https://kendo.cdn.telerik.com/2026.3.811/js/kendo.all.min.js';

    // CSS
    document.write('<link rel="stylesheet" href="' + KENDO_THEME + '" />');
    // Fonts (Inter — modern, professional)
    document.write('<link rel="preconnect" href="https://fonts.googleapis.com">');
    document.write('<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>');
    document.write('<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">');

    // JS — urutan penting: jQuery dulu, baru Kendo
    document.write('<script src="' + JQUERY_URL + '"><\/script>');
    document.write('<script src="' + KENDO_JS + '"><\/script>');
})();