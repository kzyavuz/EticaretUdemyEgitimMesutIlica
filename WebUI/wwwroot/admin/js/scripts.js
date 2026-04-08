
window.addEventListener("DOMContentLoaded", (event) => {
    // Toggle the side navigation
    const sidebarToggle = document.body.querySelector("#sidebarToggle");
    if (sidebarToggle) {
        // Uncomment Below to persist sidebar toggle between refreshes
        // if (localStorage.getItem('sb|sidebar-toggle') === 'true') {
        //     document.body.classList.toggle('sb-sidenav-toggled');
        // }
        sidebarToggle.addEventListener("click", (event) => {
            event.preventDefault();
            document.body.classList.toggle("sb-sidenav-toggled");
            localStorage.setItem(
                "sb|sidebar-toggle",
                document.body.classList.contains("sb-sidenav-toggled"),
            );
        });
    }
});
/**
 * form-utils.js
 * Genel amaçlı form yardımcı fonksiyonları.
 * Tüm sayfalarda kullanılabilir.
 */

const FormUtils = (() => {

    // ---------------------------------------------------------------
    // Görsel Önizleme — Dosyadan
    // Kullanım: <input type="file" onchange="FormUtils.previewImage(this)" />
    // ---------------------------------------------------------------
    function previewImage(input, previewImgId = 'imagePreview', placeholderId = 'upload-placeholder', deleteBtnWrapperId = 'delete-btn-wrap', previewWrapId = null) {
        if (!input.files || !input.files[0]) return;

        const reader = new FileReader();
        reader.onload = function (e) {
            _showPreview(e.target.result, previewImgId, placeholderId, deleteBtnWrapperId, previewWrapId);
        };
        reader.readAsDataURL(input.files[0]);
    }

    // ---------------------------------------------------------------
    // Görsel Önizleme — URL'den
    // Kullanım: FormUtils.previewImageFromUrl('logoUrlInput', 'imagePreview', ...)
    // ---------------------------------------------------------------
    function previewImageFromUrl(urlInputId = 'logoUrlInput', previewImgId = 'imagePreview', placeholderId = 'upload-placeholder', deleteBtnWrapperId = 'delete-btn-wrap', logoUrlHiddenId = 'LogoUrl', previewWrapId = null) {
        const urlInput = document.getElementById(urlInputId);
        if (!urlInput) return;

        const url = urlInput.value.trim();
        if (!url) return;

        const img = new Image();
        img.onload = function () {
            _showPreview(url, previewImgId, placeholderId, deleteBtnWrapperId, previewWrapId);

            // URL'yi hidden input'a yaz (form submit için)
            const hidden = document.getElementById(logoUrlHiddenId);
            if (hidden) hidden.value = url;
        };
        img.onerror = function () {
            alert('Geçersiz görsel URL\'si. Lütfen doğrudan bir görsel bağlantısı girin.');
        };
        img.src = url;
    }

    // ---------------------------------------------------------------
    // Görsel Sıfırlama
    // Kullanım: FormUtils.deleteImage()
    // ---------------------------------------------------------------
    function deleteImage(inputId = 'logoInput', previewImgId = 'imagePreview', placeholderId = 'upload-placeholder', deleteBtnWrapperId = 'delete-btn-wrap', deleteFlagId = 'DeleteLogo') {
        const input = document.getElementById(inputId);
        if (input) input.value = '';

        const urlInput = document.getElementById('logoUrlInput');
        if (urlInput) urlInput.value = '';

        const hidden = document.getElementById('LogoUrl');
        if (hidden) hidden.value = '';

        // Her iki önizlemeyi sıfırla
        ['filePreview', 'urlPreview', previewImgId].forEach(id => {
            const el = document.getElementById(id);
            if (el) { el.src = ''; el.classList.add('d-none'); }
        });
        ['file-preview-wrap', 'url-preview-wrap'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.classList.add('d-none');
        });

        const placeholder = document.getElementById(placeholderId);
        if (placeholder) placeholder.classList.remove('d-none');

        if (deleteBtnWrapperId) {
            const deleteWrap = document.getElementById(deleteBtnWrapperId);
            if (deleteWrap) deleteWrap.classList.add('d-none');
        }

        if (deleteFlagId) {
            const flag = document.getElementById(deleteFlagId);
            if (flag) flag.value = 'true';
        }
    }

    function clearFileInput(inputId = 'logoInput', previewImgId = 'filePreview', previewWrapId = 'file-preview-wrap', placeholderId = 'file-upload-placeholder') {
        const input = document.getElementById(inputId);
        if (input) input.value = '';

        const img = document.getElementById(previewImgId);
        if (img) { img.src = ''; img.classList.add('d-none'); }

        const wrap = document.getElementById(previewWrapId);
        if (wrap) wrap.classList.add('d-none');

        const placeholder = document.getElementById(placeholderId);
        if (placeholder) placeholder.classList.remove('d-none');
    }

    function clearUrlInput(urlInputId = 'logoUrlInput', previewImgId = 'urlPreview', previewWrapId = 'url-preview-wrap', urlHiddenId = 'LogoUrl') {
        const urlInput = document.getElementById(urlInputId);
        if (urlInput) urlInput.value = '';

        const img = document.getElementById(previewImgId);
        if (img) { img.src = ''; img.classList.add('d-none'); }

        const wrap = document.getElementById(previewWrapId);
        if (wrap) wrap.classList.add('d-none');

        const hidden = document.getElementById(urlHiddenId);
        if (hidden) hidden.value = '';
    }

    // ---------------------------------------------------------------
    // Tab Geçişi (Dosya / URL)
    // Kullanım: FormUtils.initImageTabs('tab-file', 'tab-url', 'pane-file', 'pane-url')
    // ---------------------------------------------------------------
    function initImageTabs(fileTabId = 'tab-file', urlTabId = 'tab-url', filePaneId = 'pane-file', urlPaneId = 'pane-url') {
        const fileTab = document.getElementById(fileTabId);
        const urlTab = document.getElementById(urlTabId);
        const filePane = document.getElementById(filePaneId);
        const urlPane = document.getElementById(urlPaneId);

        if (!fileTab || !urlTab) return;

        fileTab.addEventListener('click', function () {
            fileTab.classList.add('active');
            urlTab.classList.remove('active');
            if (filePane) filePane.classList.remove('d-none');
            if (urlPane) urlPane.classList.add('d-none');
        });

        urlTab.addEventListener('click', function () {
            urlTab.classList.add('active');
            fileTab.classList.remove('active');
            if (urlPane) urlPane.classList.remove('d-none');
            if (filePane) filePane.classList.add('d-none');
        });
    }

    // ---------------------------------------------------------------
    // Ortak: Preview göster
    // ---------------------------------------------------------------
    function _showPreview(src, previewImgId, placeholderId, deleteBtnWrapperId, previewWrapId = null) {
        const img = document.getElementById(previewImgId);
        if (img) {
            img.src = src;
            img.classList.remove('d-none');
        }

        if (previewWrapId) {
            const wrap = document.getElementById(previewWrapId);
            if (wrap) wrap.classList.remove('d-none');
        }

        const placeholder = document.getElementById(placeholderId);
        if (placeholder) placeholder.classList.add('d-none');

        if (deleteBtnWrapperId) {
            const deleteWrap = document.getElementById(deleteBtnWrapperId);
            if (deleteWrap) deleteWrap.classList.remove('d-none');
        }
    }

    // ---------------------------------------------------------------
    // Toggle Switch
    // Kullanım: FormUtils.ToggleSwitch.init('toggleSwitch', 'isActiveCheckbox', true, { ... })
    // ---------------------------------------------------------------
    const ToggleSwitch = (() => {

        const ACTIVE_CLASSES = 'bg-success bg-opacity-25 border border-success rounded-pill position-relative flex-shrink-0 status-toggle-button';
        const INACTIVE_CLASSES = 'bg-secondary bg-opacity-25 border border-secondary rounded-pill position-relative flex-shrink-0 status-toggle-button';

        function init(switchId = 'toggleSwitch', checkboxId = 'isActiveCheckbox', initialState = true, labels = null) {
            const sw = document.getElementById(switchId);
            if (!sw) return;

            let state = initialState;
            _applyState(sw, state, checkboxId, labels);

            sw.addEventListener('click', function () {
                state = !state;
                _applyState(sw, state, checkboxId, labels);
            });
        }

        function _applyState(sw, state, checkboxId, labels) {
            const thumb = sw.querySelector('[data-toggle-thumb]') || sw.querySelector('.toggle-thumb');
            const checkbox = document.getElementById(checkboxId);

            sw.className = (state ? ACTIVE_CLASSES : INACTIVE_CLASSES);

            if (thumb) thumb.style.left = state ? '20px' : '3px';
            if (checkbox) checkbox.checked = state;

            if (labels) {
                const labelEl = document.getElementById(labels.labelId);
                const subEl = document.getElementById(labels.subId);
                if (labelEl) labelEl.textContent = state ? labels.activeLabel : labels.inactiveLabel;
                if (subEl) subEl.textContent = state ? labels.activeSub : labels.inactiveSub;
            }
        }

        return { init };
    })();

    // ---------------------------------------------------------------
    // Drag & Drop Desteği
    // Kullanım: FormUtils.initDropZone('drop-zone', 'logoInput')
    // ---------------------------------------------------------------
    function initDropZone(dropZoneId = 'drop-zone', fileInputId = 'logoInput', onFileDrop = null) {
        const zone = document.getElementById(dropZoneId);
        const input = document.getElementById(fileInputId);
        if (!zone || !input) return;

        zone.addEventListener('dragover', (e) => {
            e.preventDefault();
            zone.classList.add('border-primary');
        });

        zone.addEventListener('dragleave', () => {
            zone.classList.remove('border-primary');
        });

        zone.addEventListener('drop', (e) => {
            e.preventDefault();
            zone.classList.remove('border-primary');

            const file = e.dataTransfer.files[0];
            if (!file) return;

            const dt = new DataTransfer();
            dt.items.add(file);
            input.files = dt.files;

            if (typeof onFileDrop === 'function') {
                onFileDrop(file);
            } else {
                input.dispatchEvent(new Event('change'));
            }
        });
    }

    return {
        previewImage,
        previewImageFromUrl,
        clearFileInput,
        clearUrlInput,
        deleteImage,
        initImageTabs,
        ToggleSwitch,
        initDropZone,
    };
})();

/**
 * Evrensel silme onayı — tüm admin sayfalarında kullanılır.
 * @param {number} id        - Silinecek kaydın id'si
 * @param {string} name      - Kullanıcıya gösterilecek kayıt adı
 * @param {string} title     - Swal başlığı (ör. 'Markayı Sil')
 * @param {string} formId    - Gizli formun id'si (varsayılan: 'deleteForm')
 * @param {string} inputId   - Gizli id inputunun id'si (varsayılan: 'deleteItemId')
 */
function confirmDelete(id, name, title, formId, inputId) {
    title   = title   || 'Kaydı Sil';
    formId  = formId  || 'deleteForm';
    inputId = inputId || 'deleteItemId';

    Swal.fire({
        title: title,
        html: '<p>Aşağıdaki kaydı silmek istediğinizden emin misiniz?</p><strong>' + name + '</strong>',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: '<i class="fas fa-trash me-1"></i> Evet, Sil',
        cancelButtonText: 'Vazgeç'
    }).then((result) => {
        if (result.isConfirmed) {
            var form = document.getElementById(formId);
            if (id !== null && id !== undefined) {
                document.getElementById(inputId).value = id;
            }
            form.submit();
        }
    });
}