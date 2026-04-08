window.addEventListener('DOMContentLoaded', event => {
    // Simple-DataTables
    const datatablesSimple = document.getElementById('datatablesSimple');
    if (datatablesSimple) {

        function initTooltips() {
            document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
                var existing = bootstrap.Tooltip.getInstance(el);
                if (existing) existing.dispose();
                new bootstrap.Tooltip(el, { container: 'body' });
            });
        }

        const dt = new simpleDatatables.DataTable(datatablesSimple, {
            perPage: 10,
            labels: {
                perPage: "satır göster",
                noRows: "Kayıt bulunamadı",
                noResults: "Arama kriterine uygun kayıt bulunamadı.",
                info: "{start} - {end} arası {rows} kayıttan gösteriliyor"
            }
        });

        // DataTable render olduktan sonra search placeholder'ı değiştir
        setTimeout(() => {
            const searchInput = document.querySelector('.datatable-input');
            if (searchInput) {
                searchInput.placeholder = "Ara...";
            }
        }, 0);

        // Her render sonrası tooltip'leri yeniden init et (dt.on kullanılmalı, addEventListener çalışmaz)
        dt.on('datatable.init', initTooltips);
        dt.on('datatable.page', initTooltips);
        dt.on('datatable.perpage', initTooltips);
        dt.on('datatable.search', initTooltips);
        dt.on('datatable.sort', initTooltips);
        dt.on('datatable.update', initTooltips);
    }
});