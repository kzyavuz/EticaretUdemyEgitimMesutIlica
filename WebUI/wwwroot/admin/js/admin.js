/* =============================================
   KOZLOW ADMIN — Main JavaScript
   ============================================= */

// ── Bulk Delete (Global Scope) ──
window.initBulkDelete = function (itemName) {
  const form = document.getElementById("bulkDeleteForm");
  const bulkBar = document.querySelector(".bulk-action-bar");

  if (!form || !bulkBar) return;

  const deleteBtn = bulkBar.querySelector(".btn-nx.danger-btn");
  if (!deleteBtn) return;

  deleteBtn.addEventListener("click", function () {
    // Wait for TableSelect initialization
    if (typeof window.TableSelectInstance === "undefined") return;
    const selected = window.TableSelectInstance.getSelected();
    if (selected.length === 0) return;

    Swal.fire({
      title: "Silme Onayı",
      html: `<strong>${selected.length}</strong> ${itemName} silinecek. Emin misiniz?`,
      icon: "warning",
      showCancelButton: true,
      confirmButtonColor: "#dc3545",
      cancelButtonColor: "#6c757d",
      confirmButtonText: "Evet, Sil!",
      cancelButtonText: "İptal Et",
    }).then((result) => {
      if (result.isConfirmed) {
        // Clear form
        form.innerHTML = "";

        // Re-add antiforgery token
        const tokenInput = document.querySelector(
          'input[name="__RequestVerificationToken"]',
        );
        if (tokenInput) {
          const newTokenInput = tokenInput.cloneNode(true);
          form.appendChild(newTokenInput);
        }

        // Add selected IDs
        selected.forEach((checkbox) => {
          const input = document.createElement("input");
          input.type = "hidden";
          input.name = "ids";
          input.value = checkbox.value;
          form.appendChild(input);
        });

        form.submit();
      }
    });
  });
};

// ── Single Delete (Global Scope) ──
window.confirmDelete = function (id, itemName, title) {
  Swal.fire({
    title: title || "Silme Onayı",
    html: `"<strong>${itemName}</strong>" silinecek. Emin misiniz?`,
    icon: "warning",
    showCancelButton: true,
    confirmButtonColor: "#dc3545",
    cancelButtonColor: "#6c757d",
    confirmButtonText: "Evet, Sil!",
    cancelButtonText: "İptal Et",
  }).then((result) => {
    if (result.isConfirmed) {
      const form = document.getElementById("deleteForm");
      if (form) {
        document.getElementById("deleteItemId").value = id;
        form.submit();
      }
    }
  });
};

(function () {
  "use strict";

  // ── Theme ──
  const ThemeManager = {
    KEY: "nexus-theme",
    current: null,

    init() {
      const saved = localStorage.getItem(this.KEY) || "dark";
      this.set(saved, false);
    },

    set(theme, animate = true) {
      if (animate) {
        // Fade out
        document.body.style.opacity = "0";
        document.body.style.transition = "opacity 0.2s ease-in-out";

        setTimeout(() => {
          // Change theme
          this.current = theme;
          document.documentElement.setAttribute("data-theme", theme);
          localStorage.setItem(this.KEY, theme);
          this.updateIcon();

          // Fade in
          document.body.style.opacity = "1";

          setTimeout(() => {
            document.body.style.transition = "";
          }, 200);
        }, 200);
      } else {
        this.current = theme;
        document.documentElement.setAttribute("data-theme", theme);
        localStorage.setItem(this.KEY, theme);
        this.updateIcon();
      }
    },

    toggle() {
      this.set(this.current === "dark" ? "light" : "dark");
    },

    updateIcon() {
      document.querySelectorAll(".theme-toggle").forEach((btn) => {
        btn.innerHTML =
          this.current === "dark"
            ? '<i class="fas fa-moon"></i>'
            : '<i class="fas fa-sun"></i>';
      });
    },
  };

  // ── Sidebar ──
  const Sidebar = {
    collapsed: false,

    init() {
      this.collapsed = localStorage.getItem("nx-sidebar") === "collapsed";
      if (this.collapsed) document.body.classList.add("sidebar-collapsed");

      // Legacy: Bootstrap sidebar toggle (#sidebarToggle)
      const legacySidebarToggle = document.body.querySelector("#sidebarToggle");
      if (legacySidebarToggle) {
        legacySidebarToggle.addEventListener("click", (event) => {
          event.preventDefault();
          document.body.classList.toggle("sb-sidenav-toggled");
          localStorage.setItem(
            "sb|sidebar-toggle",
            document.body.classList.contains("sb-sidenav-toggled"),
          );
        });
      }

      // Toggle button
      document.querySelectorAll(".topbar-toggle").forEach((btn) => {
        btn.addEventListener("click", () => this.toggle());
      });

      // Mobile overlay
      const overlay = document.querySelector(".sidebar-overlay");
      if (overlay) {
        overlay.addEventListener("click", () => this.closeMobile());
      }

      // Sub-nav toggles
      document.querySelectorAll(".nav-has-sub > .nav-link").forEach((link) => {
        link.addEventListener("click", (e) => {
          e.preventDefault();
          const parent = link.closest(".nav-has-sub");
          parent.classList.toggle("open");
        });
      });

      // Active link
      this.setActive();
    },

    toggle() {
      if (window.innerWidth < 992) {
        this.toggleMobile();
        return;
      }
      this.collapsed = !this.collapsed;
      document.body.classList.toggle("sidebar-collapsed", this.collapsed);
      localStorage.setItem(
        "nx-sidebar",
        this.collapsed ? "collapsed" : "expanded",
      );
    },

    toggleMobile() {
      const sidebar = document.querySelector(".sidebar");
      const overlay = document.querySelector(".sidebar-overlay");
      sidebar.classList.toggle("mobile-open");
      overlay.classList.toggle("active");
    },

    closeMobile() {
      document.querySelector(".sidebar")?.classList.remove("mobile-open");
      document.querySelector(".sidebar-overlay")?.classList.remove("active");
    },

    setActive() {
      const page = location.pathname.split("/").pop() || "index.html";
      document.querySelectorAll(".nav-link[href]").forEach((link) => {
        const href = link.getAttribute("href");
        if (
          href &&
          (href === page || page.includes(href.replace(".html", "")))
        ) {
          link.classList.add("active");
          const sub = link.closest(".nav-has-sub");
          if (sub) sub.classList.add("open");
        }
      });
    },
  };

  // ── Table Checkboxes ──
  const TableSelect = {
    init() {
      document.querySelectorAll(".check-all").forEach((master) => {
        master.addEventListener("change", () => {
          const table = master.closest("table") || document;
          table.querySelectorAll(".row-check").forEach((cb) => {
            cb.checked = master.checked;
          });
          this.updateBulkBar();
        });
      });

      document.querySelectorAll(".row-check").forEach((cb) => {
        cb.addEventListener("change", () => this.updateBulkBar());
      });
    },

    getSelected() {
      return [...document.querySelectorAll(".row-check:checked")];
    },

    updateBulkBar() {
      const selected = this.getSelected();
      const bar = document.querySelector(".bulk-action-bar");
      if (!bar) return;
      const countEl = bar.querySelector(".bulk-count");
      if (selected.length > 0) {
        bar.style.display = "flex";
        if (countEl) countEl.textContent = selected.length;
      } else {
        bar.style.display = "none";
      }
    },
  };

  // ── Image Upload Preview ──
  const ImageUpload = {
    init() {
      document.querySelectorAll(".upload-zone").forEach((zone) => {
        const input = zone.querySelector('input[type="file"]');
        const preview = zone
          .closest(".upload-wrapper")
          ?.querySelector(".image-previews");
        if (!input) return;

        input.addEventListener("change", () => {
          if (!preview) return;
          [...input.files].forEach((file) => {
            if (!file.type.startsWith("image/")) return;
            const reader = new FileReader();
            reader.onload = (e) => {
              const item = document.createElement("div");
              item.className = "image-preview-item";
              item.innerHTML = `
                <img src="${e.target.result}" style="width:100%;height:100%;object-fit:cover;border-radius:6px;" alt="">
                <div class="remove-img" onclick="this.closest('.image-preview-item').remove()">✕</div>
              `;
              preview.appendChild(item);
            };
            reader.readAsDataURL(file);
          });
        });

        // Drag & drop
        zone.addEventListener("dragover", (e) => {
          e.preventDefault();
          zone.style.borderColor = "var(--accent)";
          zone.style.background = "var(--accent-soft)";
        });

        zone.addEventListener("dragleave", () => {
          zone.style.borderColor = "";
          zone.style.background = "";
        });

        zone.addEventListener("drop", (e) => {
          e.preventDefault();
          zone.style.borderColor = "";
          zone.style.background = "";
          const dt = e.dataTransfer;
          if (dt.files) {
            const evt = new Event("change");
            input.files = dt.files;
            input.dispatchEvent(evt);
          }
        });
      });
    },
  };

  // ── Form Utilities ──
  const FormUtils = (() => {
    function previewImage(
      input,
      previewImgId = "imagePreview",
      placeholderId = "upload-placeholder",
      deleteBtnWrapperId = "delete-btn-wrap",
      previewWrapId = null,
    ) {
      if (!input.files || !input.files[0]) return;

      const reader = new FileReader();
      reader.onload = function (e) {
        _showPreview(
          e.target.result,
          previewImgId,
          placeholderId,
          deleteBtnWrapperId,
          previewWrapId,
        );
      };
      reader.readAsDataURL(input.files[0]);
    }

    function previewImageFromUrl(
      urlInputId = "logoUrlInput",
      previewImgId = "imagePreview",
      placeholderId = "upload-placeholder",
      deleteBtnWrapperId = "delete-btn-wrap",
      logoUrlHiddenId = "LogoUrl",
      previewWrapId = null,
    ) {
      const urlInput = document.getElementById(urlInputId);
      if (!urlInput) return;

      const url = urlInput.value.trim();
      if (!url) return;

      const img = new Image();
      img.onload = function () {
        _showPreview(
          url,
          previewImgId,
          placeholderId,
          deleteBtnWrapperId,
          previewWrapId,
        );

        const hidden = document.getElementById(logoUrlHiddenId);
        if (hidden) hidden.value = url;
      };
      img.onerror = function () {
        alert(
          "Geçersiz görsel URL'si. Lütfen doğrudan bir görsel bağlantısı girin.",
        );
      };
      img.src = url;
    }

    function deleteImage(
      inputId = "logoInput",
      previewImgId = "imagePreview",
      placeholderId = "upload-placeholder",
      deleteBtnWrapperId = "delete-btn-wrap",
      deleteFlagId = "DeleteLogo",
    ) {
      const input = document.getElementById(inputId);
      if (input) input.value = "";

      const urlInput = document.getElementById("logoUrlInput");
      if (urlInput) urlInput.value = "";

      const hidden = document.getElementById("LogoUrl");
      if (hidden) hidden.value = "";

      ["filePreview", "urlPreview", previewImgId].forEach((id) => {
        const el = document.getElementById(id);
        if (el) {
          el.src = "";
          el.classList.add("d-none");
        }
      });
      ["file-preview-wrap", "url-preview-wrap"].forEach((id) => {
        const el = document.getElementById(id);
        if (el) el.classList.add("d-none");
      });

      const placeholder = document.getElementById(placeholderId);
      if (placeholder) placeholder.classList.remove("d-none");

      if (deleteBtnWrapperId) {
        const deleteWrap = document.getElementById(deleteBtnWrapperId);
        if (deleteWrap) deleteWrap.classList.add("d-none");
      }

      if (deleteFlagId) {
        const flag = document.getElementById(deleteFlagId);
        if (flag) flag.value = "true";
      }
    }

    function clearFileInput(
      inputId = "logoInput",
      previewImgId = "filePreview",
      previewWrapId = "file-preview-wrap",
      placeholderId = "file-upload-placeholder",
    ) {
      const input = document.getElementById(inputId);
      if (input) input.value = "";

      const img = document.getElementById(previewImgId);
      if (img) {
        img.src = "";
        img.classList.add("d-none");
      }

      const wrap = document.getElementById(previewWrapId);
      if (wrap) wrap.classList.add("d-none");

      const placeholder = document.getElementById(placeholderId);
      if (placeholder) placeholder.classList.remove("d-none");
    }

    function clearUrlInput(
      urlInputId = "logoUrlInput",
      previewImgId = "urlPreview",
      previewWrapId = "url-preview-wrap",
      urlHiddenId = "LogoUrl",
    ) {
      const urlInput = document.getElementById(urlInputId);
      if (urlInput) urlInput.value = "";

      const img = document.getElementById(previewImgId);
      if (img) {
        img.src = "";
        img.classList.add("d-none");
      }

      const wrap = document.getElementById(previewWrapId);
      if (wrap) wrap.classList.add("d-none");

      const hidden = document.getElementById(urlHiddenId);
      if (hidden) hidden.value = "";
    }

    function initImageTabs(
      fileTabId = "tab-file",
      urlTabId = "tab-url",
      filePaneId = "pane-file",
      urlPaneId = "pane-url",
    ) {
      const fileTab = document.getElementById(fileTabId);
      const urlTab = document.getElementById(urlTabId);
      const filePane = document.getElementById(filePaneId);
      const urlPane = document.getElementById(urlPaneId);

      if (!fileTab || !urlTab) return;

      fileTab.addEventListener("click", function () {
        fileTab.classList.add("active");
        urlTab.classList.remove("active");
        if (filePane) filePane.classList.remove("d-none");
        if (urlPane) urlPane.classList.add("d-none");
      });

      urlTab.addEventListener("click", function () {
        urlTab.classList.add("active");
        fileTab.classList.remove("active");
        if (urlPane) urlPane.classList.remove("d-none");
        if (filePane) filePane.classList.add("d-none");
      });
    }

    function initDropZone(
      dropZoneId = "drop-zone",
      fileInputId = "logoInput",
      onFileDrop = null,
    ) {
      const zone = document.getElementById(dropZoneId);
      const input = document.getElementById(fileInputId);
      if (!zone || !input) return;

      zone.addEventListener("dragover", (e) => {
        e.preventDefault();
        zone.classList.add("border-primary");
      });

      zone.addEventListener("dragleave", () => {
        zone.classList.remove("border-primary");
      });

      zone.addEventListener("drop", (e) => {
        e.preventDefault();
        zone.classList.remove("border-primary");

        const file = e.dataTransfer.files[0];
        if (!file) return;

        const dt = new DataTransfer();
        dt.items.add(file);
        input.files = dt.files;

        if (typeof onFileDrop === "function") {
          onFileDrop(file);
        } else {
          input.dispatchEvent(new Event("change"));
        }
      });
    }

    function _showPreview(
      src,
      previewImgId,
      placeholderId,
      deleteBtnWrapperId,
      previewWrapId = null,
    ) {
      const img = document.getElementById(previewImgId);
      if (img) {
        img.src = src;
        img.classList.remove("d-none");
      }

      if (previewWrapId) {
        const wrap = document.getElementById(previewWrapId);
        if (wrap) wrap.classList.remove("d-none");
      }

      const placeholder = document.getElementById(placeholderId);
      if (placeholder) placeholder.classList.add("d-none");

      if (deleteBtnWrapperId) {
        const deleteWrap = document.getElementById(deleteBtnWrapperId);
        if (deleteWrap) deleteWrap.classList.remove("d-none");
      }
    }

    // Toggle Switch
    const ToggleSwitch = (() => {
      const ACTIVE_CLASSES =
        "bg-success bg-opacity-25 border border-success rounded-pill position-relative flex-shrink-0 status-toggle-button";
      const INACTIVE_CLASSES =
        "bg-secondary bg-opacity-25 border border-secondary rounded-pill position-relative flex-shrink-0 status-toggle-button";

      function init(
        switchId = "toggleSwitch",
        checkboxId = "isActiveCheckbox",
        initialState = true,
        labels = null,
      ) {
        const sw = document.getElementById(switchId);
        if (!sw) return;

        let state = initialState;
        _applyState(sw, state, checkboxId, labels);

        sw.addEventListener("click", function () {
          state = !state;
          _applyState(sw, state, checkboxId, labels);
        });
      }

      function _applyState(sw, state, checkboxId, labels) {
        const thumb =
          sw.querySelector("[data-toggle-thumb]") ||
          sw.querySelector(".toggle-thumb");
        const checkbox = document.getElementById(checkboxId);

        sw.className = state ? ACTIVE_CLASSES : INACTIVE_CLASSES;

        if (thumb) thumb.style.left = state ? "20px" : "3px";
        if (checkbox) checkbox.checked = state;

        if (labels) {
          const labelEl = document.getElementById(labels.labelId);
          const subEl = document.getElementById(labels.subId);
          if (labelEl)
            labelEl.textContent = state
              ? labels.activeLabel
              : labels.inactiveLabel;
          if (subEl)
            subEl.textContent = state ? labels.activeSub : labels.inactiveSub;
        }
      }

      return { init };
    })();

    return {
      previewImage,
      previewImageFromUrl,
      clearFileInput,
      clearUrlInput,
      deleteImage,
      initImageTabs,
      initDropZone,
      ToggleSwitch,
    };
  })();

  $(document).ready(function () {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(
      document.querySelectorAll('[data-bs-toggle="tooltip"]'),
    );
    tooltipTriggerList.map(function (tooltipTriggerEl) {
      return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    const table = $("#dataTable").DataTable({
      responsive: true,
      autoWidth: false,
      pageLength: 25,
      lengthMenu: [25, 50, 75, 100],
      language: {
        search: "",
        searchPlaceholder: "Tüm sütunlarda ara...",
        lengthMenu: "_MENU_ kayıt göster",
        info: "<strong>_TOTAL_</strong> üründen <strong>_START_</strong>–<strong>_END_</strong> gösteriliyor",
        infoEmpty: "Kayıt bulunamadı",
        infoFiltered: "(<strong>_MAX_</strong> içinden)",
        paginate: { first: "«", previous: "‹", next: "›", last: "»" },
        emptyTable: "Tabloda veri yok",
        zeroRecords: "Eşleşen ürün bulunamadı",
      },
      columnDefs: [
        {
          targets: 0,
          orderable: false,
          searchable: false,
          responsivePriority: 1,
        },
        { targets: 1, responsivePriority: 2 },
        { targets: 2, responsivePriority: 1 },
        {
          targets: -1,
          orderable: false,
          searchable: false,
          responsivePriority: 1,
        },
        { targets: "_all", responsivePriority: 4 },
      ],
      ordering: true, // Sıralama aktif
      order: [], // Varsayılan sıralama yok, backend sırası korunur
      dom: '<"dt-top-bar"lf>t<"dt-bottom-bar"ip>',
      initComplete: function () {
        $("#dataTable").css("visibility", "visible");
        $("#tableSpinner").fadeOut(250);
      },
    });

    // Re-initialize tooltips when table is redrawn
    table.on("draw", function () {
      const tooltipTriggerList = [].slice.call(
        document.querySelectorAll('[data-bs-toggle="tooltip"]'),
      );
      tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
      });
    });
  });

  // ── Init ──
  document.addEventListener("DOMContentLoaded", () => {
    ThemeManager.init();
    Sidebar.init();
    TableSelect.init();
    ImageUpload.init();

    // Expose TableSelect globally for initBulkDelete
    window.TableSelectInstance = TableSelect;

    // Theme toggle buttons
    document.querySelectorAll(".theme-toggle").forEach((btn) => {
      btn.addEventListener("click", () => ThemeManager.toggle());
    });

    // Expose globally
    window.NxTheme = ThemeManager;
  });

  // Expose FormUtils immediately (not waiting for DOMContentLoaded)
  window.FormUtils = FormUtils;
})();
